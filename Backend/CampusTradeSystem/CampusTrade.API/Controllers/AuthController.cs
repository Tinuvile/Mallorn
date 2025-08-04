using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CampusTrade.API.Models.DTOs.Auth;
using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusTrade.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
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

            var tokenResponse = await _authService.LoginWithTokenAsync(loginRequest, ipAddress, userAgent);

            if (tokenResponse == null)
            {
                return Unauthorized(ApiResponse.CreateError("用户名或密码错误", "LOGIN_FAILED"));
            }

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
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse.CreateError("无效的用户身份", "INVALID_USER"));
            }

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
