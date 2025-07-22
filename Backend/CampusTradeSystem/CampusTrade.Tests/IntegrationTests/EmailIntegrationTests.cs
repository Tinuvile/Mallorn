using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using CampusTrade.API.Services.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CampusTrade.Tests.IntegrationTests
{
    /// <summary>
    /// 邮件服务集成测试
    /// 从原 EmailConsoleTest 项目迁移而来
    /// </summary>
    public class EmailIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly EmailService _emailService;

        public EmailIntegrationTests()
        {
            // 构建配置
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Email:SmtpServer", "smtp.qq.com"),
                    new KeyValuePair<string, string>("Email:SmtpPort", "587"),
                    new KeyValuePair<string, string>("Email:Username", "1870707155@qq.com"),
                    new KeyValuePair<string, string>("Email:Password", "gsqlerqxaryaefcf"),
                    new KeyValuePair<string, string>("Email:SenderEmail", "1870707155@qq.com"),
                    new KeyValuePair<string, string>("Email:SenderName", "校园交易系统"),
                    new KeyValuePair<string, string>("Email:EnableSsl", "true")
                })
                .Build();

            // 构建服务容器
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging(builder => builder.AddConsole());
            services.AddScoped<EmailService>();

            _serviceProvider = services.BuildServiceProvider();
            _emailService = _serviceProvider.GetRequiredService<EmailService>();
        }

        /// <summary>
        /// 测试原始邮件发送代码（来自 EmailConsoleTest）
        /// </summary>
        [Fact]
        public void TestRawEmailSending()
        {
            // 创建邮件消息
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("1870707155@qq.com"); // 发件人邮箱
            mail.To.Add("1591503159@qq.com"); // 收件人邮箱
            mail.Subject = "测试邮件"; // 邮件主题
            mail.Body = "这是通过C#发送的测试邮件！"; // 邮件内容
            mail.IsBodyHtml = false; // 是否为HTML格式

            // 配置SMTP客户端
            SmtpClient smtp = new SmtpClient("smtp.qq.com", 587); // QQ邮箱SMTP服务器地址和端口号
            smtp.Credentials = new NetworkCredential("1870707155@qq.com", "gsqlerqxaryaefcf"); // 邮箱账号和授权码
            smtp.EnableSsl = true; // 启用SSL加密

            try
            {
                smtp.Send(mail); // 发送邮件
                Assert.True(true, "邮件发送成功！");
            }
            catch (Exception ex)
            {
                Assert.Fail($"邮件发送失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 测试 EmailService 发送邮件
        /// </summary>
        [Fact]
        public async Task TestEmailServiceSending()
        {
            // Arrange
            var recipientEmail = "1591503159@qq.com";
            var subject = "EmailService 测试邮件";
            var body = "这是通过 EmailService 发送的测试邮件！";

            // Act
            var result = await _emailService.SendEmailAsync(recipientEmail, subject, body);

            // Assert
            Assert.True(result.Success, $"邮件发送应该成功，但失败了：{result.Message}");
        }

        /// <summary>
        /// 测试邮件发送失败场景
        /// </summary>
        [Fact]
        public async Task TestEmailServiceWithInvalidEmail()
        {
            // Arrange
            var invalidEmail = "invalid-email-address";
            var subject = "测试邮件";
            var body = "测试内容";

            // Act
            var result = await _emailService.SendEmailAsync(invalidEmail, subject, body);

            // Assert
            Assert.False(result.Success, "发送到无效邮箱地址应该失败");
            Assert.Contains("邮件发送失败", result.Message);
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}
