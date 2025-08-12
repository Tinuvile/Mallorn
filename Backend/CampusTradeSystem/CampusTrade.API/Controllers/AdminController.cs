using CampusTrade.API.Models.DTOs.Admin;
using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusTrade.API.Infrastructure.Extensions;

namespace CampusTrade.API.Controllers
{
    /// <summary>
    /// 管理员控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 需要授权访问
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IAdminService adminService,
            ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        #region 管理员管理
        /// <summary>
        /// 创建管理员（仅系统管理员）
        /// </summary>
        /// <param name="createDto">创建管理员请求</param>
        /// <returns>创建结果</returns>
        [HttpPost]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminDto createDto)
        {
            try
            {
                // 获取当前用户ID
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized("用户身份验证失败");

                // 获取操作员的管理员信息
                var operatorAdmin = await _adminService.GetAdminByUserIdAsync(userId);
                if (operatorAdmin == null)
                {
                    return Forbid(ApiResponse.CreateError("只有管理员可以执行此操作").ToString());
                }

                var result = await _adminService.CreateAdminAsync(createDto, operatorAdmin.AdminId);

                if (result.Success)
                {
                    return Ok(ApiResponse.CreateSuccess(new { adminId = result.AdminId }, result.Message));
                }

                return BadRequest(ApiResponse.CreateError(result.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建管理员时发生异常");
                return StatusCode(500, ApiResponse.CreateError("系统异常，请稍后重试"));
            }
        }

        /// <summary>
        /// 更新管理员信息（仅系统管理员）
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="updateDto">更新请求</param>
        /// <returns>更新结果</returns>
        [HttpPut("{adminId}")]
        public async Task<IActionResult> UpdateAdmin(int adminId, [FromBody] UpdateAdminDto updateDto)
        {
            try
            {
               // 获取当前用户ID
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized("用户身份验证失败");

                var operatorAdmin = await _adminService.GetAdminByUserIdAsync(userId);
                if (operatorAdmin == null)
                {
                    return Forbid(ApiResponse.CreateError("只有管理员可以执行此操作").ToString());
                }

                var result = await _adminService.UpdateAdminAsync(adminId, updateDto, operatorAdmin.AdminId);

                if (result.Success)
                {
                    return Ok(ApiResponse.CreateSuccess(result.Message));
                }

                return BadRequest(ApiResponse.CreateError(result.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新管理员时发生异常");
                return StatusCode(500, ApiResponse.CreateError("系统异常，请稍后重试"));
            }
        }

        /// <summary>
        /// 删除管理员（仅系统管理员）
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{adminId}")]
        public async Task<IActionResult> DeleteAdmin(int adminId)
        {
            try
            {
                // 获取当前用户ID
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized("用户身份验证失败");

                var operatorAdmin = await _adminService.GetAdminByUserIdAsync(userId);
                if (operatorAdmin == null)
                {
                    return Forbid(ApiResponse.CreateError("只有管理员可以执行此操作").ToString());
                }

                var result = await _adminService.DeleteAdminAsync(adminId, operatorAdmin.AdminId);

                if (result.Success)
                {
                    return Ok(ApiResponse.CreateSuccess(result.Message));
                }

                return BadRequest(ApiResponse.CreateError(result.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除管理员时发生异常");
                return StatusCode(500, ApiResponse.CreateError("系统异常，请稍后重试"));
            }
        }

        /// <summary>
        /// 获取管理员详情
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <returns>管理员详情</returns>
        [HttpGet("{adminId}")]
        public async Task<IActionResult> GetAdminDetails(int adminId)
        {
            try
            {
                var admin = await _adminService.GetAdminDetailsAsync(adminId);
                if (admin == null)
                {
                    return NotFound(ApiResponse.CreateError("管理员不存在"));
                }

                return Ok(ApiResponse.CreateSuccess(admin));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取管理员详情时发生异常");
                return StatusCode(500, ApiResponse.CreateError("系统异常，请稍后重试"));
            }
        }

        /// <summary>
        /// 分页获取管理员列表
        /// </summary>
        /// <param name="pageIndex">页索引</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="role">角色筛选</param>
        /// <param name="searchKeyword">搜索关键字</param>
        /// <returns>管理员列表</returns>
        [HttpGet]
        public async Task<IActionResult> GetAdmins(
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? role = null,
            [FromQuery] string? searchKeyword = null)
        {
            try
            {
                // 获取当前用户ID
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized("用户身份验证失败");

                var currentAdmin = await _adminService.GetAdminByUserIdAsync(userId);
                if (currentAdmin == null)
                {
                    return Forbid(ApiResponse.CreateError("只有管理员可以查看管理员列表").ToString());
                }

                var (admins, totalCount) = await _adminService.GetAdminsAsync(pageIndex, pageSize, role, searchKeyword);

                return Ok(ApiResponse.CreateSuccess(new
                {
                    admins,
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
                _logger.LogError(ex, "获取管理员列表时发生异常");
                return StatusCode(500, ApiResponse.CreateError("系统异常，请稍后重试"));
            }
        }

        /// <summary>
        /// 获取当前用户的管理员信息
        /// </summary>
        /// <returns>管理员信息</returns>
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentAdminInfo()
        {
            try
            {
                // 获取当前用户ID
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized("用户身份验证失败");

                var admin = await _adminService.GetAdminByUserIdAsync(userId);
                if (admin == null)
                {
                    return NotFound(ApiResponse.CreateError("当前用户不是管理员"));
                }

                return Ok(ApiResponse.CreateSuccess(admin));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取当前管理员信息时发生异常");
                return StatusCode(500, ApiResponse.CreateError("系统异常，请稍后重试"));
            }
        }
        #endregion

        #region 举报处理
        /// <summary>
        /// 处理举报
        /// </summary>
        /// <param name="reportId">举报ID</param>
        /// <param name="handleDto">处理请求</param>
        /// <returns>处理结果</returns>
        [HttpPost("reports/{reportId}/handle")]
        public async Task<IActionResult> HandleReport(int reportId, [FromBody] HandleReportDto handleDto)
        {
            try
            {
                // 获取当前用户ID
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized("用户身份验证失败");

                var admin = await _adminService.GetAdminByUserIdAsync(userId);
                if (admin == null)
                {
                    return Forbid(ApiResponse.CreateError("只有管理员可以处理举报").ToString());
                }

                var result = await _adminService.HandleReportAsync(reportId, handleDto, admin.AdminId);

                if (result.Success)
                {
                    return Ok(ApiResponse.CreateSuccess(result.Message));
                }

                return BadRequest(ApiResponse.CreateError(result.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理举报时发生异常");
                return StatusCode(500, ApiResponse.CreateError("系统异常，请稍后重试"));
            }
        }

        /// <summary>
        /// 获取管理员负责的举报列表
        /// </summary>
        /// <param name="pageIndex">页索引</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="status">状态筛选</param>
        /// <returns>举报列表</returns>
        [HttpGet("reports")]
        public async Task<IActionResult> GetAdminReports(
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null)
        {
            try
            {
                // 获取当前用户ID
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized("用户身份验证失败");

                var admin = await _adminService.GetAdminByUserIdAsync(userId);
                if (admin == null)
                {
                    return Forbid(ApiResponse.CreateError("只有管理员可以查看举报列表").ToString());
                }

                var (reports, totalCount) = await _adminService.GetAdminReportsAsync(admin.AdminId, pageIndex, pageSize, status);

                return Ok(ApiResponse.CreateSuccess(new
                {
                    reports,
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
                _logger.LogError(ex, "获取管理员举报列表时发生异常");
                return StatusCode(500, ApiResponse.CreateError("系统异常，请稍后重试"));
            }
        }
        #endregion

        #region 审计日志
        /// <summary>
        /// 获取管理员操作日志
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="pageIndex">页索引</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="startDate">起始时间</param>
        /// <param name="endDate">结束时间</param>
        /// <returns>操作日志列表</returns>
        [HttpGet("{adminId}/audit-logs")]
        public async Task<IActionResult> GetAdminAuditLogs(
            int adminId,
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 10,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // 获取当前用户ID
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized("用户身份验证失败");

                var currentAdmin = await _adminService.GetAdminByUserIdAsync(userId);
                if (currentAdmin == null)
                {
                    return Forbid(ApiResponse.CreateError("只有管理员可以查看审计日志").ToString());
                }

                // 验证权限：只能查看自己的日志，或者系统管理员可以查看所有日志
                if (currentAdmin.AdminId != adminId && currentAdmin.Role != "super")
                {
                    return Forbid(ApiResponse.CreateError("没有权限查看此管理员的操作日志").ToString());
                }

                var (logs, totalCount) = await _adminService.GetAdminAuditLogsAsync(adminId, pageIndex, pageSize, startDate, endDate);

                return Ok(ApiResponse.CreateSuccess(new
                {
                    logs,
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
                _logger.LogError(ex, "获取管理员操作日志时发生异常");
                return StatusCode(500, ApiResponse.CreateError("系统异常，请稍后重试"));
            }
        }

        /// <summary>
        /// 获取所有审计日志（仅系统管理员）
        /// </summary>
        /// <param name="pageIndex">页索引</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="targetAdminId">目标管理员ID筛选</param>
        /// <param name="actionType">操作类型筛选</param>
        /// <param name="startDate">起始时间</param>
        /// <param name="endDate">结束时间</param>
        /// <returns>所有审计日志</returns>
        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAllAuditLogs(
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? targetAdminId = null,
            [FromQuery] string? actionType = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // 获取当前用户ID
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized("用户身份验证失败");

                var currentAdmin = await _adminService.GetAdminByUserIdAsync(userId);
                if (currentAdmin == null)
                {
                    return Forbid(ApiResponse.CreateError("只有管理员可以查看审计日志").ToString());
                }

                var (logs, totalCount) = await _adminService.GetAllAuditLogsAsync(
                    currentAdmin.AdminId, pageIndex, pageSize, targetAdminId, actionType, startDate, endDate);

                return Ok(ApiResponse.CreateSuccess(new
                {
                    logs,
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
                _logger.LogError(ex, "获取所有审计日志时发生异常");
                return StatusCode(500, ApiResponse.CreateError("系统异常，请稍后重试"));
            }
        }
        #endregion

        #region 统计信息
        /// <summary>
        /// 获取管理员统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetAdminStatistics()
        {
            try
            {
                // 获取当前用户ID
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized("用户身份验证失败");

                var admin = await _adminService.GetAdminByUserIdAsync(userId);
                if (admin == null)
                {
                    return Forbid(ApiResponse.CreateError("只有管理员可以查看统计信息").ToString());
                }

                var statistics = await _adminService.GetAdminStatisticsAsync();

                return Ok(ApiResponse.CreateSuccess(statistics));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取管理员统计信息时发生异常");
                return StatusCode(500, ApiResponse.CreateError("系统异常，请稍后重试"));
            }
        }

        /// <summary>
        /// 验证分类管理权限
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>权限验证结果</returns>
        [HttpGet("permissions/category/{categoryId}")]
        public async Task<IActionResult> ValidateCategoryPermission(int categoryId)
        {
            try
            {
                // 获取当前用户ID
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized("用户身份验证失败");

                var admin = await _adminService.GetAdminByUserIdAsync(userId);
                if (admin == null)
                {
                    return Ok(ApiResponse.CreateSuccess(new { hasPermission = false, message = "用户不是管理员" }));
                }

                var hasPermission = await _adminService.ValidateCategoryPermissionAsync(admin.AdminId, categoryId);

                return Ok(ApiResponse.CreateSuccess(new { hasPermission, adminRole = admin.Role }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证分类管理权限时发生异常");
                return StatusCode(500, ApiResponse.CreateError("系统异常，请稍后重试"));
            }
        }

        /// <summary>
        /// 验证举报处理权限
        /// </summary>
        /// <param name="reportId">举报ID</param>
        /// <returns>权限验证结果</returns>
        [HttpGet("permissions/report/{reportId}")]
        public async Task<IActionResult> ValidateReportPermission(int reportId)
        {
            try
            {
                // 获取当前用户ID
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized("用户身份验证失败");

                var admin = await _adminService.GetAdminByUserIdAsync(userId);
                if (admin == null)
                {
                    return Ok(ApiResponse.CreateSuccess(new { hasPermission = false, message = "用户不是管理员" }));
                }

                var hasPermission = await _adminService.ValidateReportPermissionAsync(admin.AdminId, reportId);

                return Ok(ApiResponse.CreateSuccess(new { hasPermission, adminRole = admin.Role }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证举报处理权限时发生异常");
                return StatusCode(500, ApiResponse.CreateError("系统异常，请稍后重试"));
            }
        }
        #endregion
    }
}
