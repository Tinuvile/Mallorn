using CampusTrade.API.Infrastructure.Extensions;
using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Models.DTOs.Exchange;
using CampusTrade.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusTrade.API.Controllers
{
    /// <summary>
    /// 换物控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExchangeController : ControllerBase
    {
        private readonly IExchangeService _exchangeService;
        private readonly ILogger<ExchangeController> _logger;

        public ExchangeController(IExchangeService exchangeService, ILogger<ExchangeController> logger)
        {
            _exchangeService = exchangeService;
            _logger = logger;
        }



        /// <summary>
        /// 创建换物请求
        /// </summary>
        /// <param name="exchangeRequest">换物请求DTO</param>
        /// <returns>创建结果</returns>
        [HttpPost("request")]
        public async Task<IActionResult> CreateExchangeRequest([FromBody] ExchangeRequestDto exchangeRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse.CreateError($"请求参数无效: {errors}"));
                }

                var userId = User.GetUserId();
                var (success, message, exchangeRequestId) = await _exchangeService.CreateExchangeRequestAsync(exchangeRequest, userId);

                if (success)
                {
                    return Ok(ApiResponse.CreateSuccess(new { exchangeRequestId }, message));
                }

                return BadRequest(ApiResponse.CreateError(message));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("换物请求认证失败: {Message}", ex.Message);
                return Unauthorized(ApiResponse.CreateError("认证失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建换物请求时发生错误");
                return StatusCode(500, ApiResponse.CreateError("系统内部错误"));
            }
        }

        /// <summary>
        /// 处理换物回应
        /// </summary>
        /// <param name="exchangeResponse">换物回应DTO</param>
        /// <returns>处理结果</returns>
        [HttpPost("response")]
        public async Task<IActionResult> HandleExchangeResponse([FromBody] ExchangeResponseDto exchangeResponse)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse.CreateError($"请求参数无效: {errors}"));
                }

                var userId = User.GetUserId();
                var (success, message) = await _exchangeService.HandleExchangeResponseAsync(exchangeResponse, userId);

                if (success)
                {
                    return Ok(ApiResponse.CreateSuccess(message));
                }

                return BadRequest(ApiResponse.CreateError(message));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("换物回应认证失败: {Message}", ex.Message);
                return Unauthorized(ApiResponse.CreateError("认证失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理换物回应时发生错误");
                return StatusCode(500, ApiResponse.CreateError("系统内部错误"));
            }
        }

        /// <summary>
        /// 获取我的换物请求记录
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>换物请求列表</returns>
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyExchangeRequests([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.GetUserId();
                var (exchangeRequests, totalCount) = await _exchangeService.GetUserExchangeRequestsAsync(userId, pageIndex, pageSize);

                var result = new
                {
                    exchangeRequests,
                    totalCount,
                    pageIndex,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(ApiResponse.CreateSuccess(result));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("获取换物请求记录认证失败: {Message}", ex.Message);
                return Unauthorized(ApiResponse.CreateError("认证失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取换物请求记录时发生错误");
                return StatusCode(500, ApiResponse.CreateError("系统内部错误"));
            }
        }

        /// <summary>
        /// 获取换物请求详情
        /// </summary>
        /// <param name="exchangeRequestId">换物请求ID</param>
        /// <returns>换物请求详情</returns>
        [HttpGet("{exchangeRequestId}")]
        public async Task<IActionResult> GetExchangeRequestDetails(int exchangeRequestId)
        {
            try
            {
                var userId = User.GetUserId();
                var exchangeRequest = await _exchangeService.GetExchangeRequestDetailsAsync(exchangeRequestId, userId);

                if (exchangeRequest == null)
                {
                    return NotFound(ApiResponse.CreateError("换物请求不存在或无权限访问"));
                }

                return Ok(ApiResponse.CreateSuccess(exchangeRequest));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("获取换物请求详情认证失败: {Message}", ex.Message);
                return Unauthorized(ApiResponse.CreateError("认证失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取换物请求详情时发生错误，请求ID: {ExchangeRequestId}", exchangeRequestId);
                return StatusCode(500, ApiResponse.CreateError("系统内部错误"));
            }
        }
    }
}
