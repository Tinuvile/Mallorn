using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// Product实体的Repository实现类
    /// 继承基础Repository，提供Product特有的查询和操作方法
    /// </summary>
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(CampusTradeDbContext context) : base(context) { }

        #region 读取操作
        /// <summary>
        /// 根据用户ID分页获取商品
        /// </summary>
        public async Task<(IEnumerable<Product> Products, int TotalCount)> GetByUserIdAsync(int userId)
        {
            var query = _dbSet.Where(p => p.UserId == userId);
            var totalCount = await query.CountAsync();
            var products = await query.Include(p => p.User).Include(p => p.Category).Include(p => p.ProductImages).OrderByDescending(p => p.PublishTime).ToListAsync();
            return (products, totalCount);
        }
        /// <summary>
        /// 根据分类ID分页获取商品
        /// </summary>
        public async Task<(IEnumerable<Product> Products, int TotalCount)> GetByCategoryIdAsync(int categoryId)
        {
            var query = _dbSet.Where(p => p.CategoryId == categoryId && p.Status == Product.ProductStatus.OnSale);
            var totalCount = await query.CountAsync();
            var products = await query.Include(p => p.User).Include(p => p.Category).Include(p => p.ProductImages).OrderByDescending(p => p.PublishTime).ToListAsync();
            return (products, totalCount);
        }
        /// <summary>
        /// 根据标题模糊查询商品
        /// </summary>
        public async Task<(IEnumerable<Product> Products, int TotalCount)> GetByTitleAsync(string title)
        {
            var query = _dbSet.Where(p => p.Title.Contains(title) && p.Status == Product.ProductStatus.OnSale);
            var totalCount = await query.CountAsync();
            var products = await query.Include(p => p.User).Include(p => p.Category).Include(p => p.ProductImages).OrderByDescending(p => p.PublishTime).ToListAsync();
            return (products, totalCount);
        }
        /// <summary>
        /// 判断指定用户下商品标题是否存在
        /// </summary>
        public async Task<bool> IsProductExistsAsync(string title, int userId)
        {
            var count = await _dbSet.CountAsync(p => p.Title == title && p.UserId == userId);
            return count > 0;
        }
        /// <summary>
        /// 获取商品总数
        /// </summary>
        public async Task<int> GetTotalProductsNumberAsync()
        {
            return await _dbSet.CountAsync();
        }
        /// <summary>
        /// 获取浏览量最高的商品
        /// </summary>
        public async Task<IEnumerable<Product>> GetTopViewProductsAsync(int count)
        {
            return await _dbSet.Where(p => p.Status == Product.ProductStatus.OnSale).Include(p => p.User).Include(p => p.Category).Include(p => p.ProductImages).OrderByDescending(p => p.ViewCount).Take(count).ToListAsync();
        }
        /// <summary>
        /// 分页多条件查询商品
        /// </summary>
        public async Task<(IEnumerable<Product> Products, int TotalCount)> GetPagedProductsAsync(
            int pageIndex,
            int pageSize,
            int? categoryId = null,
            string? status = null,
            string? keyword = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? userId = null)
        {
            var query = _dbSet.AsQueryable();
            if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
            if (!string.IsNullOrEmpty(status)) query = query.Where(p => p.Status == status);
            if (!string.IsNullOrEmpty(keyword)) query = query.Where(p => p.Title.Contains(keyword) || p.Description!.Contains(keyword));
            if (minPrice.HasValue) query = query.Where(p => p.BasePrice >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(p => p.BasePrice <= maxPrice.Value);
            if (userId.HasValue) query = query.Where(p => p.UserId == userId.Value);
            var totalCount = await query.CountAsync();
            var products = await query.Include(p => p.User).Include(p => p.Category).Include(p => p.ProductImages).OrderByDescending(p => p.PublishTime).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return (products, totalCount);
        }
        /// <summary>
        /// 获取即将自动下架的商品
        /// </summary>
        public async Task<IEnumerable<Product>> GetAutoRemoveProductsAsync(DateTime beforeTime)
        {
            return await _dbSet.Where(p => p.AutoRemoveTime.HasValue && p.AutoRemoveTime.Value <= beforeTime && p.Status == Product.ProductStatus.OnSale).Include(p => p.User).ToListAsync();
        }
        /// <summary>
        /// 获取商品图片URL集合
        /// </summary>
        public async Task<IEnumerable<string>> GetProductImagesAsync(int productId)
        {
            return await _context.Set<ProductImage>().Where(pi => pi.ProductId == productId).Select(pi => pi.ImageUrl).ToListAsync();
        }
        #endregion

        #region 更新操作
        /// <summary>
        /// 设置商品状态
        /// </summary>
        public async Task<Product> SetProductStatusAsync(int productId, string status)
        {
            var product = await GetByPrimaryKeyAsync(productId);
            if (product == null) throw new ArgumentException($"商品ID {productId} 不存在");
            product.Status = status;
            Update(product);
            return product;
        }
        /// <summary>
        /// 更新商品详情
        /// </summary>
        public async Task<Product?> UpdateProductDetailsAsync(int productId, string? title = null, string? description = null, decimal? basePrice = null)
        {
            var product = await GetByPrimaryKeyAsync(productId);
            if (product == null) return null;
            if (!string.IsNullOrEmpty(title)) product.Title = title;
            if (!string.IsNullOrEmpty(description)) product.Description = description;
            if (basePrice.HasValue) product.BasePrice = basePrice.Value;
            Update(product);
            return product;
        }
        /// <summary>
        /// 增加商品浏览量
        /// </summary>
        public async Task IncreaseViewCountAsync(int productId)
        {
            await _context.Database.ExecuteSqlRawAsync("UPDATE PRODUCTS SET VIEW_COUNT = VIEW_COUNT + 1 WHERE PRODUCT_ID = {0}", productId);
        }
        #endregion

        #region 删除操作
        /// <summary>
        /// 逻辑删除商品（下架）
        /// </summary>
        public async Task<bool> DeleteProductAsync(int productId)
        {
            var product = await GetByPrimaryKeyAsync(productId);
            if (product == null) return false;
            product.Status = Product.ProductStatus.OffShelf;
            Update(product);
            return true;
        }
        /// <summary>
        /// 批量逻辑删除用户的所有商品
        /// </summary>
        public async Task<int> DeleteProductsByUserAsync(int userId)
        {
            var products = await _dbSet.Where(p => p.UserId == userId).ToListAsync();
            foreach (var product in products) product.Status = Product.ProductStatus.OffShelf;
            UpdateRange(products);
            return products.Count;
        }
        #endregion

        #region 关系查询
        /// <summary>
        /// 查询商品及其订单信息
        /// </summary>
        public async Task<Product?> GetProductWithOrdersAsync(int productId)
        {
            return await _dbSet.Include(p => p.User).Include(p => p.Category).Include(p => p.ProductImages).FirstOrDefaultAsync(p => p.ProductId == productId);
        }
        #endregion
    }
}
