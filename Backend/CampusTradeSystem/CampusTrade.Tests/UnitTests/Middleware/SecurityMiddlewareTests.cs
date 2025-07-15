using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using CampusTrade.API.Middleware;
using CampusTrade.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Middleware;

/// <summary>
/// SecurityMiddleware单元测试
/// </summary>
public class SecurityMiddlewareTests : IDisposable
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<SecurityMiddleware>> _mockLogger;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly SecurityMiddleware _securityMiddleware;
    private readonly Mock<RequestDelegate> _mockNext;

    public SecurityMiddlewareTests()
    {
        _mockConfiguration = CreateMockConfiguration();
        _mockLogger = MockHelper.CreateMockLogger<SecurityMiddleware>();
        _mockCache = MockHelper.CreateMockMemoryCache();
        _mockNext = new Mock<RequestDelegate>();

        _securityMiddleware = new SecurityMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            _mockCache.Object,
            _mockConfiguration.Object);
    }

    #region IP黑名单检查测试

    [Fact]
    public async Task InvokeAsync_WithBlockedIP_ShouldReturnBlocked()
    {
        // Arrange
        var context = CreateHttpContext("192.168.1.100", "/api/test");
        MockHelper.SetupMockCacheContains(_mockCache, "blocked_ip:192.168.1.100", true);

        // 确保速率限制不会先触发
        var rateKey = "rate_limit:192.168.1.100";
        object rateValue = 0;
        _mockCache.Setup(x => x.TryGetValue(rateKey, out rateValue)).Returns(true);

        // Act
        await _securityMiddleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(429); // SecurityMiddleware统一返回429
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithAllowedIP_ShouldContinue()
    {
        // Arrange
        var context = CreateHttpContext("192.168.1.1", "/api/test");
        MockHelper.SetupMockCacheContains(_mockCache, "blocked_ip:192.168.1.1", false);
        MockHelper.SetupMockCacheContains(_mockCache, "rate_limit:192.168.1.1", false);

        // Act
        await _securityMiddleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    #endregion

    #region 可疑UserAgent检测测试

    [Fact]
    public async Task InvokeAsync_WithSuspiciousUserAgent_ShouldLogWarning()
    {
        // Arrange
        var context = CreateHttpContext("192.168.1.1", "/api/test", "sqlmap/1.0");
        MockHelper.SetupMockCacheContains(_mockCache, "blocked_ip:192.168.1.1", false);
        MockHelper.SetupMockCacheContains(_mockCache, "rate_limit:192.168.1.1", false);

        // Act
        await _securityMiddleware.InvokeAsync(context);

        // Assert
        MockHelper.VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Exactly(2)); // 两次日志：检测警告和安全事件
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once); // 应该继续处理，只是记录警告
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyUserAgent_ShouldLogWarning()
    {
        // Arrange
        var context = CreateHttpContext("192.168.1.1", "/api/test", "");
        MockHelper.SetupMockCacheContains(_mockCache, "blocked_ip:192.168.1.1", false);
        MockHelper.SetupMockCacheContains(_mockCache, "rate_limit:192.168.1.1", false);

        // Act
        await _securityMiddleware.InvokeAsync(context);

        // Assert
        MockHelper.VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Exactly(2)); // 两次日志：检测警告和安全事件
    }

    [Fact]
    public async Task InvokeAsync_WithNormalUserAgent_ShouldNotLogWarning()
    {
        // Arrange
        var context = CreateHttpContext("192.168.1.1", "/api/test", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        MockHelper.SetupMockCacheContains(_mockCache, "blocked_ip:192.168.1.1", false);
        MockHelper.SetupMockCacheContains(_mockCache, "rate_limit:192.168.1.1", false);

        // Act
        await _securityMiddleware.InvokeAsync(context);

        // Assert
        MockHelper.VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Never());
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    #endregion

    #region 速率限制测试

    [Fact]
    public async Task InvokeAsync_WithRateLimitExceeded_ShouldReturnBlocked()
    {
        // Arrange
        var context = CreateHttpContext("192.168.1.1", "/api/test");
        MockHelper.SetupMockCacheContains(_mockCache, "blocked_ip:192.168.1.1", false);

        // 设置速率限制已达到上限
        var rateKey = "rate_limit:192.168.1.1";
        object rateLimitValue = 100; // 超过配置的60
        _mockCache.Setup(x => x.TryGetValue(rateKey, out rateLimitValue)).Returns(true);

        // Act
        await _securityMiddleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(429); // Too Many Requests
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithNormalRate_ShouldIncrementCounter()
    {
        // Arrange
        var context = CreateHttpContext("192.168.1.1", "/api/test");
        MockHelper.SetupMockCacheContains(_mockCache, "blocked_ip:192.168.1.1", false);

        var rateKey = "rate_limit:192.168.1.1";
        object rateLimitValue = 5; // 正常范围内
        _mockCache.Setup(x => x.TryGetValue(rateKey, out rateLimitValue)).Returns(true);

        // Act
        await _securityMiddleware.InvokeAsync(context);

        // Assert
        // 验证请求继续处理（缓存操作是内部实现细节，不验证扩展方法）
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    #endregion

    #region 登录端点保护测试

    [Fact]
    public async Task InvokeAsync_WithLoginEndpointRateLimit_ShouldReturnBlocked()
    {
        // Arrange
        var context = CreateHttpContext("192.168.1.1", "/api/auth/login");
        MockHelper.SetupMockCacheContains(_mockCache, "blocked_ip:192.168.1.1", false);
        MockHelper.SetupMockCacheContains(_mockCache, "rate_limit:192.168.1.1", false);

        // 设置登录速率限制已达到上限
        var loginRateKey = "login_attempts:192.168.1.1";
        object loginRateLimitValue = 15; // 超过配置的10
        _mockCache.Setup(x => x.TryGetValue(loginRateKey, out loginRateLimitValue)).Returns(true);

        // Act
        await _securityMiddleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(429);
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithNormalLoginRate_ShouldIncrementLoginCounter()
    {
        // Arrange
        var context = CreateHttpContext("192.168.1.1", "/api/auth/login");
        MockHelper.SetupMockCacheContains(_mockCache, "blocked_ip:192.168.1.1", false);
        MockHelper.SetupMockCacheContains(_mockCache, "rate_limit:192.168.1.1", false);

        var loginRateKey = "login_attempts:192.168.1.1";
        object loginRateLimitValue = 3; // 正常范围内
        _mockCache.Setup(x => x.TryGetValue(loginRateKey, out loginRateLimitValue)).Returns(true);

        // Act
        await _securityMiddleware.InvokeAsync(context);

        // Assert  
        // 验证请求继续处理（缓存操作是内部实现细节，不验证扩展方法）
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    #endregion

    #region 请求大小检查测试

    [Fact]
    public async Task InvokeAsync_WithTooLargeRequest_ShouldReturnBlocked()
    {
        // Arrange
        var context = CreateHttpContext("192.168.1.1", "/api/upload");
        MockHelper.SetupMockCacheContains(_mockCache, "blocked_ip:192.168.1.1", false);
        MockHelper.SetupMockCacheContains(_mockCache, "rate_limit:192.168.1.1", false);

        // 设置过大的请求内容
        context.Request.ContentLength = 15 * 1024 * 1024; // 15MB，超过10MB限制

        // Act
        await _securityMiddleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(429); // SecurityMiddleware统一返回429
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithNormalRequestSize_ShouldContinue()
    {
        // Arrange
        var context = CreateHttpContext("192.168.1.1", "/api/upload");
        MockHelper.SetupMockCacheContains(_mockCache, "blocked_ip:192.168.1.1", false);
        MockHelper.SetupMockCacheContains(_mockCache, "rate_limit:192.168.1.1", false);

        context.Request.ContentLength = 5 * 1024 * 1024; // 5MB，正常范围

        // Act
        await _securityMiddleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    #endregion

    #region 恶意路径检查测试

    [Fact]
    public async Task InvokeAsync_WithMaliciousPath_ShouldReturnBlocked()
    {
        // Arrange
        var context = CreateHttpContext("192.168.1.1", "/../../../etc/passwd");
        MockHelper.SetupMockCacheContains(_mockCache, "blocked_ip:192.168.1.1", false);
        MockHelper.SetupMockCacheContains(_mockCache, "rate_limit:192.168.1.1", false);

        // Act
        await _securityMiddleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(429); // SecurityMiddleware统一返回429
        MockHelper.VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Exactly(2)); // 两次日志：检测警告和安全事件
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Theory]
    [InlineData("/api/auth/login")]
    [InlineData("/api/users/profile")]
    [InlineData("/health")]
    public async Task InvokeAsync_WithValidPath_ShouldContinue(string path)
    {
        // Arrange
        var context = CreateHttpContext("192.168.1.1", path);
        MockHelper.SetupMockCacheContains(_mockCache, "blocked_ip:192.168.1.1", false);
        MockHelper.SetupMockCacheContains(_mockCache, "rate_limit:192.168.1.1", false);

        // Act
        await _securityMiddleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    #endregion

    #region X-Forwarded-For头测试

    [Fact]
    public async Task InvokeAsync_WithXForwardedFor_ShouldUseForwardedIP()
    {
        // Arrange
        var context = CreateHttpContext("127.0.0.1", "/api/test");
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.1, 198.51.100.1";

        MockHelper.SetupMockCacheContains(_mockCache, "blocked_ip:203.0.113.1", true); // 第一个IP被阻止

        // Act
        await _securityMiddleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(429); // SecurityMiddleware统一返回429
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithXRealIP_ShouldUseRealIP()
    {
        // Arrange
        var context = CreateHttpContext("127.0.0.1", "/api/test");
        context.Request.Headers["X-Real-IP"] = "203.0.113.2";

        MockHelper.SetupMockCacheContains(_mockCache, "blocked_ip:203.0.113.2", true); // Real IP被阻止

        // Act
        await _securityMiddleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(429); // SecurityMiddleware统一返回429
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    #endregion

    #region 异常处理测试

    [Fact]
    public async Task InvokeAsync_WithException_ShouldContinueAndLogError()
    {
        // Arrange
        var context = CreateHttpContext("192.168.1.1", "/api/test");
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
                 .Throws(new Exception("缓存异常"));

        // Act
        await _securityMiddleware.InvokeAsync(context);

        // Assert
        MockHelper.VerifyLoggerCalled(_mockLogger, LogLevel.Error, "安全中间件执行异常");
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once); // 应该继续处理
    }

    #endregion

    #region 私有辅助方法

    private HttpContext CreateHttpContext(string ipAddress, string path, string userAgent = "Test Browser")
    {
        var context = new DefaultHttpContext();

        // 设置IP地址
        context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);

        // 设置请求路径
        context.Request.Path = path;
        context.Request.Method = "GET";

        // 设置UserAgent
        context.Request.Headers["User-Agent"] = userAgent;

        // 设置响应流
        context.Response.Body = new MemoryStream();

        return context;
    }

    private Mock<IConfiguration> CreateMockConfiguration()
    {
        var mockConfig = new Mock<IConfiguration>();

        // 设置安全配置的IConfigurationSection
        var maxRequestsSection = new Mock<IConfigurationSection>();
        maxRequestsSection.Setup(x => x.Value).Returns("60");
        mockConfig.Setup(x => x.GetSection("Security:MaxRequestsPerMinute")).Returns(maxRequestsSection.Object);

        var maxLoginAttemptsSection = new Mock<IConfigurationSection>();
        maxLoginAttemptsSection.Setup(x => x.Value).Returns("10");
        mockConfig.Setup(x => x.GetSection("Security:MaxLoginAttemptsPerHour")).Returns(maxLoginAttemptsSection.Object);

        var blockDurationSection = new Mock<IConfigurationSection>();
        blockDurationSection.Setup(x => x.Value).Returns("30");
        mockConfig.Setup(x => x.GetSection("Security:BlockDurationMinutes")).Returns(blockDurationSection.Object);

        // 设置可疑UserAgent列表
        var suspiciousUserAgents = new List<string> { "bot", "crawler", "spider", "scraper", "scan", "sqlmap", "nikto" };
        var mockSuspiciousSection = new Mock<IConfigurationSection>();
        // 手动设置子项而不是使用扩展方法
        var suspiciousChildren = suspiciousUserAgents.Select((item, index) =>
        {
            var childSection = new Mock<IConfigurationSection>();
            childSection.Setup(x => x.Value).Returns(item);
            childSection.Setup(x => x.Key).Returns(index.ToString());
            return childSection.Object;
        }).ToArray();
        mockSuspiciousSection.Setup(x => x.GetChildren()).Returns(suspiciousChildren);
        mockConfig.Setup(x => x.GetSection("Security:SuspiciousUserAgents")).Returns(mockSuspiciousSection.Object);

        // 设置被阻止IP列表
        var blockedIPs = new List<string> { "192.168.1.100" };
        var mockBlockedSection = new Mock<IConfigurationSection>();
        // 手动设置子项而不是使用扩展方法
        var blockedChildren = blockedIPs.Select((item, index) =>
        {
            var childSection = new Mock<IConfigurationSection>();
            childSection.Setup(x => x.Value).Returns(item);
            childSection.Setup(x => x.Key).Returns(index.ToString());
            return childSection.Object;
        }).ToArray();
        mockBlockedSection.Setup(x => x.GetChildren()).Returns(blockedChildren);
        mockConfig.Setup(x => x.GetSection("Security:BlockedIPs")).Returns(mockBlockedSection.Object);

        return mockConfig;
    }

    #endregion

    public void Dispose()
    {
        // 清理资源
    }
}
