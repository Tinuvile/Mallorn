using CampusTrade.API.Models.DTOs.Auth;
using CampusTrade.API.Models.Entities;
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
    private readonly AuthService _authService;
    public AuthServiceTests()
    {
        _mockConfiguration = MockHelper.CreateMockConfiguration();
        _mockTokenService = new Mock<ITokenService>();
        _mockLogger = MockHelper.CreateMockLogger<AuthService>();

        var context = TestDbContextFactory.CreateInMemoryDbContext();
        _authService = new AuthService(context, _mockConfiguration.Object, _mockTokenService.Object, _mockLogger.Object);
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

        var expectedTokenResponse = JwtTestHelper.CreateTestTokenResponse(TestDbContextFactory.GetTestUser(1));
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

        var expectedTokenResponse = JwtTestHelper.CreateTestTokenResponse(TestDbContextFactory.GetTestUser(1));
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
            Username = "nonexistent",
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
            Username = "zhangsan",
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
            Username = "lisi", // 这个用户在测试数据中是禁用的
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
            Username = "wangwu",
            Phone = "13800138003"
        };

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
        result.UserId.Should().Be(1);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_WithValidEmail_ShouldReturnUser()
    {
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
        var result = await _authService.GetUserByUsernameAsync("lisi"); // 禁用用户

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
    public async Task ValidateStudentAsync_WithMismatchedInfo_ShouldReturnFalse()
    {
        // Act
        var result = await _authService.ValidateStudentAsync("2025001", "李四"); // 学号和姓名不匹配

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

    #endregion

    #region LogoutAllDevicesAsync Tests

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

    #endregion

    #region RefreshTokenAsync Tests

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

    #endregion

    public void Dispose()
    {
        // 清理资源
    }
}
