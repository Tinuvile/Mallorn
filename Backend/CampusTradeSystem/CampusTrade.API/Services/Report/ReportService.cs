using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.Report
{
    /// <summary>
    /// 举报服务类 - 处理举报相关业务逻辑
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly IReportsRepository _reportsRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            IReportsRepository reportsRepository,
            IUnitOfWork unitOfWork,
            ILogger<ReportService> logger)
        {
            _reportsRepository = reportsRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// 创建举报
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <param name="reporterId">举报人ID</param>
        /// <param name="type">举报类型</param>
        /// <param name="description">举报描述</param>
        /// <param name="evidenceFiles">证据文件URL列表</param>
        /// <returns>创建结果</returns>
        public async Task<(bool Success, string Message, int? ReportId)> CreateReportAsync(
            int orderId, 
            int reporterId, 
            string type, 
            string? description = null,
            List<EvidenceFileInfo>? evidenceFiles = null)
        {
            try
            {
                // 验证举报类型
                var validTypes = new[] { "商品问题", "服务问题", "欺诈", "虚假描述", "其他" };
                if (!validTypes.Contains(type))
                {
                    return (false, "无效的举报类型", null);
                }

                // 检查用户是否已经对该订单进行过举报
                var existingReports = await _reportsRepository.GetByOrderIdAsync(orderId);
                if (existingReports.Any(r => r.ReporterId == reporterId && r.Status != "已关闭"))
                {
                    return (false, "您已经对此订单提交过举报，请等待处理结果", null);
                }

                // 创建举报记录
                var report = new Reports
                {
                    OrderId = orderId,
                    ReporterId = reporterId,
                    Type = type,
                    Description = description,
                    Status = "待处理",
                    Priority = CalculatePriority(type),
                    CreateTime = DateTime.Now
                };

                await _reportsRepository.AddAsync(report);
                await _unitOfWork.SaveChangesAsync();

                // 添加证据文件
                if (evidenceFiles != null && evidenceFiles.Any())
                {
                    foreach (var evidence in evidenceFiles)
                    {
                        await _reportsRepository.AddReportEvidenceAsync(
                            report.ReportId, 
                            evidence.FileType, 
                            evidence.FileUrl);
                    }
                    await _unitOfWork.SaveChangesAsync();
                }

                _logger.LogInformation("用户 {ReporterId} 对订单 {OrderId} 创建举报成功，举报ID: {ReportId}", 
                    reporterId, orderId, report.ReportId);

                return (true, "举报提交成功，我们将尽快处理", report.ReportId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建举报时发生异常");
                return (false, "系统异常，请稍后重试", null);
            }
        }

        /// <summary>
        /// 添加举报证据
        /// </summary>
        /// <param name="reportId">举报ID</param>
        /// <param name="evidenceFiles">证据文件信息</param>
        /// <returns>添加结果</returns>
        public async Task<(bool Success, string Message)> AddReportEvidenceAsync(
            int reportId, 
            List<EvidenceFileInfo> evidenceFiles)
        {
            try
            {
                // 验证举报是否存在且可以添加证据
                var report = await _reportsRepository.GetByPrimaryKeyAsync(reportId);
                if (report == null)
                {
                    return (false, "举报不存在");
                }

                if (report.Status == "已处理" || report.Status == "已关闭")
                {
                    return (false, "该举报已处理，无法添加证据");
                }

                // 添加证据文件
                foreach (var evidence in evidenceFiles)
                {
                    await _reportsRepository.AddReportEvidenceAsync(
                        reportId, 
                        evidence.FileType, 
                        evidence.FileUrl);
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("为举报 {ReportId} 添加了 {Count} 个证据文件", 
                    reportId, evidenceFiles.Count);

                return (true, "证据添加成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加举报证据时发生异常");
                return (false, "系统异常，请稍后重试");
            }
        }

        /// <summary>
        /// 获取用户的举报列表
        /// </summary>
        /// <param name="reporterId">举报人ID</param>
        /// <param name="pageIndex">页索引</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>举报列表</returns>
        public async Task<(IEnumerable<Reports> Reports, int TotalCount)> GetUserReportsAsync(
            int reporterId, 
            int pageIndex = 0, 
            int pageSize = 10)
        {
            try
            {
                var allReports = await _reportsRepository.GetByReporterIdAsync(reporterId);
                var totalCount = allReports.Count();
                
                var pagedReports = allReports
                    .Skip(pageIndex * pageSize)
                    .Take(pageSize)
                    .ToList();

                return (pagedReports, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户举报列表时发生异常");
                return (Enumerable.Empty<Reports>(), 0);
            }
        }

        /// <summary>
        /// 获取举报详情
        /// </summary>
        /// <param name="reportId">举报ID</param>
        /// <param name="requestUserId">请求用户ID（用于权限验证）</param>
        /// <returns>举报详情</returns>
        public async Task<Reports?> GetReportDetailsAsync(int reportId, int requestUserId)
        {
            try
            {
                var report = await _reportsRepository.GetReportWithDetailsAsync(reportId);
                
                // 权限验证：只有举报人可以查看详情
                if (report != null && report.ReporterId != requestUserId)
                {
                    _logger.LogWarning("用户 {UserId} 尝试访问不属于自己的举报 {ReportId}", 
                        requestUserId, reportId);
                    return null;
                }

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取举报详情时发生异常");
                return null;
            }
        }

        /// <summary>
        /// 获取举报的证据列表
        /// </summary>
        /// <param name="reportId">举报ID</param>
        /// <param name="requestUserId">请求用户ID（用于权限验证）</param>
        /// <returns>证据列表</returns>
        public async Task<IEnumerable<ReportEvidence>?> GetReportEvidencesAsync(int reportId, int requestUserId)
        {
            try
            {
                // 先验证权限
                var report = await _reportsRepository.GetByPrimaryKeyAsync(reportId);
                if (report == null || report.ReporterId != requestUserId)
                {
                    return null;
                }

                return await _reportsRepository.GetReportEvidencesAsync(reportId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取举报证据时发生异常");
                return null;
            }
        }

        /// <summary>
        /// 撤销举报（仅限待处理状态）
        /// </summary>
        /// <param name="reportId">举报ID</param>
        /// <param name="reporterId">举报人ID</param>
        /// <returns>撤销结果</returns>
        public async Task<(bool Success, string Message)> CancelReportAsync(int reportId, int reporterId)
        {
            try
            {
                var report = await _reportsRepository.GetByPrimaryKeyAsync(reportId);
                if (report == null)
                {
                    return (false, "举报不存在");
                }

                if (report.ReporterId != reporterId)
                {
                    return (false, "无权限操作此举报");
                }

                if (report.Status != "待处理")
                {
                    return (false, "只能撤销待处理状态的举报");
                }

                var success = await _reportsRepository.UpdateReportStatusAsync(reportId, "已关闭");
                if (success)
                {
                    await _unitOfWork.SaveChangesAsync();
                    return (true, "举报已撤销");
                }

                return (false, "撤销失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤销举报时发生异常");
                return (false, "系统异常，请稍后重试");
            }
        }

        /// <summary>
        /// 计算举报优先级
        /// </summary>
        /// <param name="type">举报类型</param>
        /// <returns>优先级（1-10）</returns>
        private int CalculatePriority(string type)
        {
            return type switch
            {
                "欺诈" => 9,
                "虚假描述" => 7,
                "商品问题" => 5,
                "服务问题" => 4,
                "其他" => 3,
                _ => 3
            };
        }
    }

    /// <summary>
    /// 证据文件信息
    /// </summary>
    public class EvidenceFileInfo
    {
        public string FileType { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
    }
}
