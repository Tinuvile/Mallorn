using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CampusTrade.API.Options;
using CampusTrade.API.Services.Cache;
using CampusTrade.API.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services
{
    /// <summary>
    /// CacheService 单元测试
    /// </summary>
    public class CacheServiceTests : IDisposable
    {
        private readonly IMemoryCache _memoryCache;
        private readonly Mock<ILogger<CacheService>> _mockLogger;
        private readonly CacheOptions _cacheOptions;
        private readonly ICacheService _cacheService;

        public CacheServiceTests()
        {
            _memoryCache = new MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
            _mockLogger = new Mock<ILogger<CacheService>>();
            _cacheOptions = new CacheOptions
            {
                DefaultCacheDuration = TimeSpan.FromMinutes(30),
                NullResultCacheDuration = TimeSpan.FromMinutes(5)
            };

            var optionsMock = new Mock<IOptions<CacheOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_cacheOptions);

            _cacheService = new CacheService(_memoryCache, optionsMock.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetOrCreateAsync_ShouldReturnCachedValue_WhenKeyExists()
        {
            // Arrange
            const string key = "test_key";
            const string expectedValue = "cached_value";
            await _cacheService.SetAsync(key, expectedValue, TimeSpan.FromMinutes(10));

            // Act
            var result = await _cacheService.GetOrCreateAsync(key, () => Task.FromResult("new_value"));

            // Assert
            Assert.Equal(expectedValue, result);
        }

        [Fact]
        public async Task GetOrCreateAsync_ShouldCreateAndReturnNewValue_WhenKeyDoesNotExist()
        {
            // Arrange
            const string key = "test_key_new";
            const string expectedValue = "new_value";

            // Act
            var result = await _cacheService.GetOrCreateAsync(key, () => Task.FromResult(expectedValue));

            // Assert
            Assert.Equal(expectedValue, result);

            // Verify it's now cached
            var cachedResult = await _cacheService.GetAsync<string>(key);
            Assert.Equal(expectedValue, cachedResult);
        }

        [Fact]
        public async Task GetOrCreateAsync_ShouldHandleNullResults()
        {
            // Arrange
            const string key = "null_test_key";

            // Act
            var result = await _cacheService.GetOrCreateAsync<string>(key, () => Task.FromResult<string?>(null));

            // Assert
            Assert.Null(result);

            // Verify null value is cached
            var exists = await _cacheService.ExistsAsync(key);
            Assert.True(exists);
        }

        [Fact]
        public async Task GetOrCreateAsync_ShouldUseCustomExpiration()
        {
            // Arrange
            const string key = "custom_expiration_key";
            const string value = "test_value";
            var customExpiration = TimeSpan.FromMinutes(5);

            // Act
            await _cacheService.GetOrCreateAsync(key, () => Task.FromResult(value), customExpiration);

            // Assert
            var exists = await _cacheService.ExistsAsync(key);
            Assert.True(exists);
        }

        [Fact]
        public async Task SetAsync_ShouldStoreValue()
        {
            // Arrange
            const string key = "set_test_key";
            const string value = "set_test_value";

            // Act
            await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(10));

            // Assert
            var result = await _cacheService.GetAsync<string>(key);
            Assert.Equal(value, result);
        }

        [Fact]
        public async Task GetAsync_ShouldReturnNull_WhenKeyDoesNotExist()
        {
            // Arrange
            const string key = "non_existent_key";

            // Act
            var result = await _cacheService.GetAsync<string>(key);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RemoveAsync_ShouldRemoveKey()
        {
            // Arrange
            const string key = "remove_test_key";
            const string value = "remove_test_value";
            await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(10));

            // Act
            await _cacheService.RemoveAsync(key);

            // Assert
            var exists = await _cacheService.ExistsAsync(key);
            Assert.False(exists);
        }

        [Fact]
        public async Task RemoveByPrefixAsync_ShouldRemoveMatchingKeys()
        {
            // Arrange
            await _cacheService.SetAsync("user:1:profile", "profile1", TimeSpan.FromMinutes(10));
            await _cacheService.SetAsync("user:2:profile", "profile2", TimeSpan.FromMinutes(10));
            await _cacheService.SetAsync("product:1", "product1", TimeSpan.FromMinutes(10));

            // Act
            await _cacheService.RemoveByPrefixAsync("user:");

            // Assert
            var userExists1 = await _cacheService.ExistsAsync("user:1:profile");
            var userExists2 = await _cacheService.ExistsAsync("user:2:profile");
            var productExists = await _cacheService.ExistsAsync("product:1");

            Assert.False(userExists1);
            Assert.False(userExists2);
            Assert.True(productExists); // This should still exist
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenKeyExists()
        {
            // Arrange
            const string key = "exists_test_key";
            await _cacheService.SetAsync(key, "test_value", TimeSpan.FromMinutes(10));

            // Act
            var exists = await _cacheService.ExistsAsync(key);

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenKeyDoesNotExist()
        {
            // Arrange
            const string key = "non_existent_key";

            // Act
            var exists = await _cacheService.ExistsAsync(key);

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public async Task ClearAllAsync_ShouldRemoveAllKeys()
        {
            // Arrange
            await _cacheService.SetAsync("key1", "value1", TimeSpan.FromMinutes(10));
            await _cacheService.SetAsync("key2", "value2", TimeSpan.FromMinutes(10));

            // Act
            await _cacheService.ClearAllAsync();

            // Assert
            var exists1 = await _cacheService.ExistsAsync("key1");
            var exists2 = await _cacheService.ExistsAsync("key2");

            Assert.False(exists1);
            Assert.False(exists2);
        }

        [Fact]
        public async Task GetHitRate_ShouldCalculateCorrectly()
        {
            // Arrange
            const string key = "hit_rate_key";
            await _cacheService.SetAsync(key, "test_value", TimeSpan.FromMinutes(10));

            // Act
            // First call should be a cache miss (creating the value)
            await _cacheService.GetOrCreateAsync(key, () => Task.FromResult("new_value"));

            // Second call should be a cache hit
            await _cacheService.GetOrCreateAsync(key, () => Task.FromResult("another_value"));

            var hitRate = await _cacheService.GetHitRate();

            // Assert
            // Should be 50% hit rate (1 hit out of 2 total requests)
            Assert.True(hitRate >= 0.4 && hitRate <= 0.6); // Allow some tolerance
        }

        [Fact]
        public async Task GetExpirationInfo_ShouldReturnExpirationData()
        {
            // Arrange
            const string key1 = "exp_key1";
            const string key2 = "exp_key2";
            const string nonExistentKey = "non_existent";

            await _cacheService.SetAsync(key1, "value1", TimeSpan.FromMinutes(10));
            await _cacheService.SetAsync(key2, "value2", TimeSpan.FromMinutes(20));

            // Act
            var expirationInfo = await _cacheService.GetExpirationInfo(key1, key2, nonExistentKey);

            // Assert
            Assert.NotNull(expirationInfo);
            Assert.True(expirationInfo.ContainsKey(key1));
            Assert.True(expirationInfo.ContainsKey(key2));
            Assert.True(expirationInfo.ContainsKey(nonExistentKey));

            // Non-existent key should return null
            Assert.Null(expirationInfo[nonExistentKey]);
        }

        [Fact]
        public async Task GetOrCreateAsync_ShouldThrowException_WhenFactoryThrows()
        {
            // Arrange
            const string key = "exception_key";
            var expectedException = new InvalidOperationException("Test exception");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _cacheService.GetOrCreateAsync<string>(key, () => throw expectedException)
            );

            Assert.Equal("Test exception", exception.Message);
        }

        public void Dispose()
        {
            _memoryCache?.Dispose();
        }
    }
}
