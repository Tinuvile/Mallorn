using System.ComponentModel.DataAnnotations;

namespace CampusTrade.API.Options;

/// <summary>
/// JWT配置选项类
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// JWT签名密钥
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 32, ErrorMessage = "SecretKey长度必须在32-256字符之间")]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// 令牌发行者
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Issuer不能为空且长度不能超过100字符")]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// 令牌接收者
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Audience不能为空且长度不能超过100字符")]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access Token过期时间（分钟）
    /// </summary>
    [Range(1, 1440, ErrorMessage = "AccessToken过期时间必须在1-1440分钟之间")]
    public int AccessTokenExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Refresh Token过期时间（天）
    /// </summary>
    [Range(1, 365, ErrorMessage = "RefreshToken过期时间必须在1-365天之间")]
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// 是否要求HTTPS
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// 是否保存Token到AuthenticationProperties
    /// </summary>
    public bool SaveToken { get; set; } = true;

    /// <summary>
    /// 是否要求过期时间
    /// </summary>
    public bool RequireExpirationTime { get; set; } = true;

    /// <summary>
    /// 是否验证发行者
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// 是否验证接收者
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// 是否验证生命周期
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// 是否验证签名密钥
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;

    /// <summary>
    /// 时钟偏移容忍度（分钟）
    /// </summary>
    [Range(0, 60, ErrorMessage = "时钟偏移容忍度必须在0-60分钟之间")]
    public int ClockSkewMinutes { get; set; } = 5;

    /// <summary>
    /// 是否启用Refresh Token轮换
    /// </summary>
    public bool RefreshTokenRotation { get; set; } = true;

    /// <summary>
    /// 撤销时是否同时撤销子Token
    /// </summary>
    public bool RevokeDescendantRefreshTokens { get; set; } = true;

    /// <summary>
    /// 是否启用Token黑名单
    /// </summary>
    public bool EnableTokenBlacklist { get; set; } = true;

    /// <summary>
    /// 最大登录设备数量
    /// </summary>
    [Range(1, 10, ErrorMessage = "最大登录设备数量必须在1-10之间")]
    public int MaxActiveDevices { get; set; } = 3;

    // 计算属性，提供TimeSpan格式
    public TimeSpan AccessTokenExpiration => TimeSpan.FromMinutes(AccessTokenExpirationMinutes);
    public TimeSpan RefreshTokenExpiration => TimeSpan.FromDays(RefreshTokenExpirationDays);
    public TimeSpan ClockSkew => TimeSpan.FromMinutes(ClockSkewMinutes);

    /// <summary>
    /// 验证配置有效性
    /// </summary>
    /// <returns></returns>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(SecretKey) || SecretKey.Length < 32)
            return false;

        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
            return false;

        if (AccessTokenExpirationMinutes <= 0 || RefreshTokenExpirationDays <= 0)
            return false;

        return true;
    }

    /// <summary>
    /// 获取验证错误信息
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(SecretKey))
            errors.Add("SecretKey不能为空");
        else if (SecretKey.Length < 32)
            errors.Add("SecretKey长度至少32字符");

        if (string.IsNullOrWhiteSpace(Issuer))
            errors.Add("Issuer不能为空");

        if (string.IsNullOrWhiteSpace(Audience))
            errors.Add("Audience不能为空");

        if (AccessTokenExpirationMinutes <= 0)
            errors.Add("AccessToken过期时间必须大于0");

        if (RefreshTokenExpirationDays <= 0)
            errors.Add("RefreshToken过期时间必须大于0");

        return errors;
    }
}
