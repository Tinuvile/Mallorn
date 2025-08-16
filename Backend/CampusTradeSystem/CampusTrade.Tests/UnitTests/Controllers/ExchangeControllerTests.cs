using System.Security.Claims;
using CampusTrade.API.Controllers;
using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Models.DTOs.Exchange;
using CampusTrade.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Controllers
{
    /// <summary>
    /// 换物控制器单元测试
    /// </summary>
    public class ExchangeControllerTests
    {
        private readonly Mock<IExchangeService> _mockExchangeService;
        private readonly Mock<ILogger<ExchangeController>> _mockLogger;
        private readonly ExchangeController _controller;
        private readonly ClaimsPrincipal _testUser;

        public ExchangeControllerTests()
        {
            _mockExchangeService = new Mock<IExchangeService>();
            _mockLogger = new Mock<ILogger<ExchangeController>>();
            _controller = new ExchangeController(_mockExchangeService.Object, _mockLogger.Object);

            // 设置测试用户
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "1"),
                new("userId", "1")
            };
            _testUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = _testUser }
            };
        }

        #region CreateExchangeRequest Tests

        [Fact]
        public async Task CreateExchangeRequest_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new ExchangeRequestDto
            {
                OfferProductId = 1,
                RequestProductId = 2,
                Terms = "商品状态良好，希望交换"
            };

            _mockExchangeService.Setup(x => x.CreateExchangeRequestAsync(It.IsAny<ExchangeRequestDto>(), It.IsAny<int>()))
                .ReturnsAsync((true, "换物请求已发送", 1));

            // Act
            var result = await _controller.CreateExchangeRequest(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Message.Should().Be("换物请求已发送");
        }

        [Fact]
        public async Task CreateExchangeRequest_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var request = new ExchangeRequestDto(); // Invalid request
            _controller.ModelState.AddModelError("OfferProductId", "提供商品ID不能为空");

            // Act
            var result = await _controller.CreateExchangeRequest(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Message.Should().Contain("请求参数无效");
        }

        [Fact]
        public async Task CreateExchangeRequest_ServiceFailure_ReturnsBadRequest()
        {
            // Arrange
            var request = new ExchangeRequestDto
            {
                OfferProductId = 1,
                RequestProductId = 2,
                Terms = "商品状态良好，希望交换"
            };

            _mockExchangeService.Setup(x => x.CreateExchangeRequestAsync(It.IsAny<ExchangeRequestDto>(), It.IsAny<int>()))
                .ReturnsAsync((false, "提供的商品不存在或不属于您", null));

            // Act
            var result = await _controller.CreateExchangeRequest(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Message.Should().Be("提供的商品不存在或不属于您");
        }

        [Fact]
        public async Task CreateExchangeRequest_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(); // No claims
            var request = new ExchangeRequestDto
            {
                OfferProductId = 1,
                RequestProductId = 2,
                Terms = "商品状态良好，希望交换"
            };

            // Act
            var result = await _controller.CreateExchangeRequest(request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorizedResult = result as UnauthorizedObjectResult;
            var apiResponse = unauthorizedResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Message.Should().Be("认证失败");
        }

        #endregion

        #region HandleExchangeResponse Tests

        [Fact]
        public async Task HandleExchangeResponse_ValidResponse_ReturnsOkResult()
        {
            // Arrange
            var response = new ExchangeResponseDto
            {
                ExchangeRequestId = 1,
                Status = "同意",
                ResponseMessage = "同意交换"
            };

            _mockExchangeService.Setup(x => x.HandleExchangeResponseAsync(It.IsAny<ExchangeResponseDto>(), It.IsAny<int>()))
                .ReturnsAsync((true, "回应已提交"));

            // Act
            var result = await _controller.HandleExchangeResponse(response);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Message.Should().Be("回应已提交");
        }

        [Fact]
        public async Task HandleExchangeResponse_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var response = new ExchangeResponseDto(); // Invalid response
            _controller.ModelState.AddModelError("ExchangeRequestId", "换物请求ID不能为空");

            // Act
            var result = await _controller.HandleExchangeResponse(response);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task HandleExchangeResponse_ServiceFailure_ReturnsBadRequest()
        {
            // Arrange
            var response = new ExchangeResponseDto
            {
                ExchangeRequestId = 1,
                Status = "同意"
            };

            _mockExchangeService.Setup(x => x.HandleExchangeResponseAsync(It.IsAny<ExchangeResponseDto>(), It.IsAny<int>()))
                .ReturnsAsync((false, "无权限操作此换物请求"));

            // Act
            var result = await _controller.HandleExchangeResponse(response);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Message.Should().Be("无权限操作此换物请求");
        }

        #endregion

        #region GetMyExchangeRequests Tests

        [Fact]
        public async Task GetMyExchangeRequests_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var exchangeRequests = new List<ExchangeRequestInfoDto>
            {
                new()
                {
                    ExchangeRequestId = 1,
                    OfferProductId = 1,
                    RequestProductId = 2,
                    Terms = "测试条件",
                    Status = "待回应",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockExchangeService.Setup(x => x.GetUserExchangeRequestsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((exchangeRequests, 1));

            // Act
            var result = await _controller.GetMyExchangeRequests();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetMyExchangeRequests_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var exchangeRequests = new List<ExchangeRequestInfoDto>();
            _mockExchangeService.Setup(x => x.GetUserExchangeRequestsAsync(It.IsAny<int>(), 2, 5))
                .ReturnsAsync((exchangeRequests, 10));

            // Act
            var result = await _controller.GetMyExchangeRequests(2, 5);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mockExchangeService.Verify(x => x.GetUserExchangeRequestsAsync(1, 2, 5), Times.Once);
        }

        #endregion

        #region GetExchangeRequestDetails Tests

        [Fact]
        public async Task GetExchangeRequestDetails_ValidId_ReturnsOkResult()
        {
            // Arrange
            var exchangeRequest = new ExchangeRequestInfoDto
            {
                ExchangeRequestId = 1,
                OfferProductId = 1,
                RequestProductId = 2,
                Terms = "测试条件",
                Status = "待回应",
                CreatedAt = DateTime.UtcNow
            };

            _mockExchangeService.Setup(x => x.GetExchangeRequestDetailsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(exchangeRequest);

            // Act
            var result = await _controller.GetExchangeRequestDetails(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().Be(exchangeRequest);
        }

        [Fact]
        public async Task GetExchangeRequestDetails_NotFound_ReturnsNotFound()
        {
            // Arrange
            _mockExchangeService.Setup(x => x.GetExchangeRequestDetailsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((ExchangeRequestInfoDto?)null);

            // Act
            var result = await _controller.GetExchangeRequestDetails(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            var apiResponse = notFoundResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Message.Should().Be("换物请求不存在或无权限访问");
        }

        #endregion
    }
}
