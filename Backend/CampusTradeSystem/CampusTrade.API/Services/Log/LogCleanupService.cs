using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CampusTrade.API.Services
{
    public class LogCleanupService : BackgroundService
    {
        private readonly string[] _logDirs = new[]
        {
            "logs/business",
            "logs/security",
            "logs/errors",
            "logs/performance"
        };

        private readonly TimeSpan _cleanupInterval = TimeSpan.FromDays(1); // 每天执行
        private readonly int _retainDays = 30; // 只保留最近30天的日志

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    CleanOldLogs();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "日志清理任务执行失败");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }
        }

        private void CleanOldLogs()
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-_retainDays);

            foreach (var dir in _logDirs)
            {
                if (!Directory.Exists(dir)) continue;

                var files = Directory.GetFiles(dir, "*.log");
                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.LastWriteTimeUtc < cutoffDate)
                        {
                            fileInfo.Delete();
                            Log.Information("已删除过期日志: {File}", file);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "无法删除日志文件: {File}", file);
                    }
                }
            }
        }
    }
}
