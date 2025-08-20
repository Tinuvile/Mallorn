using CampusTrade.API.Data;
using CampusTrade.API.Infrastructure.Extensions;
using CampusTrade.API.Models.DTOs;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Controllers
{
    /// <summary>
    /// 订单超时测试控制器 - 专门用于测试过期订单处理机制
    /// </summary>
    [ApiController]
    [Route("api/test/timeout")]
    [AllowAnonymous] // 仅测试环境使用
    public class OrderTimeoutTestController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly CampusTradeDbContext _context;
        private readonly ILogger<OrderTimeoutTestController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public OrderTimeoutTestController(
            IOrderService orderService,
            CampusTradeDbContext context,
            ILogger<OrderTimeoutTestController> logger,
            IUnitOfWork unitOfWork)
        {
            _orderService = orderService;
            _context = context;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 创建批量测试订单，模拟不同的过期时间
        /// </summary>
        [HttpPost("create-batch-orders")]
        public async Task<IActionResult> CreateBatchOrdersAsync([FromBody] BatchOrderRequest request)
        {
            try
            {
                var results = new List<BatchOrderResult>();
                var baseTime = DateTime.Now;

                // 创建即将过期的订单（1分钟后过期）
                for (int i = 0; i < request.ImmediateExpiryCount; i++)
                {
                    var orderId = await CreateTestOrderWithCustomExpiry(
                        request.BuyerId + i,
                        request.SellerId,
                        request.ProductId,
                        baseTime.AddMinutes(1));

                    results.Add(new BatchOrderResult
                    {
                        OrderId = orderId,
                        ExpiryType = "即将过期(1分钟)",
                        ExpireTime = baseTime.AddMinutes(1)
                    });
                }

                // 创建短期过期的订单（3分钟后过期）
                for (int i = 0; i < request.ShortTermExpiryCount; i++)
                {
                    var orderId = await CreateTestOrderWithCustomExpiry(
                        request.BuyerId + request.ImmediateExpiryCount + i,
                        request.SellerId,
                        request.ProductId,
                        baseTime.AddMinutes(3));

                    results.Add(new BatchOrderResult
                    {
                        OrderId = orderId,
                        ExpiryType = "短期过期(3分钟)",
                        ExpireTime = baseTime.AddMinutes(3)
                    });
                }

                // 创建中期过期的订单（10分钟后过期）
                for (int i = 0; i < request.MediumTermExpiryCount; i++)
                {
                    var orderId = await CreateTestOrderWithCustomExpiry(
                        request.BuyerId + request.ImmediateExpiryCount + request.ShortTermExpiryCount + i,
                        request.SellerId,
                        request.ProductId,
                        baseTime.AddMinutes(10));

                    results.Add(new BatchOrderResult
                    {
                        OrderId = orderId,
                        ExpiryType = "中期过期(10分钟)",
                        ExpireTime = baseTime.AddMinutes(10)
                    });
                }

                // 创建已经过期的订单
                for (int i = 0; i < request.AlreadyExpiredCount; i++)
                {
                    var orderId = await CreateTestOrderWithCustomExpiry(
                        request.BuyerId + request.ImmediateExpiryCount + request.ShortTermExpiryCount + request.MediumTermExpiryCount + i,
                        request.SellerId,
                        request.ProductId,
                        baseTime.AddMinutes(-5)); // 5分钟前就过期了

                    results.Add(new BatchOrderResult
                    {
                        OrderId = orderId,
                        ExpiryType = "已过期",
                        ExpireTime = baseTime.AddMinutes(-5)
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = $"成功创建 {results.Count} 个测试订单",
                    CreatedOrders = results,
                    CreateTime = baseTime,
                    NextExpiry = results.Where(r => r.ExpireTime > baseTime).OrderBy(r => r.ExpireTime).FirstOrDefault()?.ExpireTime
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量创建测试订单失败");
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// 持续创建过期订单，模拟持续的订单流
        /// </summary>
        [HttpPost("continuous-order-creation")]
        public async Task<IActionResult> StartContinuousOrderCreationAsync([FromBody] ContinuousOrderRequest request)
        {
            try
            {
                var results = new List<BatchOrderResult>();
                var baseTime = DateTime.Now;

                // 创建一个持续的订单流，每个订单都有不同的过期时间
                for (int i = 0; i < request.TotalOrders; i++)
                {
                    // 随机分配过期时间（1-15分钟）
                    var random = new Random(i);
                    var expiryMinutes = random.Next(1, 16);
                    var expireTime = baseTime.AddMinutes(expiryMinutes);

                    var orderId = await CreateTestOrderWithCustomExpiry(
                        request.StartBuyerId + i,
                        request.SellerId,
                        request.ProductId,
                        expireTime);

                    results.Add(new BatchOrderResult
                    {
                        OrderId = orderId,
                        ExpiryType = $"随机过期({expiryMinutes}分钟)",
                        ExpireTime = expireTime
                    });

                    // 模拟订单创建的时间间隔
                    if (request.IntervalSeconds > 0 && i < request.TotalOrders - 1)
                    {
                        await Task.Delay(request.IntervalSeconds * 1000);
                    }
                }

                return Ok(new
                {
                    Success = true,
                    Message = $"持续创建了 {results.Count} 个测试订单",
                    Orders = results.OrderBy(r => r.ExpireTime),
                    StartTime = baseTime,
                    EndTime = DateTime.Now,
                    NextExpiries = results.Where(r => r.ExpireTime > DateTime.Now)
                        .OrderBy(r => r.ExpireTime)
                        .Take(5)
                        .Select(r => new { r.OrderId, r.ExpireTime, r.ExpiryType })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "持续创建订单失败");
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// 检查过期订单处理状态
        /// </summary>
        [HttpGet("check-expiry-status")]
        public async Task<IActionResult> CheckExpiryStatusAsync()
        {
            try
            {
                var currentTime = DateTime.Now;

                // 获取所有待付款订单
                var pendingOrders = await _context.Orders
                    .Where(o => o.Status == Order.OrderStatus.PendingPayment)
                    .Include(o => o.Buyer)
                    .Include(o => o.Product)
                    .ToListAsync();

                // 分类统计
                var expired = pendingOrders.Where(o => o.ExpireTime.HasValue && o.ExpireTime.Value < currentTime).ToList();
                var expiringSoon = pendingOrders.Where(o =>
                    o.ExpireTime.HasValue &&
                    o.ExpireTime.Value >= currentTime &&
                    o.ExpireTime.Value <= currentTime.AddMinutes(5)).ToList();
                var normal = pendingOrders.Where(o =>
                    o.ExpireTime.HasValue &&
                    o.ExpireTime.Value > currentTime.AddMinutes(5)).ToList();

                return Ok(new
                {
                    CurrentTime = currentTime,
                    TotalPendingOrders = pendingOrders.Count,
                    ExpiredOrders = new
                    {
                        Count = expired.Count,
                        Orders = expired.Select(o => new
                        {
                            o.OrderId,
                            o.BuyerId,
                            BuyerName = o.Buyer?.Username,
                            ProductTitle = o.Product?.Title,
                            o.ExpireTime,
                            ExpiredMinutes = currentTime.Subtract(o.ExpireTime!.Value).TotalMinutes
                        })
                    },
                    ExpiringSoonOrders = new
                    {
                        Count = expiringSoon.Count,
                        Orders = expiringSoon.Select(o => new
                        {
                            o.OrderId,
                            o.BuyerId,
                            BuyerName = o.Buyer?.Username,
                            ProductTitle = o.Product?.Title,
                            o.ExpireTime,
                            RemainingMinutes = o.ExpireTime!.Value.Subtract(currentTime).TotalMinutes
                        })
                    },
                    NormalOrders = new
                    {
                        Count = normal.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查过期状态失败");
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// 手动触发过期订单处理
        /// </summary>
        [HttpPost("process-expired")]
        public async Task<IActionResult> ProcessExpiredOrdersAsync()
        {
            try
            {
                var beforeCount = await GetExpiredOrderCount();
                var processedCount = await _orderService.ProcessExpiredOrdersAsync();
                var afterCount = await GetExpiredOrderCount();

                return Ok(new
                {
                    Success = true,
                    ProcessedCount = processedCount,
                    BeforeProcessing = beforeCount,
                    AfterProcessing = afterCount,
                    ProcessTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "手动处理过期订单失败");
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// 获取订单处理性能报告
        /// </summary>
        [HttpGet("performance-report")]
        public async Task<IActionResult> GetPerformanceReportAsync()
        {
            try
            {
                var startTime = DateTime.Now;

                // 测试查询性能
                var pendingOrders = await _context.Orders
                    .Where(o => o.Status == Order.OrderStatus.PendingPayment)
                    .CountAsync();

                var expiredOrders = await _context.Orders
                    .Where(o => o.Status == Order.OrderStatus.PendingPayment &&
                               o.ExpireTime.HasValue &&
                               o.ExpireTime.Value < DateTime.Now)
                    .CountAsync();

                var cancelledOrders = await _context.Orders
                    .Where(o => o.Status == Order.OrderStatus.Cancelled)
                    .CountAsync();

                var queryTime = DateTime.Now.Subtract(startTime).TotalMilliseconds;

                // 获取即将过期的订单
                var processStart = DateTime.Now;
                var expiringOrders = await _orderService.GetExpiringOrdersAsync(30);
                var processTime = DateTime.Now.Subtract(processStart).TotalMilliseconds;

                return Ok(new
                {
                    QueryPerformance = new
                    {
                        QueryTimeMs = queryTime,
                        PendingOrdersCount = pendingOrders,
                        ExpiredOrdersCount = expiredOrders,
                        CancelledOrdersCount = cancelledOrders
                    },
                    ProcessPerformance = new
                    {
                        ProcessTimeMs = processTime,
                        ExpiringOrdersCount = expiringOrders.Count
                    },
                    ReportTime = DateTime.Now,
                    DatabaseStatus = "连接正常"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取性能报告失败");
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// 清理所有测试数据
        /// </summary>
        [HttpDelete("cleanup-test-data")]
        public async Task<IActionResult> CleanupTestDataAsync()
        {
            try
            {
                // 删除测试用户创建的订单（用户ID大于10000的认为是测试数据）
                var testOrders = await _context.Orders
                    .Where(o => o.BuyerId >= 10000 || o.SellerId >= 10000)
                    .ToListAsync();

                var testAbstractOrders = await _context.AbstractOrders
                    .Where(ao => testOrders.Select(o => o.OrderId).Contains(ao.AbstractOrderId))
                    .ToListAsync();

                _context.Orders.RemoveRange(testOrders);
                _context.AbstractOrders.RemoveRange(testAbstractOrders);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    DeletedOrders = testOrders.Count,
                    DeletedAbstractOrders = testAbstractOrders.Count,
                    CleanupTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理测试数据失败");
                return BadRequest(new { Error = ex.Message });
            }
        }

        #region 私有方法

        private async Task<int> CreateTestOrderWithCustomExpiry(int buyerId, int sellerId, int productId, DateTime expireTime)
        {
            // 获取下一个订单ID - 让触发器处理abstract_orders
            var nextOrderId = await GetNextOrderIdAsync();

            // 直接创建订单，触发器会自动处理abstract_orders
            var order = new Order
            {
                OrderId = nextOrderId,
                BuyerId = buyerId,
                SellerId = sellerId,
                ProductId = productId,
                TotalAmount = 100.00m,
                FinalPrice = 100.00m,
                Status = Order.OrderStatus.PendingPayment,
                CreateTime = DateTime.Now,
                ExpireTime = expireTime
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return order.OrderId;
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

        private async Task<int> GetExpiredOrderCount()
        {
            return await _context.Orders
                .Where(o => o.Status == Order.OrderStatus.PendingPayment &&
                           o.ExpireTime.HasValue &&
                           o.ExpireTime.Value < DateTime.Now)
                .CountAsync();
        }

        #endregion
    }

    #region 请求和响应模型

    public class BatchOrderRequest
    {
        public int BuyerId { get; set; } = 10001;
        public int SellerId { get; set; } = 10002;
        public int ProductId { get; set; } = 2001;
        public int ImmediateExpiryCount { get; set; } = 3;
        public int ShortTermExpiryCount { get; set; } = 5;
        public int MediumTermExpiryCount { get; set; } = 5;
        public int AlreadyExpiredCount { get; set; } = 2;
    }

    public class ContinuousOrderRequest
    {
        public int StartBuyerId { get; set; } = 20001;
        public int SellerId { get; set; } = 20002;
        public int ProductId { get; set; } = 2001;
        public int TotalOrders { get; set; } = 20;
        public int IntervalSeconds { get; set; } = 2; // 每2秒创建一个订单
    }

    public class BatchOrderResult
    {
        public int OrderId { get; set; }
        public string ExpiryType { get; set; } = "";
        public DateTime ExpireTime { get; set; }
    }

    #endregion
}
