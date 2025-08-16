using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// Reports实体的Repository接口
    /// 继承基础IRepository，提供Reports特有的查询和操作方法
    /// </summary>
    public interface IReportsRepository : IRepository<Reports>
    {
        #region 创建操作
        // 举报创建由基础仓储 AddAsync 提供
        #endregion

        #region 读取操作
        /// <summary>
        /// 根据举报人ID获取举报集合
        /// </summary>
        Task<IEnumerable<Reports>> GetByReporterIdAsync(int reporterId);
        /// <summary>
        /// 根据订单ID获取举报集合
        /// </summary>
        Task<IEnumerable<Reports>> GetByOrderIdAsync(int orderId);
        /// <summary>
        /// 根据状态获取举报集合
        /// </summary>
        Task<IEnumerable<Reports>> GetByStatusAsync(string status);
        /// <summary>
        /// 获取待处理举报集合
        /// </summary>
        Task<IEnumerable<Reports>> GetPendingReportsAsync();
        /// <summary>
        /// 获取超时未处理举报集合
        /// </summary>
        Task<IEnumerable<Reports>> GetOverdueReportsAsync();
        /// <summary>
        /// 分页获取举报
        /// </summary>
        Task<(IEnumerable<Reports> Reports, int TotalCount)> GetPagedReportsAsync(int pageIndex, int pageSize, string? status = null, string? type = null, int? priority = null, DateTime? startDate = null, DateTime? endDate = null);
        /// <summary>
        /// 获取高优先级举报集合
        /// </summary>
        Task<IEnumerable<Reports>> GetHighPriorityReportsAsync();
        /// <summary>
        /// 获取举报详情（包含所有关联信息）
        /// </summary>
        Task<Reports?> GetReportWithDetailsAsync(int reportId);
        /// <summary>
        /// 获取举报证据集合
        /// </summary>
        Task<IEnumerable<ReportEvidence>> GetReportEvidencesAsync(int reportId);
        #endregion

        #region 更新操作
        /// <summary>
        /// 更新举报状态
        /// </summary>
        Task<bool> UpdateReportStatusAsync(int reportId, string newStatus);
        /// <summary>
        /// 分配举报优先级
        /// </summary>
        Task<bool> AssignPriorityAsync(int reportId, int priority);
        /// <summary>
        /// 批量更新举报状态
        /// </summary>
        Task<int> BulkUpdateReportStatusAsync(List<int> reportIds, string newStatus);
        /// <summary>
        /// 添加举报证据
        /// </summary>
        Task AddReportEvidenceAsync(int reportId, string fileType, string fileUrl);

        #endregion

        #region 删除操作
        // 举报删除由基础仓储 Delete 提供
        #endregion

        #region 统计操作
        /// <summary>
        /// 获取举报统计信息
        /// </summary>
        Task<Dictionary<string, int>> GetReportStatisticsAsync();
        /// <summary>
        /// 获取指定类型举报数量
        /// </summary>
        Task<int> GetReportCountByTypeAsync(string type);
        /// <summary>
        /// 获取用户举报统计
        /// </summary>
        Task<Dictionary<int, int>> GetUserReportStatisticsAsync();
        #endregion
    }
}
