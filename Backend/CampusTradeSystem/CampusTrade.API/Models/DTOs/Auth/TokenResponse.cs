using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CampusTrade.API.Models.DTOs.Auth;

/// <summary>
/// Token响应模型
/// </summary>
public class TokenResponse
{
    /// <summary>
    /// 访问令牌
    /// </summary>
    [Required]
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// 刷新令牌
    /// </summary>
    [Required]
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// 令牌类型
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// 过期秒数
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// 具体过期时间
    /// </summary>
    [JsonPropertyName("expires_at")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 学号
    /// </summary>
    [JsonPropertyName("student_id")]
    public string StudentId { get; set; } = string.Empty;

    /// <summary>
    /// 信用分
    /// </summary>
    [JsonPropertyName("credit_score")]
    public decimal CreditScore { get; set; }

    /// <summary>
    /// 设备标识
    /// </summary>
    [JsonPropertyName("device_id")]
    public string? DeviceId { get; set; }

    /// <summary>
    /// 用户状态
    /// </summary>
    [JsonPropertyName("user_status")]
    public string UserStatus { get; set; } = "Active";

    /// <summary>
    /// 是否需要验证邮箱
    /// </summary>
    [JsonPropertyName("email_verified")]
    public bool EmailVerified { get; set; } = false;

    /// <summary>
    /// 是否启用双因子认证
    /// </summary>
    [JsonPropertyName("two_factor_enabled")]
    public bool TwoFactorEnabled { get; set; } = false;

    /// <summary>
    /// 刷新令牌过期时间
    /// </summary>
    [JsonPropertyName("refresh_expires_at")]
    public DateTime RefreshExpiresAt { get; set; }
}

/// <summary>
/// Token验证响应模型
/// </summary>
public class TokenValidationResponse
{
    /// <summary>
    /// 是否有效
    /// </summary>
    [JsonPropertyName("is_valid")]
    public bool IsValid { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    [JsonPropertyName("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 权限列表
    /// </summary>
    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; } = new();
}
