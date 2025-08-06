using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Serilog;

namespace CampusTrade.API.Data;

/// <summary>
/// 数据库性能拦截器，用于记录慢查询日志
/// </summary>
public class DatabasePerformanceInterceptor : DbCommandInterceptor
{
    private readonly Serilog.ILogger _logger;
    private readonly long _thresholdMs;

    public DatabasePerformanceInterceptor()
    {
        _logger = Log.Logger.ForContext("LogType", "Performance");
        _thresholdMs = 0; // 超过 200ms 认为是慢查询，可从配置读取
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData);
        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override async ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData);
        return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override async ValueTask<object> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData);
        return await base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void LogIfSlow(DbCommand command, CommandExecutedEventData eventData)
    {
        var elapsedMs = eventData.Duration.TotalMilliseconds;
        if (elapsedMs >= _thresholdMs)
        {
            _logger.Warning("慢SQL查询: {CommandText} 耗时: {ElapsedMs}ms",
                command.CommandText.Trim(), elapsedMs);
        }
    }
}
