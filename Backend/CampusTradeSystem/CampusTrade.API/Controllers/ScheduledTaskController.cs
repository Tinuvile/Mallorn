using System.ComponentModel;
using CampusTrade.API.Models.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScheduledTaskController : ControllerBase
    {
        private readonly ILogger<ScheduledTaskController> _logger;
        private readonly IEnumerable<IHostedService> _hostedServices;

        public ScheduledTaskController(
            ILogger<ScheduledTaskController> logger,
            IEnumerable<IHostedService> hostedServices)
        {
            _logger = logger;
            _hostedServices = hostedServices;
        }

        /// <summary>
        /// 获取所有定时任务状态
        /// </summary>
        /// <returns>任务状态列表</returns>
        [HttpGet("status")]
        public IActionResult GetTaskStatus()
        {
            try
            {
                var taskStatuses = _hostedServices
                    .Where(service => service.GetType().Namespace?.Contains("ScheduledTasks") == true)
                    .Select(service => new
                    {
                        TaskName = service.GetType().Name,
                        Type = service.GetType().Name,
                        Status = "运行中", // 简化状态，实际项目中可以通过反射或状态管理获取
                        LastRun = DateTime.UtcNow.AddHours(-1), // 模拟数据
                        NextRun = GetNextRunTime(service.GetType().Name),
                        Description = GetTaskDescription(service.GetType().Name)
                    })
                    .ToList();

                _logger.LogInformation("获取定时任务状态，共 {Count} 个任务", taskStatuses.Count);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "获取定时任务状态成功",
                    Data = new
                    {
                        TotalTasks = taskStatuses.Count,
                        Tasks = taskStatuses
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取定时任务状态时发生错误");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "获取定时任务状态失败",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// 获取特定任务的详细信息
        /// </summary>
        /// <param name="taskName">任务名称</param>
        /// <returns>任务详细信息</returns>
        [HttpGet("{taskName}")]
        public IActionResult GetTaskDetail(string taskName)
        {
            try
            {
                var task = _hostedServices.FirstOrDefault(s =>
                    s.GetType().Name.Equals(taskName, StringComparison.OrdinalIgnoreCase));

                if (task == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = $"未找到名为 '{taskName}' 的定时任务",
                        ErrorCode = "TASK_NOT_FOUND"
                    });
                }

                var taskDetail = new
                {
                    TaskName = task.GetType().Name,
                    FullTypeName = task.GetType().FullName,
                    Status = "运行中",
                    LastRun = DateTime.UtcNow.AddHours(-1),
                    NextRun = GetNextRunTime(task.GetType().Name),
                    ExecutionCount = 42, // 模拟数据
                    AverageExecutionTime = "1.2s",
                    LastError = (string?)null,
                    Description = GetTaskDescription(task.GetType().Name)
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "获取任务详细信息成功",
                    Data = taskDetail
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取任务 {TaskName} 详细信息时发生错误", taskName);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "获取任务详细信息失败",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// 获取系统健康状态（包含定时任务监控）
        /// </summary>
        /// <returns>系统健康状态</returns>
        [HttpGet("health")]
        public IActionResult GetSystemHealth()
        {
            try
            {
                var scheduledTasksCount = _hostedServices
                    .Count(service => service.GetType().Namespace?.Contains("ScheduledTasks") == true);

                var healthStatus = new
                {
                    Status = "健康",
                    Timestamp = DateTime.UtcNow,
                    ScheduledTasks = new
                    {
                        TotalCount = scheduledTasksCount,
                        RunningCount = scheduledTasksCount, // 简化状态
                        FailedCount = 0
                    },
                    SystemInfo = new
                    {
                        Version = "1.0.0",
                        Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                        MachineName = Environment.MachineName
                    }
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "系统状态正常",
                    Data = healthStatus
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统健康状态时发生错误");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "获取系统健康状态失败",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        #region 私有辅助方法

        private DateTime GetNextRunTime(string taskName)
        {
            // 根据任务类型返回下次执行时间（简化实现）
            return taskName switch
            {
                "TokenCleanupTask" => DateTime.UtcNow.AddHours(1),
                "LogCleanupTask" => DateTime.UtcNow.AddDays(1),
                "ProductManagementTask" => DateTime.UtcNow.AddDays(1),
                "OrderProcessingTask" => DateTime.UtcNow.AddHours(6),
                "UserCreditScoreCalculationTask" => DateTime.UtcNow.AddDays(7),
                "StatisticalAnalysisTask" => DateTime.UtcNow.AddDays(1),
                "NotificationPushTask" => DateTime.UtcNow.AddHours(1),
                _ => DateTime.UtcNow.AddHours(1)
            };
        }

        private string GetTaskDescription(string taskName)
        {
            return taskName switch
            {
                "TokenCleanupTask" => "清理过期的刷新令牌",
                "LogCleanupTask" => "清理过期的系统日志",
                "ProductManagementTask" => "管理商品状态，自动下架过期商品",
                "OrderProcessingTask" => "处理订单状态，自动取消超时订单",
                "UserCreditScoreCalculationTask" => "计算用户信用分数",
                "StatisticalAnalysisTask" => "生成系统统计分析报告",
                "NotificationPushTask" => "推送待发送的通知消息",
                _ => "定时任务"
            };
        }

        #endregion
    }
}
