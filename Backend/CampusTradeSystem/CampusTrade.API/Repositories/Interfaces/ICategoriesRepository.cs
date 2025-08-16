using System.Collections.Generic;
using System.Threading.Tasks;
using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 分类管理仓储接口（CategoriesRepository Interface）
    /// </summary>
    public interface ICategoriesRepository : IRepository<Category>
    {
        #region 读取操作
        /// <summary>
        /// 获取所有根分类
        /// </summary>
        /// <returns>根分类集合</returns>
        Task<IEnumerable<Category>> GetRootCategoriesAsync();
        /// <summary>
        /// 获取指定父分类的所有子分类
        /// </summary>
        /// <param name="parentId">父分类ID</param>
        /// <returns>子分类集合</returns>
        Task<IEnumerable<Category>> GetSubCategoriesAsync(int parentId);
        /// <summary>
        /// 获取完整分类树
        /// </summary>
        /// <returns>分类树集合</returns>
        Task<IEnumerable<Category>> GetCategoryTreeAsync();
        /// <summary>
        /// 获取带有子分类的分类信息
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>分类实体或null</returns>
        Task<Category?> GetCategoryWithChildrenAsync(int categoryId);
        /// <summary>
        /// 获取分类路径
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>分类路径集合</returns>
        Task<IEnumerable<Category>> GetCategoryPathAsync(int categoryId);
        /// <summary>
        /// 获取分类全名
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>分类全名</returns>
        Task<string> GetCategoryFullNameAsync(int categoryId);
        /// <summary>
        /// 获取分类下商品数量
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>商品数量</returns>
        Task<int> GetProductCountByCategoryAsync(int categoryId);
        /// <summary>
        /// 获取分类下活跃商品数量
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>活跃商品数量</returns>
        Task<int> GetActiveProductCountByCategoryAsync(int categoryId);
        /// <summary>
        /// 获取所有分类的商品数量统计
        /// </summary>
        /// <returns>分类ID-商品数量字典</returns>
        Task<Dictionary<int, int>> GetCategoryProductCountsAsync();
        /// <summary>
        /// 获取包含商品的分类集合
        /// </summary>
        /// <returns>分类集合</returns>
        Task<IEnumerable<Category>> GetCategoriesWithProductsAsync();
        /// <summary>
        /// 搜索分类
        /// </summary>
        /// <param name="keyword">关键词</param>
        /// <returns>分类集合</returns>
        Task<IEnumerable<Category>> SearchCategoriesAsync(string keyword);
        /// <summary>
        /// 根据名称获取分类
        /// </summary>
        /// <param name="name">分类名称</param>
        /// <param name="parentId">父分类ID</param>
        /// <returns>分类实体或null</returns>
        Task<Category?> GetCategoryByNameAsync(string name, int? parentId = null);
        #endregion

        #region 更新操作
        /// <summary>
        /// 移动分类到新父分类
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <param name="newParentId">新父分类ID</param>
        /// <returns>是否成功</returns>
        Task<bool> MoveCategoryAsync(int categoryId, int? newParentId);
        #endregion

        #region 删除操作
        /// <summary>
        /// 判断分类是否可删除
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>是否可删除</returns>
        Task<bool> CanDeleteCategoryAsync(int categoryId);
        #endregion

        #region 关系查询
        // 暂无特定关系查询，使用基础仓储接口方法
        #endregion

        #region 高级查询
        // 暂无特定高级查询，使用基础仓储接口方法
        #endregion
    }
}
