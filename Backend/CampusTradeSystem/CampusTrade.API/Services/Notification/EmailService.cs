using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.Email
{
    /// <summary>
    /// 邮件服务 - 负责发送电子邮件通知
    /// </summary>
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly bool _enableSsl;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // 从配置中读取SMTP服务器设置
            _smtpServer = _configuration["Email:SmtpServer"];
            if (string.IsNullOrEmpty(_smtpServer))
            {
                throw new InvalidOperationException("未配置Email:SmtpServer，请检查appsettings.json配置");
            }

            _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            _smtpUsername = _configuration["Email:Username"];
            if (string.IsNullOrEmpty(_smtpUsername))
            {
                throw new InvalidOperationException("未配置Email:Username，请检查appsettings.json配置");
            }

            _smtpPassword = _configuration["Email:Password"];
            if (string.IsNullOrEmpty(_smtpPassword))
            {
                throw new InvalidOperationException("未配置Email:Password，请检查appsettings.json配置");
            }

            _senderEmail = _configuration["Email:SenderEmail"];
            if (string.IsNullOrEmpty(_senderEmail))
            {
                throw new InvalidOperationException("未配置Email:SenderEmail，请检查appsettings.json配置");
            }

            _senderName = _configuration["Email:SenderName"] ?? "校园交易系统";
            _enableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true");
        }

        /// <summary>
        /// 发送邮件
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
                    From = new MailAddress(_senderEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
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
    }
}
