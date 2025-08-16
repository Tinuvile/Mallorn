using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CampusTrade.API.Options;
using Microsoft.IdentityModel.Tokens;

namespace CampusTrade.API.Infrastructure.Utils.Security;

/// <summary>
/// Token操作工具类
/// </summary>
public static class TokenHelper
{
    /// <summary>
    /// 从JWT Token中提取用户ID
    /// </summary>
    /// <param name="token">JWT Token</param>
    /// <returns>用户ID，如果提取失败返回null</returns>
    public static int? GetUserIdFromToken(string token)
    {
        var claims = GetClaimsFromToken(token);
        var userIdClaim = claims?.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// 从JWT Token中提取所有Claims
    /// </summary>
    /// <param name="token">JWT Token</param>
    /// <returns>Claims集合</returns>
    public static IEnumerable<Claim>? GetClaimsFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(token))
                return null;

            var jsonToken = tokenHandler.ReadJwtToken(token);
            return jsonToken.Claims;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 从Token中获取过期时间
    /// </summary>
    /// <param name="token">JWT Token</param>
    /// <returns>过期时间</returns>
    public static DateTime? GetExpirationFromToken(string token)
    {
        var claims = GetClaimsFromToken(token);
        var expClaim = claims?.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp)?.Value;

        if (long.TryParse(expClaim, out var exp))
        {
            return DateTimeOffset.FromUnixTimeSeconds(exp).DateTime;
        }

        return null;
    }

    /// <summary>
    /// 从Token中获取JTI（JWT ID）
    /// </summary>
    /// <param name="token">JWT Token</param>
    /// <returns>JWT ID</returns>
    public static string? GetJtiFromToken(string token)
    {
        var claims = GetClaimsFromToken(token);
        return claims?.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
    }

    /// <summary>
    /// 创建SecurityKey
    /// </summary>
    /// <param name="secretKey">密钥</param>
    /// <returns>SecurityKey</returns>
    public static SecurityKey CreateSecurityKey(string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        return new SymmetricSecurityKey(keyBytes);
    }

    /// <summary>
    /// 创建SigningCredentials
    /// </summary>
    /// <param name="secretKey">密钥</param>
    /// <returns>SigningCredentials</returns>
    public static SigningCredentials CreateSigningCredentials(string secretKey)
    {
        var securityKey = CreateSecurityKey(secretKey);
        return new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    }

    /// <summary>
    /// 创建TokenValidationParameters
    /// </summary>
    /// <param name="jwtOptions">JWT配置</param>
    /// <returns>TokenValidationParameters</returns>
    public static TokenValidationParameters CreateTokenValidationParameters(JwtOptions jwtOptions)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = jwtOptions.ValidateIssuer,
            ValidateAudience = jwtOptions.ValidateAudience,
            ValidateLifetime = jwtOptions.ValidateLifetime,
            ValidateIssuerSigningKey = jwtOptions.ValidateIssuerSigningKey,
            RequireExpirationTime = jwtOptions.RequireExpirationTime,

            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = CreateSecurityKey(jwtOptions.SecretKey),

            ClockSkew = jwtOptions.ClockSkew,

            // 设置名称和角色声明类型
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    }

    /// <summary>
    /// 验证Token签名和基本格式
    /// </summary>
    /// <param name="token">JWT Token</param>
    /// <param name="jwtOptions">JWT配置</param>
    /// <returns>验证结果</returns>
    public static (bool IsValid, ClaimsPrincipal? Principal, string? Error) ValidateToken(string token, JwtOptions jwtOptions)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = CreateTokenValidationParameters(jwtOptions);

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            return (true, principal, null);
        }
        catch (SecurityTokenExpiredException)
        {
            return (false, null, "Token已过期");
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return (false, null, "Token签名无效");
        }
        catch (SecurityTokenValidationException ex)
        {
            return (false, null, $"Token验证失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, null, $"Token验证异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 从ClaimsPrincipal中提取用户信息
    /// </summary>
    /// <param name="principal">ClaimsPrincipal</param>
    /// <returns>用户信息</returns>
    public static (int UserId, string Username, string Email, string StudentId) ExtractUserInfo(ClaimsPrincipal principal)
    {
        var userIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = principal.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        var email = principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        var studentId = principal.FindFirst("student_id")?.Value ?? string.Empty;

        int.TryParse(userIdStr, out var userId);

        return (userId, username, email, studentId);
    }

    /// <summary>
    /// 创建自定义Claims列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="username">用户名</param>
    /// <param name="email">邮箱</param>
    /// <param name="studentId">学号</param>
    /// <param name="additionalClaims">额外的Claims</param>
    /// <returns>Claims列表</returns>
    public static List<Claim> CreateUserClaims(int userId, string username, string email, string studentId, IEnumerable<Claim>? additionalClaims = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username ?? email), // 如果用户名为空，使用邮箱作为用户名
            new(ClaimTypes.Email, email ?? string.Empty),
            new("student_id", studentId ?? string.Empty),
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, SecurityHelper.GenerateJwtId()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (additionalClaims != null)
        {
            claims.AddRange(additionalClaims);
        }

        return claims;
    }

    /// <summary>
    /// 撤销刷新令牌
    /// </summary>
    /// <param name="token">刷新令牌实体</param>
    /// <param name="reason">撤销原因</param>
    /// <param name="revokedBy">撤销者ID</param>
    public static void RevokeRefreshToken(Models.Entities.RefreshToken token, string? reason, int? revokedBy)
    {
        token.IsRevoked = 1;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokeReason = reason;
        token.RevokedBy = revokedBy;
    }

    /// <summary>
    /// 判断刷新令牌是否有效
    /// </summary>
    /// <param name="token">刷新令牌实体</param>
    /// <returns>是否有效</returns>
    public static bool IsRefreshTokenValid(Models.Entities.RefreshToken token)
    {
        return token.IsRevoked == 0 && token.ExpiryDate > DateTime.UtcNow;
    }

    /// <summary>
    /// 更新刷新令牌的最后使用时间
    /// </summary>
    /// <param name="token">刷新令牌实体</param>
    public static void UpdateRefreshTokenLastUsed(Models.Entities.RefreshToken token)
    {
        token.LastUsedAt = DateTime.UtcNow;
    }
}
