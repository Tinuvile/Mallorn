using System.Threading.Tasks;
using CampusTrade.API.Models.DTOs.Admin;
using CampusTrade.API.Models.DTOs.Product;
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
        private readonly Mock<IProductRepository> _mockProductRepository;
        private readonly Mock<IRepository<Category>> _mockCategoryRepository;
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
            _mockProductRepository = new Mock<IProductRepository>();
            _mockCategoryRepository = new Mock<IRepository<Category>>();
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
                _mockProductRepository.Object,
                _mockCategoryRepository.Object,
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

        #region 商品CRUD权限测试

        [Fact]
        public async Task ValidateProductPermissionAsync_SuperAdmin_HasPermissionForAllProducts()
        {
            // Arrange
            var adminId = 1;
            var productId = 100;

            var admin = new Admin
            {
                AdminId = adminId,
                Role = Admin.Roles.Super
            };

            _mockAdminRepository.Setup(x => x.GetAdminWithDetailsAsync(adminId))
                .ReturnsAsync(admin);

            // Act
            var result = await _adminService.ValidateProductPermissionAsync(adminId, productId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateProductPermissionAsync_CategoryAdmin_HasPermissionForAssignedCategory()
        {
            // Arrange
            var adminId = 1;
            var productId = 100;
            var assignedCategoryId = 2;

            var admin = new Admin
            {
                AdminId = adminId,
                Role = Admin.Roles.CategoryAdmin,
                AssignedCategory = assignedCategoryId
            };

            var product = new Product
            {
                ProductId = productId,
                CategoryId = assignedCategoryId,
                Title = "Test Product",
                Status = Product.ProductStatus.OnSale
            };

            var category = new Category
            {
                CategoryId = assignedCategoryId,
                Name = "Electronics",
                ParentId = null
            };

            var allCategories = new List<Category> { category };

            _mockAdminRepository.Setup(x => x.GetAdminWithDetailsAsync(adminId))
                .ReturnsAsync(admin);
            _mockProductRepository.Setup(x => x.GetByPrimaryKeyAsync(productId))
                .ReturnsAsync(product);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(allCategories);

            // Act
            var result = await _adminService.ValidateProductPermissionAsync(adminId, productId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateProductPermissionAsync_CategoryAdmin_NoPermissionForOtherCategory()
        {
            // Arrange
            var adminId = 1;
            var productId = 100;
            var assignedCategoryId = 2;
            var productCategoryId = 3;

            var admin = new Admin
            {
                AdminId = adminId,
                Role = Admin.Roles.CategoryAdmin,
                AssignedCategory = assignedCategoryId
            };

            var product = new Product
            {
                ProductId = productId,
                CategoryId = productCategoryId,
                Title = "Test Product",
                Status = Product.ProductStatus.OnSale
            };

            var assignedCategory = new Category
            {
                CategoryId = assignedCategoryId,
                Name = "Electronics",
                ParentId = null
            };

            var productCategory = new Category
            {
                CategoryId = productCategoryId,
                Name = "Books",
                ParentId = null
            };

            var allCategories = new List<Category> { assignedCategory, productCategory };

            _mockAdminRepository.Setup(x => x.GetAdminWithDetailsAsync(adminId))
                .ReturnsAsync(admin);
            _mockProductRepository.Setup(x => x.GetByPrimaryKeyAsync(productId))
                .ReturnsAsync(product);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(allCategories);

            // Act
            var result = await _adminService.ValidateProductPermissionAsync(adminId, productId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateProductPermissionAsync_CategoryAdmin_HasPermissionForSubCategory()
        {
            // Arrange
            var adminId = 1;
            var productId = 100;
            var assignedCategoryId = 2; // 管理员分配的根分类
            var subCategoryId = 5; // 子分类

            var admin = new Admin
            {
                AdminId = adminId,
                Role = Admin.Roles.CategoryAdmin,
                AssignedCategory = assignedCategoryId
            };

            var product = new Product
            {
                ProductId = productId,
                CategoryId = subCategoryId, // 商品在子分类中
                Title = "Test Product",
                Status = Product.ProductStatus.OnSale
            };

            var assignedCategory = new Category
            {
                CategoryId = assignedCategoryId,
                Name = "Electronics", // 根分类
                ParentId = null
            };

            var subCategory = new Category
            {
                CategoryId = subCategoryId,
                Name = "Smartphones", // 子分类
                ParentId = assignedCategoryId
            };

            var allCategories = new List<Category> { assignedCategory, subCategory };

            _mockAdminRepository.Setup(x => x.GetAdminWithDetailsAsync(adminId))
                .ReturnsAsync(admin);
            _mockProductRepository.Setup(x => x.GetByPrimaryKeyAsync(productId))
                .ReturnsAsync(product);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(allCategories);

            // Act
            var result = await _adminService.ValidateProductPermissionAsync(adminId, productId);

            // Assert
            Assert.True(result); // 因为商品的根分类是管理员分配的分类
        }

        [Fact]
        public async Task ValidateProductPermissionAsync_CategoryAdmin_HasPermissionForDeepNestedCategory()
        {
            // Arrange
            var adminId = 1;
            var productId = 100;
            var rootCategoryId = 1; // 管理员分配的根分类
            var level2CategoryId = 3; // 二级分类
            var level3CategoryId = 6; // 三级分类

            var admin = new Admin
            {
                AdminId = adminId,
                Role = Admin.Roles.CategoryAdmin,
                AssignedCategory = rootCategoryId
            };

            var product = new Product
            {
                ProductId = productId,
                CategoryId = level3CategoryId, // 商品在三级分类中
                Title = "Deep Nested Product",
                Status = Product.ProductStatus.OnSale
            };

            var rootCategory = new Category
            {
                CategoryId = rootCategoryId,
                Name = "Electronics", // 根分类
                ParentId = null
            };

            var level2Category = new Category
            {
                CategoryId = level2CategoryId,
                Name = "Mobile Devices", // 二级分类
                ParentId = rootCategoryId
            };

            var level3Category = new Category
            {
                CategoryId = level3CategoryId,
                Name = "Smartphone Cases", // 三级分类
                ParentId = level2CategoryId
            };

            var allCategories = new List<Category> { rootCategory, level2Category, level3Category };

            _mockAdminRepository.Setup(x => x.GetAdminWithDetailsAsync(adminId))
                .ReturnsAsync(admin);
            _mockProductRepository.Setup(x => x.GetByPrimaryKeyAsync(productId))
                .ReturnsAsync(product);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(allCategories);

            // Act
            var result = await _adminService.ValidateProductPermissionAsync(adminId, productId);

            // Assert
            Assert.True(result); // 因为商品的根分类是管理员分配的分类
        }

        [Fact]
        public async Task UpdateProductAsAdminAsync_SuperAdmin_Success()
        {
            // Arrange
            var adminId = 1;
            var productId = 100;
            var updateDto = new AdminUpdateProductDto
            {
                Title = "Updated Product Title",
                Status = "已下架",
                AdminNote = "违规内容"
            };

            var admin = new Admin
            {
                AdminId = adminId,
                Role = Admin.Roles.Super,
                UserId = 1
            };

            var product = new Product
            {
                ProductId = productId,
                CategoryId = 2,
                Title = "Original Title",
                Status = Product.ProductStatus.OnSale,
                UserId = 10
            };

            _mockAdminRepository.Setup(x => x.GetAdminWithDetailsAsync(adminId))
                .ReturnsAsync(admin);
            _mockProductRepository.Setup(x => x.GetByPrimaryKeyAsync(productId))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _adminService.UpdateProductAsAdminAsync(adminId, productId, updateDto);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("商品信息更新成功", result.Message);
        }

        [Fact]
        public async Task UpdateProductAsAdminAsync_CategoryAdminWithoutPermission_Failure()
        {
            // Arrange
            var adminId = 1;
            var productId = 100;
            var updateDto = new AdminUpdateProductDto
            {
                Title = "Updated Product Title"
            };

            var admin = new Admin
            {
                AdminId = adminId,
                Role = Admin.Roles.CategoryAdmin,
                AssignedCategory = 2
            };

            var product = new Product
            {
                ProductId = productId,
                CategoryId = 3, // 不同的分类
                Title = "Original Title",
                Status = Product.ProductStatus.OnSale
            };

            var assignedCategory = new Category
            {
                CategoryId = 2,
                Name = "Electronics",
                ParentId = null
            };

            var productCategory = new Category
            {
                CategoryId = 3,
                Name = "Books",
                ParentId = null
            };

            var allCategories = new List<Category> { assignedCategory, productCategory };

            _mockAdminRepository.Setup(x => x.GetAdminWithDetailsAsync(adminId))
                .ReturnsAsync(admin);
            _mockProductRepository.Setup(x => x.GetByPrimaryKeyAsync(productId))
                .ReturnsAsync(product);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(allCategories);

            // Act
            var result = await _adminService.UpdateProductAsAdminAsync(adminId, productId, updateDto);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("无权限操作此商品", result.Message);
        }

        [Fact]
        public async Task DeleteProductAsAdminAsync_SuperAdmin_Success()
        {
            // Arrange
            var adminId = 1;
            var productId = 100;
            var reason = "违规商品";

            var admin = new Admin
            {
                AdminId = adminId,
                Role = Admin.Roles.Super,
                UserId = 1
            };

            var product = new Product
            {
                ProductId = productId,
                CategoryId = 2,
                Title = "Test Product",
                Status = Product.ProductStatus.OnSale,
                UserId = 10
            };

            _mockAdminRepository.Setup(x => x.GetAdminWithDetailsAsync(adminId))
                .ReturnsAsync(admin);
            _mockProductRepository.Setup(x => x.GetByPrimaryKeyAsync(productId))
                .ReturnsAsync(product);
            _mockProductRepository.Setup(x => x.DeleteProductAsync(productId))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _adminService.DeleteProductAsAdminAsync(adminId, productId, reason);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("商品删除成功", result.Message);
        }

        [Fact]
        public async Task GetManagedCategoryIdsAsync_SuperAdmin_ReturnsAllCategories()
        {
            // Arrange
            var adminId = 1;
            var admin = new Admin
            {
                AdminId = adminId,
                Role = Admin.Roles.Super
            };

            var allCategories = new List<Category>
            {
                new Category { CategoryId = 1, Name = "Electronics" },
                new Category { CategoryId = 2, Name = "Books" },
                new Category { CategoryId = 3, Name = "Clothing" }
            };

            _mockAdminRepository.Setup(x => x.GetAdminWithDetailsAsync(adminId))
                .ReturnsAsync(admin);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(allCategories);

            // Act
            var result = await _adminService.GetManagedCategoryIdsAsync(adminId);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(1, result);
            Assert.Contains(2, result);
            Assert.Contains(3, result);
        }

        [Fact]
        public async Task GetManagedCategoryIdsAsync_CategoryAdmin_ReturnsAssignedAndSubCategories()
        {
            // Arrange
            var adminId = 1;
            var assignedCategoryId = 1;
            var admin = new Admin
            {
                AdminId = adminId,
                Role = Admin.Roles.CategoryAdmin,
                AssignedCategory = assignedCategoryId
            };

            var categories = new List<Category>
            {
                new Category { CategoryId = 1, Name = "Electronics", ParentId = null },
                new Category { CategoryId = 2, Name = "Smartphones", ParentId = 1 },
                new Category { CategoryId = 3, Name = "Laptops", ParentId = 1 },
                new Category { CategoryId = 4, Name = "Books", ParentId = null }
            };

            _mockAdminRepository.Setup(x => x.GetAdminWithDetailsAsync(adminId))
                .ReturnsAsync(admin);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await _adminService.GetManagedCategoryIdsAsync(adminId);

            // Assert
            Assert.Equal(3, result.Count); // 包含主分类和2个子分类
            Assert.Contains(1, result); // 主分类
            Assert.Contains(2, result); // 子分类
            Assert.Contains(3, result); // 子分类
            Assert.DoesNotContain(4, result); // 不相关的分类
        }

        #endregion
    }
}
