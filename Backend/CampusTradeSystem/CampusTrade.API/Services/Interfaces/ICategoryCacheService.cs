using System.Collections.Generic;
using System.Threading.Tasks;
using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Services.Interfaces
{
    /// <summary>
    /// 分类tree缓存服务接口
    /// </summary>
    public interface ICategoryCacheService
    {
        /// <summary>
        /// 获取完整的分类树（唯一缓存入口）
        /// </summary>
        Task<List<Category>> GetCategoryTreeAsync();

        /// <summary>
        /// 强制刷新分类树缓存
        /// </summary>
        Task RefreshCategoryTreeAsync();

        /// <summary>
        /// 使分类树缓存失效(仅清除缓存，不重建)
        /// </summary>    
        Task InvalidateCategoryTreeCacheAsync();
    }
}
