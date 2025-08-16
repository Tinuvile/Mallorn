using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CampusTrade.API.Infrastructure.Utils.Security;
using CampusTrade.API.Models.DTOs.Auth;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Options;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace CampusTrade.API.Services.Auth;

/// <summary>
/// Token服务实现
/// </summary>
public class TokenService : ITokenService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtOptions _jwtOptions;
    private readonly IMemoryCache _cache;

    public TokenService(
        IUnitOfWork unitOfWork,
        IOptions<JwtOptions> jwtOptions,
        IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _jwtOptions = jwtOptions.Value;
        _cache = cache;
    }

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

            Log.Logger.Information("生成访问令牌成功，用户ID: {UserId}", user.UserId);
            return accessToken;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "生成访问令牌失败，用户ID: {UserId}", user.UserId);
            throw;
        }
    }

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

            await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
            await _unitOfWork.SaveChangesAsync();

            Log.Logger.Information("生成刷新令牌成功，用户ID: {UserId}, 设备ID: {DeviceId}", userId, refreshToken.DeviceId);
            return refreshToken;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "生成刷新令牌失败，用户ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<TokenResponse> GenerateTokenResponseAsync(User user, string? ipAddress = null, string? userAgent = null, string? deviceId = null, IEnumerable<Claim>? additionalClaims = null)
    {
        try
        {
            // 简化设备数量限制：只保留最新的活跃Token
            var activeTokens = await _unitOfWork.RefreshTokens.FindAsync(
                rt => rt.UserId == user.UserId && rt.IsRevoked == 0 && rt.ExpiryDate > DateTime.UtcNow);

            var tokensToRevoke = activeTokens
                .OrderByDescending(rt => rt.LastUsedAt ?? rt.CreatedAt)
                .Skip(_jwtOptions.MaxActiveDevices - 1)
                .ToList();

            foreach (var token in tokensToRevoke)
            {
                TokenHelper.RevokeRefreshToken(token, "设备数量限制", user.UserId);
            }

            // 生成访问令牌
            var accessToken = await GenerateAccessTokenAsync(user, additionalClaims);

            // 生成刷新令牌
            var refreshToken = await GenerateRefreshTokenAsync(user.UserId, ipAddress, userAgent, deviceId);

            // 更新用户登录信息
            await _unitOfWork.Users.UpdateLastLoginAsync(user.UserId, ipAddress ?? "Unknown");

            await _unitOfWork.SaveChangesAsync();

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

            Log.Logger.Information("生成Token响应成功，用户ID: {UserId}", user.UserId);
            return response;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "生成Token响应失败，用户ID: {UserId}", user.UserId);
            throw;
        }
    }

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
            Log.Logger.Error(ex, "验证访问令牌失败");
            return new TokenValidationResponse
            {
                IsValid = false,
                Error = "Token验证异常"
            };
        }
    }

    public async Task<RefreshToken?> ValidateRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var tokens = await _unitOfWork.RefreshTokens.GetWithIncludeAsync(
                filter: rt => rt.Token == refreshToken,
                includeProperties: rt => rt.User);

            var refreshTokenEntity = tokens.FirstOrDefault();
            if (refreshTokenEntity == null)
            {
                Log.Logger.Warning("刷新令牌不存在: {Token}", SecurityHelper.ObfuscateSensitive(refreshToken));
                return null;
            }

            if (!TokenHelper.IsRefreshTokenValid(refreshTokenEntity))
            {
                Log.Logger.Warning("刷新令牌无效，用户ID: {UserId}, 原因: {Reason}",
                    refreshTokenEntity.UserId, refreshTokenEntity.IsRevoked == 1 ? "已撤销" : "已过期");
                return null;
            }

            // 更新最后使用时间
            TokenHelper.UpdateRefreshTokenLastUsed(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            return refreshTokenEntity;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "验证刷新令牌失败");
            return null;
        }
    }

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
            if (refreshToken.User.IsActive == 0)
            {
                throw new UnauthorizedAccessException("用户账户已被禁用");
            }

            // Token轮换：撤销旧的刷新令牌
            if (_jwtOptions.RefreshTokenRotation || refreshTokenRequest.EnableRotation == true)
            {
                TokenHelper.RevokeRefreshToken(refreshToken, "Token轮换", refreshToken.UserId);
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
                await _unitOfWork.SaveChangesAsync();
            }

            Log.Logger.Information("刷新Token成功，用户ID: {UserId}", refreshToken.UserId);
            return response;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "刷新Token失败，用户ID: {UserId}", refreshToken.UserId);
            throw;
        }
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, string? reason = null, int? revokedBy = null)
    {
        try
        {
            var token = await _unitOfWork.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
            if (token == null)
            {
                Log.Logger.Warning("尝试撤销不存在的刷新令牌: {Token}", SecurityHelper.ObfuscateSensitive(refreshToken));
                return false;
            }

            if (token.IsRevoked == 1)
            {
                Log.Logger.Debug("刷新令牌已经撤销: {Token}", SecurityHelper.ObfuscateSensitive(refreshToken));
                return true;
            }

            TokenHelper.RevokeRefreshToken(token, reason, revokedBy);

            // 如果启用了级联撤销，同时撤销派生的Token
            if (_jwtOptions.RevokeDescendantRefreshTokens && !string.IsNullOrEmpty(token.ReplacedByToken))
            {
                await RevokeRefreshTokenAsync(token.ReplacedByToken, "级联撤销", revokedBy);
            }

            await _unitOfWork.SaveChangesAsync();

            Log.Logger.Information("撤销刷新令牌成功，用户ID: {UserId}", token.UserId);
            return true;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "撤销刷新令牌失败");
            return false;
        }
    }

    public async Task<int> RevokeAllUserTokensAsync(int userId, string? reason = null, int? revokedBy = null)
    {
        try
        {
            var tokens = await _unitOfWork.RefreshTokens.FindAsync(rt => rt.UserId == userId && rt.IsRevoked == 0);

            foreach (var token in tokens)
            {
                TokenHelper.RevokeRefreshToken(token, reason ?? "撤销所有Token", revokedBy);
            }

            await _unitOfWork.SaveChangesAsync();

            Log.Logger.Information("撤销用户所有Token成功，用户ID: {UserId}, 数量: {Count}", userId, tokens.Count());
            return tokens.Count();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "撤销用户所有Token失败，用户ID: {UserId}", userId);
            return 0;
        }
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveRefreshTokensAsync(int userId)
    {
        try
        {
            return await _unitOfWork.RefreshTokens.FindAsync(
                rt => rt.UserId == userId && rt.IsRevoked == 0 && rt.ExpiryDate > DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "获取活跃刷新令牌失败，用户ID: {UserId}", userId);
            return Enumerable.Empty<RefreshToken>();
        }
    }

    public async Task<int> CleanupExpiredTokensAsync()
    {
        try
        {
            var deletedCount = await _unitOfWork.RefreshTokens.CleanupExpiredTokensAsync();
            await _unitOfWork.SaveChangesAsync();

            Log.Logger.Information("清理过期刷新令牌成功，数量: {Count}", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "清理过期刷新令牌失败");
            return 0;
        }
    }

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
            Log.Logger.Error(ex, "检查Token黑名单失败，JTI: {Jti}", jti);
            return false;
        }
    }

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
                Log.Logger.Debug("Token加入黑名单成功，JTI: {Jti}", jti);
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Token加入黑名单失败，JTI: {Jti}", jti);
            return false;
        }
    }
}
