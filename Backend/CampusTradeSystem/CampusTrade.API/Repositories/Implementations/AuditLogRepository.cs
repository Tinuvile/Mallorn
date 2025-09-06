using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// 审计日志仓储实现
    /// </summary>
    public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
    {
        public AuditLogRepository(CampusTradeDbContext context) : base(context) { }

        /// <summary>
        /// 根据管理员ID获取审计日志
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetByAdminIdAsync(int adminId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _dbSet
                .Where(log => log.AdminId == adminId)
                .Include(log => log.Admin)
                    .ThenInclude(admin => admin.User)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(log => log.LogTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(log => log.LogTime <= endDate.Value);

            return await query
                .OrderByDescending(log => log.LogTime)
                .ToListAsync();
        }

        /// <summary>
        /// 根据操作类型获取审计日志
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetByActionTypeAsync(string actionType, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _dbSet
                .Where(log => log.ActionType == actionType)
                .Include(log => log.Admin)
                    .ThenInclude(admin => admin.User)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(log => log.LogTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(log => log.LogTime <= endDate.Value);

            return await query
                .OrderByDescending(log => log.LogTime)
                .ToListAsync();
        }

        /// <summary>
        /// 分页获取审计日志
        /// </summary>
        public async Task<(IEnumerable<AuditLog> Logs, int TotalCount)> GetPagedLogsAsync(
            int pageIndex,
            int pageSize,
            int? adminId = null,
            string? actionType = null,
            int? categoryId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _dbSet
                .Include(log => log.Admin)
                    .ThenInclude(admin => admin.User)
                .AsQueryable();

            // 筛选条件
            if (adminId.HasValue)
                query = query.Where(log => log.AdminId == adminId.Value);

            if (!string.IsNullOrEmpty(actionType))
                query = query.Where(log => log.ActionType == actionType);

            // 基于分类筛选：显示超级管理员和对应分类的模块管理员的日志
            if (categoryId.HasValue)
            {
                query = query.Where(log =>
                    log.Admin.Role == Admin.Roles.Super || // 超级管理员
                    (log.Admin.Role == Admin.Roles.CategoryAdmin && log.Admin.AssignedCategory == categoryId.Value) // 对应分类的模块管理员
                );
            }

            if (startDate.HasValue)
                query = query.Where(log => log.LogTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(log => log.LogTime <= endDate.Value);

            var totalCount = await query.CountAsync();

            var logs = await query
                .OrderByDescending(log => log.LogTime)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, totalCount);
        }

        /// <summary>
        /// 获取审计日志统计信息
        /// </summary>
        public async Task<Dictionary<string, int>> GetAuditStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _dbSet.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(log => log.LogTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(log => log.LogTime <= endDate.Value);

            var stats = new Dictionary<string, int>();

            // 按操作类型统计
            var actionStats = await query
                .GroupBy(log => log.ActionType)
                .Select(g => new { ActionType = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ActionType, x => x.Count);

            foreach (var stat in actionStats)
                stats[$"操作_{stat.Key}"] = stat.Value;

            // 按管理员统计
            var adminStats = await query
                .Include(log => log.Admin)
                .GroupBy(log => log.Admin.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Role, x => x.Count);

            foreach (var stat in adminStats)
                stats[$"角色_{stat.Key}"] = stat.Value;

            // 总数统计
            stats["总操作数"] = await query.CountAsync();

            return stats;
        }

        /// <summary>
        /// 记录管理员操作日志
        /// </summary>
        public async Task<int> LogAdminActionAsync(int adminId, string actionType, int? targetId = null, string? detail = null)
        {
            var auditLog = new AuditLog
            {
                AdminId = adminId,
                ActionType = actionType,
                TargetId = targetId,
                LogDetail = detail,
                LogTime = DateTime.Now
            };

            await _dbSet.AddAsync(auditLog);
            await _context.SaveChangesAsync();

            return auditLog.LogId;
        }

        /// <summary>
        /// 获取指定日期范围内的操作数量
        /// </summary>
        public async Task<int> GetOperationCountByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet.CountAsync(log => log.LogTime >= startDate && log.LogTime < endDate);
        }
    }
}
