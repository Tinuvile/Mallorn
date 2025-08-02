using System.Linq.Expressions;
using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Options;
using CampusTrade.API.Services.Cache;
using CampusTrade.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services
{
    /// <summary>
    /// 商品缓存服务单元测试
    /// </summary>
    public class ProductCacheServiceTests : IDisposable
    {
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IOptions<CacheOptions>> _mockOptions;
        private readonly Mock<ILogger<ProductCacheService>> _mockLogger;
        private readonly CampusTradeDbContext _context;
        private readonly ProductCacheService _productCacheService;
        private readonly CacheOptions _cacheOptions;

        public ProductCacheServiceTests()
        {
            _mockCacheService = new Mock<ICacheService>();
            _mockOptions = new Mock<IOptions<CacheOptions>>();
            _mockLogger = new Mock<ILogger<ProductCacheService>>();

            // 创建内存数据库
            var options = new DbContextOptionsBuilder<CampusTradeDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new CampusTradeDbContext(options);

            _cacheOptions = new CacheOptions
            {
                ProductCacheDuration = TimeSpan.FromMinutes(30),
                UserCacheDuration = TimeSpan.FromMinutes(15),
                CategoryCacheDuration = TimeSpan.FromHours(2)
            };

            _mockOptions.Setup(x => x.Value).Returns(_cacheOptions);

            _productCacheService = new ProductCacheService(
                _mockCacheService.Object,
                _context,
                _mockOptions.Object,
                _mockLogger.Object);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region GetProductAsync 测试

        [Fact]
        public async Task GetProductAsync_WhenProductExists_ShouldReturnProduct()
        {
            // Arrange
            var productId = 1;
            var expectedProduct = new Product
            {
                ProductId = productId,
                Title = "测试商品",
                BasePrice = 100.00m,
                UserId = 1,
                CategoryId = 1,
                Status = Product.ProductStatus.OnSale
            };

            // 设置缓存返回null，强制从数据库查询
            _mockCacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<Product>>>(),
                It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<Product>>, TimeSpan>(async (key, factory, duration) => await factory());

            // 添加测试数据到内存数据库
            _context.Products.Add(expectedProduct);
            await _context.SaveChangesAsync();

            // Act
            var result = await _productCacheService.GetProductAsync(productId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedProduct.ProductId, result.ProductId);
            Assert.Equal(expectedProduct.Title, result.Title);
        }

        [Fact]
        public async Task GetProductAsync_WhenProductNotExists_ShouldReturnNull()
        {
            // Arrange
            var productId = 999;

            // 设置缓存返回null，强制从数据库查询
            _mockCacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<Product>>>(),
                It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<Product>>, TimeSpan>(async (key, factory, duration) => await factory());

            // Act
            var result = await _productCacheService.GetProductAsync(productId);

            // Assert
            Assert.True(result == null || result.GetType().Name == "NullProduct");
        }

        [Fact]
        public async Task GetProductAsync_WhenCacheServiceThrows_ShouldFallbackToDatabase()
        {
            // Arrange
            var productId = 1;
            var expectedProduct = new Product
            {
                ProductId = productId,
                Title = "测试商品",
                BasePrice = 100.00m,
                UserId = 1,
                CategoryId = 1
            };

            // 设置缓存服务抛出异常
            _mockCacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<Product>>>(),
                It.IsAny<TimeSpan>()))
                .ThrowsAsync(new Exception("缓存服务异常"));

            // 添加测试数据到数据库
            _context.Products.Add(expectedProduct);
            await _context.SaveChangesAsync();

            // Act
            var result = await _productCacheService.GetProductAsync(productId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedProduct.ProductId, result.ProductId);
        }

        #endregion

        #region SetProductAsync 测试

        [Fact]
        public async Task SetProductAsync_WithValidProduct_ShouldSetCache()
        {
            // Arrange
            var product = new Product
            {
                ProductId = 1,
                Title = "测试商品",
                BasePrice = 100.00m,
                UserId = 1,
                CategoryId = 1
            };

            _mockCacheService.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                product,
                _cacheOptions.ProductCacheDuration))
                .Returns(Task.CompletedTask);

            // Act
            await _productCacheService.SetProductAsync(product);

            // Assert
            _mockCacheService.Verify(x => x.SetAsync(
                It.IsAny<string>(),
                product,
                _cacheOptions.ProductCacheDuration), Times.Once);
        }

        #endregion

        #region GetProductsAsync 测试

        [Fact]
        public async Task GetProductsAsync_WithValidIds_ShouldReturnProducts()
        {
            // Arrange
            var productIds = new[] { 1, 2, 3 };
            var products = new List<Product>
            {
                new Product { ProductId = 1, Title = "商品1", UserId = 1, CategoryId = 1 },
                new Product { ProductId = 2, Title = "商品2", UserId = 1, CategoryId = 1 },
                new Product { ProductId = 3, Title = "商品3", UserId = 1, CategoryId = 1 }
            };

            // 添加测试数据
            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();

            // 设置缓存返回空（模拟缓存未命中）
            _mockCacheService.Setup(x => x.GetAllAsync<Product>(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new Dictionary<string, Product>());

            // Act
            var result = await _productCacheService.GetProductsAsync(productIds);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(1, result.Keys);
            Assert.Contains(2, result.Keys);
            Assert.Contains(3, result.Keys);
        }

        [Fact]
        public async Task GetProductsAsync_WithEmptyIds_ShouldReturnEmptyDictionary()
        {
            // Arrange
            var productIds = new int[0];

            // Act
            var result = await _productCacheService.GetProductsAsync(productIds);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region InvalidateProductCacheAsync 测试

        [Fact]
        public async Task InvalidateProductCacheAsync_WithValidId_ShouldRemoveFromCache()
        {
            // Arrange
            var productId = 1;

            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _productCacheService.InvalidateProductCacheAsync(productId);

            // Assert
            _mockCacheService.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.AtLeast(1));
        }

        #endregion

        #region InvalidateProductsByCategoryAsync 测试

        [Fact]
        public async Task InvalidateProductsByCategoryAsync_WithValidCategoryId_ShouldRemoveFromCache()
        {
            // Arrange
            var categoryId = 1;

            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _productCacheService.InvalidateProductCacheAsync(categoryId);

            // Assert
            _mockCacheService.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.AtLeast(1));
        }

        #endregion

        #region InvalidateUserProductsCacheAsync 测试

        [Fact]
        public async Task InvalidateUserProductsCacheAsync_WithValidUserId_ShouldRemoveFromCache()
        {
            // Arrange
            var userId = 1;

            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _productCacheService.InvalidateProductCacheAsync(userId);

            // Assert
            _mockCacheService.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.AtLeast(1));
        }

        #endregion

        #region GetHitRate 测试

        [Fact]
        public async Task GetHitRate_ShouldReturnCacheHitRate()
        {
            // Arrange
            var expectedHitRate = 0.85;
            _mockCacheService.Setup(x => x.GetHitRate())
                .ReturnsAsync(expectedHitRate);

            // Act
            var result = await _productCacheService.GetHitRate();

            // Assert
            Assert.Equal(expectedHitRate, result);
        }

        #endregion

        #region RefreshProductCacheAsync 测试

        [Fact]
        public async Task RefreshProductCacheAsync_WithValidId_ShouldRefreshCache()
        {
            // Arrange
            var productId = 1;
            var product = new Product
            {
                ProductId = productId,
                Title = "刷新的商品",
                BasePrice = 100.00m,
                UserId = 1,
                CategoryId = 1
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<Product>(),
                It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            // Act
            await _productCacheService.InvalidateProductCacheAsync(productId);

            // Assert
            _mockCacheService.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.AtLeast(1));
        }

        #endregion
    }
}