using System;
using System.Threading.Tasks;
using CampusTrade.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.ScheduledTasks
{
    public class OrderProcessingTask : ScheduledService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        // 注入 IServiceScopeFactory 而非直接注入 DbContext
        public OrderProcessingTask(ILogger<OrderProcessingTask> logger, IServiceScopeFactory scopeFactory) : base(logger)
        {
            _scopeFactory = scopeFactory;
        }

        protected override TimeSpan Interval => TimeSpan.FromHours(6); // 每 6 小时执行一次

        protected override async Task ExecuteTaskAsync()
        {
            // 手动创建作用域，确保 DbContext 在作用域内使用
            using (var scope = _scopeFactory.CreateScope())
            {
                // 从作用域中获取 DbContext（符合 Scoped 生命周期）
                var context = scope.ServiceProvider.GetRequiredService<CampusTradeDbContext>();

                var cutoffDate = DateTime.Now.AddHours(-24); // 取消 24 小时未支付的订单
                var ordersToCancel = await context.Orders.Where(o => o.Status == "待付款" && o.CreateTime < cutoffDate).ToListAsync();
                foreach (var order in ordersToCancel)
                {
                    order.Status = "已取消";
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
