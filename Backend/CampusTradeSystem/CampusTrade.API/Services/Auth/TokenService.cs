using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CampusTrade.API.Data;
using CampusTrade.API.Models.DTOs.Auth;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Options;
using CampusTrade.API.Utils.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CampusTrade.API.Services.Auth;

/// <summary>
/// Token服务实现
/// </summary>
public class TokenService : ITokenService
{
    private readonly CampusTradeDbContext _context;
    private readonly JwtOptions _jwtOptions;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        CampusTradeDbContext context,
        IOptions<JwtOptions> jwtOptions,
        IMemoryCache cache,
        ILogger<TokenService> logger)
    {
        _context = context;
        _jwtOptions = jwtOptions.Value;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// 生成访问令牌
    /// </summary>
    public async Task<string> GenerateAccessTokenAsync(User user, IEnumerable<Claim>? additionalClaims = null)
    {
        try
        {
            var claims = TokenHelper.CreateUserClaims(
                user.UserId,
                user.Username ?? user.Email,
                user.Email,
                user.StudentId,
                additionalClaims);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = TokenHelper.CreateSecurityKey(_jwtOptions.SecretKey);
            var credentials = TokenHelper.CreateSigningCredentials(_jwtOptions.SecretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.Add(_jwtOptions.AccessTokenExpiration),
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,
                SigningCredentials = credentials
            };

            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(securityToken);

            _logger.LogDebug("生成访问令牌成功，用户ID: {UserId}", user.UserId);
            return accessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成访问令牌失败，用户ID: {UserId}", user.UserId);
            throw;
        }
    }

    /// <summary>
    /// 生成刷新令牌
    /// </summary>
    public async Task<RefreshToken> GenerateRefreshTokenAsync(int userId, string? ipAddress = null, string? userAgent = null, string? deviceId = null)
    {
        try
        {
            // 生成唯一的刷新令牌
            var tokenValue = SecurityHelper.GenerateRandomToken(64);
            var expiryDate = DateTime.UtcNow.Add(_jwtOptions.RefreshTokenExpiration);

            var refreshToken = new RefreshToken
            {
                Token = tokenValue,
                UserId = userId,
                ExpiryDate = expiryDate,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceId = deviceId ?? SecurityHelper.GenerateDeviceFingerprint(userAgent, ipAddress),
                CreatedBy = userId
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            _logger.LogDebug("生成刷新令牌成功，用户ID: {UserId}, 设备ID: {DeviceId}", userId, refreshToken.DeviceId);
            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成刷新令牌失败，用户ID: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 生成完整的Token响应
    /// </summary>
    public async Task<TokenResponse> GenerateTokenResponseAsync(User user, string? ipAddress = null, string? userAgent = null, string? deviceId = null, IEnumerable<Claim>? additionalClaims = null)
    {
        try
        {
            // 简化设备数量限制：只保留最新的活跃Token
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.UserId && rt.IsRevoked == 0 && rt.ExpiryDate > DateTime.UtcNow)
                .OrderByDescending(rt => rt.LastUsedAt ?? rt.CreatedAt)
                .Skip(_jwtOptions.MaxActiveDevices - 1)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.Revoke("设备数量限制", user.UserId);
            }

            // 生成访问令牌
            var accessToken = await GenerateAccessTokenAsync(user, additionalClaims);

            // 生成刷新令牌
            var refreshToken = await GenerateRefreshTokenAsync(user.UserId, ipAddress, userAgent, deviceId);

            // 直接更新用户登录信息
            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIp = ipAddress;
            user.LoginCount++;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresIn = (int)_jwtOptions.AccessTokenExpiration.TotalSeconds,
                ExpiresAt = DateTime.UtcNow.Add(_jwtOptions.AccessTokenExpiration),
                RefreshExpiresAt = refreshToken.ExpiryDate,
                UserId = user.UserId,
                Username = user.Username ?? user.Email,
                Email = user.Email,
                StudentId = user.StudentId,
                CreditScore = user.CreditScore,
                DeviceId = refreshToken.DeviceId,
                EmailVerified = user.EmailVerified == 1,
                TwoFactorEnabled = user.TwoFactorEnabled == 1,
                UserStatus = user.IsActive == 1 ? "Active" : "Inactive"
            };

            _logger.LogInformation("生成Token响应成功，用户ID: {UserId}", user.UserId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成Token响应失败，用户ID: {UserId}", user.UserId);
            throw;
        }
    }

    /// <summary>
    /// 验证访问令牌
    /// </summary>
    public async Task<TokenValidationResponse> ValidateAccessTokenAsync(string token)
    {
        try
        {
            var (isValid, principal, error) = TokenHelper.ValidateToken(token, _jwtOptions);

            if (!isValid || principal == null)
            {
                return new TokenValidationResponse
                {
                    IsValid = false,
                    Error = error
                };
            }

            // 检查是否在黑名单中
            var jti = TokenHelper.GetJtiFromToken(token);
            if (!string.IsNullOrEmpty(jti) && await IsTokenBlacklistedAsync(jti))
            {
                return new TokenValidationResponse
                {
                    IsValid = false,
                    Error = "Token已被撤销"
                };
            }

            var (userId, username, email, studentId) = TokenHelper.ExtractUserInfo(principal);
            var expiration = TokenHelper.GetExpirationFromToken(token);

            return new TokenValidationResponse
            {
                IsValid = true,
                UserId = userId,
                ExpiresAt = expiration,
                Permissions = principal.Claims
                    .Where(c => c.Type == "permission")
                    .Select(c => c.Value)
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证访问令牌失败");
            return new TokenValidationResponse
            {
                IsValid = false,
                Error = "Token验证异常"
            };
        }
    }

    /// <summary>
    /// 验证刷新令牌
    /// </summary>
    public async Task<RefreshToken?> ValidateRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var token = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token == null)
            {
                _logger.LogWarning("刷新令牌不存在: {Token}", SecurityHelper.ObfuscateSensitive(refreshToken));
                return null;
            }

            if (!token.IsValid())
            {
                _logger.LogWarning("刷新令牌无效，用户ID: {UserId}, 原因: {Reason}",
                    token.UserId, token.IsRevoked == 1 ? "已撤销" : "已过期");
                return null;
            }

            // 更新最后使用时间
            token.UpdateLastUsed();
            await _context.SaveChangesAsync();

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证刷新令牌失败");
            return null;
        }
    }

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest refreshTokenRequest)
    {
        var refreshToken = await ValidateRefreshTokenAsync(refreshTokenRequest.RefreshToken);
        if (refreshToken?.User == null)
        {
            throw new UnauthorizedAccessException("无效的刷新令牌");
        }

        try
        {
            // 检查用户状态
            if (refreshToken.User.IsActive != 1)
            {
                throw new UnauthorizedAccessException("用户账户已被禁用");
            }

            // Token轮换：撤销旧的刷新令牌
            if (_jwtOptions.RefreshTokenRotation || refreshTokenRequest.EnableRotation == true)
            {
                refreshToken.Revoke("Token轮换", refreshToken.UserId);
            }

            // 生成新的Token响应
            var response = await GenerateTokenResponseAsync(
                refreshToken.User,
                refreshTokenRequest.IpAddress,
                refreshTokenRequest.UserAgent,
                refreshTokenRequest.DeviceId);

            // 如果启用了轮换，设置替换关系
            if (_jwtOptions.RefreshTokenRotation)
            {
                refreshToken.ReplacedByToken = response.RefreshToken;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("刷新Token成功，用户ID: {UserId}", refreshToken.UserId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新Token失败，用户ID: {UserId}", refreshToken.UserId);
            throw;
        }
    }

    /// <summary>
    /// 撤销刷新令牌
    /// </summary>
    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, string? reason = null, int? revokedBy = null)
    {
        try
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token == null)
            {
                _logger.LogWarning("尝试撤销不存在的刷新令牌: {Token}", SecurityHelper.ObfuscateSensitive(refreshToken));
                return false;
            }

            if (token.IsRevoked == 1)
            {
                _logger.LogDebug("刷新令牌已经撤销: {Token}", SecurityHelper.ObfuscateSensitive(refreshToken));
                return true;
            }

            token.Revoke(reason, revokedBy);

            // 如果启用了级联撤销，同时撤销派生的Token
            if (_jwtOptions.RevokeDescendantRefreshTokens && !string.IsNullOrEmpty(token.ReplacedByToken))
            {
                await RevokeRefreshTokenAsync(token.ReplacedByToken, "级联撤销", revokedBy);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("撤销刷新令牌成功，用户ID: {UserId}", token.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "撤销刷新令牌失败");
            return false;
        }
    }

    /// <summary>
    /// 撤销用户的所有刷新令牌
    /// </summary>
    public async Task<int> RevokeAllUserTokensAsync(int userId, string? reason = null, int? revokedBy = null)
    {
        try
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.IsRevoked == 0)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.Revoke(reason ?? "撤销所有Token", revokedBy);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("撤销用户所有Token成功，用户ID: {UserId}, 数量: {Count}", userId, tokens.Count);
            return tokens.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "撤销用户所有Token失败，用户ID: {UserId}", userId);
            return 0;
        }
    }



    /// <summary>
    /// 获取用户的活跃刷新令牌列表
    /// </summary>
    public async Task<IEnumerable<RefreshToken>> GetActiveRefreshTokensAsync(int userId)
    {
        try
        {
            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.IsRevoked == 0 && rt.ExpiryDate > DateTime.UtcNow)
                .OrderByDescending(rt => rt.LastUsedAt ?? rt.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活跃刷新令牌失败，用户ID: {UserId}", userId);
            return Enumerable.Empty<RefreshToken>();
        }
    }

    /// <summary>
    /// 清理过期的刷新令牌
    /// </summary>
    public async Task<int> CleanupExpiredTokensAsync()
    {
        try
        {
            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.ExpiryDate <= DateTime.UtcNow)
                .ToListAsync();

            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();

            _logger.LogInformation("清理过期刷新令牌成功，数量: {Count}", expiredTokens.Count);
            return expiredTokens.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理过期刷新令牌失败");
            return 0;
        }
    }

    /// <summary>
    /// 检查Token是否在黑名单中
    /// </summary>
    public async Task<bool> IsTokenBlacklistedAsync(string jti)
    {
        if (!_jwtOptions.EnableTokenBlacklist)
            return false;

        try
        {
            var cacheKey = $"blacklist:{jti}";
            return _cache.TryGetValue(cacheKey, out _);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查Token黑名单失败，JTI: {Jti}", jti);
            return false;
        }
    }

    /// <summary>
    /// 将Token添加到黑名单
    /// </summary>
    public async Task<bool> BlacklistTokenAsync(string jti, DateTime expiration)
    {
        if (!_jwtOptions.EnableTokenBlacklist)
            return true;

        try
        {
            var cacheKey = $"blacklist:{jti}";
            var timeToLive = expiration - DateTime.UtcNow;

            if (timeToLive > TimeSpan.Zero)
            {
                _cache.Set(cacheKey, true, timeToLive);
                _logger.LogDebug("Token加入黑名单成功，JTI: {Jti}", jti);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token加入黑名单失败，JTI: {Jti}", jti);
            return false;
        }
    }


}
