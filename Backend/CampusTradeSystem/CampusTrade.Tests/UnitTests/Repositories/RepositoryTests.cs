using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Implementations;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Repositories
{
    public class RepositoryTests : IDisposable
    {
        private readonly CampusTradeDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TestDataSeeder _seeder;

        public RepositoryTests()
        {
            var databaseName = $"TestDb_{Guid.NewGuid()}";
            _context = TestDbContextFactory.CreateInMemoryDbContext(databaseName);
            _unitOfWork = new UnitOfWork(_context);
            _seeder = new TestDataSeeder(_context);
        }

        public void Dispose()
        {
            _unitOfWork.Dispose();
            _context.Dispose();
        }

        #region 交易业务测试
        [Fact]
        public async Task CreateOrder_WithNegotiation_CompletesSuccessfully()
        {
            // Arrange
            await _seeder.SeedOrderTestDataAsync();
            var buyer = await _context.Users.FirstOrDefaultAsync(u => u.Username == "buyer");
            var seller = await _context.Users.FirstOrDefaultAsync(u => u.Username == "seller");
            var product = await _context.Products.FirstOrDefaultAsync();
            Assert.NotNull(buyer);
            Assert.NotNull(seller);
            Assert.NotNull(product);

            // 1. 创建订单
            var order = new Order
            {
                BuyerId = buyer.UserId,
                SellerId = seller.UserId,
                ProductId = product.ProductId,
                Status = Order.OrderStatus.PendingPayment,
                CreateTime = DateTime.UtcNow
            };
            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            // 2. 创建议价
            var negotiation = new Negotiation
            {
                OrderId = order.OrderId,
                ProposedPrice = 80.0m,
                Status = "等待回应",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Negotiations.AddAsync(negotiation);
            await _unitOfWork.SaveChangesAsync();

            // 3. 更新订单金额
            order.TotalAmount = negotiation.ProposedPrice;
            _unitOfWork.Orders.Update(order);
            await _unitOfWork.SaveChangesAsync();

            // 验证
            var savedOrder = await _unitOfWork.Orders.GetByPrimaryKeyAsync(order.OrderId);
            Assert.NotNull(savedOrder);
            Assert.Equal(negotiation.ProposedPrice, savedOrder.TotalAmount);
        }
        #endregion

        #region 用户账户测试
        [Fact]
        public async Task VirtualAccount_Credit_UpdatesBalanceCorrectly()
        {
            await _seeder.SeedVirtualAccountDataAsync();
            var creditResult = await _unitOfWork.VirtualAccounts.CreditAsync(1, 50.0m, "测试充值");
            Assert.True(creditResult);
            var account = await _unitOfWork.VirtualAccounts.GetByUserIdAsync(1);
            Assert.NotNull(account);
            Assert.Equal(150.0m, account.Balance);
        }
        #endregion

        #region 商品管理测试
        [Fact]
        public async Task Products_Search_ReturnsCorrectResults()
        {
            // Arrange
            await _seeder.SeedProductDataAsync();
            var repository = _unitOfWork.Products;

            // Act
            var result = await repository.GetByTitleAsync("测试");
            var products = result.Products;
            var total = result.TotalCount;

            // Assert
            Assert.NotEmpty(products);
            Assert.True(total > 0);
            Assert.All(products, p => Assert.Contains("测试", p.Title));
        }
        #endregion

        #region 用户唯一性测试
        [Fact]
        public async Task AddUser_DuplicateEmail_ShouldFail()
        {
            var user1 = new User { Email = "dup@test.com", Username = "user1" };
            var user2 = new User { Email = "dup@test.com", Username = "user2" };
            await _unitOfWork.Users.AddAsync(user1);
            await _unitOfWork.SaveChangesAsync();
            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await _unitOfWork.Users.AddAsync(user2);
                await _unitOfWork.SaveChangesAsync();
            });
        }
        #endregion

        #region 商品上下架与状态变更
        [Fact]
        public async Task Product_SetStatus_ChangesStatus()
        {
            var product = new Product { Title = "A", Status = Product.ProductStatus.OnSale };
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.Products.SetProductStatusAsync(product.ProductId, Product.ProductStatus.OffShelf);
            await _unitOfWork.SaveChangesAsync();
            var updated = await _unitOfWork.Products.GetByPrimaryKeyAsync(product.ProductId);
            Assert.NotNull(updated);
            Assert.Equal(Product.ProductStatus.OffShelf, updated.Status);
        }
        #endregion

        #region 通知发送与重试
        [Fact]
        public async Task Notification_MarkSendStatus_Works()
        {
            var notification = new Notification { RecipientId = 1, SendStatus = Notification.SendStatuses.Pending };
            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.Notifications.MarkSendStatusAsync(notification.NotificationId, Notification.SendStatuses.Success);
            await _unitOfWork.SaveChangesAsync();
            var updated = await _unitOfWork.Notifications.GetByPrimaryKeyAsync(notification.NotificationId);
            Assert.NotNull(updated);
            Assert.Equal(Notification.SendStatuses.Success, updated.SendStatus);
        }
        #endregion

        #region 充值与余额
        [Fact]
        public async Task RechargeRecord_UpdateStatus_ChangesStatus()
        {
            var record = new RechargeRecord { UserId = 1, Amount = 100, Status = "待处理" };
            await _unitOfWork.RechargeRecords.AddAsync(record);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.RechargeRecords.UpdateRechargeStatusAsync(record.RechargeId, "已完成", DateTime.UtcNow);
            await _unitOfWork.SaveChangesAsync();
            var updated = await _unitOfWork.RechargeRecords.GetByPrimaryKeyAsync(record.RechargeId);
            Assert.NotNull(updated);
            Assert.Equal("已完成", updated.Status);
        }
        #endregion

        #region 举报处理
        [Fact]
        public async Task Reports_BulkUpdateStatus_Works()
        {
            var report1 = new Reports { ReporterId = 1, Status = "待处理" };
            var report2 = new Reports { ReporterId = 1, Status = "待处理" };
            await _unitOfWork.Reports.AddAsync(report1);
            await _unitOfWork.Reports.AddAsync(report2);
            await _unitOfWork.SaveChangesAsync();
            var ids = new List<int> { report1.ReportId, report2.ReportId };
            await _unitOfWork.Reports.BulkUpdateReportStatusAsync(ids, "已处理");
            await _unitOfWork.SaveChangesAsync();
            var updated1 = await _unitOfWork.Reports.GetByPrimaryKeyAsync(report1.ReportId);
            var updated2 = await _unitOfWork.Reports.GetByPrimaryKeyAsync(report2.ReportId);
            Assert.NotNull(updated1);
            Assert.NotNull(updated2);
            Assert.Equal("已处理", updated1.Status);
            Assert.Equal("已处理", updated2.Status);
        }
        #endregion

        #region 评价与评分
        [Fact]
        public async Task Review_AddAndGetProductAverageRating_Works()
        {
            // Arrange: 创建商品、订单、抽象订单
            var product = new Product { Title = "评分商品", Status = Product.ProductStatus.OnSale };
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();
            var buyer = new User { Email = "buyer@rating.com", Username = "buyer" };
            var seller = new User { Email = "seller@rating.com", Username = "seller" };
            await _unitOfWork.Users.AddAsync(buyer);
            await _unitOfWork.Users.AddAsync(seller);
            await _unitOfWork.SaveChangesAsync();
            var order = new Order
            {
                BuyerId = buyer.UserId,
                SellerId = seller.UserId,
                ProductId = product.ProductId,
                Status = Order.OrderStatus.Completed,
                CreateTime = DateTime.UtcNow
            };
            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();
            var review1 = new Review { OrderId = order.OrderId, Rating = 5 };
            var review2 = new Review { OrderId = order.OrderId, Rating = 3 };
            await _unitOfWork.Reviews.AddAsync(review1);
            await _unitOfWork.Reviews.AddAsync(review2);
            await _unitOfWork.SaveChangesAsync();
            var avg = await _unitOfWork.Reviews.GetProductAverageRatingAsync(product.ProductId);
            Assert.Equal(4, avg);
        }
        #endregion

        #region TestDataSeeder

        private class TestDataSeeder
        {
            private readonly CampusTradeDbContext _context;

            public TestDataSeeder(CampusTradeDbContext context)
            {
                _context = context;
            }

            public async Task SeedOrderTestDataAsync()
            {
                if (!await _context.Orders.AnyAsync())
                {
                    // 添加测试用户
                    var buyer = new User { StudentId = "BUYER001", Username = "buyer" };
                    var seller = new User { StudentId = "SELLER001", Username = "seller" };
                    _context.Users.AddRange(buyer, seller);
                    await _context.SaveChangesAsync();

                    // 添加测试商品
                    var product = new Product
                    {
                        Title = "测试商品",
                        UserId = seller.UserId,
                        BasePrice = 100.0m
                    };
                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();
                }
            }

            public async Task SeedVirtualAccountDataAsync()
            {
                if (!await _context.VirtualAccounts.AnyAsync())
                {
                    var accounts = new List<VirtualAccount>
                    {
                        new VirtualAccount { UserId = 1, Balance = 100.0m },
                        new VirtualAccount { UserId = 2, Balance = 100.0m }
                    };
                    _context.VirtualAccounts.AddRange(accounts);
                    await _context.SaveChangesAsync();
                }
            }

            public async Task SeedProductDataAsync()
            {
                // 添加测试数据
                if (!await _context.Products.AnyAsync())
                {
                    var category = new Category { Name = "测试分类" };
                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();

                    var user = new User
                    {
                        StudentId = "TEST001",
                        Username = "testuser",
                        Email = "test@example.com"
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    var products = Enumerable.Range(1, 5).Select(i => new Product
                    {
                        Title = $"测试商品{i}",
                        Description = $"测试描述{i}",
                        BasePrice = i * 100.0m,
                        CategoryId = category.CategoryId,
                        UserId = user.UserId,
                        Status = "在售"
                    });

                    _context.Products.AddRange(products);
                    await _context.SaveChangesAsync();
                }
            }
        }

        #endregion
    }
}
