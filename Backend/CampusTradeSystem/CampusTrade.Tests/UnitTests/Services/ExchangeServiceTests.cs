using CampusTrade.API.Models.DTOs.Exchange;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Exchange;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services
{
    /// <summary>
    /// 换物服务单元测试
    /// </summary>
    public class ExchangeServiceTests
    {
        private readonly Mock<IExchangeRequestsRepository> _mockExchangeRequestsRepository;
        private readonly Mock<IRepository<Product>> _mockProductsRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ExchangeService>> _mockLogger;
        private readonly ExchangeService _service;

        public ExchangeServiceTests()
        {
            _mockExchangeRequestsRepository = new Mock<IExchangeRequestsRepository>();
            _mockProductsRepository = new Mock<IRepository<Product>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ExchangeService>>();

            _service = new ExchangeService(
                _mockExchangeRequestsRepository.Object,
                _mockProductsRepository.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object);
        }

        #region CreateExchangeRequestAsync Tests

        [Fact]
        public async Task CreateExchangeRequestAsync_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new ExchangeRequestDto
            {
                OfferProductId = 1,
                RequestProductId = 2,
                Terms = "商品状态良好，希望交换"
            };
            var userId = 1;

            var offerProduct = new Product
            {
                ProductId = 1,
                UserId = 1, // Same as userId
                Status = Product.ProductStatus.OnSale
            };

            var requestProduct = new Product
            {
                ProductId = 2,
                UserId = 2, // Different from userId
                Status = Product.ProductStatus.OnSale
            };

            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(offerProduct);
            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(2))
                .ReturnsAsync(requestProduct);
            _mockExchangeRequestsRepository.Setup(x => x.HasPendingExchangeAsync(1))
                .ReturnsAsync(false);
            _mockExchangeRequestsRepository.Setup(x => x.AddAsync(It.IsAny<ExchangeRequest>()))
                .ReturnsAsync((ExchangeRequest exchangeRequest) =>
                {
                    exchangeRequest.ExchangeId = 1;
                    return exchangeRequest;
                });

            // Act
            var result = await _service.CreateExchangeRequestAsync(request, userId);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("换物请求已发送");
            result.ExchangeRequestId.Should().Be(1);

            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _mockExchangeRequestsRepository.Verify(x => x.AddAsync(It.IsAny<ExchangeRequest>()), Times.Once);
        }

        [Fact]
        public async Task CreateExchangeRequestAsync_OfferProductNotFound_ReturnsFailure()
        {
            // Arrange
            var request = new ExchangeRequestDto
            {
                OfferProductId = 999,
                RequestProductId = 2,
                Terms = "商品状态良好，希望交换"
            };
            var userId = 1;

            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(999))
                .ReturnsAsync((Product?)null);

            // Act
            var result = await _service.CreateExchangeRequestAsync(request, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("提供的商品不存在或不属于您");
            result.ExchangeRequestId.Should().BeNull();
        }

        [Fact]
        public async Task CreateExchangeRequestAsync_OfferProductNotOwnedByUser_ReturnsFailure()
        {
            // Arrange
            var request = new ExchangeRequestDto
            {
                OfferProductId = 1,
                RequestProductId = 2,
                Terms = "商品状态良好，希望交换"
            };
            var userId = 1;

            var offerProduct = new Product
            {
                ProductId = 1,
                UserId = 999, // Different from userId
                Status = Product.ProductStatus.OnSale
            };

            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(offerProduct);

            // Act
            var result = await _service.CreateExchangeRequestAsync(request, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("提供的商品不存在或不属于您");
        }

        [Fact]
        public async Task CreateExchangeRequestAsync_OfferProductNotOnSale_ReturnsFailure()
        {
            // Arrange
            var request = new ExchangeRequestDto
            {
                OfferProductId = 1,
                RequestProductId = 2,
                Terms = "商品状态良好，希望交换"
            };
            var userId = 1;

            var offerProduct = new Product
            {
                ProductId = 1,
                UserId = 1,
                Status = Product.ProductStatus.OffShelf // Not on sale
            };

            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(offerProduct);

            // Act
            var result = await _service.CreateExchangeRequestAsync(request, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("提供的商品状态不允许交换");
        }

        [Fact]
        public async Task CreateExchangeRequestAsync_RequestProductNotFound_ReturnsFailure()
        {
            // Arrange
            var request = new ExchangeRequestDto
            {
                OfferProductId = 1,
                RequestProductId = 999,
                Terms = "商品状态良好，希望交换"
            };
            var userId = 1;

            var offerProduct = new Product
            {
                ProductId = 1,
                UserId = 1,
                Status = Product.ProductStatus.OnSale
            };

            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(offerProduct);
            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(999))
                .ReturnsAsync((Product?)null);

            // Act
            var result = await _service.CreateExchangeRequestAsync(request, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("请求的商品不存在");
        }

        [Fact]
        public async Task CreateExchangeRequestAsync_RequestOwnProduct_ReturnsFailure()
        {
            // Arrange
            var request = new ExchangeRequestDto
            {
                OfferProductId = 1,
                RequestProductId = 2,
                Terms = "商品状态良好，希望交换"
            };
            var userId = 1;

            var offerProduct = new Product
            {
                ProductId = 1,
                UserId = 1,
                Status = Product.ProductStatus.OnSale
            };

            var requestProduct = new Product
            {
                ProductId = 2,
                UserId = 1, // Same as userId
                Status = Product.ProductStatus.OnSale
            };

            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(offerProduct);
            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(2))
                .ReturnsAsync(requestProduct);

            // Act
            var result = await _service.CreateExchangeRequestAsync(request, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("不能请求交换自己的商品");
        }

        [Fact]
        public async Task CreateExchangeRequestAsync_PendingExchangeExists_ReturnsFailure()
        {
            // Arrange
            var request = new ExchangeRequestDto
            {
                OfferProductId = 1,
                RequestProductId = 2,
                Terms = "商品状态良好，希望交换"
            };
            var userId = 1;

            var offerProduct = new Product
            {
                ProductId = 1,
                UserId = 1,
                Status = Product.ProductStatus.OnSale
            };

            var requestProduct = new Product
            {
                ProductId = 2,
                UserId = 2,
                Status = Product.ProductStatus.OnSale
            };

            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(offerProduct);
            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(2))
                .ReturnsAsync(requestProduct);
            _mockExchangeRequestsRepository.Setup(x => x.HasPendingExchangeAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _service.CreateExchangeRequestAsync(request, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("该商品已有待处理的换物请求");
        }

        #endregion

        #region HandleExchangeResponseAsync Tests

        [Fact]
        public async Task HandleExchangeResponseAsync_AcceptExchange_ReturnsSuccess()
        {
            // Arrange
            var response = new ExchangeResponseDto
            {
                ExchangeRequestId = 1,
                Status = "同意",
                ResponseMessage = "同意交换"
            };
            var userId = 2; // Owner of requested product

            var exchangeRequest = new ExchangeRequest
            {
                ExchangeId = 1,
                OfferProductId = 1,
                RequestProductId = 2,
                Status = "待回应"
            };

            var offerProduct = new Product
            {
                ProductId = 1,
                UserId = 1,
                Status = Product.ProductStatus.OnSale
            };

            var requestProduct = new Product
            {
                ProductId = 2,
                UserId = 2, // Same as userId
                Status = Product.ProductStatus.OnSale
            };

            _mockExchangeRequestsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(exchangeRequest);
            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(2))
                .ReturnsAsync(requestProduct);
            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(offerProduct);
            _mockExchangeRequestsRepository.Setup(x => x.UpdateExchangeStatusAsync(1, "同意"))
                .ReturnsAsync(true);

            // Act
            var result = await _service.HandleExchangeResponseAsync(response, userId);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("回应已提交");

            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _mockExchangeRequestsRepository.Verify(x => x.UpdateExchangeStatusAsync(1, "同意"), Times.Once);
            _mockProductsRepository.Verify(x => x.Update(It.IsAny<Product>()), Times.Exactly(2));

            // Verify products status were updated
            offerProduct.Status.Should().Be(Product.ProductStatus.InTransaction);
            requestProduct.Status.Should().Be(Product.ProductStatus.InTransaction);
        }

        [Fact]
        public async Task HandleExchangeResponseAsync_RejectExchange_ReturnsSuccess()
        {
            // Arrange
            var response = new ExchangeResponseDto
            {
                ExchangeRequestId = 1,
                Status = "拒绝",
                ResponseMessage = "商品不符合要求"
            };
            var userId = 2;

            var exchangeRequest = new ExchangeRequest
            {
                ExchangeId = 1,
                RequestProductId = 2,
                Status = "待回应"
            };

            var requestProduct = new Product
            {
                ProductId = 2,
                UserId = 2,
                Status = Product.ProductStatus.OnSale
            };

            _mockExchangeRequestsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(exchangeRequest);
            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(2))
                .ReturnsAsync(requestProduct);
            _mockExchangeRequestsRepository.Setup(x => x.UpdateExchangeStatusAsync(1, "拒绝"))
                .ReturnsAsync(true);

            // Act
            var result = await _service.HandleExchangeResponseAsync(response, userId);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("回应已提交");

            // Product status should not change for rejection
            _mockProductsRepository.Verify(x => x.Update(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task HandleExchangeResponseAsync_ExchangeRequestNotFound_ReturnsFailure()
        {
            // Arrange
            var response = new ExchangeResponseDto
            {
                ExchangeRequestId = 999,
                Status = "同意"
            };
            var userId = 2;

            _mockExchangeRequestsRepository.Setup(x => x.GetByPrimaryKeyAsync(999))
                .ReturnsAsync((ExchangeRequest?)null);

            // Act
            var result = await _service.HandleExchangeResponseAsync(response, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("换物请求不存在");
        }

        [Fact]
        public async Task HandleExchangeResponseAsync_UnauthorizedUser_ReturnsFailure()
        {
            // Arrange
            var response = new ExchangeResponseDto
            {
                ExchangeRequestId = 1,
                Status = "同意"
            };
            var userId = 999; // Not the owner

            var exchangeRequest = new ExchangeRequest
            {
                ExchangeId = 1,
                RequestProductId = 2,
                Status = "待回应"
            };

            var requestProduct = new Product
            {
                ProductId = 2,
                UserId = 2 // Different from userId
            };

            _mockExchangeRequestsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(exchangeRequest);
            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(2))
                .ReturnsAsync(requestProduct);

            // Act
            var result = await _service.HandleExchangeResponseAsync(response, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("无权限操作此换物请求");
        }

        [Fact]
        public async Task HandleExchangeResponseAsync_InvalidStatus_ReturnsFailure()
        {
            // Arrange
            var response = new ExchangeResponseDto
            {
                ExchangeRequestId = 1,
                Status = "同意"
            };
            var userId = 2;

            var exchangeRequest = new ExchangeRequest
            {
                ExchangeId = 1,
                RequestProductId = 2,
                Status = "已同意" // Not "待回应"
            };

            var requestProduct = new Product
            {
                ProductId = 2,
                UserId = 2
            };

            _mockExchangeRequestsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(exchangeRequest);
            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(2))
                .ReturnsAsync(requestProduct);

            // Act
            var result = await _service.HandleExchangeResponseAsync(response, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("换物请求状态不允许回应");
        }

        #endregion

        #region GetUserExchangeRequestsAsync Tests

        [Fact]
        public async Task GetUserExchangeRequestsAsync_ValidUser_ReturnsExchangeRequests()
        {
            // Arrange
            var userId = 1;
            var exchangeRequests = new List<ExchangeRequest>
            {
                new() { ExchangeId = 1, OfferProductId = 1, RequestProductId = 2, Terms = "条件1", Status = "待回应", CreatedAt = DateTime.UtcNow },
                new() { ExchangeId = 2, OfferProductId = 3, RequestProductId = 4, Terms = "条件2", Status = "同意", CreatedAt = DateTime.UtcNow }
            };

            _mockExchangeRequestsRepository.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(exchangeRequests);

            // Act
            var result = await _service.GetUserExchangeRequestsAsync(userId, 1, 10);

            // Assert
            result.ExchangeRequests.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.ExchangeRequests.First().ExchangeRequestId.Should().Be(1);
        }

        [Fact]
        public async Task GetUserExchangeRequestsAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var userId = 1;
            var exchangeRequests = new List<ExchangeRequest>();
            for (int i = 1; i <= 15; i++)
            {
                exchangeRequests.Add(new ExchangeRequest
                {
                    ExchangeId = i,
                    OfferProductId = i,
                    RequestProductId = i + 100,
                    Terms = $"条件{i}",
                    Status = "待回应",
                    CreatedAt = DateTime.UtcNow
                });
            }

            _mockExchangeRequestsRepository.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(exchangeRequests);

            // Act
            var result = await _service.GetUserExchangeRequestsAsync(userId, 2, 5);

            // Assert
            result.ExchangeRequests.Should().HaveCount(5);
            result.TotalCount.Should().Be(15);
            result.ExchangeRequests.First().ExchangeRequestId.Should().Be(6); // Second page, first item
        }

        #endregion

        #region GetExchangeRequestDetailsAsync Tests

        [Fact]
        public async Task GetExchangeRequestDetailsAsync_ValidRequest_ReturnsDetails()
        {
            // Arrange
            var exchangeRequestId = 1;
            var userId = 1; // Owner of offer product

            var exchangeRequest = new ExchangeRequest
            {
                ExchangeId = 1,
                OfferProductId = 1,
                RequestProductId = 2,
                Terms = "测试条件",
                Status = "待回应",
                CreatedAt = DateTime.UtcNow
            };

            var offerProduct = new Product
            {
                ProductId = 1,
                UserId = 1 // Same as userId
            };

            var requestProduct = new Product
            {
                ProductId = 2,
                UserId = 2
            };

            _mockExchangeRequestsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(exchangeRequest);
            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(offerProduct);
            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(2))
                .ReturnsAsync(requestProduct);

            // Act
            var result = await _service.GetExchangeRequestDetailsAsync(exchangeRequestId, userId);

            // Assert
            result.Should().NotBeNull();
            result!.ExchangeRequestId.Should().Be(1);
            result.OfferProductId.Should().Be(1);
            result.RequestProductId.Should().Be(2);
            result.Terms.Should().Be("测试条件");
            result.Status.Should().Be("待回应");
        }

        [Fact]
        public async Task GetExchangeRequestDetailsAsync_ExchangeRequestNotFound_ReturnsNull()
        {
            // Arrange
            var exchangeRequestId = 999;
            var userId = 1;

            _mockExchangeRequestsRepository.Setup(x => x.GetByPrimaryKeyAsync(999))
                .ReturnsAsync((ExchangeRequest?)null);

            // Act
            var result = await _service.GetExchangeRequestDetailsAsync(exchangeRequestId, userId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetExchangeRequestDetailsAsync_UnauthorizedUser_ReturnsNull()
        {
            // Arrange
            var exchangeRequestId = 1;
            var userId = 999; // Not owner of either product

            var exchangeRequest = new ExchangeRequest
            {
                ExchangeId = 1,
                OfferProductId = 1,
                RequestProductId = 2
            };

            var offerProduct = new Product
            {
                ProductId = 1,
                UserId = 1 // Different from userId
            };

            var requestProduct = new Product
            {
                ProductId = 2,
                UserId = 2 // Different from userId
            };

            _mockExchangeRequestsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(exchangeRequest);
            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(offerProduct);
            _mockProductsRepository.Setup(x => x.GetByPrimaryKeyAsync(2))
                .ReturnsAsync(requestProduct);

            // Act
            var result = await _service.GetExchangeRequestDetailsAsync(exchangeRequestId, userId);

            // Assert
            result.Should().BeNull();
        }

        #endregion
    }
}
