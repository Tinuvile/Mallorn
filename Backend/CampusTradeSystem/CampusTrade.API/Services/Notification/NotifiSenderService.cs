using System;
using System.Linq;
using System.Threading.Tasks;
using CampusTrade.API.Data;
using CampusTrade.API.Infrastructure.Utils.Notificate;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.Notification
{
    /// <summary>
    /// 通知发送器服务 - 协调SignalR和Email发送服务
    /// </summary>
    public class NotifiSenderService
    {
        private readonly CampusTradeDbContext _context;
        private readonly SignalRNotificationService _signalRService;
        private readonly EmailService _emailService;
        private readonly ILogger<NotifiSenderService> _logger;

        public NotifiSenderService(
            CampusTradeDbContext context,
            SignalRNotificationService signalRService,
            EmailService emailService,
            ILogger<NotifiSenderService> logger)
        {
            _context = context;
            _signalRService = signalRService;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// 发送单个通知
        /// </summary>
        /// <param name="notificationId">通知ID</param>
        /// <param name="forceResend">是否强制重新发送所有渠道</param>
        /// <returns>发送结果</returns>
        public async Task<(bool Success, string ErrorMessage)> SendNotificationAsync(int notificationId, bool forceResend = false)
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
                if (notification.SendStatus == Models.Entities.Notification.SendStatuses.Success && !forceResend)
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

                // 检查各渠道的当前状态，只发送失败的渠道
                var (signalRResult, emailResult) = await SendNotificationChannelsAsync(notification, content, forceResend);

                // 判断整体发送结果
                bool hasEmail = !string.IsNullOrEmpty(notification.Recipient.Email);
                bool overallSuccess;
                string resultMessage;

                if (hasEmail)
                {
                    // 用户有邮箱：两个渠道都必须成功
                    overallSuccess = signalRResult.Success && emailResult.Success;
                    if (overallSuccess)
                    {
                        resultMessage = "SignalR和邮件通知均发送成功";
                    }
                    else if (signalRResult.Success && !emailResult.Success)
                    {
                        resultMessage = $"SignalR通知发送成功，邮件通知失败: {emailResult.ErrorMessage}";
                    }
                    else if (!signalRResult.Success && emailResult.Success)
                    {
                        resultMessage = $"邮件通知发送成功，SignalR通知失败: {signalRResult.ErrorMessage}";
                    }
                    else
                    {
                        resultMessage = $"两种通知方式均失败 - SignalR: {signalRResult.ErrorMessage}, 邮件: {emailResult.ErrorMessage}";
                    }
                }
                else
                {
                    // 用户没有邮箱：只需SignalR成功
                    overallSuccess = signalRResult.Success;
                    resultMessage = overallSuccess 
                        ? "SignalR通知发送成功（用户未设置邮箱）" 
                        : $"SignalR通知发送失败: {signalRResult.ErrorMessage}";
                }

                // 更新主通知状态
                await UpdateNotificationStatus(notificationId, overallSuccess, overallSuccess ? null : resultMessage);

                _logger.LogInformation($"通知发送完成 - NotificationId: {notificationId}, " +
                                     $"Overall: {(overallSuccess ? "成功" : "失败")}, " +
                                     $"SignalR: {(signalRResult.Success ? "成功" : "失败")}, " +
                                     $"Email: {(emailResult.Success ? "成功" : "失败")}, " +
                                     $"Message: {resultMessage}");

                return (overallSuccess, resultMessage);
            }
            catch (Exception ex)
            {
                var errorMsg = $"发送通知异常: {ex.Message}";
                _logger.LogError(ex, $"发送通知异常 - NotificationId: {notificationId}");
                await UpdateNotificationStatus(notificationId, false, errorMsg);
                return (false, errorMsg);
            }
        }

        /// <summary>
        /// 智能发送通知渠道（只发送失败的渠道）
        /// </summary>
        /// <param name="notification">通知实体</param>
        /// <param name="content">渲染后的内容</param>
        /// <param name="forceResend">是否强制重新发送所有渠道</param>
        /// <returns>各渠道发送结果</returns>
        private async Task<((bool Success, string ErrorMessage), (bool Success, string ErrorMessage))> 
            SendNotificationChannelsAsync(Models.Entities.Notification notification, string content, bool forceResend)
        {
            // 检查SignalR渠道状态
            var existingSignalR = await _context.SignalRNotifications
                .FirstOrDefaultAsync(sr => sr.NotificationId == notification.NotificationId);
            
            bool needSendSignalR = forceResend || existingSignalR == null || 
                                  existingSignalR.SendStatus != SignalRNotification.SendStatuses.Success;

            // 检查Email渠道状态
            var existingEmail = await _context.EmailNotifications
                .FirstOrDefaultAsync(en => en.NotificationId == notification.NotificationId);
            
            bool needSendEmail = forceResend || existingEmail == null || 
                                existingEmail.SendStatus != EmailNotification.SendStatuses.Success;

            // 用户没有邮箱的情况
            bool hasEmail = !string.IsNullOrEmpty(notification.Recipient.Email);
            if (!hasEmail)
            {
                needSendEmail = false;
            }

            // 并行发送需要发送的渠道
            Task<(bool Success, string ErrorMessage)> signalRTask;
            Task<(bool Success, string ErrorMessage)> emailTask;

            if (needSendSignalR)
            {
                _logger.LogInformation($"发送SignalR通知 - NotificationId: {notification.NotificationId}");
                signalRTask = _signalRService.SendSignalRNotificationAsync(notification, content);
            }
            else
            {
                _logger.LogInformation($"跳过SignalR通知（已成功） - NotificationId: {notification.NotificationId}");
                signalRTask = Task.FromResult((true, "已发送成功，跳过重复发送"));
            }

            if (needSendEmail && hasEmail)
            {
                _logger.LogInformation($"发送邮件通知 - NotificationId: {notification.NotificationId}");
                emailTask = _emailService.SendNotificationEmailAsync(notification, content);
            }
            else if (!hasEmail)
            {
                _logger.LogInformation($"跳过邮件通知（用户未设置邮箱） - NotificationId: {notification.NotificationId}");
                emailTask = Task.FromResult((true, "用户未设置邮箱，跳过邮件发送"));
            }
            else
            {
                _logger.LogInformation($"跳过邮件通知（已成功） - NotificationId: {notification.NotificationId}");
                emailTask = Task.FromResult((true, "已发送成功，跳过重复发送"));
            }

            // 等待结果
            var signalRResult = await signalRTask;
            var emailResult = await emailTask;

            return (signalRResult, emailResult);
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

            notification.LastAttemptTime = DateTime.UtcNow;

            if (success)
            {
                notification.SendStatus = Models.Entities.Notification.SendStatuses.Success;
                notification.SentAt = DateTime.UtcNow;
                _logger.LogInformation($"通知发送成功 - NotificationId: {notificationId}");
            }
            else
            {
                notification.RetryCount++;
                if (notification.RetryCount >= Models.Entities.Notification.MaxRetryCount)
                {
                    notification.SendStatus = Models.Entities.Notification.SendStatuses.Failed;
                    _logger.LogError($"通知发送失败，超过最大重试次数 - NotificationId: {notificationId}, " +
                                   $"RetryCount: {notification.RetryCount}, Error: {errorMessage}");
                }
                else
                {
                    _logger.LogWarning($"通知发送失败，将重试 - NotificationId: {notificationId}, " +
                                     $"RetryCount: {notification.RetryCount}, Error: {errorMessage}");
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
                .Where(n => n.SendStatus == Models.Entities.Notification.SendStatuses.Pending &&
                           n.RetryCount < Models.Entities.Notification.MaxRetryCount)
                .OrderByDescending(n => n.Template.Priority)  // 优先级高的优先
                .ThenBy(n => n.LastAttemptTime)  // 然后按最后尝试时间排序
                .ThenBy(n => n.CreatedAt)  // 最后按创建时间排序
                .Take(batchSize)
                .ToListAsync();

            int successCount = 0;
            int failedCount = 0;

            _logger.LogInformation($"开始处理通知队列，共 {pendingNotifications.Count} 条待发送通知");

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

            _logger.LogInformation($"通知队列处理完成 - 总计: {pendingNotifications.Count}, 成功: {successCount}, 失败: {failedCount}");
            return (pendingNotifications.Count, successCount, failedCount);
        }

        /// <summary>
        /// 重试失败的通知
        /// </summary>
        /// <param name="batchSize">批次大小</param>
        /// <returns>处理结果统计</returns>
        public async Task<(int Total, int Success, int Failed)> RetryFailedNotificationsAsync(int batchSize = 5)
        {
            var retryTime = DateTime.UtcNow.AddMinutes(-Models.Entities.Notification.DefaultRetryIntervalMinutes);

            var failedNotifications = await _context.Notifications
                .Include(n => n.Template)
                .Where(n => n.SendStatus == Models.Entities.Notification.SendStatuses.Pending &&
                           n.RetryCount > 0 &&
                           n.RetryCount < Models.Entities.Notification.MaxRetryCount &&
                           n.LastAttemptTime < retryTime)
                .OrderByDescending(n => n.Template.Priority)
                .ThenBy(n => n.LastAttemptTime)
                .Take(batchSize)
                .ToListAsync();

            int successCount = 0;
            int failedCount = 0;

            _logger.LogInformation($"开始重试失败通知，共 {failedNotifications.Count} 条");

            foreach (var notification in failedNotifications)
            {
                var result = await SendNotificationAsync(notification.NotificationId);
                if (result.Success)
                    successCount++;
                else
                    failedCount++;

                await Task.Delay(200);
            }

            _logger.LogInformation($"重试失败通知完成 - 总计: {failedNotifications.Count}, 成功: {successCount}, 失败: {failedCount}");
            return (failedNotifications.Count, successCount, failedCount);
        }

        /// <summary>
        /// 协调重试所有渠道的失败通知
        /// </summary>
        /// <param name="batchSize">批次大小</param>
        /// <returns>处理结果统计</returns>
        public async Task<(int TotalSignalR, int SuccessSignalR, int FailedSignalR, int TotalEmail, int SuccessEmail, int FailedEmail)> 
            RetryAllChannelFailuresAsync(int batchSize = 10)
        {
            // 重试SignalR失败的通知
            var signalRResult = await _signalRService.RetryFailedSignalRNotificationsAsync(batchSize);

            // 重试邮件失败的通知
            var emailResult = await _emailService.RetryFailedEmailNotificationsAsync(batchSize);

            _logger.LogInformation($"所有渠道重试完成 - SignalR: {signalRResult.Total}总/{signalRResult.Success}成功/{signalRResult.Failed}失败, " +
                                 $"Email: {emailResult.Total}总/{emailResult.Success}成功/{emailResult.Failed}失败");

            return (signalRResult.Total, signalRResult.Success, signalRResult.Failed,
                    emailResult.Total, emailResult.Success, emailResult.Failed);
        }

        /// <summary>
        /// 获取通知队列状态统计
        /// </summary>
        /// <returns>统计信息</returns>
        public async Task<(int Pending, int Success, int Failed, int Total)> GetQueueStatsAsync()
        {
            var pending = await _context.Notifications
                .CountAsync(n => n.SendStatus == Models.Entities.Notification.SendStatuses.Pending);

            var success = await _context.Notifications
                .CountAsync(n => n.SendStatus == Models.Entities.Notification.SendStatuses.Success);

            var failed = await _context.Notifications
                .CountAsync(n => n.SendStatus == Models.Entities.Notification.SendStatuses.Failed);

            var total = pending + success + failed;

            return (pending, success, failed, total);
        }

        /// <summary>
        /// 获取邮件发送统计信息
        /// </summary>
        /// <returns>邮件统计信息</returns>
        public async Task<(int Pending, int Success, int Failed, int Total)> GetEmailStatsAsync()
        {
            return await _emailService.GetEmailStatsAsync();
        }

        /// <summary>
        /// 获取SignalR发送统计信息
        /// </summary>
        /// <returns>SignalR统计信息</returns>
        public async Task<(int Pending, int Success, int Failed, int Total)> GetSignalRStatsAsync()
        {
            return await _signalRService.GetSignalRStatsAsync();
        }

        /// <summary>
        /// 获取指定时间范围内的邮件统计
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>邮件统计信息</returns>
        public async Task<(int Pending, int Success, int Failed, int Total, double SuccessRate)> GetEmailStatsAsync(DateTime? startTime, DateTime? endTime)
        {
            return await _emailService.GetEmailStatsAsync(startTime, endTime);
        }

        /// <summary>
        /// 获取指定时间范围内的SignalR统计
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>SignalR统计信息</returns>
        public async Task<(int Pending, int Success, int Failed, int Total, double SuccessRate)> GetSignalRStatsAsync(DateTime? startTime, DateTime? endTime)
        {
            return await _signalRService.GetSignalRStatsAsync(startTime, endTime);
        }

        /// <summary>
        /// 获取邮件发送失败原因统计
        /// </summary>
        /// <param name="topN">返回前N个失败原因</param>
        /// <returns>失败原因统计</returns>
        public async Task<Dictionary<string, int>> GetEmailFailureReasonsAsync(int topN = 10)
        {
            return await _emailService.GetEmailFailureReasonsAsync(topN);
        }

        /// <summary>
        /// 获取SignalR发送失败原因统计
        /// </summary>
        /// <param name="topN">返回前N个失败原因</param>
        /// <returns>失败原因统计</returns>
        public async Task<Dictionary<string, int>> GetSignalRFailureReasonsAsync(int topN = 10)
        {
            return await _signalRService.GetSignalRFailureReasonsAsync(topN);
        }

        /// <summary>
        /// 获取邮件每小时发送趋势
        /// </summary>
        /// <param name="days">统计天数</param>
        /// <returns>每小时发送趋势</returns>
        public async Task<Dictionary<int, (int Success, int Failed)>> GetHourlyEmailTrendAsync(int days = 7)
        {
            return await _emailService.GetHourlyEmailTrendAsync(days);
        }

        /// <summary>
        /// 获取SignalR每小时发送趋势
        /// </summary>
        /// <param name="days">统计天数</param>
        /// <returns>每小时发送趋势</returns>
        public async Task<Dictionary<int, (int Success, int Failed)>> GetHourlySignalRTrendAsync(int days = 7)
        {
            return await _signalRService.GetHourlySignalRTrendAsync(days);
        }

        /// <summary>
        /// 获取所有渠道的详细统计信息
        /// </summary>
        /// <returns>详细统计信息</returns>
        public async Task<object> GetDetailedStatsAsync()
        {
            var notificationStats = await GetQueueStatsAsync();
            var signalRStats = await _signalRService.GetSignalRStatsAsync();
            var emailStats = await _emailService.GetEmailStatsAsync();

            return new
            {
                Notification = new
                {
                    Pending = notificationStats.Pending,
                    Success = notificationStats.Success,
                    Failed = notificationStats.Failed,
                    Total = notificationStats.Total
                },
                SignalR = new
                {
                    Pending = signalRStats.Pending,
                    Success = signalRStats.Success,
                    Failed = signalRStats.Failed,
                    Total = signalRStats.Total
                },
                Email = new
                {
                    Pending = emailStats.Pending,
                    Success = emailStats.Success,
                    Failed = emailStats.Failed,
                    Total = emailStats.Total
                }
            };
        }
    }
}
