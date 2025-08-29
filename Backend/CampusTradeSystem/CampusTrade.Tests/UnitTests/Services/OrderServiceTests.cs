using CampusTrade.API.Models.DTOs.Order;
using CampusTrade.API.Models.DTOs.Payment;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services;
using CampusTrade.API.Services.Interfaces;
using CampusTrade.API.Services.Order;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services
{
    /// <summary>
    /// 订单服务单元测试
    /// </summary>
    public class OrderServiceTests
    {
        private readonly Mock<IOrderRepository> _mockOrderRepository;
        private readonly Mock<IRepository<Product>> _mockProductRepository;
        private readonly Mock<IRepository<User>> _mockUserRepository;
        private readonly Mock<IRepository<AbstractOrder>> _mockAbstractOrderRepository;
        private readonly Mock<IVirtualAccountsRepository> _mockVirtualAccountRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<OrderService>> _mockLogger;
        private readonly OrderService _orderService;
        private readonly Mock<ICreditService> _mockCreditService;


        public OrderServiceTests()
        {
            _mockOrderRepository = new Mock<IOrderRepository>();
            _mockProductRepository = new Mock<IRepository<Product>>();
            _mockUserRepository = new Mock<IRepository<User>>();
            _mockAbstractOrderRepository = new Mock<IRepository<AbstractOrder>>();
            _mockVirtualAccountRepository = new Mock<IVirtualAccountsRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<OrderService>>();
            _mockCreditService = new Mock<ICreditService>();

            _orderService = new OrderService(
                _mockOrderRepository.Object,
                _mockProductRepository.Object,
                _mockUserRepository.Object,
                _mockAbstractOrderRepository.Object,
                _mockVirtualAccountRepository.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object
                ,_mockCreditService.Object

            );
        }

        #region 订单创建测试

        [Fact]
        public async Task CreateOrderAsync_ValidRequest_ShouldSucceed()
        {
            // Arrange
            var userId = 1001;
            var request = new CreateOrderRequest
            {
                ProductId = 2001,
                FinalPrice = 99.99m,
                Remarks = "测试订单"
            };

            var product = new Product
            {
                ProductId = 2001,
                UserId = 1002, // 不同的用户ID，避免自购
                Title = "测试商品",
                BasePrice = 100.00m,
                Status = Product.ProductStatus.OnSale
            };

            _mockProductRepository.Setup(x => x.GetByPrimaryKeyAsync(request.ProductId))
                .ReturnsAsync(product);

            _mockOrderRepository.Setup(x => x.GetByBuyerIdAsync(userId))
                .ReturnsAsync(new List<Order>());

            _mockAbstractOrderRepository.Setup(x => x.AddAsync(It.IsAny<AbstractOrder>()))
                .ReturnsAsync((AbstractOrder order) => order);

            _mockOrderRepository.Setup(x => x.AddAsync(It.IsAny<Order>()))
                .ReturnsAsync((Order order) => order);

            _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync())
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            // 模拟订单详情查询
            var orderDetail = new OrderDetailResponse
            {
                OrderId = 1,
                BuyerId = userId,
                SellerId = 1002,
                ProductId = request.ProductId,
                TotalAmount = request.FinalPrice,
                Status = Order.OrderStatus.PendingPayment,
                CreateTime = DateTime.Now,
                ExpireTime = DateTime.Now.AddMinutes(30)
            };

            _mockOrderRepository.Setup(x => x.GetOrderWithDetailsAsync(It.IsAny<int>()))
                .ReturnsAsync(new Order
                {
                    OrderId = 1,
                    BuyerId = userId,
                    SellerId = 1002,
                    ProductId = request.ProductId,
                    TotalAmount = request.FinalPrice,
                    Status = Order.OrderStatus.PendingPayment,
                    CreateTime = DateTime.Now,
                    ExpireTime = DateTime.Now.AddMinutes(30),
                    Product = product,
                    Buyer = new User { UserId = userId, Username = "TestBuyer" },
                    Seller = new User { UserId = 1002, Username = "TestSeller" }
                });

            // Act
            var result = await _orderService.CreateOrderAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(Order.OrderStatus.PendingPayment, result.Status);
            Assert.NotNull(result.ExpireTime);
            Assert.True(result.ExpireTime > DateTime.Now);

            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _mockOrderRepository.Verify(x => x.AddAsync(It.IsAny<Order>()), Times.Once);
        }

        [Fact]
        public async Task CreateOrderAsync_ProductNotFound_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = 1001;
            var request = new CreateOrderRequest { ProductId = 9999 };

            _mockProductRepository.Setup(x => x.GetByPrimaryKeyAsync(request.ProductId))
                .ReturnsAsync((Product?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _orderService.CreateOrderAsync(userId, request));
        }

        [Fact]
        public async Task CreateOrderAsync_ProductNotOnSale_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var userId = 1001;
            var request = new CreateOrderRequest { ProductId = 2001 };
            var product = new Product
            {
                ProductId = 2001,
                UserId = 1002,
                Status = Product.ProductStatus.OffShelf
            };

            _mockProductRepository.Setup(x => x.GetByPrimaryKeyAsync(request.ProductId))
                .ReturnsAsync(product);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _orderService.CreateOrderAsync(userId, request));
        }

        [Fact]
        public async Task CreateOrderAsync_SelfPurchase_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var userId = 1001;
            var request = new CreateOrderRequest { ProductId = 2001 };
            var product = new Product
            {
                ProductId = 2001,
                UserId = userId, // 同一个用户
                Status = Product.ProductStatus.OnSale
            };

            _mockProductRepository.Setup(x => x.GetByPrimaryKeyAsync(request.ProductId))
                .ReturnsAsync(product);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _orderService.CreateOrderAsync(userId, request));
        }

        #endregion

        #region 状态转换测试

        [Theory]
        [InlineData(Order.OrderStatus.PendingPayment, Order.OrderStatus.Paid, "buyer", true)]
        [InlineData(Order.OrderStatus.PendingPayment, Order.OrderStatus.Cancelled, "buyer", true)]
        [InlineData(Order.OrderStatus.PendingPayment, Order.OrderStatus.Cancelled, "seller", true)]
        [InlineData(Order.OrderStatus.Paid, Order.OrderStatus.Shipped, "seller", true)]
        [InlineData(Order.OrderStatus.Shipped, Order.OrderStatus.Delivered, "buyer", true)]
        [InlineData(Order.OrderStatus.Delivered, Order.OrderStatus.Completed, "buyer", true)]
        [InlineData(Order.OrderStatus.Delivered, Order.OrderStatus.Completed, "seller", true)]
        public void IsValidStatusTransition_ValidTransitions_ShouldReturnTrue(
            string currentStatus, string newStatus, string userRole, bool expected)
        {
            // Act
            var result = _orderService.IsValidStatusTransition(currentStatus, newStatus, userRole);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(Order.OrderStatus.PendingPayment, Order.OrderStatus.Shipped, "buyer", false)]
        [InlineData(Order.OrderStatus.Paid, Order.OrderStatus.PendingPayment, "seller", false)]
        [InlineData(Order.OrderStatus.Shipped, Order.OrderStatus.Paid, "buyer", false)]
        [InlineData(Order.OrderStatus.Completed, Order.OrderStatus.Cancelled, "buyer", false)]
        [InlineData(Order.OrderStatus.Cancelled, Order.OrderStatus.Paid, "buyer", false)]
        public void IsValidStatusTransition_InvalidTransitions_ShouldReturnFalse(
            string currentStatus, string newStatus, string userRole, bool expected)
        {
            // Act
            var result = _orderService.IsValidStatusTransition(currentStatus, newStatus, userRole);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region 订单状态更新测试

        [Fact]
        public async Task UpdateOrderStatusAsync_ValidTransition_ShouldSucceed()
        {
            // Arrange
            var orderId = 1001;
            var userId = 1002; // 买家
            var request = new UpdateOrderStatusRequest
            {
                Status = Order.OrderStatus.Paid,
                Remarks = "确认付款"
            };

            var order = new Order
            {
                OrderId = orderId,
                BuyerId = userId,
                SellerId = 1003,
                Status = Order.OrderStatus.PendingPayment
            };

            _mockOrderRepository.Setup(x => x.GetOrderWithDetailsAsync(orderId))
                .ReturnsAsync(order);

            _mockOrderRepository.Setup(x => x.UpdateOrderStatusAsync(orderId, request.Status))
                .ReturnsAsync(true);

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync())
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _orderService.UpdateOrderStatusAsync(orderId, userId, request);

            // Assert
            Assert.True(result);
            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _mockOrderRepository.Verify(x => x.UpdateOrderStatusAsync(orderId, request.Status), Times.Once);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_UnauthorizedUser_ShouldReturnFalse()
        {
            // Arrange
            var orderId = 1001;
            var unauthorizedUserId = 9999;
            var request = new UpdateOrderStatusRequest { Status = Order.OrderStatus.Paid };

            var order = new Order
            {
                OrderId = orderId,
                BuyerId = 1002,
                SellerId = 1003,
                Status = Order.OrderStatus.PendingPayment
            };

            _mockOrderRepository.Setup(x => x.GetOrderWithDetailsAsync(orderId))
                .ReturnsAsync(order);

            // Act
            var result = await _orderService.UpdateOrderStatusAsync(orderId, unauthorizedUserId, request);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region 过期订单测试

        [Fact]
        public async Task ProcessExpiredOrdersAsync_HasExpiredOrders_ShouldCancelThem()
        {
            // Arrange
            var expiredOrders = new List<Order>
            {
                new Order
                {
                    OrderId = 1001,
                    Status = Order.OrderStatus.PendingPayment,
                    ExpireTime = DateTime.Now.AddHours(-1)
                },
                new Order
                {
                    OrderId = 1002,
                    Status = Order.OrderStatus.PendingPayment,
                    ExpireTime = DateTime.Now.AddMinutes(-30)
                }
            };

            _mockOrderRepository.Setup(x => x.GetExpiredOrdersAsync())
                .ReturnsAsync(expiredOrders);

            _mockOrderRepository.Setup(x => x.UpdateOrderStatusAsync(It.IsAny<int>(), Order.OrderStatus.Cancelled))
                .ReturnsAsync(true);

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync())
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _orderService.ProcessExpiredOrdersAsync();

            // Assert
            Assert.Equal(2, result);
            _mockOrderRepository.Verify(x => x.UpdateOrderStatusAsync(1001, Order.OrderStatus.Cancelled), Times.Once);
            _mockOrderRepository.Verify(x => x.UpdateOrderStatusAsync(1002, Order.OrderStatus.Cancelled), Times.Once);
        }

        [Fact]
        public async Task ProcessExpiredOrdersAsync_NoExpiredOrders_ShouldReturnZero()
        {
            // Arrange
            _mockOrderRepository.Setup(x => x.GetExpiredOrdersAsync())
                .ReturnsAsync(new List<Order>());

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync())
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _orderService.ProcessExpiredOrdersAsync();

            // Assert
            Assert.Equal(0, result);
        }

        #endregion

        #region 权限测试

        [Fact]
        public async Task HasOrderPermissionAsync_AuthorizedUser_ShouldReturnTrue()
        {
            // Arrange
            var orderId = 1001;
            var buyerId = 1002;
            var order = new Order
            {
                OrderId = orderId,
                BuyerId = buyerId,
                SellerId = 1003
            };

            _mockOrderRepository.Setup(x => x.GetByPrimaryKeyAsync(orderId))
                .ReturnsAsync(order);

            // Act
            var result = await _orderService.HasOrderPermissionAsync(orderId, buyerId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasOrderPermissionAsync_UnauthorizedUser_ShouldReturnFalse()
        {
            // Arrange
            var orderId = 1001;
            var unauthorizedUserId = 9999;
            var order = new Order
            {
                OrderId = orderId,
                BuyerId = 1002,
                SellerId = 1003
            };

            _mockOrderRepository.Setup(x => x.GetByPrimaryKeyAsync(orderId))
                .ReturnsAsync(order);

            // Act
            var result = await _orderService.HasOrderPermissionAsync(orderId, unauthorizedUserId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HasOrderPermissionAsync_OrderNotFound_ShouldReturnFalse()
        {
            // Arrange
            var orderId = 9999;
            var userId = 1002;

            _mockOrderRepository.Setup(x => x.GetByPrimaryKeyAsync(orderId))
                .ReturnsAsync((Order?)null);

            // Act
            var result = await _orderService.HasOrderPermissionAsync(orderId, userId);

            // Assert
            Assert.False(result);
        }

        #endregion
    }
}
