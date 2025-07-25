using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CampusTrade.API.Infrastructure.Hubs;
using CampusTrade.API.Services.Auth;
using CampusTrade.API.Services.Email;
using CampusTrade.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.IntegrationTests
{
    /// <summary>
    /// 通知系统集成测试
    /// </summary>
    public class NotificationIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly NotifiService _notifiService;
        private readonly NotifiSenderService _notifiSenderService;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        public NotificationIntegrationTests()
        {
            var services = new ServiceCollection();

            // 添加必要的服务
            services.AddLogging(builder => builder.AddConsole());

            // 使用内存数据库
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            services.AddSingleton(context);

            // 创建Mock服务
            _mockHubContext = new Mock<IHubContext<NotificationHub>>();

            // 注册Mock服务
            services.AddSingleton(_mockHubContext.Object);

            // 注册一个简单的Mock EmailService
            services.AddScoped<EmailService>(provider =>
            {
                var config = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
                var logger = provider.GetService<ILogger<EmailService>>();

                // 设置基本的配置值
                config.Setup(x => x["Email:SmtpServer"]).Returns("smtp.test.com");
                config.Setup(x => x["Email:SmtpPort"]).Returns("587");
                config.Setup(x => x["Email:Username"]).Returns("test@test.com");
                config.Setup(x => x["Email:Password"]).Returns("fake-password-for-testing-only");
                config.Setup(x => x["Email:SenderEmail"]).Returns("test@test.com");
                config.Setup(x => x["Email:SenderName"]).Returns("Test");
                config.Setup(x => x["Email:EnableSsl"]).Returns("true");

                return new EmailService(config.Object, logger);
            });

            // 注册通知服务
            services.AddScoped<NotifiService>();
            services.AddScoped<NotifiSenderService>();

            _serviceProvider = services.BuildServiceProvider();
            _notifiService = _serviceProvider.GetRequiredService<NotifiService>();
            _notifiSenderService = _serviceProvider.GetRequiredService<NotifiSenderService>();
        }

        [Fact]
        public async Task CreateNotification_ShouldWork()
        {
            // Arrange
            var context = _serviceProvider.GetRequiredService<CampusTrade.API.Data.CampusTradeDbContext>();

            // 清理可能存在的实体
            context.ChangeTracker.Clear();

            // 创建测试数据
            var user = new CampusTrade.API.Models.Entities.User
            {
                UserId = 101,  // 使用不同的ID避免冲突
                Email = "test1@test.com",
                Username = "testuser1",
                IsActive = 1
            };
            var template = new CampusTrade.API.Models.Entities.NotificationTemplate
            {
                TemplateId = 101,
                TemplateName = "测试模板1",
                TemplateContent = "你好，{username}！",
                IsActive = 1
            };

            context.Users.Add(user);
            context.NotificationTemplates.Add(template);
            await context.SaveChangesAsync();

            var parameters = new Dictionary<string, object>
            {
                { "username", "张三" }
            };

            // Act
            var result = await _notifiService.CreateNotificationAsync(101, 101, parameters);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.NotificationId);
        }

        [Fact]
        public async Task ProcessNotificationQueue_ShouldWork()
        {
            // Arrange
            var context = _serviceProvider.GetRequiredService<CampusTrade.API.Data.CampusTradeDbContext>();

            // 清理可能存在的实体
            context.ChangeTracker.Clear();

            // 创建测试数据
            var user = new CampusTrade.API.Models.Entities.User
            {
                UserId = 201,  // 使用不同的ID
                Email = "test2@test.com",
                Username = "testuser2",
                IsActive = 1
            };
            var template = new CampusTrade.API.Models.Entities.NotificationTemplate
            {
                TemplateId = 201,
                TemplateName = "测试模板2",
                TemplateContent = "你好，{username}！",
                IsActive = 1,
                Priority = 1
            };
            var notification = new CampusTrade.API.Models.Entities.Notification
            {
                NotificationId = 201,
                TemplateId = 201,
                RecipientId = 201,
                TemplateParams = "{\"username\":\"张三\"}",
                SendStatus = CampusTrade.API.Models.Entities.Notification.SendStatuses.Pending,
                RetryCount = 0,
                CreatedAt = DateTime.Now,
                LastAttemptTime = DateTime.Now
            };

            context.Users.Add(user);
            context.NotificationTemplates.Add(template);
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            // Act
            var result = await _notifiSenderService.ProcessNotificationQueueAsync(10);

            // Assert
            Assert.True(result.Total > 0);
        }

        [Fact]
        public void MockSignalRHub_ShouldBeConfigured()
        {
            // Act & Assert
            // 验证Mock Hub上下文已正确配置
            Assert.NotNull(_mockHubContext);
            Assert.NotNull(_mockHubContext.Object);
        }

        [Fact]
        public async Task GetNotificationStats_ShouldReturnCorrectCounts()
        {
            // Arrange
            var context = _serviceProvider.GetRequiredService<CampusTrade.API.Data.CampusTradeDbContext>();

            // 清理可能存在的实体
            context.ChangeTracker.Clear();

            var notifications = new[]
            {
                new CampusTrade.API.Models.Entities.Notification
                {
                    NotificationId = 301,
                    SendStatus = CampusTrade.API.Models.Entities.Notification.SendStatuses.Pending,
                    TemplateId = 301,
                    RecipientId = 301,
                    RetryCount = 0,
                    CreatedAt = DateTime.Now,
                    LastAttemptTime = DateTime.Now
                },
                new CampusTrade.API.Models.Entities.Notification
                {
                    NotificationId = 302,
                    SendStatus = CampusTrade.API.Models.Entities.Notification.SendStatuses.Success,
                    TemplateId = 301,
                    RecipientId = 301,
                    RetryCount = 0,
                    CreatedAt = DateTime.Now,
                    LastAttemptTime = DateTime.Now
                }
            };

            context.Notifications.AddRange(notifications);
            await context.SaveChangesAsync();

            // Act
            var stats = await _notifiService.GetNotificationStatsAsync();

            // Assert
            Assert.Equal(1, stats.Pending);
            Assert.Equal(1, stats.Success);
            Assert.Equal(0, stats.Failed);
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}
