using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// 分类管理仓储实现类（CategoriesRepository Implementation）
    /// </summary>
    public class CategoriesRepository : Repository<Category>, ICategoriesRepository
    {
        public CategoriesRepository(CampusTradeDbContext context) : base(context) { }

        #region 创建操作
        // 暂无特定创建操作，使用基础仓储接口方法
        #endregion

        #region 读取操作
        /// <summary>
        /// 获取所有根分类
        /// </summary>
        public async Task<IEnumerable<Category>> GetRootCategoriesAsync()
        {
            return await _context.Categories
                .Where(c => c.ParentId == null)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 获取指定父分类的所有子分类
        /// </summary>
        public async Task<IEnumerable<Category>> GetSubCategoriesAsync(int parentId)
        {
            return await _context.Categories
                .Where(c => c.ParentId == parentId)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 获取完整分类树
        /// </summary>
        public async Task<IEnumerable<Category>> GetCategoryTreeAsync()
        {
            return await _context.Categories
                .Include(c => c.Children)
                .Where(c => c.ParentId == null)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 获取带有子分类的分类信息
        /// </summary>
        public async Task<Category?> GetCategoryWithChildrenAsync(int categoryId)
        {
            return await _context.Categories
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        }

        /// <summary>
        /// 获取分类路径
        /// </summary>
        public async Task<IEnumerable<Category>> GetCategoryPathAsync(int categoryId)
        {
            var categories = new List<Category>();
            var currentCategory = await GetByPrimaryKeyAsync(categoryId);
            while (currentCategory != null)
            {
                categories.Insert(0, currentCategory);
                if (currentCategory.ParentId.HasValue)
                {
                    currentCategory = await GetByPrimaryKeyAsync(currentCategory.ParentId.Value);
                }
                else
                {
                    break;
                }
            }
            return categories;
        }

        /// <summary>
        /// 获取分类全名
        /// </summary>
        public async Task<string> GetCategoryFullNameAsync(int categoryId)
        {
            var path = await GetCategoryPathAsync(categoryId);
            return string.Join(" > ", path.Select(c => c.Name));
        }

        /// <summary>
        /// 获取分类下商品数量
        /// </summary>
        public async Task<int> GetProductCountByCategoryAsync(int categoryId)
        {
            return await _context.Products
                .CountAsync(p => p.CategoryId == categoryId);
        }

        /// <summary>
        /// 获取分类下活跃商品数量
        /// </summary>
        public async Task<int> GetActiveProductCountByCategoryAsync(int categoryId)
        {
            return await _context.Products
                .CountAsync(p => p.CategoryId == categoryId && p.Status == "在售");
        }

        /// <summary>
        /// 获取所有分类的商品数量统计
        /// </summary>
        public async Task<Dictionary<int, int>> GetCategoryProductCountsAsync()
        {
            return await _context.Categories
                .Select(c => new { c.CategoryId, Count = c.Products.Count })
                .ToDictionaryAsync(x => x.CategoryId, x => x.Count);
        }

        /// <summary>
        /// 获取包含商品的分类集合
        /// </summary>
        public async Task<IEnumerable<Category>> GetCategoriesWithProductsAsync()
        {
            return await _context.Categories
                .Where(c => c.Products.Any())
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 搜索分类
        /// </summary>
        public async Task<IEnumerable<Category>> SearchCategoriesAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await GetAllAsync();
            return await _context.Categories
                .Where(c => c.Name.Contains(keyword))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据名称获取分类
        /// </summary>
        public async Task<Category?> GetCategoryByNameAsync(string name, int? parentId = null)
        {
            var query = _context.Categories.Where(c => c.Name == name);
            if (parentId.HasValue)
            {
                query = query.Where(c => c.ParentId == parentId);
            }
            else
            {
                query = query.Where(c => c.ParentId == null);
            }
            return await query.FirstOrDefaultAsync();
        }
        #endregion

        #region 更新操作
        /// <summary>
        /// 移动分类到新父分类
        /// </summary>
        public async Task<bool> MoveCategoryAsync(int categoryId, int? newParentId)
        {
            var category = await GetByPrimaryKeyAsync(categoryId);
            if (category == null) return false;
            category.ParentId = newParentId;
            Update(category);
            return true;
        }
        #endregion

        #region 删除操作
        /// <summary>
        /// 判断分类是否可删除
        /// </summary>
        public async Task<bool> CanDeleteCategoryAsync(int categoryId)
        {
            var childrenCount = await _context.Categories.CountAsync(c => c.ParentId == categoryId);
            if (childrenCount > 0) return false;
            var productsCount = await _context.Products.CountAsync(p => p.CategoryId == categoryId);
            return productsCount == 0;
        }
        #endregion

        #region 关系查询
        // 暂无特定关系查询，使用基础仓储接口方法
        #endregion

        #region 高级查询
        // 暂无特定高级查询，使用基础仓储接口方法
        #endregion
    }
}
