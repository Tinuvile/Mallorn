using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusTrade.API.Data;
using CampusTrade.API.Infrastructure.Hubs;
using CampusTrade.API.Infrastructure.Utils.Notificate;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Services.Email;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.Auth
{
    /// <summary>
    /// 通知发送器服务 - 负责从队列中取出通知并发送
    /// </summary>
    public class NotifiSenderService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly CampusTradeDbContext _context;
        private readonly EmailService _emailService;
        private readonly ILogger<NotifiSenderService> _logger;

        public NotifiSenderService(
            IHubContext<NotificationHub> hubContext,
            CampusTradeDbContext context,
            EmailService emailService,
            ILogger<NotifiSenderService> logger)
        {
            _hubContext = hubContext;
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// 发送单个通知
        /// </summary>
        /// <param name="notificationId">通知ID</param>
        /// <returns>发送结果</returns>
        public async Task<(bool Success, string ErrorMessage)> SendNotificationAsync(int notificationId)
        {
            try
            {
                // 获取通知详情
                var notification = await _context.Notifications
                    .Include(n => n.Template)
                    .Include(n => n.Recipient)
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId);

                if (notification == null)
                {
                    return (false, "通知不存在");
                }

                // 如果已经发送成功，直接返回
                if (notification.SendStatus == Notification.SendStatuses.Success)
                {
                    return (true, "通知已发送");
                }

                // 生成渲染后的内容
                string content;
                try
                {
                    content = notification.GetRenderedContent();
                }
                catch (Exception ex)
                {
                    var errorMsg = $"内容渲染失败: {ex.Message}";
                    await UpdateNotificationStatus(notificationId, false, errorMsg);
                    return (false, errorMsg);
                }

                // 1. 通过SignalR发送消息到前端
                var pushSuccess = await PushNotificationToUser(
                    notification.RecipientId,
                    notification.Template.TemplateName,
                    content
                );

                // 2. 发送邮件通知（如果用户有有效邮箱）
                var emailSuccess = false;
                var emailResultMsg = string.Empty;

                if (!string.IsNullOrEmpty(notification.Recipient.Email))
                {
                    var emailResult = await _emailService.SendEmailAsync(
                        notification.Recipient.Email,
                        notification.Template.TemplateName, // 邮件主题使用模板名称
                        content // 邮件内容使用渲染后的通知内容
                    );

                    emailSuccess = emailResult.Success;
                    emailResultMsg = emailResult.Message;

                    _logger.LogInformation($"邮件通知结果 - NotificationId: {notificationId}, " +
                                           $"收件人: {notification.Recipient.Email}, " +
                                           $"结果: {(emailSuccess ? "成功" : "失败")}, " +
                                           $"消息: {emailResultMsg}");
                }
                else
                {
                    _logger.LogWarning($"用户 {notification.RecipientId} 没有设置邮箱，跳过邮件通知");
                }

                // 3. 只要有一种方式发送成功，就认为通知发送成功
                var isSuccess = pushSuccess || emailSuccess;
                var resultMessage = isSuccess ? "通知发送成功" : "通知发送失败";

                if (pushSuccess && emailSuccess)
                {
                    resultMessage = "Web端和邮箱通知均发送成功";
                }
                else if (pushSuccess)
                {
                    resultMessage = "Web端通知发送成功，邮箱通知失败或未发送";
                }
                else if (emailSuccess)
                {
                    resultMessage = "邮箱通知发送成功，Web端通知失败";
                }

                // 更新通知状态
                await UpdateNotificationStatus(notificationId, isSuccess, isSuccess ? null : "通知发送失败");

                return (isSuccess, resultMessage);
            }
            catch (Exception ex)
            {
                var errorMsg = $"发送通知异常: {ex.Message}";
                Console.WriteLine($"[ERROR] {errorMsg}");
                await UpdateNotificationStatus(notificationId, false, errorMsg);
                return (false, errorMsg);
            }
        }

        /// <summary>
        /// 通过SignalR推送消息到用户
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="title">消息标题</param>
        /// <param name="content">消息内容</param>
        /// <returns>推送结果</returns>
        private async Task<bool> PushNotificationToUser(int userId, string title, string content)
        {
            try
            {
                await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", new
                {
                    Title = title,
                    Content = content,
                    Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });

                Console.WriteLine($"[INFO] SignalR推送成功 - 用户: {userId}, 内容: {content}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SignalR推送失败 - 用户: {userId}, 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 更新通知状态
        /// </summary>
        /// <param name="notificationId">通知ID</param>
        /// <param name="success">是否成功</param>
        /// <param name="errorMessage">错误消息</param>
        private async Task UpdateNotificationStatus(int notificationId, bool success, string? errorMessage = null)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null) return;

            notification.LastAttemptTime = DateTime.Now;

            if (success)
            {
                notification.SendStatus = Notification.SendStatuses.Success;
                notification.SentAt = DateTime.Now;
                Console.WriteLine($"[INFO] 通知发送成功 - NotificationId: {notificationId}");
            }
            else
            {
                notification.RetryCount++;
                if (notification.RetryCount >= Notification.MaxRetryCount)
                {
                    notification.SendStatus = Notification.SendStatuses.Failed;
                    Console.WriteLine($"[ERROR] 通知发送失败，超过最大重试次数 - NotificationId: {notificationId}, RetryCount: {notification.RetryCount}, Error: {errorMessage}");
                }
                else
                {
                    Console.WriteLine($"[WARN] 通知发送失败，将重试 - NotificationId: {notificationId}, RetryCount: {notification.RetryCount}, Error: {errorMessage}");
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 批量处理待发送的通知队列
        /// </summary>
        /// <param name="batchSize">批次大小</param>
        /// <returns>处理结果统计</returns>
        public async Task<(int Total, int Success, int Failed)> ProcessNotificationQueueAsync(int batchSize = 10)
        {
            // 获取待发送的通知，按优先级和时间排序
            var pendingNotifications = await _context.Notifications
                .Include(n => n.Template)
                .Where(n => n.SendStatus == Notification.SendStatuses.Pending &&
                           n.RetryCount < Notification.MaxRetryCount)
                .OrderByDescending(n => n.Template.Priority)  // 优先级高的优先
                .ThenBy(n => n.LastAttemptTime)  // 然后按最后尝试时间排序
                .ThenBy(n => n.CreatedAt)  // 最后按创建时间排序
                .Take(batchSize)
                .ToListAsync();

            int successCount = 0;
            int failedCount = 0;

            Console.WriteLine($"[INFO] 开始处理通知队列，共 {pendingNotifications.Count} 条待发送通知");

            foreach (var notification in pendingNotifications)
            {
                var result = await SendNotificationAsync(notification.NotificationId);
                if (result.Success)
                    successCount++;
                else
                    failedCount++;

                // 避免过于频繁的发送
                await Task.Delay(100);
            }

            Console.WriteLine($"[INFO] 通知队列处理完成 - 总计: {pendingNotifications.Count}, 成功: {successCount}, 失败: {failedCount}");
            return (pendingNotifications.Count, successCount, failedCount);
        }

        /// <summary>
        /// 重试失败的通知
        /// </summary>
        /// <param name="batchSize">批次大小</param>
        /// <returns>处理结果统计</returns>
        public async Task<(int Total, int Success, int Failed)> RetryFailedNotificationsAsync(int batchSize = 5)
        {
            var retryTime = DateTime.Now.AddMinutes(-Notification.DefaultRetryIntervalMinutes);

            var failedNotifications = await _context.Notifications
                .Include(n => n.Template)
                .Where(n => n.SendStatus == Notification.SendStatuses.Pending &&
                           n.RetryCount > 0 &&
                           n.RetryCount < Notification.MaxRetryCount &&
                           n.LastAttemptTime < retryTime)
                .OrderByDescending(n => n.Template.Priority)
                .ThenBy(n => n.LastAttemptTime)
                .Take(batchSize)
                .ToListAsync();

            int successCount = 0;
            int failedCount = 0;

            Console.WriteLine($"[INFO] 开始重试失败通知，共 {failedNotifications.Count} 条");

            foreach (var notification in failedNotifications)
            {
                var result = await SendNotificationAsync(notification.NotificationId);
                if (result.Success)
                    successCount++;
                else
                    failedCount++;

                await Task.Delay(200);
            }

            Console.WriteLine($"[INFO] 重试失败通知完成 - 总计: {failedNotifications.Count}, 成功: {successCount}, 失败: {failedCount}");
            return (failedNotifications.Count, successCount, failedCount);
        }

        /// <summary>
        /// 获取通知队列状态统计
        /// </summary>
        /// <returns>统计信息</returns>
        public async Task<(int Pending, int Success, int Failed, int Total)> GetQueueStatsAsync()
        {
            var pending = await _context.Notifications
                .CountAsync(n => n.SendStatus == Notification.SendStatuses.Pending);

            var success = await _context.Notifications
                .CountAsync(n => n.SendStatus == Notification.SendStatuses.Success);

            var failed = await _context.Notifications
                .CountAsync(n => n.SendStatus == Notification.SendStatuses.Failed);

            var total = pending + success + failed;

            return (pending, success, failed, total);
        }
    }
}
