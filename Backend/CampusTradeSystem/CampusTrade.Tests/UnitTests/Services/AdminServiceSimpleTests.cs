using System.Threading.Tasks;
using CampusTrade.API.Models.DTOs.Admin;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Admin;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services
{
    /// <summary>
    /// 管理员服务单元测试
    /// </summary>
    public class AdminServiceTests
    {
        private readonly Mock<IAdminRepository> _mockAdminRepository;
        private readonly Mock<IAuditLogRepository> _mockAuditLogRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IReportsRepository> _mockReportsRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICategoriesRepository> _mockCategoriesRepository;
        private readonly Mock<ILogger<AdminService>> _mockLogger;
        private readonly AdminService _adminService;

        public AdminServiceTests()
        {
            _mockAdminRepository = new Mock<IAdminRepository>();
            _mockAuditLogRepository = new Mock<IAuditLogRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockReportsRepository = new Mock<IReportsRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCategoriesRepository = new Mock<ICategoriesRepository>();
            _mockLogger = new Mock<ILogger<AdminService>>();

            // 设置UnitOfWork返回模拟的Categories仓储
            _mockUnitOfWork.Setup(u => u.Categories).Returns(_mockCategoriesRepository.Object);

            _adminService = new AdminService(
                _mockAdminRepository.Object,
                _mockAuditLogRepository.Object,
                _mockUserRepository.Object,
                _mockReportsRepository.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task CreateAdminAsync_SystemAdminCreatesModuleAdmin_Success()
        {
            // Arrange
            var operatorAdminId = 1;
            var createDto = new CreateAdminDto
            {
                UserId = 123,
                Role = "category_admin",
                AssignedCategory = 2
            };

            var operatorAdmin = new Admin
            {
                AdminId = operatorAdminId,
                Role = Admin.Roles.Super
            };

            var user = new User
            {
                UserId = 123,
                Username = "testuser",
                Email = "test@example.com"
            };

            var category = new Category
            {
                CategoryId = 2,
                Name = "Electronics"
            };

            _mockAdminRepository.Setup(x => x.GetByPrimaryKeyAsync(operatorAdminId))
                .ReturnsAsync(operatorAdmin);
            _mockUserRepository.Setup(x => x.GetByPrimaryKeyAsync(createDto.UserId))
                .ReturnsAsync(user);
            _mockCategoriesRepository.Setup(x => x.GetByPrimaryKeyAsync(createDto.AssignedCategory.Value))
                .ReturnsAsync(category);
            _mockAdminRepository.Setup(x => x.IsCategoryAssignedAsync(It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync(false);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _adminService.CreateAdminAsync(createDto, operatorAdminId);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.AdminId);
            Assert.Contains("管理员创建成功", result.Message);
        }

        [Fact]
        public async Task CreateAdminAsync_NonSystemAdminTries_Failure()
        {
            // Arrange
            var operatorAdminId = 1;
            var createDto = new CreateAdminDto
            {
                UserId = 123,
                Role = "category_admin",
                AssignedCategory = 2
            };

            var operatorAdmin = new Admin
            {
                AdminId = operatorAdminId,
                Role = Admin.Roles.CategoryAdmin
            };

            _mockAdminRepository.Setup(x => x.GetByPrimaryKeyAsync(operatorAdminId))
                .ReturnsAsync(operatorAdmin);

            // Act
            var result = await _adminService.CreateAdminAsync(createDto, operatorAdminId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("只有系统管理员可以创建管理员", result.Message);
            Assert.Null(result.AdminId);
        }
    }
}
