using System;
using System.Threading;
using System.Threading.Tasks;
using CampusTrade.API.Services.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.Background
{
    /// <summary>
    /// 通知发送后台服务 - 持续监控队列并发送通知
    /// </summary>
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationBackgroundService> _logger;
        private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(10); // 每10秒检查一次
        private readonly TimeSpan _retryInterval = TimeSpan.FromMinutes(5); // 每5分钟重试一次失败的通知

        // 新增：用于立即触发处理的信号量
        private readonly SemaphoreSlim _processingSignal = new SemaphoreSlim(0);
        private static NotificationBackgroundService? _instance;

        public NotificationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<NotificationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _instance = this; // 设置单例引用，供外部触发使用
        }

        /// <summary>
        /// 外部调用此方法来立即触发通知处理
        /// </summary>
        public static void TriggerProcessing()
        {
            _instance?._processingSignal.Release();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("通知发送后台服务启动");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var senderService = scope.ServiceProvider.GetRequiredService<NotifiSenderService>();

                    // 1. 处理待发送通知
                    var (totalPending, successPending, failedPending) = await senderService.ProcessNotificationQueueAsync(20);

                    if (totalPending > 0)
                    {
                        _logger.LogInformation($"处理待发送通知: 总计{totalPending}条, 成功{successPending}条, 失败{failedPending}条");
                    }

                    // 2. 每分钟重试一次失败的通知
                    if (DateTime.Now.Second < 10) // 大约每分钟的前10秒执行重试
                    {
                        var (totalRetry, successRetry, failedRetry) = await senderService.RetryFailedNotificationsAsync(10);

                        if (totalRetry > 0)
                        {
                            _logger.LogInformation($"重试失败通知: 总计{totalRetry}条, 成功{successRetry}条, 失败{failedRetry}条");
                        }
                    }

                    // 3. 每5分钟输出一次队列状态
                    if (DateTime.Now.Minute % 5 == 0 && DateTime.Now.Second < 10)
                    {
                        var (pending, success, failed, total) = await senderService.GetQueueStatsAsync();
                        _logger.LogInformation($"队列状态 - 待发送: {pending}, 已成功: {success}, 已失败: {failed}, 总计: {total}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "通知发送后台服务执行异常");
                }

                // 等待下一次执行：要么等待10秒，要么等待外部触发信号
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                cts.CancelAfter(_processingInterval);

                try
                {
                    await _processingSignal.WaitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // 超时或取消，继续下一次循环
                }
            }

            _logger.LogInformation("通知发送后台服务停止");
        }
    }
}
