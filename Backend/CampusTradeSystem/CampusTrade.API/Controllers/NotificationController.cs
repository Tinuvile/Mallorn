using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CampusTrade.API.Infrastructure.Utils.Notificate;
using CampusTrade.API.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Controllers
{
    /// <summary>
    /// 通知控制器 - 用于测试通知系统
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly NotifiService _notifiService;
        private readonly NotifiSenderService _senderService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            NotifiService notifiService,
            NotifiSenderService senderService,
            ILogger<NotificationController> logger)
        {
            _notifiService = notifiService;
            _senderService = senderService;
            _logger = logger;
        }

        /// <summary>
        /// 测试创建通知
        /// </summary>
        /// <param name="request">通知创建请求</param>
        /// <returns>创建结果</returns>
        [HttpPost("create")]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest request)
        {
            try
            {
                var result = await _notifiService.CreateNotificationAsync(
                    request.RecipientId,
                    request.TemplateId,
                    request.Parameters,
                    request.OrderId
                );

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        notificationId = result.NotificationId
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建通知时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = "系统异常，请稍后重试"
                });
            }
        }

        /// <summary>
        /// 获取通知队列状态
        /// </summary>
        /// <returns>队列状态统计</returns>
        [HttpGet("queue-stats")]
        public async Task<IActionResult> GetQueueStats()
        {
            try
            {
                var stats = await _senderService.GetQueueStatsAsync();
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        pending = stats.Pending,
                        success = stats.Success,
                        failed = stats.Failed,
                        total = stats.Total
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取队列状态时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = "系统异常，请稍后重试"
                });
            }
        }

        /// <summary>
        /// 手动触发队列处理
        /// </summary>
        /// <returns>处理结果</returns>
        [HttpPost("process-queue")]
        public async Task<IActionResult> ProcessQueue()
        {
            try
            {
                var result = await _senderService.ProcessNotificationQueueAsync(10);
                return Ok(new
                {
                    success = true,
                    message = "队列处理完成",
                    data = new
                    {
                        total = result.Total,
                        success = result.Success,
                        failed = result.Failed
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理队列时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = "系统异常，请稍后重试"
                });
            }
        }

        /// <summary>
        /// 获取用户通知历史
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="pageIndex">页索引</param>
        /// <returns>通知历史</returns>
        [HttpGet("user/{userId}/history")]
        public async Task<IActionResult> GetUserNotifications(
            int userId,
            [FromQuery] int pageSize = 10,
            [FromQuery] int pageIndex = 0)
        {
            try
            {
                var notifications = await _notifiService.GetUserNotificationsAsync(userId, pageSize, pageIndex);
                return Ok(new
                {
                    success = true,
                    data = notifications.Select(n => new
                    {
                        notificationId = n.NotificationId,
                        templateName = n.Template?.TemplateName,
                        status = n.SendStatus,
                        createdAt = n.CreatedAt,
                        sentAt = n.SentAt,
                        retryCount = n.RetryCount,
                        content = n.GetRenderedContent()
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户通知历史时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = "系统异常，请稍后重试"
                });
            }
        }
    }

    /// <summary>
    /// 创建通知请求模型
    /// </summary>
    public class CreateNotificationRequest
    {
        public int RecipientId { get; set; }
        public int TemplateId { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public int? OrderId { get; set; }
    }
}
