using System;
using System.Threading.Tasks;
using CampusTrade.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.ScheduledTasks
{
    public class LogCleanupTask : ScheduledService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        /// 注入 IServiceScopeFactory 而非直接注入 DbContext
        public LogCleanupTask(ILogger<LogCleanupTask> logger, IServiceScopeFactory scopeFactory) : base(logger)
        {
            _scopeFactory = scopeFactory;
        }

        protected override TimeSpan Interval => TimeSpan.FromDays(1); // 每天执行一次

        protected override async Task ExecuteTaskAsync()
        {
            // 手动创建作用域，确保DbContext在作用域内使用（符合Scoped生命周期）
            using (var scope = _scopeFactory.CreateScope())
            {
                // 从当前作用域中获取DbContext
                var context = scope.ServiceProvider.GetRequiredService<CampusTradeDbContext>();
                var cutoffDate = DateTime.Now.AddDays(-7); // 清理7天前的日志

                // 清理LoginLogs表中的日志
                var loginLogsToDelete = await context.LoginLogs.Where(l => l.LogTime < cutoffDate).ToListAsync();
                context.LoginLogs.RemoveRange(loginLogsToDelete);

                // 清理AuditLogs表中的日志
                var auditLogsToDelete = await context.AuditLogs.Where(l => l.LogTime < cutoffDate).ToListAsync();
                context.AuditLogs.RemoveRange(auditLogsToDelete);

                await context.SaveChangesAsync();
            }
        }
    }
}
