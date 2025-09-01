using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// Product实体的Repository接口
    /// 继承基础IRepository，提供Product特有的查询和操作方法
    /// </summary>
    public interface IProductRepository : IRepository<Product>
    {
        #region 创建操作
        // 商品创建由基础仓储 AddAsync 提供
        #endregion

        #region 读取操作
        /// <summary>
        /// 根据用户ID分页获取商品
        /// </summary>
        Task<(IEnumerable<Product> Products, int TotalCount)> GetByUserIdAsync(int userId);
        /// <summary>
        /// 根据分类ID分页获取商品
        /// </summary>
        Task<(IEnumerable<Product> Products, int TotalCount)> GetByCategoryIdAsync(int categoryId);
        /// <summary>
        /// 根据标题模糊查询商品
        /// </summary>
        Task<(IEnumerable<Product> Products, int TotalCount)> GetByTitleAsync(string title);
        /// <summary>
        /// 判断指定用户下商品标题是否存在
        /// </summary>
        Task<bool> IsProductExistsAsync(string title, int userId);
        /// <summary>
        /// 获取商品总数
        /// </summary>
        Task<int> GetTotalProductsNumberAsync();
        /// <summary>
        /// 获取浏览量最高的商品
        /// </summary>
        Task<IEnumerable<Product>> GetTopViewProductsAsync(int count);
        /// <summary>
        /// 分页多条件查询商品
        /// </summary>
        Task<(IEnumerable<Product> Products, int TotalCount)> GetPagedProductsAsync(
            int pageIndex,
            int pageSize,
            int? categoryId = null,
            string? status = null,
            string? keyword = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? userId = null);
        /// <summary>
        /// 获取即将自动下架的商品
        /// </summary>
        Task<IEnumerable<Product>> GetAutoRemoveProductsAsync(DateTime beforeTime);
        /// <summary>
        /// 获取商品图片URL集合
        /// </summary>
        Task<IEnumerable<string>> GetProductImagesAsync(int productId);
        #endregion

        #region 更新操作
        /// <summary>
        /// 设置商品状态
        /// </summary>
        Task<Product> SetProductStatusAsync(int productId, string status);
        /// <summary>
        /// 更新商品详情
        /// </summary>
        Task<Product?> UpdateProductDetailsAsync(
            int productId,
            string? title = null,
            string? description = null,
            decimal? basePrice = null
        );
        /// <summary>
        /// 增加商品浏览量
        /// </summary>
        Task IncreaseViewCountAsync(int productId);
        #endregion

        #region 删除操作
        /// <summary>
        /// 逻辑删除商品（下架）
        /// </summary>
        Task<bool> DeleteProductAsync(int productId);
        /// <summary>
        /// 批量逻辑删除用户的所有商品
        /// </summary>
        Task<int> DeleteProductsByUserAsync(int userId);
        #endregion

        #region 关系查询
        /// <summary>
        /// 查询商品及其订单信息
        /// </summary>
        Task<Product?> GetProductWithOrdersAsync(int productId);
        #endregion
    }
}
