using CampusTrade.API.Models.DTOs.Exchange;
using CampusTrade.API.Services.Exchange;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CampusTrade.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExchangeController : ControllerBase
    {
        private readonly IExchangeService _exchangeService;

        // 构造函数，通过依赖注入获取 ExchangeService 实例
        public ExchangeController(IExchangeService exchangeService)
        {
            _exchangeService = exchangeService;
        }

        /// <summary>
        /// 用户发起换物请求
        /// </summary>
        /// <param name="exchangeRequest">换物请求DTO，包含交换商品ID和条件</param>
        /// <returns>换物请求结果</returns>
        [HttpPost("request")]
        public async Task<IActionResult> CreateExchangeRequest([FromBody] ExchangeRequestDto exchangeRequest)
        {
            // 检查请求参数是否有效
            if (!ModelState.IsValid)
            {
                return BadRequest("请求参数无效");
            }

            // 调用服务层方法处理换物请求
            var result = await _exchangeService.CreateExchangeRequest(exchangeRequest);
            if (result != null)
            {
                return Ok(new { message = "换物请求成功", data = result });
            }

            return BadRequest("换物请求失败");
        }

        /// <summary>
        /// 处理换物请求回应
        /// </summary>
        /// <param name="exchangeId">换物请求ID</param>
        /// <param name="status">回应状态（接受、拒绝、反报价）</param>
        /// <returns>换物请求结果</returns>
        [HttpPost("response/{exchangeId}")]
        public async Task<IActionResult> HandleExchangeResponse(int exchangeId, [FromBody] string status)
        {
            // 校验状态参数
            if (string.IsNullOrEmpty(status))
            {
                return BadRequest("状态不能为空");
            }

            // 调用服务层处理回应
            var result = await _exchangeService.HandleExchangeResponse(exchangeId, status);
            if (result)
            {
                return Ok("换物状态更新成功");
            }

            return BadRequest("换物处理失败");
        }
    }
}
