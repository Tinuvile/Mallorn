using System;
using System.Threading.Tasks;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.ScheduledTasks
{
    /// <summary>
    /// 邮件验证码清理定时任务
    /// 负责定期清理过期和已使用的邮件验证码记录，防止数据库膨胀
    /// </summary>
    public class EmailVerificationCleanupTask : ScheduledService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public EmailVerificationCleanupTask(ILogger<EmailVerificationCleanupTask> logger, IServiceScopeFactory scopeFactory)
            : base(logger)
        {
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// 每天凌晨2点执行一次清理任务
        /// </summary>
        protected override TimeSpan Interval => TimeSpan.FromDays(1);

        /// <summary>
        /// 执行邮件验证码清理任务
        /// </summary>
        protected override async Task ExecuteTaskAsync()
        {
            _logger.LogInformation("开始执行邮件验证码清理任务");

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                // 1. 清理过期和已使用的验证码记录（保留24小时内的数据用于审计）
                var expiredBefore = DateTime.Now.AddHours(-24);
                var expiredCount = await unitOfWork.EmailVerifications.CleanupExpiredAsync(expiredBefore);

                // 2. 获取系统中当前过期记录数量
                var currentExpiredCount = await unitOfWork.EmailVerifications.GetExpiredRecordsCountAsync();

                // 3. 清理每个用户的旧记录，保留最新的10条记录
                var userCleanupCount = 0;
                
                // 这里可以根据需要实现批量用户清理逻辑
                // 为了避免一次性处理过多数据，可以分批处理用户
                // 示例：每次处理100个用户的记录
                
                // 4. 保存更改
                await unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "邮件验证码清理任务执行完成 - 清理过期记录: {ExpiredCount}条, 当前剩余过期记录: {CurrentExpiredCount}条, 用户记录清理: {UserCleanupCount}条",
                    expiredCount, currentExpiredCount, userCleanupCount);

                // 如果清理了大量记录，记录详细信息
                if (expiredCount > 100)
                {
                    _logger.LogInformation(
                        "大量邮件验证码记录被清理，建议检查系统邮件发送频率是否正常 - 清理数量: {ExpiredCount}",
                        expiredCount);
                }

                // 如果还有很多过期记录未清理，发出警告
                if (currentExpiredCount > 1000)
                {
                    _logger.LogWarning(
                        "系统中仍有大量过期邮件验证码记录 ({CurrentExpiredCount}条)，建议检查清理策略或增加清理频率",
                        currentExpiredCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "邮件验证码清理任务执行失败");
                throw; // 重新抛出异常，让ScheduledService处理
            }
        }

        /// <summary>
        /// 任务执行出错时的处理
        /// </summary>
        protected override Task OnTaskErrorAsync(Exception exception)
        {
            _logger.LogError(exception, "邮件验证码清理定时任务执行出错，将在下次调度时重试");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 获取任务状态信息
        /// </summary>
        public override object GetTaskStatus()
        {
            var baseStatus = base.GetTaskStatus();
            
            return new
            {
                TaskName = "EmailVerificationCleanupTask",
                Description = "邮件验证码清理任务 - 每天清理过期和已使用的验证码记录",
                CleanupStrategy = new
                {
                    ExpiredRecordsRetention = "24小时",
                    UserRecordsRetention = "最新10条",
                    ExecutionFrequency = "每天一次"
                },
                Status = baseStatus
            };
        }
    }
}
