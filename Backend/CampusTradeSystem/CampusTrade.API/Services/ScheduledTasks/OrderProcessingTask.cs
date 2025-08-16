using CampusTrade.API.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.ScheduledTasks
{
    /// <summary>
    /// 订单处理定时任务
    /// 定期处理过期订单和订单状态维护
    /// </summary>
    public class OrderProcessingTask : ScheduledService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public OrderProcessingTask(ILogger<OrderProcessingTask> logger, IServiceScopeFactory scopeFactory)
            : base(logger)
        {
            _scopeFactory = scopeFactory;
        }

        protected override TimeSpan Interval => TimeSpan.FromHours(6); // 每 6 小时执行一次

        protected override async Task ExecuteTaskAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

            try
            {
                // 处理过期订单
                var processedCount = await orderService.ProcessExpiredOrdersAsync();

                if (processedCount > 0)
                {
                    _logger.LogInformation("定时任务处理了 {Count} 个过期订单", processedCount);
                }
                else
                {
                    _logger.LogInformation("定时任务未发现需要处理的过期订单");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "订单处理定时任务执行失败");
                throw;
            }
        }

        protected override async Task OnTaskErrorAsync(Exception exception)
        {
            _logger.LogError(exception, "订单处理定时任务发生错误，将在下次调度时重试");
            await Task.CompletedTask;
        }
    }
}
