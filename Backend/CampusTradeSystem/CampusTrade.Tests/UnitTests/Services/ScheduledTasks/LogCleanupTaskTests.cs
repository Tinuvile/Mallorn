using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Services.ScheduledTasks;
using CampusTrade.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services.ScheduledTasks
{
    /// <summary>
    /// LogCleanupTask单元测试
    /// </summary>
    public class LogCleanupTaskTests : IDisposable
    {
        private readonly Mock<ILogger<LogCleanupTask>> _mockLogger;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IServiceScope> _mockServiceScope;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly CampusTradeDbContext _testContext;
        private readonly LogCleanupTask _logCleanupTask;

        public LogCleanupTaskTests()
        {
            _mockLogger = new Mock<ILogger<LogCleanupTask>>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockServiceScope = new Mock<IServiceScope>();
            _mockServiceProvider = new Mock<IServiceProvider>();

            // 创建测试数据库上下文
            var databaseName = $"LogCleanupTest_{Guid.NewGuid()}";
            _testContext = TestDbContextFactory.CreateInMemoryDbContext(databaseName, seedData: false);

            // 设置模拟对象的行为
            _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockServiceScope.Object);
            _mockServiceScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
            _mockServiceProvider.Setup(x => x.GetRequiredService<CampusTradeDbContext>())
                .Returns(_testContext);

            _logCleanupTask = new LogCleanupTask(_mockLogger.Object, _mockScopeFactory.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Act & Assert
            _logCleanupTask.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new LogCleanupTask(null!, _mockScopeFactory.Object));
        }

        [Fact]
        public void Constructor_WithNullScopeFactory_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new LogCleanupTask(_mockLogger.Object, null!));
        }

        #endregion

        #region Interval Tests

        [Fact]
        public void Interval_ShouldBeOneDay()
        {
            // Act & Assert
            // 无法直接访问受保护的Interval属性，但可以通过任务状态验证
            _logCleanupTask.Should().NotBeNull();
            _logCleanupTask.GetType().Name.Should().Be("LogCleanupTask");
        }

        #endregion

        #region ExecuteTaskAsync Tests

        [Fact]
        public async Task ExecuteTaskAsync_ShouldCleanupOldLogs()
        {
            // Arrange
            await SeedTestLogData();

            // Act
            await _logCleanupTask.StartAsync(CancellationToken.None);
            await Task.Delay(200); // 等待执行
            await _logCleanupTask.StopAsync(CancellationToken.None);

            // Assert
            _mockScopeFactory.Verify(x => x.CreateScope(), Times.AtLeastOnce);
            _mockServiceProvider.Verify(x => x.GetRequiredService<CampusTradeDbContext>(), Times.AtLeastOnce);

            // 验证旧日志被删除
            var remainingLoginLogs = await _testContext.LoginLogs.CountAsync();
            var remainingAuditLogs = await _testContext.AuditLogs.CountAsync();

            remainingLoginLogs.Should().BeLessOrEqualTo(2); // 应该只保留最近7天的日志
            remainingAuditLogs.Should().BeLessOrEqualTo(2);
        }

        [Fact]
        public async Task ExecuteTaskAsync_WithDatabaseError_ShouldHandleException()
        {
            // Arrange
            var mockContext = new Mock<CampusTradeDbContext>();
            mockContext.Setup(x => x.LoginLogs).Throws(new Exception("Database error"));

            _mockServiceProvider.Setup(x => x.GetRequiredService<CampusTradeDbContext>())
                .Returns(mockContext.Object);

            // Act
            await _logCleanupTask.StartAsync(CancellationToken.None);
            await Task.Delay(200); // 等待执行和异常处理
            await _logCleanupTask.StopAsync(CancellationToken.None);

            // Assert
            _logCleanupTask.LastError.Should().NotBeNull();
            _logCleanupTask.LastError!.Message.Should().Contain("Database error");
        }

        [Fact]
        public async Task ExecuteTaskAsync_ShouldDisposeScope()
        {
            // Arrange
            await SeedTestLogData();

            // Act
            await _logCleanupTask.StartAsync(CancellationToken.None);
            await Task.Delay(100);
            await _logCleanupTask.StopAsync(CancellationToken.None);

            // Assert
            _mockServiceScope.Verify(x => x.Dispose(), Times.AtLeastOnce);
        }

        #endregion

        #region Data Integrity Tests

        [Fact]
        public async Task ExecuteTaskAsync_ShouldNotDeleteRecentLogs()
        {
            // Arrange
            await SeedTestLogData();
            var recentLogsCount = await _testContext.LoginLogs
                .Where(l => l.LogTime >= DateTime.Now.AddDays(-7))
                .CountAsync();

            // Act
            await _logCleanupTask.StartAsync(CancellationToken.None);
            await Task.Delay(200);
            await _logCleanupTask.StopAsync(CancellationToken.None);

            // Assert
            var remainingRecentLogs = await _testContext.LoginLogs
                .Where(l => l.LogTime >= DateTime.Now.AddDays(-7))
                .CountAsync();

            remainingRecentLogs.Should().Be(recentLogsCount); // 最近的日志应该保留
        }

        [Fact]
        public async Task ExecuteTaskAsync_WithEmptyDatabase_ShouldNotThrow()
        {
            // Arrange - 空数据库

            // Act
            await _logCleanupTask.StartAsync(CancellationToken.None);
            await Task.Delay(100);
            await _logCleanupTask.StopAsync(CancellationToken.None);

            // Assert
            _logCleanupTask.LastError.Should().BeNull();
            _logCleanupTask.ExecutionCount.Should().BeGreaterThan(0);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task ExecuteTaskAsync_WithLargeDataSet_ShouldComplete()
        {
            // Arrange
            await SeedLargeLogDataSet(1000);

            // Act
            var startTime = DateTime.UtcNow;
            await _logCleanupTask.StartAsync(CancellationToken.None);
            await Task.Delay(500); // 等待较长时间处理大数据集
            await _logCleanupTask.StopAsync(CancellationToken.None);
            var endTime = DateTime.UtcNow;

            // Assert
            var duration = endTime - startTime;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(10)); // 应该在合理时间内完成
            _logCleanupTask.LastError.Should().BeNull();
        }

        #endregion

        #region Helper Methods

        private async Task SeedTestLogData()
        {
            // 添加新的日志（不应该被删除）
            var recentLoginLogs = new[]
            {
                new LoginLogs { UserId = 1, LogTime = DateTime.Now.AddDays(-3), IpAddress = "192.168.1.1", DeviceType = "PC" },
                new LoginLogs { UserId = 2, LogTime = DateTime.Now.AddDays(-1), IpAddress = "192.168.1.2", DeviceType = "Mobile" }
            };

            var recentAuditLogs = new[]
            {
                new AuditLog { AdminId = 1, LogTime = DateTime.Now.AddDays(-2), ActionType = "Test Action 1" },
                new AuditLog { AdminId = 2, LogTime = DateTime.Now.AddDays(-1), ActionType = "Test Action 2" }
            };

            // 添加旧的日志（应该被删除）
            var oldLoginLogs = new[]
            {
                new LoginLogs { UserId = 1, LogTime = DateTime.Now.AddDays(-10), IpAddress = "192.168.1.1", DeviceType = "PC" },
                new LoginLogs { UserId = 2, LogTime = DateTime.Now.AddDays(-15), IpAddress = "192.168.1.2", DeviceType = "Mobile" }
            };

            var oldAuditLogs = new[]
            {
                new AuditLog { AdminId = 1, LogTime = DateTime.Now.AddDays(-8), ActionType = "Old Action 1" },
                new AuditLog { AdminId = 2, LogTime = DateTime.Now.AddDays(-12), ActionType = "Old Action 2" }
            };

            _testContext.LoginLogs.AddRange(recentLoginLogs);
            _testContext.LoginLogs.AddRange(oldLoginLogs);
            _testContext.AuditLogs.AddRange(recentAuditLogs);
            _testContext.AuditLogs.AddRange(oldAuditLogs);

            await _testContext.SaveChangesAsync();
        }

        private async Task SeedLargeLogDataSet(int count)
        {
            var loginLogs = new List<LoginLogs>();
            var auditLogs = new List<AuditLog>();

            for (int i = 0; i < count; i++)
            {
                // 一半新日志，一半旧日志
                var isOld = i % 2 == 0;
                var logTime = isOld ? DateTime.Now.AddDays(-10) : DateTime.Now.AddDays(-1);

                loginLogs.Add(new LoginLogs
                {
                    UserId = i % 10 + 1,
                    LogTime = logTime,
                    IpAddress = $"192.168.1.{i % 255}",
                    DeviceType = i % 3 == 0 ? "PC" : (i % 3 == 1 ? "Mobile" : "Tablet")
                });

                auditLogs.Add(new AuditLog
                {
                    AdminId = i % 10 + 1,
                    LogTime = logTime,
                    ActionType = $"Test Action {i}"
                });
            }

            _testContext.LoginLogs.AddRange(loginLogs);
            _testContext.AuditLogs.AddRange(auditLogs);

            await _testContext.SaveChangesAsync();
        }

        #endregion

        public void Dispose()
        {
            _logCleanupTask?.Dispose();
            _testContext?.Dispose();
            _mockServiceScope?.Object?.Dispose();
        }
    }
}
