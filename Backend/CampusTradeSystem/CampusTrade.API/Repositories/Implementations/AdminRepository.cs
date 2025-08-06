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
    /// 管理员仓储实现类（AdminRepository Implementation）
    /// </summary>
    public class AdminRepository : Repository<Admin>, IAdminRepository
    {
        public AdminRepository(CampusTradeDbContext context) : base(context) { }

        #region 创建操作
        // 暂无特定创建操作，使用基础仓储接口方法
        #endregion

        #region 读取操作
        /// <summary>
        /// 根据用户ID获取管理员
        /// </summary>
        public async Task<Admin?> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(a => a.User)
                .Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.UserId == userId);
        }

        /// <summary>
        /// 根据角色获取管理员列表
        /// </summary>
        public async Task<IEnumerable<Admin>> GetByRoleAsync(string role)
        {
            return await _dbSet
                .Where(a => a.Role == role)
                .Include(a => a.User)
                .Include(a => a.Category)
                .ToListAsync();
        }

        /// <summary>
        /// 获取所有分类管理员
        /// </summary>
        public async Task<IEnumerable<Admin>> GetCategoryAdminsAsync()
        {
            return await _dbSet
                .Where(a => a.Role == Admin.Roles.CategoryAdmin)
                .Include(a => a.User)
                .Include(a => a.Category)
                .ToListAsync();
        }

        /// <summary>
        /// 根据分类ID获取分类管理员
        /// </summary>
        public async Task<Admin?> GetCategoryAdminByCategoryIdAsync(int categoryId)
        {
            return await _dbSet
                .Include(a => a.User)
                .Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.AssignedCategory == categoryId && a.Role == Admin.Roles.CategoryAdmin);
        }

        /// <summary>
        /// 获取所有活跃管理员（最近30天有活动）
        /// </summary>
        public async Task<IEnumerable<Admin>> GetActiveAdminsAsync()
        {
            var recentDate = DateTime.Now.AddDays(-30);
            return await _dbSet
                .Include(a => a.User)
                .Include(a => a.Category)
                .Include(a => a.AuditLogs)
                .Where(a => a.AuditLogs.Any(al => al.LogTime >= recentDate))
                .OrderByDescending(a => a.AuditLogs.Max(al => al.LogTime))
                .ToListAsync();
        }

        /// <summary>
        /// 获取管理员审计日志
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetAuditLogsByAdminAsync(int adminId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Set<AuditLog>()
                .Where(al => al.AdminId == adminId)
                .AsQueryable();
            if (startDate.HasValue)
                query = query.Where(al => al.LogTime >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(al => al.LogTime <= endDate.Value);
            return await query
                .Include(al => al.Admin)
                    .ThenInclude(a => a.User)
                .OrderByDescending(al => al.LogTime)
                .ToListAsync();
        }

        /// <summary>
        /// 获取管理员统计信息
        /// </summary>
        public async Task<Dictionary<string, int>> GetAdminStatisticsAsync()
        {
            var stats = new Dictionary<string, int>();
            var roleStats = await _dbSet
                .GroupBy(a => a.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Role, x => x.Count);
            foreach (var stat in roleStats)
                stats[stat.Key] = stat.Value;
            var categoryAdminCount = await _dbSet
                .CountAsync(a => a.Role == Admin.Roles.CategoryAdmin && a.AssignedCategory.HasValue);
            stats["已分配分类的管理员"] = categoryAdminCount;
            var recentActiveCount = await _dbSet
                .Include(a => a.AuditLogs)
                .Where(a => a.AuditLogs.Any(al => al.LogTime >= DateTime.Now.AddDays(-30)))
                .CountAsync();
            stats["最近活跃管理员"] = recentActiveCount;
            return stats;
        }
        #endregion

        #region 更新操作
        /// <summary>
        /// 创建审计日志
        /// </summary>
        public async Task CreateAuditLogAsync(int adminId, string actionType, int? targetId = null, string? detail = null)
        {
            var auditLog = new AuditLog
            {
                AdminId = adminId,
                ActionType = actionType,
                TargetId = targetId,
                LogDetail = detail,
            };
            await _context.Set<AuditLog>().AddAsync(auditLog);
        }
        #endregion

        #region 删除操作
        // 暂无特定删除操作，使用基础仓储接口方法
        #endregion

        #region 关系查询
        // 暂无特定关系查询，使用基础仓储接口方法
        #endregion

        #region 高级查询
        // 暂无特定高级查询，使用基础仓储接口方法
        #endregion
    }
}
