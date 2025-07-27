using CampusTrade.API.Models.DTOs.Bargain;
using CampusTrade.API.Services.Bargain;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CampusTrade.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BargainController : ControllerBase
    {
        private readonly IBargainService _bargainService;

        // 构造函数，通过依赖注入获取 BargainService 实例
        public BargainController(IBargainService bargainService)
        {
            _bargainService = bargainService;
        }

        /// <summary>
        /// 买家发起议价请求
        /// </summary>
        /// <param name="bargainRequest">议价请求DTO，包含订单ID和议价价格</param>
        /// <returns>议价结果</returns>
        [HttpPost("request")]
        public async Task<IActionResult> CreateBargainRequest([FromBody] BargainRequestDto bargainRequest)
        {
            // 检查请求参数是否有效
            if (!ModelState.IsValid)
            {
                return BadRequest("请求参数无效");
            }

            // 调用服务层方法处理议价请求
            var result = await _bargainService.CreateBargainRequest(bargainRequest);
            if (result != null)
            {
                return Ok(new { message = "议价请求成功", data = result });
            }

            return BadRequest("议价请求失败");
        }

        /// <summary>
        /// 卖家处理议价
        /// </summary>
        /// <param name="negotiationId">议价记录ID</param>
        /// <param name="status">操作状态（接受、拒绝、反报价）</param>
        /// <returns>议价状态更新结果</returns>
        [HttpPost("response/{negotiationId}")]
        public async Task<IActionResult> HandleBargainResponse(int negotiationId, [FromBody] string status)
        {
            // 校验状态参数
            if (string.IsNullOrEmpty(status))
            {
                return BadRequest("状态不能为空");
            }

            // 调用服务层处理卖家回应
            var result = await _bargainService.HandleBargainResponse(negotiationId, status);
            if (result)
            {
                return Ok("议价状态更新成功");
            }

            return BadRequest("议价处理失败");
        }
    }
}
