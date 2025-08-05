using CampusTrade.API.Controllers;
using CampusTrade.API.Models.DTOs.Bargain;
using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Controllers
{
    /// <summary>
    /// 议价控制器单元测试
    /// </summary>
    public class BargainControllerTests
    {
        private readonly Mock<IBargainService> _mockBargainService;
        private readonly Mock<ILogger<BargainController>> _mockLogger;
        private readonly BargainController _controller;
        private readonly ClaimsPrincipal _testUser;

        public BargainControllerTests()
        {
            _mockBargainService = new Mock<IBargainService>();
            _mockLogger = new Mock<ILogger<BargainController>>();
            _controller = new BargainController(_mockBargainService.Object, _mockLogger.Object);

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

        #region CreateBargainRequest Tests

        [Fact]
        public async Task CreateBargainRequest_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new BargainRequestDto
            {
                OrderId = 1,
                ProposedPrice = 100.00m
            };

            _mockBargainService.Setup(x => x.CreateBargainRequestAsync(It.IsAny<BargainRequestDto>(), It.IsAny<int>()))
                .ReturnsAsync((true, "议价请求已发送", 1));

            // Act
            var result = await _controller.CreateBargainRequest(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Message.Should().Be("议价请求已发送");
        }

        [Fact]
        public async Task CreateBargainRequest_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var request = new BargainRequestDto(); // Invalid request
            _controller.ModelState.AddModelError("OrderId", "订单ID不能为空");

            // Act
            var result = await _controller.CreateBargainRequest(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Message.Should().Contain("请求参数无效");
        }

        [Fact]
        public async Task CreateBargainRequest_ServiceFailure_ReturnsBadRequest()
        {
            // Arrange
            var request = new BargainRequestDto
            {
                OrderId = 1,
                ProposedPrice = 100.00m
            };

            _mockBargainService.Setup(x => x.CreateBargainRequestAsync(It.IsAny<BargainRequestDto>(), It.IsAny<int>()))
                .ReturnsAsync((false, "订单不存在", null));

            // Act
            var result = await _controller.CreateBargainRequest(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Message.Should().Be("订单不存在");
        }

        [Fact]
        public async Task CreateBargainRequest_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(); // No claims
            var request = new BargainRequestDto
            {
                OrderId = 1,
                ProposedPrice = 100.00m
            };

            // Act
            var result = await _controller.CreateBargainRequest(request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorizedResult = result as UnauthorizedObjectResult;
            var apiResponse = unauthorizedResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Message.Should().Be("认证失败");
        }

        #endregion

        #region HandleBargainResponse Tests

        [Fact]
        public async Task HandleBargainResponse_ValidResponse_ReturnsOkResult()
        {
            // Arrange
            var response = new BargainResponseDto
            {
                NegotiationId = 1,
                Status = "接受"
            };

            _mockBargainService.Setup(x => x.HandleBargainResponseAsync(It.IsAny<BargainResponseDto>(), It.IsAny<int>()))
                .ReturnsAsync((true, "回应已提交"));

            // Act
            var result = await _controller.HandleBargainResponse(response);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse;
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Message.Should().Be("回应已提交");
        }

        [Fact]
        public async Task HandleBargainResponse_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var response = new BargainResponseDto(); // Invalid response
            _controller.ModelState.AddModelError("NegotiationId", "议价记录ID不能为空");

            // Act
            var result = await _controller.HandleBargainResponse(response);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task HandleBargainResponse_ServiceFailure_ReturnsBadRequest()
        {
            // Arrange
            var response = new BargainResponseDto
            {
                NegotiationId = 1,
                Status = "接受"
            };

            _mockBargainService.Setup(x => x.HandleBargainResponseAsync(It.IsAny<BargainResponseDto>(), It.IsAny<int>()))
                .ReturnsAsync((false, "无权限操作此议价"));

            // Act
            var result = await _controller.HandleBargainResponse(response);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var apiResponse = badRequestResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Message.Should().Be("无权限操作此议价");
        }

        #endregion

        #region GetMyNegotiations Tests

        [Fact]
        public async Task GetMyNegotiations_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var negotiations = new List<NegotiationDto>
            {
                new() { NegotiationId = 1, OrderId = 1, ProposedPrice = 100.00m, Status = "等待回应", CreatedAt = DateTime.UtcNow }
            };

            _mockBargainService.Setup(x => x.GetUserNegotiationsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((negotiations, 1));

            // Act
            var result = await _controller.GetMyNegotiations();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetMyNegotiations_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var negotiations = new List<NegotiationDto>();
            _mockBargainService.Setup(x => x.GetUserNegotiationsAsync(It.IsAny<int>(), 2, 5))
                .ReturnsAsync((negotiations, 10));

            // Act
            var result = await _controller.GetMyNegotiations(2, 5);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mockBargainService.Verify(x => x.GetUserNegotiationsAsync(1, 2, 5), Times.Once);
        }

        #endregion

        #region GetNegotiationDetails Tests

        [Fact]
        public async Task GetNegotiationDetails_ValidId_ReturnsOkResult()
        {
            // Arrange
            var negotiation = new NegotiationDto
            {
                NegotiationId = 1,
                OrderId = 1,
                ProposedPrice = 100.00m,
                Status = "等待回应",
                CreatedAt = DateTime.UtcNow
            };

            _mockBargainService.Setup(x => x.GetNegotiationDetailsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(negotiation);

            // Act
            var result = await _controller.GetNegotiationDetails(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().Be(negotiation);
        }

        [Fact]
        public async Task GetNegotiationDetails_NotFound_ReturnsNotFound()
        {
            // Arrange
            _mockBargainService.Setup(x => x.GetNegotiationDetailsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((NegotiationDto?)null);

            // Act
            var result = await _controller.GetNegotiationDetails(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            var apiResponse = notFoundResult!.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Message.Should().Be("议价记录不存在或无权限访问");
        }

        #endregion
    }
}