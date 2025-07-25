using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CampusTrade.API.Data;
using CampusTrade.API.Infrastructure.Utils.Notificate;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Services.Auth;
using CampusTrade.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services
{
    /// <summary>
    /// 通知服务单元测试
    /// </summary>
    public class NotificationServiceTests : IDisposable
    {
        private readonly CampusTradeDbContext _context;
        private readonly NotifiService _notifiService;

        public NotificationServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryDbContext();
            _notifiService = new NotifiService(_context);
        }

        [Fact]
        public async Task CreateNotificationAsync_ValidInput_ShouldCreateNotification()
        {
            // Arrange
            var user = new User { UserId = 1, Email = "test@test.com", IsActive = 1 };
            var template = new NotificationTemplate
            {
                TemplateId = 1,
                TemplateName = "测试模板",
                TemplateContent = "你好，{username}，你的订单号是{orderNo}",
                IsActive = 1
            };

            _context.Users.Add(user);
            _context.NotificationTemplates.Add(template);
            await _context.SaveChangesAsync();

            var parameters = new Dictionary<string, object>
            {
                { "username", "张三" },
                { "orderNo", "12345" }
            };

            // Act
            var result = await _notifiService.CreateNotificationAsync(1, 1, parameters);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.NotificationId);
            Assert.Equal("通知已创建并触发发送", result.Message);

            var notification = await _context.Notifications.FindAsync(result.NotificationId);
            Assert.NotNull(notification);
            Assert.Equal(1, notification.RecipientId);
            Assert.Equal(1, notification.TemplateId);
        }

        [Fact]
        public async Task CreateNotificationAsync_InvalidUser_ShouldReturnFailure()
        {
            // Arrange
            var template = new NotificationTemplate
            {
                TemplateId = 1,
                TemplateName = "测试模板",
                TemplateContent = "测试内容",
                IsActive = 1
            };

            _context.NotificationTemplates.Add(template);
            await _context.SaveChangesAsync();

            var parameters = new Dictionary<string, object>();

            // Act
            var result = await _notifiService.CreateNotificationAsync(999, 1, parameters);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("目标用户不存在或已禁用", result.Message);
            Assert.Null(result.NotificationId);
        }

        [Fact]
        public async Task CreateNotificationAsync_InvalidTemplate_ShouldReturnFailure()
        {
            // Arrange
            var user = new User { UserId = 1, Email = "test@test.com", IsActive = 1 };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var parameters = new Dictionary<string, object>();

            // Act
            var result = await _notifiService.CreateNotificationAsync(1, 999, parameters);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("通知模板不存在或已禁用", result.Message);
            Assert.Null(result.NotificationId);
        }

        [Fact]
        public async Task GetNotificationStatsAsync_ShouldReturnCorrectStats()
        {
            // Arrange
            var notifications = new[]
            {
                new Notification { NotificationId = 1, SendStatus = Notification.SendStatuses.Pending },
                new Notification { NotificationId = 2, SendStatus = Notification.SendStatuses.Success },
                new Notification { NotificationId = 3, SendStatus = Notification.SendStatuses.Failed },
                new Notification { NotificationId = 4, SendStatus = Notification.SendStatuses.Pending }
            };

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            // Act
            var stats = await _notifiService.GetNotificationStatsAsync();

            // Assert
            Assert.Equal(2, stats.Pending);
            Assert.Equal(1, stats.Success);
            Assert.Equal(1, stats.Failed);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }

    /// <summary>
    /// 通知扩展方法测试
    /// </summary>
    public class NotificationExtensionsTests
    {
        [Fact]
        public void GetRenderedContent_ValidTemplate_ShouldRenderCorrectly()
        {
            // Arrange
            var template = new NotificationTemplate
            {
                TemplateContent = "你好，{username}，你的订单号是{orderNo}"
            };

            var notification = new Notification
            {
                Template = template,
                TemplateParams = "{\"username\":\"张三\",\"orderNo\":\"12345\"}"
            };

            // Act
            var result = notification.GetRenderedContent();

            // Assert
            Assert.Equal("你好，张三，你的订单号是12345", result);
        }

        [Fact]
        public void GetRenderedContent_NoTemplate_ShouldReturnEmpty()
        {
            // Arrange
            var notification = new Notification();

            // Act
            var result = notification.GetRenderedContent();

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetRenderedContent_NoParams_ShouldReturnOriginalTemplate()
        {
            // Arrange
            var template = new NotificationTemplate
            {
                TemplateContent = "你好，{username}，你的订单号是{orderNo}"
            };

            var notification = new Notification
            {
                Template = template,
                TemplateParams = null
            };

            // Act
            var result = notification.GetRenderedContent();

            // Assert
            Assert.Equal("你好，{username}，你的订单号是{orderNo}", result);
        }

        [Fact]
        public void GetRenderedContent_InvalidParams_ShouldReturnOriginalTemplate()
        {
            // Arrange
            var template = new NotificationTemplate
            {
                TemplateContent = "你好，{username}，你的订单号是{orderNo}"
            };

            var notification = new Notification
            {
                Template = template,
                TemplateParams = "invalid json"
            };

            // Act
            var result = notification.GetRenderedContent();

            // Assert
            Assert.Equal("你好，{username}，你的订单号是{orderNo}", result);
        }
    }
}
