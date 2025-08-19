using CampusTrade.API.Data;
using CampusTrade.API.Models.DTOs.Order;
using CampusTrade.API.Models.DTOs.Payment;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services;
using CampusTrade.API.Services.Interfaces;
using CampusTrade.API.Services.Order;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.IntegrationTests
{
    /// <summary>
    /// 订单服务集成测试 - 使用真实的数据库上下文和服务
    /// </summary>
    public class OrderServiceIntegrationTests : IDisposable
    {
        private readonly CampusTradeDbContext _context;
        private readonly OrderService _orderService;
        private readonly Mock<ILogger<OrderService>> _mockLogger;
        private readonly Mock<ICreditService> _mockCreditService;


        public OrderServiceIntegrationTests()
        {
            // 使用内存数据库
            var options = new DbContextOptionsBuilder<CampusTradeDbContext>()
                .UseInMemoryDatabase(databaseName: "OrderIntegrationTestDb_" + Guid.NewGuid())
                .Options;

            _context = new CampusTradeDbContext(options);
            _context.Database.EnsureCreated();

            // 创建真实的仓储实现（如果有的话）或Mock
            var mockOrderRepository = new Mock<IOrderRepository>();
            var mockProductRepository = new Mock<IRepository<Product>>();
            var mockUserRepository = new Mock<IRepository<User>>();
            var mockAbstractOrderRepository = new Mock<IRepository<AbstractOrder>>();
            var mockVirtualAccountRepository = new Mock<IVirtualAccountsRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<OrderService>>();
            _mockCreditService = new Mock<ICreditService>();

            // 配置Mock的基本行为
            SetupMockRepositories(mockOrderRepository, mockProductRepository, mockUserRepository,
                                mockAbstractOrderRepository, mockVirtualAccountRepository, mockUnitOfWork);

            _orderService = new OrderService(
                mockOrderRepository.Object,
                mockProductRepository.Object,
                mockUserRepository.Object,
                mockAbstractOrderRepository.Object,
                mockVirtualAccountRepository.Object,
                mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockCreditService.Object
            );
        }

        private void SetupMockRepositories(
            Mock<IOrderRepository> mockOrderRepository,
            Mock<IRepository<Product>> mockProductRepository,
            Mock<IRepository<User>> mockUserRepository,
            Mock<IRepository<AbstractOrder>> mockAbstractOrderRepository,
            Mock<IVirtualAccountsRepository> mockVirtualAccountRepository,
            Mock<IUnitOfWork> mockUnitOfWork)
        {
            // 配置Product Repository
            mockProductRepository.Setup(x => x.GetByPrimaryKeyAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => _context.Products.FirstOrDefault(p => p.ProductId == id));

            // 配置User Repository
            mockUserRepository.Setup(x => x.GetByPrimaryKeyAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => _context.Users.FirstOrDefault(u => u.UserId == id));

            // 配置Order Repository
            mockOrderRepository.Setup(x => x.GetByPrimaryKeyAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => _context.Orders.FirstOrDefault(o => o.OrderId == id));

            mockOrderRepository.Setup(x => x.GetByBuyerIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int buyerId) => _context.Orders.Where(o => o.BuyerId == buyerId).ToList());

            mockOrderRepository.Setup(x => x.GetOrderWithDetailsAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => _context.Orders
                    .Include(o => o.Product)
                    .Include(o => o.Buyer)
                    .Include(o => o.Seller)
                    .FirstOrDefault(o => o.OrderId == id));

            mockOrderRepository.Setup(x => x.AddAsync(It.IsAny<Order>()))
                .ReturnsAsync((Order order) =>
                {
                    _context.Orders.Add(order);
                    _context.SaveChanges();
                    return order;
                });

            mockOrderRepository.Setup(x => x.UpdateOrderStatusAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((int orderId, string status) =>
                {
                    var order = _context.Orders.Find(orderId);
                    if (order != null)
                    {
                        order.Status = status;
                        _context.SaveChanges();
                        return true;
                    }
                    return false;
                });

            // 配置Abstract Order Repository
            mockAbstractOrderRepository.Setup(x => x.AddAsync(It.IsAny<AbstractOrder>()))
                .ReturnsAsync((AbstractOrder abstractOrder) =>
                {
                    _context.AbstractOrders.Add(abstractOrder);
                    _context.SaveChanges();
                    return abstractOrder;
                });

            // 配置Unit of Work
            mockUnitOfWork.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(() => _context.SaveChanges());

            mockUnitOfWork.Setup(x => x.BeginTransactionAsync())
                .Returns(Task.CompletedTask);

            mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            mockUnitOfWork.Setup(x => x.RollbackTransactionAsync())
                .Returns(Task.CompletedTask);

            // 配置Virtual Account Repository (基本设置)
            mockVirtualAccountRepository.Setup(x => x.HasSufficientBalanceAsync(It.IsAny<int>(), It.IsAny<decimal>()))
                .ReturnsAsync(true);

            mockVirtualAccountRepository.Setup(x => x.GetBalanceAsync(It.IsAny<int>()))
                .ReturnsAsync(1000.00m);

            mockVirtualAccountRepository.Setup(x => x.DebitAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            mockVirtualAccountRepository.Setup(x => x.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>()))
                .ReturnsAsync(true);
        }

        [Fact]
        public async Task CreateOrder_IntegrationTest_ShouldSucceed()
        {
            // Arrange - 准备测试数据
            await SeedTestDataAsync();

            var buyerId = 1001;
            var request = new CreateOrderRequest
            {
                ProductId = 2001,
                FinalPrice = 99.99m,
                Remarks = "集成测试订单"
            };

            // Act - 执行订单创建
            var result = await _orderService.CreateOrderAsync(buyerId, request);

            // Assert - 验证结果
            Assert.NotNull(result);
            Assert.Equal(Order.OrderStatus.PendingPayment, result.Status);
            Assert.Equal(buyerId, result.BuyerId);
            Assert.Equal(1002, result.SellerId);
            Assert.Equal(request.ProductId, result.ProductId);
            Assert.Equal(request.FinalPrice, result.TotalAmount);
        }

        [Fact]
        public void OrderStatusTransition_IntegrationTest_ShouldWorkCorrectly()
        {
            // Arrange & Act & Assert - 测试各种状态转换
            var testCases = new[]
            {
                new { Current = Order.OrderStatus.PendingPayment, New = Order.OrderStatus.Paid, Role = "buyer", Expected = true },
                new { Current = Order.OrderStatus.PendingPayment, New = Order.OrderStatus.Cancelled, Role = "buyer", Expected = true },
                new { Current = Order.OrderStatus.Paid, New = Order.OrderStatus.Shipped, Role = "seller", Expected = true },
                new { Current = Order.OrderStatus.Shipped, New = Order.OrderStatus.Delivered, Role = "buyer", Expected = true },
                new { Current = Order.OrderStatus.Delivered, New = Order.OrderStatus.Completed, Role = "buyer", Expected = true },
                new { Current = Order.OrderStatus.PendingPayment, New = Order.OrderStatus.Shipped, Role = "buyer", Expected = false },
                new { Current = Order.OrderStatus.Completed, New = Order.OrderStatus.Cancelled, Role = "buyer", Expected = false }
            };

            foreach (var testCase in testCases)
            {
                var result = _orderService.IsValidStatusTransition(testCase.Current, testCase.New, testCase.Role);
                Assert.Equal(testCase.Expected, result);
            }
        }

        [Fact]
        public async Task Database_Context_ShouldWorkCorrectly()
        {
            // Arrange - 创建测试数据
            await SeedTestDataAsync();

            // Act - 查询数据
            var categories = await _context.Categories.ToListAsync();
            var users = await _context.Users.ToListAsync();
            var products = await _context.Products.ToListAsync();

            // Assert - 验证数据
            Assert.NotEmpty(categories);
            Assert.NotEmpty(users);
            Assert.NotEmpty(products);

            var category = categories.First(c => c.CategoryId == 3001);
            var user = users.First(u => u.UserId == 1001);
            var product = products.First(p => p.ProductId == 2001);

            Assert.Equal("电子产品", category.Name);
            Assert.Equal("testbuyer", user.Username);
            Assert.Equal("测试商品", product.Title);
        }

        [Fact]
        public async Task OrderPermission_IntegrationTest_ShouldWorkCorrectly()
        {
            // Arrange
            await SeedTestDataAsync();

            // 创建一个测试订单
            var order = new Order
            {
                OrderId = 5001,
                BuyerId = 1001,
                SellerId = 1002,
                ProductId = 2001,
                TotalAmount = 100.00m,
                Status = Order.OrderStatus.PendingPayment,
                CreateTime = DateTime.UtcNow,
                ExpireTime = DateTime.UtcNow.AddMinutes(30)
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Act & Assert
            var hasBuyerPermission = await _orderService.HasOrderPermissionAsync(5001, 1001);
            var hasSellerPermission = await _orderService.HasOrderPermissionAsync(5001, 1002);
            var hasNoPermission = await _orderService.HasOrderPermissionAsync(5001, 9999);

            Assert.True(hasBuyerPermission);
            Assert.True(hasSellerPermission);
            Assert.False(hasNoPermission);
        }

        [Fact]
        public async Task CompleteOrderFlow_IntegrationTest_ShouldWorkEndToEnd()
        {
            // Arrange
            await SeedTestDataAsync();

            var buyerId = 1001;
            var sellerId = 1002;
            var createRequest = new CreateOrderRequest
            {
                ProductId = 2001,
                FinalPrice = 99.99m,
                Remarks = "完整流程测试"
            };

            // Act 1: 创建订单
            var createdOrder = await _orderService.CreateOrderAsync(buyerId, createRequest);
            Assert.NotNull(createdOrder);
            Assert.Equal(Order.OrderStatus.PendingPayment, createdOrder.Status);

            // Act 2: 买家付款
            var paymentRequest = new UpdateOrderStatusRequest
            {
                Status = Order.OrderStatus.Paid,
                Remarks = "已付款"
            };
            var paymentResult = await _orderService.UpdateOrderStatusAsync(createdOrder.OrderId, buyerId, paymentRequest);
            Assert.True(paymentResult);

            // Act 3: 卖家发货
            var shipRequest = new UpdateOrderStatusRequest
            {
                Status = Order.OrderStatus.Shipped,
                Remarks = "已发货"
            };
            var shipResult = await _orderService.UpdateOrderStatusAsync(createdOrder.OrderId, sellerId, shipRequest);
            Assert.True(shipResult);

            // Act 4: 买家确认收货
            var deliverRequest = new UpdateOrderStatusRequest
            {
                Status = Order.OrderStatus.Delivered,
                Remarks = "已收货"
            };
            var deliverResult = await _orderService.UpdateOrderStatusAsync(createdOrder.OrderId, buyerId, deliverRequest);
            Assert.True(deliverResult);

            // Act 5: 完成订单
            var completeRequest = new UpdateOrderStatusRequest
            {
                Status = Order.OrderStatus.Completed,
                Remarks = "交易完成"
            };
            var completeResult = await _orderService.UpdateOrderStatusAsync(createdOrder.OrderId, buyerId, completeRequest);
            Assert.True(completeResult);

            // Assert - 验证整个流程
            var finalOrder = await _context.Orders.FindAsync(createdOrder.OrderId);
            Assert.NotNull(finalOrder);
            Assert.Equal(Order.OrderStatus.Completed, finalOrder.Status);
        }

        /// <summary>
        /// 准备测试数据
        /// </summary>
        private async Task SeedTestDataAsync()
        {
            // 清空现有数据
            _context.Categories.RemoveRange(_context.Categories);
            _context.Users.RemoveRange(_context.Users);
            _context.Products.RemoveRange(_context.Products);
            _context.Orders.RemoveRange(_context.Orders);
            await _context.SaveChangesAsync();

            // 创建测试分类
            var category = new Category
            {
                CategoryId = 3001,
                Name = "电子产品"
            };
            _context.Categories.Add(category);

            // 创建测试买家
            var buyer = new User
            {
                UserId = 1001,
                Username = "testbuyer",
                Email = "buyer@example.com",
                PasswordHash = "hashedpassword",
                StudentId = "2021001",
                FullName = "测试买家",
                Phone = "13800138000",
                IsActive = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreditScore = 85.0m
            };
            _context.Users.Add(buyer);

            // 创建测试卖家
            var seller = new User
            {
                UserId = 1002,
                Username = "testseller",
                Email = "seller@example.com",
                PasswordHash = "hashedpassword",
                StudentId = "2021002",
                FullName = "测试卖家",
                Phone = "13800138001",
                IsActive = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreditScore = 90.0m
            };
            _context.Users.Add(seller);

            await _context.SaveChangesAsync();

            // 创建测试商品
            var product = new Product
            {
                ProductId = 2001,
                Title = "测试商品",
                Description = "这是一个集成测试商品",
                BasePrice = 100.00m,
                CategoryId = 3001,
                UserId = 1002, // 卖家ID
                Status = "在售",
                ViewCount = 0,
                PublishTime = DateTime.UtcNow
            };
            _context.Products.Add(product);

            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
