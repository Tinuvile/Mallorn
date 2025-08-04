using System;
using System.Threading.Tasks;
using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.ScheduledTasks
{
    public class ProductManagementTask : ScheduledService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        // 注入IServiceScopeFactory
        public ProductManagementTask(ILogger<ProductManagementTask> logger, IServiceScopeFactory scopeFactory) : base(logger)
        {
            _scopeFactory = scopeFactory;
        }

        protected override TimeSpan Interval => TimeSpan.FromDays(1); // 每天执行一次

        protected override async Task ExecuteTaskAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<CampusTradeDbContext>();

                var cutoffDate = DateTime.Now.AddDays(-30); // 下架 30 天未更新的商品
                var productsToDeactivate = await context.Products
                    .Where(p => p.PublishTime < cutoffDate && p.Status == Product.ProductStatus.OnSale)
                    .ToListAsync();

                foreach (var product in productsToDeactivate)
                {
                    product.Status = Product.ProductStatus.OffShelf;
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
