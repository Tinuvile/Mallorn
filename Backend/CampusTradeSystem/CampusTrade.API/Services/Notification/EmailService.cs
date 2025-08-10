using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.Notification
{
    /// <summary>
    /// 邮件服务 - 负责发送电子邮件通知并管理邮件记录
    /// </summary>
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly CampusTradeDbContext _context;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly bool _enableSsl;

        public EmailService(
            IConfiguration configuration, 
            ILogger<EmailService> logger,
            CampusTradeDbContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;

            // 从配置中读取SMTP服务器设置
            _smtpServer = _configuration["Email:SmtpServer"] ?? throw new InvalidOperationException("未配置Email:SmtpServer，请检查appsettings.json配置");

            _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            _smtpUsername = _configuration["Email:Username"] ?? throw new InvalidOperationException("未配置Email:Username，请检查appsettings.json配置");

            _smtpPassword = _configuration["Email:Password"] ?? throw new InvalidOperationException("未配置Email:Password，请检查appsettings.json配置");

            _senderEmail = _configuration["Email:SenderEmail"] ?? throw new InvalidOperationException("未配置Email:SenderEmail，请检查appsettings.json配置");

            _senderName = _configuration["Email:SenderName"] ?? "校园交易系统";
            _enableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true");
        }

        /// <summary>
        /// 简单发送邮件（不记录数据库）
        /// </summary>
        /// <param name="recipientEmail">收件人邮箱</param>
        /// <param name="subject">邮件主题</param>
        /// <param name="body">邮件内容</param>
        /// <returns>发送结果</returns>
        public async Task<(bool Success, string Message)> SendEmailAsync(string recipientEmail, string subject, string body)
        {
            if (string.IsNullOrEmpty(recipientEmail))
            {
                return (false, "收件人邮箱不能为空");
            }

            try
            {
                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_senderEmail, _senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true // 支持HTML格式
                };

                mailMessage.To.Add(recipientEmail);

                using var smtpClient = new SmtpClient(_smtpServer, _smtpPort)
                {
                    EnableSsl = _enableSsl,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword)
                };

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"邮件发送成功 - 收件人: {recipientEmail}, 主题: {subject}");
                return (true, "邮件发送成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"邮件发送失败 - 收件人: {recipientEmail}, 主题: {subject}");
                return (false, $"邮件发送失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送通知类型邮件（记录数据库并支持重试）
        /// </summary>
        /// <param name="notification">通知实体</param>
        /// <param name="content">邮件内容</param>
        /// <returns>发送结果</returns>
        public async Task<(bool Success, string ErrorMessage)> SendNotificationEmailAsync(
            Models.Entities.Notification notification, 
            string content)
        {
            // 检查用户是否有邮箱
            if (string.IsNullOrEmpty(notification.Recipient.Email))
            {
                _logger.LogWarning($"用户 {notification.RecipientId} 没有设置邮箱，跳过邮件通知");
                return (true, "用户未设置邮箱，跳过邮件发送"); // 返回成功，因为不影响整体通知成功
            }

            var emailNotification = new EmailNotification
            {
                EmailType = EmailNotification.EmailTypes.Notification,
                NotificationId = notification.NotificationId,
                RecipientEmail = notification.Recipient.Email,
                Subject = $"【校园交易系统】{notification.Template.TemplateName}",
                Content = content, // 直接使用传入的内容，不进行额外渲染
                SendStatus = EmailNotification.SendStatuses.Pending,
                CreatedAt = DateTime.UtcNow,
                LastAttemptTime = DateTime.UtcNow
            };

            return await SendEmailNotificationAsync(emailNotification);
        }

        /// <summary>
        /// 实际发送邮件通知并管理数据库记录
        /// </summary>
        /// <param name="emailNotification">邮件通知实体</param>
        /// <returns>发送结果</returns>
        private async Task<(bool Success, string ErrorMessage)> SendEmailNotificationAsync(EmailNotification emailNotification)
        {
            try
            {
                // 保存邮件通知记录
                _context.EmailNotifications.Add(emailNotification);
                await _context.SaveChangesAsync();

                // 发送邮件
                var sendResult = await SendEmailAsync(
                    emailNotification.RecipientEmail,
                    emailNotification.Subject,
                    emailNotification.Content);

                if (sendResult.Success)
                {
                    // 发送成功，更新状态
                    emailNotification.SendStatus = EmailNotification.SendStatuses.Success;
                    emailNotification.SentAt = DateTime.UtcNow;
                    
                    _logger.LogInformation($"邮件通知发送成功 - " +
                                         $"Type: {emailNotification.EmailType}, " +
                                         $"Email: {emailNotification.RecipientEmail}, " +
                                         $"NotificationId: {emailNotification.NotificationId}");
                }
                else
                {
                    // 发送失败，增加重试次数
                    emailNotification.RetryCount++;
                    emailNotification.SendStatus = emailNotification.RetryCount >= EmailNotification.MaxRetryCount 
                        ? EmailNotification.SendStatuses.Failed 
                        : EmailNotification.SendStatuses.Pending;
                    emailNotification.ErrorMessage = sendResult.Message;
                    emailNotification.LastAttemptTime = DateTime.UtcNow;

                    _logger.LogWarning($"邮件通知发送失败 - " +
                                     $"Type: {emailNotification.EmailType}, " +
                                     $"Email: {emailNotification.RecipientEmail}, " +
                                     $"RetryCount: {emailNotification.RetryCount}, " +
                                     $"Error: {sendResult.Message}");
                }

                await _context.SaveChangesAsync();
                return (sendResult.Success, sendResult.Message);
            }
            catch (Exception ex)
            {
                var errorMsg = $"邮件通知发送异常: {ex.Message}";
                _logger.LogError(ex, $"邮件通知发送异常 - Email: {emailNotification.RecipientEmail}");

                // 更新错误状态
                try
                {
                    emailNotification.SendStatus = EmailNotification.SendStatuses.Failed;
                    emailNotification.ErrorMessage = errorMsg;
                    emailNotification.RetryCount++;
                    emailNotification.LastAttemptTime = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, $"保存邮件通知错误状态失败 - Email: {emailNotification.RecipientEmail}");
                }

                return (false, errorMsg);
            }
        }

        /// <summary>
        /// 重试失败的邮件通知
        /// </summary>
        /// <param name="batchSize">批次大小</param>
        /// <returns>处理结果</returns>
        public async Task<(int Total, int Success, int Failed)> RetryFailedEmailNotificationsAsync(int batchSize = 10)
        {
            var retryTime = DateTime.UtcNow.AddMinutes(-EmailNotification.DefaultRetryIntervalMinutes);

            var failedNotifications = await _context.EmailNotifications
                .Where(en => en.SendStatus == EmailNotification.SendStatuses.Pending &&
                           en.RetryCount > 0 &&
                           en.RetryCount < EmailNotification.MaxRetryCount &&
                           en.LastAttemptTime < retryTime)
                .OrderBy(en => en.LastAttemptTime)
                .Take(batchSize)
                .ToListAsync();

            int successCount = 0;
            int failedCount = 0;

            _logger.LogInformation($"开始重试失败的邮件通知，共 {failedNotifications.Count} 条");

            foreach (var emailNotification in failedNotifications)
            {
                try
                {
                    var result = await SendEmailAsync(
                        emailNotification.RecipientEmail,
                        emailNotification.Subject,
                        emailNotification.Content);

                    if (result.Success)
                    {
                        emailNotification.SendStatus = EmailNotification.SendStatuses.Success;
                        emailNotification.SentAt = DateTime.UtcNow;
                        successCount++;
                    }
                    else
                    {
                        emailNotification.RetryCount++;
                        emailNotification.SendStatus = emailNotification.RetryCount >= EmailNotification.MaxRetryCount
                            ? EmailNotification.SendStatuses.Failed
                            : EmailNotification.SendStatuses.Pending;
                        emailNotification.ErrorMessage = result.Message;
                        failedCount++;
                    }

                    emailNotification.LastAttemptTime = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // 避免过于频繁的重试
                    await Task.Delay(200);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"重试邮件通知异常 - EmailNotificationId: {emailNotification.EmailNotificationId}");
                    failedCount++;
                }
            }

            _logger.LogInformation($"邮件通知重试完成 - 总计: {failedNotifications.Count}, 成功: {successCount}, 失败: {failedCount}");
            return (failedNotifications.Count, successCount, failedCount);
        }

        /// <summary>
        /// 获取邮件通知状态统计
        /// </summary>
        /// <returns>统计信息</returns>
        public async Task<(int Pending, int Success, int Failed, int Total)> GetEmailStatsAsync()
        {
            var pending = await _context.EmailNotifications
                .CountAsync(en => en.SendStatus == EmailNotification.SendStatuses.Pending);

            var success = await _context.EmailNotifications
                .CountAsync(en => en.SendStatus == EmailNotification.SendStatuses.Success);

            var failed = await _context.EmailNotifications
                .CountAsync(en => en.SendStatus == EmailNotification.SendStatuses.Failed);

            var total = pending + success + failed;

            return (pending, success, failed, total);
        }

        /// <summary>
        /// 获取指定时间范围内的邮件统计
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>时间范围内的统计信息</returns>
        public async Task<(int Pending, int Success, int Failed, int Total, double SuccessRate)> GetEmailStatsAsync(DateTime? startTime, DateTime? endTime)
        {
            var query = _context.EmailNotifications.AsQueryable();

            if (startTime.HasValue)
                query = query.Where(en => en.CreatedAt >= startTime.Value);

            if (endTime.HasValue)
                query = query.Where(en => en.CreatedAt <= endTime.Value);

            var pending = await query.CountAsync(en => en.SendStatus == EmailNotification.SendStatuses.Pending);
            var success = await query.CountAsync(en => en.SendStatus == EmailNotification.SendStatuses.Success);
            var failed = await query.CountAsync(en => en.SendStatus == EmailNotification.SendStatuses.Failed);
            var total = pending + success + failed;
            var successRate = total > 0 ? (double)success / total * 100 : 0;

            return (pending, success, failed, total, successRate);
        }

        /// <summary>
        /// 获取邮件发送失败原因统计
        /// </summary>
        /// <param name="topN">返回前N个失败原因</param>
        /// <returns>失败原因统计</returns>
        public async Task<Dictionary<string, int>> GetEmailFailureReasonsAsync(int topN = 10)
        {
            var failureReasons = await _context.EmailNotifications
                .Where(en => en.SendStatus == EmailNotification.SendStatuses.Failed && 
                           !string.IsNullOrEmpty(en.ErrorMessage))
                .GroupBy(en => en.ErrorMessage)
                .Select(g => new { Reason = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(topN)
                .ToDictionaryAsync(x => x.Reason!, x => x.Count);

            return failureReasons;
        }

        /// <summary>
        /// 获取每小时邮件发送趋势
        /// </summary>
        /// <param name="days">统计天数，默认7天</param>
        /// <returns>每小时发送趋势</returns>
        public async Task<Dictionary<int, (int Success, int Failed)>> GetHourlyEmailTrendAsync(int days = 7)
        {
            var startTime = DateTime.UtcNow.AddDays(-days);
            
            var hourlyData = await _context.EmailNotifications
                .Where(en => en.CreatedAt >= startTime)
                .GroupBy(en => en.CreatedAt.Hour)
                .Select(g => new
                {
                    Hour = g.Key,
                    Success = g.Count(en => en.SendStatus == EmailNotification.SendStatuses.Success),
                    Failed = g.Count(en => en.SendStatus == EmailNotification.SendStatuses.Failed)
                })
                .ToDictionaryAsync(x => x.Hour, x => (x.Success, x.Failed));

            return hourlyData;
        }

    }
}
