using CampusTrade.API.Data;
using CampusTrade.API.Models.DTOs.Review;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Services.Review;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services
{
    /// <summary>
    /// 评价服务单元测试
    /// </summary>
    public class ReviewServiceTests : IDisposable
    {
        private readonly CampusTradeDbContext _context;
        private readonly ReviewService _reviewService;

        public ReviewServiceTests()
        {
            // 创建内存数据库
            var options = new DbContextOptionsBuilder<CampusTradeDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new CampusTradeDbContext(options);
            _reviewService = new ReviewService(_context);

            // 初始化测试数据
            SeedTestData();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SeedTestData()
        {
            // 创建测试用户
            var buyer = new User
            {
                UserId = 1,
                Username = "buyer_user",
                Email = "buyer@test.com",
                IsActive = 1
            };

            var seller = new User
            {
                UserId = 2,
                Username = "seller_user",
                Email = "seller@test.com",
                IsActive = 1
            };

            // 创建测试商品
            var product = new Product
            {
                ProductId = 1,
                Title = "Test Product",
                Description = "Test Description",
                BasePrice = 100.00m,
                UserId = 2,
                Status = "在售",
                PublishTime = DateTime.Now
            };

            // 创建测试订单
            var completedOrder = new Order
            {
                OrderId = 1,
                ProductId = 1,
                BuyerId = 1,
                SellerId = 2,
                Status = "已完成",
                TotalAmount = 100.00m,
                CreateTime = DateTime.Now
            };

            var pendingOrder = new Order
            {
                OrderId = 2,
                ProductId = 1,
                BuyerId = 1,
                SellerId = 2,
                Status = "待发货",
                TotalAmount = 100.00m,
                CreateTime = DateTime.Now
            };

            _context.Users.AddRange(buyer, seller);
            _context.Products.Add(product);
            _context.Orders.AddRange(completedOrder, pendingOrder);
            _context.SaveChanges();
        }

        #region CreateReviewAsync Tests

        [Fact]
        public async Task CreateReviewAsync_ValidReview_ReturnsTrue()
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

            // Act
            var result = await _reviewService.CreateReviewAsync(createReviewDto, 1);

            // Assert
            Assert.True(result);

            // 验证评论是否已保存到数据库
            var savedReview = await _context.Reviews.FirstOrDefaultAsync(r => r.OrderId == 1);
            Assert.NotNull(savedReview);
            Assert.Equal(4.5m, savedReview.Rating);
            Assert.Equal("Great product!", savedReview.Content);
            Assert.Equal(0, savedReview.IsAnonymous); // false -> 0
        }

        [Fact]
        public async Task CreateReviewAsync_OrderNotFound_ThrowsException()
        {
            // Arrange
            var createReviewDto = new CreateReviewDto
            {
                OrderId = 999, // 不存在的订单ID
                Rating = 4.5m,
                DescAccuracy = 4,
                ServiceAttitude = 5,
                Content = "Test review"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _reviewService.CreateReviewAsync(createReviewDto, 1));
            Assert.Equal("订单不存在", exception.Message);
        }

        [Fact]
        public async Task CreateReviewAsync_NotBuyer_ThrowsUnauthorizedAccessException()
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

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _reviewService.CreateReviewAsync(createReviewDto, 2)); // 卖家尝试评论
            Assert.Equal("只有订单买家才能评论", exception.Message);
        }

        [Fact]
        public async Task CreateReviewAsync_OrderNotCompleted_ThrowsInvalidOperationException()
        {
            // Arrange
            var createReviewDto = new CreateReviewDto
            {
                OrderId = 2, // 状态为"待发货"的订单
                Rating = 4.5m,
                DescAccuracy = 4,
                ServiceAttitude = 5,
                Content = "Test review"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _reviewService.CreateReviewAsync(createReviewDto, 1));
            Assert.Equal("订单未完成，无法评论", exception.Message);
        }

        [Fact]
        public async Task CreateReviewAsync_AlreadyReviewed_ThrowsInvalidOperationException()
        {
            // Arrange
            // 先创建一个评论
            var existingReview = new API.Models.Entities.Review
            {
                OrderId = 1,
                Rating = 3.0m,
                DescAccuracy = 3,
                ServiceAttitude = 3,
                IsAnonymous = 0,
                Content = "Already reviewed",
                CreateTime = DateTime.Now
            };
            _context.Reviews.Add(existingReview);
            await _context.SaveChangesAsync();

            var createReviewDto = new CreateReviewDto
            {
                OrderId = 1,
                Rating = 4.5m,
                DescAccuracy = 4,
                ServiceAttitude = 5,
                Content = "Second review attempt"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _reviewService.CreateReviewAsync(createReviewDto, 1));
            Assert.Equal("该订单已提交评论，无法重复评论", exception.Message);
        }

        [Fact]
        public async Task CreateReviewAsync_AnonymousReview_SavesCorrectly()
        {
            // Arrange
            var createReviewDto = new CreateReviewDto
            {
                OrderId = 1,
                Rating = 4.0m,
                DescAccuracy = 4,
                ServiceAttitude = 4,
                IsAnonymous = true, // 匿名评论
                Content = "Anonymous review"
            };

            // Act
            var result = await _reviewService.CreateReviewAsync(createReviewDto, 1);

            // Assert
            Assert.True(result);

            var savedReview = await _context.Reviews.FirstOrDefaultAsync(r => r.OrderId == 1);
            Assert.NotNull(savedReview);
            Assert.Equal(1, savedReview.IsAnonymous); // true -> 1
        }

        #endregion

        #region GetReviewsByItemIdAsync Tests

        [Fact]
        public async Task GetReviewsByItemIdAsync_ProductWithReviews_ReturnsReviews()
        {
            // Arrange
            // 创建评论
            var review = new API.Models.Entities.Review
            {
                OrderId = 1,
                Rating = 4.5m,
                DescAccuracy = 4,
                ServiceAttitude = 5,
                IsAnonymous = 0,
                Content = "Great product!",
                CreateTime = DateTime.Now
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Act
            var result = await _reviewService.GetReviewsByItemIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            var reviewDto = result.First();
            Assert.Equal(4.5m, reviewDto.Rating);
            Assert.Equal("Great product!", reviewDto.Content);
            Assert.Equal("buyer_user", reviewDto.ReviewerDisplayName); // 非匿名，显示用户名
        }

        [Fact]
        public async Task GetReviewsByItemIdAsync_AnonymousReview_HidesUsername()
        {
            // Arrange
            var review = new API.Models.Entities.Review
            {
                OrderId = 1,
                Rating = 4.0m,
                DescAccuracy = 4,
                ServiceAttitude = 4,
                IsAnonymous = 1, // 匿名评论
                Content = "Anonymous review",
                CreateTime = DateTime.Now
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Act
            var result = await _reviewService.GetReviewsByItemIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            var reviewDto = result.First();
            Assert.Equal("匿名用户", reviewDto.ReviewerDisplayName);
        }

        [Fact]
        public async Task GetReviewsByItemIdAsync_ProductWithNoOrders_ReturnsEmpty()
        {
            // Act
            var result = await _reviewService.GetReviewsByItemIdAsync(999); // 不存在的商品ID

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetReviewsByItemIdAsync_ProductWithNoReviews_ReturnsEmpty()
        {
            // Act
            var result = await _reviewService.GetReviewsByItemIdAsync(1); // 存在订单但无评论

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetReviewByOrderIdAsync Tests

        [Fact]
        public async Task GetReviewByOrderIdAsync_ExistingReview_ReturnsReview()
        {
            // Arrange
            var review = new API.Models.Entities.Review
            {
                OrderId = 1,
                Rating = 4.5m,
                DescAccuracy = 4,
                ServiceAttitude = 5,
                IsAnonymous = 0,
                Content = "Great product!",
                CreateTime = DateTime.Now
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Act
            var result = await _reviewService.GetReviewByOrderIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4.5m, result.Rating);
            Assert.Equal("Great product!", result.Content);
            Assert.Equal("buyer_user", result.ReviewerDisplayName);
        }

        [Fact]
        public async Task GetReviewByOrderIdAsync_NoReview_ReturnsNull()
        {
            // Act
            var result = await _reviewService.GetReviewByOrderIdAsync(1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetReviewByOrderIdAsync_AnonymousReview_HidesUsername()
        {
            // Arrange
            var review = new API.Models.Entities.Review
            {
                OrderId = 1,
                Rating = 3.0m,
                DescAccuracy = 3,
                ServiceAttitude = 3,
                IsAnonymous = 1,
                Content = "Anonymous review",
                CreateTime = DateTime.Now
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Act
            var result = await _reviewService.GetReviewByOrderIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("匿名用户", result.ReviewerDisplayName);
        }

        #endregion

        #region ReplyToReviewAsync Tests

        [Fact]
        public async Task ReplyToReviewAsync_ValidReply_ReturnsTrue()
        {
            // Arrange
            var review = new API.Models.Entities.Review
            {
                OrderId = 1,
                Rating = 4.0m,
                DescAccuracy = 4,
                ServiceAttitude = 4,
                IsAnonymous = 0,
                Content = "Good product",
                CreateTime = DateTime.Now
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            var replyDto = new ReplyReviewDto
            {
                ReviewId = review.ReviewId,
                SellerReply = "Thank you for your feedback!"
            };

            // Act
            var result = await _reviewService.ReplyToReviewAsync(replyDto, 2); // 卖家ID为2

            // Assert
            Assert.True(result);

            // 验证回复是否已保存
            var updatedReview = await _context.Reviews.FindAsync(review.ReviewId);
            Assert.NotNull(updatedReview);
            Assert.Equal("Thank you for your feedback!", updatedReview.SellerReply);
        }

        [Fact]
        public async Task ReplyToReviewAsync_ReviewNotFound_ThrowsException()
        {
            // Arrange
            var replyDto = new ReplyReviewDto
            {
                ReviewId = 999, // 不存在的评论ID
                SellerReply = "Test reply"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _reviewService.ReplyToReviewAsync(replyDto, 2));
            Assert.Equal("评论不存在", exception.Message);
        }

        [Fact]
        public async Task ReplyToReviewAsync_NotSeller_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var review = new API.Models.Entities.Review
            {
                OrderId = 1,
                Rating = 4.0m,
                DescAccuracy = 4,
                ServiceAttitude = 4,
                IsAnonymous = 0,
                Content = "Good product",
                CreateTime = DateTime.Now
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            var replyDto = new ReplyReviewDto
            {
                ReviewId = review.ReviewId,
                SellerReply = "Unauthorized reply"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _reviewService.ReplyToReviewAsync(replyDto, 1)); // 买家尝试回复
            Assert.Equal("无权限回复该评论", exception.Message);
        }

        [Fact]
        public async Task ReplyToReviewAsync_Expired48Hours_ThrowsInvalidOperationException()
        {
            // Arrange
            var review = new API.Models.Entities.Review
            {
                OrderId = 1,
                Rating = 4.0m,
                DescAccuracy = 4,
                ServiceAttitude = 4,
                IsAnonymous = 0,
                Content = "Good product",
                CreateTime = DateTime.Now.AddHours(-49) // 49小时前创建，超出48小时限制
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            var replyDto = new ReplyReviewDto
            {
                ReviewId = review.ReviewId,
                SellerReply = "Late reply"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _reviewService.ReplyToReviewAsync(replyDto, 2));
            Assert.Equal("回复已超出48小时限制，无法修改", exception.Message);
        }

        #endregion

        #region DeleteReviewAsync Tests

        [Fact]
        public async Task DeleteReviewAsync_ValidDelete_ReturnsTrue()
        {
            // Arrange
            var review = new API.Models.Entities.Review
            {
                OrderId = 1,
                Rating = 4.0m,
                DescAccuracy = 4,
                ServiceAttitude = 4,
                IsAnonymous = 0,
                Content = "Review to delete",
                CreateTime = DateTime.Now
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Act
            var result = await _reviewService.DeleteReviewAsync(review.ReviewId, 1); // 买家删除自己的评论

            // Assert
            Assert.True(result);

            // 验证评论是否已删除
            var deletedReview = await _context.Reviews.FindAsync(review.ReviewId);
            Assert.Null(deletedReview);
        }

        [Fact]
        public async Task DeleteReviewAsync_ReviewNotFound_ThrowsException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _reviewService.DeleteReviewAsync(999, 1));
            Assert.Equal("评论不存在", exception.Message);
        }

        [Fact]
        public async Task DeleteReviewAsync_NotOwner_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var review = new API.Models.Entities.Review
            {
                OrderId = 1,
                Rating = 4.0m,
                DescAccuracy = 4,
                ServiceAttitude = 4,
                IsAnonymous = 0,
                Content = "Someone else's review",
                CreateTime = DateTime.Now
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _reviewService.DeleteReviewAsync(review.ReviewId, 2)); // 卖家尝试删除买家的评论
            Assert.Equal("只能删除自己写的评论", exception.Message);
        }

        #endregion
    }
}
