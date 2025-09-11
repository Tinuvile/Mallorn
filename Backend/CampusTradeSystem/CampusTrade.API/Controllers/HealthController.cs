using CampusTrade.API.Models.DTOs.Common;
using Microsoft.AspNetCore.Mvc;

namespace CampusTrade.API.Controllers
{
    /// <summary>
    /// 健康检查控制器
    /// </summary>
    [ApiController]
    [Route("")]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// 健康检查端点
        /// </summary>
        /// <returns>健康状态</returns>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(ApiResponse<object>.CreateSuccess(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            }, "API服务正常运行"));
        }
    }
}
