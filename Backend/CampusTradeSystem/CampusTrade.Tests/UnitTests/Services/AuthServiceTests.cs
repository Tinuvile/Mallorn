using System.Linq.Expressions;
using CampusTrade.API.Models.DTOs.Auth;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Auth;
using CampusTrade.API.Services.Interfaces;
using CampusTrade.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services;

/// <summary>
/// AuthService单元测试
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IRepository<Student>> _mockStudentRepository;
    private readonly Mock<IUserCacheService> _mockUserCacheService;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // 初始化模拟对象
        _mockConfiguration = MockHelper.CreateMockConfiguration();
        _mockTokenService = new Mock<ITokenService>();
        _mockLogger = MockHelper.CreateMockLogger<AuthService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockStudentRepository = new Mock<IRepository<Student>>();
        _mockUserCacheService = new Mock<IUserCacheService>();

        // 配置UnitOfWork与仓储的关系
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.Students).Returns(_mockStudentRepository.Object);

        // 配置默认测试数据（基于TestDbContextFactory的测试对象）
        SetupDefaultTestData();

        // 初始化AuthService（依赖模拟的UnitOfWork）
        _authService = new AuthService(
            _mockUnitOfWork.Object,
            _mockConfiguration.Object,
            _mockTokenService.Object,
            _mockUserCacheService.Object
        );
    }

    /// <summary>
    /// 配置模拟仓储的默认返回值（使用TestDbContextFactory生成测试对象）
    /// </summary>
    private void SetupDefaultTestData()
    {
        // 测试用户和学生对象（复用TestDbContextFactory的方法）
        var activeUser = TestDbContextFactory.GetTestUser(1); // 活跃用户
        var inactiveUser = TestDbContextFactory.GetTestUser(2); // 禁用用户
        var validStudent = TestDbContextFactory.GetTestStudent("2025001"); // 有效学生
        var validStudent2 = TestDbContextFactory.GetTestStudent("2025003"); // 王五学生

        // 1. 用户仓储模拟配置
        // 按邮箱查询
        _mockUserRepository.Setup(r => r.GetByEmailAsync(activeUser.Email))
            .ReturnsAsync(activeUser);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(inactiveUser.Email))
            .ReturnsAsync(inactiveUser);
        _mockUserRepository.Setup(r => r.GetByEmailAsync("nonexistent@test.com"))
            .ReturnsAsync((User?)null);
        _mockUserRepository.Setup(r => r.GetByEmailAsync("wangwu@test.com"))
            .ReturnsAsync((User?)null);

        // 按学号查询
        _mockUserRepository.Setup(r => r.GetByStudentIdAsync(activeUser.StudentId))
            .ReturnsAsync(activeUser);
        _mockUserRepository.Setup(r => r.GetByStudentIdAsync("9999999"))
            .ReturnsAsync((User?)null);
        _mockUserRepository.Setup(r => r.GetByStudentIdAsync("2025003"))
            .ReturnsAsync((User?)null);

        // 按用户名查询（使用通用Mock）
        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((Expression<Func<User, bool>> expr) =>
            {
                var compiled = expr.Compile();
                var users = new[] { activeUser, inactiveUser };
                return users.FirstOrDefault(compiled);
            });

        // 带学生信息的用户查询
        _mockUserRepository.Setup(r => r.GetUserWithStudentAsync(activeUser.UserId))
            .ReturnsAsync(activeUser);
        _mockUserRepository.Setup(r => r.GetUserWithStudentAsync(inactiveUser.UserId))
            .ReturnsAsync(inactiveUser);

        // 检查用户是否存在的方法
        _mockUserRepository.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((Expression<Func<User, bool>> expr) =>
            {
                var compiled = expr.Compile();
                var users = new[] { activeUser, inactiveUser };
                return users.Any(compiled);
            });

        // 2. 学生仓储模拟配置
        _mockStudentRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync((Expression<Func<Student, bool>> expr) =>
            {
                // 简单模拟，基于学号
                if (expr.ToString().Contains("2025001"))
                    return validStudent;
                if (expr.ToString().Contains("2025003"))
                    return validStudent2;
                return null;
            });

        // 3. 配置通用模拟
        _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("TestSecretKeyForJwtThatIsLongEnough123456");
        _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        _mockConfiguration.Setup(c => c["Jwt:AccessTokenExpiryMinutes"]).Returns("30");

        // 4. UserCacheService模拟配置
        // 学生身份验证缓存
        _mockUserCacheService.Setup(c => c.ValidateStudentAsync("2025001", "张三"))
            .ReturnsAsync(true);
        _mockUserCacheService.Setup(c => c.ValidateStudentAsync("2025003", "王五"))
            .ReturnsAsync(true);
        _mockUserCacheService.Setup(c => c.ValidateStudentAsync("9999999", It.IsAny<string>()))
            .ReturnsAsync(false);
        _mockUserCacheService.Setup(c => c.ValidateStudentAsync(It.IsAny<string>(), "无效姓名"))
            .ReturnsAsync(false);

        // 用户名查询缓存
        _mockUserCacheService.Setup(c => c.GetUserByUsernameAsync(activeUser.Username))
            .ReturnsAsync(activeUser);
        _mockUserCacheService.Setup(c => c.GetUserByUsernameAsync("nonexistent_user"))
            .ReturnsAsync((User?)null);

        // 5. 通用仓储方法模拟
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u); // 添加用户时返回自身
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1); // 模拟保存成功
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync())
            .Returns(Task.CompletedTask); // 模拟事务提交成功
        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync())
            .Returns(Task.CompletedTask); // 模拟事务开始成功
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync())
            .Returns(Task.CompletedTask); // 模拟事务回滚成功
    }


    #region LoginWithTokenAsync Tests

    [Fact]
    public async Task LoginWithTokenAsync_WithValidCredentials_ShouldReturnTokenResponse()
    {
        // Arrange
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "zhangsan",
            Password = "Test123!",
            DeviceId = "test_device"
        };
        var testUser = TestDbContextFactory.GetTestUser(1);
        var expectedToken = JwtTestHelper.CreateTestTokenResponse(testUser);

        _mockTokenService.Setup(t => t.GenerateTokenResponseAsync(
            It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null
        )).ReturnsAsync(expectedToken);

        // Act
        var result = await _authService.LoginWithTokenAsync(loginRequest, "192.168.1.1", "Test Browser");

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(testUser.UserId);
        result.Username.Should().Be(testUser.Username);
        _mockTokenService.Verify(t => t.GenerateTokenResponseAsync(
            It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null
        ), Times.Once);
    }

    [Fact]
    public async Task LoginWithTokenAsync_WithEmailLogin_ShouldReturnTokenResponse()
    {
        // Arrange
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "zhangsan@test.com", // 邮箱登录
            Password = "Test123!",
            DeviceId = "test_device"
        };
        var testUser = TestDbContextFactory.GetTestUser(1);
        var expectedToken = JwtTestHelper.CreateTestTokenResponse(testUser);

        _mockTokenService.Setup(t => t.GenerateTokenResponseAsync(
            It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null
        )).ReturnsAsync(expectedToken);

        // Act
        var result = await _authService.LoginWithTokenAsync(loginRequest, "192.168.1.1", "Test Browser");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(testUser.Email);
    }

    [Fact]
    public async Task LoginWithTokenAsync_WithInvalidUsername_ShouldReturnNull()
    {
        // Arrange
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "nonexistent@test.com",
            Password = "Test123!",
            DeviceId = "test_device"
        };

        // Act
        var result = await _authService.LoginWithTokenAsync(loginRequest, "192.168.1.1", "Test Browser");

        // Assert
        result.Should().BeNull();
        _mockTokenService.Verify(t => t.GenerateTokenResponseAsync(
            It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null
        ), Times.Never);
    }

    [Fact]
    public async Task LoginWithTokenAsync_WithInvalidPassword_ShouldReturnNull()
    {
        // Arrange
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "zhangsan@test.com",
            Password = "WrongPassword", // 密码错误
            DeviceId = "test_device"
        };

        // Act
        var result = await _authService.LoginWithTokenAsync(loginRequest, "192.168.1.1", "Test Browser");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginWithTokenAsync_WithInactiveUser_ShouldReturnNull()
    {
        // Arrange
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "lisi@test.com", // 禁用用户
            Password = "Test123!",
            DeviceId = "test_device"
        };

        // Act
        var result = await _authService.LoginWithTokenAsync(loginRequest, "192.168.1.1", "Test Browser");

        // Assert
        result.Should().BeNull();
    }

    #endregion


    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldReturnUser()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            StudentId = "2025003",
            Name = "王五",
            Email = "wangwu@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!",
            Username = "wangwu"
        };

        // 模拟学生验证通过
        _mockStudentRepository.Setup(r => r.FirstOrDefaultAsync(It.Is<Expression<Func<Student, bool>>>(
            expr => expr.ToString().Contains("StudentId == \"2025003\"") && expr.ToString().Contains("Name == \"王五\""))
        )).ReturnsAsync(TestDbContextFactory.GetTestStudent("2025003"));

        // 模拟邮箱/用户名未被使用
        _mockUserRepository.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        result.Should().NotBeNull();
        result!.StudentId.Should().Be(registerDto.StudentId);
        result.Email.Should().Be(registerDto.Email);
        BCrypt.Net.BCrypt.Verify(registerDto.Password, result.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_WithExistingStudentId_ShouldThrowException()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            StudentId = "2025001", // 已注册学号
            Name = "张三",
            Email = "new@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!"
        };

        // 模拟学生验证通过但学号已存在
        _mockStudentRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(TestDbContextFactory.GetTestStudent("2025001"));
        _mockUserRepository.Setup(r => r.AnyAsync(It.Is<Expression<Func<User, bool>>>(
            expr => expr.ToString().Contains("StudentId == \"2025001\""))
        )).ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(registerDto));
        exception.Message.Should().Contain("该学号已被注册");
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowException()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            StudentId = "2025005",
            Name = "测试用户",
            Email = "zhangsan@test.com", // 已存在邮箱
            Password = "Test123!",
            ConfirmPassword = "Test123!"
        };

        // 模拟学生验证通过但邮箱已存在
        _mockStudentRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(TestDbContextFactory.GetTestStudent("2025005"));
        _mockUserRepository.Setup(r => r.AnyAsync(It.Is<Expression<Func<User, bool>>>(
            expr => expr.ToString().Contains("Email == \"zhangsan@test.com\""))
        )).ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(registerDto));
        exception.Message.Should().Contain("该邮箱已被注册");
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidStudent_ShouldThrowException()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            StudentId = "9999999", // 无效学号
            Name = "不存在",
            Email = "notexist@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!"
        };

        // 模拟学生验证失败
        _mockStudentRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync((Student?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(registerDto));
        exception.Message.Should().Contain("学生身份验证失败");
    }

    #endregion


    #region GetUserByUsernameAsync Tests

    [Fact]
    public async Task GetUserByUsernameAsync_WithValidUsername_ShouldReturnUser()
    {
        // Act
        var result = await _authService.GetUserByUsernameAsync("zhangsan");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("zhangsan");
    }

    [Fact]
    public async Task GetUserByUsernameAsync_WithValidEmail_ShouldReturnUser()
    {
        // Act
        var result = await _authService.GetUserByUsernameAsync("zhangsan@test.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("zhangsan@test.com");
    }

    [Fact]
    public async Task GetUserByUsernameAsync_WithInvalidUsername_ShouldReturnNull()
    {
        // Act
        var result = await _authService.GetUserByUsernameAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByUsernameAsync_WithInactiveUser_ShouldReturnNull()
    {
        // Act
        var result = await _authService.GetUserByUsernameAsync("lisi");

        // Assert
        result.Should().BeNull();
    }

    #endregion


    #region ValidateStudentAsync Tests

    [Fact]
    public async Task ValidateStudentAsync_WithValidStudent_ShouldReturnTrue()
    {
        // Act
        var result = await _authService.ValidateStudentAsync("2025001", "张三");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateStudentAsync_WithInvalidStudentId_ShouldReturnFalse()
    {
        // Act
        var result = await _authService.ValidateStudentAsync("9999999", "张三");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateStudentAsync_WithInvalidName_ShouldReturnFalse()
    {
        // Act
        var result = await _authService.ValidateStudentAsync("2025001", "错误姓名");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateStudentAsync_WithNullName_ShouldReturnFalse()
    {
        // Act
        var result = await _authService.ValidateStudentAsync("2025001", null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateStudentAsync_WithEmptyStudentId_ShouldReturnFalse()
    {
        // Act
        var result = await _authService.ValidateStudentAsync("", "张三");

        // Assert
        result.Should().BeFalse();
    }

    #endregion


    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var refreshToken = "test_refresh_token";
        _mockTokenService.Setup(t => t.RevokeRefreshTokenAsync(refreshToken, "用户注销", null))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.LogoutAsync(refreshToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var refreshToken = "invalid_token";
        _mockTokenService.Setup(t => t.RevokeRefreshTokenAsync(refreshToken, "用户注销", null))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.LogoutAsync(refreshToken);

        // Assert
        result.Should().BeFalse();
    }

    #endregion


    #region LogoutAllDevicesAsync Tests

    [Fact]
    public async Task LogoutAllDevicesAsync_WithValidUserId_ShouldReturnTrue()
    {
        // Arrange
        var userId = 1;
        _mockTokenService.Setup(t => t.RevokeAllUserTokensAsync(userId, "注销所有设备", null))
            .ReturnsAsync(2); // 成功撤销2个令牌

        // Act
        var result = await _authService.LogoutAllDevicesAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAllDevicesAsync_WithNoActiveTokens_ShouldReturnFalse()
    {
        // Arrange
        var userId = 999;
        _mockTokenService.Setup(t => t.RevokeAllUserTokensAsync(userId, "注销所有设备", null))
            .ReturnsAsync(0); // 无令牌可撤销

        // Act
        var result = await _authService.LogoutAllDevicesAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion


    #region RefreshTokenAsync Tests

    [Fact]
    public async Task RefreshTokenAsync_WithValidRequest_ShouldReturnTokenResponse()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "valid_token", DeviceId = "test_device" };
        var expectedToken = JwtTestHelper.CreateTestTokenResponse(TestDbContextFactory.GetTestUser(1));
        _mockTokenService.Setup(t => t.RefreshTokenAsync(request)).ReturnsAsync(expectedToken);

        // Act
        var result = await _authService.RefreshTokenAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(1);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidRequest_ShouldThrowException()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "invalid_token" };
        _mockTokenService.Setup(t => t.RefreshTokenAsync(request))
            .ThrowsAsync(new UnauthorizedAccessException("无效的刷新令牌"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.RefreshTokenAsync(request)
        );
        exception.Message.Should().Contain("无效的刷新令牌");
    }

    #endregion


    public void Dispose()
    {
        // 清理测试资源
        GC.SuppressFinalize(this);
    }
}
