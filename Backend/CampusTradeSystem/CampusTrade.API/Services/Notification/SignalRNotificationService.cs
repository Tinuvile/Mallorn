using CampusTrade.API.Data;
using CampusTrade.API.Infrastructure.Hubs;
using CampusTrade.API.Infrastructure.Utils.Notificate;
using CampusTrade.API.Models.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.Notification
{
    /// <summary>
    /// SignalR通知发送服务
    /// </summary>
    public class SignalRNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly CampusTradeDbContext _context;
        private readonly ILogger<SignalRNotificationService> _logger;

        public SignalRNotificationService(
            IHubContext<NotificationHub> hubContext,
            CampusTradeDbContext context,
            ILogger<SignalRNotificationService> logger)
        {
            _hubContext = hubContext;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 发送SignalR通知
        /// </summary>
        /// <param name="notification">通知实体</param>
        /// <param name="content">渲染后的内容</param>
        /// <returns>发送结果</returns>
        public async Task<(bool Success, string ErrorMessage)> SendSignalRNotificationAsync(
            Models.Entities.Notification notification, 
            string content)
        {
            var signalRNotification = new SignalRNotification
            {
                NotificationId = notification.NotificationId,
                SendStatus = SignalRNotification.SendStatuses.Pending,
                CreatedAt = DateTime.UtcNow,
                LastAttemptTime = DateTime.UtcNow
            };

            try
            {
                // 保存SignalR通知记录
                _context.SignalRNotifications.Add(signalRNotification);
                await _context.SaveChangesAsync();

                // 发送SignalR消息
                var sendResult = await SendSignalRMessageAsync(
                    notification.RecipientId, 
                    notification.Template.TemplateName, 
                    content,
                    signalRNotification.SignalRNotificationId);

                if (sendResult.Success)
                {
                    // 发送成功，更新状态
                    signalRNotification.SendStatus = SignalRNotification.SendStatuses.Success;
                    signalRNotification.SentAt = DateTime.UtcNow;
                    
                    _logger.LogInformation($"SignalR通知发送成功 - NotificationId: {notification.NotificationId}, " +
                                         $"RecipientId: {notification.RecipientId}");
                }
                else
                {
                    // 发送失败，增加重试次数
                    signalRNotification.RetryCount++;
                    signalRNotification.SendStatus = signalRNotification.RetryCount >= SignalRNotification.MaxRetryCount 
                        ? SignalRNotification.SendStatuses.Failed 
                        : SignalRNotification.SendStatuses.Pending;
                    signalRNotification.ErrorMessage = sendResult.ErrorMessage;
                    signalRNotification.LastAttemptTime = DateTime.UtcNow;

                    _logger.LogWarning($"SignalR通知发送失败 - NotificationId: {notification.NotificationId}, " +
                                     $"RetryCount: {signalRNotification.RetryCount}, Error: {sendResult.ErrorMessage}");
                }

                await _context.SaveChangesAsync();
                return sendResult;
            }
            catch (Exception ex)
            {
                var errorMsg = $"SignalR通知发送异常: {ex.Message}";
                _logger.LogError(ex, $"SignalR通知发送异常 - NotificationId: {notification.NotificationId}");

                // 更新错误状态
                try
                {
                    signalRNotification.SendStatus = SignalRNotification.SendStatuses.Failed;
                    signalRNotification.ErrorMessage = errorMsg;
                    signalRNotification.RetryCount++;
                    signalRNotification.LastAttemptTime = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, $"保存SignalR通知错误状态失败 - NotificationId: {notification.NotificationId}");
                }

                return (false, errorMsg);
            }
        }

        /// <summary>
        /// 发送SignalR消息到用户
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="title">消息标题</param>
        /// <param name="content">消息内容</param>
        /// <param name="signalRNotificationId">SignalR通知记录ID</param>
        /// <returns>发送结果</returns>
        private async Task<(bool Success, string ErrorMessage)> SendSignalRMessageAsync(
            int userId, 
            string title, 
            string content,
            int signalRNotificationId)
        {
            try
            {
                // 构建通知消息
                var message = new
                {
                    Id = signalRNotificationId,
                    Title = title,
                    Content = content,
                    Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    UserId = userId
                };

                // 发送到用户组
                await _hubContext.Clients.Group($"user_{userId}")
                    .SendAsync("ReceiveNotification", message);

                _logger.LogDebug($"SignalR消息发送成功 - UserId: {userId}, Title: {title}");
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                var errorMsg = $"SignalR消息发送失败: {ex.Message}";
                _logger.LogError(ex, $"SignalR消息发送失败 - UserId: {userId}, Title: {title}");
                return (false, errorMsg);
            }
        }

        /// <summary>
        /// 重试失败的SignalR通知
        /// </summary>
        /// <param name="batchSize">批次大小</param>
        /// <returns>处理结果</returns>
        public async Task<(int Total, int Success, int Failed)> RetryFailedSignalRNotificationsAsync(int batchSize = 10)
        {
            var retryTime = DateTime.UtcNow.AddMinutes(-SignalRNotification.DefaultRetryIntervalMinutes);

            var failedNotifications = await _context.SignalRNotifications
                .Include(sr => sr.Notification)
                .ThenInclude(n => n.Template)
                .Where(sr => sr.SendStatus == SignalRNotification.SendStatuses.Pending &&
                           sr.RetryCount > 0 &&
                           sr.RetryCount < SignalRNotification.MaxRetryCount &&
                           sr.LastAttemptTime < retryTime)
                .OrderBy(sr => sr.LastAttemptTime)
                .Take(batchSize)
                .ToListAsync();

            int successCount = 0;
            int failedCount = 0;

            _logger.LogInformation($"开始重试失败的SignalR通知，共 {failedNotifications.Count} 条");

            foreach (var signalRNotification in failedNotifications)
            {
                try
                {
                    var content = signalRNotification.Notification.GetRenderedContent();
                    var result = await SendSignalRMessageAsync(
                        signalRNotification.Notification.RecipientId,
                        signalRNotification.Notification.Template.TemplateName,
                        content,
                        signalRNotification.SignalRNotificationId);

                    if (result.Success)
                    {
                        signalRNotification.SendStatus = SignalRNotification.SendStatuses.Success;
                        signalRNotification.SentAt = DateTime.UtcNow;
                        successCount++;
                    }
                    else
                    {
                        signalRNotification.RetryCount++;
                        signalRNotification.SendStatus = signalRNotification.RetryCount >= SignalRNotification.MaxRetryCount
                            ? SignalRNotification.SendStatuses.Failed
                            : SignalRNotification.SendStatuses.Pending;
                        signalRNotification.ErrorMessage = result.ErrorMessage;
                        failedCount++;
                    }

                    signalRNotification.LastAttemptTime = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // 避免过于频繁的重试
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"重试SignalR通知异常 - SignalRNotificationId: {signalRNotification.SignalRNotificationId}");
                    failedCount++;
                }
            }

            _logger.LogInformation($"SignalR通知重试完成 - 总计: {failedNotifications.Count}, 成功: {successCount}, 失败: {failedCount}");
            return (failedNotifications.Count, successCount, failedCount);
        }

        /// <summary>
        /// 获取SignalR通知状态统计
        /// </summary>
        /// <returns>统计信息</returns>
        public async Task<(int Pending, int Success, int Failed, int Total)> GetSignalRStatsAsync()
        {
            var pending = await _context.SignalRNotifications
                .CountAsync(sr => sr.SendStatus == SignalRNotification.SendStatuses.Pending);

            var success = await _context.SignalRNotifications
                .CountAsync(sr => sr.SendStatus == SignalRNotification.SendStatuses.Success);

            var failed = await _context.SignalRNotifications
                .CountAsync(sr => sr.SendStatus == SignalRNotification.SendStatuses.Failed);

            var total = pending + success + failed;

            return (pending, success, failed, total);
        }
    }
}
