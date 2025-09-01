using System.Threading.Tasks;
using CampusTrade.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.ScheduledTasks
{
    public class StatisticalAnalysisTask : ScheduledService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        // 注入 IServiceScopeFactory 而非直接注入 DbContext
        public StatisticalAnalysisTask(ILogger<StatisticalAnalysisTask> logger, IServiceScopeFactory scopeFactory) : base(logger)
        {
            _scopeFactory = scopeFactory;
        }

        protected override TimeSpan Interval => TimeSpan.FromDays(1); // 每天执行一次

        protected override async Task ExecuteTaskAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<CampusTradeDbContext>();

                // 这里添加统计分析逻辑，例如计算每日订单量、销售额等
                var dailyOrders = await context.Orders
                    .Where(o => o.CreateTime.Date == System.DateTime.Now.Date)
                    .CountAsync();

                // 可以将统计结果保存到数据库或日志中
                _logger.LogInformation($"今日订单量: {dailyOrders}");
            }
        }
    }
}
