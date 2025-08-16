using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CampusTrade.API.Models.DTOs.Auth;

/// <summary>
/// 用户注册请求DTO
/// </summary>
public class RegisterDto
{
    /// <summary>
    /// 学号
    /// </summary>
    [Required(ErrorMessage = "学号不能为空")]
    [StringLength(20, ErrorMessage = "学号长度不能超过20字符")]
    [JsonPropertyName("student_id")]
    public string StudentId { get; set; } = string.Empty;

    /// <summary>
    /// 姓名
    /// </summary>
    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(100, ErrorMessage = "姓名长度不能超过100字符")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱地址
    /// </summary>
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [StringLength(100, ErrorMessage = "邮箱长度不能超过100字符")]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6-100字符之间")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 确认密码
    /// </summary>
    [Required(ErrorMessage = "确认密码不能为空")]
    [Compare("Password", ErrorMessage = "两次输入的密码不一致")]
    [JsonPropertyName("confirm_password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// 用户名（可选）
    /// </summary>
    [StringLength(50, ErrorMessage = "用户名长度不能超过50字符")]
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// 手机号（可选）
    /// </summary>
    [Phone(ErrorMessage = "手机号格式不正确")]
    [StringLength(20, ErrorMessage = "手机号长度不能超过20字符")]
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}
