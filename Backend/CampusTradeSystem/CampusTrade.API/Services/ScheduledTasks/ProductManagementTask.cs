using CampusTrade.API.Services.Product;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.ScheduledTasks
{
    /// <summary>
    /// 商品管理定时任务
    /// 负责智能下架功能：默认20天自动下架，高浏览量商品延期10天
    /// </summary>
    public class ProductManagementTask : ScheduledService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ProductManagementTask(ILogger<ProductManagementTask> logger, IServiceScopeFactory scopeFactory)
            : base(logger)
        {
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// 每天执行一次智能下架检查
        /// </summary>
        protected override TimeSpan Interval => TimeSpan.FromDays(1);

        /// <summary>
        /// 执行商品管理任务
        /// </summary>
        protected override async Task ExecuteTaskAsync()
        {
            _logger.LogInformation("开始执行商品智能下架任务");

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

                // 执行智能下架逻辑
                var (processedCount, successCount, extendedCount) = await productService.ProcessAutoRemoveProductsAsync();

                _logger.LogInformation(
                    "商品智能下架任务执行完成 - 处理商品数: {ProcessedCount}, 下架成功: {SuccessCount}, 延期处理: {ExtendedCount}",
                    processedCount, successCount, extendedCount);

                // 如果有商品被处理，记录详细信息
                if (processedCount > 0)
                {
                    _logger.LogInformation(
                        "智能下架处理结果: 共处理 {ProcessedCount} 个商品，其中 {SuccessCount} 个成功下架，{ExtendedCount} 个高浏览量商品获得延期",
                        processedCount, successCount, extendedCount);
                }
                else
                {
                    _logger.LogInformation("没有需要下架的商品");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "商品智能下架任务执行失败");
                throw; // 重新抛出异常，让ScheduledService处理
            }
        }

        /// <summary>
        /// 任务执行出错时的处理
        /// </summary>
        protected override Task OnTaskErrorAsync(Exception exception)
        {
            _logger.LogError(exception, "商品管理定时任务执行出错");

            return base.OnTaskErrorAsync(exception);
        }

        /// <summary>
        /// 获取任务状态信息
        /// </summary>
        public override object GetTaskStatus()
        {
            var baseStatus = base.GetTaskStatus();

            return new
            {
                TaskName = "ProductManagementTask",
                Description = "商品智能下架管理",
                Features = new[]
                {
                    "默认20天自动下架",
                    "高浏览量商品延期10天",
                    "批量处理优化",
                    "缓存自动更新"
                },
                BaseStatus = baseStatus
            };
        }
    }
}
