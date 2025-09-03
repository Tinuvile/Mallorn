using System.Security.Claims;
using CampusTrade.API.Infrastructure.Extensions;
using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Models.DTOs.Payment;
using CampusTrade.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusTrade.API.Controllers
{
    /// <summary>
    /// 充值管理控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RechargeController : ControllerBase
    {
        private readonly IRechargeService _rechargeService;
        private readonly ILogger<RechargeController> _logger;

        public RechargeController(IRechargeService rechargeService, ILogger<RechargeController> logger)
        {
            _rechargeService = rechargeService;
            _logger = logger;
        }

        /// <summary>
        /// 创建充值订单
        /// </summary>
        /// <param name="request">充值请求</param>
        /// <returns>充值订单信息</returns>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<RechargeResponse>>> CreateRecharge([FromBody] CreateRechargeRequest request)
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized(ApiResponse.CreateError("用户身份验证失败"));

                var result = await _rechargeService.CreateRechargeAsync(userId, request);

                _logger.LogInformation("用户 {UserId} 创建充值订单成功，金额 {Amount}，方式 {Method}",
                    userId, request.Amount, request.Method);

                return Ok(ApiResponse<RechargeResponse>.CreateSuccess(result, "充值订单创建成功"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("创建充值订单参数错误：{Message}", ex.Message);
                return BadRequest(ApiResponse.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建充值订单时发生错误");
                return StatusCode(500, ApiResponse.CreateError("服务器内部错误"));
            }
        }

        /// <summary>
        /// 完成模拟充值
        /// </summary>
        /// <param name="rechargeId">充值订单ID</param>
        /// <returns>是否成功</returns>
        [HttpPost("{rechargeId}/simulate-complete")]
        public async Task<ActionResult<ApiResponse<object>>> CompleteSimulationRecharge(int rechargeId)
        {
            _logger.LogInformation("收到完成模拟充值请求，充值ID: {RechargeId}", rechargeId);

            try
            {
                var userId = User.GetUserId();
                if (userId == 0)
                {
                    _logger.LogWarning("完成模拟充值失败：用户身份验证失败，充值ID: {RechargeId}", rechargeId);
                    return Unauthorized(ApiResponse.CreateError("用户身份验证失败"));
                }

                _logger.LogInformation("开始调用充值服务完成模拟充值，用户 {UserId}，充值ID: {RechargeId}", userId, rechargeId);
                var result = await _rechargeService.CompleteSimulationRechargeAsync(rechargeId, userId);

                if (result)
                {
                    _logger.LogInformation("用户 {UserId} 完成模拟充值 {RechargeId} 成功", userId, rechargeId);
                    return Ok(ApiResponse<object>.CreateSuccess(new { success = true }, "模拟充值完成"));
                }
                else
                {
                    _logger.LogWarning("用户 {UserId} 完成模拟充值 {RechargeId} 失败", userId, rechargeId);
                    return BadRequest(ApiResponse.CreateError("模拟充值失败"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成模拟充值时发生异常，充值ID: {RechargeId}，错误: {ErrorMessage}",
                    rechargeId, ex.Message);
                return StatusCode(500, ApiResponse.CreateError("服务器内部错误"));
            }
        }

        /// <summary>
        /// 获取用户充值记录
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <returns>充值记录列表</returns>
        [HttpGet("records")]
        public async Task<ActionResult<ApiResponse<object>>> GetUserRechargeRecords(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized(ApiResponse.CreateError("用户身份验证失败"));

                var (records, totalCount) = await _rechargeService.GetUserRechargeRecordsAsync(userId, pageIndex, pageSize);

                var result = new
                {
                    records = records,
                    totalCount = totalCount,
                    pageIndex = pageIndex,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(ApiResponse<object>.CreateSuccess(result, "获取充值记录成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户充值记录时发生错误");
                return StatusCode(500, ApiResponse.CreateError("服务器内部错误"));
            }
        }
    }
}
