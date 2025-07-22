using CampusTrade.API.Services.ScheduledTasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services.ScheduledTasks
{
    /// <summary>
    /// ScheduledService基类单元测试
    /// </summary>
    public class ScheduledServiceTests : IDisposable
    {
        private readonly Mock<ILogger> _mockLogger;

        public ScheduledServiceTests()
        {
            _mockLogger = new Mock<ILogger>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidLogger_ShouldCreateInstance()
        {
            // Act
            var service = new TestScheduledService(_mockLogger.Object, TimeSpan.FromSeconds(1));

            // Assert
            service.Should().NotBeNull();
            // 无法直接访问受保护的Interval属性，通过其他方式验证
            service.GetType().Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new TestScheduledService(null!, TimeSpan.FromSeconds(1)));
        }

        #endregion

        #region StartAsync Tests

        [Fact]
        public async Task StartAsync_ShouldStartTimerAndSetNextExecutionTime()
        {
            // Arrange
            var service = new TestScheduledService(_mockLogger.Object, TimeSpan.FromSeconds(1));
            var cancellationToken = CancellationToken.None;

            // Act
            await service.StartAsync(cancellationToken);

            // Assert
            service.NextExecutionTime.Should().NotBeNull();
            service.NextExecutionTime.Should().BeAfter(DateTime.UtcNow.AddSeconds(-1));
            service.NextExecutionTime.Should().BeBefore(DateTime.UtcNow.AddSeconds(5));
        }

        [Fact]
        public async Task StartAsync_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var service = new TestScheduledService(_mockLogger.Object, TimeSpan.FromSeconds(1));
            service.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                service.StartAsync(CancellationToken.None));
        }

        #endregion

        #region StopAsync Tests

        [Fact]
        public async Task StopAsync_ShouldStopTimer()
        {
            // Arrange
            var service = new TestScheduledService(_mockLogger.Object, TimeSpan.FromSeconds(1));
            await service.StartAsync(CancellationToken.None);

            // Act
            await service.StopAsync(CancellationToken.None);

            // Assert - 验证日志被调用
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("已停止")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Task Execution Tests

        [Fact]
        public async Task ExecuteTaskAsync_ShouldIncrementExecutionCount()
        {
            // Arrange
            var service = new TestScheduledService(_mockLogger.Object, TimeSpan.FromMilliseconds(100));
            service.SetExecuteDelay(TimeSpan.FromMilliseconds(50));

            // Act
            await service.StartAsync(CancellationToken.None);
            await Task.Delay(TimeSpan.FromMilliseconds(200)); // 等待执行
            await service.StopAsync(CancellationToken.None);

            // Assert
            service.ExecutionCount.Should().BeGreaterThan(0);
            service.LastExecutionTime.Should().NotBeNull();
        }

        [Fact]
        public async Task ExecuteTaskAsync_WithException_ShouldSetLastError()
        {
            // Arrange
            var service = new TestScheduledService(_mockLogger.Object, TimeSpan.FromMilliseconds(100));
            service.SetShouldThrow(true);

            // Act
            await service.StartAsync(CancellationToken.None);
            await Task.Delay(TimeSpan.FromMilliseconds(200)); // 等待执行
            await service.StopAsync(CancellationToken.None);

            // Assert
            service.LastError.Should().NotBeNull();
            service.LastError!.Message.Should().Contain("测试异常");
        }

        [Fact]
        public async Task ExecuteTaskAsync_ShouldPreventConcurrentExecution()
        {
            // Arrange
            var service = new TestScheduledService(_mockLogger.Object, TimeSpan.FromMilliseconds(50));
            service.SetExecuteDelay(TimeSpan.FromMilliseconds(200)); // 执行时间比间隔长

            // Act
            await service.StartAsync(CancellationToken.None);
            await Task.Delay(TimeSpan.FromMilliseconds(300)); // 等待多次触发
            await service.StopAsync(CancellationToken.None);

            // Assert
            // 由于并发保护，执行次数应该小于理论上的触发次数
            service.ExecutionCount.Should().BeLessOrEqualTo(2);
        }

        #endregion

        #region GetTaskStatus Tests

        [Fact]
        public void GetTaskStatus_ShouldReturnCorrectStatus()
        {
            // Arrange
            var service = new TestScheduledService(_mockLogger.Object, TimeSpan.FromSeconds(5));

            // Act
            var status = service.GetTaskStatus();

            // Assert
            status.Should().NotBeNull();
            var type = status.GetType();
            var taskName = type.GetProperty("TaskName")?.GetValue(status)?.ToString();
            var statusValue = type.GetProperty("Status")?.GetValue(status)?.ToString();
            Assert.Equal("TestScheduledService", taskName);
            Assert.Equal("正常", statusValue);
        }

        [Fact]
        public async Task GetTaskStatus_AfterExecution_ShouldShowUpdatedInfo()
        {
            // Arrange
            var service = new TestScheduledService(_mockLogger.Object, TimeSpan.FromMilliseconds(100));
            service.SetExecuteDelay(TimeSpan.FromMilliseconds(50));

            // Act
            await service.StartAsync(CancellationToken.None);
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            var status = service.GetTaskStatus();
            await service.StopAsync(CancellationToken.None);

            // Assert
            var type = status.GetType();
            var executionCount = (int?)type.GetProperty("ExecutionCount")?.GetValue(status);
            var lastExecutionTime = type.GetProperty("LastExecutionTime")?.GetValue(status);
            Assert.True(executionCount > 0);
            Assert.NotNull(lastExecutionTime);
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_ShouldReleaseResources()
        {
            // Arrange
            var service = new TestScheduledService(_mockLogger.Object, TimeSpan.FromSeconds(1));

            // Act
            service.Dispose();

            // Assert - 应该没有异常
            service.Dispose(); // 第二次调用应该安全
        }

        [Fact]
        public async Task Dispose_ShouldLogResourceRelease()
        {
            // Arrange
            var service = new TestScheduledService(_mockLogger.Object, TimeSpan.FromSeconds(1));
            await service.StartAsync(CancellationToken.None);

            // Act
            service.Dispose();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("资源已释放")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        public void Dispose()
        {
            // 清理资源
        }

        #region Test Helper Class

        /// <summary>
        /// 用于测试的ScheduledService实现
        /// </summary>
        private class TestScheduledService : ScheduledService
        {
            private TimeSpan _executeDelay = TimeSpan.Zero;
            private bool _shouldThrow = false;
            private readonly TimeSpan _interval;

            public TestScheduledService(ILogger logger, TimeSpan interval) : base(logger)
            {
                _interval = interval;
            }

            protected override TimeSpan Interval => _interval;

            protected override async Task ExecuteTaskAsync()
            {
                if (_executeDelay > TimeSpan.Zero)
                {
                    await Task.Delay(_executeDelay);
                }

                if (_shouldThrow)
                {
                    throw new Exception("测试异常");
                }

                // 模拟正常的任务执行
                await Task.CompletedTask;
            }

            public void SetExecuteDelay(TimeSpan delay)
            {
                _executeDelay = delay;
            }

            public void SetShouldThrow(bool shouldThrow)
            {
                _shouldThrow = shouldThrow;
            }

            protected override async Task OnTaskErrorAsync(Exception exception)
            {
                // 测试特定的错误处理
                await base.OnTaskErrorAsync(exception);
            }
        }

        #endregion
    }
}
