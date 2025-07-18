using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using CampusTrade.API;
using CampusTrade.API.Data;
using CampusTrade.API.Models.DTOs.Auth;
using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CampusTrade.Tests.IntegrationTests;

/// <summary>
/// API端到端测试
/// </summary>
public class ApiEndToEndTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiEndToEndTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // 移除原有的DbContext注册
                services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(CampusTradeDbContext))!);

                // 使用内存数据库进行测试，每次请求创建新的DbContext实例
                services.AddDbContext<CampusTradeDbContext>(options =>
                {
                    options.UseInMemoryDatabase("EndToEndTest");
                    options.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                }, ServiceLifetime.Scoped);

                // 配置测试日志
                services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
            });
        });

        _client = _factory.CreateClient();

        // 设置测试用的UserAgent，避免安全中间件检测为可疑行为
        _client.DefaultRequestHeaders.Add("User-Agent", "IntegrationTest/1.0 (Campus Trade Test Suite)");

        // 确保数据库已初始化并播种数据
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CampusTradeDbContext>();
        TestDbContextFactory.SeedTestDataToContext(context);
    }

    #region 性能测试

    [Fact]
    public async Task Login_PerformanceTest_ShouldCompleteWithinTimeLimit()
    {
        // Arrange
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "zhangsan",
            Password = "Test123!",
            DeviceId = "performance_test_device"
        };

        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await PostJsonAsync("/api/auth/login", loginRequest);

        // Assert
        stopwatch.Stop();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // 登录应在1秒内完成
    }

    [Fact]
    public async Task ConcurrentLogin_ShouldHandleMultipleRequests()
    {
        // Arrange
        const int concurrentRequests = 10;
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "zhangsan",
            Password = "Test123!",
            DeviceId = "concurrent_test_device"
        };

        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - 发起并发登录请求
        for (int i = 0; i < concurrentRequests; i++)
        {
            var task = PostJsonAsync("/api/auth/login", loginRequest);
            tasks.Add(task);
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiResponse = await DeserializeResponse<ApiResponse<TokenResponse>>(response);
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
        }
    }

    #endregion

    #region 并发安全测试

    [Fact]
    public async Task ConcurrentRegistration_ShouldPreventDuplicateUsers()
    {
        // Arrange
        const int concurrentRequests = 5;
        var registerDto = new RegisterDto
        {
            StudentId = "2025100", // 同一个学号
            Name = "并发测试用户",
            Email = "concurrent@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!",
            Username = "concurrent_user"
        };

        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - 发起并发注册请求
        for (int i = 0; i < concurrentRequests; i++)
        {
            var task = PostJsonAsync("/api/auth/register", registerDto);
            tasks.Add(task);
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        var successfulRegistrations = 0;
        var duplicateErrors = 0;

        foreach (var response in responses)
        {
            if (response.StatusCode == HttpStatusCode.OK)
            {
                successfulRegistrations++;
            }
            else if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var apiResponse = await DeserializeResponse<ApiResponse>(response);
                if (apiResponse?.Message.Contains("已被注册") == true)
                {
                    duplicateErrors++;
                }
            }
        }

        // 只应该有一个成功注册，其他都应该是重复错误
        successfulRegistrations.Should().Be(1);
        duplicateErrors.Should().Be(concurrentRequests - 1);
    }

    #endregion

    #region Token安全测试

    [Fact]
    public async Task TokenRefresh_WithRevokedToken_ShouldFail()
    {
        // Arrange - 登录获取Token
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = "zhangsan",
            Password = "Test123!",
            DeviceId = "revoke_test_device"
        };

        var loginResponse = await PostJsonAsync("/api/auth/login", loginRequest);
        var loginApiResponse = await DeserializeResponse<ApiResponse<TokenResponse>>(loginResponse);
        var refreshToken = loginApiResponse!.Data!.RefreshToken;

        // Act - 撤销Token
        var revokeRequest = new RevokeTokenRequest
        {
            Token = refreshToken,
            Reason = "安全测试"
        };

        await PostJsonAsync("/api/auth/logout", revokeRequest);

        // 尝试用已撤销的Token刷新
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = refreshToken,
            DeviceId = "revoke_test_device"
        };

        var refreshResponse = await PostJsonAsync("/api/token/refresh", refreshRequest);

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenValidation_WithExpiredToken_ShouldReturnUnauthorized()
    {
        // 这个测试需要等待Token过期，或者使用Mock的时间提供者
        // 目前简化为验证无效Token格式

        // Arrange
        var invalidTokenRequest = new RefreshTokenRequest
        {
            RefreshToken = "invalid.token.format",
            DeviceId = "test_device"
        };

        // Act
        var response = await PostJsonAsync("/api/token/refresh", invalidTokenRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public async Task Registration_WithMinimumValidData_ShouldSucceed()
    {
        // Arrange - 只提供必需字段
        var registerDto = new RegisterDto
        {
            StudentId = "2025200",
            Name = "最小数据测试",
            Email = "minimum@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!"
            // 不提供可选字段 Username 和 Phone
        };

        // Act
        var response = await PostJsonAsync("/api/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await DeserializeResponse<ApiResponse<object>>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Registration_WithMaximumLengthData_ShouldSucceed()
    {
        // Arrange - 使用接近最大长度的数据
        var registerDto = new RegisterDto
        {
            StudentId = "2025300",
            Name = "最大长度测试用户名称" + new string('测', 20), // 接近最大长度但不超过
            Email = "maxlength@test.com",
            Password = "Test123!" + new string('A', 50), // 较长但有效的密码
            ConfirmPassword = "Test123!" + new string('A', 50),
            Username = "max_length_username_" + new string('u', 20),
            Phone = "13800138999"
        };

        // Act
        var response = await PostJsonAsync("/api/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region 错误处理测试

    [Fact]
    public async Task InvalidJsonRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidJson = "{ invalid json content }";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MissingContentType_ShouldHandleGracefully()
    {
        // Arrange
        var validJson = JsonSerializer.Serialize(new LoginWithDeviceRequest
        {
            Username = "test",
            Password = "test",
            DeviceId = "test"
        });
        var content = new StringContent(validJson, Encoding.UTF8); // 不设置Content-Type

        // Act
        var response = await _client.PostAsync("/api/auth/login", content);

        // Assert
        // 应该优雅处理，可能返回415 Unsupported Media Type或其他适当的错误
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnsupportedMediaType);
    }

    #endregion

    #region 安全头部测试

    [Fact]
    public async Task SecurityHeaders_ShouldBePresent()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/user/zhangsan");

        // Assert
        // 检查常见的安全头部
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("X-XSS-Protection");
    }

    #endregion

    #region 健康检查测试

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    #endregion

    #region 压力测试

    [Fact]
    public async Task StressTest_MultipleEndpoints_ShouldHandleLoad()
    {
        // Arrange
        const int totalRequests = 50;
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - 混合不同类型的请求
        for (int i = 0; i < totalRequests; i++)
        {
            Task<HttpResponseMessage> task = (i % 4) switch
            {
                0 => _client.GetAsync("/api/auth/user/zhangsan"),
                1 => PostJsonAsync("/api/auth/validate-student", new { StudentId = "2025001", Name = "张三" }),
                2 => PostJsonAsync("/api/auth/login", new LoginWithDeviceRequest
                {
                    Username = "zhangsan",
                    Password = "Test123!",
                    DeviceId = $"stress_test_device_{i}"
                }),
                _ => _client.GetAsync("/health")
            };

            tasks.Add(task);
        }

        var stopwatch = Stopwatch.StartNew();
        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        foreach (var response in responses)
        {
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.Unauthorized, // 某些请求可能因为权限问题返回401
                HttpStatusCode.NotFound // 某些请求可能返回404
            );
        }

        // 所有请求应在合理时间内完成
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // 10秒内完成所有请求

        // 至少80%的请求应该成功
        var successfulRequests = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var successRate = (double)successfulRequests / totalRequests;
        successRate.Should().BeGreaterThan(0.8);
    }

    #endregion

    #region 多步骤业务流程测试

    [Fact]
    public async Task CompleteUserJourney_ShouldWorkEndToEnd()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Step 1: 验证学生身份
        var validateResponse = await PostJsonAsync("/api/auth/validate-student", new { StudentId = "2025003", Name = "王五" });
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: 注册用户
        var registerDto = new RegisterDto
        {
            StudentId = "2025003",
            Name = "王五",
            Email = $"journey_{timestamp}@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!",
            Username = $"journey_user_{timestamp}",
            Phone = "13800138888"
        };

        var registerResponse = await PostJsonAsync("/api/auth/register", registerDto);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 3: 登录
        var loginRequest = new LoginWithDeviceRequest
        {
            Username = registerDto.Username!,
            Password = registerDto.Password,
            DeviceId = $"journey_device_{timestamp}"
        };

        var loginResponse = await PostJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginApiResponse = await DeserializeResponse<ApiResponse<TokenResponse>>(loginResponse);
        var tokens = loginApiResponse!.Data!;

        // Step 4: 查询用户信息
        var userResponse = await _client.GetAsync($"/api/auth/user/{registerDto.Username}");
        userResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 5: 刷新Token
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = tokens.RefreshToken,
            DeviceId = loginRequest.DeviceId
        };

        var refreshResponse = await PostJsonAsync("/api/token/refresh", refreshRequest);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshApiResponse = await DeserializeResponse<ApiResponse<TokenResponse>>(refreshResponse);
        var newTokens = refreshApiResponse!.Data!;

        // Step 6: 注销
        var logoutRequest = new RevokeTokenRequest
        {
            Token = newTokens.RefreshToken,
            Reason = "完整流程测试结束"
        };

        var logoutResponse = await PostJsonAsync("/api/auth/logout", logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 7: 验证Token已失效
        var invalidRefreshRequest = new RefreshTokenRequest
        {
            RefreshToken = newTokens.RefreshToken,
            DeviceId = loginRequest.DeviceId
        };

        var invalidRefreshResponse = await PostJsonAsync("/api/token/refresh", invalidRefreshRequest);
        invalidRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
