using CampusTrade.API.Models.DTOs;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CampusTrade.API.Controllers
{
    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        private readonly ICreditService _creditService;

        public TestController(ICreditService creditService) // 如果你使用接口，这里用 ICreditService

        {
            _creditService = creditService;
        }

        /// <summary>
        /// 测试信用加分接口
        /// 模拟交易完成，为用户加分
        /// </summary>
        [HttpPost("credit")]
        public async Task<IActionResult> TestCredit()
        {
            var testEvent = new CreditEvent
            {
                UserId = 2352491, // 替换为数据库中已存在的用户ID
                EventType = CreditEventType.TransactionCompleted,
                Description = "测试订单完成信用加分"
            };

            await _creditService.ApplyCreditChangeAsync(testEvent);

            return Ok("信用加分已执行（模拟订单完成）");
        }
    }
}
