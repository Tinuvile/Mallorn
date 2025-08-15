using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CampusTrade.API.Infrastructure.Utils.Notificate;
using CampusTrade.API.Services.Auth;
using CampusTrade.API.Services.Notification;
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
        /// 获取邮件发送统计监控
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>邮件发送统计</returns>
        [HttpGet("email-stats")]
        public async Task<IActionResult> GetEmailStats([FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime)
        {
            try
            {
                if (startTime.HasValue || endTime.HasValue)
                {
                    var emailStatsWithRate = await _senderService.GetEmailStatsAsync(startTime, endTime);
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            successRate = Math.Round(emailStatsWithRate.SuccessRate, 2),
                            totalSent = emailStatsWithRate.Total,
                            successful = emailStatsWithRate.Success,
                            failed = emailStatsWithRate.Failed,
                            pending = emailStatsWithRate.Pending,
                            period = new
                            {
                                startTime = startTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                                endTime = endTime?.ToString("yyyy-MM-dd HH:mm:ss")
                            }
                        },
                        message = $"邮件发送成功率: {emailStatsWithRate.SuccessRate:F2}%"
                    });
                }
                else
                {
                    var emailStats = await _senderService.GetEmailStatsAsync();
                    var successRate = emailStats.Total > 0 ? (double)emailStats.Success / emailStats.Total * 100 : 0;
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            successRate = Math.Round(successRate, 2),
                            totalSent = emailStats.Total,
                            successful = emailStats.Success,
                            failed = emailStats.Failed,
                            pending = emailStats.Pending,
                            period = new
                            {
                                startTime = (string?)null,
                                endTime = (string?)null
                            }
                        },
                        message = $"邮件发送成功率: {successRate:F2}%"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取邮件统计时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = "获取邮件统计失败，请稍后重试"
                });
            }
        }

        /// <summary>
        /// 获取SignalR发送统计监控
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>SignalR发送统计</returns>
        [HttpGet("signalr-stats")]
        public async Task<IActionResult> GetSignalRStats([FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime)
        {
            try
            {
                if (startTime.HasValue || endTime.HasValue)
                {
                    var signalRStatsWithRate = await _senderService.GetSignalRStatsAsync(startTime, endTime);
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            successRate = Math.Round(signalRStatsWithRate.SuccessRate, 2),
                            totalSent = signalRStatsWithRate.Total,
                            successful = signalRStatsWithRate.Success,
                            failed = signalRStatsWithRate.Failed,
                            pending = signalRStatsWithRate.Pending,
                            period = new
                            {
                                startTime = startTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                                endTime = endTime?.ToString("yyyy-MM-dd HH:mm:ss")
                            }
                        },
                        message = $"SignalR发送成功率: {signalRStatsWithRate.SuccessRate:F2}%"
                    });
                }
                else
                {
                    var signalRStats = await _senderService.GetSignalRStatsAsync();
                    var successRate = signalRStats.Total > 0 ? (double)signalRStats.Success / signalRStats.Total * 100 : 0;
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            successRate = Math.Round(successRate, 2),
                            totalSent = signalRStats.Total,
                            successful = signalRStats.Success,
                            failed = signalRStats.Failed,
                            pending = signalRStats.Pending,
                            period = new
                            {
                                startTime = (string?)null,
                                endTime = (string?)null
                            }
                        },
                        message = $"SignalR发送成功率: {successRate:F2}%"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取SignalR统计时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = "获取SignalR统计失败，请稍后重试"
                });
            }
        }



        /// <summary>
        /// 获取所有渠道的详细统计监控
        /// </summary>
        /// <returns>详细统计信息</returns>
        [HttpGet("detailed-stats")]
        public async Task<IActionResult> GetDetailedStats()
        {
            try
            {
                var detailedStats = await _senderService.GetDetailedStatsAsync();
                return Ok(new
                {
                    success = true,
                    data = detailedStats,
                    message = "获取详细统计成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取详细统计时发生异常");
                return StatusCode(500, new
                {
                    success = false,
                    message = "获取详细统计失败，请稍后重试"
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
        
        /// <summary>
        /// 生成系统优化建议
        /// </summary>
        private List<string> GenerateRecommendations(
            (int Pending, int Success, int Failed, int Total) queueStats,
            (int Pending, int Success, int Failed, int Total) emailStats,
            (int Pending, int Success, int Failed, int Total) signalRStats,
            double overallHealth)
        {
            var recommendations = new List<string>();

            // 基于队列积压情况
            if (queueStats.Pending > 100)
                recommendations.Add("⚠️ 通知队列积压严重，建议检查后台服务状态或增加处理频率");
            else if (queueStats.Pending > 50)
                recommendations.Add("📊 通知队列有一定积压，建议监控队列处理速度");

            // 基于邮件失败率
            var emailFailureRate = emailStats.Total > 0 ? (double)emailStats.Failed / emailStats.Total * 100 : 0;
            if (emailFailureRate > 20)
                recommendations.Add("📧 邮件发送失败率过高，建议检查SMTP配置和网络连接");
            else if (emailFailureRate > 10)
                recommendations.Add("📬 邮件发送失败率偏高，建议关注邮件服务状态");

            // 基于SignalR失败率
            var signalRFailureRate = signalRStats.Total > 0 ? (double)signalRStats.Failed / signalRStats.Total * 100 : 0;
            if (signalRFailureRate > 15)
                recommendations.Add("🔔 SignalR发送失败率过高，建议检查连接池和Hub配置");
            else if (signalRFailureRate > 8)
                recommendations.Add("📡 SignalR发送失败率偏高，建议监控连接状态");

            // 基于整体健康度
            if (overallHealth < 80)
                recommendations.Add("🚨 系统整体健康度偏低，建议进行全面检查和优化");
            else if (overallHealth < 90)
                recommendations.Add("⚡ 系统性能有提升空间，建议优化配置和监控关键指标");

            // 积极的建议
            if (recommendations.Count == 0)
                recommendations.Add("✅ 系统运行状态良好，继续保持当前配置");

            return recommendations;
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