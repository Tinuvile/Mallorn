using System.Security.Claims;
using CampusTrade.API.Models.DTOs.Auth;
using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Services.Auth;

/// <summary>
/// Token服务接口
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// 生成访问令牌
    /// </summary>
    /// <param name="user">用户信息</param>
    /// <param name="additionalClaims">额外的Claims</param>
    /// <returns>JWT访问令牌</returns>
    Task<string> GenerateAccessTokenAsync(User user, IEnumerable<Claim>? additionalClaims = null);

    /// <summary>
    /// 生成刷新令牌
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="userAgent">用户代理</param>
    /// <param name="deviceId">设备ID</param>
    /// <returns>刷新令牌实体</returns>
    Task<RefreshToken> GenerateRefreshTokenAsync(int userId, string? ipAddress = null, string? userAgent = null, string? deviceId = null);

    /// <summary>
    /// 生成完整的Token响应
    /// </summary>
    /// <param name="user">用户信息</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="userAgent">用户代理</param>
    /// <param name="deviceId">设备ID</param>
    /// <param name="additionalClaims">额外的Claims</param>
    /// <returns>Token响应</returns>
    Task<TokenResponse> GenerateTokenResponseAsync(User user, string? ipAddress = null, string? userAgent = null, string? deviceId = null, IEnumerable<Claim>? additionalClaims = null);

    /// <summary>
    /// 验证访问令牌
    /// </summary>
    /// <param name="token">访问令牌</param>
    /// <returns>验证结果</returns>
    Task<TokenValidationResponse> ValidateAccessTokenAsync(string token);

    /// <summary>
    /// 验证刷新令牌
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <returns>刷新令牌实体（如果有效）</returns>
    Task<RefreshToken?> ValidateRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    /// <param name="refreshTokenRequest">刷新令牌请求</param>
    /// <returns>新的Token响应</returns>
    Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest refreshTokenRequest);

    /// <summary>
    /// 撤销刷新令牌
    /// </summary>
    /// <param name="refreshToken">要撤销的刷新令牌</param>
    /// <param name="reason">撤销原因</param>
    /// <param name="revokedBy">撤销者ID</param>
    /// <returns>撤销是否成功</returns>
    Task<bool> RevokeRefreshTokenAsync(string refreshToken, string? reason = null, int? revokedBy = null);

    /// <summary>
    /// 撤销用户的所有刷新令牌
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="reason">撤销原因</param>
    /// <param name="revokedBy">撤销者ID</param>
    /// <returns>撤销的令牌数量</returns>
    Task<int> RevokeAllUserTokensAsync(int userId, string? reason = null, int? revokedBy = null);

    /// <summary>
    /// 获取用户的活跃刷新令牌列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>活跃的刷新令牌列表</returns>
    Task<IEnumerable<RefreshToken>> GetActiveRefreshTokensAsync(int userId);

    /// <summary>
    /// 清理过期的刷新令牌
    /// </summary>
    /// <returns>清理的令牌数量</returns>
    Task<int> CleanupExpiredTokensAsync();

    /// <summary>
    /// 检查Token是否在黑名单中
    /// </summary>
    /// <param name="jti">JWT ID</param>
    /// <returns>是否在黑名单中</returns>
    Task<bool> IsTokenBlacklistedAsync(string jti);

    /// <summary>
    /// 将Token添加到黑名单
    /// </summary>
    /// <param name="jti">JWT ID</param>
    /// <param name="expiration">过期时间</param>
    /// <returns>添加是否成功</returns>
    Task<bool> BlacklistTokenAsync(string jti, DateTime expiration);
}
