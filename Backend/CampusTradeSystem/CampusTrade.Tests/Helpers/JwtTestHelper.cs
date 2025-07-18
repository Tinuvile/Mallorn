using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CampusTrade.API.Models.DTOs.Auth;
using CampusTrade.API.Models.Entities;
using Microsoft.IdentityModel.Tokens;

namespace CampusTrade.Tests.Helpers;

/// <summary>
/// JWT测试辅助类
/// </summary>
public static class JwtTestHelper
{
    private const string TestSecretKey = "YourSecretKeyForCampusTradingPlatformProduction2025!MustBe32CharactersLong";
    private const string TestIssuer = "CampusTrade.API";
    private const string TestAudience = "CampusTrade.Client";

    private static readonly byte[] Key = Encoding.UTF8.GetBytes(TestSecretKey);
    private static readonly SymmetricSecurityKey SecurityKey = new(Key);
    private static readonly SigningCredentials SigningCredentials = new(SecurityKey, SecurityAlgorithms.HmacSha256);

    /// <summary>
    /// 生成测试用的有效JWT Token
    /// </summary>
    public static string GenerateTestJwtToken(User user, int expirationMinutes = 15)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim("username", user.Username),
            new Claim("student_id", user.StudentId ?? ""),
            new Claim("full_name", user.FullName ?? ""),
            new Claim("credit_score", user.CreditScore.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: SigningCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// 生成过期的JWT Token
    /// </summary>
    public static string GenerateExpiredJwtToken(User user)
    {
        return GenerateTestJwtToken(user, -10); // 10分钟前过期
    }

    /// <summary>
    /// 生成签名无效的JWT Token
    /// </summary>
    public static string GenerateInvalidJwtToken(User user)
    {
        var invalidKey = Encoding.UTF8.GetBytes("InvalidKey12345678901234567890123456789012");
        var invalidSecurityKey = new SymmetricSecurityKey(invalidKey);
        var invalidSigningCredentials = new SigningCredentials(invalidSecurityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: invalidSigningCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// 从JWT Token中提取用户ID
    /// </summary>
    public static int ExtractUserId(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);
        var userIdClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub);
        return int.Parse(userIdClaim?.Value ?? "0");
    }

    /// <summary>
    /// 从JWT Token中提取JTI (JWT ID)
    /// </summary>
    public static string? ExtractJti(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);
        return jsonToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
    }

    /// <summary>
    /// 从JWT Token中提取用户名
    /// </summary>
    public static string? ExtractUsername(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);

        // 尝试多种可能的name claim类型
        var nameClaimTypes = new[]
        {
            "unique_name",  // JWT序列化后的标准名称
            ClaimTypes.Name,
            "name",
            "username",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
        };

        foreach (var claimType in nameClaimTypes)
        {
            var claim = jsonToken.Claims.FirstOrDefault(x => x.Type == claimType);
            if (claim != null)
            {
                return claim.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// 验证JWT Token的基本格式
    /// </summary>
    public static bool IsValidJwtFormat(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.CanReadToken(token);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 验证JWT Token
    /// </summary>
    public static TokenValidationResult ValidateJwtToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = SecurityKey,
                ValidateIssuer = true,
                ValidIssuer = TestIssuer,
                ValidateAudience = true,
                ValidAudience = TestAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return TokenValidationResult.CreateValidResult(validatedToken);
        }
        catch (Exception ex)
        {
            return TokenValidationResult.CreateInvalidResult(ex.Message);
        }
    }

    /// <summary>
    /// 创建测试TokenResponse
    /// </summary>
    public static TokenResponse CreateTestTokenResponse(User user, string? deviceId = null)
    {
        var accessToken = GenerateTestJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = 900, // 15分钟
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            StudentId = user.StudentId ?? "",
            CreditScore = user.CreditScore,
            DeviceId = deviceId,
            UserStatus = user.IsActive == 1 ? "Active" : "Inactive",
            EmailVerified = true,
            TwoFactorEnabled = false,
            RefreshExpiresAt = DateTime.UtcNow.AddDays(7)
        };
    }

    /// <summary>
    /// 生成随机RefreshToken
    /// </summary>
    public static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    /// <summary>
    /// 创建测试Claims Principal
    /// </summary>
    public static ClaimsPrincipal CreateTestClaimsPrincipal(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("username", user.Username),
            new Claim("student_id", user.StudentId ?? "")
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }
}

/// <summary>
/// Token验证结果
/// </summary>
public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public string? Error { get; set; }
    public SecurityToken? ValidatedToken { get; set; }

    public static TokenValidationResult CreateValidResult(SecurityToken token)
    {
        return new TokenValidationResult { IsValid = true, ValidatedToken = token };
    }

    public static TokenValidationResult CreateInvalidResult(string error)
    {
        return new TokenValidationResult { IsValid = false, Error = error };
    }
}
