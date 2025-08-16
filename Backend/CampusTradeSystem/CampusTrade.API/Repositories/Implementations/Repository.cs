using System.Linq.Expressions;
using CampusTrade.API.Data;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly CampusTradeDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(CampusTradeDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        #region 基础查询方法

        public virtual async Task<T?> GetByPrimaryKeyAsync(int key)
        {
            return await _dbSet.FindAsync(key);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            var count = await _dbSet.CountAsync(predicate);
            return count > 0;
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            if (predicate == null)
                return await _dbSet.CountAsync();
            return await _dbSet.CountAsync(predicate);
        }

        #endregion

        #region 分页查询

        public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            string includeProperties = "")
        {
            IQueryable<T> query = _dbSet;

            // 应用过滤条件
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // 获取总数
            int totalCount = await query.CountAsync();

            // 包含导航属性
            foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            // 应用排序
            if (orderBy != null)
            {
                query = orderBy(query);
            }

            // 分页
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return (items, totalCount);
        }

        #endregion

        #region 创建和更新

        public virtual async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            return entity;
        }

        public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
            return entities;
        }

        public virtual void Update(T entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }


        public virtual void UpdateRange(IEnumerable<T> entities)
        {
            _dbSet.UpdateRange(entities);
        }

        #endregion

        #region 删除

        public virtual void Delete(T entity)
        {
            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            _dbSet.Remove(entity);
        }

        public virtual void DeleteRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        public virtual async Task DeleteByPrimaryKeyAsync(int key)
        {
            var entity = await GetByPrimaryKeyAsync(key);
            if (entity != null)
            {
                Delete(entity);
            }
        }

        #endregion

        #region 其他操作

        public virtual async Task<IEnumerable<T>> GetWithIncludeAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            params Expression<Func<T, object>>[] includeProperties)
        {
            IQueryable<T> query = _dbSet;

            // 应用过滤条件
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // 包含导航属性
            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }

            // 应用排序
            if (orderBy != null)
            {
                return await orderBy(query).ToListAsync();
            }

            return await query.ToListAsync();
        }

        public virtual async Task<int> BulkUpdateAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, T>> updateExpression)
        {
            var entities = await _dbSet.Where(predicate).ToListAsync();
            var compiled = updateExpression.Compile();

            foreach (var entity in entities)
            {
                var updatedEntity = compiled(entity);
                _context.Entry(entity).CurrentValues.SetValues(updatedEntity);
            }

            return entities.Count;
        }

        public virtual async Task<int> BulkDeleteAsync(Expression<Func<T, bool>> predicate)
        {
            var entities = await _dbSet.Where(predicate).ToListAsync();
            _dbSet.RemoveRange(entities);
            return entities.Count;
        }

        public virtual async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        #endregion

        #region 辅助方法

        protected virtual IQueryable<T> ApplyIncludes(IQueryable<T> query, string includeProperties)
        {
            foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }
            return query;
        }

        protected virtual IQueryable<T> ApplyIncludes(IQueryable<T> query, params Expression<Func<T, object>>[] includeProperties)
        {
            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }
            return query;
        }

        #endregion
    }
}
