using System.Net;
using System.Security.Claims;
using CampusTrade.API.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CampusTrade.Tests.Helpers;

/// <summary>
/// Mock对象创建辅助类
/// </summary>
public static class MockHelper
{
    /// <summary>
    /// 创建Mock IConfiguration
    /// </summary>
    public static Mock<IConfiguration> CreateMockConfiguration(Dictionary<string, string>? configValues = null)
    {
        var mockConfig = new Mock<IConfiguration>();

        var defaultValues = new Dictionary<string, string>
        {
            ["Jwt:SecretKey"] = "YourSecretKeyForCampusTradingPlatformProduction2025!MustBe32CharactersLong",
            ["Jwt:Issuer"] = "CampusTrade.API",
            ["Jwt:Audience"] = "CampusTrade.Client",
            ["Jwt:AccessTokenExpirationMinutes"] = "15",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Jwt:MaxActiveDevices"] = "3",
            ["Security:MaxRequestsPerMinute"] = "100",
            ["Security:MaxLoginAttemptsPerHour"] = "15",
            ["Security:BlockDurationMinutes"] = "30"
        };

        // 合并用户提供的配置值
        if (configValues != null)
        {
            foreach (var kvp in configValues)
            {
                defaultValues[kvp.Key] = kvp.Value;
            }
        }

        // 设置配置值
        foreach (var kvp in defaultValues)
        {
            mockConfig.Setup(x => x[kvp.Key]).Returns(kvp.Value);
        }

        return mockConfig;
    }

    /// <summary>
    /// 创建Mock ILogger
    /// </summary>
    public static Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    /// <summary>
    /// 创建Mock IMemoryCache
    /// </summary>
    public static Mock<IMemoryCache> CreateMockMemoryCache()
    {
        var mockCache = new Mock<IMemoryCache>();
        var mockCacheEntry = new Mock<ICacheEntry>();

        // 设置 ICacheEntry 的基本属性
        mockCacheEntry.SetupAllProperties();

        // 设置 CreateEntry 方法返回 mock 的 ICacheEntry
        mockCache.Setup(x => x.CreateEntry(It.IsAny<object>()))
                 .Returns(mockCacheEntry.Object);

        return mockCache;
    }

    /// <summary>
    /// 创建带有验证功能的Mock IMemoryCache
    /// </summary>
    public static (Mock<IMemoryCache> mockCache, Mock<ICacheEntry> mockCacheEntry) CreateMockMemoryCacheWithEntry()
    {
        var mockCache = new Mock<IMemoryCache>();
        var mockCacheEntry = new Mock<ICacheEntry>();

        // 设置 ICacheEntry 的基本属性
        mockCacheEntry.SetupAllProperties();

        // 设置 CreateEntry 方法返回 mock 的 ICacheEntry
        mockCache.Setup(x => x.CreateEntry(It.IsAny<object>()))
                 .Returns(mockCacheEntry.Object);

        return (mockCache, mockCacheEntry);
    }

    /// <summary>
    /// 设置Mock缓存包含指定键
    /// </summary>
    public static void SetupMockCacheContains(Mock<IMemoryCache> mockCache, string key, bool exists = true)
    {
        object? cachedValue = exists ? new object() : null;
        mockCache.Setup(x => x.TryGetValue(key, out cachedValue))
                .Returns(exists);
    }

    /// <summary>
    /// 创建Mock HttpContext
    /// </summary>
    public static Mock<HttpContext> CreateMockHttpContext(string? ipAddress = null, ClaimsPrincipal? user = null)
    {
        var mockHttpContext = new Mock<HttpContext>();
        var mockRequest = new Mock<HttpRequest>();
        var mockResponse = new Mock<HttpResponse>();
        var mockConnection = new Mock<ConnectionInfo>();
        var mockHeaders = new HeaderDictionary();

        // 设置IP地址
        if (!string.IsNullOrEmpty(ipAddress))
        {
            mockConnection.Setup(x => x.RemoteIpAddress)
                         .Returns(IPAddress.Parse(ipAddress));
        }

        // 设置用户信息
        if (user != null)
        {
            mockHttpContext.Setup(x => x.User).Returns(user);
        }

        // 设置Headers
        mockRequest.Setup(x => x.Headers).Returns(mockHeaders);

        // 组装HttpContext
        mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
        mockHttpContext.Setup(x => x.Response).Returns(mockResponse.Object);
        mockHttpContext.Setup(x => x.Connection).Returns(mockConnection.Object);

        return mockHttpContext;
    }

    /// <summary>
    /// 创建带认证信息的Mock HttpContext
    /// </summary>
    public static Mock<HttpContext> CreateMockHttpContextWithAuth(string token, ClaimsPrincipal? user = null)
    {
        var mockHttpContext = CreateMockHttpContext(user: user);
        var mockHeaders = new HeaderDictionary
        {
            ["Authorization"] = $"Bearer {token}"
        };

        mockHttpContext.Setup(x => x.Request.Headers).Returns(mockHeaders);
        return mockHttpContext;
    }

    /// <summary>
    /// 创建Mock ClaimsPrincipal
    /// </summary>
    public static ClaimsPrincipal CreateMockClaimsPrincipal(int userId, string email, string username)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim("username", username),
            new Claim("student_id", "2025001")
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    /// <summary>
    /// 验证Logger被调用
    /// </summary>
    public static void VerifyLoggerCalled<T>(Mock<ILogger<T>> mockLogger, LogLevel logLevel, string message)
    {
        mockLogger.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// 验证Logger被调用指定次数
    /// </summary>
    public static void VerifyLoggerCalled<T>(Mock<ILogger<T>> mockLogger, LogLevel logLevel, Times times)
    {
        mockLogger.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    /// <summary>
    /// 创建测试用的IOptions<T>
    /// </summary>
    public static IOptions<T> CreateMockOptions<T>(T value) where T : class
    {
        var mockOptions = new Mock<IOptions<T>>();
        mockOptions.Setup(x => x.Value).Returns(value);
        return mockOptions.Object;
    }

    /// <summary>
    /// 创建JWT配置选项
    /// </summary>
    public static object CreateMockJwtOptions()
    {
        return new
        {
            SecretKey = "YourSecretKeyForCampusTradingPlatformProduction2025!MustBe32CharactersLong",
            Issuer = "CampusTrade.API",
            Audience = "CampusTrade.Client",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7,
            MaxActiveDevices = 3,
            RefreshTokenRotation = true,
            EnableTokenBlacklist = true
        };
    }

    /// <summary>
    /// 创建Mock配置节
    /// </summary>
    public static Mock<IConfigurationSection> CreateMockConfigurationSection(string value)
    {
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(x => x.Value).Returns(value);
        return mockSection;
    }

    /// <summary>
    /// 设置复杂的配置结构
    /// </summary>
    public static void SetupNestedConfiguration(Mock<IConfiguration> mockConfig, string sectionName, Dictionary<string, string> values)
    {
        var mockSection = new Mock<IConfigurationSection>();

        foreach (var kvp in values)
        {
            var childSection = CreateMockConfigurationSection(kvp.Value);
            mockSection.Setup(x => x[kvp.Key]).Returns(kvp.Value);
            mockConfig.Setup(x => x.GetSection($"{sectionName}:{kvp.Key}")).Returns(childSection.Object);
        }

        mockConfig.Setup(x => x.GetSection(sectionName)).Returns(mockSection.Object);
    }
}
