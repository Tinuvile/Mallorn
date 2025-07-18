using System.Linq.Expressions;
using CampusTrade.API.Models.DTOs.Auth;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Auth;
using CampusTrade.Tests.Helpers;
using FluentAssertions;
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
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockConfiguration = MockHelper.CreateMockConfiguration();
        _mockTokenService = new Mock<ITokenService>();
        _mockLogger = MockHelper.CreateMockLogger<AuthService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockStudentRepository = new Mock<IRepository<Student>>();

        // 设置UnitOfWork返回Mock repositories
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.Students).Returns(_mockStudentRepository.Object);

        // 设置默认的测试用户数据
        SetupDefaultTestData();

        _authService = new AuthService(_mockUnitOfWork.Object, _mockConfiguration.Object, _mockTokenService.Object, _mockLogger.Object);
    }

    private void SetupDefaultTestData()
    {
        var testUser = TestDbContextFactory.GetTestUser(1);
        var testStudent = TestDbContextFactory.GetTestStudent("2025001");

        // 设置用户查询Mock
        _mockUserRepository.Setup(r => r.GetByEmailAsync("zhangsan@test.com"))
            .ReturnsAsync(testUser);
        _mockUserRepository.Setup(r => r.GetByStudentIdAsync("2025001"))
            .ReturnsAsync(testUser);

        // 设置不存在的用户
        _mockUserRepository.Setup(r => r.GetByEmailAsync("nonexistent@test.com"))
            .ReturnsAsync((User?)null);
        _mockUserRepository.Setup(r => r.GetByStudentIdAsync("nonexistent"))
            .ReturnsAsync((User?)null);

        // 设置禁用用户
        var inactiveUser = TestDbContextFactory.GetTestUser(2);
        inactiveUser.IsActive = 0;
        _mockUserRepository.Setup(r => r.GetByEmailAsync("lisi@test.com"))
            .ReturnsAsync(inactiveUser);

        // 设置学生查询Mock
        _mockStudentRepository.Setup(r => r.FirstOrDefaultAsync(s => s.StudentId == "2025001"))
            .ReturnsAsync(testStudent);
        _mockStudentRepository.Setup(r => r.FirstOrDefaultAsync(s => s.StudentId == "2025001" && s.Name == "张三"))
            .ReturnsAsync(testStudent);

        // 设置不存在的学生
        _mockStudentRepository.Setup(r => r.FirstOrDefaultAsync(s => s.StudentId == "9999999"))
            .ReturnsAsync((Student?)null);
        _mockStudentRepository.Setup(r => r.FirstOrDefaultAsync(s => s.StudentId == "9999999" && s.Name == "不存在"))
            .ReturnsAsync((Student?)null);

        // 设置学生身份验证Mock
        _mockStudentRepository.Setup(r => r.FirstOrDefaultAsync(s => s.StudentId == "2025001" && s.Name == "张三"))
            .ReturnsAsync(testStudent);
        _mockStudentRepository.Setup(r => r.FirstOrDefaultAsync(s => s.StudentId == "2025001" && s.Name == "错误姓名"))
            .ReturnsAsync((Student?)null);

        // 设置邮箱和学号存在性检查
        _mockUserRepository.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>()))
            .ReturnsAsync(false);
        // 注释掉不存在的接口方法的Mock设置
        // _mockUserRepository.Setup(r => r.IsEmailExistsAsync("zhangsan@test.com")).ReturnsAsync(true);
        // _mockUserRepository.Setup(r => r.IsStudentIdExistsAsync("2025001")).ReturnsAsync(true);
        // _mockUserRepository.Setup(r => r.IsUserLockedAsync(1)).ReturnsAsync(false);
        // _mockUserRepository.Setup(r => r.IsEmailExistsAsync("wangwu@test.com")).ReturnsAsync(false);
        // _mockUserRepository.Setup(r => r.IsStudentIdExistsAsync("2025003")).ReturnsAsync(false);
        // _mockUserRepository.Setup(r => r.IsEmailExistsAsync("new@test.com")).ReturnsAsync(false);
        // _mockUserRepository.Setup(r => r.IsStudentIdExistsAsync("2025001")).ReturnsAsync(true);
        // _mockUserRepository.Setup(r => r.IsEmailExistsAsync("zhangsan@test.com")).ReturnsAsync(true);
        // _mockUserRepository.Setup(r => r.IsStudentIdExistsAsync("2025005")).ReturnsAsync(false);
    }

    [Fact]
    public async Task LoginWithTokenAsync_WithValidCredentials_ShouldReturnTokenResponse()
    {
        // Arrange
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "zhangsan@test.com",
            Password = "Test123!",
            DeviceId = "test_device"
        };

        var testUser = TestDbContextFactory.GetTestUser(1);
        var expectedTokenResponse = JwtTestHelper.CreateTestTokenResponse(testUser);

        // Mock GetUserByUsernameAsync
        _mockUserRepository.Setup(r => r.GetByEmailAsync("zhangsan@test.com"))
            .ReturnsAsync(testUser);
        _mockUserRepository.Setup(r => r.GetUserWithStudentAsync(1))
            .ReturnsAsync(testUser);

        // Mock user repository methods for login process
        // _mockUserRepository.Setup(r => r.IsUserLockedAsync(1)).ReturnsAsync(false);
        // _mockUserRepository.Setup(r => r.ResetFailedLoginAttemptsAsync(1)).Returns(Task.CompletedTask);
        // _mockUserRepository.Setup(r => r.UnlockUserAsync(1)).Returns(Task.CompletedTask);
        // _mockUserRepository.Setup(r => r.UpdateLastLoginAsync(1, It.IsAny<string>())).Returns(Task.CompletedTask);

        _mockTokenService.Setup(x => x.GenerateTokenResponseAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null))
                        .ReturnsAsync(expectedTokenResponse);

        // Act
        var result = await _authService.LoginWithTokenAsync(loginRequest, "192.168.1.1", "Test Browser");

        // Assert
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeEmpty();
        result.UserId.Should().Be(1);
        result.Username.Should().Be("zhangsan");
        _mockTokenService.Verify(x => x.GenerateTokenResponseAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null), Times.Once);
    }

    [Fact]
    public async Task LoginWithTokenAsync_WithEmailLogin_ShouldReturnTokenResponse()
    {
        // Arrange
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "zhangsan@test.com", // 使用邮箱登录
            Password = "Test123!",
            DeviceId = "test_device"
        };

        var testUser = TestDbContextFactory.GetTestUser(1);
        var expectedTokenResponse = JwtTestHelper.CreateTestTokenResponse(testUser);

        // Mock GetUserByUsernameAsync
        _mockUserRepository.Setup(r => r.GetByEmailAsync("zhangsan@test.com"))
            .ReturnsAsync(testUser);
        _mockUserRepository.Setup(r => r.GetUserWithStudentAsync(1))
            .ReturnsAsync(testUser);

        // Mock user repository methods for login process
        // _mockUserRepository.Setup(r => r.IsUserLockedAsync(1)).ReturnsAsync(false);
        // _mockUserRepository.Setup(r => r.ResetFailedLoginAttemptsAsync(1)).Returns(Task.CompletedTask);
        // _mockUserRepository.Setup(r => r.UnlockUserAsync(1)).Returns(Task.CompletedTask);
        // _mockUserRepository.Setup(r => r.UpdateLastLoginAsync(1, It.IsAny<string>())).Returns(Task.CompletedTask);

        _mockTokenService.Setup(x => x.GenerateTokenResponseAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null))
                        .ReturnsAsync(expectedTokenResponse);

        // Act
        var result = await _authService.LoginWithTokenAsync(loginRequest, "192.168.1.1", "Test Browser");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("zhangsan@test.com");
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
        _mockTokenService.Verify(x => x.GenerateTokenResponseAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null), Times.Never);
    }

    [Fact]
    public async Task LoginWithTokenAsync_WithInvalidPassword_ShouldReturnNull()
    {
        // Arrange
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "zhangsan@test.com",
            Password = "WrongPassword",
            DeviceId = "test_device"
        };

        // Act
        var result = await _authService.LoginWithTokenAsync(loginRequest, "192.168.1.1", "Test Browser");

        // Assert
        result.Should().BeNull();
        _mockTokenService.Verify(x => x.GenerateTokenResponseAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null), Times.Never);
    }

    [Fact]
    public async Task LoginWithTokenAsync_WithInactiveUser_ShouldReturnNull()
    {
        // Arrange
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "lisi@test.com", // 这个用户在测试数据中是禁用的
            Password = "Test123!",
            DeviceId = "test_device"
        };

        // Act
        var result = await _authService.LoginWithTokenAsync(loginRequest, "192.168.1.1", "Test Browser");

        // Assert
        result.Should().BeNull();
    }

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
            Username = "wangwu",
            Phone = "13800138003"
        };

        // 设置学生验证通过
        var testStudent = TestDbContextFactory.GetTestStudent("2025003");
        testStudent.Name = "王五";
        _mockStudentRepository.Setup(r => r.FirstOrDefaultAsync(s => s.StudentId == "2025003" && s.Name == "王五"))
            .ReturnsAsync(testStudent);

        // 设置邮箱和学号不存在
        _mockUserRepository.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>()))
            .ReturnsAsync(false);
        // _mockUserRepository.Setup(r => r.IsEmailExistsAsync("wangwu@test.com")).ReturnsAsync(false);
        // _mockUserRepository.Setup(r => r.IsStudentIdExistsAsync("2025003")).ReturnsAsync(false);

        // 设置用户创建
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        result.Should().NotBeNull();
        result!.StudentId.Should().Be("2025003");
        result.Email.Should().Be("wangwu@test.com");
        result.Username.Should().Be("wangwu");
        result.CreditScore.Should().Be(60.0m);
        BCrypt.Net.BCrypt.Verify("Test123!", result.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_WithExistingStudentId_ShouldThrowException()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            StudentId = "2025001", // 这个学号已经注册了
            Name = "张三",
            Email = "new@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!"
        };

        // 设置学生验证通过
        var testStudent = TestDbContextFactory.GetTestStudent("2025001");
        _mockStudentRepository.Setup(r => r.FirstOrDefaultAsync(s => s.StudentId == "2025001" && s.Name == "张三"))
            .ReturnsAsync(testStudent);

        // 设置邮箱不存在但学号存在
        _mockUserRepository.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>()))
            .ReturnsAsync(false);
        // _mockUserRepository.Setup(r => r.IsEmailExistsAsync("new@test.com")).ReturnsAsync(false);
        // _mockUserRepository.Setup(r => r.IsStudentIdExistsAsync("2025001")).ReturnsAsync(true);

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
            Email = "zhangsan@test.com", // 这个邮箱已经存在
            Password = "Test123!",
            ConfirmPassword = "Test123!"
        };

        // 设置学生验证通过
        var testStudent = TestDbContextFactory.GetTestStudent("2025005");
        testStudent.Name = "测试用户";
        _mockStudentRepository.Setup(r => r.FirstOrDefaultAsync(s => s.StudentId == "2025005" && s.Name == "测试用户"))
            .ReturnsAsync(testStudent);

        // 设置邮箱存在但学号不存在
        _mockUserRepository.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>()))
            .ReturnsAsync(false);
        // _mockUserRepository.Setup(r => r.IsEmailExistsAsync("zhangsan@test.com")).ReturnsAsync(true);
        // _mockUserRepository.Setup(r => r.IsStudentIdExistsAsync("2025005")).ReturnsAsync(false);

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
            StudentId = "9999999", // 不存在的学号
            Name = "不存在",
            Email = "notexist@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!"
        };

        // 设置学生验证失败
        _mockStudentRepository.Setup(r => r.FirstOrDefaultAsync(s => s.StudentId == "9999999" && s.Name == "不存在"))
            .ReturnsAsync((Student?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(registerDto));
        exception.Message.Should().Contain("学生身份验证失败");
    }

    [Fact]
    public async Task GetUserByUsernameAsync_WithValidUsername_ShouldReturnUser()
    {
        // Arrange
        var testUser = TestDbContextFactory.GetTestUser(1);
        _mockUserRepository.Setup(r => r.GetByEmailAsync("zhangsan"))
            .ReturnsAsync((User?)null); // 邮箱查找失败
        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(testUser); // 用户名查找成功
        _mockUserRepository.Setup(r => r.GetUserWithStudentAsync(1))
            .ReturnsAsync(testUser);

        // Act
        var result = await _authService.GetUserByUsernameAsync("zhangsan");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("zhangsan");
        result.UserId.Should().Be(1);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_WithValidEmail_ShouldReturnUser()
    {
        // Arrange
        var testUser = TestDbContextFactory.GetTestUser(1);
        _mockUserRepository.Setup(r => r.GetByEmailAsync("zhangsan@test.com"))
            .ReturnsAsync(testUser);
        _mockUserRepository.Setup(r => r.GetUserWithStudentAsync(1))
            .ReturnsAsync(testUser);

        // Act
        var result = await _authService.GetUserByUsernameAsync("zhangsan@test.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("zhangsan@test.com");
        result.UserId.Should().Be(1);
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
        var result = await _authService.GetUserByUsernameAsync("lisi@test.com"); // 禁用用户

        // Assert
        result.Should().BeNull();
    }

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

    [Fact]
    public async Task LogoutAsync_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var refreshToken = "test_refresh_token";
        _mockTokenService.Setup(x => x.RevokeRefreshTokenAsync(refreshToken, It.IsAny<string>(), null))
                         .ReturnsAsync(true);

        // Act
        var result = await _authService.LogoutAsync(refreshToken);

        // Assert
        result.Should().BeTrue();
        _mockTokenService.Verify(x => x.RevokeRefreshTokenAsync(refreshToken, "用户注销", null), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var refreshToken = "invalid_token";
        _mockTokenService.Setup(x => x.RevokeRefreshTokenAsync(refreshToken, It.IsAny<string>(), null))
                         .ReturnsAsync(false);

        // Act
        var result = await _authService.LogoutAsync(refreshToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task LogoutAllDevicesAsync_WithValidUserId_ShouldReturnTrue()
    {
        // Arrange
        var userId = 1;
        _mockTokenService.Setup(x => x.RevokeAllUserTokensAsync(userId, It.IsAny<string>(), null))
                         .ReturnsAsync(2); // 返回撤销了2个Token

        // Act
        var result = await _authService.LogoutAllDevicesAsync(userId);

        // Assert
        result.Should().BeTrue();
        _mockTokenService.Verify(x => x.RevokeAllUserTokensAsync(userId, "注销所有设备", null), Times.Once);
    }

    [Fact]
    public async Task LogoutAllDevicesAsync_WithNoActiveTokens_ShouldReturnFalse()
    {
        // Arrange
        var userId = 999;
        _mockTokenService.Setup(x => x.RevokeAllUserTokensAsync(userId, It.IsAny<string>(), null))
                         .ReturnsAsync(0); // 没有Token被撤销

        // Act
        var result = await _authService.LogoutAllDevicesAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidRequest_ShouldReturnTokenResponse()
    {
        // Arrange
        var refreshTokenRequest = new RefreshTokenRequest
        {
            RefreshToken = "valid_refresh_token",
            DeviceId = "test_device"
        };

        var expectedTokenResponse = JwtTestHelper.CreateTestTokenResponse(TestDbContextFactory.GetTestUser(1));
        _mockTokenService.Setup(x => x.RefreshTokenAsync(refreshTokenRequest))
                         .ReturnsAsync(expectedTokenResponse);

        // Act
        var result = await _authService.RefreshTokenAsync(refreshTokenRequest);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeEmpty();
        result.UserId.Should().Be(1);
        _mockTokenService.Verify(x => x.RefreshTokenAsync(refreshTokenRequest), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidRequest_ShouldThrowException()
    {
        // Arrange
        var refreshTokenRequest = new RefreshTokenRequest
        {
            RefreshToken = "invalid_refresh_token"
        };

        _mockTokenService.Setup(x => x.RefreshTokenAsync(refreshTokenRequest))
                         .ThrowsAsync(new UnauthorizedAccessException("无效的刷新令牌"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.RefreshTokenAsync(refreshTokenRequest));
        exception.Message.Should().Contain("无效的刷新令牌");
    }

    public void Dispose()
    {
        // 清理资源
    }
}
