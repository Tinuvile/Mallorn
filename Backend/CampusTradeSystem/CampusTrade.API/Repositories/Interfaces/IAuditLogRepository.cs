using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 审计日志仓储接口
    /// </summary>
    public interface IAuditLogRepository : IRepository<AuditLog>
    {
        /// <summary>
        /// 根据管理员ID获取审计日志
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="startDate">起始时间</param>
        /// <param name="endDate">结束时间</param>
        /// <returns>审计日志列表</returns>
        Task<IEnumerable<AuditLog>> GetByAdminIdAsync(int adminId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// 根据操作类型获取审计日志
        /// </summary>
        /// <param name="actionType">操作类型</param>
        /// <param name="startDate">起始时间</param>
        /// <param name="endDate">结束时间</param>
        /// <returns>审计日志列表</returns>
        Task<IEnumerable<AuditLog>> GetByActionTypeAsync(string actionType, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// 分页获取审计日志
        /// </summary>
        /// <param name="pageIndex">页索引</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="adminId">管理员ID筛选</param>
        /// <param name="actionType">操作类型筛选</param>
        /// <param name="categoryId">商品分类ID筛选（筛选超级管理员和对应分类模块管理员的日志）</param>
        /// <param name="startDate">起始时间</param>
        /// <param name="endDate">结束时间</param>
        /// <returns>审计日志列表和总数</returns>
        Task<(IEnumerable<AuditLog> Logs, int TotalCount)> GetPagedLogsAsync(
            int pageIndex,
            int pageSize,
            int? adminId = null,
            string? actionType = null,
            int? categoryId = null,
            DateTime? startDate = null,
            DateTime? endDate = null);

        /// <summary>
        /// 获取审计日志统计信息
        /// </summary>
        /// <param name="startDate">起始时间</param>
        /// <param name="endDate">结束时间</param>
        /// <returns>统计信息</returns>
        Task<Dictionary<string, int>> GetAuditStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// 记录管理员操作日志
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="actionType">操作类型</param>
        /// <param name="targetId">目标ID</param>
        /// <param name="detail">详细描述</param>
        /// <returns>审计日志ID</returns>
        Task<int> LogAdminActionAsync(int adminId, string actionType, int? targetId = null, string? detail = null);
        
        /// <summary>
        /// 获取指定日期范围内的操作数量
        /// </summary>
        /// <param name="startDate">起始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>操作数量</returns>
        Task<int> GetOperationCountByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}
