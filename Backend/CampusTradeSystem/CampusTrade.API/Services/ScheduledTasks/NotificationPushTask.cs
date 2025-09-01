using System.Threading.Tasks;
using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.ScheduledTasks
{
    public class NotificationPushTask : ScheduledService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        // 注入 IServiceScopeFactory 而非直接注入 DbContext
        public NotificationPushTask(ILogger<NotificationPushTask> logger, IServiceScopeFactory scopeFactory) : base(logger)
        {
            _scopeFactory = scopeFactory;
        }

        protected override TimeSpan Interval => TimeSpan.FromHours(1); // 每小时执行一次

        protected override async Task ExecuteTaskAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<CampusTradeDbContext>();

                var pendingNotifications = await context.Notifications.Where(n => n.SendStatus == "待发送").ToListAsync();
                foreach (var notification in pendingNotifications)
                {
                    // 这里添加通知推送逻辑，例如发送邮件、短信等
                    notification.SendStatus = "成功";
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
