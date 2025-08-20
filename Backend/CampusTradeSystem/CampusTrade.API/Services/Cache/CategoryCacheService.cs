using System.Collections.Generic;
using System.Linq;
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
    public class CategoryCacheService : ICategoryCacheService
    {
        private readonly ICacheService _cache;
        private readonly CampusTradeDbContext _context;
        private readonly CacheOptions _options;
        private readonly ILogger<CategoryCacheService> _logger;
        private readonly SemaphoreSlim _treeLock = new(1, 1);

        public CategoryCacheService(
            ICacheService cache,
            CampusTradeDbContext context,
            IOptions<CacheOptions> options,
            ILogger<CategoryCacheService> logger)
        {
            _cache = cache;
            _options = options.Value;
            _context = context;
            _logger = logger;
        }

        public async Task<List<Category>> GetCategoryTreeAsync()
        {
            var key = CacheKeyHelper.CategoryTreeKey();
            await _treeLock.WaitAsync();
            try
            {
                var result = await _cache.GetOrCreateAsync(key, async () =>
                {
                    var flatCategories = await _context.Categories
                        .AsNoTracking()
                        .ToListAsync();
                    return BuildCategoryTree(flatCategories, parentId: null);
                }, _options.CategoryCacheDuration); // 使用配置值

                return result ?? new List<Category>();
            }
            finally
            {
                _treeLock.Release();
            }
        }

        public async Task RefreshCategoryTreeAsync()
        {
            await _treeLock.WaitAsync();
            try
            {
                await _cache.RemoveAsync(CacheKeyHelper.CategoryTreeKey());
                await GetCategoryTreeAsync(); // 强制立即重建缓存
                _logger.LogInformation("分类树缓存已强制刷新并重建");
            }
            finally
            {
                _treeLock.Release();
            }
        }

        public async Task InvalidateCategoryTreeCacheAsync()
        {
            await _treeLock.WaitAsync();
            try
            {
                await _cache.RemoveAsync(CacheKeyHelper.CategoryTreeKey());
                _logger.LogInformation("分类树缓存已失效");
            }
            finally
            {
                _treeLock.Release();
            }
        }

        private static List<Category> BuildCategoryTree(List<Category> flatCategories, int? parentId)
        {
            return flatCategories
            .Where(c => c.ParentId == parentId)
            .OrderBy(c => c.CategoryId) // 改用ID排序
            // 或 .OrderBy(c => c.Name)  // 改用名称排序
            .Select(c => new Category
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Children = BuildCategoryTree(flatCategories, c.CategoryId)
            })
            .ToList();
        }
    }
}
