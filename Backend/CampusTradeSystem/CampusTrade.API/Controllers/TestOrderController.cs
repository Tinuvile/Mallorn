using Microsoft.AspNetCore.Mvc;
using CampusTrade.API.Services.Interfaces;
using CampusTrade.API.Models.DTOs;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Data;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Controllers
{
    /// <summary>
    /// 订单测试控制器（仅开发环境）
    /// </summary>
    [ApiController]
    [Route("api/test/[controller]")]
    public class TestOrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<TestOrderController> _logger;
        private readonly CampusTradeDbContext _context;

        public TestOrderController(
            IOrderService orderService, 
            ILogger<TestOrderController> logger,
            CampusTradeDbContext context)
        {
            _orderService = orderService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// 测试订单状态验证
        /// </summary>
        [HttpGet("status-transitions")]
        public ActionResult<object> TestStatusTransitions()
        {
            var testCases = new[]
            {
                new { From = "待付款", To = "已付款", Role = "buyer", Expected = true },
                new { From = "待付款", To = "已取消", Role = "buyer", Expected = true },
                new { From = "待付款", To = "已取消", Role = "seller", Expected = true },
                new { From = "已付款", To = "已发货", Role = "seller", Expected = true },
                new { From = "已付款", To = "已发货", Role = "buyer", Expected = false },
                new { From = "已发货", To = "已送达", Role = "buyer", Expected = true },
                new { From = "已发货", To = "已送达", Role = "seller", Expected = false },
                new { From = "已送达", To = "已完成", Role = "buyer", Expected = true },
                new { From = "已送达", To = "已完成", Role = "seller", Expected = true },
                new { From = "已完成", To = "已取消", Role = "buyer", Expected = false },
                new { From = "已取消", To = "已付款", Role = "buyer", Expected = false }
            };

            var results = testCases.Select(tc => new
            {
                tc.From,
                tc.To,
                tc.Role,
                tc.Expected,
                Actual = _orderService.IsValidStatusTransition(tc.From, tc.To, tc.Role),
                Result = _orderService.IsValidStatusTransition(tc.From, tc.To, tc.Role) == tc.Expected ? "PASS" : "FAIL"
            }).ToList();

            return Ok(new
            {
                message = "订单状态转换测试完成",
                totalTests = results.Count,
                passCount = results.Count(r => r.Result == "PASS"),
                failCount = results.Count(r => r.Result == "FAIL"),
                details = results
            });
        }

        /// <summary>
        /// 测试过期订单处理
        /// </summary>
        [HttpPost("process-expired")]
        public async Task<ActionResult<object>> TestProcessExpiredOrders()
        {
            try
            {
                var processedCount = await _orderService.ProcessExpiredOrdersAsync();
                return Ok(new
                {
                    message = "过期订单处理测试完成",
                    processedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试过期订单处理失败");
                return StatusCode(500, new { message = "测试失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取即将过期的订单（测试用）
        /// </summary>
        [HttpGet("expiring-orders")]
        public async Task<ActionResult<object>> GetExpiringOrders([FromQuery] int beforeMinutes = 30)
        {
            try
            {
                var orders = await _orderService.GetExpiringOrdersAsync(beforeMinutes);
                return Ok(new
                {
                    message = $"获取{beforeMinutes}分钟内即将过期的订单",
                    count = orders.Count,
                    orders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取即将过期订单失败");
                return StatusCode(500, new { message = "获取失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 创建测试订单数据（包括不同过期时间的订单）
        /// </summary>
        [HttpPost("create-test-orders")]
        public async Task<ActionResult<object>> CreateTestOrders([FromBody] CreateTestOrdersRequest request)
        {
            try
            {
                var createdOrders = new List<object>();

                // 1. 创建已过期的订单
                for (int i = 0; i < request.ExpiredOrderCount; i++)
                {
                    var expiredOrder = await CreateTestOrderWithExpireTimeAsync(
                        request.BuyerId, 
                        request.SellerId, 
                        request.ProductId,
                        DateTime.Now.AddMinutes(-request.ExpiredMinutes - (i * 10)), // 间隔10分钟
                        $"过期订单 #{i + 1} - 过期{request.ExpiredMinutes + (i * 10)}分钟"
                    );
                    createdOrders.Add(expiredOrder);
                }

                // 2. 创建即将过期的订单
                for (int i = 0; i < request.ExpiringOrderCount; i++)
                {
                    var expiringOrder = await CreateTestOrderWithExpireTimeAsync(
                        request.BuyerId,
                        request.SellerId,
                        request.ProductId,
                        DateTime.Now.AddMinutes(request.ExpiringMinutes - (i * 2)), // 间隔2分钟
                        $"即将过期订单 #{i + 1} - {request.ExpiringMinutes - (i * 2)}分钟后过期"
                    );
                    createdOrders.Add(expiringOrder);
                }

                // 3. 创建正常订单
                for (int i = 0; i < request.NormalOrderCount; i++)
                {
                    var normalOrder = await CreateTestOrderWithExpireTimeAsync(
                        request.BuyerId,
                        request.SellerId,
                        request.ProductId,
                        DateTime.Now.AddMinutes(30 + (i * 5)), // 30分钟后开始，间隔5分钟
                        $"正常订单 #{i + 1}"
                    );
                    createdOrders.Add(normalOrder);
                }

                return Ok(new
                {
                    message = "测试订单创建完成",
                    totalCreated = createdOrders.Count,
                    expiredOrders = request.ExpiredOrderCount,
                    expiringOrders = request.ExpiringOrderCount,
                    normalOrders = request.NormalOrderCount,
                    orders = createdOrders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建测试订单失败");
                return StatusCode(500, new { message = "创建测试订单失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 执行完整的过期订单测试流程
        /// </summary>
        [HttpPost("full-expiry-test")]
        public async Task<ActionResult<object>> RunFullExpiryTest([FromBody] FullExpiryTestRequest request)
        {
            try
            {
                var testResult = new
                {
                    StartTime = DateTime.Now,
                    Steps = new List<object>()
                };

                var steps = (List<object>)testResult.Steps;

                // 步骤1: 清理现有测试数据
                steps.Add(new { Step = 1, Action = "清理现有测试数据", Time = DateTime.Now });
                await CleanupTestDataAsync(request.BuyerId, request.SellerId);

                // 步骤2: 创建测试订单
                steps.Add(new { Step = 2, Action = "创建测试订单", Time = DateTime.Now });
                var createOrdersRequest = new CreateTestOrdersRequest
                {
                    BuyerId = request.BuyerId,
                    SellerId = request.SellerId,
                    ProductId = request.ProductId,
                    ExpiredOrderCount = request.ExpiredOrderCount,
                    ExpiringOrderCount = request.ExpiringOrderCount,
                    NormalOrderCount = request.NormalOrderCount,
                    ExpiredMinutes = request.ExpiredMinutes,
                    ExpiringMinutes = request.ExpiringMinutes
                };

                var createResult = await CreateTestOrders(createOrdersRequest);
                steps.Add(new { Step = 3, Action = "订单创建完成", Time = DateTime.Now, Result = createResult.Value });

                // 步骤3: 查询即将过期的订单
                steps.Add(new { Step = 4, Action = "查询即将过期的订单", Time = DateTime.Now });
                var expiringOrders = await _orderService.GetExpiringOrdersAsync(request.ExpiringMinutes + 5);
                steps.Add(new { Step = 5, Action = "即将过期订单查询完成", Time = DateTime.Now, Count = expiringOrders.Count });

                // 步骤4: 处理过期订单
                steps.Add(new { Step = 6, Action = "开始处理过期订单", Time = DateTime.Now });
                var processedCount = await _orderService.ProcessExpiredOrdersAsync();
                steps.Add(new { Step = 7, Action = "过期订单处理完成", Time = DateTime.Now, ProcessedCount = processedCount });

                // 步骤5: 验证结果
                steps.Add(new { Step = 8, Action = "验证处理结果", Time = DateTime.Now });
                var verificationResult = await VerifyExpiryTestResultAsync(request.BuyerId, request.SellerId);
                steps.Add(new { Step = 9, Action = "验证完成", Time = DateTime.Now, Result = verificationResult });

                return Ok(new
                {
                    message = "完整过期订单测试完成",
                    success = true,
                    testResult,
                    endTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完整过期订单测试失败");
                return StatusCode(500, new { message = "测试失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 查看当前所有测试订单状态
        /// </summary>
        [HttpGet("test-orders-status")]
        public async Task<ActionResult<object>> GetTestOrdersStatus([FromQuery] int buyerId, [FromQuery] int sellerId)
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.Product)
                    .Include(o => o.Buyer)
                    .Include(o => o.Seller)
                    .Where(o => o.BuyerId == buyerId || o.SellerId == sellerId)
                    .OrderBy(o => o.CreateTime)
                    .Select(o => new
                    {
                        o.OrderId,
                        o.Status,
                        o.CreateTime,
                        o.ExpireTime,
                        IsExpired = o.ExpireTime.HasValue && o.ExpireTime.Value < DateTime.Now,
                        RemainingMinutes = o.ExpireTime.HasValue 
                            ? (int)(o.ExpireTime.Value - DateTime.Now).TotalMinutes 
                            : (int?)null,
                        ProductTitle = o.Product != null ? o.Product.Title : "未知商品",
                        BuyerName = o.Buyer != null ? o.Buyer.Username : "未知买家",
                        SellerName = o.Seller != null ? o.Seller.Username : "未知卖家"
                    })
                    .ToListAsync();

                var summary = new
                {
                    TotalOrders = orders.Count,
                    PendingPayment = orders.Count(o => o.Status == Order.OrderStatus.PendingPayment),
                    Cancelled = orders.Count(o => o.Status == Order.OrderStatus.Cancelled),
                    Expired = orders.Count(o => o.IsExpired),
                    ExpiringIn10Minutes = orders.Count(o => o.RemainingMinutes.HasValue && o.RemainingMinutes > 0 && o.RemainingMinutes <= 10)
                };

                return Ok(new
                {
                    message = "测试订单状态查询完成",
                    summary,
                    orders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询测试订单状态失败");
                return StatusCode(500, new { message = "查询失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 清理测试数据
        /// </summary>
        [HttpDelete("cleanup-test-data")]
        public async Task<ActionResult<object>> CleanupTestData([FromQuery] int buyerId, [FromQuery] int sellerId)
        {
            try
            {
                var deletedCount = await CleanupTestDataAsync(buyerId, sellerId);
                return Ok(new
                {
                    message = "测试数据清理完成",
                    deletedOrderCount = deletedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理测试数据失败");
                return StatusCode(500, new { message = "清理失败", error = ex.Message });
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 创建指定过期时间的测试订单
        /// </summary>
        private async Task<object> CreateTestOrderWithExpireTimeAsync(
            int buyerId, int sellerId, int productId, DateTime expireTime, string remarks)
        {
            // 获取下一个订单ID
            var nextOrderId = await GetNextOrderIdAsync();

            // 直接创建订单，触发器会自动处理abstract_orders
            var order = new Order
            {
                OrderId = nextOrderId,
                BuyerId = buyerId,
                SellerId = sellerId,
                ProductId = productId,
                TotalAmount = 99.99m,
                Status = Order.OrderStatus.PendingPayment,
                CreateTime = DateTime.Now,
                ExpireTime = expireTime
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return new
            {
                OrderId = order.OrderId,
                Status = order.Status,
                CreateTime = order.CreateTime,
                ExpireTime = order.ExpireTime,
                IsExpired = expireTime < DateTime.Now,
                Remarks = remarks
            };
        }

        /// <summary>
        /// 获取下一个订单ID
        /// </summary>
        private async Task<int> GetNextOrderIdAsync()
        {
            using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT ABSTRACT_ORDER_SEQ.NEXTVAL FROM DUAL";
            await _context.Database.OpenConnectionAsync();
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// 清理测试数据
        /// </summary>
        private async Task<int> CleanupTestDataAsync(int buyerId, int sellerId)
        {
            var orders = await _context.Orders
                .Where(o => o.BuyerId == buyerId || o.SellerId == sellerId)
                .ToListAsync();

            var orderIds = orders.Select(o => o.OrderId).ToList();
            var abstractOrders = await _context.AbstractOrders
                .Where(ao => orderIds.Contains(ao.AbstractOrderId))
                .ToListAsync();

            _context.Orders.RemoveRange(orders);
            _context.AbstractOrders.RemoveRange(abstractOrders);

            await _context.SaveChangesAsync();
            return orders.Count;
        }

        /// <summary>
        /// 验证过期测试结果
        /// </summary>
        private async Task<object> VerifyExpiryTestResultAsync(int buyerId, int sellerId)
        {
            var orders = await _context.Orders
                .Where(o => o.BuyerId == buyerId || o.SellerId == sellerId)
                .ToListAsync();

            var now = DateTime.Now;
            var results = new
            {
                TotalOrders = orders.Count,
                ExpiredButNotCancelled = orders.Count(o => 
                    o.ExpireTime.HasValue && 
                    o.ExpireTime.Value < now && 
                    o.Status == Order.OrderStatus.PendingPayment),
                ExpiredAndCancelled = orders.Count(o => 
                    o.ExpireTime.HasValue && 
                    o.ExpireTime.Value < now && 
                    o.Status == Order.OrderStatus.Cancelled),
                NotExpiredButCancelled = orders.Count(o => 
                    (!o.ExpireTime.HasValue || o.ExpireTime.Value >= now) && 
                    o.Status == Order.OrderStatus.Cancelled),
                NotExpiredAndPending = orders.Count(o => 
                    (!o.ExpireTime.HasValue || o.ExpireTime.Value >= now) && 
                    o.Status == Order.OrderStatus.PendingPayment)
            };

            return results;
        }

        #endregion
    }

    /// <summary>
    /// 创建测试订单请求
    /// </summary>
    public class CreateTestOrdersRequest
    {
        /// <summary>
        /// 买家ID
        /// </summary>
        public int BuyerId { get; set; }

        /// <summary>
        /// 卖家ID
        /// </summary>
        public int SellerId { get; set; }

        /// <summary>
        /// 商品ID
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// 已过期订单数量
        /// </summary>
        public int ExpiredOrderCount { get; set; } = 3;

        /// <summary>
        /// 即将过期订单数量
        /// </summary>
        public int ExpiringOrderCount { get; set; } = 2;

        /// <summary>
        /// 正常订单数量
        /// </summary>
        public int NormalOrderCount { get; set; } = 2;

        /// <summary>
        /// 过期分钟数（过期订单）
        /// </summary>
        public int ExpiredMinutes { get; set; } = 60;

        /// <summary>
        /// 即将过期分钟数
        /// </summary>
        public int ExpiringMinutes { get; set; } = 15;
    }

    /// <summary>
    /// 完整过期测试请求
    /// </summary>
    public class FullExpiryTestRequest : CreateTestOrdersRequest
    {
        // 继承 CreateTestOrdersRequest 的所有属性
    }
}
