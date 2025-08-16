using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Models.DTOs.Report;
using CampusTrade.API.Services.Interfaces;
using CampusTrade.API.Services.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusTrade.API.Controllers
{
    /// <summary>
    /// 举报控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 需要授权访问
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ILogger<ReportController> _logger;

        public ReportController(
            IReportService reportService,
            ILogger<ReportController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        /// <summary>
        /// 创建举报
        /// </summary>
        /// <param name="request">举报创建请求</param>
        /// <returns>创建结果</returns>
        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportDto request)
        {
            try
            {
                // 获取当前用户ID（从JWT Token中获取）
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int reporterId))
                {
                    return Unauthorized(ApiResponse.CreateError("用户身份验证失败"));
                }

                // 转换证据文件信息
                var evidenceFiles = request.EvidenceFiles?.Select(ef => new EvidenceFileInfo
                {
                    FileType = ef.FileType,
                    FileUrl = ef.FileUrl
                }).ToList();

                var result = await _reportService.CreateReportAsync(
                    request.OrderId,
                    reporterId,
                    request.Type,
                    request.Description,
                    evidenceFiles);

                if (result.Success)
                {
                    return Ok(ApiResponse.CreateSuccess(new { reportId = result.ReportId }, result.Message));
                }

                return BadRequest(ApiResponse.CreateError(result.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建举报时发生异常");
                return StatusCode(500, ApiResponse.CreateError("系统异常，请稍后重试"));
            }
        }

        /// <summary>
        /// 添加举报证据
        /// </summary>
        /// <param name="reportId">举报ID</param>
        /// <param name="request">证据添加请求</param>
        /// <returns>添加结果</returns>
        [HttpPost("{reportId}/evidence")]
        public async Task<IActionResult> AddReportEvidence(int reportId, [FromBody] CreateReportDto request)
        {
            try
            {
                if (request.EvidenceFiles == null || !request.EvidenceFiles.Any())
                {
                    return BadRequest(ApiResponse.CreateError("证据文件不能为空"));
                }

                // 转换证据文件信息
                var evidenceFiles = request.EvidenceFiles.Select(ef => new EvidenceFileInfo
                {
                    FileType = ef.FileType,
                    FileUrl = ef.FileUrl
                }).ToList();

                var result = await _reportService.AddReportEvidenceAsync(reportId, evidenceFiles);

                if (result.Success)
                {
                    return Ok(ApiResponse.CreateSuccess(result.Message));
                }

                return BadRequest(ApiResponse.CreateError(result.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加举报证据时发生异常");
                return StatusCode(500, ApiResponse.CreateError("系统异常，请稍后重试"));
            }
        }

        /// <summary>
        /// 获取当前用户的举报列表
        /// </summary>
        /// <param name="pageIndex">页索引</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>举报列表</returns>
        [HttpGet("my-reports")]
        public async Task<IActionResult> GetMyReports(
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // 获取当前用户ID
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int reporterId))
                {
                    return Unauthorized(new { success = false, message = "用户身份验证失败" });
                }

                var (reports, totalCount) = await _reportService.GetUserReportsAsync(reporterId, pageIndex, pageSize);

                // 转换为响应模型
                var reportList = reports.Select(r => new ReportListItemDto
                {
                    ReportId = r.ReportId,
                    OrderId = r.OrderId,
                    Type = r.Type,
                    Priority = r.Priority,
                    Status = r.Status,
                    Description = r.Description,
                    CreateTime = r.CreateTime,
                    EvidenceCount = r.Evidences?.Count ?? 0
                }).ToList();

                return Ok(ApiResponse.CreateSuccess(new
                {
                    reports = reportList,
                    pagination = new
                    {
                        pageIndex,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户举报列表时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = "系统异常，请稍后重试"
                });
            }
        }

        /// <summary>
        /// 获取举报详情
        /// </summary>
        /// <param name="reportId">举报ID</param>
        /// <returns>举报详情</returns>
        [HttpGet("{reportId}")]
        public async Task<IActionResult> GetReportDetails(int reportId)
        {
            try
            {
                // 获取当前用户ID
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int requestUserId))
                {
                    return Unauthorized(new { success = false, message = "用户身份验证失败" });
                }

                var report = await _reportService.GetReportDetailsAsync(reportId, requestUserId);
                if (report == null)
                {
                    return NotFound(ApiResponse.CreateError("举报不存在或无权限访问"));
                }

                var reportDetails = new ReportDetailDto
                {
                    ReportId = report.ReportId,
                    OrderId = report.OrderId,
                    Type = report.Type,
                    Priority = report.Priority,
                    Status = report.Status,
                    Description = report.Description,
                    CreateTime = report.CreateTime,
                    Reporter = report.Reporter != null ? new ReporterInfoDto
                    {
                        UserId = report.Reporter.UserId,
                        Username = report.Reporter.Username
                    } : null,
                    Evidences = report.Evidences?.Select(e => new EvidenceDto
                    {
                        EvidenceId = e.EvidenceId,
                        FileType = e.FileType,
                        FileUrl = e.FileUrl,
                        UploadedAt = e.UploadedAt
                    }).ToList() ?? new List<EvidenceDto>()
                };

                return Ok(ApiResponse.CreateSuccess(reportDetails));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取举报详情时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = "系统异常，请稍后重试"
                });
            }
        }

        /// <summary>
        /// 获取举报的证据列表
        /// </summary>
        /// <param name="reportId">举报ID</param>
        /// <returns>证据列表</returns>
        [HttpGet("{reportId}/evidence")]
        public async Task<IActionResult> GetReportEvidences(int reportId)
        {
            try
            {
                // 获取当前用户ID
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int requestUserId))
                {
                    return Unauthorized(new { success = false, message = "用户身份验证失败" });
                }

                var evidences = await _reportService.GetReportEvidencesAsync(reportId, requestUserId);
                if (evidences == null)
                {
                    return NotFound(ApiResponse.CreateError("举报不存在或无权限访问"));
                }

                var evidenceList = evidences.Select(e => new EvidenceDto
                {
                    EvidenceId = e.EvidenceId,
                    FileType = e.FileType,
                    FileUrl = e.FileUrl,
                    UploadedAt = e.UploadedAt
                }).ToList();

                return Ok(ApiResponse.CreateSuccess(evidenceList));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取举报证据时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = "系统异常，请稍后重试"
                });
            }
        }

        /// <summary>
        /// 撤销举报
        /// </summary>
        /// <param name="reportId">举报ID</param>
        /// <returns>撤销结果</returns>
        [HttpPost("{reportId}/cancel")]
        public async Task<IActionResult> CancelReport(int reportId)
        {
            try
            {
                // 获取当前用户ID
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int reporterId))
                {
                    return Unauthorized(new { success = false, message = "用户身份验证失败" });
                }

                var result = await _reportService.CancelReportAsync(reportId, reporterId);

                if (result.Success)
                {
                    return Ok(ApiResponse.CreateSuccess(result.Message));
                }

                return BadRequest(ApiResponse.CreateError(result.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤销举报时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = "系统异常，请稍后重试"
                });
            }
        }

        /// <summary>
        /// 获取举报类型列表
        /// </summary>
        /// <returns>举报类型列表</returns>
        [HttpGet("types")]
        public IActionResult GetReportTypes()
        {
            var types = new[]
            {
                new { value = "商品问题", label = "商品问题", priority = 5 },
                new { value = "服务问题", label = "服务问题", priority = 4 },
                new { value = "欺诈", label = "欺诈", priority = 9 },
                new { value = "虚假描述", label = "虚假描述", priority = 7 },
                new { value = "其他", label = "其他", priority = 3 }
            };

            return Ok(new
            {
                success = true,
                data = types
            });
        }
    }
}
