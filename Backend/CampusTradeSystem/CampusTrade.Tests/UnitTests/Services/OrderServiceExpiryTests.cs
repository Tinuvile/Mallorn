using Moq;
using CampusTrade.API.Services.Order;
using CampusTrade.API.Services.Interfaces;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Models.DTOs.Order;
using CampusTrade.API.Models.DTOs.Payment;
using CampusTrade.API.Models.Entities;
using Microsoft.Extensions.Logging;

namespace CampusTrade.Tests.UnitTests.Services
{
    /// <summary>
    /// OrderService 过期订单处理功能专项测试
    /// </summary>
    public class OrderServiceExpiryTests
    {
        private readonly Mock<IOrderRepository> _mockOrderRepository;
        private readonly Mock<IRepository<Product>> _mockProductRepository;
        private readonly Mock<IRepository<User>> _mockUserRepository;
        private readonly Mock<IRepository<AbstractOrder>> _mockAbstractOrderRepository;
        private readonly Mock<IVirtualAccountsRepository> _mockVirtualAccountRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<OrderService>> _mockLogger;
        private readonly OrderService _orderService;

        public OrderServiceExpiryTests()
        {
            _mockOrderRepository = new Mock<IOrderRepository>();
            _mockProductRepository = new Mock<IRepository<Product>>();
            _mockUserRepository = new Mock<IRepository<User>>();
            _mockAbstractOrderRepository = new Mock<IRepository<AbstractOrder>>();
            _mockVirtualAccountRepository = new Mock<IVirtualAccountsRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<OrderService>>();

            // 设置 UnitOfWork
            _mockUnitOfWork.Setup(u => u.Orders).Returns(_mockOrderRepository.Object);

            _orderService = new OrderService(
                _mockOrderRepository.Object,
                _mockProductRepository.Object,
                _mockUserRepository.Object,
                _mockAbstractOrderRepository.Object,
                _mockVirtualAccountRepository.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object
            );
        }

        /// <summary>
        /// 测试获取即将过期订单功能
        /// </summary>
        [Fact]
        public async Task GetExpiringOrdersAsync_ShouldReturnCorrectOrders()
        {
            // Arrange
            var beforeMinutes = 30;
            var cutoffTime = DateTime.Now.AddMinutes(beforeMinutes);
            var mockOrders = new List<Order>
            {
                new Order
                {
                    OrderId = 1,
                    Status = Order.OrderStatus.PendingPayment,
                    ExpireTime = DateTime.Now.AddMinutes(10),
                    BuyerId = 1001,
                    SellerId = 1002
                },
                new Order
                {
                    OrderId = 2,
                    Status = Order.OrderStatus.PendingPayment,
                    ExpireTime = DateTime.Now.AddMinutes(25),
                    BuyerId = 1001,
                    SellerId = 1002
                }
            };

            _mockOrderRepository.Setup(r => r.GetExpiringOrdersAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(mockOrders);

            _mockOrderRepository.Setup(r => r.GetOrderWithDetailsAsync(It.IsAny<int>()))
                .ReturnsAsync((int orderId) => mockOrders.FirstOrDefault(o => o.OrderId == orderId));

            // Act
            var result = await _orderService.GetExpiringOrdersAsync(beforeMinutes);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            _mockOrderRepository.Verify(r => r.GetExpiringOrdersAsync(It.IsAny<DateTime>()), Times.Once);
        }

        /// <summary>
        /// 测试处理过期订单功能
        /// </summary>
        [Fact]
        public async Task ProcessExpiredOrdersAsync_ShouldCancelExpiredOrders()
        {
            // Arrange
            var expiredOrders = new List<Order>
            {
                new Order
                {
                    OrderId = 1,
                    Status = Order.OrderStatus.PendingPayment,
                    ExpireTime = DateTime.Now.AddMinutes(-10),
                    BuyerId = 1001,
                    SellerId = 1002
                },
                new Order
                {
                    OrderId = 2,
                    Status = Order.OrderStatus.PendingPayment,
                    ExpireTime = DateTime.Now.AddMinutes(-20),
                    BuyerId = 1003,
                    SellerId = 1004
                }
            };

            _mockOrderRepository.Setup(r => r.GetExpiredOrdersAsync())
                .ReturnsAsync(expiredOrders);

            _mockOrderRepository.Setup(r => r.UpdateOrderStatusAsync(It.IsAny<int>(), Order.OrderStatus.Cancelled))
                .ReturnsAsync(true);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync())
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _orderService.ProcessExpiredOrdersAsync();

            // Assert
            Assert.Equal(2, result);

            // 验证每个过期订单都被更新状态
            _mockOrderRepository.Verify(
                r => r.UpdateOrderStatusAsync(It.IsAny<int>(), Order.OrderStatus.Cancelled),
                Times.Exactly(2));

            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        /// <summary>
        /// 测试处理过期订单时的异常处理
        /// </summary>
        [Fact]
        public async Task ProcessExpiredOrdersAsync_WhenExceptionOccurs_ShouldThrowException()
        {
            // Arrange
            _mockOrderRepository.Setup(r => r.GetExpiredOrdersAsync())
                .ThrowsAsync(new Exception("数据库连接失败"));

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync())
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _orderService.ProcessExpiredOrdersAsync());
            Assert.Equal("数据库连接失败", exception.Message);

            // 验证回滚事务被调用
            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        /// <summary>
        /// 测试大批量过期订单处理
        /// </summary>
        [Fact]
        public async Task ProcessExpiredOrdersAsync_WithLargeBatch_ShouldHandleEfficiently()
        {
            // Arrange
            var batchSize = 100;
            var expiredOrders = new List<Order>();

            for (int i = 1; i <= batchSize; i++)
            {
                expiredOrders.Add(new Order
                {
                    OrderId = i,
                    Status = Order.OrderStatus.PendingPayment,
                    ExpireTime = DateTime.Now.AddMinutes(-i),
                    BuyerId = 1001 + (i % 10), // 分散到不同用户
                    SellerId = 2001 + (i % 5)
                });
            }

            _mockOrderRepository.Setup(r => r.GetExpiredOrdersAsync())
                .ReturnsAsync(expiredOrders);

            _mockOrderRepository.Setup(r => r.UpdateOrderStatusAsync(It.IsAny<int>(), Order.OrderStatus.Cancelled))
                .ReturnsAsync(true);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync())
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(batchSize);

            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await _orderService.ProcessExpiredOrdersAsync();
            stopwatch.Stop();

            // Assert
            Assert.Equal(batchSize, result);
            Assert.True(stopwatch.ElapsedMilliseconds < 1000,
                $"处理 {batchSize} 个订单耗时 {stopwatch.ElapsedMilliseconds}ms，应该在1秒内完成");

            _mockOrderRepository.Verify(r => r.UpdateOrderStatusAsync(It.IsAny<int>(), Order.OrderStatus.Cancelled), Times.Exactly(batchSize));
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// 测试空结果处理
        /// </summary>
        [Fact]
        public async Task ProcessExpiredOrdersAsync_WithNoExpiredOrders_ShouldReturn0()
        {
            // Arrange
            _mockOrderRepository.Setup(r => r.GetExpiredOrdersAsync())
                .ReturnsAsync(new List<Order>());

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync())
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(0);

            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _orderService.ProcessExpiredOrdersAsync();

            // Assert
            Assert.Equal(0, result);

            _mockOrderRepository.Verify(r => r.UpdateOrderStatusAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// 测试获取即将过期订单的边界条件
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(60)]
        [InlineData(1440)] // 24小时
        public async Task GetExpiringOrdersAsync_WithDifferentTimeRanges_ShouldWork(int beforeMinutes)
        {
            // Arrange
            var mockOrders = new List<Order>();
            _mockOrderRepository.Setup(r => r.GetExpiringOrdersAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(mockOrders);

            // Act
            var result = await _orderService.GetExpiringOrdersAsync(beforeMinutes);

            // Assert
            Assert.NotNull(result);
            _mockOrderRepository.Verify(r => r.GetExpiringOrdersAsync(It.IsAny<DateTime>()), Times.Once);
        }

        /// <summary>
        /// 测试状态转换验证
        /// </summary>
        [Theory]
        [InlineData(Order.OrderStatus.PendingPayment, Order.OrderStatus.Paid, "buyer", true)]
        [InlineData(Order.OrderStatus.PendingPayment, Order.OrderStatus.Cancelled, "buyer", true)]
        [InlineData(Order.OrderStatus.PendingPayment, Order.OrderStatus.Cancelled, "seller", true)]
        [InlineData(Order.OrderStatus.Paid, Order.OrderStatus.Shipped, "seller", true)]
        [InlineData(Order.OrderStatus.Shipped, Order.OrderStatus.Delivered, "buyer", true)]
        [InlineData(Order.OrderStatus.Delivered, Order.OrderStatus.Completed, "buyer", true)]
        [InlineData(Order.OrderStatus.Delivered, Order.OrderStatus.Completed, "seller", true)]
        [InlineData(Order.OrderStatus.PendingPayment, Order.OrderStatus.Shipped, "buyer", false)]
        [InlineData(Order.OrderStatus.Paid, Order.OrderStatus.Delivered, "seller", false)]
        [InlineData(Order.OrderStatus.Completed, Order.OrderStatus.Cancelled, "buyer", false)]
        public void IsValidStatusTransition_ShouldReturnCorrectResult(
            string fromStatus, string toStatus, string userRole, bool expected)
        {
            // Act
            var result = _orderService.IsValidStatusTransition(fromStatus, toStatus, userRole);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试并发处理过期订单的安全性
        /// </summary>
        [Fact]
        public async Task ProcessExpiredOrdersAsync_ConcurrentCalls_ShouldBeSafe()
        {
            // Arrange
            var expiredOrders = new List<Order>
            {
                new Order
                {
                    OrderId = 1,
                    Status = Order.OrderStatus.PendingPayment,
                    ExpireTime = DateTime.Now.AddMinutes(-10)
                }
            };

            _mockOrderRepository.Setup(r => r.GetExpiredOrdersAsync())
                .ReturnsAsync(expiredOrders);

            _mockOrderRepository.Setup(r => r.UpdateOrderStatusAsync(It.IsAny<int>(), Order.OrderStatus.Cancelled))
                .ReturnsAsync(true);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync())
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act - 并发调用
            var tasks = new List<Task<int>>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(_orderService.ProcessExpiredOrdersAsync());
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, result => Assert.True(result >= 0));

            // 验证方法被正确调用（可能被调用多次）
            _mockOrderRepository.Verify(r => r.GetExpiredOrdersAsync(), Times.AtLeast(1));
        }
    }
}
