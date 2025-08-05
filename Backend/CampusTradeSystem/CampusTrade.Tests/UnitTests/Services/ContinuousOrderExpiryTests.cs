using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CampusTrade.API.Services.Interfaces;
using CampusTrade.API.Models.DTOs.Order;
using CampusTrade.API.Models.DTOs.Payment;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Data;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.Tests.UnitTests.Services
{
    /// <summary>
    /// 持续过期订单处理专项测试
    /// 测试系统在持续产生过期订单的情况下的处理能力
    /// </summary>
    public class ContinuousOrderExpiryTests : IDisposable
    {
        private readonly CampusTradeDbContext _context;
        private readonly ILogger<ContinuousOrderExpiryTests> _logger;

        public ContinuousOrderExpiryTests()
        {
            var options = new DbContextOptionsBuilder<CampusTradeDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new CampusTradeDbContext(options);

            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<ContinuousOrderExpiryTests>();

            InitializeTestData();
        }

        private void InitializeTestData()
        {
            // 创建测试用户
            var users = new[]
            {
                new User
                {
                    UserId = 1001,
                    Email = "buyer1@test.com",
                    Username = "TestBuyer1",
                    FullName = "测试买家1",
                    StudentId = "STU1001",
                    PasswordHash = "hash",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsActive = 1,
                    EmailVerified = 1,
                    CreditScore = 75.0m
                },
                new User
                {
                    UserId = 1002,
                    Email = "seller1@test.com",
                    Username = "TestSeller1",
                    FullName = "测试卖家1",
                    StudentId = "STU1002",
                    PasswordHash = "hash",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsActive = 1,
                    EmailVerified = 1,
                    CreditScore = 85.0m
                }
            };
            _context.Users.AddRange(users);

            // 创建测试分类和商品
            var category = new Category { CategoryId = 1, Name = "测试分类" };
            _context.Categories.Add(category);

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
        /// 测试大批量过期订单的处理性能
        /// </summary>
        [Fact]
        public async Task ProcessLargeBatchOfExpiredOrders_ShouldCompleteEfficiently()
        {
            // Arrange - 创建大量过期订单
            var batchSize = 50;
            var createdOrders = new List<int>();

            for (int i = 0; i < batchSize; i++)
            {
                var orderId = await CreateExpiredOrderAsync(i);
                createdOrders.Add(orderId);
            }

            _logger.LogInformation("创建了 {Count} 个过期订单", createdOrders.Count);

            // Act - 测量处理时间
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var now = DateTime.Now;
            var ordersToCancel = await _context.Orders
                .Where(o => o.Status == Order.OrderStatus.PendingPayment &&
                           o.ExpireTime.HasValue &&
                           o.ExpireTime.Value < now)
                .ToListAsync();

            foreach (var order in ordersToCancel)
            {
                order.Status = Order.OrderStatus.Cancelled;
            }

            var processedCount = await _context.SaveChangesAsync();
            stopwatch.Stop();

            // Assert
            Assert.Equal(batchSize, processedCount);
            Assert.True(stopwatch.ElapsedMilliseconds < 5000,
                $"处理 {batchSize} 个过期订单耗时 {stopwatch.ElapsedMilliseconds}ms，应该在5秒内完成");

            _logger.LogInformation("处理 {Count} 个过期订单耗时 {ElapsedMs}ms",
                processedCount, stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// 测试分批连续处理过期订单
        /// 模拟系统持续产生新的过期订单的场景
        /// </summary>
        [Fact]
        public async Task ProcessContinuousExpiredOrders_ShouldHandleMultipleBatches()
        {
            // Arrange
            var batchCount = 5;
            var ordersPerBatch = 10;
            var totalProcessed = 0;
            var processingTimes = new List<long>();

            // Act - 模拟分批处理过期订单
            for (int batch = 0; batch < batchCount; batch++)
            {
                // 1. 创建新的过期订单
                var batchOrders = new List<int>();
                for (int i = 0; i < ordersPerBatch; i++)
                {
                    var orderId = await CreateExpiredOrderAsync(batch * ordersPerBatch + i);
                    batchOrders.Add(orderId);
                }

                // 2. 处理这批过期订单
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var now = DateTime.Now;
                var ordersToCancel = await _context.Orders
                    .Where(o => batchOrders.Contains(o.OrderId) &&
                               o.Status == Order.OrderStatus.PendingPayment &&
                               o.ExpireTime.HasValue &&
                               o.ExpireTime.Value < now)
                    .ToListAsync();

                foreach (var order in ordersToCancel)
                {
                    order.Status = Order.OrderStatus.Cancelled;
                }

                var processedInBatch = await _context.SaveChangesAsync();
                stopwatch.Stop();

                totalProcessed += processedInBatch;
                processingTimes.Add(stopwatch.ElapsedMilliseconds);

                _logger.LogInformation("批次 {Batch}: 处理了 {Count} 个订单，耗时 {ElapsedMs}ms",
                    batch + 1, processedInBatch, stopwatch.ElapsedMilliseconds);

                // 3. 模拟间隔时间
                await Task.Delay(100); // 100ms间隔
            }

            // Assert
            Assert.Equal(batchCount * ordersPerBatch, totalProcessed);

            // 验证每批处理时间都在合理范围内
            var maxProcessingTime = processingTimes.Max();
            var avgProcessingTime = processingTimes.Average();

            Assert.True(maxProcessingTime < 2000,
                $"单批最大处理时间 {maxProcessingTime}ms 超过预期");
            Assert.True(avgProcessingTime < 1000,
                $"平均处理时间 {avgProcessingTime}ms 超过预期");

            _logger.LogInformation(
                "总处理订单数: {Total}, 平均处理时间: {AvgMs}ms, 最大处理时间: {MaxMs}ms",
                totalProcessed, avgProcessingTime, maxProcessingTime);
        }

        /// <summary>
        /// 测试混合场景：同时存在过期和未过期订单
        /// </summary>
        [Fact]
        public async Task ProcessMixedOrders_ShouldOnlyCancelExpiredOnes()
        {
            // Arrange - 创建混合订单
            var expiredCount = 15;
            var normalCount = 10;
            var expiredOrders = new List<int>();
            var normalOrders = new List<int>();

            // 创建已过期的订单
            for (int i = 0; i < expiredCount; i++)
            {
                var orderId = await CreateExpiredOrderAsync(i);
                expiredOrders.Add(orderId);
            }

            // 创建正常订单（未过期）
            for (int i = 0; i < normalCount; i++)
            {
                var orderId = await CreateNormalOrderAsync(i);
                normalOrders.Add(orderId);
            }

            // Act - 处理过期订单
            var now = DateTime.Now;
            var ordersToCancel = await _context.Orders
                .Where(o => o.Status == Order.OrderStatus.PendingPayment &&
                           o.ExpireTime.HasValue &&
                           o.ExpireTime.Value < now)
                .ToListAsync();

            foreach (var order in ordersToCancel)
            {
                order.Status = Order.OrderStatus.Cancelled;
            }

            var processedCount = await _context.SaveChangesAsync();

            // Assert
            Assert.Equal(expiredCount, processedCount);

            // 验证只有过期订单被取消
            var cancelledOrders = await _context.Orders
                .Where(o => o.Status == Order.OrderStatus.Cancelled)
                .Select(o => o.OrderId)
                .ToListAsync();

            var pendingOrders = await _context.Orders
                .Where(o => o.Status == Order.OrderStatus.PendingPayment)
                .Select(o => o.OrderId)
                .ToListAsync();

            Assert.Equal(expiredCount, cancelledOrders.Count);
            Assert.Equal(normalCount, pendingOrders.Count);

            // 验证正确的订单被取消/保留
            Assert.True(expiredOrders.All(id => cancelledOrders.Contains(id)),
                "所有过期订单都应该被取消");
            Assert.True(normalOrders.All(id => pendingOrders.Contains(id)),
                "所有正常订单都应该保持待付款状态");

            _logger.LogInformation(
                "混合处理完成: 取消了 {Cancelled} 个过期订单, 保留了 {Pending} 个正常订单",
                cancelledOrders.Count, pendingOrders.Count);
        }

        /// <summary>
        /// 测试高频过期订单检测
        /// 模拟高频检查过期订单的场景
        /// </summary>
        [Fact]
        public async Task HighFrequencyExpiryDetection_ShouldMaintainPerformance()
        {
            // Arrange - 创建测试订单
            var totalOrders = 100;
            var expiredRatio = 0.3; // 30%的订单已过期
            var expiredCount = (int)(totalOrders * expiredRatio);
            var detectionRounds = 10;

            // 创建混合订单
            for (int i = 0; i < expiredCount; i++)
            {
                await CreateExpiredOrderAsync(i);
            }

            for (int i = 0; i < totalOrders - expiredCount; i++)
            {
                await CreateNormalOrderAsync(i);
            }

            var detectionTimes = new List<long>();

            // Act - 多轮检测过期订单
            for (int round = 0; round < detectionRounds; round++)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var now = DateTime.Now;
                var expiringOrders = await _context.Orders
                    .Where(o => o.Status == Order.OrderStatus.PendingPayment &&
                               o.ExpireTime.HasValue &&
                               o.ExpireTime.Value < now.AddMinutes(30) && // 30分钟内过期
                               o.ExpireTime.Value > now) // 还没过期
                    .CountAsync();

                var expiredOrders = await _context.Orders
                    .Where(o => o.Status == Order.OrderStatus.PendingPayment &&
                               o.ExpireTime.HasValue &&
                               o.ExpireTime.Value < now)
                    .CountAsync();

                stopwatch.Stop();
                detectionTimes.Add(stopwatch.ElapsedMilliseconds);

                _logger.LogInformation(
                    "检测轮次 {Round}: 即将过期 {Expiring} 个, 已过期 {Expired} 个, 耗时 {ElapsedMs}ms",
                    round + 1, expiringOrders, expiredOrders, stopwatch.ElapsedMilliseconds);

                // 模拟检测间隔
                await Task.Delay(50);
            }

            // Assert
            var maxDetectionTime = detectionTimes.Max();
            var avgDetectionTime = detectionTimes.Average();

            Assert.True(maxDetectionTime < 500,
                $"单次检测最大耗时 {maxDetectionTime}ms 超过预期");
            Assert.True(avgDetectionTime < 200,
                $"平均检测耗时 {avgDetectionTime}ms 超过预期");

            _logger.LogInformation(
                "高频检测完成: {Rounds} 轮检测, 平均耗时 {AvgMs}ms, 最大耗时 {MaxMs}ms",
                detectionRounds, avgDetectionTime, maxDetectionTime);
        }

        #region 辅助方法

        /// <summary>
        /// 创建已过期的测试订单
        /// </summary>
        private async Task<int> CreateExpiredOrderAsync(int index)
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
                ExpireTime = DateTime.Now.AddMinutes(-10 - index) // 过期时间递增
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return order.OrderId;
        }

        /// <summary>
        /// 创建正常的测试订单（未过期）
        /// </summary>
        private async Task<int> CreateNormalOrderAsync(int index)
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
                CreateTime = DateTime.Now,
                ExpireTime = DateTime.Now.AddMinutes(30 + index) // 30分钟后过期，时间递增
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return order.OrderId;
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
