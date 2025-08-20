using System.Net;
using System.Text.Json;
using CampusTrade.API.Models.DTOs.Common;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace CampusTrade.API.Infrastructure.Middleware;

/// <summary>
/// 安全检查中间件
/// </summary>
public class SecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;

    // 速率限制配置
    private readonly int _maxRequestsPerMinute;
    private readonly int _maxLoginAttemptsPerHour;
    private readonly TimeSpan _blockDuration;

    // 可疑行为检测
    private readonly List<string> _suspiciousUserAgents;
    private readonly List<string> _blockedIPs;

    public SecurityMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        _next = next;
        _cache = cache;
        _configuration = configuration;

        // 从配置读取安全参数
        _maxRequestsPerMinute = configuration.GetValue<int>("Security:MaxRequestsPerMinute", 60);
        _maxLoginAttemptsPerHour = configuration.GetValue<int>("Security:MaxLoginAttemptsPerHour", 10);
        _blockDuration = TimeSpan.FromMinutes(configuration.GetValue<int>("Security:BlockDurationMinutes", 30));

        // 可疑UserAgent列表
        _suspiciousUserAgents = configuration.GetSection("Security:SuspiciousUserAgents")
            .Get<List<string>>() ?? new List<string>
            {
                "bot", "crawler", "spider", "scraper", "scan", "sqlmap", "nikto"
            };

        // 被阻止的IP列表（可以从配置或数据库读取）
        _blockedIPs = configuration.GetSection("Security:BlockedIPs")
            .Get<List<string>>() ?? new List<string>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = GetClientIPAddress(context);
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var path = context.Request.Path.ToString();

        try
        {
            // 1. IP黑名单检查
            if (await IsIPBlocked(ipAddress))
            {
                await HandleBlocked(context, "IP地址被阻止", "IP_BLOCKED");
                return;
            }

            // 2. 可疑UserAgent检查
            if (IsSuspiciousUserAgent(userAgent))
            {
                Log.Warning("检测到可疑UserAgent访问, IP: {IP}, UserAgent: {UserAgent}", ipAddress, userAgent);
                await LogSecurityEvent("SUSPICIOUS_USER_AGENT", ipAddress, userAgent, path);
            }

            // 3. 速率限制检查
            if (await IsRateLimited(ipAddress, path))
            {
                await HandleBlocked(context, "请求过于频繁，请稍后再试", "RATE_LIMITED");
                return;
            }

            // 4. 登录端点特殊保护
            if (IsLoginEndpoint(path))
            {
                if (await IsLoginRateLimited(ipAddress))
                {
                    await HandleBlocked(context, "登录尝试过于频繁，请稍后再试", "LOGIN_RATE_LIMITED");
                    return;
                }
            }

            // 5. 请求大小检查
            if (context.Request.ContentLength > 10 * 1024 * 1024) // 10MB
            {
                await HandleBlocked(context, "请求内容过大", "REQUEST_TOO_LARGE");
                return;
            }

            // 6. 恶意路径检查
            if (IsMaliciousPath(path))
            {
                Log.Warning("检测到恶意路径访问, IP: {IP}, Path: {Path}", ipAddress, path);
                await LogSecurityEvent("MALICIOUS_PATH", ipAddress, userAgent, path);
                await HandleBlocked(context, "无效的请求路径", "INVALID_PATH");
                return;
            }

            // 记录正常访问（仅在Debug模式下）
            if (Log.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                Log.Debug("安全检查通过, IP: {IP}, Path: {Path}", ipAddress, path);
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "安全中间件执行异常, IP: {IP}, Path: {Path}", ipAddress, path);
            await _next(context); // 继续处理，避免因安全中间件异常导致服务不可用
        }
    }

    /// <summary>
    /// 获取客户端真实IP地址
    /// </summary>
    private string GetClientIPAddress(HttpContext context)
    {
        // 检查常见的代理头
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// 检查IP是否被阻止
    /// </summary>
    private async Task<bool> IsIPBlocked(string ipAddress)
    {
        // 检查静态黑名单
        if (_blockedIPs.Contains(ipAddress))
        {
            return true;
        }

        // 检查动态阻止列表（缓存中）
        var blockKey = $"blocked_ip:{ipAddress}";
        return _cache.TryGetValue(blockKey, out _);
    }

    /// <summary>
    /// 检查是否为可疑UserAgent
    /// </summary>
    private bool IsSuspiciousUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            return true; // 空UserAgent可疑
        }

        return _suspiciousUserAgents.Any(suspicious =>
            userAgent.ToLower().Contains(suspicious.ToLower()));
    }

    /// <summary>
    /// 速率限制检查
    /// </summary>
    private async Task<bool> IsRateLimited(string ipAddress, string path)
    {
        var key = $"rate_limit:{ipAddress}";
        var currentCount = _cache.Get<int>(key);

        if (currentCount >= _maxRequestsPerMinute)
        {
            // 动态阻止IP一段时间
            var blockKey = $"blocked_ip:{ipAddress}";
            _cache.Set(blockKey, true, _blockDuration);

            await LogSecurityEvent("RATE_LIMIT_EXCEEDED", ipAddress, "", path);
            return true;
        }

        // 增加计数
        _cache.Set(key, currentCount + 1, TimeSpan.FromMinutes(1));
        return false;
    }

    /// <summary>
    /// 登录速率限制检查
    /// </summary>
    private async Task<bool> IsLoginRateLimited(string ipAddress)
    {
        var key = $"login_attempts:{ipAddress}";
        var currentCount = _cache.Get<int>(key);

        if (currentCount >= _maxLoginAttemptsPerHour)
        {
            await LogSecurityEvent("LOGIN_RATE_LIMIT_EXCEEDED", ipAddress, "", "/api/auth/login");
            return true;
        }

        // 增加登录尝试计数
        _cache.Set(key, currentCount + 1, TimeSpan.FromHours(1));
        return false;
    }

    /// <summary>
    /// 检查是否为登录端点
    /// </summary>
    private bool IsLoginEndpoint(string path)
    {
        return path.ToLower().Contains("/auth/login");
    }

    /// <summary>
    /// 检查是否为恶意路径
    /// </summary>
    private bool IsMaliciousPath(string path)
    {
        var maliciousPatterns = new[]
        {
            "../", "..\\", ".env", "wp-admin", "admin.php", "phpmyadmin",
            " sql ", " union ", " select ", " insert ", " delete ", " drop ",
            "eval(", "javascript:", "vbscript:", "<script", "--", "/*", "*/"
        };

        var lowerPath = path.ToLower();
        return maliciousPatterns.Any(pattern => lowerPath.Contains(pattern.ToLower()));
    }

    /// <summary>
    /// 记录安全事件
    /// </summary>
    private async Task LogSecurityEvent(string eventType, string ipAddress, string userAgent, string path)
    {
        Log.Warning("安全事件: {EventType}, IP: {IP}, UserAgent: {UserAgent}, Path: {Path}",
            eventType, ipAddress, userAgent, path);

        // 这里可以扩展为存储到数据库或发送警报
        // 例如：await _securityEventService.LogEventAsync(eventType, ipAddress, userAgent, path);
    }

    /// <summary>
    /// 处理被阻止的请求
    /// </summary>
    private async Task HandleBlocked(HttpContext context, string message, string errorCode)
    {
        context.Response.StatusCode = 429; // Too Many Requests
        context.Response.ContentType = "application/json";

        var response = ApiResponse.CreateError(message, errorCode);
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// 安全中间件扩展
/// </summary>
public static class SecurityMiddlewareExtensions
{
    /// <summary>
    /// 使用安全检查中间件
    /// </summary>
    /// <param name="builder">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseSecurity(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityMiddleware>();
    }
}
