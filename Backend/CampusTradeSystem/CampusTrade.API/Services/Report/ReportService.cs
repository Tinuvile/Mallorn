using CampusTrade.API.Infrastructure.Utils.Performance;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Serilog;

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
        private readonly Serilog.ILogger _serilogLogger;

        public ReportService(
            IReportsRepository reportsRepository,
            IUnitOfWork unitOfWork,
            ILogger<ReportService> logger)
        {
            _reportsRepository = reportsRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _serilogLogger = Log.ForContext<ReportService>();
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
            using var performanceTracker = new PerformanceTracker(_serilogLogger, "CreateReport", "ReportService")
                .AddContext("OrderId", orderId)
                .AddContext("ReporterId", reporterId)
                .AddContext("ReportType", type);

            try
            {
                _serilogLogger.Information("开始创建举报 - 订单ID: {OrderId}, 举报人ID: {ReporterId}, 类型: {ReportType}",
                    orderId, reporterId, type);

                // 验证举报类型
                var validTypes = new[] { "商品问题", "服务问题", "欺诈", "虚假描述", "其他" };
                if (!validTypes.Contains(type))
                {
                    _serilogLogger.Warning("举报类型验证失败 - 无效类型: {ReportType}, 订单ID: {OrderId}, 用户ID: {ReporterId}",
                        type, orderId, reporterId);
                    return (false, "无效的举报类型", null);
                }

                // 检查用户是否已经对该订单进行过举报
                var existingReports = await _reportsRepository.GetByOrderIdAsync(orderId);
                var duplicateReport = existingReports.FirstOrDefault(r => r.ReporterId == reporterId && r.Status != "已关闭");

                if (duplicateReport != null)
                {
                    _serilogLogger.Warning("重复举报拦截 - 用户ID: {ReporterId}, 订单ID: {OrderId}, 现有举报ID: {ExistingReportId}, 状态: {Status}",
                        reporterId, orderId, duplicateReport.ReportId, duplicateReport.Status);
                    return (false, "您已经对此订单提交过举报，请等待处理结果", null);
                }

                // 创建举报记录
                var priority = CalculatePriority(type);
                var report = new Reports
                {
                    OrderId = orderId,
                    ReporterId = reporterId,
                    Type = type,
                    Description = description,
                    Status = "待处理",
                    Priority = priority,
                    CreateTime = DateTime.Now
                };

                await _reportsRepository.AddAsync(report);
                await _unitOfWork.SaveChangesAsync();

                _serilogLogger.Information("举报记录创建成功 - 举报ID: {ReportId}, 优先级: {Priority}",
                    report.ReportId, priority);

                // 添加证据文件
                var evidenceCount = 0;
                if (evidenceFiles != null && evidenceFiles.Any())
                {
                    foreach (var evidence in evidenceFiles)
                    {
                        await _reportsRepository.AddReportEvidenceAsync(
                            report.ReportId,
                            evidence.FileType,
                            evidence.FileUrl);
                        evidenceCount++;
                    }
                    await _unitOfWork.SaveChangesAsync();

                    _serilogLogger.Information("举报证据添加完成 - 举报ID: {ReportId}, 证据数量: {EvidenceCount}",
                        report.ReportId, evidenceCount);
                }

                // 根据优先级记录不同级别的日志
                if (priority >= 7)
                {
                    _serilogLogger.Warning("高优先级举报创建 - 举报ID: {ReportId}, 类型: {ReportType}, 优先级: {Priority}, 需要优先处理",
                        report.ReportId, type, priority);
                }

                _serilogLogger.Information("用户举报创建成功 - 用户ID: {ReporterId}, 订单ID: {OrderId}, 举报类型: {ReportType}, 举报ID: {ReportId}, 优先级: {Priority}, 证据数量: {EvidenceCount}",
                    reporterId, orderId, type, report.ReportId, report.Priority, evidenceFiles?.Count ?? 0);

                return (true, "举报提交成功，我们将尽快处理", report.ReportId);
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "创建举报失败 - 订单ID: {OrderId}, 用户ID: {ReporterId}, 错误: {ErrorMessage}",
                    orderId, reporterId, ex.Message);
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
            using var performanceTracker = new PerformanceTracker(_serilogLogger, "AddReportEvidence", "ReportService")
                .AddContext("ReportId", reportId)
                .AddContext("EvidenceCount", evidenceFiles.Count);

            try
            {
                _serilogLogger.Information("开始添加举报证据 - 举报ID: {ReportId}, 证据数量: {Count}",
                    reportId, evidenceFiles.Count);

                // 验证举报是否存在且可以添加证据
                var report = await _reportsRepository.GetByPrimaryKeyAsync(reportId);
                if (report == null)
                {
                    _serilogLogger.Warning("添加证据失败 - 举报不存在: {ReportId}", reportId);
                    return (false, "举报不存在");
                }

                if (report.Status == "已处理" || report.Status == "已关闭")
                {
                    _serilogLogger.Warning("添加证据失败 - 举报状态不允许: 举报ID: {ReportId}, 状态: {Status}",
                        reportId, report.Status);
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

                _serilogLogger.Information("举报证据添加成功 - 举报ID: {ReportId}, 添加证据数: {Count}, 举报类型: {ReportType}",
                    reportId, evidenceFiles.Count, report.Type);

                return (true, "证据添加成功");
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "添加举报证据异常 - 举报ID: {ReportId}, 错误: {ErrorMessage}",
                    reportId, ex.Message);
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
                _serilogLogger.Debug("查询举报详情 - 举报ID: {ReportId}, 请求用户: {UserId}",
                    reportId, requestUserId);

                var report = await _reportsRepository.GetReportWithDetailsAsync(reportId);

                // 权限验证：只有举报人可以查看详情 （修改：后续需加入管理员）
                if (report != null && report.ReporterId != requestUserId)
                {
                    _serilogLogger.Warning("举报详情访问权限拒绝 - 举报ID: {ReportId}, 举报人: {ReporterId}, 请求用户: {RequestUserId}",
                        reportId, report.ReporterId, requestUserId);
                    return null;
                }

                if (report != null)
                {
                    _serilogLogger.Information("举报详情查询成功 - 举报ID: {ReportId}, 类型: {ReportType}, 状态: {Status}",
                        reportId, report.Type, report.Status);
                }
                else
                {
                    _serilogLogger.Information("举报详情查询 - 未找到记录: {ReportId}", reportId);
                }

                return report;
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "获取举报详情异常 - 举报ID: {ReportId}, 用户ID: {UserId}",
                    reportId, requestUserId);
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
                // 先验证权限（修改：后续需加入管理员）
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
