using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CampusTrade.API.Data;
using CampusTrade.API.Infrastructure.Utils.Cache;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Options;
using CampusTrade.API.Services.Cache;
using CampusTrade.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CampusTrade.API.Services.Cache
{
    public class ProductCacheService : IProductCacheService
    {
        private readonly ICacheService _cache;
        private readonly CampusTradeDbContext _context;
        private readonly CacheOptions _options;
        private readonly ILogger<ProductCacheService> _logger;
        private readonly SemaphoreSlim _productLock = new(1, 1);
        private readonly SemaphoreSlim _categoryProductsLock = new(1, 1);
        private readonly SemaphoreSlim _userProductsLock = new(1, 1);

        // 内存缓存用于频繁访问的基本产品信息
        private static readonly ConcurrentDictionary<int, Models.Entities.Product> _basicProductCache = new();

        public ProductCacheService(
            ICacheService cache,
            CampusTradeDbContext context,
            IOptions<CacheOptions> options,
            ILogger<ProductCacheService> logger)
        {
            _cache = cache;
            _context = context;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<Models.Entities.Product?> GetProductAsync(int productId)
        {
            var key = CacheKeyHelper.ProductKey(productId);

            try
            {
                // 1. 检查内存缓存
                if (_basicProductCache.TryGetValue(productId, out var cachedProduct))
                    return cachedProduct is NullProduct ? null : cachedProduct;

                // 2. 检查分布式缓存或查询数据库
                return await _cache.GetOrCreateAsync(key, async () =>
                {
                    var product = await _context.Products
                        .Include(p => p.ProductImages)
                        .FirstOrDefaultAsync(p => p.ProductId == productId);

                    if (product != null)
                        _basicProductCache[productId] = product; // 更新内存缓存

                    return product ?? NullProduct.Instance;
                }, _options.ProductCacheDuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get product cache for ProductId: {ProductId}", productId);
                return await _context.Products.FindAsync(productId); // 降级查询
            }
        }

        public async Task SetProductAsync(Models.Entities.Product product)
        {
            var key = CacheKeyHelper.ProductKey(product.ProductId);

            await _productLock.WaitAsync();
            try
            {
                // 更新内存缓存
                _basicProductCache[product.ProductId] = product;

                // 更新分布式缓存
                await _cache.SetAsync(key, product, _options.ProductCacheDuration);
            }
            finally
            {
                _productLock.Release();
            }
        }

        public async Task<Dictionary<int, Models.Entities.Product>> GetProductsAsync(IEnumerable<int> productIds)
        {
            var result = new Dictionary<int, Models.Entities.Product>();
            var missingIds = new List<int>();

            // 首先检查内存缓存
            foreach (var productId in productIds)
            {
                if (_basicProductCache.TryGetValue(productId, out var product) && product is not NullProduct)
                {
                    result[productId] = product;
                }
                else
                {
                    missingIds.Add(productId);
                }
            }

            if (missingIds.Count == 0)
            {
                return result;
            }

            // 然后检查分布式缓存
            var cacheKeys = missingIds.Select(CacheKeyHelper.ProductKey).ToList();
            var cachedProducts = await _cache.GetAllAsync<Models.Entities.Product>(cacheKeys);

            foreach (var pair in cachedProducts)
            {
                var key = pair.Key;
                var product = pair.Value;

                if (product is not NullProduct)
                {
                    var productId = int.Parse(key.Split(':').Last());
                    result[productId] = product;
                    _basicProductCache[productId] = product;
                    missingIds.Remove(productId);
                }
            }

            if (missingIds.Count == 0)
            {
                return result;
            }

            // 最后从数据库获取剩余产品
            var dbProducts = await _context.Products
                .Where(p => missingIds.Contains(p.ProductId))
                .Include(p => p.ProductImages)
                .ToListAsync();

            foreach (var product in dbProducts)
            {
                result[product.ProductId] = product;
                _basicProductCache[product.ProductId] = product; // 填充内存缓存
                await SetProductAsync(product); // 填充分布式缓存
            }

            return result;
        }

        public async Task<List<Models.Entities.Product>> GetProductsByCategoryAsync(int categoryId, int pageIndex, int pageSize)
        {
            var key = CacheKeyHelper.ProductListKey(categoryId, pageIndex, pageSize);

            await _categoryProductsLock.WaitAsync();
            try
            {
                var result = await _cache.GetOrCreateAsync(key, async () =>
                {
                    return await _context.Products
                    .Where(p => p.CategoryId == categoryId && p.Status == "在售")
                    .OrderByDescending(p => p.PublishTime)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .Include(p => p.ProductImages)
                    .ToListAsync();
                }, _options.ProductCacheDuration);

                return result ?? new List<Models.Entities.Product>(); // 确保不返回null
            }
            finally
            {
                _categoryProductsLock.Release();
            }
        }

        public async Task<List<Models.Entities.Product>> GetProductsByUserAsync(int userId, int pageIndex, int pageSize)
        {
            var key = CacheKeyHelper.ProductListKey(null, pageIndex, pageSize);

            await _userProductsLock.WaitAsync();
            try
            {
                var result = await _cache.GetOrCreateAsync(key, async () =>
                {
                    return await _context.Products
                        .Where(p => p.UserId == userId)
                        .OrderByDescending(p => p.PublishTime)
                        .Skip((pageIndex - 1) * pageSize)
                        .Take(pageSize)
                        .Include(p => p.ProductImages)
                        .ToListAsync();
                }, _options.ProductCacheDuration); // 用户产品列表缓存时间

                return result ?? new List<Models.Entities.Product>(); // 确保不返回null
            }
            finally
            {
                _userProductsLock.Release();
            }
        }

        public async Task RemoveAllProductDataAsync(int productId)
        {
            var tasks = new List<Task>
            {
                _cache.RemoveAsync(CacheKeyHelper.ProductKey(productId))
            };

            // 从内存缓存中移除
            _basicProductCache.TryRemove(productId, out _);

            // 注意: 这里可能需要清理相关的列表缓存，但实现较复杂
            // 可以考虑使用缓存标签或其他机制来管理关联缓存

            await Task.WhenAll(tasks);
            _logger.LogInformation("Cleared all cache data for ProductId: {ProductId}", productId);
        }

        public async Task RefreshCategoryProductsAsync(int categoryId)
        {
            await _categoryProductsLock.WaitAsync();
            try
            {
                _logger.LogInformation("开始刷新分类 {CategoryId} 的商品缓存...", categoryId);

                // 1. 获取该分类下所有在售商品
                var activeProducts = await _context.Products
                    .Where(p => p.CategoryId == categoryId && p.Status == "在售")
                    .Include(p => p.ProductImages)
                    .ToListAsync();

                // 2. 清理旧缓存
                var cacheKeyPrefix = CacheKeyHelper.GetCategoryProductsCachePrefix(categoryId);
                await _cache.RemoveByPrefixAsync(cacheKeyPrefix);

                // 3. 更新内存缓存
                foreach (var product in activeProducts)
                {
                    _basicProductCache[product.ProductId] = product;
                }

                // 4. 重新加载分页缓存（预加载前3页）
                var refreshTasks = new List<Task>();
                for (int page = 1; page <= 3; page++)
                {
                    refreshTasks.Add(GetProductsByCategoryAsync(categoryId, page, 20));
                }

                // 5. 并行重新加载所有分页
                await Task.WhenAll(refreshTasks);

                _logger.LogInformation("成功刷新分类 {CategoryId} 的缓存，共处理 {Count} 个商品",
                categoryId, activeProducts.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新分类 {CategoryId} 缓存时发生错误", categoryId);
                throw;
            }
            finally
            {
                _categoryProductsLock.Release();
            }
        }

        public async Task<double> GetHitRate()
        {
            return await _cache.GetHitRate();
        }

        // Null object pattern for product
        private class NullProduct : Models.Entities.Product
        {
            public static readonly NullProduct Instance = new();
            private NullProduct() { }
        }

        public async Task InvalidateProductCacheAsync(int productId)
        {
            try
            {
                // 1. 移除内存缓存
                _basicProductCache.TryRemove(productId, out _);

                // 2. 移除分布式缓存中的产品详情
                await _cache.RemoveAsync(CacheKeyHelper.ProductKey(productId));

                // 3. 移除产品统计信息缓存
                await _cache.RemoveAsync(CacheKeyHelper.ProductStatsKey(productId));

                _logger.LogInformation("已失效产品 {ProductId} 的所有缓存", productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "失效产品 {ProductId} 缓存时出错", productId);
            }
        }

        public async Task InvalidateProductsByCategoryAsync(int categoryId)
        {
            try
            {
                // 获取分类下所有产品ID
                var productIds = await _context.Products
                    .Where(p => p.CategoryId == categoryId)
                    .Select(p => p.ProductId)
                    .ToListAsync();

                // 批量失效这些产品的缓存
                foreach (var productId in productIds)
                {
                    await InvalidateProductCacheAsync(productId);
                }

                // 失效分类产品列表缓存
                await _cache.RemoveByPrefixAsync(CacheKeyHelper.GetCategoryProductsCachePrefix(categoryId));

                _logger.LogInformation("已失效分类 {CategoryId} 下所有产品的缓存", categoryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "失效分类 {CategoryId} 产品缓存时出错", categoryId);
            }
        }
        public async Task<List<int>> GetActiveCategoryIdsAsync()
        {
            return await _context.Products
                .Where(p => p.Status == "在售")
                .Select(p => p.CategoryId)
                .Distinct()
                .ToListAsync();
        }
        public async Task RefreshAllActiveProductsAsync()
        {
            var activeProductIds = await _context.Products
                .Where(p => p.Status == "在售")
                .Select(p => p.ProductId)
                .ToListAsync();

            foreach (var productId in activeProductIds)
            {
                var product = await _context.Products
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.ProductId == productId);

                if (product != null)
                {
                    await SetProductAsync(product);
                }
            }

            // 同时刷新分类缓存
            var categoryIds = await GetActiveCategoryIdsAsync();
            foreach (var categoryId in categoryIds)
            {
                await RefreshCategoryProductsAsync(categoryId);
            }
        }
    }
    // ProductCacheService.cs

}
