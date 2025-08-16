using System.Security.Claims;
using CampusTrade.API.Controllers;
using CampusTrade.API.Models.DTOs.Review;
using CampusTrade.API.Services.Review;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Controllers
{
    /// <summary>
    /// 评价控制器单元测试
    /// </summary>
    public class ReviewsControllerTests
    {
        private readonly Mock<IReviewService> _mockReviewService;
        private readonly ReviewsController _controller;

        public ReviewsControllerTests()
        {
            _mockReviewService = new Mock<IReviewService>();
            _controller = new ReviewsController(_mockReviewService.Object);

            // 设置用户身份
            SetupUserClaims(1);
        }

        private void SetupUserClaims(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("UserId", userId.ToString())
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

        #region CreateReview Tests

        [Fact]
        public async Task CreateReview_ValidRequest_ReturnsOk()
        {
            // Arrange
            var createReviewDto = new CreateReviewDto
            {
                OrderId = 1,
                Rating = 4.5m,
                DescAccuracy = 4,
                ServiceAttitude = 5,
                IsAnonymous = false,
                Content = "Great product!"
            };

            _mockReviewService
                .Setup(s => s.CreateReviewAsync(It.IsAny<CreateReviewDto>(), It.IsAny<int>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CreateReview(createReviewDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            // 验证服务方法被正确调用
            _mockReviewService.Verify(
                s => s.CreateReviewAsync(createReviewDto, 1),
                Times.Once);
        }

        [Fact]
        public async Task CreateReview_ServiceReturnsFalse_ReturnsBadRequest()
        {
            // Arrange
            var createReviewDto = new CreateReviewDto
            {
                OrderId = 1,
                Rating = 4.5m,
                DescAccuracy = 4,
                ServiceAttitude = 5,
                Content = "Test review"
            };

            _mockReviewService
                .Setup(s => s.CreateReviewAsync(It.IsAny<CreateReviewDto>(), It.IsAny<int>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.CreateReview(createReviewDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task CreateReview_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var createReviewDto = new CreateReviewDto
            {
                OrderId = 1,
                Rating = 4.5m,
                DescAccuracy = 4,
                ServiceAttitude = 5,
                Content = "Test review"
            };

            _mockReviewService
                .Setup(s => s.CreateReviewAsync(It.IsAny<CreateReviewDto>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("订单不存在"));

            // Act
            var result = await _controller.CreateReview(createReviewDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task CreateReview_UnauthorizedAccess_ReturnsForbid()
        {
            // Arrange
            var createReviewDto = new CreateReviewDto
            {
                OrderId = 1,
                Rating = 4.5m,
                DescAccuracy = 4,
                ServiceAttitude = 5,
                Content = "Test review"
            };

            _mockReviewService
                .Setup(s => s.CreateReviewAsync(It.IsAny<CreateReviewDto>(), It.IsAny<int>()))
                .ThrowsAsync(new UnauthorizedAccessException("只有订单买家才能评论"));

            // Act
            var result = await _controller.CreateReview(createReviewDto);

            // Assert
            var forbidResult = Assert.IsType<ForbidResult>(result);
        }

        #endregion

        #region GetReviewsByItemId Tests

        [Fact]
        public async Task GetReviewsByItemId_ValidItemId_ReturnsOk()
        {
            // Arrange
            var itemId = 1;
            var expectedReviews = new List<ReviewDto>
            {
                new ReviewDto
                {
                    ReviewId = 1,
                    OrderId = 1,
                    Rating = 4.5m,
                    DescAccuracy = 4,
                    ServiceAttitude = 5,
                    Content = "Great product!",
                    ReviewerDisplayName = "test_user",
                    CreateTime = DateTime.Now
                }
            };

            _mockReviewService
                .Setup(s => s.GetReviewsByItemIdAsync(itemId))
                .ReturnsAsync(expectedReviews);

            // Act
            var result = await _controller.GetReviewsByItemId(itemId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var reviews = Assert.IsType<List<ReviewDto>>(okResult.Value);
            Assert.Single(reviews);
            Assert.Equal(4.5m, reviews.First().Rating);
        }

        [Fact]
        public async Task GetReviewsByItemId_NoReviews_ReturnsEmptyList()
        {
            // Arrange
            var itemId = 1;
            var expectedReviews = new List<ReviewDto>();

            _mockReviewService
                .Setup(s => s.GetReviewsByItemIdAsync(itemId))
                .ReturnsAsync(expectedReviews);

            // Act
            var result = await _controller.GetReviewsByItemId(itemId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var reviews = Assert.IsType<List<ReviewDto>>(okResult.Value);
            Assert.Empty(reviews);
        }

        [Fact]
        public async Task GetReviewsByItemId_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var itemId = 1;

            _mockReviewService
                .Setup(s => s.GetReviewsByItemIdAsync(itemId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetReviewsByItemId(itemId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        #endregion

        #region GetReviewByOrderId Tests

        [Fact]
        public async Task GetReviewByOrderId_ExistingReview_ReturnsOk()
        {
            // Arrange
            var orderId = 1;
            var expectedReview = new ReviewDto
            {
                ReviewId = 1,
                OrderId = orderId,
                Rating = 4.0m,
                DescAccuracy = 4,
                ServiceAttitude = 4,
                Content = "Good product",
                ReviewerDisplayName = "test_user",
                CreateTime = DateTime.Now
            };

            _mockReviewService
                .Setup(s => s.GetReviewByOrderIdAsync(orderId))
                .ReturnsAsync(expectedReview);

            // Act
            var result = await _controller.GetReviewByOrderId(orderId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var review = Assert.IsType<ReviewDto>(okResult.Value);
            Assert.Equal(orderId, review.OrderId);
            Assert.Equal(4.0m, review.Rating);
        }

        [Fact]
        public async Task GetReviewByOrderId_NoReview_ReturnsNotFound()
        {
            // Arrange
            var orderId = 1;

            _mockReviewService
                .Setup(s => s.GetReviewByOrderIdAsync(orderId))
                .ReturnsAsync((ReviewDto?)null);

            // Act
            var result = await _controller.GetReviewByOrderId(orderId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task GetReviewByOrderId_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var orderId = 1;

            _mockReviewService
                .Setup(s => s.GetReviewByOrderIdAsync(orderId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetReviewByOrderId(orderId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        #endregion

        #region ReplyToReview Tests

        [Fact]
        public async Task ReplyToReview_ValidReply_ReturnsOk()
        {
            // Arrange
            SetupUserClaims(2); // 设置为卖家用户

            var replyDto = new ReplyReviewDto
            {
                ReviewId = 1,
                SellerReply = "Thank you for your feedback!"
            };

            _mockReviewService
                .Setup(s => s.ReplyToReviewAsync(It.IsAny<ReplyReviewDto>(), It.IsAny<int>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ReplyToReview(replyDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            _mockReviewService.Verify(
                s => s.ReplyToReviewAsync(replyDto, 2),
                Times.Once);
        }

        [Fact]
        public async Task ReplyToReview_ServiceReturnsFalse_ReturnsBadRequest()
        {
            // Arrange
            SetupUserClaims(2);

            var replyDto = new ReplyReviewDto
            {
                ReviewId = 1,
                SellerReply = "Test reply"
            };

            _mockReviewService
                .Setup(s => s.ReplyToReviewAsync(It.IsAny<ReplyReviewDto>(), It.IsAny<int>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ReplyToReview(replyDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task ReplyToReview_UnauthorizedAccess_ReturnsForbid()
        {
            // Arrange
            var replyDto = new ReplyReviewDto
            {
                ReviewId = 1,
                SellerReply = "Unauthorized reply"
            };

            _mockReviewService
                .Setup(s => s.ReplyToReviewAsync(It.IsAny<ReplyReviewDto>(), It.IsAny<int>()))
                .ThrowsAsync(new UnauthorizedAccessException("无权限回复该评论"));

            // Act
            var result = await _controller.ReplyToReview(replyDto);

            // Assert
            var forbidResult = Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task ReplyToReview_Expired_ReturnsBadRequest()
        {
            // Arrange
            var replyDto = new ReplyReviewDto
            {
                ReviewId = 1,
                SellerReply = "Late reply"
            };

            _mockReviewService
                .Setup(s => s.ReplyToReviewAsync(It.IsAny<ReplyReviewDto>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("回复已超出48小时限制，无法修改"));

            // Act
            var result = await _controller.ReplyToReview(replyDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        #endregion

        #region DeleteReview Tests

        [Fact]
        public async Task DeleteReview_ValidDelete_ReturnsOk()
        {
            // Arrange
            var reviewId = 1;

            _mockReviewService
                .Setup(s => s.DeleteReviewAsync(reviewId, It.IsAny<int>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteReview(reviewId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            _mockReviewService.Verify(
                s => s.DeleteReviewAsync(reviewId, 1),
                Times.Once);
        }

        [Fact]
        public async Task DeleteReview_ServiceReturnsFalse_ReturnsBadRequest()
        {
            // Arrange
            var reviewId = 1;

            _mockReviewService
                .Setup(s => s.DeleteReviewAsync(reviewId, It.IsAny<int>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteReview(reviewId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task DeleteReview_UnauthorizedAccess_ReturnsForbid()
        {
            // Arrange
            var reviewId = 1;

            _mockReviewService
                .Setup(s => s.DeleteReviewAsync(reviewId, It.IsAny<int>()))
                .ThrowsAsync(new UnauthorizedAccessException("只能删除自己写的评论"));

            // Act
            var result = await _controller.DeleteReview(reviewId);

            // Assert
            var forbidResult = Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteReview_ReviewNotFound_ReturnsBadRequest()
        {
            // Arrange
            var reviewId = 999;

            _mockReviewService
                .Setup(s => s.DeleteReviewAsync(reviewId, It.IsAny<int>()))
                .ThrowsAsync(new Exception("评论不存在"));

            // Act
            var result = await _controller.DeleteReview(reviewId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        #endregion
    }
}
