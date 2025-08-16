using System;
using System.Threading;
using System.Threading.Tasks;
using CampusTrade.API.Options;
using CampusTrade.API.Services.Cache;
using CampusTrade.API.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CampusTrade.API.Services.Background
{
    /// <summary>
    /// 后台定时30minutes,刷新各缓存内容
    /// 其它三类定时刷新，user类只登记命中率，按需刷新
    /// </summary>
    public class CacheRefreshBackgroundService : BackgroundService
    {
        private readonly ILogger<CacheRefreshBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly CacheOptions _options;

        public CacheRefreshBackgroundService(
            ILogger<CacheRefreshBackgroundService> logger,
            IOptions<CacheOptions> options,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _options = options.Value;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 刷新各部分缓存
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Cache Refresh Service is starting with initial delay of {InitialDelay}", _options.InitialDelaySeconds);

            try
            {
                // 初始延迟
                await Task.Delay(_options.InitialDelaySeconds, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Cache Refresh Service was cancelled during initial delay");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting cache refresh cycle at: {Time}", DateTimeOffset.Now);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        // 1. 刷新分类缓存
                        var categoryCache = scope.ServiceProvider.GetRequiredService<ICategoryCacheService>();
                        await categoryCache.RefreshCategoryTreeAsync();
                        _logger.LogInformation("Category tree cache refreshed");

                        // 2. 刷新活跃商品缓存
                        var productCache = scope.ServiceProvider.GetRequiredService<IProductCacheService>();
                        await productCache.RefreshAllActiveProductsAsync();
                        _logger.LogInformation("Active products cache refreshed");

                        // 3. 刷新系统配置缓存
                        var configCache = scope.ServiceProvider.GetRequiredService<ISystemConfigCacheService>();
                        await configCache.RefreshJwtOptionsAsync();
                        await configCache.RefreshCacheOptionsAsync();
                        _logger.LogInformation("System config cache refreshed");

                        // 4. 用户缓存按需刷新（通常由事件驱动，这里只记录命中率）
                        var userCache = scope.ServiceProvider.GetRequiredService<IUserCacheService>();
                        var hitRate = await userCache.GetHitRate();
                        _logger.LogInformation("User cache hit rate: {HitRate:P}", hitRate);
                    }

                    _logger.LogInformation("Cache refresh cycle completed at: {Time}", DateTimeOffset.Now);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Cache refresh cycle was cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during cache refresh");
                }

                try
                {
                    // 使用配置的间隔时间
                    await Task.Delay(_options.IntervalMinutes, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Cache Refresh Service was cancelled during interval delay");
                    break;
                }
            }
        }

        /// <summary>
        /// 停止缓存功能
        /// </summary>
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Cache Refresh Service is stopping");
            await base.StopAsync(stoppingToken);
        }
    }
}
