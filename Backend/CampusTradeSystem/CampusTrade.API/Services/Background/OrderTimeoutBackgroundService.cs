using CampusTrade.API.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.Background
{
    /// <summary>
    /// 订单超时监控后台服务
    /// 定期检查并处理过期订单
    /// </summary>
    public class OrderTimeoutBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderTimeoutBackgroundService> _logger;

        // 检查间隔（1分钟）
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

        public OrderTimeoutBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<OrderTimeoutBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("订单超时监控服务已启动");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredOrdersAsync();
                    await Task.Delay(CheckInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // 服务被取消时正常退出
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "订单超时监控服务执行时发生错误");
                    // 发生错误时等待一段时间再继续
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("订单超时监控服务已停止");
        }

        /// <summary>
        /// 处理过期订单
        /// </summary>
        private async Task ProcessExpiredOrdersAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

            try
            {
                var processedCount = await orderService.ProcessExpiredOrdersAsync();

                if (processedCount > 0)
                {
                    _logger.LogInformation("处理了 {Count} 个过期订单", processedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理过期订单时发生错误");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("正在停止订单超时监控服务...");
            await base.StopAsync(cancellationToken);
        }
    }
}
