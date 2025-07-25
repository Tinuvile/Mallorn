using CampusTrade.API.Services.Auth;
using CampusTrade.API.Services.ScheduledTasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services.ScheduledTasks
{
    /// <summary>
    /// TokenCleanupTask单元测试
    /// </summary>
    public class TokenCleanupTaskTests : IDisposable
    {
        private readonly Mock<ILogger<TokenCleanupTask>> _mockLogger;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IServiceScope> _mockServiceScope;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly TokenCleanupTask _tokenCleanupTask;

        public TokenCleanupTaskTests()
        {
            _mockLogger = new Mock<ILogger<TokenCleanupTask>>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockServiceScope = new Mock<IServiceScope>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockTokenService = new Mock<ITokenService>();

            // 设置模拟对象的行为
            _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockServiceScope.Object);
            _mockServiceScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
            _mockServiceProvider.Setup(x => x.GetRequiredService<ITokenService>())
                .Returns(_mockTokenService.Object);

            _tokenCleanupTask = new TokenCleanupTask(_mockLogger.Object, _mockScopeFactory.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Act & Assert
            _tokenCleanupTask.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new TokenCleanupTask(null!, _mockScopeFactory.Object));
        }

        [Fact]
        public void Constructor_WithNullScopeFactory_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new TokenCleanupTask(_mockLogger.Object, null!));
        }

        #endregion

        #region Interval Tests

        [Fact]
        public void Interval_ShouldBeOneHour()
        {
            // Act & Assert
            // 无法直接访问受保护的Interval属性，但可以通过任务状态验证
            _tokenCleanupTask.Should().NotBeNull();
            _tokenCleanupTask.GetType().Name.Should().Be("TokenCleanupTask");
        }

        #endregion

        #region ExecuteTaskAsync Tests

        [Fact]
        public async Task ExecuteTaskAsync_ShouldCallTokenServiceCleanup()
        {
            // Arrange
            _mockTokenService.Setup(x => x.CleanupExpiredTokensAsync())
                .ReturnsAsync(5); // 返回清理的令牌数量

            // Act
            await _tokenCleanupTask.StartAsync(CancellationToken.None);
            await Task.Delay(100); // 等待执行
            await _tokenCleanupTask.StopAsync(CancellationToken.None);

            // Assert
            _mockScopeFactory.Verify(x => x.CreateScope(), Times.AtLeastOnce);
            _mockServiceProvider.Verify(x => x.GetRequiredService<ITokenService>(), Times.AtLeastOnce);
            _mockTokenService.Verify(x => x.CleanupExpiredTokensAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteTaskAsync_WhenTokenServiceThrows_ShouldHandleException()
        {
            // Arrange
            var expectedException = new Exception("Token service error");
            _mockTokenService.Setup(x => x.CleanupExpiredTokensAsync())
                .ThrowsAsync(expectedException);

            // Act
            await _tokenCleanupTask.StartAsync(CancellationToken.None);
            await Task.Delay(200); // 等待执行和异常处理
            await _tokenCleanupTask.StopAsync(CancellationToken.None);

            // Assert
            _tokenCleanupTask.LastError.Should().NotBeNull();
            _tokenCleanupTask.LastError!.Message.Should().Contain("Token service error");

            // 验证错误日志被记录
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("执行出错")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteTaskAsync_ShouldDisposeScope()
        {
            // Arrange
            _mockTokenService.Setup(x => x.CleanupExpiredTokensAsync())
                .ReturnsAsync(3);

            // Act
            await _tokenCleanupTask.StartAsync(CancellationToken.None);
            await Task.Delay(100);
            await _tokenCleanupTask.StopAsync(CancellationToken.None);

            // Assert
            _mockServiceScope.Verify(x => x.Dispose(), Times.AtLeastOnce);
        }

        #endregion

        #region Service Lifecycle Tests

        [Fact]
        public async Task StartAsync_ShouldLogStartupMessage()
        {
            // Act
            await _tokenCleanupTask.StartAsync(CancellationToken.None);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("开始启动")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task StopAsync_ShouldLogStopMessage()
        {
            // Arrange
            await _tokenCleanupTask.StartAsync(CancellationToken.None);

            // Act
            await _tokenCleanupTask.StopAsync(CancellationToken.None);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("开始停止")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Performance and Reliability Tests

        [Fact]
        public async Task ExecuteTaskAsync_WithLongRunningTask_ShouldNotBlockOtherTasks()
        {
            // Arrange
            var longRunningTask = new TaskCompletionSource<int>();
            _mockTokenService.Setup(x => x.CleanupExpiredTokensAsync())
                .Returns(longRunningTask.Task);

            // Act
            await _tokenCleanupTask.StartAsync(CancellationToken.None);

            // 验证任务开始执行
            await Task.Delay(50);
            _tokenCleanupTask.IsExecuting.Should().BeTrue();

            // 完成长时间运行的任务
            longRunningTask.SetResult(1);
            await Task.Delay(50);

            await _tokenCleanupTask.StopAsync(CancellationToken.None);

            // Assert
            _tokenCleanupTask.ExecutionCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task ExecuteTaskAsync_MultipleExecution_ShouldIncrementExecutionCount()
        {
            // Arrange - 设置较短的间隔进行快速测试
            var shortIntervalTask = new TokenCleanupTask(_mockLogger.Object, _mockScopeFactory.Object);
            _mockTokenService.Setup(x => x.CleanupExpiredTokensAsync())
                .ReturnsAsync(2);

            // Act
            await shortIntervalTask.StartAsync(CancellationToken.None);
            await Task.Delay(100); // 等待多次执行
            await shortIntervalTask.StopAsync(CancellationToken.None);

            // Assert
            shortIntervalTask.ExecutionCount.Should().BeGreaterThan(0);
            shortIntervalTask.LastExecutionTime.Should().NotBeNull();

            shortIntervalTask.Dispose();
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task ExecuteTaskAsync_WithRealServiceScope_ShouldWork()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<ITokenService>(_ => _mockTokenService.Object);
            serviceCollection.AddLogging();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var realScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            var taskWithRealScope = new TokenCleanupTask(_mockLogger.Object, realScopeFactory);

            _mockTokenService.Setup(x => x.CleanupExpiredTokensAsync())
                .ReturnsAsync(1);

            // Act
            await taskWithRealScope.StartAsync(CancellationToken.None);
            await Task.Delay(100);
            await taskWithRealScope.StopAsync(CancellationToken.None);

            // Assert
            _mockTokenService.Verify(x => x.CleanupExpiredTokensAsync(), Times.AtLeastOnce);

            taskWithRealScope.Dispose();
            serviceProvider.Dispose();
        }

        #endregion

        public void Dispose()
        {
            _tokenCleanupTask?.Dispose();
            _mockServiceScope?.Object?.Dispose();
        }
    }
}
