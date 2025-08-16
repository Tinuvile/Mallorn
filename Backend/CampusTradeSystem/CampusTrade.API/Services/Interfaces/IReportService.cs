using CampusTrade.API.Models.Entities;
using CampusTrade.API.Services.Report;

namespace CampusTrade.API.Services.Interfaces
{
    /// <summary>
    /// 举报服务接口
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// 创建举报
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <param name="reporterId">举报人ID</param>
        /// <param name="type">举报类型</param>
        /// <param name="description">举报描述</param>
        /// <param name="evidenceFiles">证据文件URL列表</param>
        /// <returns>创建结果</returns>
        Task<(bool Success, string Message, int? ReportId)> CreateReportAsync(
            int orderId,
            int reporterId,
            string type,
            string? description = null,
            List<EvidenceFileInfo>? evidenceFiles = null);

        /// <summary>
        /// 添加举报证据
        /// </summary>
        /// <param name="reportId">举报ID</param>
        /// <param name="evidenceFiles">证据文件信息</param>
        /// <returns>添加结果</returns>
        Task<(bool Success, string Message)> AddReportEvidenceAsync(
            int reportId,
            List<EvidenceFileInfo> evidenceFiles);

        /// <summary>
        /// 获取用户的举报列表
        /// </summary>
        /// <param name="reporterId">举报人ID</param>
        /// <param name="pageIndex">页索引</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>举报列表</returns>
        Task<(IEnumerable<Reports> Reports, int TotalCount)> GetUserReportsAsync(
            int reporterId,
            int pageIndex = 0,
            int pageSize = 10);

        /// <summary>
        /// 获取举报详情
        /// </summary>
        /// <param name="reportId">举报ID</param>
        /// <param name="requestUserId">请求用户ID（用于权限验证）</param>
        /// <returns>举报详情</returns>
        Task<Reports?> GetReportDetailsAsync(int reportId, int requestUserId);

        /// <summary>
        /// 获取举报的证据列表
        /// </summary>
        /// <param name="reportId">举报ID</param>
        /// <param name="requestUserId">请求用户ID（用于权限验证）</param>
        /// <returns>证据列表</returns>
        Task<IEnumerable<ReportEvidence>?> GetReportEvidencesAsync(int reportId, int requestUserId);

        /// <summary>
        /// 撤销举报（仅限待处理状态）
        /// </summary>
        /// <param name="reportId">举报ID</param>
        /// <param name="reporterId">举报人ID</param>
        /// <returns>撤销结果</returns>
        Task<(bool Success, string Message)> CancelReportAsync(int reportId, int reporterId);
    }
}
