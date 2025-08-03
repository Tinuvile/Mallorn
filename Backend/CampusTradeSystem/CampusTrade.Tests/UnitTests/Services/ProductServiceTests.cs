using System.Linq.Expressions;
using CampusTrade.API.Models.DTOs.Product;
using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Services.Product;
using CampusTrade.API.Services.Interfaces;
using CampusTrade.API.Services.File;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services
{
    /// <summary>
    /// 商品服务单元测试
    /// </summary>
    public class ProductServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IProductCacheService> _mockProductCache;
        private readonly Mock<ICategoryCacheService> _mockCategoryCache;
        private readonly Mock<IUserCacheService> _mockUserCache;
        private readonly Mock<IFileService> _mockFileService;
        private readonly Mock<ILogger<ProductService>> _mockLogger;
        private readonly Mock<IProductRepository> _mockProductRepository;
        private readonly Mock<IRepository<ProductImage>> _mockProductImagesRepository;
        private readonly Mock<ICategoriesRepository> _mockCategoriesRepository;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockProductCache = new Mock<IProductCacheService>();
            _mockCategoryCache = new Mock<ICategoryCacheService>();
            _mockUserCache = new Mock<IUserCacheService>();
            _mockFileService = new Mock<IFileService>();
            _mockLogger = new Mock<ILogger<ProductService>>();
            _mockProductRepository = new Mock<IProductRepository>();
            _mockProductImagesRepository = new Mock<IRepository<ProductImage>>();
            _mockCategoriesRepository = new Mock<ICategoriesRepository>();

            // 设置UnitOfWork的属性
            _mockUnitOfWork.Setup(x => x.Products).Returns(_mockProductRepository.Object);
            _mockUnitOfWork.Setup(x => x.ProductImages).Returns(_mockProductImagesRepository.Object);
            _mockUnitOfWork.Setup(x => x.Categories).Returns(_mockCategoriesRepository.Object);

            _productService = new ProductService(
                _mockUnitOfWork.Object,
                _mockProductCache.Object,
                _mockCategoryCache.Object,
                _mockUserCache.Object,
                _mockFileService.Object,
                _mockLogger.Object
            );
        }

        #region 商品发布测试

        [Fact]
        public async Task CreateProductAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var userId = 1;
            var createDto = new CreateProductDto
            {
                Title = "测试商品",
                Description = "测试商品描述",
                BasePrice = 100.00m,
                CategoryId = 1,
                ImageUrls = new List<string> { "http://example.com/image1.jpg" }
            };

            var mockUser = new User { UserId = userId, Username = "testuser" };
            var mockCategory = new Category { CategoryId = 1, Name = "测试分类" };
            var categories = new List<Category> { mockCategory };

            // 设置Mock返回值
            _mockUserCache.Setup(x => x.GetUserAsync(userId))
                .ReturnsAsync(mockUser);
            _mockCategoryCache.Setup(x => x.GetCategoryTreeAsync())
                .ReturnsAsync(categories);
            _mockProductRepository.Setup(x => x.IsProductExistsAsync(createDto.Title, userId))
                .ReturnsAsync(false);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockProductRepository.Setup(x => x.AddAsync(It.IsAny<Product>()))
                .ReturnsAsync((Product p) => p);

            // 模拟GetByPrimaryKeyAsync返回创建的产品
            var expectedProduct = new Product
            {
                ProductId = 1,
                UserId = userId,
                Title = createDto.Title,
                Description = createDto.Description,
                BasePrice = createDto.BasePrice,
                CategoryId = createDto.CategoryId,
                Status = Product.ProductStatus.OnSale,
                PublishTime = DateTime.Now,
                ViewCount = 0
            };

            _mockProductRepository.Setup(x => x.GetByPrimaryKeyAsync(It.IsAny<int>()))
                .ReturnsAsync(expectedProduct);

            // Mock GetWithIncludeAsync for GetProductDetailInternalAsync
            _mockProductRepository.Setup(x => x.GetWithIncludeAsync(
                It.IsAny<Expression<Func<Product, bool>>>(),
                null,
                It.IsAny<Expression<Func<Product, object>>[]>()))
                .ReturnsAsync(new List<Product> { expectedProduct });

            // Act
            var result = await _productService.CreateProductAsync(createDto, userId);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(createDto.Title, result.Data.Title);
            _mockProductRepository.Verify(x => x.AddAsync(It.IsAny<Product>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task CreateProductAsync_WithNonExistentUser_ShouldReturnError()
        {
            // Arrange
            var userId = 999;
            var createDto = new CreateProductDto
            {
                Title = "测试商品",
                BasePrice = 100.00m,
                CategoryId = 1
            };

            _mockUserCache.Setup(x => x.GetUserAsync(userId))
                .ReturnsAsync((User)null);

            // Act
            var result = await _productService.CreateProductAsync(createDto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("用户不存在", result.Message);
        }

        [Fact]
        public async Task CreateProductAsync_WithDuplicateTitle_ShouldReturnError()
        {
            // Arrange
            var userId = 1;
            var createDto = new CreateProductDto
            {
                Title = "重复商品标题",
                BasePrice = 100.00m,
                CategoryId = 1
            };

            var mockUser = new User { UserId = userId };
            var mockCategory = new Category { CategoryId = 1, Name = "测试分类" };
            var categories = new List<Category> { mockCategory };

            _mockUserCache.Setup(x => x.GetUserAsync(userId))
                .ReturnsAsync(mockUser);
            _mockCategoryCache.Setup(x => x.GetCategoryTreeAsync())
                .ReturnsAsync(categories);
            _mockProductRepository.Setup(x => x.IsProductExistsAsync(createDto.Title, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _productService.CreateProductAsync(createDto, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("同名商品", result.Message);
        }

        #endregion

        #region 商品更新测试

        [Fact]
        public async Task UpdateProductAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var productId = 1;
            var userId = 1;
            var updateDto = new UpdateProductDto
            {
                Title = "更新后的商品标题",
                BasePrice = 150.00m
            };

            var existingProduct = new Product
            {
                ProductId = productId,
                UserId = userId,
                Title = "原商品标题",
                BasePrice = 100.00m,
                CategoryId = 1
            };

            _mockProductRepository.Setup(x => x.GetByPrimaryKeyAsync(productId))
                .ReturnsAsync(existingProduct);
            _mockProductRepository.Setup(x => x.IsProductExistsAsync(updateDto.Title, userId))
                .ReturnsAsync(false);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _productService.UpdateProductAsync(productId, updateDto, userId);

            // Assert
            Assert.True(result.Success);
            _mockProductRepository.Verify(x => x.Update(existingProduct), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateProductAsync_WithUnauthorizedUser_ShouldReturnError()
        {
            // Arrange
            var productId = 1;
            var userId = 1;
            var unauthorizedUserId = 2;
            var updateDto = new UpdateProductDto { Title = "新标题" };

            var existingProduct = new Product
            {
                ProductId = productId,
                UserId = userId,
                Title = "原标题"
            };

            _mockProductRepository.Setup(x => x.GetByPrimaryKeyAsync(productId))
                .ReturnsAsync(existingProduct);

            // Act
            var result = await _productService.UpdateProductAsync(productId, updateDto, unauthorizedUserId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("无权限", result.Message);
        }

        #endregion

        #region 商品删除测试

        [Fact]
        public async Task DeleteProductAsync_WithValidProduct_ShouldReturnSuccess()
        {
            // Arrange
            var productId = 1;
            var userId = 1;
            var product = new Product
            {
                ProductId = productId,
                UserId = userId,
                Title = "测试商品",
                Status = Product.ProductStatus.OnSale
            };

            _mockProductRepository.Setup(x => x.GetByPrimaryKeyAsync(productId))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _productService.DeleteProductAsync(productId, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(Product.ProductStatus.OffShelf, product.Status);
            _mockProductRepository.Verify(x => x.Update(product), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region 商品查询测试

        [Fact]
        public async Task GetProductsAsync_WithValidQuery_ShouldReturnPagedResult()
        {
            // Arrange
            var queryDto = new ProductQueryDto
            {
                PageIndex = 0,
                PageSize = 10,
                Keyword = "测试"
            };

            var products = new List<Product>
            {
                new Product { ProductId = 1, Title = "测试商品1", UserId = 1, CategoryId = 1 },
                new Product { ProductId = 2, Title = "测试商品2", UserId = 1, CategoryId = 1 }
            };

            _mockProductRepository.Setup(x => x.GetPagedProductsAsync(
                queryDto.PageIndex, queryDto.PageSize, queryDto.CategoryId,
                queryDto.Status, queryDto.Keyword, queryDto.MinPrice,
                queryDto.MaxPrice, queryDto.UserId))
                .ReturnsAsync((products, 2));

            var mockUser = new User { UserId = 1, Username = "testuser" };
            _mockUserCache.Setup(x => x.GetUserAsync(It.IsAny<int>()))
                .ReturnsAsync(mockUser);

            var mockCategory = new Category { CategoryId = 1, Name = "测试分类" };
            _mockCategoryCache.Setup(x => x.GetCategoryTreeAsync())
                .ReturnsAsync(new List<Category> { mockCategory });

            _mockProductImagesRepository.Setup(x => x.FindAsync(It.IsAny<Expression<Func<ProductImage, bool>>>()))
                .ReturnsAsync(new List<ProductImage>());

            // Act
            var result = await _productService.GetProductsAsync(queryDto);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.TotalCount);
            Assert.Equal(2, result.Data.Products.Count);
        }

        #endregion

        #region 智能下架测试

        [Fact]
        public async Task ProcessAutoRemoveProductsAsync_WithHighViewProduct_ShouldExtendTime()
        {
            // Arrange
            var highViewProduct = new Product
            {
                ProductId = 1,
                ViewCount = 150, // 超过高浏览量阈值
                Status = Product.ProductStatus.OnSale,
                PublishTime = DateTime.Now.AddDays(-19),
                AutoRemoveTime = DateTime.Now.AddDays(1),
                UserId = 1,
                CategoryId = 1
            };

            var productsToRemove = new List<Product> { highViewProduct };

            _mockProductRepository.Setup(x => x.GetAutoRemoveProductsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(productsToRemove);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _productService.ProcessAutoRemoveProductsAsync();

            // Assert
            Assert.Equal(1, result.ProcessedCount);
            Assert.Equal(0, result.SuccessCount);
            Assert.Equal(1, result.ExtendedCount);
            Assert.True(highViewProduct.AutoRemoveTime > DateTime.Now.AddDays(5));
        }

        [Fact]
        public async Task ProcessAutoRemoveProductsAsync_WithLowViewProduct_ShouldRemove()
        {
            // Arrange
            var lowViewProduct = new Product
            {
                ProductId = 1,
                ViewCount = 50, // 低于高浏览量阈值
                Status = Product.ProductStatus.OnSale,
                PublishTime = DateTime.Now.AddDays(-21),
                AutoRemoveTime = DateTime.Now.AddDays(-1),
                UserId = 1,
                CategoryId = 1
            };

            var productsToRemove = new List<Product> { lowViewProduct };

            _mockProductRepository.Setup(x => x.GetAutoRemoveProductsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(productsToRemove);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _productService.ProcessAutoRemoveProductsAsync();

            // Assert
            Assert.Equal(1, result.ProcessedCount);
            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(0, result.ExtendedCount);
            Assert.Equal(Product.ProductStatus.OffShelf, lowViewProduct.Status);
        }

        #endregion

        #region 图片管理测试

        [Fact]
        public async Task AddProductImagesAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var productId = 1;
            var userId = 1;
            var imageUrls = new List<string> { "http://example.com/image1.jpg", "http://example.com/image2.jpg" };

            var product = new Product
            {
                ProductId = productId,
                UserId = userId,
                Title = "测试商品"
            };

            _mockProductRepository.Setup(x => x.GetByPrimaryKeyAsync(productId))
                .ReturnsAsync(product);
            _mockProductImagesRepository.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<ProductImage>>()))
                .ReturnsAsync((IEnumerable<ProductImage> images) => images);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _productService.AddProductImagesAsync(productId, imageUrls, userId);

            // Assert
            Assert.True(result.Success);
            _mockProductImagesRepository.Verify(x => x.AddRangeAsync(It.IsAny<IEnumerable<ProductImage>>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RemoveProductImageAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var productId = 1;
            var imageId = 1;
            var userId = 1;

            var product = new Product { ProductId = productId, UserId = userId };
            var image = new ProductImage { ImageId = imageId, ProductId = productId };

            _mockProductRepository.Setup(x => x.GetByPrimaryKeyAsync(productId))
                .ReturnsAsync(product);
            _mockProductImagesRepository.Setup(x => x.GetByPrimaryKeyAsync(imageId))
                .ReturnsAsync(image);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _productService.RemoveProductImageAsync(productId, imageId, userId);

            // Assert
            Assert.True(result.Success);
            _mockProductImagesRepository.Verify(x => x.Delete(image), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region 搜索功能测试

        [Fact]
        public async Task SearchProductsAsync_WithValidKeyword_ShouldReturnResults()
        {
            // Arrange
            var keyword = "测试商品";
            var pageIndex = 0;
            var pageSize = 10;

            var searchResults = new List<Product>
            {
                new Product { ProductId = 1, Title = "测试商品1", UserId = 1, CategoryId = 1, Status = Product.ProductStatus.OnSale },
                new Product { ProductId = 2, Title = "测试商品2", UserId = 1, CategoryId = 1, Status = Product.ProductStatus.OnSale }
            };

            _mockProductRepository.Setup(x => x.GetPagedProductsAsync(
                pageIndex, pageSize, null, Product.ProductStatus.OnSale, keyword, null, null, null))
                .ReturnsAsync((searchResults, 2));

            var mockUser = new User { UserId = 1, Username = "testuser" };
            _mockUserCache.Setup(x => x.GetUserAsync(It.IsAny<int>()))
                .ReturnsAsync(mockUser);

            var mockCategory = new Category { CategoryId = 1, Name = "测试分类" };
            _mockCategoryCache.Setup(x => x.GetCategoryTreeAsync())
                .ReturnsAsync(new List<Category> { mockCategory });

            _mockProductImagesRepository.Setup(x => x.FindAsync(It.IsAny<Expression<Func<ProductImage, bool>>>()))
                .ReturnsAsync(new List<ProductImage>());

            // Act
            var result = await _productService.SearchProductsAsync(keyword, pageIndex, pageSize);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.TotalCount);
            Assert.Equal(2, result.Data.Products.Count);
        }

        [Fact]
        public async Task SearchProductsAsync_WithEmptyKeyword_ShouldReturnError()
        {
            // Act
            var result = await _productService.SearchProductsAsync("", 0, 10);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("关键词不能为空", result.Message);
        }

        #endregion

        #region 分类管理测试

        [Fact]
        public async Task GetCategoryTreeAsync_WithValidData_ShouldReturnTree()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category { CategoryId = 1, Name = "教材", ParentId = null, Children = new List<Category>() },
                new Category { CategoryId = 2, Name = "数码", ParentId = null, Children = new List<Category>() },
                new Category { CategoryId = 3, Name = "日用", ParentId = null, Children = new List<Category>() }
            };

            _mockCategoryCache.Setup(x => x.GetCategoryTreeAsync())
                .ReturnsAsync(categories);
            _mockCategoriesRepository.Setup(x => x.GetProductCountByCategoryAsync(It.IsAny<int>()))
                .ReturnsAsync(5);
            _mockCategoriesRepository.Setup(x => x.GetActiveProductCountByCategoryAsync(It.IsAny<int>()))
                .ReturnsAsync(3);

            // Act
            var result = await _productService.GetCategoryTreeAsync(true);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(3, result.Data.RootCategories.Count);
            Assert.Equal(3, result.Data.TotalCount);
        }

        #endregion
    }
}