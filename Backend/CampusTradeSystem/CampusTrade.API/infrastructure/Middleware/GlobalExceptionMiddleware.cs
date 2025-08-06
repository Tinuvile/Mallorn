using System.Diagnostics;
using System.Net;
using System.Text.Json;
using CampusTrade.API.Models.DTOs.Common;
using Serilog;
using Serilog.Context;

namespace CampusTrade.API.Infrastructure.Middleware;

/// <summary>
/// 全局异常处理中间件
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        try
        {
            await _next(context);

            // 记录成功的请求（仅在开发环境或需要审计时）
            stopwatch.Stop();
            if (_environment.IsDevelopment() || context.Response.StatusCode >= 400)
            {
                LogRequestCompletion(context, stopwatch.ElapsedMilliseconds, traceId);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await HandleExceptionAsync(context, ex, traceId, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string traceId, long elapsedMs)
    {
        var ipAddress = GetClientIPAddress(context);
        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();
        var userId = GetUserId(context);

        // 结构化日志记录
        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("UserId", userId ?? "anonymous"))
        using (LogContext.PushProperty("IPAddress", ipAddress))
        using (LogContext.PushProperty("UserAgent", userAgent ?? "unknown"))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        using (LogContext.PushProperty("ElapsedMilliseconds", elapsedMs))
        {
            var (statusCode, errorCode, message, logLevel) = GetErrorDetails(exception);

            // 根据异常类型选择日志级别
            if (logLevel == LogLevel.Error)
            {
                Log.Logger.Error(exception, "未处理的异常: {ExceptionType}", exception.GetType().Name);
            }
            else
            {
                Log.Logger.Warning(exception, "应用程序异常: {ExceptionType}", exception.GetType().Name);
            }

            var response = context.Response;
            response.ContentType = "application/json";
            response.StatusCode = statusCode;

            var errorResponse = new ApiResponse
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };

            // 在开发环境中包含详细错误信息
            if (_environment.IsDevelopment())
            {
                var devResponse = new
                {
                    errorResponse.Success,
                    errorResponse.Message,
                    errorResponse.ErrorCode,
                    errorResponse.Timestamp,
                    TraceId = traceId,
                    ExceptionType = exception.GetType().Name,
                    StackTrace = exception.StackTrace,
                    InnerException = exception.InnerException?.Message
                };

                var devJson = JsonSerializer.Serialize(devResponse, GetJsonOptions());
                await response.WriteAsync(devJson);
            }
            else
            {
                var json = JsonSerializer.Serialize(errorResponse, GetJsonOptions());
                await response.WriteAsync(json);
            }
        }
    }

    private void LogRequestCompletion(HttpContext context, long elapsedMs, string traceId)
    {
        var ipAddress = GetClientIPAddress(context);
        var userId = GetUserId(context);

        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("UserId", userId ?? "anonymous"))
        using (LogContext.PushProperty("IPAddress", ipAddress))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        using (LogContext.PushProperty("ResponseStatusCode", context.Response.StatusCode))
        using (LogContext.PushProperty("ElapsedMilliseconds", elapsedMs))
        {
            if (context.Response.StatusCode >= 400)
            {
                Log.Logger.Warning("请求完成，状态码: {StatusCode}", context.Response.StatusCode);
            }
            else
            {
                Log.Logger.Information("请求完成");
            }
        }
    }

    private string GetClientIPAddress(HttpContext context)
    {
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

    private string? GetUserId(HttpContext context)
    {
        return context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }

    private (int statusCode, string errorCode, string message, LogLevel logLevel) GetErrorDetails(Exception exception)
    {
        return exception switch
        {
            // 具体异常类型要放在基类之前
            ArgumentNullException => (400, "INVALID_ARGUMENT", "请求参数不能为空", LogLevel.Warning),
            ArgumentException argEx => (400, "INVALID_ARGUMENT", argEx.Message, LogLevel.Warning),

            UnauthorizedAccessException => (401, "UNAUTHORIZED", "访问被拒绝", LogLevel.Warning),
            KeyNotFoundException => (404, "NOT_FOUND", "请求的资源未找到", LogLevel.Warning),
            InvalidOperationException invEx => (400, "INVALID_OPERATION", invEx.Message, LogLevel.Warning),
            TimeoutException => (408, "REQUEST_TIMEOUT", "请求超时", LogLevel.Warning),
            NotSupportedException => (501, "NOT_SUPPORTED", "不支持的操作", LogLevel.Warning),
            TaskCanceledException => (499, "REQUEST_CANCELLED", "请求被取消", LogLevel.Information),

            // JWT相关异常 - 更具体的类型要放在前面
            Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException => (401, "TOKEN_EXPIRED", "Token已过期", LogLevel.Warning),
            Microsoft.IdentityModel.Tokens.SecurityTokenException => (401, "INVALID_TOKEN", "Token无效", LogLevel.Warning),

            // 数据库相关异常 - 更具体的类型要放在前面
            Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => (409, "CONCURRENCY_ERROR", "数据并发冲突", LogLevel.Warning),
            Microsoft.EntityFrameworkCore.DbUpdateException => (500, "DATABASE_ERROR", "数据更新失败", LogLevel.Error),
            System.Data.Common.DbException => (500, "DATABASE_ERROR", "数据库操作失败", LogLevel.Error),

            // 默认处理
            _ => (500, "INTERNAL_ERROR", "内部服务器错误", LogLevel.Error)
        };
    }

    private JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };
    }
}

/// <summary>
/// 全局异常处理中间件扩展
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    /// <summary>
    /// 使用全局异常处理中间件
    /// </summary>
    /// <param name="builder">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
