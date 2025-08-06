using System.Linq.Expressions;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 基础Repository接口
    /// 定义通用的CRUD操作和查询方法
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public interface IRepository<T> where T : class
    {
        #region 基础查询方法
        // 根据主键获取实体
        Task<T?> GetByPrimaryKeyAsync(int key);
        // 获取所有实体
        Task<IEnumerable<T>> GetAllAsync();
        // 根据条件查询实体集合
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        // 根据条件查询第一个实体
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        // 判断是否存在满足条件的实体
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        // 获取满足条件的实体数量
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
        #endregion

        #region 分页查询
        // 获取分页数据
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            string includeProperties = "");
        #endregion

        #region 创建和更新
        // 添加实体
        Task<T> AddAsync(T entity);
        // 批量添加实体
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
        // 更新实体
        void Update(T entity);
        // 批量更新实体
        void UpdateRange(IEnumerable<T> entities);
        #endregion

        #region 删除
        // 删除实体
        void Delete(T entity);
        // 批量删除实体
        void DeleteRange(IEnumerable<T> entities);
        // 根据主键删除实体
        Task DeleteByPrimaryKeyAsync(int Key);
        #endregion

        #region 其他操作
        // 高级查询
        Task<IEnumerable<T>> GetWithIncludeAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            params Expression<Func<T, object>>[] includeProperties);

        // 批量操作
        Task<int> BulkUpdateAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, T>> updateExpression);
        Task<int> BulkDeleteAsync(Expression<Func<T, bool>> predicate);

        // 保存更改
        Task<int> SaveChangesAsync();
        #endregion
    }
}
