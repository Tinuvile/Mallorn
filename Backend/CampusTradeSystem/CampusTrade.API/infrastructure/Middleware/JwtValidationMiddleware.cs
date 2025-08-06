using System.IdentityModel.Tokens.Jwt;
using CampusTrade.API.Infrastructure.Utils.Security;
using CampusTrade.API.Services.Auth;
using Serilog;

namespace CampusTrade.API.Infrastructure.Middleware;

/// <summary>
/// JWT验证中间件
/// </summary>
public class JwtValidationMiddleware
{
    private readonly RequestDelegate _next;

    public JwtValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
    {
        // 获取Authorization头
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader[7..]; // 去掉"Bearer "前缀

            try
            {
                // 验证Token格式
                var tokenHandler = new JwtSecurityTokenHandler();
                if (tokenHandler.CanReadToken(token))
                {
                    var jwtToken = tokenHandler.ReadJwtToken(token);
                    var jti = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;

                    // 检查Token是否在黑名单中
                    if (!string.IsNullOrEmpty(jti))
                    {
                        var isBlacklisted = await tokenService.IsTokenBlacklistedAsync(jti);
                        if (isBlacklisted)
                        {
                            Log.Logger.Warning("检测到已撤销的Token访问, JTI: {Jti}, IP: {IpAddress}",
                                jti, context.Connection.RemoteIpAddress);

                            context.Response.StatusCode = 401;
                            await context.Response.WriteAsync("{\"success\":false,\"message\":\"Token已被撤销\",\"error_code\":\"TOKEN_REVOKED\"}");
                            return;
                        }
                    }

                    // 检查Token是否即将过期（5分钟内）
                    var exp = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp)?.Value;
                    if (long.TryParse(exp, out var expUnix))
                    {
                        var expiration = DateTimeOffset.FromUnixTimeSeconds(expUnix).DateTime;
                        if (expiration <= DateTime.UtcNow.AddMinutes(5))
                        {
                            // 添加即将过期的头信息
                            context.Response.Headers.Append("X-Token-Warning", "Token即将过期，请及时刷新");
                            Log.Logger.Debug("Token即将过期, JTI: {Jti}, 过期时间: {Expiration}", jti, expiration);
                        }
                    }

                    // 记录Token使用情况（用于审计）
                    var userId = jwtToken.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        Log.Logger.Debug("用户 {UserId} 使用Token访问 {Path}, IP: {IpAddress}",
                            userId, context.Request.Path, context.Connection.RemoteIpAddress);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Warning(ex, "JWT中间件处理Token时发生错误");
                // 继续处理，让JWT认证中间件处理具体的验证错误
            }
        }

        await _next(context);
    }
}

/// <summary>
/// JWT验证中间件扩展
/// </summary>
public static class JwtValidationMiddlewareExtensions
{
    /// <summary>
    /// 使用JWT验证中间件
    /// </summary>
    /// <param name="builder">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseJwtValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtValidationMiddleware>();
    }
}
