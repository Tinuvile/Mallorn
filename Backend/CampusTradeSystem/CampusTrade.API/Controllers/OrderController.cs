using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusTrade.API.Models.DTOs;
using CampusTrade.API.Services.Interfaces;
using System.Security.Claims;

namespace CampusTrade.API.Controllers
{
    /// <summary>
    /// 订单管理控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("用户身份验证失败");
            }
            return userId;
        }

        /// <summary>
        /// 创建订单
        /// </summary>
        /// <param name="request">创建订单请求</param>
        /// <returns>订单详情</returns>
        [HttpPost]
        public async Task<ActionResult<OrderDetailResponse>> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _orderService.CreateOrderAsync(userId, request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建订单时发生错误");
                return StatusCode(500, new { message = "创建订单失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 获取订单详情
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>订单详情</returns>
        [HttpGet("{orderId}")]
        public async Task<ActionResult<OrderDetailResponse>> GetOrderDetail(int orderId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _orderService.GetOrderDetailAsync(orderId, userId);
                
                if (result == null)
                {
                    return NotFound(new { message = "订单不存在或无权访问" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取订单详情时发生错误，订单ID: {OrderId}", orderId);
                return StatusCode(500, new { message = "获取订单详情失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 获取用户订单列表
        /// </summary>
        /// <param name="role">用户角色（buyer/seller）</param>
        /// <param name="status">订单状态筛选</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <returns>订单列表</returns>
        [HttpGet]
        public async Task<ActionResult<object>> GetUserOrders(
            [FromQuery] string? role = null,
            [FromQuery] string? status = null,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                var (orders, totalCount) = await _orderService.GetUserOrdersAsync(userId, role, status, pageIndex, pageSize);
                
                return Ok(new
                {
                    orders,
                    totalCount,
                    pageIndex,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户订单列表时发生错误");
                return StatusCode(500, new { message = "获取订单列表失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 获取商品订单列表
        /// </summary>
        /// <param name="productId">商品ID</param>
        /// <returns>订单列表</returns>
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<List<OrderListResponse>>> GetProductOrders(int productId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _orderService.GetProductOrdersAsync(productId, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取商品订单列表时发生错误，商品ID: {ProductId}", productId);
                return StatusCode(500, new { message = "获取商品订单列表失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 获取用户订单统计
        /// </summary>
        /// <returns>订单统计信息</returns>
        [HttpGet("statistics")]
        public async Task<ActionResult<OrderStatisticsResponse>> GetUserOrderStatistics()
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _orderService.GetUserOrderStatisticsAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户订单统计时发生错误");
                return StatusCode(500, new { message = "获取订单统计失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 更新订单状态
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <param name="request">状态更新请求</param>
        /// <returns>操作结果</returns>
        [HttpPut("{orderId}/status")]
        public async Task<ActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _orderService.UpdateOrderStatusAsync(orderId, userId, request);
                
                if (!success)
                {
                    return BadRequest(new { message = "状态更新失败，请检查权限或状态转换是否合法" });
                }

                return Ok(new { message = "订单状态更新成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新订单状态时发生错误，订单ID: {OrderId}", orderId);
                return StatusCode(500, new { message = "更新订单状态失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 买家确认付款
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>操作结果</returns>
        [HttpPost("{orderId}/confirm-payment")]
        public async Task<ActionResult> ConfirmPayment(int orderId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _orderService.ConfirmPaymentAsync(orderId, userId);
                
                if (!success)
                {
                    return BadRequest(new { message = "确认付款失败，请检查订单状态或权限" });
                }

                return Ok(new { message = "付款确认成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确认付款时发生错误，订单ID: {OrderId}", orderId);
                return StatusCode(500, new { message = "确认付款失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 卖家发货
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <param name="request">发货信息</param>
        /// <returns>操作结果</returns>
        [HttpPost("{orderId}/ship")]
        public async Task<ActionResult> ShipOrder(int orderId, [FromBody] ShipOrderRequest? request = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _orderService.ShipOrderAsync(orderId, userId, request?.TrackingInfo);
                
                if (!success)
                {
                    return BadRequest(new { message = "发货失败，请检查订单状态或权限" });
                }

                return Ok(new { message = "发货成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发货时发生错误，订单ID: {OrderId}", orderId);
                return StatusCode(500, new { message = "发货失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 买家确认收货
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>操作结果</returns>
        [HttpPost("{orderId}/confirm-delivery")]
        public async Task<ActionResult> ConfirmDelivery(int orderId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _orderService.ConfirmDeliveryAsync(orderId, userId);
                
                if (!success)
                {
                    return BadRequest(new { message = "确认收货失败，请检查订单状态或权限" });
                }

                return Ok(new { message = "确认收货成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确认收货时发生错误，订单ID: {OrderId}", orderId);
                return StatusCode(500, new { message = "确认收货失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 完成订单
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>操作结果</returns>
        [HttpPost("{orderId}/complete")]
        public async Task<ActionResult> CompleteOrder(int orderId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _orderService.CompleteOrderAsync(orderId, userId);
                
                if (!success)
                {
                    return BadRequest(new { message = "完成订单失败，请检查订单状态或权限" });
                }

                return Ok(new { message = "订单已完成" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成订单时发生错误，订单ID: {OrderId}", orderId);
                return StatusCode(500, new { message = "完成订单失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 取消订单
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <param name="request">取消订单请求</param>
        /// <returns>操作结果</returns>
        [HttpPost("{orderId}/cancel")]
        public async Task<ActionResult> CancelOrder(int orderId, [FromBody] CancelOrderRequest? request = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _orderService.CancelOrderAsync(orderId, userId, request?.Reason);
                
                if (!success)
                {
                    return BadRequest(new { message = "取消订单失败，请检查订单状态或权限" });
                }

                return Ok(new { message = "订单已取消" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消订单时发生错误，订单ID: {OrderId}", orderId);
                return StatusCode(500, new { message = "取消订单失败，请稍后重试" });
            }
        }
    }

    /// <summary>
    /// 发货请求DTO
    /// </summary>
    public class ShipOrderRequest
    {
        /// <summary>
        /// 物流信息
        /// </summary>
        public string? TrackingInfo { get; set; }
    }

    /// <summary>
    /// 取消订单请求DTO
    /// </summary>
    public class CancelOrderRequest
    {
        /// <summary>
        /// 取消原因
        /// </summary>
        public string? Reason { get; set; }
    }
}