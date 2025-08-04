using System.Security.Claims;
using CampusTrade.API.Controllers;
using CampusTrade.API.Models.DTOs.Auth;
using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Services.Auth;
using CampusTrade.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Controllers;

/// <summary>
/// AuthController单元测试
/// </summary>
public class AuthControllerTests : IDisposable
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly AuthController _authController;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _mockLogger = MockHelper.CreateMockLogger<AuthController>();
        _authController = new AuthController(_mockAuthService.Object, _mockLogger.Object);

        // 设置基本的HttpContext
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
        httpContext.Request.Headers["User-Agent"] = "Test Browser";

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnSuccessResponse()
    {
        // Arrange
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "zhangsan",
            Password = "Test123!",
            DeviceId = "test_device"
        };

        var expectedTokenResponse = JwtTestHelper.CreateTestTokenResponse(TestDbContextFactory.GetTestUser(1));
        _mockAuthService.Setup(x => x.LoginWithTokenAsync(loginRequest, It.IsAny<string>(), It.IsAny<string>()))
                       .ReturnsAsync(expectedTokenResponse);

        // Act
        var result = await _authController.Login(loginRequest);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeOfType<ApiResponse<TokenResponse>>();

        var apiResponse = okResult.Value as ApiResponse<TokenResponse>;
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.UserId.Should().Be(1);
        apiResponse.Data.Username.Should().Be("zhangsan");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorizedResponse()
    {
        // Arrange
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "invalid",
            Password = "wrong",
            DeviceId = "test_device"
        };

        _mockAuthService.Setup(x => x.LoginWithTokenAsync(loginRequest, It.IsAny<string>(), It.IsAny<string>()))
                       .ReturnsAsync((TokenResponse?)null);

        // Act
        var result = await _authController.Login(loginRequest);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.Value.Should().BeOfType<ApiResponse>();

        var apiResponse = unauthorizedResult.Value as ApiResponse;
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("用户名或密码错误");
    }

    [Fact]
    public async Task Login_WithInvalidModel_ShouldReturnBadRequest()
    {
        // Arrange
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "", // 无效：用户名为空
            Password = "Test123!",
            DeviceId = "test_device"
        };

        _authController.ModelState.AddModelError("Username", "用户名不能为空");

        // Act
        var result = await _authController.Login(loginRequest);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<ApiResponse>();

        var apiResponse = badRequestResult.Value as ApiResponse;
        apiResponse!.Success.Should().BeFalse();
        apiResponse.ErrorCode.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Login_WithServiceException_ShouldReturnInternalServerError()
    {
        // Arrange
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "zhangsan",
            Password = "Test123!",
            DeviceId = "test_device"
        };

        _mockAuthService.Setup(x => x.LoginWithTokenAsync(loginRequest, It.IsAny<string>(), It.IsAny<string>()))
                       .ThrowsAsync(new Exception("数据库连接失败"));

        // Act
        var result = await _authController.Login(loginRequest);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);

        var apiResponse = objectResult.Value as ApiResponse;
        apiResponse!.Success.Should().BeFalse();
        apiResponse.ErrorCode.Should().Be("INTERNAL_ERROR");
    }

    #endregion

    #region Register Tests

    [Fact]
    public async Task Register_WithValidData_ShouldReturnSuccessResponse()
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

        var expectedUser = new User
        {
            UserId = 3,
            StudentId = "2025003",
            Username = "wangwu",
            Email = "wangwu@test.com",
            FullName = "王五"
        };

        _mockAuthService.Setup(x => x.RegisterAsync(registerDto))
                       .ReturnsAsync(expectedUser);

        // Act
        var result = await _authController.Register(registerDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeOfType<ApiResponse<object>>();

        var apiResponse = okResult.Value as ApiResponse<object>;
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().Contain("注册成功");
    }

    [Fact]
    public async Task Register_WithExistingUser_ShouldReturnBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            StudentId = "2025001", // 已存在的学号
            Name = "张三",
            Email = "zhangsan@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!"
        };

        _mockAuthService.Setup(x => x.RegisterAsync(registerDto))
                       .ThrowsAsync(new ArgumentException("该学号已被注册"));

        // Act
        var result = await _authController.Register(registerDto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<ApiResponse>();

        var apiResponse = badRequestResult.Value as ApiResponse;
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("该学号已被注册");
    }

    [Fact]
    public async Task Register_WithInvalidModel_ShouldReturnBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            StudentId = "",
            Name = "测试",
            Email = "invalid-email", // 无效邮箱格式
            Password = "123", // 密码太短
            ConfirmPassword = "456" // 确认密码不匹配
        };

        _authController.ModelState.AddModelError("Email", "邮箱格式不正确");
        _authController.ModelState.AddModelError("Password", "密码长度必须在6-100字符之间");

        // Act
        var result = await _authController.Register(registerDto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<ApiResponse>();

        var apiResponse = badRequestResult.Value as ApiResponse;
        apiResponse!.Success.Should().BeFalse();
        apiResponse.ErrorCode.Should().Be("VALIDATION_ERROR");
    }

    #endregion

    #region GetUser Tests

    [Fact]
    public async Task GetUser_WithValidUsername_ShouldReturnUserInfo()
    {
        // Arrange
        var username = "zhangsan";
        var expectedUser = TestDbContextFactory.GetTestUser(1);

        _mockAuthService.Setup(x => x.GetUserByUsernameAsync(username))
                       .ReturnsAsync(expectedUser);

        // Act
        var result = await _authController.GetUser(username);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeOfType<ApiResponse<object>>();

        var apiResponse = okResult.Value as ApiResponse<object>;
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUser_WithInvalidUsername_ShouldReturnNotFound()
    {
        // Arrange
        var username = "nonexistent";

        _mockAuthService.Setup(x => x.GetUserByUsernameAsync(username))
                       .ReturnsAsync((User?)null);

        // Act
        var result = await _authController.GetUser(username);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeOfType<ApiResponse>();

        var apiResponse = notFoundResult.Value as ApiResponse;
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("用户不存在");
    }

    #endregion

    #region ValidateStudent Tests

    [Fact]
    public async Task ValidateStudent_WithValidStudentInfo_ShouldReturnSuccess()
    {
        // Arrange
        var validationDto = new StudentValidationDto
        {
            StudentId = "2025001",
            Name = "张三"
        };

        _mockAuthService.Setup(x => x.ValidateStudentAsync(validationDto.StudentId, validationDto.Name))
                       .ReturnsAsync(true);

        // Act
        var result = await _authController.ValidateStudent(validationDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeOfType<ApiResponse<object>>();

        var apiResponse = okResult.Value as ApiResponse<object>;
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().Contain("学生身份验证成功");
    }

    [Fact]
    public async Task ValidateStudent_WithInvalidStudentInfo_ShouldReturnSuccess()
    {
        // Arrange
        var validationDto = new StudentValidationDto
        {
            StudentId = "9999999",
            Name = "不存在"
        };

        _mockAuthService.Setup(x => x.ValidateStudentAsync(validationDto.StudentId, validationDto.Name))
                       .ReturnsAsync(false);

        // Act
        var result = await _authController.ValidateStudent(validationDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeOfType<ApiResponse<object>>();

        var apiResponse = okResult.Value as ApiResponse<object>;
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().Contain("学生身份验证失败");
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_WithValidToken_ShouldReturnSuccess()
    {
        // Arrange
        var logoutRequest = new LogoutRequest
        {
            RefreshToken = "valid_refresh_token"
        };

        _mockAuthService.Setup(x => x.LogoutAsync(logoutRequest.RefreshToken, It.IsAny<string>()))
                       .ReturnsAsync(true);

        // Act
        var result = await _authController.Logout(logoutRequest);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeOfType<ApiResponse>();

        var apiResponse = okResult.Value as ApiResponse;
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().Contain("退出登录成功");
    }

    [Fact]
    public async Task Logout_WithInvalidToken_ShouldReturnBadRequest()
    {
        // Arrange
        var logoutRequest = new LogoutRequest
        {
            RefreshToken = "invalid_token"
        };

        _mockAuthService.Setup(x => x.LogoutAsync(logoutRequest.RefreshToken, It.IsAny<string>()))
                       .ReturnsAsync(false);

        // Act
        var result = await _authController.Logout(logoutRequest);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<ApiResponse>();

        var apiResponse = badRequestResult.Value as ApiResponse;
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("退出登录失败");
    }

    #endregion

    #region LogoutAll Tests

    [Fact]
    public async Task LogoutAll_WithValidUserId_ShouldReturnSuccess()
    {
        // Arrange
        var userId = 1;
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "Test"));

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claims }
        };

        _mockAuthService.Setup(x => x.LogoutAllDevicesAsync(userId, It.IsAny<string>()))
                       .ReturnsAsync(true); // Return success boolean

        // Act
        var result = await _authController.LogoutAll();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeOfType<ApiResponse<object>>();

        var apiResponse = okResult.Value as ApiResponse<object>;
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().Contain("已退出所有设备");
    }

    [Fact]
    public async Task LogoutAll_WithInvalidUserId_ShouldReturnUnauthorized()
    {
        // Arrange - No user claims set (invalid user)
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await _authController.LogoutAll();

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.Value.Should().BeOfType<ApiResponse>();

        var apiResponse = unauthorizedResult.Value as ApiResponse;
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("无效的用户身份");
    }

    #endregion

    public void Dispose()
    {
        // 清理资源
    }
}

