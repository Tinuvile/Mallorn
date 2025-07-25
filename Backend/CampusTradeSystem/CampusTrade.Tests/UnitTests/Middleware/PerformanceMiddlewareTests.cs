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

namespace CampusTrade.API.Tests.Middleware;

public class PerformanceMiddlewareTests
{
    [Fact]
    public async Task PerformanceMiddleware_LogsRequestDuration()
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
                app.UseMiddleware<PerformanceMiddleware>();
                app.Run(async context =>
                {
                    // 模拟一个耗时请求
                    await Task.Delay(1500);
                    context.Response.StatusCode = 200;
                });
            }));

        var client = testServer.CreateClient();

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
            e.Properties.ContainsKey("ElapsedMs") &&
            e.Properties["ElapsedMs"].ToString().Contains("1500"));
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

        var middleware = new PerformanceMiddleware(_ => Task.CompletedTask);

        // Act
        var userId = middleware.GetUserId(mockContext.Object);

        // Assert
        Assert.Equal("123", userId);

        // Test anonymous case
        mockIdentity.Setup(i => i.FindFirst(ClaimTypes.NameIdentifier)).Returns((Claim)null);
        userId = middleware.GetUserId(mockContext.Object);
        Assert.Equal("Anonymous", userId);
    }
}
