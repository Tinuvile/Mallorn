using CampusTrade.API.Models.DTOs.Bargain;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Bargain;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services
{
    /// <summary>
    /// 议价服务单元测试
    /// </summary>
    public class BargainServiceTests
    {
        private readonly Mock<INegotiationsRepository> _mockNegotiationsRepository;
        private readonly Mock<IRepository<Order>> _mockOrdersRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<BargainService>> _mockLogger;
        private readonly BargainService _service;

        public BargainServiceTests()
        {
            _mockNegotiationsRepository = new Mock<INegotiationsRepository>();
            _mockOrdersRepository = new Mock<IRepository<Order>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<BargainService>>();

            _service = new BargainService(
                _mockNegotiationsRepository.Object,
                _mockOrdersRepository.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object);
        }

        #region CreateBargainRequestAsync Tests

        [Fact]
        public async Task CreateBargainRequestAsync_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new BargainRequestDto
            {
                OrderId = 1,
                ProposedPrice = 100.00m
            };
            var userId = 2;

            var order = new Order
            {
                OrderId = 1,
                BuyerId = 3,
                SellerId = 4,
                Status = "待付款",
                TotalAmount = 150.00m
            };

            _mockOrdersRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(order);
            _mockNegotiationsRepository.Setup(x => x.HasActiveNegotiationAsync(1))
                .ReturnsAsync(false);
            _mockNegotiationsRepository.Setup(x => x.AddAsync(It.IsAny<Negotiation>()))
                .ReturnsAsync((Negotiation negotiation) =>
                {
                    negotiation.NegotiationId = 1;
                    return negotiation;
                });

            // Act
            var result = await _service.CreateBargainRequestAsync(request, userId);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("议价请求已发送");
            result.NegotiationId.Should().Be(1);

            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _mockNegotiationsRepository.Verify(x => x.AddAsync(It.IsAny<Negotiation>()), Times.Once);
        }

        [Fact]
        public async Task CreateBargainRequestAsync_OrderNotFound_ReturnsFailure()
        {
            // Arrange
            var request = new BargainRequestDto
            {
                OrderId = 999,
                ProposedPrice = 100.00m
            };
            var userId = 2;

            _mockOrdersRepository.Setup(x => x.GetByPrimaryKeyAsync(999))
                .ReturnsAsync((Order?)null);

            // Act
            var result = await _service.CreateBargainRequestAsync(request, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("订单不存在");
            result.NegotiationId.Should().BeNull();

            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateBargainRequestAsync_BuyerCannotBargainOwnOrder_ReturnsFailure()
        {
            // Arrange
            var request = new BargainRequestDto
            {
                OrderId = 1,
                ProposedPrice = 100.00m
            };
            var userId = 2;

            var order = new Order
            {
                OrderId = 1,
                BuyerId = 2, // Same as userId
                SellerId = 3,
                Status = "待付款"
            };

            _mockOrdersRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(order);

            // Act
            var result = await _service.CreateBargainRequestAsync(request, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("不能对自己的订单发起议价");
        }

        [Fact]
        public async Task CreateBargainRequestAsync_InvalidOrderStatus_ReturnsFailure()
        {
            // Arrange
            var request = new BargainRequestDto
            {
                OrderId = 1,
                ProposedPrice = 100.00m
            };
            var userId = 2;

            var order = new Order
            {
                OrderId = 1,
                BuyerId = 3,
                SellerId = 4,
                Status = "已完成" // Invalid status for bargaining
            };

            _mockOrdersRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(order);

            // Act
            var result = await _service.CreateBargainRequestAsync(request, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("订单状态不允许议价");
        }

        [Fact]
        public async Task CreateBargainRequestAsync_ActiveNegotiationExists_ReturnsFailure()
        {
            // Arrange
            var request = new BargainRequestDto
            {
                OrderId = 1,
                ProposedPrice = 100.00m
            };
            var userId = 2;

            var order = new Order
            {
                OrderId = 1,
                BuyerId = 3,
                SellerId = 4,
                Status = "待付款"
            };

            _mockOrdersRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(order);
            _mockNegotiationsRepository.Setup(x => x.HasActiveNegotiationAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _service.CreateBargainRequestAsync(request, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("已存在未完成的议价请求");
        }

        #endregion

        #region HandleBargainResponseAsync Tests

        [Fact]
        public async Task HandleBargainResponseAsync_AcceptBargain_ReturnsSuccess()
        {
            // Arrange
            var response = new BargainResponseDto
            {
                NegotiationId = 1,
                Status = "接受"
            };
            var userId = 4; // Seller ID

            var negotiation = new Negotiation
            {
                NegotiationId = 1,
                OrderId = 1,
                ProposedPrice = 100.00m,
                Status = "等待回应"
            };

            var order = new Order
            {
                OrderId = 1,
                BuyerId = 3,
                SellerId = 4,
                Status = "待付款",
                TotalAmount = 150.00m
            };

            _mockNegotiationsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(negotiation);
            _mockOrdersRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(order);
            _mockNegotiationsRepository.Setup(x => x.UpdateNegotiationStatusAsync(1, "接受"))
                .ReturnsAsync(true);

            // Act
            var result = await _service.HandleBargainResponseAsync(response, userId);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("回应已提交");

            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _mockNegotiationsRepository.Verify(x => x.UpdateNegotiationStatusAsync(1, "接受"), Times.Once);

            // Verify order amount was updated
            order.TotalAmount.Should().Be(100.00m);
        }

        [Fact]
        public async Task HandleBargainResponseAsync_NegotiationNotFound_ReturnsFailure()
        {
            // Arrange
            var response = new BargainResponseDto
            {
                NegotiationId = 999,
                Status = "接受"
            };
            var userId = 4;

            _mockNegotiationsRepository.Setup(x => x.GetByPrimaryKeyAsync(999))
                .ReturnsAsync((Negotiation?)null);

            // Act
            var result = await _service.HandleBargainResponseAsync(response, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("议价记录不存在");
        }

        [Fact]
        public async Task HandleBargainResponseAsync_UnauthorizedUser_ReturnsFailure()
        {
            // Arrange
            var response = new BargainResponseDto
            {
                NegotiationId = 1,
                Status = "接受"
            };
            var userId = 999; // Not the seller

            var negotiation = new Negotiation
            {
                NegotiationId = 1,
                OrderId = 1,
                Status = "等待回应"
            };

            var order = new Order
            {
                OrderId = 1,
                SellerId = 4 // Different from userId
            };

            _mockNegotiationsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(negotiation);
            _mockOrdersRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(order);

            // Act
            var result = await _service.HandleBargainResponseAsync(response, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("无权限操作此议价");
        }

        [Fact]
        public async Task HandleBargainResponseAsync_InvalidStatus_ReturnsFailure()
        {
            // Arrange
            var response = new BargainResponseDto
            {
                NegotiationId = 1,
                Status = "接受"
            };
            var userId = 4;

            var negotiation = new Negotiation
            {
                NegotiationId = 1,
                OrderId = 1,
                Status = "已接受" // Not "等待回应"
            };

            var order = new Order
            {
                OrderId = 1,
                SellerId = 4
            };

            _mockNegotiationsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(negotiation);
            _mockOrdersRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(order);

            // Act
            var result = await _service.HandleBargainResponseAsync(response, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("议价状态不允许回应");
        }

        #endregion

        #region GetUserNegotiationsAsync Tests

        [Fact]
        public async Task GetUserNegotiationsAsync_ValidUser_ReturnsNegotiations()
        {
            // Arrange
            var userId = 1;
            var negotiations = new List<Negotiation>
            {
                new() { NegotiationId = 1, OrderId = 1, ProposedPrice = 100.00m, Status = "等待回应", CreatedAt = DateTime.UtcNow },
                new() { NegotiationId = 2, OrderId = 2, ProposedPrice = 200.00m, Status = "接受", CreatedAt = DateTime.UtcNow }
            };

            _mockNegotiationsRepository.Setup(x => x.GetPendingNegotiationsAsync(userId))
                .ReturnsAsync(negotiations);

            // Act
            var result = await _service.GetUserNegotiationsAsync(userId, 1, 10);

            // Assert
            result.Negotiations.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Negotiations.First().NegotiationId.Should().Be(1);
        }

        [Fact]
        public async Task GetUserNegotiationsAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var userId = 1;
            var negotiations = new List<Negotiation>();
            for (int i = 1; i <= 15; i++)
            {
                negotiations.Add(new Negotiation
                {
                    NegotiationId = i,
                    OrderId = i,
                    ProposedPrice = i * 10,
                    Status = "等待回应",
                    CreatedAt = DateTime.UtcNow
                });
            }

            _mockNegotiationsRepository.Setup(x => x.GetPendingNegotiationsAsync(userId))
                .ReturnsAsync(negotiations);

            // Act
            var result = await _service.GetUserNegotiationsAsync(userId, 2, 5);

            // Assert
            result.Negotiations.Should().HaveCount(5);
            result.TotalCount.Should().Be(15);
            result.Negotiations.First().NegotiationId.Should().Be(6); // Second page, first item
        }

        #endregion

        #region GetNegotiationDetailsAsync Tests

        [Fact]
        public async Task GetNegotiationDetailsAsync_ValidNegotiation_ReturnsDetails()
        {
            // Arrange
            var negotiationId = 1;
            var userId = 3; // Buyer

            var negotiation = new Negotiation
            {
                NegotiationId = 1,
                OrderId = 1,
                ProposedPrice = 100.00m,
                Status = "等待回应",
                CreatedAt = DateTime.UtcNow
            };

            var order = new Order
            {
                OrderId = 1,
                BuyerId = 3,
                SellerId = 4
            };

            _mockNegotiationsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(negotiation);
            _mockOrdersRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(order);

            // Act
            var result = await _service.GetNegotiationDetailsAsync(negotiationId, userId);

            // Assert
            result.Should().NotBeNull();
            result!.NegotiationId.Should().Be(1);
            result.OrderId.Should().Be(1);
            result.ProposedPrice.Should().Be(100.00m);
            result.Status.Should().Be("等待回应");
        }

        [Fact]
        public async Task GetNegotiationDetailsAsync_NegotiationNotFound_ReturnsNull()
        {
            // Arrange
            var negotiationId = 999;
            var userId = 1;

            _mockNegotiationsRepository.Setup(x => x.GetByPrimaryKeyAsync(999))
                .ReturnsAsync((Negotiation?)null);

            // Act
            var result = await _service.GetNegotiationDetailsAsync(negotiationId, userId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetNegotiationDetailsAsync_UnauthorizedUser_ReturnsNull()
        {
            // Arrange
            var negotiationId = 1;
            var userId = 999; // Not buyer or seller

            var negotiation = new Negotiation
            {
                NegotiationId = 1,
                OrderId = 1
            };

            var order = new Order
            {
                OrderId = 1,
                BuyerId = 3,
                SellerId = 4
            };

            _mockNegotiationsRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(negotiation);
            _mockOrdersRepository.Setup(x => x.GetByPrimaryKeyAsync(1))
                .ReturnsAsync(order);

            // Act
            var result = await _service.GetNegotiationDetailsAsync(negotiationId, userId);

            // Assert
            result.Should().BeNull();
        }

        #endregion
    }
}
