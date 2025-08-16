using System;
using System.Threading.Tasks;
using CampusTrade.API.Data;
using CampusTrade.API.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.ScheduledTasks
{
    public class TokenCleanupTask : ScheduledService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        // 注入 IServiceScopeFactory 而非直接注入 DbContext
        public TokenCleanupTask(ILogger<TokenCleanupTask> logger, IServiceScopeFactory scopeFactory) : base(logger)
        {
            _scopeFactory = scopeFactory;
        }

        protected override TimeSpan Interval => TimeSpan.FromHours(1); // 每小时执行一次

        protected override async Task ExecuteTaskAsync()
        {
            // 手动创建作用域（每次执行任务时创建，用完自动释放）
            using (var scope = _scopeFactory.CreateScope())
            {
                // 从当前作用域中获取需要的服务（此时服务生命周期与作用域绑定）
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

                // 执行清理逻辑（TokenService 依赖的 DbContext 会在当前作用域内生效）
                await tokenService.CleanupExpiredTokensAsync();
            }
        }
    }
}
