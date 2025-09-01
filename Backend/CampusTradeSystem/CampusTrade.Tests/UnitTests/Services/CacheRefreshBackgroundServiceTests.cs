using System;
using System.Threading;
using System.Threading.Tasks;
using CampusTrade.API.Options;
using CampusTrade.API.Services.Background;
using CampusTrade.API.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services
{
    /// <summary>
    /// CacheRefreshBackgroundService 单元测试
    /// </summary>
    public class CacheRefreshBackgroundServiceTests
    {
        private readonly Mock<ILogger<CacheRefreshBackgroundService>> _mockLogger;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IServiceScope> _mockServiceScope;
        private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
        private readonly Mock<ICategoryCacheService> _mockCategoryCache;
        private readonly Mock<IProductCacheService> _mockProductCache;
        private readonly Mock<ISystemConfigCacheService> _mockConfigCache;
        private readonly Mock<IUserCacheService> _mockUserCache;
        private readonly CacheOptions _cacheOptions;

        public CacheRefreshBackgroundServiceTests()
        {
            _mockLogger = new Mock<ILogger<CacheRefreshBackgroundService>>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockServiceScope = new Mock<IServiceScope>();
            _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
            _mockCategoryCache = new Mock<ICategoryCacheService>();
            _mockProductCache = new Mock<IProductCacheService>();
            _mockConfigCache = new Mock<ISystemConfigCacheService>();
            _mockUserCache = new Mock<IUserCacheService>();

            _cacheOptions = new CacheOptions
            {
                InitialDelaySeconds = TimeSpan.FromMilliseconds(100),
                IntervalMinutes = TimeSpan.FromMilliseconds(200)
            };

            // Setup service provider mock
            var mockScopeServiceProvider = new Mock<IServiceProvider>();
            mockScopeServiceProvider.Setup(sp => sp.GetRequiredService<ICategoryCacheService>())
                .Returns(_mockCategoryCache.Object);
            mockScopeServiceProvider.Setup(sp => sp.GetRequiredService<IProductCacheService>())
                .Returns(_mockProductCache.Object);
            mockScopeServiceProvider.Setup(sp => sp.GetRequiredService<ISystemConfigCacheService>())
                .Returns(_mockConfigCache.Object);
            mockScopeServiceProvider.Setup(sp => sp.GetRequiredService<IUserCacheService>())
                .Returns(_mockUserCache.Object);

            _mockServiceScope.Setup(s => s.ServiceProvider).Returns(mockScopeServiceProvider.Object);
            _mockServiceProvider.Setup(sp => sp.CreateScope()).Returns(_mockServiceScope.Object);
        }

        [Fact]
        public async Task StartAsync_ShouldStartService_Successfully()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<CacheOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_cacheOptions);

            var service = new CacheRefreshBackgroundService(
                _mockLogger.Object,
                optionsMock.Object,
                _mockServiceProvider.Object
            );

            // Act
            await service.StartAsync(CancellationToken.None);

            // Assert
            // Service should start without throwing exceptions
            Assert.True(true);
        }

        [Fact]
        public async Task StopAsync_ShouldStopService_Successfully()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<CacheOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_cacheOptions);

            var service = new CacheRefreshBackgroundService(
                _mockLogger.Object,
                optionsMock.Object,
                _mockServiceProvider.Object
            );

            await service.StartAsync(CancellationToken.None);

            // Act
            await service.StopAsync(CancellationToken.None);

            // Assert
            // Verify the stop log message
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cache Refresh Service is stopping")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldRefreshAllCaches_WhenCalled()
        {
            // Arrange
            var shortOptions = new CacheOptions
            {
                InitialDelaySeconds = TimeSpan.FromMilliseconds(50),
                IntervalMinutes = TimeSpan.FromMilliseconds(100)
            };

            var optionsMock = new Mock<IOptions<CacheOptions>>();
            optionsMock.Setup(o => o.Value).Returns(shortOptions);

            _mockUserCache.Setup(x => x.GetHitRate()).ReturnsAsync(0.75);

            var service = new CacheRefreshBackgroundService(
                _mockLogger.Object,
                optionsMock.Object,
                _mockServiceProvider.Object
            );

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

            // Act
            await service.StartAsync(cts.Token);

            // Wait for at least one cycle to complete
            await Task.Delay(TimeSpan.FromMilliseconds(200), cts.Token);

            await service.StopAsync(CancellationToken.None);

            // Assert
            // Verify that all cache refresh methods were called
            _mockCategoryCache.Verify(x => x.RefreshCategoryTreeAsync(), Times.AtLeastOnce);
            _mockProductCache.Verify(x => x.RefreshAllActiveProductsAsync(), Times.AtLeastOnce);
            _mockConfigCache.Verify(x => x.RefreshJwtOptionsAsync(), Times.AtLeastOnce);
            _mockConfigCache.Verify(x => x.RefreshCacheOptionsAsync(), Times.AtLeastOnce);
            _mockUserCache.Verify(x => x.GetHitRate(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleExceptions_Gracefully()
        {
            // Arrange
            var shortOptions = new CacheOptions
            {
                InitialDelaySeconds = TimeSpan.FromMilliseconds(50),
                IntervalMinutes = TimeSpan.FromMilliseconds(100)
            };

            var optionsMock = new Mock<IOptions<CacheOptions>>();
            optionsMock.Setup(o => o.Value).Returns(shortOptions);

            // Setup one of the cache services to throw an exception
            _mockCategoryCache.Setup(x => x.RefreshCategoryTreeAsync())
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            var service = new CacheRefreshBackgroundService(
                _mockLogger.Object,
                optionsMock.Object,
                _mockServiceProvider.Object
            );

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

            // Act
            await service.StartAsync(cts.Token);

            // Wait for at least one cycle to complete
            await Task.Delay(TimeSpan.FromMilliseconds(200), cts.Token);

            await service.StopAsync(CancellationToken.None);

            // Assert
            // Verify that error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error occurred during cache refresh")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldRespectCancellationToken()
        {
            // Arrange
            var longOptions = new CacheOptions
            {
                InitialDelaySeconds = TimeSpan.FromMilliseconds(50),
                IntervalMinutes = TimeSpan.FromMinutes(10) // Long interval
            };

            var optionsMock = new Mock<IOptions<CacheOptions>>();
            optionsMock.Setup(o => o.Value).Returns(longOptions);

            var service = new CacheRefreshBackgroundService(
                _mockLogger.Object,
                optionsMock.Object,
                _mockServiceProvider.Object
            );

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

            // Act
            await service.StartAsync(cts.Token);

            // Wait for cancellation
            await Task.Delay(TimeSpan.FromMilliseconds(300));

            await service.StopAsync(CancellationToken.None);

            // Assert
            // Verify that cancellation was handled
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("was cancelled")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<CacheOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_cacheOptions);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CacheRefreshBackgroundService(
                    null!,
                    optionsMock.Object,
                    _mockServiceProvider.Object
                ));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenOptionsIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CacheRefreshBackgroundService(
                    _mockLogger.Object,
                    null!,
                    _mockServiceProvider.Object
                ));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenServiceProviderIsNull()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<CacheOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_cacheOptions);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CacheRefreshBackgroundService(
                    _mockLogger.Object,
                    optionsMock.Object,
                    null!
                ));
        }

        [Fact]
        public async Task ExecuteAsync_ShouldLogStartAndCompletion_ForEachCycle()
        {
            // Arrange
            var shortOptions = new CacheOptions
            {
                InitialDelaySeconds = TimeSpan.FromMilliseconds(50),
                IntervalMinutes = TimeSpan.FromMilliseconds(100)
            };

            var optionsMock = new Mock<IOptions<CacheOptions>>();
            optionsMock.Setup(o => o.Value).Returns(shortOptions);

            _mockUserCache.Setup(x => x.GetHitRate()).ReturnsAsync(0.85);

            var service = new CacheRefreshBackgroundService(
                _mockLogger.Object,
                optionsMock.Object,
                _mockServiceProvider.Object
            );

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

            // Act
            await service.StartAsync(cts.Token);

            // Wait for at least one cycle to complete
            await Task.Delay(TimeSpan.FromMilliseconds(200), cts.Token);

            await service.StopAsync(CancellationToken.None);

            // Assert
            // Verify start log
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting cache refresh cycle")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);

            // Verify completion log
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cache refresh cycle completed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
    }
}
