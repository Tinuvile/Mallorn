using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Repositories
{
    /// <summary>
    /// ReportsRepository中商品分类相关功能的单元测试
    /// </summary>
    public class ReportsRepositoryProductCategoryTests : IDisposable
    {
        private readonly CampusTradeDbContext _context;
        private readonly ReportsRepository _repository;

        public ReportsRepositoryProductCategoryTests()
        {
            var options = new DbContextOptionsBuilder<CampusTradeDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new CampusTradeDbContext(options);
            _repository = new ReportsRepository(_context);
        }

        [Fact]
        public async Task GetReportProductPrimaryCategoryAsync_ValidReportWithNestedCategory_ReturnsPrimaryCategory()
        {
            // Arrange
            // 创建分类层次结构：电子产品 -> 手机 -> 智能手机
            var primaryCategory = new Category
            {
                CategoryId = 1,
                Name = "电子产品",
                ParentId = null
            };

            var secondaryCategory = new Category
            {
                CategoryId = 2,
                Name = "手机",
                ParentId = 1,
                Parent = primaryCategory
            };

            var tertiaryCategory = new Category
            {
                CategoryId = 3,
                Name = "智能手机",
                ParentId = 2,
                Parent = secondaryCategory
            };

            // 创建用户
            var user = new User
            {
                UserId = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
                StudentId = "2023001",
                CreditScore = 60.0m,
                IsActive = 1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // 创建商品
            var product = new Product
            {
                ProductId = 1,
                UserId = 1,
                CategoryId = 3, // 使用三级分类
                Title = "iPhone 14",
                Description = "最新款iPhone",
                BasePrice = 5999.00m,
                Status = "在售",
                Category = tertiaryCategory
            };

            // 创建抽象订单
            var abstractOrder = new AbstractOrder
            {
                AbstractOrderId = 1,
                OrderType = "normal"
            };

            // 创建订单
            var order = new Order
            {
                OrderId = 1,
                BuyerId = 1,
                SellerId = 2,
                ProductId = 1,
                TotalAmount = 5999.00m,
                Status = "已完成",
                Product = product,
                AbstractOrder = abstractOrder
            };

            // 创建举报
            var report = new Reports
            {
                ReportId = 1,
                OrderId = 1,
                ReporterId = 1,
                Type = "商品问题",
                Status = "待处理",
                Priority = 5,
                AbstractOrder = abstractOrder
            };

            // 设置关联关系
            abstractOrder.Order = order;

            // 添加到数据库
            await _context.Categories.AddRangeAsync(primaryCategory, secondaryCategory, tertiaryCategory);
            await _context.Users.AddAsync(user);
            await _context.Products.AddAsync(product);
            await _context.AbstractOrders.AddAsync(abstractOrder);
            await _context.Orders.AddAsync(order);
            await _context.Reports.AddAsync(report);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetReportProductPrimaryCategoryAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.CategoryId);
            Assert.Equal("电子产品", result.Name);
            Assert.Null(result.ParentId);
        }

        [Fact]
        public async Task GetReportProductPrimaryCategoryAsync_ReportWithPrimaryCategory_ReturnsSameCategory()
        {
            // Arrange
            // 创建只有一级分类的情况
            var primaryCategory = new Category
            {
                CategoryId = 1,
                Name = "图书",
                ParentId = null
            };

            var user = new User
            {
                UserId = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
                StudentId = "2023001",
                CreditScore = 60.0m,
                IsActive = 1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var product = new Product
            {
                ProductId = 1,
                UserId = 1,
                CategoryId = 1, // 直接使用一级分类
                Title = "数据结构与算法",
                Description = "计算机科学经典教材",
                BasePrice = 89.00m,
                Status = "在售",
                Category = primaryCategory
            };

            var abstractOrder = new AbstractOrder
            {
                AbstractOrderId = 1,
                OrderType = "normal"
            };

            var order = new Order
            {
                OrderId = 1,
                BuyerId = 1,
                SellerId = 2,
                ProductId = 1,
                TotalAmount = 89.00m,
                Status = "已完成",
                Product = product,
                AbstractOrder = abstractOrder
            };

            var report = new Reports
            {
                ReportId = 1,
                OrderId = 1,
                ReporterId = 1,
                Type = "商品问题",
                Status = "待处理",
                Priority = 5,
                AbstractOrder = abstractOrder
            };

            abstractOrder.Order = order;

            await _context.Categories.AddAsync(primaryCategory);
            await _context.Users.AddAsync(user);
            await _context.Products.AddAsync(product);
            await _context.AbstractOrders.AddAsync(abstractOrder);
            await _context.Orders.AddAsync(order);
            await _context.Reports.AddAsync(report);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetReportProductPrimaryCategoryAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.CategoryId);
            Assert.Equal("图书", result.Name);
            Assert.Null(result.ParentId);
        }

        [Fact]
        public async Task GetReportProductPrimaryCategoryAsync_NonExistentReport_ReturnsNull()
        {
            // Act
            var result = await _repository.GetReportProductPrimaryCategoryAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetReportProductPrimaryCategoryAsync_ReportWithoutProduct_ReturnsNull()
        {
            // Arrange
            // 创建一个没有关联产品的举报（可能是换物请求或其他类型）
            var abstractOrder = new AbstractOrder
            {
                AbstractOrderId = 1,
                OrderType = "exchange" // 换物请求，没有关联商品
            };

            var report = new Reports
            {
                ReportId = 1,
                OrderId = 1,
                ReporterId = 1,
                Type = "其他",
                Status = "待处理",
                Priority = 3,
                AbstractOrder = abstractOrder
            };

            await _context.AbstractOrders.AddAsync(abstractOrder);
            await _context.Reports.AddAsync(report);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetReportProductPrimaryCategoryAsync(1);

            // Assert
            Assert.Null(result);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
