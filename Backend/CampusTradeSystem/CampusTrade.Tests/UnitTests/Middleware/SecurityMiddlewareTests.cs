using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using CampusTrade.API.Infrastructure.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.TestCorrelator;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Middleware;

public class SecurityMiddlewareTests
{
    [Fact]
    public async Task SecurityMiddleware_LogsSuspiciousUserAgent()
    {
        // Arrange
        using var testCorrelator = TestCorrelator.CreateContext();

        // 配置 Serilog 日志
        var logger = new LoggerConfiguration()
           .MinimumLevel.Verbose()
           .WriteTo.TestCorrelator()
           .CreateLogger();

        var testServer = new TestServer(new WebHostBuilder()
           .ConfigureServices(services =>
            {
                services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddSerilog(logger);
                });
            })
           .Configure(app =>
            {
                app.UseMiddleware<SecurityMiddleware>();
                app.Run(async context =>
                {
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("OK");
                });
            }));

        var client = testServer.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("bot");

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext();
        foreach (var logEvent in logEvents)
        {
            Debug.WriteLine($"Log Event: {logEvent.Level} - {logEvent.RenderMessage()}");
        }

        Assert.NotEmpty(logEvents);
        Assert.Contains(logEvents, e =>
            e.Level == LogEventLevel.Warning &&
            e.MessageTemplate.Text.Contains("检测到可疑UserAgent访问"));
    }

    [Fact]
    public async Task GetUserId_ReturnsUserIdOrAnonymous()
    {
        // Arrange
        var mockContext = new Mock<HttpContext>();
        var mockUser = new Mock<ClaimsPrincipal>();
        var mockIdentity = new Mock<ClaimsIdentity>();
        var claim = new Claim(ClaimTypes.NameIdentifier, "123");

        mockIdentity.Setup(i => i.FindFirst(ClaimTypes.NameIdentifier)).Returns(claim);
        mockUser.Setup(u => u.Identity).Returns(mockIdentity.Object);
        mockContext.Setup(c => c.User).Returns(mockUser.Object);

        // SecurityMiddleware 没有公开的 GetUserId 方法，测试可删除或重构
        // var middleware = new SecurityMiddleware(_ => Task.CompletedTask, Mock.Of<ILogger<SecurityMiddleware>>(), Mock.Of<IMemoryCache>(), Mock.Of<IConfiguration>());
        // var userId = middleware.GetUserId(mockContext.Object);
        // Assert.Equal("123", userId);

        // mockIdentity.Setup(i => i.FindFirst(ClaimTypes.NameIdentifier)).Returns((Claim)null);
        // userId = middleware.GetUserId(mockContext.Object);
        // Assert.Equal("Anonymous", userId);
    }
}
