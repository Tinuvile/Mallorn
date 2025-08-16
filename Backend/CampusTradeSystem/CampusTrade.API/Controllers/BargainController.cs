using CampusTrade.API.Infrastructure.Extensions;
using CampusTrade.API.Models.DTOs.Bargain;
using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusTrade.API.Controllers
{
    /// <summary>
    /// 议价控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BargainController : ControllerBase
    {
        private readonly IBargainService _bargainService;
        private readonly ILogger<BargainController> _logger;

        public BargainController(IBargainService bargainService, ILogger<BargainController> logger)
        {
            _bargainService = bargainService;
            _logger = logger;
        }



        /// <summary>
        /// 创建议价请求
        /// </summary>
        /// <param name="bargainRequest">议价请求DTO</param>
        /// <returns>创建结果</returns>
        [HttpPost("request")]
        public async Task<IActionResult> CreateBargainRequest([FromBody] BargainRequestDto bargainRequest)
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
                var (success, message, negotiationId) = await _bargainService.CreateBargainRequestAsync(bargainRequest, userId);

                if (success)
                {
                    return Ok(ApiResponse.CreateSuccess(new { negotiationId }, message));
                }

                return BadRequest(ApiResponse.CreateError(message));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("议价请求认证失败: {Message}", ex.Message);
                return Unauthorized(ApiResponse.CreateError("认证失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建议价请求时发生错误");
                return StatusCode(500, ApiResponse.CreateError("系统内部错误"));
            }
        }

        /// <summary>
        /// 处理议价回应
        /// </summary>
        /// <param name="bargainResponse">议价回应DTO</param>
        /// <returns>处理结果</returns>
        [HttpPost("response")]
        public async Task<IActionResult> HandleBargainResponse([FromBody] BargainResponseDto bargainResponse)
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
                var (success, message) = await _bargainService.HandleBargainResponseAsync(bargainResponse, userId);

                if (success)
                {
                    return Ok(ApiResponse.CreateSuccess(message));
                }

                return BadRequest(ApiResponse.CreateError(message));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("议价回应认证失败: {Message}", ex.Message);
                return Unauthorized(ApiResponse.CreateError("认证失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理议价回应时发生错误");
                return StatusCode(500, ApiResponse.CreateError("系统内部错误"));
            }
        }

        /// <summary>
        /// 获取我的议价记录
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>议价记录列表</returns>
        [HttpGet("my-negotiations")]
        public async Task<IActionResult> GetMyNegotiations([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.GetUserId();
                var (negotiations, totalCount) = await _bargainService.GetUserNegotiationsAsync(userId, pageIndex, pageSize);

                var result = new
                {
                    negotiations,
                    totalCount,
                    pageIndex,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(ApiResponse.CreateSuccess(result));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("获取议价记录认证失败: {Message}", ex.Message);
                return Unauthorized(ApiResponse.CreateError("认证失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取议价记录时发生错误");
                return StatusCode(500, ApiResponse.CreateError("系统内部错误"));
            }
        }

        /// <summary>
        /// 获取议价详情
        /// </summary>
        /// <param name="negotiationId">议价ID</param>
        /// <returns>议价详情</returns>
        [HttpGet("{negotiationId}")]
        public async Task<IActionResult> GetNegotiationDetails(int negotiationId)
        {
            try
            {
                var userId = User.GetUserId();
                var negotiation = await _bargainService.GetNegotiationDetailsAsync(negotiationId, userId);

                if (negotiation == null)
                {
                    return NotFound(ApiResponse.CreateError("议价记录不存在或无权限访问"));
                }

                return Ok(ApiResponse.CreateSuccess(negotiation));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("获取议价详情认证失败: {Message}", ex.Message);
                return Unauthorized(ApiResponse.CreateError("认证失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取议价详情时发生错误，议价ID: {NegotiationId}", negotiationId);
                return StatusCode(500, ApiResponse.CreateError("系统内部错误"));
            }
        }
    }
}
