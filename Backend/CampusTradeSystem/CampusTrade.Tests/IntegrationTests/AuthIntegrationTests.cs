using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using CampusTrade.API;
using CampusTrade.API.Models.DTOs.Auth;
using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CampusTrade.Tests.IntegrationTests;

/// <summary>
/// 认证集成测试
/// </summary>
public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // 使用内存数据库进行测试
                services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(CampusTrade.API.Data.CampusTradeDbContext))!);

                // 添加测试专用的数据库上下文
                var context = TestDbContextFactory.CreateInMemoryDbContext("IntegrationTest");
                services.AddSingleton(context);

                // 配置测试日志
                services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
            });
        });

        _client = _factory.CreateClient();
    }

    #region 用户注册集成测试

    [Fact]
    public async Task Register_CompleteFlow_ShouldSucceed()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            StudentId = "2025005",
            Name = "集成测试用户",
            Email = "integration@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!",
            Username = "integration_user",
            Phone = "13800138005"
        };

        // Act
        var response = await PostJsonAsync("/api/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await DeserializeResponse<ApiResponse<object>>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().Contain("注册成功");
    }

    [Fact]
    public async Task Register_WithExistingStudent_ShouldFail()
    {
        // Arrange - 使用已存在的学号
        var registerDto = new RegisterDto
        {
            StudentId = "2025001", // 已存在的学号
            Name = "张三",
            Email = "duplicate@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!"
        };

        // Act
        var response = await PostJsonAsync("/api/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var apiResponse = await DeserializeResponse<ApiResponse>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("该学号已被注册");
    }

    #endregion

    #region 用户登录集成测试

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "zhangsan",
            Password = "Test123!",
            DeviceId = "integration_test_device"
        };

        // Act
        var response = await PostJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await DeserializeResponse<ApiResponse<TokenResponse>>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.AccessToken.Should().NotBeEmpty();
        apiResponse.Data.RefreshToken.Should().NotBeEmpty();
        apiResponse.Data.UserId.Should().Be(1);
        apiResponse.Data.Username.Should().Be("zhangsan");
    }

    [Fact]
    public async Task Login_WithEmailCredentials_ShouldReturnToken()
    {
        // Arrange
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "zhangsan@test.com", // 使用邮箱登录
            Password = "Test123!",
            DeviceId = "integration_test_device"
        };

        // Act
        var response = await PostJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await DeserializeResponse<ApiResponse<TokenResponse>>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Email.Should().Be("zhangsan@test.com");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "invalid_user",
            Password = "wrong_password",
            DeviceId = "test_device"
        };

        // Act
        var response = await PostJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var apiResponse = await DeserializeResponse<ApiResponse>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("用户名或密码错误");
    }

    #endregion

    #region Token刷新集成测试

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange - 先登录获取Token
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "zhangsan",
            Password = "Test123!",
            DeviceId = "refresh_test_device"
        };

        var loginResponse = await PostJsonAsync("/api/auth/login", loginRequest);
        var loginApiResponse = await DeserializeResponse<ApiResponse<TokenResponse>>(loginResponse);
        var originalRefreshToken = loginApiResponse!.Data!.RefreshToken;

        // Act - 刷新Token
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = originalRefreshToken,
            DeviceId = "refresh_test_device"
        };

        var refreshResponse = await PostJsonAsync("/api/token/refresh", refreshRequest);

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshApiResponse = await DeserializeResponse<ApiResponse<TokenResponse>>(refreshResponse);
        refreshApiResponse.Should().NotBeNull();
        refreshApiResponse!.Success.Should().BeTrue();
        refreshApiResponse.Data.Should().NotBeNull();
        refreshApiResponse.Data!.AccessToken.Should().NotBeEmpty();
        refreshApiResponse.Data.RefreshToken.Should().NotBeEmpty();
        refreshApiResponse.Data.RefreshToken.Should().NotBe(originalRefreshToken); // 新的刷新Token应该不同
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = "invalid_refresh_token_12345",
            DeviceId = "test_device"
        };

        // Act
        var response = await PostJsonAsync("/api/token/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var apiResponse = await DeserializeResponse<ApiResponse>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("刷新失败");
    }

    #endregion

    #region 用户信息查询集成测试

    [Fact]
    public async Task GetUser_WithValidUsername_ShouldReturnUserInfo()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/user/zhangsan");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await DeserializeResponse<ApiResponse<object>>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUser_WithInvalidUsername_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/user/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var apiResponse = await DeserializeResponse<ApiResponse>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("用户不存在");
    }

    #endregion

    #region 学生身份验证集成测试

    [Fact]
    public async Task ValidateStudent_WithValidInfo_ShouldReturnSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/validate-student?studentId=2025001&name=张三");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await DeserializeResponse<ApiResponse<object>>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().Contain("学生身份验证成功");
    }

    [Fact]
    public async Task ValidateStudent_WithInvalidInfo_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/validate-student?studentId=9999999&name=不存在");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var apiResponse = await DeserializeResponse<ApiResponse>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("学生身份验证失败");
    }

    #endregion

    #region 注销集成测试

    [Fact]
    public async Task Logout_WithValidToken_ShouldSucceed()
    {
        // Arrange - 先登录获取Token
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "zhangsan",
            Password = "Test123!",
            DeviceId = "logout_test_device"
        };

        var loginResponse = await PostJsonAsync("/api/auth/login", loginRequest);
        var loginApiResponse = await DeserializeResponse<ApiResponse<TokenResponse>>(loginResponse);
        var refreshToken = loginApiResponse!.Data!.RefreshToken;

        // Act - 注销
        var logoutRequest = new RevokeTokenRequest
        {
            Token = refreshToken,
            Reason = "集成测试注销"
        };

        var logoutResponse = await PostJsonAsync("/api/auth/logout", logoutRequest);

        // Assert
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var logoutApiResponse = await DeserializeResponse<ApiResponse<object>>(logoutResponse);
        logoutApiResponse.Should().NotBeNull();
        logoutApiResponse!.Success.Should().BeTrue();
        logoutApiResponse.Message.Should().Contain("注销成功");

        // 验证Token已被撤销 - 尝试用已注销的Token刷新应该失败
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = refreshToken,
            DeviceId = "logout_test_device"
        };

        var refreshResponse = await PostJsonAsync("/api/token/refresh", refreshRequest);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region 完整认证流程测试

    [Fact]
    public async Task CompleteAuthFlow_RegisterLoginRefreshLogout_ShouldSucceed()
    {
        // Step 1: 注册新用户
        var registerDto = new RegisterDto
        {
            StudentId = "2025010",
            Name = "完整流程测试",
            Email = "complete_flow@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!",
            Username = "complete_flow_user"
        };

        var registerResponse = await PostJsonAsync("/api/auth/register", registerDto);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: 登录
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "complete_flow_user",
            Password = "Test123!",
            DeviceId = "complete_flow_device"
        };

        var loginResponse = await PostJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginApiResponse = await DeserializeResponse<ApiResponse<TokenResponse>>(loginResponse);
        var tokens = loginApiResponse!.Data!;

        // Step 3: 刷新Token
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = tokens.RefreshToken,
            DeviceId = "complete_flow_device"
        };

        var refreshResponse = await PostJsonAsync("/api/token/refresh", refreshRequest);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshApiResponse = await DeserializeResponse<ApiResponse<TokenResponse>>(refreshResponse);
        var newTokens = refreshApiResponse!.Data!;

        // Step 4: 注销
        var logoutRequest = new RevokeTokenRequest
        {
            Token = newTokens.RefreshToken,
            Reason = "完整流程测试结束"
        };

        var logoutResponse = await PostJsonAsync("/api/auth/logout", logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 验证整个流程都成功
        tokens.AccessToken.Should().NotBeEmpty();
        newTokens.AccessToken.Should().NotBeEmpty();
        newTokens.RefreshToken.Should().NotBe(tokens.RefreshToken);
    }

    #endregion

    #region 安全测试

    [Fact]
    public async Task SecurityMiddleware_WithSuspiciousUserAgent_ShouldAllowButLog()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("User-Agent", "sqlmap/1.0");

        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "zhangsan",
            Password = "Test123!",
            DeviceId = "suspicious_device"
        };

        // Act
        var response = await PostJsonAsync("/api/auth/login", loginRequest);

        // Assert
        // 应该允许请求通过（SecurityMiddleware只记录警告，不阻止）
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SecurityMiddleware_WithTooLargePayload_ShouldReturnError()
    {
        // Arrange
        var largePayload = new string('A', 11 * 1024 * 1024); // 11MB payload
        var content = new StringContent(largePayload, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    #endregion

    #region 私有辅助方法

    private async Task<HttpResponseMessage> PostJsonAsync<T>(string requestUri, T data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _client.PostAsync(requestUri, content);
    }

    private async Task<T?> DeserializeResponse<T>(HttpResponseMessage response) where T : class
    {
        var json = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(json)) return null;

        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        });
    }

    #endregion

    public void Dispose()
    {
        _client?.Dispose();
    }
}
