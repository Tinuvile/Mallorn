using CampusTrade.API.Infrastructure.Utils.Performance;
using CampusTrade.API.Models.DTOs;
using CampusTrade.API.Models.DTOs.Admin;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Interfaces;
using CampusTrade.API.Services.Notification;
using Microsoft.EntityFrameworkCore;
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
        private readonly IProductRepository _productRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AdminService> _logger;
        private readonly Serilog.ILogger _serilogLogger;

        private readonly ICreditService _creditService;
        private readonly NotifiService _notificationService;


        public AdminService(
            IAdminRepository adminRepository,
            IAuditLogRepository auditLogRepository,
            IUserRepository userRepository,
            IReportsRepository reportsRepository,
            IProductRepository productRepository,
            IRepository<Category> categoryRepository,
            IUnitOfWork unitOfWork,
            ILogger<AdminService> logger,
            ICreditService creditService,
            NotifiService notificationService)
        {
            _adminRepository = adminRepository;
            _auditLogRepository = auditLogRepository;
            _userRepository = userRepository;
            _reportsRepository = reportsRepository;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _creditService = creditService;
            _notificationService = notificationService;
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
        /// 通过用户名创建管理员
        /// </summary>
        public async Task<(bool Success, string Message, int? AdminId)> CreateAdminByUsernameAsync(CreateAdminByUsernameDto createDto, int operatorAdminId)
        {
            using var performanceTracker = new PerformanceTracker(_serilogLogger, "CreateAdminByUsername", "AdminService")
                .AddContext("Username", createDto.Username)
                .AddContext("Role", createDto.Role)
                .AddContext("OperatorAdminId", operatorAdminId);

            try
            {
                _serilogLogger.Information("开始通过用户名创建管理员 - 用户名: {Username}, 角色: {Role}, 操作员: {OperatorAdminId}",
                    createDto.Username, createDto.Role, operatorAdminId);

                // 验证操作员权限（只有系统管理员可以创建管理员）
                var operatorAdmin = await _adminRepository.GetByPrimaryKeyAsync(operatorAdminId);
                if (operatorAdmin?.Role != Models.Entities.Admin.Roles.Super)
                {
                    _serilogLogger.Warning("创建管理员权限验证失败 - 操作员ID: {OperatorAdminId}, 角色: {Role}",
                        operatorAdminId, operatorAdmin?.Role);
                    return (false, "只有系统管理员可以创建管理员", null);
                }

                // 通过用户名查找用户
                var user = await _userRepository.GetByUsernameAsync(createDto.Username);
                if (user == null)
                {
                    _serilogLogger.Warning("创建管理员失败 - 用户不存在: {Username}", createDto.Username);
                    return (false, "用户不存在", null);
                }

                // 验证邮箱是否匹配
                if (user.Email != createDto.Email)
                {
                    _serilogLogger.Warning("创建管理员失败 - 邮箱不匹配: {Username}, 提供邮箱: {ProvidedEmail}, 实际邮箱: {ActualEmail}",
                        createDto.Username, createDto.Email, user.Email);
                    return (false, "用户邮箱不匹配", null);
                }

                // 验证用户是否已经是管理员
                if (await _adminRepository.IsUserAdminAsync(user.UserId))
                {
                    _serilogLogger.Warning("创建管理员失败 - 用户已是管理员: {Username}", createDto.Username);
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

                    // 检查分类是否已经有管理员
                    var existingCategoryAdmin = await _adminRepository.GetCategoryAdminByCategoryIdAsync(createDto.AssignedCategory.Value);
                    if (existingCategoryAdmin != null)
                    {
                        return (false, "该分类已经有管理员负责", null);
                    }
                }

                // 创建管理员记录
                var admin = new Models.Entities.Admin
                {
                    UserId = user.UserId,
                    Role = createDto.Role,
                    AssignedCategory = createDto.AssignedCategory,
                    CreatedAt = DateTime.UtcNow
                };

                await _adminRepository.AddAsync(admin);
                await _unitOfWork.SaveChangesAsync();

                // 记录审计日志（简化版本）
                // await _auditLogRepository.LogAsync(operatorAdminId, "创建管理员",
                //     admin.AdminId, $"为用户 {createDto.Username} 创建了管理员权限，角色：{createDto.Role}");

                _serilogLogger.Information("管理员创建成功 - 管理员ID: {AdminId}, 用户名: {Username}, 角色: {Role}",
                    admin.AdminId, createDto.Username, createDto.Role);

                return (true, "管理员创建成功", admin.AdminId);
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "通过用户名创建管理员异常 - 用户名: {Username}, 错误: {ErrorMessage}",
                    createDto.Username, ex.Message);
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

                // 如果审核通过且需要处罚，执行处罚逻辑
                var penaltyInfo = "";
                if (handleDto.HandleResult == "通过" && handleDto.ApplyPenalty && !string.IsNullOrEmpty(handleDto.PenaltyType))
                {
                    // 确定被举报用户
                    var reportedUserId = GetReportedUserId(report);
                    if (reportedUserId.HasValue)
                    {
                        // 根据处罚类型确定信用分扣减
                        var creditEventType = handleDto.PenaltyType switch
                        {
                            "轻度处罚" => CreditEventType.LightReportPenalty,
                            "中度处罚" => CreditEventType.ModerateReportPenalty, 
                            "重度处罚" => CreditEventType.SevereReportPenalty,
                            _ => CreditEventType.ModerateReportPenalty
                        };

                        var penaltyScore = creditEventType switch
                        {
                            CreditEventType.LightReportPenalty => -5,
                            CreditEventType.ModerateReportPenalty => -10,
                            CreditEventType.SevereReportPenalty => -15,
                            _ => -10
                        };

                        // 执行信用分扣减
                        await _creditService.ApplyCreditChangeAsync(new CreditEvent
                        {
                            UserId = reportedUserId.Value,
                            EventType = creditEventType,
                            Description = $"举报处理通过，{handleDto.PenaltyType}({penaltyScore}分) - 举报ID: {reportId}"
                        }, autoSave: false);

                        penaltyInfo = $", 处罚类型: {handleDto.PenaltyType}({penaltyScore}分)";
                        
                        _serilogLogger.Information("执行举报处罚 - 被举报用户ID: {UserId}, 处罚类型: {PenaltyType}, 扣减分数: {Score}",
                            reportedUserId.Value, handleDto.PenaltyType, penaltyScore);
                    }
                    else
                    {
                        _serilogLogger.Warning("无法确定被举报用户，跳过处罚 - 举报ID: {ReportId}", reportId);
                        penaltyInfo = $", 处罚失败: 无法确定被举报用户";
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                // 记录审计日志
                var logDetail = $"处理举报 - 举报ID: {reportId}, 处理结果: {handleDto.HandleResult}, 备注: {handleDto.HandleNote}{penaltyInfo}";
                await _auditLogRepository.LogAdminActionAsync(adminId, AuditLog.ActionTypes.HandleReport, reportId, logDetail);

                // 发送举报处理结果通知给举报人
                try
                {
                    var reportResultParams = new Dictionary<string, object>
                    {
                        ["reportId"] = reportId.ToString(),
                        ["result"] = GetResultDescription(handleDto.HandleResult)
                    };

                    await _notificationService.CreateNotificationAsync(
                        report.ReporterId,
                        32, // 举报处理结果模板ID
                        reportResultParams,
                        reportId
                    );

                    _serilogLogger.Information("举报处理结果通知已发送，举报人ID: {ReporterId}，举报ID: {ReportId}，处理结果: {Result}",
                        report.ReporterId, reportId, handleDto.HandleResult);
                }
                catch (Exception ex)
                {
                    _serilogLogger.Error(ex, "发送举报处理结果通知失败，举报ID: {ReportId}，举报人ID: {ReporterId}", reportId, report.ReporterId);
                    // 注意：通知发送失败不应该影响举报处理结果，所以这里只记录日志
                }

                // 如果有处罚，发送违规处罚通知给被举报人
                if (handleDto.HandleResult == "通过" && handleDto.ApplyPenalty && !string.IsNullOrEmpty(handleDto.PenaltyType))
                {
                    var reportedUserId = GetReportedUserId(report);
                    if (reportedUserId.HasValue)
                    {
                        try
                        {
                            var punishment = GetPunishmentDescription(handleDto.PenaltyType);
                            var duration = handleDto.PenaltyDuration?.ToString() ?? "永久";

                            var punishmentParams = new Dictionary<string, object>
                            {
                                ["punishment"] = punishment,
                                ["duration"] = duration == "永久" ? "永久" : $"{duration}天"
                            };

                            await _notificationService.CreateNotificationAsync(
                                reportedUserId.Value,
                                33, // 违规处罚通知模板ID
                                punishmentParams,
                                reportId
                            );

                            _serilogLogger.Information("违规处罚通知已发送，被举报人ID: {ReportedUserId}，举报ID: {ReportId}，处罚类型: {PenaltyType}",
                                reportedUserId.Value, reportId, handleDto.PenaltyType);
                        }
                        catch (Exception ex)
                        {
                            _serilogLogger.Error(ex, "发送违规处罚通知失败，举报ID: {ReportId}，被举报人ID: {ReportedUserId}", reportId, reportedUserId.Value);
                            // 注意：通知发送失败不应该影响处罚执行结果，所以这里只记录日志
                        }
                    }
                }

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
                    _serilogLogger.Warning("管理员不存在 - AdminId: {AdminId}", adminId);
                    return (Enumerable.Empty<Reports>(), 0);
                }

                _serilogLogger.Information("获取管理员举报列表 - AdminId: {AdminId}, Role: {Role}, Category: {Category}",
                    adminId, admin.Role, admin.AssignedCategory);

                // 系统管理员可以看所有举报
                if (admin.Role == Models.Entities.Admin.Roles.Super)
                {
                    _serilogLogger.Information("系统管理员查看所有举报");
                    return await _reportsRepository.GetPagedReportsAsync(pageIndex, pageSize, status: status);
                }

                // 模块管理员只能看自己负责分类的举报
                if (admin.Role == Models.Entities.Admin.Roles.CategoryAdmin && admin.AssignedCategory.HasValue)
                {
                    _serilogLogger.Information("分类管理员查看分类 {CategoryId} 的举报", admin.AssignedCategory.Value);
                    var result = await _reportsRepository.GetPagedReportsByCategoryAsync(
                        admin.AssignedCategory.Value,
                        pageIndex,
                        pageSize,
                        status: status);
                    _serilogLogger.Information("分类管理员获得 {Count} 条举报", result.TotalCount);
                    return result;
                }

                _serilogLogger.Warning("管理员角色或分类配置异常 - AdminId: {AdminId}, Role: {Role}, Category: {Category}",
                    adminId, admin.Role, admin.AssignedCategory);
                return (Enumerable.Empty<Reports>(), 0);
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "获取管理员举报列表异常 - 管理员ID: {AdminId}", adminId);
                return (Enumerable.Empty<Reports>(), 0);
            }
        }

        /// <summary>
        /// 获取举报详情（管理员专用）
        /// </summary>
        public async Task<object?> GetReportDetailAsync(int reportId, int adminId)
        {
            try
            {
                var admin = await _adminRepository.GetAdminWithDetailsAsync(adminId);
                if (admin == null)
                {
                    return null;
                }

                // 获取举报详情
                var report = await _reportsRepository.GetReportWithDetailsAsync(reportId);
                if (report == null)
                {
                    return null;
                }

                // 权限检查：系统管理员可以查看所有举报
                if (admin.Role == Models.Entities.Admin.Roles.Super)
                {
                    return ConvertToReportDetailDto(report);
                }

                // 分类管理员只能查看自己负责分类的举报
                if (admin.Role == Models.Entities.Admin.Roles.CategoryAdmin && admin.AssignedCategory.HasValue)
                {
                    // 检查举报是否属于管理员负责的分类
                    if (IsReportInCategory(report, admin.AssignedCategory.Value))
                    {
                        return ConvertToReportDetailDto(report);
                    }
                }

                return null; // 无权限访问
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "获取举报详情异常 - 举报ID: {ReportId}, 管理员ID: {AdminId}", reportId, adminId);
                return null;
            }
        }

        /// <summary>
        /// 获取举报处理结果的友好描述
        /// </summary>
        private string GetResultDescription(string handleResult)
        {
            return handleResult switch
            {
                "通过" => "举报属实，已对被举报方进行相应处理",
                "驳回" => "举报不属实，已驳回此举报",
                "需要更多信息" => "需要您提供更多证据信息",
                _ => "举报已处理"
            };
        }

        /// <summary>
        /// 获取处罚类型的友好描述
        /// </summary>
        private string GetPunishmentDescription(string penaltyType)
        {
            return penaltyType switch
            {
                "轻度处罚" => "警告并扣除信用分",
                "中度处罚" => "限制部分功能并扣除信用分",
                "重度处罚" => "暂时封禁账户并扣除信用分",
                _ => "相关处罚措施"
            };
        }

        /// <summary>
        /// 获取被举报用户ID
        /// </summary>
        private int? GetReportedUserId(Reports report)
        {
            // 根据举报类型确定被举报的用户
            // 由于举报主要针对订单，被举报用户通常是卖家
            return report.Order?.SellerId;
        }

        /// <summary>
        /// 检查举报是否属于指定分类
        /// </summary>
        private bool IsReportInCategory(Reports report, int categoryId)
        {
            try
            {
                // 通过订单获取商品分类
                var product = report.Order?.Product;
                if (product?.Category == null)
                {
                    return false;
                }

                // 检查是否在分类树中
                return IsInCategoryTree(product.Category, categoryId);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 递归检查分类是否在分类树中
        /// </summary>
        private bool IsInCategoryTree(Models.Entities.Category category, int targetCategoryId)
        {
            if (category.CategoryId == targetCategoryId)
            {
                return true;
            }

            if (category.ParentId.HasValue)
            {
                var parent = category.Parent;
                if (parent != null)
                {
                    return IsInCategoryTree(parent, targetCategoryId);
                }
            }

            return false;
        }

        /// <summary>
        /// 将举报实体转换为详情DTO
        /// </summary>
        private object ConvertToReportDetailDto(Reports report)
        {
            var evidences = new List<object>();
            if (report.Evidences != null)
            {
                foreach (var evidence in report.Evidences)
                {
                    evidences.Add(new
                    {
                        evidence_id = evidence.EvidenceId,
                        file_type = evidence.FileType,
                        file_url = evidence.FileUrl,
                        uploaded_at = evidence.UploadedAt.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
            }

            // 通过 AbstractOrder 获取 Order 信息
            var order = report.AbstractOrder?.Order;

            return new
            {
                report_id = report.ReportId,
                order_id = report.OrderId,
                type = report.Type,
                description = report.Description,
                status = report.Status,
                priority = report.Priority,
                create_time = report.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                reporter = report.Reporter != null ? new
                {
                    user_id = report.Reporter.UserId,
                    username = report.Reporter.Username
                } : null,
                evidences = evidences,
                order = order != null ? new
                {
                    order_id = order.OrderId,
                    total_amount = order.TotalAmount,
                    status = order.Status,
                    product = order.Product != null ? new
                    {
                        product_id = order.Product.ProductId,
                        title = order.Product.Title,
                        description = order.Product.Description
                    } : null
                } : null
            };
        }

        /// <summary>
        /// 获取举报审核历史
        /// </summary>
        public async Task<IEnumerable<object>?> GetReportAuditHistoryAsync(int reportId, int adminId)
        {
            try
            {
                var admin = await _adminRepository.GetAdminWithDetailsAsync(adminId);
                if (admin == null)
                {
                    return null;
                }

                // 首先检查管理员是否有权限查看该举报
                var report = await _reportsRepository.GetReportWithDetailsAsync(reportId);
                if (report == null)
                {
                    return null;
                }

                // 权限检查：系统管理员可以查看所有举报历史
                if (admin.Role == Models.Entities.Admin.Roles.Super)
                {
                    return await GetAuditHistoryForReport(reportId);
                }

                // 分类管理员只能查看自己负责分类的举报历史
                if (admin.Role == Models.Entities.Admin.Roles.CategoryAdmin && admin.AssignedCategory.HasValue)
                {
                    // 检查举报是否属于管理员负责的分类
                    if (IsReportInCategory(report, admin.AssignedCategory.Value))
                    {
                        return await GetAuditHistoryForReport(reportId);
                    }
                }

                return null; // 无权限访问
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "获取举报审核历史异常 - 举报ID: {ReportId}, 管理员ID: {AdminId}", reportId, adminId);
                return null;
            }
        }

        /// <summary>
        /// 获取指定举报的审核历史
        /// </summary>
        private async Task<IEnumerable<object>> GetAuditHistoryForReport(int reportId)
        {
            var auditLogs = await _auditLogRepository.GetReportAuditHistoryAsync(reportId);

            var historyItems = new List<object>();

            // 添加举报创建记录（虽然不在audit log中，但作为历史的起点）
            var report = await _reportsRepository.GetReportWithDetailsAsync(reportId);
            if (report != null)
            {
                historyItems.Add(new
                {
                    timestamp = report.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    action = "提交举报",
                    moderator = report.Reporter?.Username ?? "系统用户",
                    comment = $"举报类型：{report.Type}，举报内容：{report.Description ?? "无具体说明"}"
                });
            }

            // 添加审核历史记录
            foreach (var log in auditLogs)
            {
                var actionText = GetActionDisplayText(log.ActionType);
                var moderatorName = log.Admin?.User?.Username ?? "系统管理员";

                historyItems.Add(new
                {
                    timestamp = log.LogTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    action = actionText,
                    moderator = moderatorName,
                    comment = log.LogDetail ?? "无详细说明"
                });
            }

            return historyItems.OrderBy(h => ((dynamic)h).timestamp);
        }

        /// <summary>
        /// 获取操作类型的显示文本
        /// </summary>
        private string GetActionDisplayText(string actionType)
        {
            return actionType switch
            {
                AuditLog.ActionTypes.HandleReport => "审核处理",
                _ => actionType
            };
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
                    pageIndex, pageSize, adminId, null, null, startDate, endDate);

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
            int? categoryId = null,
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
                    pageIndex, pageSize, targetAdminId, actionType, categoryId, startDate, endDate);

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

        #region 商品管理
        /// <summary>
        /// 获取管理员可管理的商品列表
        /// </summary>
        public async Task<(IEnumerable<Models.DTOs.Product.ProductListDto> Products, int TotalCount)> GetManagedProductsAsync(
            int adminId,
            AdminProductQueryDto queryDto)
        {
            using var performanceTracker = new PerformanceTracker(_serilogLogger, "GetManagedProducts", "AdminService")
                .AddContext("AdminId", adminId);

            try
            {
                var admin = await _adminRepository.GetAdminWithDetailsAsync(adminId);
                if (admin == null)
                {
                    return (Enumerable.Empty<Models.DTOs.Product.ProductListDto>(), 0);
                }

                // 超级管理员可以查看所有商品
                if (admin.Role == Models.Entities.Admin.Roles.Super)
                {
                    var result = await _productRepository.GetPagedProductsAsync(
                        queryDto.PageIndex,
                        queryDto.PageSize,
                        queryDto.CategoryId,
                        queryDto.Status,
                        queryDto.SearchKeyword,
                        null, // minPrice
                        null, // maxPrice
                        queryDto.UserId
                    );

                    var productDtos = result.Products.Select(p => new Models.DTOs.Product.ProductListDto
                    {
                        ProductId = p.ProductId,
                        Title = p.Title,
                        BasePrice = p.BasePrice,
                        Status = p.Status,
                        PublishTime = p.PublishTime,
                        ViewCount = p.ViewCount,
                        MainImageUrl = p.ProductImages?.FirstOrDefault()?.ImageUrl,
                        User = new Models.DTOs.Product.ProductUserDto
                        {
                            UserId = p.UserId,
                            Username = p.User?.Username ?? ""
                        },
                        Category = new Models.DTOs.Product.ProductCategoryDto
                        {
                            CategoryId = p.CategoryId,
                            Name = p.Category?.Name ?? "",
                            ParentId = p.Category?.ParentId
                        }
                    });

                    return (productDtos, result.TotalCount);
                }

                // 分类管理员处理逻辑
                if (admin.Role == Models.Entities.Admin.Roles.CategoryAdmin)
                {
                    var managedCategoryIds = await GetManagedCategoryIdsAsync(adminId);

                    // 如果管理员没有分配分类，返回空结果
                    if (!managedCategoryIds.Any())
                    {
                        return (Enumerable.Empty<Models.DTOs.Product.ProductListDto>(), 0);
                    }

                    // 如果指定了分类且不在管理范围内，返回空结果
                    if (queryDto.CategoryId.HasValue && !managedCategoryIds.Contains(queryDto.CategoryId.Value))
                    {
                        return (Enumerable.Empty<Models.DTOs.Product.ProductListDto>(), 0);
                    }

                    // 如果指定了分类且在管理范围内，使用指定分类查询
                    if (queryDto.CategoryId.HasValue)
                    {
                        var result = await _productRepository.GetPagedProductsAsync(
                            queryDto.PageIndex,
                            queryDto.PageSize,
                            queryDto.CategoryId.Value,
                            queryDto.Status,
                            queryDto.SearchKeyword,
                            null, // minPrice
                            null, // maxPrice
                            queryDto.UserId
                        );

                        var productDtos = result.Products.Select(p => new Models.DTOs.Product.ProductListDto
                        {
                            ProductId = p.ProductId,
                            Title = p.Title,
                            BasePrice = p.BasePrice,
                            Status = p.Status,
                            PublishTime = p.PublishTime,
                            ViewCount = p.ViewCount,
                            MainImageUrl = p.ProductImages?.FirstOrDefault()?.ImageUrl,
                            User = new Models.DTOs.Product.ProductUserDto
                            {
                                UserId = p.UserId,
                                Username = p.User?.Username ?? ""
                            },
                            Category = new Models.DTOs.Product.ProductCategoryDto
                            {
                                CategoryId = p.CategoryId,
                                Name = p.Category?.Name ?? "",
                                ParentId = p.Category?.ParentId
                            }
                        });

                        return (productDtos, result.TotalCount);
                    }

                    // 如果没有指定分类，需要查询所有可管理分类下的商品
                    // 由于GetPagedProductsAsync只支持单个分类，我们需要分别查询每个分类然后合并结果
                    var allProducts = new List<Models.Entities.Product>();

                    foreach (var categoryId in managedCategoryIds)
                    {
                        var categoryResult = await _productRepository.GetPagedProductsAsync(
                            1, // 先获取所有数据
                            int.MaxValue, // 获取该分类下的所有商品
                            categoryId,
                            queryDto.Status,
                            queryDto.SearchKeyword,
                            null, // minPrice
                            null, // maxPrice
                            queryDto.UserId
                        );

                        allProducts.AddRange(categoryResult.Products);
                    }

                    // 去重（如果有商品属于多个管理的分类）
                    var distinctProducts = allProducts
                        .GroupBy(p => p.ProductId)
                        .Select(g => g.First())
                        .OrderByDescending(p => p.PublishTime)
                        .ToList();

                    // 应用分页
                    var totalCount = distinctProducts.Count;
                    var pagedProducts = distinctProducts
                        .Skip((queryDto.PageIndex - 1) * queryDto.PageSize)
                        .Take(queryDto.PageSize)
                        .ToList();

                    var finalProductDtos = pagedProducts.Select(p => new Models.DTOs.Product.ProductListDto
                    {
                        ProductId = p.ProductId,
                        Title = p.Title,
                        BasePrice = p.BasePrice,
                        Status = p.Status,
                        PublishTime = p.PublishTime,
                        ViewCount = p.ViewCount,
                        MainImageUrl = p.ProductImages?.FirstOrDefault()?.ImageUrl,
                        User = new Models.DTOs.Product.ProductUserDto
                        {
                            UserId = p.UserId,
                            Username = p.User?.Username ?? ""
                        },
                        Category = new Models.DTOs.Product.ProductCategoryDto
                        {
                            CategoryId = p.CategoryId,
                            Name = p.Category?.Name ?? "",
                            ParentId = p.Category?.ParentId
                        }
                    });

                    return (finalProductDtos, totalCount);
                }

                // 其他角色（如举报管理员）默认没有商品管理权限
                return (Enumerable.Empty<Models.DTOs.Product.ProductListDto>(), 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取管理员可管理商品列表失败");
                return (Enumerable.Empty<Models.DTOs.Product.ProductListDto>(), 0);
            }
        }

        /// <summary>
        /// 获取商品详情（管理员视角）
        /// </summary>
        public async Task<AdminProductDetailDto?> GetProductDetailForAdminAsync(int adminId, int productId)
        {
            try
            {
                // 验证权限
                if (!await ValidateProductPermissionAsync(adminId, productId))
                {
                    return null;
                }

                var product = await _productRepository.GetProductWithOrdersAsync(productId);

                if (product == null)
                {
                    return null;
                }

                // 获取管理员操作日志 - 使用现有的方法
                var operationLogs = await _auditLogRepository.GetByActionTypeAsync("Product");

                return new AdminProductDetailDto
                {
                    ProductId = product.ProductId,
                    Title = product.Title,
                    Description = product.Description,
                    BasePrice = product.BasePrice,
                    Status = product.Status,
                    PublishTime = product.PublishTime,
                    ViewCount = product.ViewCount,
                    AutoRemoveTime = product.AutoRemoveTime,
                    User = new Models.DTOs.Product.ProductUserDto
                    {
                        UserId = product.User?.UserId ?? 0,
                        Username = product.User?.Username ?? ""
                    },
                    Category = new Models.DTOs.Product.ProductCategoryDto
                    {
                        CategoryId = product.Category?.CategoryId ?? 0,
                        Name = product.Category?.Name ?? "",
                        ParentId = product.Category?.ParentId
                    },
                    Images = product.ProductImages?.Select(img => new Models.DTOs.Product.ProductImageDto
                    {
                        ImageId = img.ImageId,
                        ImageUrl = img.ImageUrl
                    }).ToList() ?? new List<Models.DTOs.Product.ProductImageDto>(),
                    OperationLogs = operationLogs.Where(log => log.TargetId == productId).Select(log => new AdminProductOperationLogDto
                    {
                        OperationTime = log.LogTime,
                        AdminName = log.Admin?.User?.Username ?? "系统",
                        OperationType = log.ActionType,
                        OperationDetail = log.LogDetail ?? ""
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取商品详情失败，ProductId: {ProductId}", productId);
                return null;
            }
        }

        /// <summary>
        /// 更新商品信息（管理员操作）
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateProductAsAdminAsync(int adminId, int productId, AdminUpdateProductDto updateDto)
        {
            try
            {
                _logger.LogInformation("管理员更新商品请求 - AdminId: {AdminId}, ProductId: {ProductId}, UpdateDto: {@UpdateDto}",
                    adminId, productId, updateDto);

                // 验证数据
                if (updateDto.BasePrice.HasValue && updateDto.BasePrice.Value <= 0)
                {
                    _logger.LogWarning("价格验证失败 - ProductId: {ProductId}, BasePrice: {BasePrice}", productId, updateDto.BasePrice);
                    return (false, "商品价格必须大于0");
                }

                // Oracle NUMBER(10,2) 限制：总共10位数字，2位小数，所以整数部分最多8位，最大值99999999.99
                if (updateDto.BasePrice.HasValue && updateDto.BasePrice.Value > 99999999.99m)
                {
                    _logger.LogWarning("价格验证失败 - ProductId: {ProductId}, BasePrice: {BasePrice}", productId, updateDto.BasePrice);
                    return (false, "商品价格不能超过99999999.99元");
                }

                // 验证权限
                if (!await ValidateProductPermissionAsync(adminId, productId))
                {
                    return (false, "无权限操作此商品");
                }

                var product = await _productRepository.GetByPrimaryKeyAsync(productId);
                if (product == null)
                {
                    return (false, "商品不存在");
                }

                var changes = new List<string>();

                await _unitOfWork.BeginTransactionAsync();

                // 获取要更新的商品（重新获取避免并发问题）
                var productToUpdate = await _productRepository.GetByPrimaryKeyAsync(productId);
                if (productToUpdate == null)
                {
                    return (false, "商品不存在");
                }

                // 直接更新字段，避免多次Repository调用
                bool hasChanges = false;

                if (!string.IsNullOrEmpty(updateDto.Title) && updateDto.Title != productToUpdate.Title)
                {
                    changes.Add($"标题: {productToUpdate.Title} -> {updateDto.Title}");
                    productToUpdate.Title = updateDto.Title;
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(updateDto.Description) && updateDto.Description != productToUpdate.Description)
                {
                    changes.Add($"描述: 已更新");
                    productToUpdate.Description = updateDto.Description;
                    hasChanges = true;
                }

                if (updateDto.BasePrice.HasValue && updateDto.BasePrice.Value != productToUpdate.BasePrice)
                {
                    _logger.LogInformation("更新价格: 原值={OldPrice}, 新值={NewPrice}, 类型={PriceType}",
                        productToUpdate.BasePrice, updateDto.BasePrice.Value, updateDto.BasePrice.Value.GetType());

                    // 验证价格范围 - Oracle NUMBER(10,2) 限制
                    if (updateDto.BasePrice.Value <= 0)
                    {
                        return (false, "商品价格必须大于0");
                    }
                    if (updateDto.BasePrice.Value > 99999999.99m)
                    {
                        return (false, "商品价格超出允许范围（最大99999999.99元）");
                    }

                    changes.Add($"价格: {productToUpdate.BasePrice} -> {updateDto.BasePrice.Value}");
                    productToUpdate.BasePrice = updateDto.BasePrice.Value;
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(updateDto.Status) && updateDto.Status != productToUpdate.Status)
                {
                    changes.Add($"状态: {productToUpdate.Status} -> {updateDto.Status}");
                    productToUpdate.Status = updateDto.Status;
                    hasChanges = true;
                }

                if (updateDto.CategoryId.HasValue && updateDto.CategoryId.Value != productToUpdate.CategoryId)
                {
                    // 验证新分类是否在管理员权限范围内
                    var admin = await _adminRepository.GetAdminWithDetailsAsync(adminId);
                    if (admin?.Role == Models.Entities.Admin.Roles.CategoryAdmin)
                    {
                        var managedCategoryIds = await GetManagedCategoryIdsAsync(adminId);
                        if (!managedCategoryIds.Contains(updateDto.CategoryId.Value))
                        {
                            return (false, "无权限将商品移动到该分类");
                        }
                    }

                    changes.Add($"分类ID: {productToUpdate.CategoryId} -> {updateDto.CategoryId.Value}");
                    productToUpdate.CategoryId = updateDto.CategoryId.Value;
                    hasChanges = true;
                }

                // 只有当确实有变更时才更新
                if (hasChanges)
                {
                    _productRepository.Update(productToUpdate);
                }

                // 记录操作日志
                var logDetail = $"管理员更新商品信息: {string.Join(", ", changes)}";
                if (!string.IsNullOrEmpty(updateDto.AdminNote))
                {
                    logDetail += $". 备注: {updateDto.AdminNote}";
                }

                await LogAdminActionAsync(adminId, AuditLog.ActionTypes.UpdateProduct, productId, logDetail);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return (true, "商品信息更新成功");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "更新商品信息失败，ProductId: {ProductId}", productId);
                return (false, "更新商品信息失败");
            }
        }

        /// <summary>
        /// 删除商品（管理员操作）
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteProductAsAdminAsync(int adminId, int productId, string? reason = null)
        {
            try
            {
                // 验证权限
                if (!await ValidateProductPermissionAsync(adminId, productId))
                {
                    return (false, "无权限操作此商品");
                }

                var product = await _productRepository.GetByPrimaryKeyAsync(productId);
                if (product == null)
                {
                    return (false, "商品不存在");
                }

                await _unitOfWork.BeginTransactionAsync();

                // 使用仓储提供的删除方法
                await _productRepository.DeleteProductAsync(productId);

                // 记录操作日志
                var logDetail = $"管理员删除商品: {product.Title}";
                if (!string.IsNullOrEmpty(reason))
                {
                    logDetail += $". 原因: {reason}";
                }

                await LogAdminActionAsync(adminId, AuditLog.ActionTypes.DeleteProduct, productId, logDetail);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return (true, "商品删除成功");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "删除商品失败，ProductId: {ProductId}", productId);
                return (false, "删除商品失败");
            }
        }

        /// <summary>
        /// 批量操作商品
        /// </summary>
        public async Task<(bool Success, string Message, Dictionary<int, string> FailedProducts)> BatchOperateProductsAsync(
            int adminId,
            BatchProductOperationDto batchDto)
        {
            var failedProducts = new Dictionary<int, string>();

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                foreach (var productId in batchDto.ProductIds)
                {
                    try
                    {
                        // 验证权限
                        if (!await ValidateProductPermissionAsync(adminId, productId))
                        {
                            failedProducts[productId] = "无操作权限";
                            continue;
                        }

                        var product = await _productRepository.GetByPrimaryKeyAsync(productId);
                        if (product == null)
                        {
                            failedProducts[productId] = "商品不存在";
                            continue;
                        }

                        switch (batchDto.OperationType.ToLower())
                        {
                            case "delete":
                            case "下架":
                                await _productRepository.SetProductStatusAsync(productId, "已下架");
                                break;
                            case "审核通过":
                                await _productRepository.SetProductStatusAsync(productId, "在售");
                                break;
                            default:
                                failedProducts[productId] = "不支持的操作类型";
                                continue;
                        }

                        // 记录操作日志
                        var logDetail = $"批量操作: {batchDto.OperationType}, 商品: {product.Title}";
                        if (!string.IsNullOrEmpty(batchDto.Reason))
                        {
                            logDetail += $", 原因: {batchDto.Reason}";
                        }

                        await LogAdminActionAsync(adminId, AuditLog.ActionTypes.BatchOperation, productId, logDetail);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "批量操作商品失败，ProductId: {ProductId}", productId);
                        failedProducts[productId] = "操作失败";
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var successCount = batchDto.ProductIds.Count - failedProducts.Count;
                return (true, $"批量操作完成，成功 {successCount} 个，失败 {failedProducts.Count} 个", failedProducts);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "批量操作商品失败");
                return (false, "批量操作失败", failedProducts);
            }
        }

        /// <summary>
        /// 验证管理员对商品的管理权限
        /// </summary>
        public async Task<bool> ValidateProductPermissionAsync(int adminId, int productId)
        {
            try
            {
                var admin = await _adminRepository.GetAdminWithDetailsAsync(adminId);
                if (admin == null)
                {
                    return false;
                }

                // 超级管理员有所有权限
                if (admin.Role == Models.Entities.Admin.Roles.Super)
                {
                    return true;
                }

                // 分类管理员只能管理根种类标签为其分配标签的商品
                if (admin.Role == Models.Entities.Admin.Roles.CategoryAdmin && admin.AssignedCategory.HasValue)
                {
                    var product = await _productRepository.GetByPrimaryKeyAsync(productId);
                    if (product == null)
                    {
                        return false;
                    }

                    // 获取商品分类的根分类
                    var productRootCategoryId = await GetRootCategoryIdAsync(product.CategoryId);

                    // 检查根分类是否是管理员分配的分类
                    return productRootCategoryId == admin.AssignedCategory.Value;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证商品权限失败，AdminId: {AdminId}, ProductId: {ProductId}", adminId, productId);
                return false;
            }
        }

        /// <summary>
        /// 获取管理员可管理的分类ID列表
        /// </summary>
        public async Task<List<int>> GetManagedCategoryIdsAsync(int adminId)
        {
            try
            {
                var admin = await _adminRepository.GetAdminWithDetailsAsync(adminId);
                if (admin == null)
                {
                    return new List<int>();
                }

                // 超级管理员可以管理所有分类
                if (admin.Role == Models.Entities.Admin.Roles.Super)
                {
                    return await _categoryRepository.GetAllAsync()
                        .ContinueWith(task => task.Result.Select(c => c.CategoryId).ToList());
                }

                // 分类管理员可以管理所有根种类标签为其分配标签的分类及其子分类
                if (admin.Role == Models.Entities.Admin.Roles.CategoryAdmin && admin.AssignedCategory.HasValue)
                {
                    // 获取管理员分配的根分类下的所有分类
                    return await GetAllCategoriesUnderRootAsync(admin.AssignedCategory.Value);
                }

                return new List<int>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取管理员可管理分类失败，AdminId: {AdminId}", adminId);
                return new List<int>();
            }
        }

        /// <summary>
        /// 递归获取所有子分类
        /// </summary>
        private async Task<List<int>> GetAllSubCategoriesAsync(int parentCategoryId)
        {
            var result = new List<int>();
            var allCategories = await _categoryRepository.GetAllAsync();
            var directChildren = allCategories
                .Where(c => c.ParentId == parentCategoryId)
                .Select(c => c.CategoryId)
                .ToList();

            result.AddRange(directChildren);

            foreach (var childId in directChildren)
            {
                var subChildren = await GetAllSubCategoriesAsync(childId);
                result.AddRange(subChildren);
            }

            return result;
        }

        /// <summary>
        /// 获取分类的根分类ID
        /// </summary>
        private async Task<int> GetRootCategoryIdAsync(int categoryId)
        {
            var allCategories = await _categoryRepository.GetAllAsync();
            var categoryMap = allCategories.ToDictionary(c => c.CategoryId, c => c);

            var currentCategoryId = categoryId;
            while (categoryMap.ContainsKey(currentCategoryId))
            {
                var currentCategory = categoryMap[currentCategoryId];
                if (currentCategory.ParentId == null)
                {
                    // 找到根分类
                    return currentCategory.CategoryId;
                }
                currentCategoryId = currentCategory.ParentId.Value;
            }

            // 如果没有找到父分类，当前分类就是根分类
            return categoryId;
        }

        /// <summary>
        /// 获取属于指定根分类的所有分类ID
        /// </summary>
        private async Task<List<int>> GetAllCategoriesUnderRootAsync(int rootCategoryId)
        {
            var result = new List<int> { rootCategoryId }; // 包含根分类本身
            var subCategories = await GetAllSubCategoriesAsync(rootCategoryId);
            result.AddRange(subCategories);
            return result;
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
                var result = new Dictionary<string, object>();

                // 获取基本统计数据
                var totalProducts = await _productRepository.GetTotalProductsNumberAsync();
                result["totalProducts"] = totalProducts;
                result["totalGoods"] = totalProducts; // 兼容前端字段名

                var pendingReports = await _reportsRepository.GetPendingReportsCountAsync();
                result["pendingReports"] = pendingReports;

                var activeModerators = await _adminRepository.GetActiveAdminCountAsync();
                result["activeModerators"] = activeModerators;

                var todayStart = DateTime.Today;
                var todayEnd = todayStart.AddDays(1);
                var todayOperations = await _auditLogRepository.GetOperationCountByDateRangeAsync(todayStart, todayEnd);
                result["todayOperations"] = todayOperations;

                var totalUsers = await _userRepository.GetActiveUserCountAsync();
                result["totalUsers"] = totalUsers;

                // 获取管理员统计
                var adminStats = await _adminRepository.GetAdminStatisticsAsync();
                foreach (var stat in adminStats)
                {
                    result[$"admin_{stat.Key}"] = stat.Value;
                }

                // 获取最近30天的操作统计
                var auditStats = await _auditLogRepository.GetAuditStatisticsAsync(DateTime.Now.AddDays(-30));
                foreach (var stat in auditStats)
                {
                    result[$"recent_{stat.Key}"] = stat.Value;
                }

                // 获取月度统计数据
                var monthlyStats = await GetMonthlyStatsAsync();
                result["monthlyStats"] = monthlyStats;

                return result;
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "获取管理员统计信息异常");

                // 返回默认统计数据，避免前端显示错误
                return new Dictionary<string, object>
                {
                    ["totalProducts"] = 0,
                    ["totalGoods"] = 0,
                    ["pendingReports"] = 0,
                    ["activeModerators"] = 0,
                    ["todayOperations"] = 0,
                    ["totalUsers"] = 0,
                    ["monthlyStats"] = new List<object>()
                };
            }
        }

        /// <summary>
        /// 获取月度统计数据
        /// </summary>
        private async Task<List<object>> GetMonthlyStatsAsync()
        {
            try
            {
                var threeMonthsAgo = DateTime.Now.AddMonths(-3);
                var monthlyData = new List<object>();

                for (int i = 2; i >= 0; i--)
                {
                    var monthStart = DateTime.Now.AddMonths(-i).Date;
                    var monthEnd = monthStart.AddMonths(1);
                    var monthName = monthStart.ToString("yyyy-MM");

                    var userCount = await _userRepository.GetUserCountByDateRangeAsync(monthStart, monthEnd);
                    var productCount = await _productRepository.GetProductCountByDateRangeAsync(monthStart, monthEnd);
                    var operationCount = await _auditLogRepository.GetOperationCountByDateRangeAsync(monthStart, monthEnd);

                    monthlyData.Add(new
                    {
                        month = monthName,
                        userCount = userCount,
                        productCount = productCount,
                        operationCount = operationCount
                    });
                }

                return monthlyData;
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "获取月度统计数据异常");
                return new List<object>();
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
