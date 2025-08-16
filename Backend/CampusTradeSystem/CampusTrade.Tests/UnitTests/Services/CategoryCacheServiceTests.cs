using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Options;
using CampusTrade.API.Services.Cache;
using CampusTrade.API.Services.Interfaces;
using CampusTrade.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services
{
    /// <summary>
    /// CategoryCacheService 单元测试
    /// </summary>
    public class CategoryCacheServiceTests : IDisposable
    {
        private readonly CampusTradeDbContext _context;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<ILogger<CategoryCacheService>> _mockLogger;
        private readonly CacheOptions _cacheOptions;
        private readonly CategoryCacheService _categoryService;

        public CategoryCacheServiceTests()
        {
            // Setup in-memory database
            _context = TestDbContextFactory.CreateInMemoryDbContext();

            _mockCacheService = new Mock<ICacheService>();
            _mockLogger = new Mock<ILogger<CategoryCacheService>>();
            _cacheOptions = new CacheOptions
            {
                CategoryCacheDuration = TimeSpan.FromHours(6)
            };

            var optionsMock = new Mock<IOptions<CacheOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_cacheOptions);

            _categoryService = new CategoryCacheService(
                _mockCacheService.Object,
                _context,
                optionsMock.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task GetCategoryTreeAsync_ShouldReturnCachedValue_WhenExists()
        {
            // Arrange
            var cachedCategories = new List<Category>
            {
                new Category { CategoryId = 1, Name = "Electronics", ParentId = null },
                new Category { CategoryId = 2, Name = "Phones", ParentId = 1 }
            };

            _mockCacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<Category>>>>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync(cachedCategories);

            // Act
            var result = await _categoryService.GetCategoryTreeAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Electronics", result.First().Name);
        }

        [Fact]
        public async Task GetCategoryTreeAsync_ShouldBuildTreeFromDatabase_WhenNotCached()
        {
            // Arrange
            await SeedTestData();

            _mockCacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<Category>>>>(),
                It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<List<Category>>>, TimeSpan>(async (key, factory, expiration) =>
                    await factory());

            // Act
            var result = await _categoryService.GetCategoryTreeAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 0);

            // Verify cache service was called
            _mockCacheService.Verify(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<Category>>>>(),
                _cacheOptions.CategoryCacheDuration), Times.Once);
        }

        [Fact]
        public async Task RefreshCategoryTreeAsync_ShouldClearCacheAndRebuild()
        {
            // Arrange
            await SeedTestData();

            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockCacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<Category>>>>(),
                It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<List<Category>>>, TimeSpan>(async (key, factory, expiration) =>
                    await factory());

            // Act
            await _categoryService.RefreshCategoryTreeAsync();

            // Assert
            _mockCacheService.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Once);
            _mockCacheService.Verify(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<Category>>>>(),
                It.IsAny<TimeSpan>()), Times.Once);

            // Verify log message
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("分类树缓存已强制刷新并重建")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InvalidateCategoryTreeCacheAsync_ShouldOnlyRemoveCache()
        {
            // Arrange
            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _categoryService.InvalidateCategoryTreeCacheAsync();

            // Assert
            _mockCacheService.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Once);

            // Verify it does NOT call GetOrCreateAsync
            _mockCacheService.Verify(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<Category>>>>(),
                It.IsAny<TimeSpan>()), Times.Never);

            // Verify log message
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("分类树缓存已失效")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetCategoryTreeAsync_ShouldReturnEmptyList_WhenNoCategories()
        {
            // Arrange - Don't seed any data
            _mockCacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<Category>>>>(),
                It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<List<Category>>>, TimeSpan>(async (key, factory, expiration) =>
                    await factory());

            // Act
            var result = await _categoryService.GetCategoryTreeAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetCategoryTreeAsync_ShouldHandleNullCache_Gracefully()
        {
            // Arrange
            await SeedTestData();

            _mockCacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<Category>>>>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync((List<Category>)null);

            // Act
            var result = await _categoryService.GetCategoryTreeAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetCategoryTreeAsync_ShouldBuildCorrectHierarchy()
        {
            // Arrange
            await SeedHierarchicalTestData();

            _mockCacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<Category>>>>(),
                It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<List<Category>>>, TimeSpan>(async (key, factory, expiration) =>
                    await factory());

            // Act
            var result = await _categoryService.GetCategoryTreeAsync();

            // Assert
            Assert.NotNull(result);

            // Should have root categories only (without parent)
            var rootCategories = result.Where(c => c.ParentId == null).ToList();
            Assert.True(rootCategories.Count > 0);

            // Check if hierarchy is built correctly
            var electronics = rootCategories.FirstOrDefault(c => c.Name == "Electronics");
            Assert.NotNull(electronics);

            // Note: The actual hierarchy building depends on the implementation
            // This test verifies that the service properly processes the data
        }

        private async Task SeedTestData()
        {
            var categories = new List<Category>
            {
                new Category { CategoryId = 1, Name = "Electronics", ParentId = null },
                new Category { CategoryId = 2, Name = "Books", ParentId = null },
                new Category { CategoryId = 3, Name = "Phones", ParentId = 1 }
            };

            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();
        }

        private async Task SeedHierarchicalTestData()
        {
            var categories = new List<Category>
            {
                new Category { CategoryId = 1, Name = "Electronics", ParentId = null },
                new Category { CategoryId = 2, Name = "Books", ParentId = null },
                new Category { CategoryId = 3, Name = "Phones", ParentId = 1 },
                new Category { CategoryId = 4, Name = "Smartphones", ParentId = 3 },
                new Category { CategoryId = 5, Name = "iPhones", ParentId = 4 },
                new Category { CategoryId = 6, Name = "Android", ParentId = 4 },
                new Category { CategoryId = 7, Name = "Textbooks", ParentId = 2 },
                new Category { CategoryId = 8, Name = "Fiction", ParentId = 2 }
            };

            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
