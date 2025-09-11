using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CampusTrade.API.Infrastructure.Extensions;
using CampusTrade.API.Infrastructure.Utils;
using CampusTrade.API.Models.DTOs.Auth;
using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Auth;
using CampusTrade.API.Services.Email;
using CampusTrade.API.Services.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace CampusTrade.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly EmailService _emailService;
    private readonly EmailVerificationService _verificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthController> _logger;
    private readonly NotifiService _notificationService;

    public AuthController(IAuthService authService, EmailService emailService, EmailVerificationService verificationService, IUnitOfWork unitOfWork, ILogger<AuthController> logger, NotifiService notificationService)
    {
        _authService = authService;
        _emailService = emailService;
        _verificationService = verificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _notificationService = notificationService;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="loginRequest">登录请求</param>
    /// <returns>完整的Token响应</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginWithDeviceRequest loginRequest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse.CreateError("请求参数验证失败", "VALIDATION_ERROR"));
        }

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
            var deviceType = DeviceDetector.GetDeviceType(userAgent); // 解析设备类型

            var tokenResponse = await _authService.LoginWithTokenAsync(loginRequest, ipAddress, userAgent);

            if (tokenResponse == null)
            {
                return Unauthorized(ApiResponse.CreateError("用户名或密码错误", "LOGIN_FAILED"));
            }

            // 获取用户上次登录信息（从User实体）
            var user = await _authService.GetUserByUsernameAsync(loginRequest.Username);
            var lastLoginIp = user?.LastLoginIp;
            var lastLoginTime = user?.LastLoginAt;

            // 检测异常登录风险
            var riskLevel = LoginLogs.RiskLevels.Low;
            var shouldSendNotification = false;
            string notificationLocation = "未知位置"; // 可以集成IP地理位置服务

            if (lastLoginIp != null && lastLoginIp != ipAddress)
            {
                // IP地址变更：中风险
                riskLevel = LoginLogs.RiskLevels.Medium;
                shouldSendNotification = true;
                _logger.LogWarning("异常登录检测：用户 {Username} 登录IP变更，旧IP: {LastIp}，新IP: {NewIp}",
                    loginRequest.Username, lastLoginIp, ipAddress);
            }
            if (lastLoginTime.HasValue && TimeHelper.Now - lastLoginTime.Value < TimeSpan.FromMinutes(5)
                && lastLoginIp != ipAddress)
            {
                // 短时间内不同IP登录：高风险
                riskLevel = LoginLogs.RiskLevels.High;
                shouldSendNotification = true;
                _logger.LogError("高危登录告警：用户 {Username} 5分钟内不同IP登录，旧IP: {LastIp}，新IP: {NewIp}",
                    loginRequest.Username, lastLoginIp, ipAddress);

                // 发送邮件/SMS通知用户
                if (riskLevel == LoginLogs.RiskLevels.High)
                {
                    var warningMsg = $"检测到异常登录：{TimeHelper.Now:yyyy-MM-dd HH:mm}，IP: {ipAddress}，设备: {deviceType}。如非本人操作，请及时修改密码。";
                    await _emailService.SendEmailAsync(
                        recipientEmail: user.Email,
                        subject: "校园交易平台 - 异常登录告警",
                        body: warningMsg
                    );
                }
            }

            // 发送登录风险通知
            if (shouldSendNotification && user != null)
            {
                try
                {
                    var notificationParams = new Dictionary<string, object>
                    {
                        ["loginTime"] = TimeHelper.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        ["ipAddress"] = ipAddress ?? "未知",
                        ["location"] = notificationLocation
                    };

                    await _notificationService.CreateNotificationAsync(
                        user.UserId,
                        25, // 账户安全提醒模板ID
                        notificationParams
                    );

                    _logger.LogInformation("登录风险通知已发送，用户ID: {UserId}，风险等级: {RiskLevel}，IP: {IpAddress}",
                        user.UserId, riskLevel, ipAddress);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "发送登录风险通知失败，用户ID: {UserId}，IP: {IpAddress}", user?.UserId, ipAddress);
                    // 注意：通知发送失败不应该影响登录结果，所以这里只记录日志
                }
            }

            // 创建登录日志
            var loginLog = new LoginLogs
            {
                UserId = tokenResponse.UserId,
                IpAddress = ipAddress,
                LogTime = TimeHelper.Now,
                DeviceType = deviceType,
                RiskLevel = riskLevel
            };
            await _unitOfWork.LoginLogs.AddAsync(loginLog);
            await _unitOfWork.SaveChangesAsync();

            return Ok(ApiResponse<TokenResponse>.CreateSuccess(tokenResponse, "登录成功"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("用户登录被拒绝，用户名: {Username}, 原因: {Reason}", loginRequest.Username, ex.Message);
            return Unauthorized(ApiResponse.CreateError(ex.Message, "LOGIN_DENIED"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户登录失败，用户名: {Username}", loginRequest.Username);
            return StatusCode(500, ApiResponse.CreateError("登录时发生内部错误", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    /// <param name="registerDto">注册信息</param>
    /// <returns>注册结果</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        _logger.LogInformation("收到注册请求，邮箱: {Email}, 学号: {StudentId}, 姓名: {Name}",
            registerDto?.Email ?? "null",
            registerDto?.StudentId ?? "null",
            registerDto?.Name ?? "null");

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) })
                .ToList();

            _logger.LogWarning("注册请求参数验证失败: {@Errors}", errors);
            return BadRequest(ApiResponse.CreateError("请求参数验证失败", "VALIDATION_ERROR"));
        }

        try
        {
            _logger.LogInformation("开始执行用户注册，学号: {StudentId}", registerDto.StudentId);
            var user = await _authService.RegisterAsync(registerDto);

            _logger.LogInformation("用户注册成功，用户ID: {UserId}, 学号: {StudentId}", user.UserId, user.StudentId);

            return Ok(ApiResponse<object>.CreateSuccess(new
            {
                userId = user.UserId,
                username = user.Username,
                email = user.Email,
                fullName = user.FullName,
                studentId = user.StudentId,
                creditScore = user.CreditScore
            }, "注册成功"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("用户注册失败，邮箱: {Email}, 原因: {Reason}", registerDto.Email, ex.Message);
            return BadRequest(ApiResponse.CreateError(ex.Message, "REGISTRATION_FAILED"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户注册失败，邮箱: {Email}", registerDto.Email);
            return StatusCode(500, ApiResponse.CreateError("注册时发生内部错误", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 验证学生身份
    /// </summary>
    /// <param name="validationDto">验证信息</param>
    /// <returns>验证结果</returns>
    [HttpPost("validate-student")]
    public async Task<IActionResult> ValidateStudent([FromBody] StudentValidationDto validationDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse.CreateError("请求参数验证失败", "VALIDATION_ERROR"));
        }

        try
        {
            var isValid = await _authService.ValidateStudentAsync(validationDto.StudentId, validationDto.Name);

            return Ok(ApiResponse<object>.CreateSuccess(new
            {
                isValid = isValid,
                studentId = validationDto.StudentId
            }, isValid ? "学生身份验证成功" : "学生身份验证失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证学生身份失败，学号: {StudentId}", validationDto.StudentId);
            return StatusCode(500, ApiResponse.CreateError("验证时发生内部错误", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <param name="username">用户名</param>
    /// <returns>用户信息</returns>
    [HttpGet("user/{username}")]
    public async Task<IActionResult> GetUser(string username)
    {
        try
        {
            var user = await _authService.GetUserByUsernameAsync(username);

            if (user == null)
            {
                return NotFound(ApiResponse.CreateError("用户不存在", "USER_NOT_FOUND"));
            }

            return Ok(ApiResponse<object>.CreateSuccess(new
            {
                userId = user.UserId,
                username = user.Username,
                email = user.Email,
                fullName = user.FullName,
                phone = user.Phone,
                studentId = user.StudentId,
                creditScore = user.CreditScore,
                createdAt = user.CreatedAt,
                student = user.Student != null ? new
                {
                    studentId = user.Student.StudentId,
                    name = user.Student.Name,
                    department = user.Student.Department
                } : null
            }, "获取用户信息成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询用户信息失败，用户名: {Username}", username);
            return StatusCode(500, ApiResponse.CreateError("查询用户时发生内部错误", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 退出登录
    /// </summary>
    /// <param name="logoutRequest">退出请求</param>
    /// <returns>退出结果</returns>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest logoutRequest)
    {
        try
        {
            var success = await _authService.LogoutAsync(logoutRequest.RefreshToken);

            if (success)
            {
                return Ok(ApiResponse.CreateSuccess("退出登录成功"));
            }
            else
            {
                return BadRequest(ApiResponse.CreateError("退出登录失败", "LOGOUT_FAILED"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户退出登录失败");
            return StatusCode(500, ApiResponse.CreateError("退出登录时发生内部错误", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 退出所有设备
    /// </summary>
    /// <returns>退出结果</returns>
    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll()
    {
        try
        {
            var userId = User.GetUserId();

            var revokedCount = await _authService.LogoutAllDevicesAsync(userId);

            return Ok(ApiResponse<object>.CreateSuccess(new
            {
                revokedTokens = revokedCount
            }, "已退出所有设备"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "退出所有设备失败");
            return StatusCode(500, ApiResponse.CreateError("退出所有设备时发生内部错误", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 发送邮箱验证码
    /// </summary>
    [HttpPost("send-verification-code")]
    public async Task<IActionResult> SendVerificationCode([FromBody] SendCodeDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse.CreateError("请求参数验证失败", "VALIDATION_ERROR"));
        }

        try
        {
            var result = await _verificationService.SendVerificationCodeAsync(dto.UserId, dto.Email);

            if (result.Success)
            {
                return Ok(ApiResponse<object>.CreateSuccess(new
                {
                    userId = dto.UserId,
                    email = dto.Email,
                    message = result.Message
                }, "验证码发送成功"));
            }
            else
            {
                return BadRequest(ApiResponse.CreateError(result.Message ?? "发送验证码失败", "SEND_CODE_FAILED"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送邮箱验证码失败，用户ID: {UserId}, 邮箱: {Email}", dto.UserId, dto.Email);
            return StatusCode(500, ApiResponse.CreateError("发送验证码时发生内部错误", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 验证邮箱验证码
    /// </summary>
    [HttpPost("verify-code")]
    public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse.CreateError("请求参数验证失败", "VALIDATION_ERROR"));
        }

        try
        {
            var result = await _verificationService.VerifyCodeAsync(dto.UserId, dto.Code);

            if (result.Valid)
            {
                return Ok(ApiResponse<object>.CreateSuccess(new
                {
                    userId = dto.UserId,
                    verified = true,
                    message = result.Message
                }, "验证码验证成功"));
            }
            else
            {
                return BadRequest(ApiResponse.CreateError(result.Message ?? "验证码验证失败", "VERIFY_CODE_FAILED"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证邮箱验证码失败，用户ID: {UserId}", dto.UserId);
            return StatusCode(500, ApiResponse.CreateError("验证验证码时发生内部错误", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 处理邮箱验证链接（用户点击链接后调用）
    /// </summary>
    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(ApiResponse.CreateError("验证令牌不能为空", "INVALID_TOKEN"));
        }

        try
        {
            var result = await _verificationService.VerifyTokenAsync(token);

            if (result.Valid)
            {
                // 对于邮箱验证链接，通常需要重定向到前端页面
                // 但也可以提供API响应供前端AJAX调用使用
                if (Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    // API调用返回JSON
                    return Ok(ApiResponse<object>.CreateSuccess(new
                    {
                        verified = true,
                        message = result.Message
                    }, "邮箱验证成功"));
                }
                else
                {
                    // 浏览器访问重定向到前端成功页面
                    return Redirect("https://your-domain.com/email-verified");
                }
            }
            else
            {
                // 验证失败
                if (Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    return BadRequest(ApiResponse.CreateError(result.Message ?? "邮箱验证失败", "EMAIL_VERIFY_FAILED"));
                }
                else
                {
                    // 浏览器访问重定向到前端失败页面
                    return Redirect("https://your-domain.com/email-verify-failed");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理邮箱验证链接失败，令牌: {Token}", token);

            if (Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return StatusCode(500, ApiResponse.CreateError("邮箱验证时发生内部错误", "INTERNAL_ERROR"));
            }
            else
            {
                return Redirect("https://your-domain.com/email-verify-failed");
            }
        }
    }

    /// <summary>
    /// 获取当前用户详细信息（包含信用分、虚拟账户等）
    /// </summary>
    /// <returns>用户详细信息</returns>
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetUserProfile()
    {
        try
        {
            var userId = User.GetUserId();
            var user = await _unitOfWork.Users.GetUserWithDetailsAsync(userId);

            if (user == null)
            {
                return NotFound(ApiResponse.CreateError("用户不存在", "USER_NOT_FOUND"));
            }

            // 获取虚拟账户信息
            var virtualAccount = await _unitOfWork.VirtualAccounts.GetByUserIdAsync(userId);

            return Ok(ApiResponse<object>.CreateSuccess(new
            {
                userId = user.UserId,
                username = user.Username,
                email = user.Email,
                fullName = user.FullName,
                phone = user.Phone,
                studentId = user.StudentId,
                creditScore = user.CreditScore,
                emailVerified = user.EmailVerified == 1,
                isActive = user.IsActive == 1,
                createdAt = user.CreatedAt,
                lastLoginAt = user.LastLoginAt,
                lastLoginIp = user.LastLoginIp,
                loginCount = user.LoginCount,
                student = user.Student != null ? new
                {
                    studentId = user.Student.StudentId,
                    name = user.Student.Name,
                    department = user.Student.Department
                } : null,
                virtualAccount = virtualAccount != null ? new
                {
                    accountId = virtualAccount.AccountId,
                    balance = virtualAccount.Balance,
                    createdAt = virtualAccount.CreatedAt
                } : null
            }, "获取用户信息成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户详细信息失败，用户ID: {UserId}", User.GetUserId());
            return StatusCode(500, ApiResponse.CreateError("获取用户信息时发生内部错误", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 更新用户基本信息
    /// </summary>
    /// <param name="updateDto">更新信息</param>
    /// <returns>更新结果</returns>
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserProfileDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse.CreateError("请求参数验证失败", "VALIDATION_ERROR"));
        }

        try
        {
            var userId = User.GetUserId();
            var user = await _unitOfWork.Users.GetByPrimaryKeyAsync(userId);

            if (user == null)
            {
                return NotFound(ApiResponse.CreateError("用户不存在", "USER_NOT_FOUND"));
            }

            // 检查用户名是否已被使用（如果要更改用户名）
            if (!string.IsNullOrEmpty(updateDto.Username) && updateDto.Username != user.Username)
            {
                var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Username == updateDto.Username);
                if (existingUser != null)
                {
                    return BadRequest(ApiResponse.CreateError("该用户名已被使用", "USERNAME_EXISTS"));
                }
                user.Username = updateDto.Username;
            }

            // 更新其他字段
            if (!string.IsNullOrEmpty(updateDto.FullName))
                user.FullName = updateDto.FullName;

            if (!string.IsNullOrEmpty(updateDto.Phone))
                user.Phone = updateDto.Phone;

            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return Ok(ApiResponse.CreateSuccess("用户信息更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户信息失败，用户ID: {UserId}", User.GetUserId());
            return StatusCode(500, ApiResponse.CreateError("更新用户信息时发生内部错误", "INTERNAL_ERROR"));
        }
    }



    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="changePasswordDto">密码修改信息</param>
    /// <returns>修改结果</returns>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse.CreateError("请求参数验证失败", "VALIDATION_ERROR"));
        }

        try
        {
            var userId = User.GetUserId();
            var user = await _unitOfWork.Users.GetByPrimaryKeyAsync(userId);

            if (user == null)
            {
                return NotFound(ApiResponse.CreateError("用户不存在", "USER_NOT_FOUND"));
            }

            // 验证当前密码
            if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
            {
                return BadRequest(ApiResponse.CreateError("当前密码不正确", "INVALID_CURRENT_PASSWORD"));
            }

            // 更新密码
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
            user.PasswordChangedAt = DateTime.UtcNow;
            user.SecurityStamp = Guid.NewGuid().ToString(); // 更新安全戳，使旧Token失效

            await _unitOfWork.SaveChangesAsync();

            // 发送密码修改成功通知
            try
            {
                var notificationParams = new Dictionary<string, object>
                {
                    ["changeTime"] = TimeHelper.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                await _notificationService.CreateNotificationAsync(
                    userId,
                    26, // 密码修改成功模板ID
                    notificationParams
                );

                _logger.LogInformation("密码修改成功通知已发送，用户ID: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送密码修改成功通知失败，用户ID: {UserId}", userId);
                // 注意：通知发送失败不应该影响密码修改结果，所以这里只记录日志
            }

            return Ok(ApiResponse.CreateSuccess("密码修改成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "修改密码失败，用户ID: {UserId}", User.GetUserId());
            return StatusCode(500, ApiResponse.CreateError("修改密码时发生内部错误", "INTERNAL_ERROR"));
        }
    }
}


/// <summary>
/// 退出登录请求DTO
/// </summary>
public class LogoutRequest
{
    /// <summary>
    /// 刷新令牌
    /// </summary>
    [Required(ErrorMessage = "刷新令牌不能为空")]
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// 学生身份验证DTO
/// </summary>
public class StudentValidationDto
{
    /// <summary>
    /// 学号
    /// </summary>
    [Required(ErrorMessage = "学号不能为空")]
    [JsonPropertyName("student_id")]
    public string StudentId { get; set; } = string.Empty;

    /// <summary>
    /// 姓名
    /// </summary>
    [Required(ErrorMessage = "姓名不能为空")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 发送验证码DTO
/// </summary>
public class SendCodeDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
}


/// <summary>
/// 确认验证码DTO
/// </summary>
public class VerifyCodeDto
{
    public int UserId { get; set; }
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// 更新用户信息DTO
/// </summary>
public class UpdateUserProfileDto
{
    /// <summary>
    /// 用户名
    /// </summary>
    [StringLength(50, ErrorMessage = "用户名长度不能超过50字符")]
    public string? Username { get; set; }

    /// <summary>
    /// 完整姓名
    /// </summary>
    [StringLength(100, ErrorMessage = "姓名长度不能超过100字符")]
    public string? FullName { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    [Phone(ErrorMessage = "手机号格式不正确")]
    [StringLength(20, ErrorMessage = "手机号长度不能超过20字符")]
    public string? Phone { get; set; }
}

/// <summary>
/// 修改密码DTO
/// </summary>
public class ChangePasswordDto
{
    /// <summary>
    /// 当前密码
    /// </summary>
    [Required(ErrorMessage = "当前密码不能为空")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新密码
    /// </summary>
    [Required(ErrorMessage = "新密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6-100字符之间")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// 确认新密码
    /// </summary>
    [Required(ErrorMessage = "确认密码不能为空")]
    [Compare("NewPassword", ErrorMessage = "两次输入的密码不一致")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
