using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CampusTrade.API.Models.DTOs.Auth;
using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusTrade.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TokenController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly ILogger<TokenController> _logger;

    public TokenController(ITokenService tokenService, ILogger<TokenController> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    /// <param name="refreshRequest">刷新请求</param>
    /// <returns>新的Token响应</returns>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest refreshRequest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse.CreateError("请求参数验证失败", "VALIDATION_ERROR"));
        }

        try
        {
            // 从请求中获取客户端信息
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            // 设置客户端信息
            refreshRequest.IpAddress ??= ipAddress;
            refreshRequest.UserAgent ??= userAgent;

            var tokenResponse = await _tokenService.RefreshTokenAsync(refreshRequest);

            return Ok(ApiResponse<TokenResponse>.CreateSuccess(tokenResponse, "Token刷新成功"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Token刷新被拒绝, 原因: {Reason}", ex.Message);
            return Unauthorized(ApiResponse.CreateError("刷新失败 - " + ex.Message, "REFRESH_DENIED"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token刷新失败");
            return StatusCode(500, ApiResponse.CreateError("Token刷新时发生内部错误", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 验证访问令牌
    /// </summary>
    /// <param name="validateRequest">验证请求</param>
    /// <returns>验证结果</returns>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest validateRequest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse.CreateError("请求参数验证失败", "VALIDATION_ERROR"));
        }

        try
        {
            var validationResult = await _tokenService.ValidateAccessTokenAsync(validateRequest.AccessToken);

            return Ok(ApiResponse<TokenValidationResponse>.CreateSuccess(validationResult, "Token验证完成"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token验证失败");
            return StatusCode(500, ApiResponse.CreateError("Token验证时发生内部错误", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 撤销刷新令牌
    /// </summary>
    /// <param name="revokeRequest">撤销请求</param>
    /// <returns>撤销结果</returns>
    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest revokeRequest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse.CreateError("请求参数验证失败", "VALIDATION_ERROR"));
        }

        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse.CreateError("无效的用户身份", "INVALID_USER"));
            }

            bool success;
            int revokedCount = 0;

            if (revokeRequest.RevokeAll)
            {
                // 撤销用户的所有Token
                revokedCount = await _tokenService.RevokeAllUserTokensAsync(userId, revokeRequest.Reason, userId);
                success = revokedCount > 0;
            }
            else
            {
                // 撤销单个Token
                success = await _tokenService.RevokeRefreshTokenAsync(revokeRequest.Token, revokeRequest.Reason, userId);
                revokedCount = success ? 1 : 0;
            }

            if (success)
            {
                return Ok(ApiResponse<object>.CreateSuccess(new
                {
                    revokedTokens = revokedCount,
                    revokeAll = revokeRequest.RevokeAll
                }, revokeRequest.RevokeAll ? "已撤销所有Token" : "Token撤销成功"));
            }
            else
            {
                return BadRequest(ApiResponse.CreateError("Token撤销失败", "REVOKE_FAILED"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token撤销失败");
            return StatusCode(500, ApiResponse.CreateError("Token撤销时发生内部错误", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 获取用户的活跃令牌列表
    /// </summary>
    /// <returns>活跃令牌列表</returns>
    [HttpGet("active")]
    [Authorize]
    public async Task<IActionResult> GetActiveTokens()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse.CreateError("无效的用户身份", "INVALID_USER"));
            }

            var activeTokens = await _tokenService.GetActiveRefreshTokensAsync(userId);

            var tokenList = activeTokens.Select(token => new
            {
                deviceId = token.DeviceId,
                ipAddress = token.IpAddress,
                userAgent = token.UserAgent,
                createdAt = token.CreatedAt,
                lastUsedAt = token.LastUsedAt,
                expiryDate = token.ExpiryDate,
                isCurrent = false // 这里可以添加逻辑判断是否为当前设备
            }).ToList();

            return Ok(ApiResponse<object>.CreateSuccess(new
            {
                activeTokens = tokenList.Count,
                tokens = tokenList
            }, "获取活跃令牌成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活跃令牌失败");
            return StatusCode(500, ApiResponse.CreateError("获取活跃令牌时发生内部错误", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 清理过期的令牌（管理员功能）
    /// </summary>
    /// <returns>清理结果</returns>
    [HttpPost("cleanup")]
    [Authorize] // 这里可以添加管理员角色验证
    public async Task<IActionResult> CleanupExpiredTokens()
    {
        try
        {
            var cleanedCount = await _tokenService.CleanupExpiredTokensAsync();

            return Ok(ApiResponse<object>.CreateSuccess(new
            {
                cleanedTokens = cleanedCount
            }, $"已清理 {cleanedCount} 个过期令牌"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理过期令牌失败");
            return StatusCode(500, ApiResponse.CreateError("清理过期令牌时发生内部错误", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 检查Token是否在黑名单中
    /// </summary>
    /// <param name="checkRequest">检查请求</param>
    /// <returns>检查结果</returns>
    [HttpPost("check-blacklist")]
    public async Task<IActionResult> CheckBlacklist([FromBody] CheckBlacklistRequest checkRequest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse.CreateError("请求参数验证失败", "VALIDATION_ERROR"));
        }

        try
        {
            var isBlacklisted = await _tokenService.IsTokenBlacklistedAsync(checkRequest.Jti);

            return Ok(ApiResponse<object>.CreateSuccess(new
            {
                jti = checkRequest.Jti,
                isBlacklisted = isBlacklisted
            }, "黑名单检查完成"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查Token黑名单失败");
            return StatusCode(500, ApiResponse.CreateError("检查黑名单时发生内部错误", "INTERNAL_ERROR"));
        }
    }
}

/// <summary>
/// Token验证请求DTO
/// </summary>
public class ValidateTokenRequest
{
    /// <summary>
    /// 访问令牌
    /// </summary>
    [Required(ErrorMessage = "访问令牌不能为空")]
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
}

/// <summary>
/// 黑名单检查请求DTO
/// </summary>
public class CheckBlacklistRequest
{
    /// <summary>
    /// JWT ID
    /// </summary>
    [Required(ErrorMessage = "JWT ID不能为空")]
    [JsonPropertyName("jti")]
    public string Jti { get; set; } = string.Empty;
}
