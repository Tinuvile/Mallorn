using System;
using System.Collections.Generic;
using System.Diagnostics;
using Serilog;

namespace CampusTrade.API.Infrastructure.Utils.Performance;

/// <summary>
/// 业务操作性能追踪器，用于记录单个业务流程或子操作的耗时日志
/// </summary>
public class PerformanceTracker : IDisposable
{
    private readonly Serilog.ILogger _logger;
    private readonly Stopwatch _stopwatch;
    private readonly string _operationName;
    private readonly string _category;
    private readonly Dictionary<string, object> _context;

    public PerformanceTracker(Serilog.ILogger logger, string operationName, string category = "Business")
    {
        _logger = logger;
        _operationName = operationName;
        _category = category;
        _context = new Dictionary<string, object>();
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// 添加额外上下文字段（例如用户ID、订单号等）
    /// </summary>
    public PerformanceTracker AddContext(string key, object value)
    {
        _context[key] = value;
        return this;
    }

    public void Dispose()
    {
        _stopwatch.Stop();

        var elapsedMs = _stopwatch.ElapsedMilliseconds;

        var logContext = _logger
            .ForContext("LogType", "Performance")
            .ForContext("Category", _category)
            .ForContext("Operation", _operationName)
            .ForContext("ElapsedMs", elapsedMs);

        foreach (var kvp in _context)
        {
            logContext = logContext.ForContext(kvp.Key, kvp.Value);
        }

        logContext.Information("业务操作性能: {Operation} 耗时: {ElapsedMs}ms", _operationName, elapsedMs);

        if (elapsedMs > GetPerformanceThreshold(_operationName))
        {
            logContext.Warning("性能异常: {Operation} 耗时: {ElapsedMs}ms 超过阈值", _operationName, elapsedMs);
        }
    }

    /// <summary>
    /// 可根据不同业务操作名设定不同阈值
    /// </summary>
    private long GetPerformanceThreshold(string operationName)
    {
        return operationName switch
        {
            "CreateOrder" => 2000,
            "ProcessPayment" => 5000,
            "SearchProducts" => 1000,
            "UploadImage" => 3000,
            _ => 2000
        };
    }
}
