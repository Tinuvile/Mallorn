using System.Threading.Tasks;
using CampusTrade.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.ScheduledTasks
{
    public class UserCreditScoreCalculationTask : ScheduledService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        // 注入 IServiceScopeFactory 而非直接注入 DbContext
        public UserCreditScoreCalculationTask(ILogger<UserCreditScoreCalculationTask> logger, IServiceScopeFactory scopeFactory) : base(logger)
        {
            _scopeFactory = scopeFactory;
        }

        protected override TimeSpan Interval => TimeSpan.FromDays(7); // 每周执行一次

        protected override async Task ExecuteTaskAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<CampusTradeDbContext>();

                var users = await context.Users.ToListAsync();
                foreach (var user in users)
                {
                    // 这里添加信用分计算逻辑
                    user.CreditScore = 80; // 示例，可根据实际业务修改
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
