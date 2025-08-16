using System.Security.Claims;
using CampusTrade.API.Infrastructure.Extensions;
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
        public async Task<ActionResult<RechargeResponse>> CreateRecharge([FromBody] CreateRechargeRequest request)
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized("用户身份验证失败");

                var result = await _rechargeService.CreateRechargeAsync(userId, request);

                _logger.LogInformation("用户 {UserId} 创建充值订单成功，金额 {Amount}，方式 {Method}",
                    userId, request.Amount, request.Method);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("创建充值订单参数错误：{Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建充值订单时发生错误");
                return StatusCode(500, "服务器内部错误");
            }
        }

        /// <summary>
        /// 完成模拟充值
        /// </summary>
        /// <param name="rechargeId">充值订单ID</param>
        /// <returns>是否成功</returns>
        [HttpPost("{rechargeId}/simulate-complete")]
        public async Task<ActionResult<bool>> CompleteSimulationRecharge(int rechargeId)
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized("用户身份验证失败");

                var result = await _rechargeService.CompleteSimulationRechargeAsync(rechargeId, userId);

                if (result)
                {
                    _logger.LogInformation("用户 {UserId} 完成模拟充值 {RechargeId}", userId, rechargeId);
                    return Ok(new { success = true, message = "模拟充值完成" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "模拟充值失败" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成模拟充值时发生错误，充值ID: {RechargeId}", rechargeId);
                return StatusCode(500, "服务器内部错误");
            }
        }

        /// <summary>
        /// 获取用户充值记录
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <returns>充值记录列表</returns>
        [HttpGet("records")]
        public async Task<ActionResult<object>> GetUserRechargeRecords(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized("用户身份验证失败");

                var (records, totalCount) = await _rechargeService.GetUserRechargeRecordsAsync(userId, pageIndex, pageSize);

                return Ok(new
                {
                    records = records,
                    totalCount = totalCount,
                    pageIndex = pageIndex,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户充值记录时发生错误");
                return StatusCode(500, "服务器内部错误");
            }
        }
    }
}
