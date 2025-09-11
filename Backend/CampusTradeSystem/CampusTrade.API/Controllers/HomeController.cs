using CampusTrade.API.Models.DTOs.Common;
using Microsoft.AspNetCore.Mvc;

namespace CampusTrade.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HomeController : ControllerBase
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 获取API状态信息
    /// </summary>
    /// <returns>API状态</returns>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(ApiResponse<object>.CreateSuccess(new
        {
            service = "CampusTrade API",
            version = "1.0.0",
            status = "运行中",
            timestamp = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        }, "API运行正常"));
    }

    /// <summary>
    /// 获取健康检查信息
    /// </summary>
    /// <returns>健康状态</returns>
    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        try
        {
            // 这里可以添加更多的健康检查逻辑
            // 例如：数据库连接检查、Redis连接检查等

            return Ok(ApiResponse<object>.CreateSuccess(new
            {
                status = "healthy",
                checks = new
                {
                    api = "healthy",
                    database = "healthy", // 这里可以实际检查数据库连接
                    memory = "healthy"
                },
                timestamp = DateTime.UtcNow
            }, "系统健康状态良好"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "健康检查失败");
            return StatusCode(503, ApiResponse.CreateError("服务不可用", "SERVICE_UNAVAILABLE"));
        }
    }

    /// <summary>
    /// 获取API信息
    /// </summary>
    /// <returns>API信息</returns>
    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        return Ok(ApiResponse<object>.CreateSuccess(new
        {
            name = "Campus Trade API",
            description = "校园交易平台后端API",
            version = "1.0.0",
            documentation = "/swagger",
            endpoints = new
            {
                auth = "/api/auth",
                token = "/api/token",
                status = "/api/home/status",
                health = "/api/home/health"
            },
            features = new[]
            {
                "JWT认证",
                "Token刷新",
                "用户管理",
                "学生身份验证",
                "统一异常处理",
                "CORS支持"
            }
        }, "API信息获取成功"));
    }
}
