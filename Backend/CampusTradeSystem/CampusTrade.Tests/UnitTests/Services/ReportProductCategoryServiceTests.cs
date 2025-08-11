using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Interfaces;
using CampusTrade.API.Services.Report;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services
{
    /// <summary>
    /// ReportService中获取商品分类相关功能的单元测试
    /// </summary>
    public class ReportProductCategoryServiceTests
    {
        private readonly Mock<IReportsRepository> _mockReportsRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ReportService>> _mockLogger;
        private readonly ReportService _reportService;

        public ReportProductCategoryServiceTests()
        {
            _mockReportsRepository = new Mock<IReportsRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ReportService>>();

            _reportService = new ReportService(
                _mockReportsRepository.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetReportProductPrimaryCategoryAsync_ValidReportId_ReturnsCategory()
        {
            // Arrange
            var reportId = 1;
            var requestUserId = 100;
            var reporterId = 100;

            var primaryCategory = new Category
            {
                CategoryId = 1,
                Name = "电子产品",
                ParentId = null
            };

            var report = new Reports
            {
                ReportId = reportId,
                ReporterId = reporterId,
                OrderId = 1,
                Type = "商品问题",
                Status = "待处理"
            };

            _mockReportsRepository.Setup(r => r.GetByPrimaryKeyAsync(reportId))
                .ReturnsAsync(report);

            _mockReportsRepository.Setup(r => r.GetReportProductPrimaryCategoryAsync(reportId))
                .ReturnsAsync(primaryCategory);

            // Act
            var result = await _reportService.GetReportProductPrimaryCategoryAsync(reportId, requestUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(primaryCategory.CategoryId, result.CategoryId);
            Assert.Equal(primaryCategory.Name, result.Name);
            Assert.Null(result.ParentId);
        }

        [Fact]
        public async Task GetReportProductPrimaryCategoryAsync_NonExistentReport_ReturnsNull()
        {
            // Arrange
            var reportId = 999;
            var requestUserId = 100;

            _mockReportsRepository.Setup(r => r.GetByPrimaryKeyAsync(reportId))
                .ReturnsAsync((Reports?)null);

            // Act
            var result = await _reportService.GetReportProductPrimaryCategoryAsync(reportId, requestUserId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetReportProductPrimaryCategoryAsync_UnauthorizedUser_ReturnsNull()
        {
            // Arrange
            var reportId = 1;
            var requestUserId = 200; // 不同的用户ID
            var reporterId = 100;

            var report = new Reports
            {
                ReportId = reportId,
                ReporterId = reporterId,
                OrderId = 1,
                Type = "商品问题",
                Status = "待处理"
            };

            _mockReportsRepository.Setup(r => r.GetByPrimaryKeyAsync(reportId))
                .ReturnsAsync(report);

            // Act
            var result = await _reportService.GetReportProductPrimaryCategoryAsync(reportId, requestUserId);

            // Assert
            Assert.Null(result);

            // Verify that GetReportProductPrimaryCategoryAsync was not called due to authorization failure
            _mockReportsRepository.Verify(r => r.GetReportProductPrimaryCategoryAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetReportProductPrimaryCategoryAsync_ExceptionThrown_ReturnsNull()
        {
            // Arrange
            var reportId = 1;
            var requestUserId = 100;

            _mockReportsRepository.Setup(r => r.GetByPrimaryKeyAsync(reportId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _reportService.GetReportProductPrimaryCategoryAsync(reportId, requestUserId);

            // Assert
            Assert.Null(result);
        }
    }
}
