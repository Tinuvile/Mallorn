using System.Security.Claims;
using CampusTrade.API.Controllers;
using CampusTrade.API.Models.DTOs.Product;
using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Services.Product;
using CampusTrade.API.Services.File;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Controllers
{
    /// <summary>
    /// 商品控制器单元测试
    /// </summary>
    public class ProductControllerTests
    {
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<IFileService> _mockFileService;
        private readonly Mock<ILogger<ProductController>> _mockLogger;
        private readonly ProductController _controller;

        public ProductControllerTests()
        {
            _mockProductService = new Mock<IProductService>();
            _mockFileService = new Mock<IFileService>();
            _mockLogger = new Mock<ILogger<ProductController>>();

            _controller = new ProductController(
                _mockProductService.Object,
                _mockFileService.Object,
                _mockLogger.Object);

            // 设置用户身份
            SetupUserClaims(1);
        }

        private void SetupUserClaims(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }

        #region 商品发布测试

        [Fact]
        public async Task CreateProduct_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var createDto = new CreateProductDto
            {
                Title = "测试商品",
                Description = "测试商品描述",
                BasePrice = 100.00m,
                CategoryId = 1
            };

            var expectedResult = ApiResponse<ProductDetailDto>.CreateSuccess(
                new ProductDetailDto { ProductId = 1, Title = "测试商品" },
                "商品发布成功");

            _mockProductService.Setup(x => x.CreateProductAsync(createDto, 1))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateProduct(createDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductDetailDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("测试商品", response.Data.Title);
        }

        [Fact]
        public async Task CreateProduct_WithInvalidData_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateProductDto
            {
                Title = "测试商品",
                BasePrice = 100.00m,
                CategoryId = 1
            };

            var expectedResult = ApiResponse<ProductDetailDto>.CreateError("分类不存在");

            _mockProductService.Setup(x => x.CreateProductAsync(createDto, 1))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateProduct(createDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductDetailDto>>(badRequestResult.Value);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task CreateProduct_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var createDto = new CreateProductDto
            {
                Title = "测试商品",
                BasePrice = 100.00m,
                CategoryId = 1
            };

            // 移除用户身份
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            // Act
            var result = await _controller.CreateProduct(createDto);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(unauthorizedResult.Value);
            Assert.False(response.Success);
            Assert.Contains("未登录", response.Message);
        }

        #endregion

        #region 商品更新测试

        [Fact]
        public async Task UpdateProduct_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var productId = 1;
            var updateDto = new UpdateProductDto
            {
                Title = "更新后的商品",
                BasePrice = 150.00m
            };

            var expectedResult = ApiResponse<ProductDetailDto>.CreateSuccess(
                new ProductDetailDto { ProductId = productId, Title = "更新后的商品" },
                "商品更新成功");

            _mockProductService.Setup(x => x.UpdateProductAsync(productId, updateDto, 1))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.UpdateProduct(productId, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductDetailDto>>(okResult.Value);
            Assert.True(response.Success);
        }

        #endregion

        #region 商品删除测试

        [Fact]
        public async Task DeleteProduct_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var productId = 1;
            var expectedResult = ApiResponse.CreateSuccess("商品删除成功");

            _mockProductService.Setup(x => x.DeleteProductAsync(productId, 1))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.DeleteProduct(productId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(okResult.Value);
            Assert.True(response.Success);
        }

        #endregion

        #region 商品查询测试

        [Fact]
        public async Task GetProductDetail_WithValidId_ShouldReturnOk()
        {
            // Arrange
            var productId = 1;
            var expectedResult = ApiResponse<ProductDetailDto>.CreateSuccess(
                new ProductDetailDto { ProductId = productId, Title = "测试商品" });

            _mockProductService.Setup(x => x.GetProductDetailAsync(productId, 1, true))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetProductDetail(productId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductDetailDto>>(okResult.Value);
            Assert.True(response.Success);
        }

        [Fact]
        public async Task GetProductDetail_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var productId = 999;
            var expectedResult = ApiResponse<ProductDetailDto>.CreateError("商品不存在");

            _mockProductService.Setup(x => x.GetProductDetailAsync(productId, 1, true))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetProductDetail(productId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductDetailDto>>(notFoundResult.Value);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task GetProducts_WithValidQuery_ShouldReturnOk()
        {
            // Arrange
            var queryDto = new ProductQueryDto
            {
                PageIndex = 0,
                PageSize = 10,
                Keyword = "测试"
            };

            var expectedResult = ApiResponse<ProductPagedListDto>.CreateSuccess(
                new ProductPagedListDto
                {
                    Products = new List<ProductListDto>
                    {
                        new ProductListDto { ProductId = 1, Title = "测试商品1" },
                        new ProductListDto { ProductId = 2, Title = "测试商品2" }
                    },
                    TotalCount = 2
                });

            _mockProductService.Setup(x => x.GetProductsAsync(queryDto, 1))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetProducts(queryDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductPagedListDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data.TotalCount);
        }

        #endregion

        #region 搜索功能测试

        [Fact]
        public async Task SearchProducts_WithValidKeyword_ShouldReturnOk()
        {
            // Arrange
            var keyword = "测试商品";
            var expectedResult = ApiResponse<ProductPagedListDto>.CreateSuccess(
                new ProductPagedListDto
                {
                    Products = new List<ProductListDto>
                    {
                        new ProductListDto { ProductId = 1, Title = "测试商品" }
                    },
                    TotalCount = 1
                });

            _mockProductService.Setup(x => x.SearchProductsAsync(keyword, 0, 20, null))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.SearchProducts(keyword);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductPagedListDto>>(okResult.Value);
            Assert.True(response.Success);
        }

        [Fact]
        public async Task SearchProducts_WithEmptyKeyword_ShouldReturnBadRequest()
        {
            // Arrange
            var keyword = "";
            var expectedResult = ApiResponse<ProductPagedListDto>.CreateError("搜索关键词不能为空");

            _mockProductService.Setup(x => x.SearchProductsAsync(keyword, 0, 20, null))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.SearchProducts(keyword);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductPagedListDto>>(badRequestResult.Value);
            Assert.False(response.Success);
        }

        #endregion

        #region 分类管理测试

        [Fact]
        public async Task GetCategoryTree_ShouldReturnOk()
        {
            // Arrange
            var expectedResult = ApiResponse<CategoryTreeDto>.CreateSuccess(
                new CategoryTreeDto
                {
                    RootCategories = new List<CategoryDto>
                    {
                        new CategoryDto { CategoryId = 1, Name = "教材" },
                        new CategoryDto { CategoryId = 2, Name = "数码" },
                        new CategoryDto { CategoryId = 3, Name = "日用" }
                    },
                    TotalCount = 3
                });

            _mockProductService.Setup(x => x.GetCategoryTreeAsync(true))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetCategoryTree();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<CategoryTreeDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(3, response.Data.TotalCount);
        }

        [Fact]
        public async Task GetSubCategories_WithParentId_ShouldReturnOk()
        {
            // Arrange
            var parentId = 1;
            var expectedResult = ApiResponse<List<CategoryDto>>.CreateSuccess(
                new List<CategoryDto>
                {
                    new CategoryDto { CategoryId = 4, Name = "计算机科学", ParentId = parentId },
                    new CategoryDto { CategoryId = 5, Name = "数学", ParentId = parentId }
                });

            _mockProductService.Setup(x => x.GetSubCategoriesAsync(parentId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetSubCategories(parentId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<List<CategoryDto>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data.Count);
        }

        #endregion

        #region 智能下架管理测试

        [Fact]
        public async Task GetProductsToAutoRemove_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var days = 7;
            var expectedResult = ApiResponse<List<ProductListDto>>.CreateSuccess(
                new List<ProductListDto>
                {
                    new ProductListDto { ProductId = 1, Title = "即将下架商品1" },
                    new ProductListDto { ProductId = 2, Title = "即将下架商品2" }
                });

            _mockProductService.Setup(x => x.GetProductsToAutoRemoveAsync(days, 1))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetProductsToAutoRemove(days);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<List<ProductListDto>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data.Count);
        }

        [Fact]
        public async Task ExtendProductAutoRemoveTime_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var productId = 1;
            var extendDays = 10;
            var expectedResult = ApiResponse.CreateSuccess("商品下架时间已延期10天");

            _mockProductService.Setup(x => x.ExtendProductAutoRemoveTimeAsync(productId, extendDays, 1))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.ExtendProductAutoRemoveTime(productId, extendDays);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(okResult.Value);
            Assert.True(response.Success);
        }

        #endregion
    }
}