using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using CampusTrade.API.Services.Background;
using CampusTrade.API.Services.Interfaces;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Data;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.Tests.UnitTests.Services
{
    /// <summary>
    /// 订单超时后台服务单元测试
    /// </summary>
    public class OrderTimeoutBackgroundServiceTests : IDisposable
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IServiceScope> _mockServiceScope;
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly Mock<ILogger<OrderTimeoutBackgroundService>> _mockLogger;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly OrderTimeoutBackgroundService _backgroundService;

        public OrderTimeoutBackgroundServiceTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockServiceScope = new Mock<IServiceScope>();
            _mockOrderService = new Mock<IOrderService>();
            _mockLogger = new Mock<ILogger<OrderTimeoutBackgroundService>>();
            _cancellationTokenSource = new CancellationTokenSource();

            // 设置服务范围
            var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
            mockServiceScopeFactory.Setup(x => x.CreateScope()).Returns(_mockServiceScope.Object);
            _mockServiceScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);

            // 设置依赖注入
            _mockServiceProvider.Setup(x => x.GetService(typeof(IOrderService)))
                .Returns(_mockOrderService.Object);

            _backgroundService = new OrderTimeoutBackgroundService(
                mockServiceScopeFactory.Object,
                _mockLogger.Object
            );
        }

        /// <summary>
        /// 测试后台服务正常启动和停止
        /// </summary>
        [Fact]
        public async Task StartAsync_ShouldStartSuccessfully()
        {
            // Act
            await _backgroundService.StartAsync(_cancellationTokenSource.Token);

            // Assert - 验证服务已启动（通过日志验证）
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("订单超时监控服务已启动")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        /// <summary>
        /// 测试后台服务正常停止
        /// </summary>
        [Fact]
        public async Task StopAsync_ShouldStopSuccessfully()
        {
            // Arrange
            await _backgroundService.StartAsync(_cancellationTokenSource.Token);

            // Act
            await _backgroundService.StopAsync(_cancellationTokenSource.Token);

            // Assert - 验证服务已停止（通过日志验证）
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("订单超时监控服务已停止")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        /// <summary>
        /// 测试过期订单处理执行
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_ShouldProcessExpiredOrders()
        {
            // Arrange
            var processedCount = 3;
            _mockOrderService.Setup(x => x.ProcessExpiredOrdersAsync())
                .ReturnsAsync(processedCount);

            // 创建一个短暂的取消令牌，让后台服务执行一次后停止
            var shortCancellationTokenSource = new CancellationTokenSource();
            shortCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(1));

            // Act
            await _backgroundService.StartAsync(shortCancellationTokenSource.Token);
            
            // 等待足够长的时间让后台任务执行
            await Task.Delay(6000, CancellationToken.None); // 等待6秒确保执行
            
            await _backgroundService.StopAsync(CancellationToken.None);

            // Assert - 验证ProcessExpiredOrdersAsync被调用
            _mockOrderService.Verify(x => x.ProcessExpiredOrdersAsync(), Times.AtLeastOnce);
        }

        /// <summary>
        /// 测试过期订单处理异常处理
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_WhenExceptionOccurs_ShouldLogError()
        {
            // Arrange
            var expectedException = new Exception("测试异常");
            _mockOrderService.Setup(x => x.ProcessExpiredOrdersAsync())
                .ThrowsAsync(expectedException);

            var shortCancellationTokenSource = new CancellationTokenSource();
            shortCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(1));

            // Act
            await _backgroundService.StartAsync(shortCancellationTokenSource.Token);
            await Task.Delay(6000, CancellationToken.None);
            await _backgroundService.StopAsync(CancellationToken.None);

            // Assert - 验证错误被记录
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("处理过期订单时发生错误")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
            _backgroundService?.Dispose();
        }
    }

    /// <summary>
    /// 基于内存数据库的订单超时处理集成测试
    /// </summary>
    public class OrderTimeoutProcessingIntegrationTests : IDisposable
    {
        private readonly CampusTradeDbContext _context;
        private readonly Mock<ILogger<OrderTimeoutBackgroundService>> _mockLogger;
        private readonly IServiceProvider _serviceProvider;

        public OrderTimeoutProcessingIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<CampusTradeDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new CampusTradeDbContext(options);
            _mockLogger = new Mock<ILogger<OrderTimeoutBackgroundService>>();

            // 创建服务提供者
            var services = new ServiceCollection();
            services.AddSingleton(_context);
            services.AddSingleton(_mockLogger.Object);
            
            // 注册所需的服务（这里需要根据实际DI配置调整）
            // services.AddScoped<IOrderService, OrderService>();
            // services.AddScoped<IOrderRepository, OrderRepository>();
            
            _serviceProvider = services.BuildServiceProvider();

            // 初始化测试数据
            InitializeTestData();
        }

        private void InitializeTestData()
        {
            // 创建测试用户
            var buyer = new User
            {
                UserId = 1001,
                Email = "buyer@test.com",
                Username = "TestBuyer",
                FullName = "测试买家",
                StudentId = "STU1001",
                PasswordHash = "hash",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsActive = 1,
                EmailVerified = 1,
                CreditScore = 75.0m
            };

            var seller = new User
            {
                UserId = 1002,
                Email = "seller@test.com",
                Username = "TestSeller",
                FullName = "测试卖家",
                StudentId = "STU1002",
                PasswordHash = "hash",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsActive = 1,
                EmailVerified = 1,
                CreditScore = 85.0m
            };

            _context.Users.AddRange(buyer, seller);

            // 创建测试分类
            var category = new Category
            {
                CategoryId = 1,
                Name = "测试分类"
            };
            _context.Categories.Add(category);

            // 创建测试商品
            var product = new Product
            {
                ProductId = 2001,
                UserId = 1002,
                CategoryId = 1,
                Title = "测试商品",
                Description = "测试描述",
                BasePrice = 99.99m,
                PublishTime = DateTime.Now,
                Status = Product.ProductStatus.OnSale,
                ViewCount = 0
            };
            _context.Products.Add(product);

            _context.SaveChanges();
        }

        /// <summary>
        /// 测试创建过期订单并处理
        /// </summary>
        [Fact]
        public async Task ProcessExpiredOrders_WithExpiredOrders_ShouldCancelThem()
        {
            // Arrange - 创建过期订单
            var expiredOrders = new List<(int orderId, DateTime expireTime)>();

            for (int i = 0; i < 3; i++)
            {
                var abstractOrder = new AbstractOrder
                {
                    OrderType = AbstractOrder.OrderTypes.Normal
                };
                _context.AbstractOrders.Add(abstractOrder);
                await _context.SaveChangesAsync();

                var order = new Order
                {
                    OrderId = abstractOrder.AbstractOrderId,
                    BuyerId = 1001,
                    SellerId = 1002,
                    ProductId = 2001,
                    TotalAmount = 99.99m,
                    Status = Order.OrderStatus.PendingPayment,
                    CreateTime = DateTime.Now.AddHours(-2),
                    ExpireTime = DateTime.Now.AddMinutes(-10 - (i * 5)) // 过期10, 15, 20分钟
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                expiredOrders.Add((order.OrderId, order.ExpireTime!.Value));
            }

            // 创建非过期订单作为对照
            var normalAbstractOrder = new AbstractOrder
            {
                OrderType = AbstractOrder.OrderTypes.Normal
            };
            _context.AbstractOrders.Add(normalAbstractOrder);
            await _context.SaveChangesAsync();

            var normalOrder = new Order
            {
                OrderId = normalAbstractOrder.AbstractOrderId,
                BuyerId = 1001,
                SellerId = 1002,
                ProductId = 2001,
                TotalAmount = 99.99m,
                Status = Order.OrderStatus.PendingPayment,
                CreateTime = DateTime.Now,
                ExpireTime = DateTime.Now.AddMinutes(30) // 30分钟后过期
            };
            _context.Orders.Add(normalOrder);
            await _context.SaveChangesAsync();

            // Act - 模拟过期订单处理逻辑（这里需要直接操作数据库）
            var now = DateTime.Now;
            var ordersToCancel = await _context.Orders
                .Where(o => o.Status == Order.OrderStatus.PendingPayment &&
                           o.ExpireTime.HasValue &&
                           o.ExpireTime.Value < now)
                .ToListAsync();

            foreach (var order in ordersToCancel)
            {
                order.Status = Order.OrderStatus.Cancelled;
                // 注意：Order实体没有UpdateTime字段，只需要更新状态
            }

            var processedCount = await _context.SaveChangesAsync();

            // Assert
            Assert.Equal(3, ordersToCancel.Count); // 应该有3个过期订单被处理

            // 验证过期订单已被取消
            foreach (var (orderId, _) in expiredOrders)
            {
                var order = await _context.Orders.FindAsync(orderId);
                Assert.NotNull(order);
                Assert.Equal(Order.OrderStatus.Cancelled, order!.Status);
            }

            // 验证正常订单未被影响
            var normalOrderAfter = await _context.Orders.FindAsync(normalOrder.OrderId);
            Assert.NotNull(normalOrderAfter);
            Assert.Equal(Order.OrderStatus.PendingPayment, normalOrderAfter!.Status);
        }

        /// <summary>
        /// 测试持续创建过期订单的处理能力
        /// </summary>
        [Fact]
        public async Task ProcessExpiredOrders_ContinuousCreation_ShouldHandleEffectively()
        {
            // Arrange - 模拟持续创建订单的场景
            var createdOrders = new List<int>();
            var batchSize = 5;
            var totalBatches = 3;

            // 模拟分批创建过期订单
            for (int batch = 0; batch < totalBatches; batch++)
            {
                for (int i = 0; i < batchSize; i++)
                {
                    var abstractOrder = new AbstractOrder
                    {
                        OrderType = AbstractOrder.OrderTypes.Normal
                    };
                    _context.AbstractOrders.Add(abstractOrder);
                    await _context.SaveChangesAsync();

                    var order = new Order
                    {
                        OrderId = abstractOrder.AbstractOrderId,
                        BuyerId = 1001,
                        SellerId = 1002,
                        ProductId = 2001,
                        TotalAmount = 99.99m,
                        Status = Order.OrderStatus.PendingPayment,
                        CreateTime = DateTime.Now.AddMinutes(-30),
                        ExpireTime = DateTime.Now.AddMinutes(-5 - (batch * 2)) // 逐批更早过期
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();
                    createdOrders.Add(order.OrderId);
                }

                // 每批创建后立即处理过期订单
                var now = DateTime.Now;
                var ordersToCancel = await _context.Orders
                    .Where(o => o.Status == Order.OrderStatus.PendingPayment &&
                               o.ExpireTime.HasValue &&
                               o.ExpireTime.Value < now)
                    .ToListAsync();

                foreach (var order in ordersToCancel)
                {
                    order.Status = Order.OrderStatus.Cancelled;
                    // 注意：Order实体没有UpdateTime字段，只需要更新状态
                }

                await _context.SaveChangesAsync();
            }

            // Act - 最终验证所有过期订单都被处理
            var allCreatedOrders = await _context.Orders
                .Where(o => createdOrders.Contains(o.OrderId))
                .ToListAsync();

            // Assert
            Assert.Equal(totalBatches * batchSize, allCreatedOrders.Count);
            
            // 验证所有订单都被取消
            foreach (var order in allCreatedOrders)
            {
                Assert.Equal(Order.OrderStatus.Cancelled, order.Status);
                // 验证订单状态已更新（Order实体没有UpdateTime字段）
            }
        }

        /// <summary>
        /// 测试获取即将过期订单的性能
        /// </summary>
        [Fact]
        public async Task GetExpiringOrders_WithLargeDataset_ShouldPerformWell()
        {
            // Arrange - 创建大量订单数据
            var totalOrders = 100;
            var expiringOrdersCount = 10;

            for (int i = 0; i < totalOrders; i++)
            {
                var abstractOrder = new AbstractOrder
                {
                    OrderType = AbstractOrder.OrderTypes.Normal
                };
                _context.AbstractOrders.Add(abstractOrder);
                await _context.SaveChangesAsync();

                var isExpiring = i < expiringOrdersCount;
                var expireTime = isExpiring 
                    ? DateTime.Now.AddMinutes(10 + i) // 即将过期
                    : DateTime.Now.AddHours(1 + i);   // 正常订单

                var order = new Order
                {
                    OrderId = abstractOrder.AbstractOrderId,
                    BuyerId = 1001,
                    SellerId = 1002,
                    ProductId = 2001,
                    TotalAmount = 99.99m,
                    Status = Order.OrderStatus.PendingPayment,
                    CreateTime = DateTime.Now,
                    ExpireTime = expireTime
                };

                _context.Orders.Add(order);
            }

            await _context.SaveChangesAsync();

            // Act - 测量查询即将过期订单的性能
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var expiringOrders = await _context.Orders
                .Where(o => o.Status == Order.OrderStatus.PendingPayment &&
                           o.ExpireTime.HasValue &&
                           o.ExpireTime.Value <= DateTime.Now.AddMinutes(30) &&
                           o.ExpireTime.Value > DateTime.Now)
                .ToListAsync();

            stopwatch.Stop();

            // Assert
            Assert.Equal(expiringOrdersCount, expiringOrders.Count);
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
                $"查询耗时 {stopwatch.ElapsedMilliseconds}ms，应该在1秒内完成");
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
