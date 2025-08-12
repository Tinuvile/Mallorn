using CampusTrade.API.Infrastructure.Utils.Performance;
using CampusTrade.API.Models.DTOs.Admin;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CampusTrade.API.Services.Admin
{
    /// <summary>
    /// 管理员服务实现
    /// </summary>
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUserRepository _userRepository;
        private readonly IReportsRepository _reportsRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AdminService> _logger;
        private readonly Serilog.ILogger _serilogLogger;

        public AdminService(
            IAdminRepository adminRepository,
            IAuditLogRepository auditLogRepository,
            IUserRepository userRepository,
            IReportsRepository reportsRepository,
            IUnitOfWork unitOfWork,
            ILogger<AdminService> logger)
        {
            _adminRepository = adminRepository;
            _auditLogRepository = auditLogRepository;
            _userRepository = userRepository;
            _reportsRepository = reportsRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _serilogLogger = Log.ForContext<AdminService>();
        }

        #region 管理员管理
        /// <summary>
        /// 创建管理员
        /// </summary>
        public async Task<(bool Success, string Message, int? AdminId)> CreateAdminAsync(CreateAdminDto createDto, int operatorAdminId)
        {
            using var performanceTracker = new PerformanceTracker(_serilogLogger, "CreateAdmin", "AdminService")
                .AddContext("UserId", createDto.UserId)
                .AddContext("Role", createDto.Role)
                .AddContext("OperatorAdminId", operatorAdminId);

            try
            {
                _serilogLogger.Information("开始创建管理员 - 用户ID: {UserId}, 角色: {Role}, 操作员: {OperatorAdminId}",
                    createDto.UserId, createDto.Role, operatorAdminId);

                // 验证操作员权限（只有系统管理员可以创建管理员）
                var operatorAdmin = await _adminRepository.GetByPrimaryKeyAsync(operatorAdminId);
                if (operatorAdmin?.Role != Models.Entities.Admin.Roles.Super)
                {
                    _serilogLogger.Warning("创建管理员权限验证失败 - 操作员ID: {OperatorAdminId}, 角色: {Role}",
                        operatorAdminId, operatorAdmin?.Role);
                    return (false, "只有系统管理员可以创建管理员", null);
                }

                // 验证用户是否存在
                var user = await _userRepository.GetByPrimaryKeyAsync(createDto.UserId);
                if (user == null)
                {
                    _serilogLogger.Warning("创建管理员失败 - 用户不存在: {UserId}", createDto.UserId);
                    return (false, "用户不存在", null);
                }

                // 验证用户是否已经是管理员
                if (await _adminRepository.IsUserAdminAsync(createDto.UserId))
                {
                    _serilogLogger.Warning("创建管理员失败 - 用户已是管理员: {UserId}", createDto.UserId);
                    return (false, "该用户已经是管理员", null);
                }

                // 如果是分类管理员，验证分类是否存在且未分配
                if (createDto.Role == Models.Entities.Admin.Roles.CategoryAdmin)
                {
                    if (!createDto.AssignedCategory.HasValue)
                    {
                        return (false, "模块管理员必须指定负责的分类", null);
                    }

                    var category = await _unitOfWork.Categories.GetByPrimaryKeyAsync(createDto.AssignedCategory.Value);
                    if (category == null)
                    {
                        return (false, "指定的分类不存在", null);
                    }

                    // 检查分类是否为一级分类
                    if (category.ParentId.HasValue)
                    {
                        return (false, "只能分配一级分类给模块管理员", null);
                    }

                    // 检查分类是否已分配
                    if (await _adminRepository.IsCategoryAssignedAsync(createDto.AssignedCategory.Value))
                    {
                        return (false, "该分类已分配给其他管理员", null);
                    }
                }

                // 创建管理员
                var admin = new Models.Entities.Admin
                {
                    UserId = createDto.UserId,
                    Role = createDto.Role,
                    AssignedCategory = createDto.Role == Models.Entities.Admin.Roles.CategoryAdmin ? createDto.AssignedCategory : null,
                    CreatedAt = DateTime.Now
                };

                await _adminRepository.AddAsync(admin);
                await _unitOfWork.SaveChangesAsync();

                // 记录审计日志
                await _auditLogRepository.LogAdminActionAsync(
                    operatorAdminId,
                    AuditLog.ActionTypes.ModifyPermission,
                    admin.AdminId,
                    $"创建管理员 - 用户ID: {createDto.UserId}, 角色: {createDto.Role}, 分配分类: {createDto.AssignedCategory}");

                _serilogLogger.Information("管理员创建成功 - 管理员ID: {AdminId}, 用户ID: {UserId}, 角色: {Role}",
                    admin.AdminId, createDto.UserId, createDto.Role);

                return (true, "管理员创建成功", admin.AdminId);
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "创建管理员异常 - 用户ID: {UserId}, 错误: {ErrorMessage}",
                    createDto.UserId, ex.Message);
                return (false, "系统异常，请稍后重试", null);
            }
        }

        /// <summary>
        /// 更新管理员信息
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateAdminAsync(int adminId, UpdateAdminDto updateDto, int operatorAdminId)
        {
            using var performanceTracker = new PerformanceTracker(_serilogLogger, "UpdateAdmin", "AdminService")
                .AddContext("AdminId", adminId)
                .AddContext("OperatorAdminId", operatorAdminId);

            try
            {
                // 验证操作员权限
                var operatorAdmin = await _adminRepository.GetByPrimaryKeyAsync(operatorAdminId);
                if (operatorAdmin?.Role != Models.Entities.Admin.Roles.Super)
                {
                    return (false, "只有系统管理员可以修改管理员信息");
                }

                var admin = await _adminRepository.GetByPrimaryKeyAsync(adminId);
                if (admin == null)
                {
                    return (false, "管理员不存在");
                }

                var changes = new List<string>();

                // 更新角色
                if (!string.IsNullOrEmpty(updateDto.Role) && updateDto.Role != admin.Role)
                {
                    changes.Add($"角色: {admin.Role} -> {updateDto.Role}");
                    admin.Role = updateDto.Role;
                }

                // 更新分配的分类
                if (updateDto.AssignedCategory != admin.AssignedCategory)
                {
                    if (updateDto.AssignedCategory.HasValue)
                    {
                        // 验证分类是否存在且为一级分类
                        var category = await _unitOfWork.Categories.GetByPrimaryKeyAsync(updateDto.AssignedCategory.Value);
                        if (category == null || category.ParentId.HasValue)
                        {
                            return (false, "只能分配存在的一级分类");
                        }

                        // 检查分类是否已分配给其他管理员
                        if (await _adminRepository.IsCategoryAssignedAsync(updateDto.AssignedCategory.Value, adminId))
                        {
                            return (false, "该分类已分配给其他管理员");
                        }
                    }

                    changes.Add($"分配分类: {admin.AssignedCategory} -> {updateDto.AssignedCategory}");
                    admin.AssignedCategory = updateDto.AssignedCategory;
                }

                if (changes.Any())
                {
                    _adminRepository.Update(admin);
                    await _unitOfWork.SaveChangesAsync();

                    // 记录审计日志
                    await _auditLogRepository.LogAdminActionAsync(
                        operatorAdminId,
                        AuditLog.ActionTypes.ModifyPermission,
                        adminId,
                        $"修改管理员信息 - {string.Join(", ", changes)}");

                    _serilogLogger.Information("管理员信息更新成功 - 管理员ID: {AdminId}, 变更: {Changes}",
                        adminId, string.Join(", ", changes));

                    return (true, "管理员信息更新成功");
                }

                return (true, "没有需要更新的信息");
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "更新管理员信息异常 - 管理员ID: {AdminId}, 错误: {ErrorMessage}",
                    adminId, ex.Message);
                return (false, "系统异常，请稍后重试");
            }
        }

        /// <summary>
        /// 删除管理员
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteAdminAsync(int adminId, int operatorAdminId)
        {
            using var performanceTracker = new PerformanceTracker(_serilogLogger, "DeleteAdmin", "AdminService")
                .AddContext("AdminId", adminId)
                .AddContext("OperatorAdminId", operatorAdminId);

            try
            {
                // 验证操作员权限
                var operatorAdmin = await _adminRepository.GetByPrimaryKeyAsync(operatorAdminId);
                if (operatorAdmin?.Role != Models.Entities.Admin.Roles.Super)
                {
                    return (false, "只有系统管理员可以删除管理员");
                }

                var admin = await _adminRepository.GetAdminWithDetailsAsync(adminId);
                if (admin == null)
                {
                    return (false, "管理员不存在");
                }

                // 不能删除自己
                if (adminId == operatorAdminId)
                {
                    return (false, "不能删除自己的管理员权限");
                }

                var deletedInfo = $"用户ID: {admin.UserId}, 角色: {admin.Role}, 分配分类: {admin.AssignedCategory}";

                _adminRepository.Delete(admin);
                await _unitOfWork.SaveChangesAsync();

                // 记录审计日志
                await _auditLogRepository.LogAdminActionAsync(
                    operatorAdminId,
                    AuditLog.ActionTypes.ModifyPermission,
                    adminId,
                    $"删除管理员 - {deletedInfo}");

                _serilogLogger.Information("管理员删除成功 - 管理员ID: {AdminId}, 信息: {Info}",
                    adminId, deletedInfo);

                return (true, "管理员删除成功");
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "删除管理员异常 - 管理员ID: {AdminId}, 错误: {ErrorMessage}",
                    adminId, ex.Message);
                return (false, "系统异常，请稍后重试");
            }
        }

        /// <summary>
        /// 获取管理员详情
        /// </summary>
        public async Task<AdminResponseDto?> GetAdminDetailsAsync(int adminId)
        {
            try
            {
                var admin = await _adminRepository.GetAdminWithDetailsAsync(adminId);
                if (admin == null)
                    return null;

                return MapToAdminResponseDto(admin);
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "获取管理员详情异常 - 管理员ID: {AdminId}", adminId);
                return null;
            }
        }

        /// <summary>
        /// 分页获取管理员列表
        /// </summary>
        public async Task<(IEnumerable<AdminResponseDto> Admins, int TotalCount)> GetAdminsAsync(
            int pageIndex, 
            int pageSize, 
            string? role = null, 
            string? searchKeyword = null)
        {
            try
            {
                var (admins, totalCount) = await _adminRepository.GetPagedAdminsAsync(pageIndex, pageSize, role, searchKeyword);
                
                var adminDtos = admins.Select(MapToAdminResponseDto).ToList();
                
                return (adminDtos, totalCount);
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "获取管理员列表异常");
                return (Enumerable.Empty<AdminResponseDto>(), 0);
            }
        }

        /// <summary>
        /// 根据用户ID获取管理员信息
        /// </summary>
        public async Task<AdminResponseDto?> GetAdminByUserIdAsync(int userId)
        {
            try
            {
                var admin = await _adminRepository.GetByUserIdAsync(userId);
                if (admin == null)
                    return null;

                return MapToAdminResponseDto(admin);
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "根据用户ID获取管理员信息异常 - 用户ID: {UserId}", userId);
                return null;
            }
        }
        #endregion

        #region 举报处理
        /// <summary>
        /// 处理举报
        /// </summary>
        public async Task<(bool Success, string Message)> HandleReportAsync(int reportId, HandleReportDto handleDto, int adminId)
        {
            using var performanceTracker = new PerformanceTracker(_serilogLogger, "HandleReport", "AdminService")
                .AddContext("ReportId", reportId)
                .AddContext("AdminId", adminId)
                .AddContext("HandleResult", handleDto.HandleResult);

            try
            {
                // 验证管理员权限
                if (!await ValidateReportPermissionAsync(adminId, reportId))
                {
                    return (false, "没有权限处理此举报");
                }

                // 获取举报信息
                var report = await _reportsRepository.GetReportWithDetailsAsync(reportId);
                if (report == null)
                {
                    return (false, "举报不存在");
                }

                if (report.Status != "待处理")
                {
                    return (false, "只能处理待处理状态的举报");
                }

                // 更新举报状态
                string newStatus = handleDto.HandleResult switch
                {
                    "通过" => "已处理",
                    "驳回" => "已驳回",
                    "需要更多信息" => "需要补充信息",
                    _ => "已处理"
                };

                await _reportsRepository.UpdateReportStatusAsync(reportId, newStatus);

                // 如果需要处罚，这里可以添加处罚逻辑
                var penaltyInfo = "";
                if (handleDto.ApplyPenalty && !string.IsNullOrEmpty(handleDto.PenaltyType))
                {
                    penaltyInfo = $", 处罚类型: {handleDto.PenaltyType}";
                    if (handleDto.PenaltyDuration.HasValue)
                    {
                        penaltyInfo += $", 处罚时长: {handleDto.PenaltyDuration}天";
                    }
                    // TODO: 实现具体的处罚逻辑（警告、禁言、封号）
                }

                await _unitOfWork.SaveChangesAsync();

                // 记录审计日志
                var logDetail = $"处理举报 - 举报ID: {reportId}, 处理结果: {handleDto.HandleResult}, 备注: {handleDto.HandleNote}{penaltyInfo}";
                await _auditLogRepository.LogAdminActionAsync(adminId, AuditLog.ActionTypes.HandleReport, reportId, logDetail);

                _serilogLogger.Information("举报处理成功 - 举报ID: {ReportId}, 管理员ID: {AdminId}, 处理结果: {Result}",
                    reportId, adminId, handleDto.HandleResult);

                return (true, "举报处理成功");
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "处理举报异常 - 举报ID: {ReportId}, 管理员ID: {AdminId}",
                    reportId, adminId);
                return (false, "系统异常，请稍后重试");
            }
        }

        /// <summary>
        /// 获取管理员负责的举报列表
        /// </summary>
        public async Task<(IEnumerable<Reports> Reports, int TotalCount)> GetAdminReportsAsync(
            int adminId, 
            int pageIndex, 
            int pageSize, 
            string? status = null)
        {
            try
            {
                var admin = await _adminRepository.GetAdminWithDetailsAsync(adminId);
                if (admin == null)
                {
                    return (Enumerable.Empty<Reports>(), 0);
                }

                // 系统管理员可以看所有举报
                if (admin.Role == Models.Entities.Admin.Roles.Super)
                {
                    return await _reportsRepository.GetPagedReportsAsync(pageIndex, pageSize, status: status);
                }

                // 模块管理员只能看自己负责分类的举报
                if (admin.Role == Models.Entities.Admin.Roles.CategoryAdmin && admin.AssignedCategory.HasValue)
                {
                    // 这里需要实现根据分类筛选举报的逻辑
                    // 暂时返回空，需要在ReportsRepository中添加相应方法
                    return (Enumerable.Empty<Reports>(), 0);
                }

                return (Enumerable.Empty<Reports>(), 0);
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "获取管理员举报列表异常 - 管理员ID: {AdminId}", adminId);
                return (Enumerable.Empty<Reports>(), 0);
            }
        }
        #endregion

        #region 权限验证
        /// <summary>
        /// 验证管理员权限
        /// </summary>
        public async Task<bool> ValidateAdminPermissionAsync(int adminId, string requiredRole)
        {
            try
            {
                var admin = await _adminRepository.GetByPrimaryKeyAsync(adminId);
                if (admin == null)
                    return false;

                // 系统管理员拥有所有权限
                if (admin.Role == Models.Entities.Admin.Roles.Super)
                    return true;

                return admin.Role == requiredRole;
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "验证管理员权限异常 - 管理员ID: {AdminId}", adminId);
                return false;
            }
        }

        /// <summary>
        /// 验证分类管理权限
        /// </summary>
        public async Task<bool> ValidateCategoryPermissionAsync(int adminId, int categoryId)
        {
            try
            {
                var admin = await _adminRepository.GetAdminWithDetailsAsync(adminId);
                if (admin == null)
                    return false;

                // 系统管理员拥有所有分类权限
                if (admin.Role == Models.Entities.Admin.Roles.Super)
                    return true;

                // 模块管理员只能管理分配给自己的分类
                if (admin.Role == Models.Entities.Admin.Roles.CategoryAdmin)
                {
                    // 获取分类的一级分类
                    var category = await _unitOfWork.Categories.GetByPrimaryKeyAsync(categoryId);
                    if (category == null)
                        return false;

                    // 找到一级分类
                    var primaryCategoryId = categoryId;
                    while (category.ParentId.HasValue)
                    {
                        primaryCategoryId = category.ParentId.Value;
                        category = await _unitOfWork.Categories.GetByPrimaryKeyAsync(primaryCategoryId);
                        if (category == null)
                            return false;
                    }

                    return admin.AssignedCategory == primaryCategoryId;
                }

                return false;
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "验证分类管理权限异常 - 管理员ID: {AdminId}, 分类ID: {CategoryId}",
                    adminId, categoryId);
                return false;
            }
        }

        /// <summary>
        /// 验证举报处理权限
        /// </summary>
        public async Task<bool> ValidateReportPermissionAsync(int adminId, int reportId)
        {
            try
            {
                var admin = await _adminRepository.GetAdminWithDetailsAsync(adminId);
                if (admin == null)
                    return false;

                // 系统管理员拥有所有举报处理权限
                if (admin.Role == Models.Entities.Admin.Roles.Super)
                    return true;

                // 模块管理员只能处理自己负责分类的商品的举报
                if (admin.Role == Models.Entities.Admin.Roles.CategoryAdmin && admin.AssignedCategory.HasValue)
                {
                    // 获取举报关联的商品分类
                    var primaryCategory = await _reportsRepository.GetReportProductPrimaryCategoryAsync(reportId);
                    return primaryCategory?.CategoryId == admin.AssignedCategory;
                }

                return false;
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "验证举报处理权限异常 - 管理员ID: {AdminId}, 举报ID: {ReportId}",
                    adminId, reportId);
                return false;
            }
        }
        #endregion

        #region 审计日志
        /// <summary>
        /// 获取管理员操作日志
        /// </summary>
        public async Task<(IEnumerable<AuditLogResponseDto> Logs, int TotalCount)> GetAdminAuditLogsAsync(
            int adminId,
            int pageIndex,
            int pageSize,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                var (logs, totalCount) = await _auditLogRepository.GetPagedLogsAsync(
                    pageIndex, pageSize, adminId, null, startDate, endDate);

                var logDtos = logs.Select(MapToAuditLogResponseDto).ToList();

                return (logDtos, totalCount);
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "获取管理员操作日志异常 - 管理员ID: {AdminId}", adminId);
                return (Enumerable.Empty<AuditLogResponseDto>(), 0);
            }
        }

        /// <summary>
        /// 获取所有审计日志（仅系统管理员）
        /// </summary>
        public async Task<(IEnumerable<AuditLogResponseDto> Logs, int TotalCount)> GetAllAuditLogsAsync(
            int requestAdminId,
            int pageIndex,
            int pageSize,
            int? targetAdminId = null,
            string? actionType = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                // 验证权限 - 只有系统管理员可以查看所有审计日志
                if (!await ValidateAdminPermissionAsync(requestAdminId, Models.Entities.Admin.Roles.Super))
                {
                    _serilogLogger.Warning("非系统管理员尝试查看所有审计日志 - 管理员ID: {AdminId}", requestAdminId);
                    return (Enumerable.Empty<AuditLogResponseDto>(), 0);
                }

                var (logs, totalCount) = await _auditLogRepository.GetPagedLogsAsync(
                    pageIndex, pageSize, targetAdminId, actionType, startDate, endDate);

                var logDtos = logs.Select(MapToAuditLogResponseDto).ToList();

                return (logDtos, totalCount);
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "获取所有审计日志异常 - 请求管理员ID: {RequestAdminId}", requestAdminId);
                return (Enumerable.Empty<AuditLogResponseDto>(), 0);
            }
        }

        /// <summary>
        /// 记录管理员操作
        /// </summary>
        public async Task<bool> LogAdminActionAsync(int adminId, string actionType, int? targetId = null, string? detail = null)
        {
            try
            {
                await _auditLogRepository.LogAdminActionAsync(adminId, actionType, targetId, detail);
                return true;
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "记录管理员操作异常 - 管理员ID: {AdminId}, 操作类型: {ActionType}",
                    adminId, actionType);
                return false;
            }
        }
        #endregion

        #region 统计信息
        /// <summary>
        /// 获取管理员统计信息
        /// </summary>
        public async Task<Dictionary<string, object>> GetAdminStatisticsAsync()
        {
            try
            {
                var adminStats = await _adminRepository.GetAdminStatisticsAsync();
                var auditStats = await _auditLogRepository.GetAuditStatisticsAsync(DateTime.Now.AddDays(-30));

                var result = new Dictionary<string, object>();

                // 管理员统计
                foreach (var stat in adminStats)
                {
                    result[stat.Key] = stat.Value;
                }

                // 最近30天的操作统计
                foreach (var stat in auditStats)
                {
                    result[$"最近30天_{stat.Key}"] = stat.Value;
                }

                return result;
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "获取管理员统计信息异常");
                return new Dictionary<string, object>();
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 映射管理员实体到响应DTO
        /// </summary>
        private AdminResponseDto MapToAdminResponseDto(Models.Entities.Admin admin)
        {
            return new AdminResponseDto
            {
                AdminId = admin.AdminId,
                UserId = admin.UserId,
                Username = admin.User?.Username,
                Email = admin.User?.Email ?? string.Empty,
                Role = admin.Role,
                RoleDisplayName = admin.Role switch
                {
                    Models.Entities.Admin.Roles.Super => "系统管理员",
                    Models.Entities.Admin.Roles.CategoryAdmin => "模块管理员",
                    _ => admin.Role
                },
                AssignedCategory = admin.AssignedCategory,
                AssignedCategoryName = admin.Category?.Name,
                CreatedAt = admin.CreatedAt,
                IsActive = admin.User?.IsActive == 1
            };
        }

        /// <summary>
        /// 映射审计日志实体到响应DTO
        /// </summary>
        private AuditLogResponseDto MapToAuditLogResponseDto(AuditLog log)
        {
            return new AuditLogResponseDto
            {
                LogId = log.LogId,
                AdminId = log.AdminId,
                AdminUsername = log.Admin?.User?.Username,
                AdminRole = log.Admin?.Role ?? string.Empty,
                ActionType = log.ActionType,
                TargetId = log.TargetId,
                LogDetail = log.LogDetail,
                LogTime = log.LogTime
            };
        }
        #endregion
    }
}
