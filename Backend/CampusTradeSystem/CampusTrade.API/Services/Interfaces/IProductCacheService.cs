using System.Collections.Generic;
using System.Threading.Tasks;
using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Services.Interfaces
{
    /// <summary>
    /// 商品缓存服务接口
    /// </summary>
    public interface IProductCacheService
    {
        /// <summary>
        /// 获取单个产品信息缓存
        /// </summary>
        Task<Models.Entities.Product?> GetProductAsync(int productId);

        /// <summary>
        /// 设置产品信息缓存
        /// </summary>
        Task SetProductAsync(Models.Entities.Product product);

        /// <summary>
        /// 批量获取产品缓存
        /// </summary>
        Task<Dictionary<int, Models.Entities.Product>> GetProductsAsync(IEnumerable<int> productIds);

        /// <summary>
        /// 获取分类产品列表缓存
        /// </summary>
        Task<List<Models.Entities.Product>> GetProductsByCategoryAsync(int categoryId, int pageIndex, int pageSize);

        /// <summary>
        /// 获取用户产品列表缓存
        /// </summary>
        Task<List<Models.Entities.Product>> GetProductsByUserAsync(int userId, int pageIndex, int pageSize);

        /// <summary>
        /// 移除产品所有相关缓存
        /// </summary>
        Task RemoveAllProductDataAsync(int productId);

        /// <summary>
        /// 刷新分类产品列表缓存
        /// </summary>
        Task RefreshCategoryProductsAsync(int categoryId);

        /// <summary>
        /// 获取缓存命中率统计
        /// </summary>
        Task<double> GetHitRate();


        /// <summary>
        /// 失效指定产品的所有缓存数据
        /// </summary>
        Task InvalidateProductCacheAsync(int productId);

        /// <summary>
        /// 失效指定分类下的所有产品缓存
        /// </summary>
        Task InvalidateProductsByCategoryAsync(int categoryId);

        /// <summary>
        /// 获得所有在售目录ID
        /// </summary>
        Task<List<int>> GetActiveCategoryIdsAsync();

        /// <summary>
        /// 更新所有在售缓存
        /// </summary>
        Task RefreshAllActiveProductsAsync();
    }
}
