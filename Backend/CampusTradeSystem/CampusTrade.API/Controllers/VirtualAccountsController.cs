using System.Security.Claims;
using CampusTrade.API.Infrastructure.Extensions;
using CampusTrade.API.Models.DTOs.Common;
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
        private readonly ICreditHistoryRepository _creditHistoryRepository;
        private readonly ILogger<VirtualAccountsController> _logger;

        public VirtualAccountsController(
            IVirtualAccountsRepository virtualAccountRepository,
            ICreditHistoryRepository creditHistoryRepository,
            ILogger<VirtualAccountsController> logger)
        {
            _virtualAccountRepository = virtualAccountRepository;
            _creditHistoryRepository = creditHistoryRepository;
            _logger = logger;
        }

        /// <summary>
        /// 获取当前用户的虚拟账户余额
        /// </summary>
        /// <returns>账户余额信息</returns>
        [HttpGet("balance")]
        public async Task<ActionResult<ApiResponse<object>>> GetBalance()
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized(ApiResponse.CreateError("用户身份验证失败"));

                var balance = await _virtualAccountRepository.GetBalanceAsync(userId);
                var account = await _virtualAccountRepository.GetByUserIdAsync(userId);

                var result = new
                {
                    userId = userId,
                    balance = balance,
                    lastUpdateTime = account?.CreatedAt ?? DateTime.UtcNow
                };

                _logger.LogInformation("用户 {UserId} 查询余额: {Balance}", userId, balance);
                return Ok(ApiResponse<object>.CreateSuccess(result, "获取余额成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户余额时发生错误");
                return StatusCode(500, ApiResponse.CreateError("服务器内部错误"));
            }
        }

        /// <summary>
        /// 获取当前用户的虚拟账户详细信息
        /// </summary>
        /// <returns>账户详细信息</returns>
        [HttpGet("details")]
        public async Task<ActionResult<ApiResponse<object>>> GetAccountDetails()
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized(ApiResponse.CreateError("用户身份验证失败"));

                var account = await _virtualAccountRepository.GetByUserIdAsync(userId);
                if (account == null)
                {
                    return NotFound(ApiResponse.CreateError("虚拟账户不存在"));
                }

                var result = new
                {
                    accountId = account.AccountId,
                    userId = account.UserId,
                    balance = account.Balance,
                    createTime = account.CreatedAt
                };

                _logger.LogInformation("用户 {UserId} 查询账户详情", userId);
                return Ok(ApiResponse<object>.CreateSuccess(result, "获取账户详情成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取账户详情时发生错误");
                return StatusCode(500, ApiResponse.CreateError("服务器内部错误"));
            }
        }

        /// <summary>
        /// 检查余额是否充足
        /// </summary>
        /// <param name="amount">需要检查的金额</param>
        /// <returns>是否余额充足</returns>
        [HttpGet("check-balance")]
        public async Task<ActionResult<ApiResponse<object>>> CheckBalance([FromQuery] decimal amount)
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized(ApiResponse.CreateError("用户身份验证失败"));

                if (amount <= 0)
                    return BadRequest(ApiResponse.CreateError("检查金额必须大于0"));

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

                return Ok(ApiResponse<object>.CreateSuccess(result, "余额检查成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查余额时发生错误");
                return StatusCode(500, ApiResponse.CreateError("服务器内部错误"));
            }
        }

        /// <summary>
        /// 获取当前用户的信用评分变化历史
        /// </summary>
        /// <param name="days">获取最近几天的记录，默认30天</param>
        /// <returns>信用评分历史记录</returns>
        [HttpGet("credit-history")]
        public async Task<ActionResult<ApiResponse<object>>> GetCreditHistory([FromQuery] int days = 30)
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == 0)
                    return Unauthorized(ApiResponse.CreateError("用户身份验证失败"));

                if (days <= 0 || days > 365)
                    return BadRequest(ApiResponse.CreateError("天数必须在1-365之间"));

                // 获取用户的信用历史记录
                var creditHistories = await _creditHistoryRepository.GetByUserIdAsync(userId);

                // 过滤最近N天的记录
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                var recentHistories = creditHistories
                    .Where(h => h.CreatedAt >= cutoffDate)
                    .OrderBy(h => h.CreatedAt)
                    .ToList();

                // 如果没有历史记录，创建一个初始记录（假设初始信用分为60.0）
                object result;
                if (!recentHistories.Any())
                {
                    // 如果没有任何历史记录，返回当前信用分作为单点数据
                    result = new[]
                    {
                        new
                        {
                            date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                            score = 60.0m,
                            changeType = "初始分数"
                        }
                    };
                }
                else
                {
                    // 转换为前端需要的格式
                    result = recentHistories.Select(h => new
                    {
                        date = h.CreatedAt.ToString("yyyy-MM-dd"),
                        score = h.NewScore,
                        changeType = h.ChangeType
                    }).ToArray();
                }

                var recordCount = recentHistories.Any() ? recentHistories.Count : 1;
                _logger.LogInformation("用户 {UserId} 查询信用历史，返回 {Count} 条记录", userId, recordCount);
                return Ok(ApiResponse<object>.CreateSuccess(result, "获取信用历史成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取信用历史时发生错误");
                return StatusCode(500, ApiResponse.CreateError("服务器内部错误"));
            }
        }
    }
}
