using System.Security.Claims;
using CampusTrade.API.Infrastructure.Extensions;
using CampusTrade.API.Models.DTOs.VirtualAccount;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusTrade.API.Controllers
{
    /// <summary>
    /// 虚拟账户管理控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VirtualAccountsController : ControllerBase
    {
        private readonly IVirtualAccountsRepository _virtualAccountRepository;
        private readonly ILogger<VirtualAccountsController> _logger;

        public VirtualAccountsController(
            IVirtualAccountsRepository virtualAccountRepository,
            ILogger<VirtualAccountsController> logger)
        {
            _virtualAccountRepository = virtualAccountRepository;
            _logger = logger;
        }

        /// <summary>
        /// 获取当前用户的虚拟账户余额
        /// </summary>
        /// <returns>账户余额信息</returns>
        [HttpGet("balance")]
        public async Task<ActionResult<object>> GetBalance()
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized("用户身份验证失败");

                var balance = await _virtualAccountRepository.GetBalanceAsync(userId);
                var account = await _virtualAccountRepository.GetByUserIdAsync(userId);

                var result = new
                {
                    userId = userId,
                    balance = balance,
                    lastUpdateTime = account?.CreatedAt ?? DateTime.UtcNow
                };

                _logger.LogInformation("用户 {UserId} 查询余额: {Balance}", userId, balance);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户余额时发生错误");
                return StatusCode(500, "服务器内部错误");
            }
        }

        /// <summary>
        /// 获取当前用户的虚拟账户详细信息
        /// </summary>
        /// <returns>账户详细信息</returns>
        [HttpGet("details")]
        public async Task<ActionResult<object>> GetAccountDetails()
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized("用户身份验证失败");

                var account = await _virtualAccountRepository.GetByUserIdAsync(userId);
                if (account == null)
                {
                    return NotFound("虚拟账户不存在");
                }

                var result = new
                {
                    accountId = account.AccountId,
                    userId = account.UserId,
                    balance = account.Balance,
                    createTime = account.CreatedAt
                };

                _logger.LogInformation("用户 {UserId} 查询账户详情", userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取账户详情时发生错误");
                return StatusCode(500, "服务器内部错误");
            }
        }

        /// <summary>
        /// 检查余额是否充足
        /// </summary>
        /// <param name="amount">需要检查的金额</param>
        /// <returns>是否余额充足</returns>
        [HttpGet("check-balance")]
        public async Task<ActionResult<object>> CheckBalance([FromQuery] decimal amount)
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized("用户身份验证失败");

                if (amount <= 0)
                    return BadRequest("检查金额必须大于0");

                var hasSufficientBalance = await _virtualAccountRepository.HasSufficientBalanceAsync(userId, amount);
                var currentBalance = await _virtualAccountRepository.GetBalanceAsync(userId);

                var result = new
                {
                    userId = userId,
                    requestAmount = amount,
                    currentBalance = currentBalance,
                    hasSufficientBalance = hasSufficientBalance
                };

                _logger.LogInformation("用户 {UserId} 检查余额，需要 {Amount}，当前 {Balance}，充足: {Sufficient}",
                    userId, amount, currentBalance, hasSufficientBalance);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查余额时发生错误");
                return StatusCode(500, "服务器内部错误");
            }
        }
    }
}
