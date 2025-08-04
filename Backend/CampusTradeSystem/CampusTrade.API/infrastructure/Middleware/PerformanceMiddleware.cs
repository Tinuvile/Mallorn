using System.Diagnostics;
using System.Security.Claims;
using Serilog;

namespace CampusTrade.API.Infrastructure.Middleware;

public class PerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Serilog.ILogger _logger;
    private readonly long _warningThresholdMs = 1000; // 默认1秒为慢请求

    public PerformanceMiddleware(RequestDelegate next)
    {
        _next = next;
        _logger = Log.Logger; // 使用 Serilog.Log 提供的全局 Logger 实例
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();

        var elapsedMs = stopwatch.ElapsedMilliseconds;
        var userId = GetUserId(context);

        var log = _logger
            .ForContext("LogType", "Performance")
            .ForContext("ElapsedMs", elapsedMs)
            .ForContext("UserId", userId)
            .ForContext("Path", context.Request.Path)
            .ForContext("Method", context.Request.Method)
            .ForContext("StatusCode", context.Response.StatusCode);

        if (elapsedMs > _warningThresholdMs)
        {
            log.Warning("慢请求: {Method} {Path} 花费 {ElapsedMs}ms", context.Request.Method, context.Request.Path, elapsedMs);
        }
        else
        {
            log.Information("请求: {Method} {Path} 花费 {ElapsedMs}ms", context.Request.Method, context.Request.Path, elapsedMs);
        }
    }

    public string GetUserId(HttpContext context)
    {
        return context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
    }
}
