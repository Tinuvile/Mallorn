using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CampusTrade.API.Models.DTOs.Auth;

/// <summary>
/// Token刷新请求模型
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// 待刷新的RefreshToken
    /// </summary>
    [Required(ErrorMessage = "RefreshToken不能为空")]
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// 设备标识
    /// </summary>
    [StringLength(100, ErrorMessage = "设备标识长度不能超过100字符")]
    [JsonPropertyName("device_id")]
    public string? DeviceId { get; set; }

    /// <summary>
    /// 客户端IP地址（可选，服务器端也会自动获取）
    /// </summary>
    [StringLength(45, ErrorMessage = "IP地址长度不能超过45字符")]
    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理信息（可选，服务器端也会自动获取）
    /// </summary>
    [StringLength(500, ErrorMessage = "用户代理信息长度不能超过500字符")]
    [JsonPropertyName("user_agent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// 是否启用Token轮换（覆盖服务器配置）
    /// </summary>
    [JsonPropertyName("enable_rotation")]
    public bool? EnableRotation { get; set; }
}

/// <summary>
/// 用户登录请求
/// </summary>
public class LoginWithDeviceRequest
{
    /// <summary>
    /// 用户名或邮箱
    /// </summary>
    [Required(ErrorMessage = "用户名或邮箱不能为空")]
    [StringLength(100, ErrorMessage = "用户名或邮箱长度不能超过100字符")]
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, ErrorMessage = "密码长度不能超过100字符")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 设备标识
    /// </summary>
    [StringLength(100, ErrorMessage = "设备标识长度不能超过100字符")]
    [JsonPropertyName("device_id")]
    public string? DeviceId { get; set; }

    /// <summary>
    /// 设备名称
    /// </summary>
    [StringLength(200, ErrorMessage = "设备名称长度不能超过200字符")]
    [JsonPropertyName("device_name")]
    public string? DeviceName { get; set; }

    /// <summary>
    /// 是否记住登录（影响Token过期时间）
    /// </summary>
    [JsonPropertyName("remember_me")]
    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// Token撤销请求模型
/// </summary>
public class RevokeTokenRequest
{
    /// <summary>
    /// 待撤销的Token
    /// </summary>
    [Required(ErrorMessage = "Token不能为空")]
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 撤销原因
    /// </summary>
    [StringLength(200, ErrorMessage = "撤销原因长度不能超过200字符")]
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// 是否撤销所有Token（该用户的所有设备）
    /// </summary>
    [JsonPropertyName("revoke_all")]
    public bool RevokeAll { get; set; } = false;
}
