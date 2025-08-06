using CampusTrade.API.Models.DTOs.Order;
using CampusTrade.API.Models.DTOs.Payment;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.Order
{
    /// <summary>
    /// 订单服务实现
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IRepository<Models.Entities.Product> _productRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<AbstractOrder> _abstractOrderRepository;
        private readonly IVirtualAccountsRepository _virtualAccountRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrderService> _logger;

        // 订单超时时间（分钟）
        private const int ORDER_TIMEOUT_MINUTES = 30;

        public OrderService(
            IOrderRepository orderRepository,
            IRepository<Models.Entities.Product> productRepository,
            IRepository<User> userRepository,
            IRepository<AbstractOrder> abstractOrderRepository,
            IVirtualAccountsRepository virtualAccountRepository,
            IUnitOfWork unitOfWork,
            ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _userRepository = userRepository;
            _abstractOrderRepository = abstractOrderRepository;
            _virtualAccountRepository = virtualAccountRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #region 订单创建
        public async Task<OrderDetailResponse> CreateOrderAsync(int userId, CreateOrderRequest request)
        {
            _logger.LogInformation("用户 {UserId} 开始创建订单，商品ID: {ProductId}", userId, request.ProductId);

            // 1. 验证商品是否存在且可购买
            var product = await _productRepository.GetByPrimaryKeyAsync(request.ProductId);
            if (product == null)
                throw new ArgumentException("商品不存在");

            if (product.Status != Models.Entities.Product.ProductStatus.OnSale)
                throw new InvalidOperationException("商品不可购买");

            if (product.UserId == userId)
                throw new InvalidOperationException("不能购买自己的商品");

            // 2. 检查是否已有未完成的订单
            var existingOrders = await _orderRepository.GetByBuyerIdAsync(userId);
            var pendingOrder = existingOrders.FirstOrDefault(o =>
                o.ProductId == request.ProductId &&
                (o.Status == Models.Entities.Order.OrderStatus.PendingPayment ||
                 o.Status == Models.Entities.Order.OrderStatus.Paid));

            if (pendingOrder != null)
                throw new InvalidOperationException("该商品已有未完成的订单");

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // 3. 获取下一个订单ID
                var nextOrderId = await GetNextOrderIdAsync();

                // 4. 直接创建订单，触发器会自动处理abstract_orders
                var order = new Models.Entities.Order
                {
                    OrderId = nextOrderId,
                    BuyerId = userId,
                    SellerId = product.UserId,
                    ProductId = request.ProductId,
                    TotalAmount = request.FinalPrice ?? product.BasePrice,
                    FinalPrice = request.FinalPrice,
                    Status = Models.Entities.Order.OrderStatus.PendingPayment,
                    CreateTime = DateTime.Now,
                    ExpireTime = DateTime.Now.AddMinutes(ORDER_TIMEOUT_MINUTES)
                };

                await _orderRepository.AddAsync(order);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("订单创建成功，订单ID: {OrderId}", order.OrderId);

                // 5. 返回订单详情
                return await GetOrderDetailAsync(order.OrderId, userId)
                    ?? throw new InvalidOperationException("订单创建失败");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "创建订单失败");
                throw;
            }
        }
        #endregion

        #region 订单查询
        public async Task<OrderDetailResponse?> GetOrderDetailAsync(int orderId, int userId)
        {
            var order = await _orderRepository.GetOrderWithDetailsAsync(orderId);
            if (order == null) return null;

            // 权限检查
            if (order.BuyerId != userId && order.SellerId != userId)
                return null;

            return new OrderDetailResponse
            {
                OrderId = order.OrderId,
                BuyerId = order.BuyerId,
                Buyer = order.Buyer != null ? new UserBriefInfo
                {
                    UserId = order.Buyer.UserId,
                    Username = order.Buyer.Username ?? "",
                    Nickname = order.Buyer.FullName,
                    AvatarUrl = null, // User实体没有头像字段
                    CreditScore = order.Buyer.CreditScore
                } : null,
                SellerId = order.SellerId,
                Seller = order.Seller != null ? new UserBriefInfo
                {
                    UserId = order.Seller.UserId,
                    Username = order.Seller.Username ?? "",
                    Nickname = order.Seller.FullName,
                    AvatarUrl = null, // User实体没有头像字段
                    CreditScore = order.Seller.CreditScore
                } : null,
                ProductId = order.ProductId,
                Product = order.Product != null ? new ProductBriefInfo
                {
                    ProductId = order.Product.ProductId,
                    Title = order.Product.Title,
                    Price = order.Product.BasePrice,
                    MainImageUrl = order.Product.ProductImages?.FirstOrDefault()?.ImageUrl,
                    Status = order.Product.Status
                } : null,
                TotalAmount = order.TotalAmount,
                FinalPrice = order.FinalPrice,
                Status = order.Status,
                CreateTime = order.CreateTime,
                ExpireTime = order.ExpireTime
            };
        }

        public async Task<(List<OrderListResponse> Orders, int TotalCount)> GetUserOrdersAsync(
            int userId, string? role = null, string? status = null, int pageIndex = 1, int pageSize = 10)
        {
            IEnumerable<Models.Entities.Order> orders;

            if (role == "buyer")
            {
                orders = await _orderRepository.GetByBuyerIdAsync(userId);
            }
            else if (role == "seller")
            {
                orders = await _orderRepository.GetBySellerIdAsync(userId);
            }
            else
            {
                // 获取用户所有订单（买家和卖家）
                var buyerOrders = await _orderRepository.GetByBuyerIdAsync(userId);
                var sellerOrders = await _orderRepository.GetBySellerIdAsync(userId);
                orders = buyerOrders.Union(sellerOrders);
            }

            // 状态筛选
            if (!string.IsNullOrEmpty(status))
            {
                orders = orders.Where(o => o.Status == status);
            }

            var totalCount = orders.Count();
            var pagedOrders = orders
                .OrderByDescending(o => o.CreateTime)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var orderResponses = pagedOrders.Select(order => new OrderListResponse
            {
                OrderId = order.OrderId,
                Product = order.Product != null ? new ProductBriefInfo
                {
                    ProductId = order.Product.ProductId,
                    Title = order.Product.Title,
                    Price = order.Product.BasePrice,
                    MainImageUrl = order.Product.ProductImages?.FirstOrDefault()?.ImageUrl,
                    Status = order.Product.Status
                } : null,
                OtherUser = order.BuyerId == userId
                    ? (order.Seller != null ? new UserBriefInfo
                    {
                        UserId = order.Seller.UserId,
                        Username = order.Seller.Username ?? "",
                        Nickname = order.Seller.FullName,
                        AvatarUrl = null,
                        CreditScore = order.Seller.CreditScore
                    } : null)
                    : (order.Buyer != null ? new UserBriefInfo
                    {
                        UserId = order.Buyer.UserId,
                        Username = order.Buyer.Username ?? "",
                        Nickname = order.Buyer.FullName,
                        AvatarUrl = null,
                        CreditScore = order.Buyer.CreditScore
                    } : null),
                TotalAmount = order.TotalAmount,
                FinalPrice = order.FinalPrice,
                Status = order.Status,
                CreateTime = order.CreateTime,
                UserRole = order.BuyerId == userId ? "buyer" : "seller",
                IsExpired = order.ExpireTime.HasValue && order.ExpireTime.Value < DateTime.Now
            }).ToList();

            return (orderResponses, totalCount);
        }

        public async Task<List<OrderListResponse>> GetProductOrdersAsync(int productId, int userId)
        {
            var orders = await _orderRepository.GetByProductIdAsync(productId);

            // 只有商品的卖家可以查看所有订单
            var product = await _productRepository.GetByPrimaryKeyAsync(productId);
            if (product?.UserId != userId)
            {
                // 买家只能看到自己的订单
                orders = orders.Where(o => o.BuyerId == userId);
            }

            return orders.Select(order => new OrderListResponse
            {
                OrderId = order.OrderId,
                Product = order.Product != null ? new ProductBriefInfo
                {
                    ProductId = order.Product.ProductId,
                    Title = order.Product.Title,
                    Price = order.Product.BasePrice,
                    MainImageUrl = order.Product.ProductImages?.FirstOrDefault()?.ImageUrl,
                    Status = order.Product.Status
                } : null,
                OtherUser = order.BuyerId == userId
                    ? (order.Seller != null ? new UserBriefInfo
                    {
                        UserId = order.Seller.UserId,
                        Username = order.Seller.Username ?? "",
                        Nickname = order.Seller.FullName,
                        AvatarUrl = null,
                        CreditScore = order.Seller.CreditScore
                    } : null)
                    : (order.Buyer != null ? new UserBriefInfo
                    {
                        UserId = order.Buyer.UserId,
                        Username = order.Buyer.Username ?? "",
                        Nickname = order.Buyer.FullName,
                        AvatarUrl = null,
                        CreditScore = order.Buyer.CreditScore
                    } : null),
                TotalAmount = order.TotalAmount,
                FinalPrice = order.FinalPrice,
                Status = order.Status,
                CreateTime = order.CreateTime,
                UserRole = order.BuyerId == userId ? "buyer" : "seller",
                IsExpired = order.ExpireTime.HasValue && order.ExpireTime.Value < DateTime.Now
            }).OrderByDescending(o => o.CreateTime).ToList();
        }

        public async Task<OrderStatisticsResponse> GetUserOrderStatisticsAsync(int userId)
        {
            var buyerOrders = await _orderRepository.GetByBuyerIdAsync(userId);
            var sellerOrders = await _orderRepository.GetBySellerIdAsync(userId);
            var allOrders = buyerOrders.Union(sellerOrders).ToList();

            var currentMonth = DateTime.Now.Date.AddDays(1 - DateTime.Now.Day);
            var monthlyOrders = allOrders.Where(o => o.CreateTime >= currentMonth).ToList();

            return new OrderStatisticsResponse
            {
                TotalOrders = allOrders.Count,
                PendingPaymentOrders = allOrders.Count(o => o.Status == Models.Entities.Order.OrderStatus.PendingPayment),
                PaidOrders = allOrders.Count(o => o.Status == Models.Entities.Order.OrderStatus.Paid),
                ShippedOrders = allOrders.Count(o => o.Status == Models.Entities.Order.OrderStatus.Shipped),
                CompletedOrders = allOrders.Count(o => o.Status == Models.Entities.Order.OrderStatus.Completed),
                CancelledOrders = allOrders.Count(o => o.Status == Models.Entities.Order.OrderStatus.Cancelled),
                TotalAmount = allOrders.Where(o => o.TotalAmount.HasValue).Sum(o => o.TotalAmount!.Value),
                MonthlyOrders = monthlyOrders.Count,
                MonthlyAmount = monthlyOrders.Where(o => o.TotalAmount.HasValue).Sum(o => o.TotalAmount!.Value)
            };
        }
        #endregion

        #region 订单状态管理
        public async Task<bool> UpdateOrderStatusAsync(int orderId, int userId, UpdateOrderStatusRequest request)
        {
            var order = await _orderRepository.GetOrderWithDetailsAsync(orderId);
            if (order == null) return false;

            // 权限检查
            if (order.BuyerId != userId && order.SellerId != userId)
                return false;

            var userRole = order.BuyerId == userId ? "buyer" : "seller";

            // 验证状态转换是否合法
            if (!IsValidStatusTransition(order.Status, request.Status, userRole))
                return false;

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // 更新订单状态
                var success = await _orderRepository.UpdateOrderStatusAsync(orderId, request.Status);
                if (!success)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return false;
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("订单 {OrderId} 状态已更新为 {Status}，操作用户: {UserId}",
                    orderId, request.Status, userId);

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "更新订单状态失败，订单ID: {OrderId}", orderId);
                return false;
            }
        }

        public async Task<bool> ConfirmPaymentAsync(int orderId, int userId)
        {
            return await UpdateOrderStatusAsync(orderId, userId, new UpdateOrderStatusRequest
            {
                Status = Models.Entities.Order.OrderStatus.Paid,
                Remarks = "买家确认付款"
            });
        }

        public async Task<bool> ShipOrderAsync(int orderId, int userId, string? trackingInfo = null)
        {
            return await UpdateOrderStatusAsync(orderId, userId, new UpdateOrderStatusRequest
            {
                Status = Models.Entities.Order.OrderStatus.Shipped,
                Remarks = string.IsNullOrEmpty(trackingInfo) ? "卖家已发货" : $"卖家已发货，物流信息: {trackingInfo}"
            });
        }

        public async Task<bool> ConfirmDeliveryAsync(int orderId, int userId)
        {
            return await UpdateOrderStatusAsync(orderId, userId, new UpdateOrderStatusRequest
            {
                Status = Models.Entities.Order.OrderStatus.Delivered,
                Remarks = "买家确认收货"
            });
        }

        public async Task<bool> CompleteOrderAsync(int orderId, int userId)
        {
            var order = await _orderRepository.GetOrderWithDetailsAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning("完成订单：订单不存在，订单ID: {OrderId}", orderId);
                return false;
            }

            // 检查权限（买家或卖家都可以完成订单）
            if (order.BuyerId != userId && order.SellerId != userId)
            {
                _logger.LogWarning("完成订单：无权限操作订单 {OrderId}，用户 {UserId}", orderId, userId);
                return false;
            }

            if (order.Status != Models.Entities.Order.OrderStatus.Delivered)
            {
                _logger.LogWarning("完成订单：订单状态不正确，订单 {OrderId}，当前状态 {Status}",
                    orderId, order.Status);
                return false;
            }

            var transferAmount = order.FinalPrice ?? order.TotalAmount ?? 0;
            if (transferAmount <= 0)
            {
                _logger.LogWarning("完成订单：金额异常，订单 {OrderId}，金额 {Amount}", orderId, transferAmount);
                return false;
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // 1. 将资金转给卖家
                var creditSuccess = await _virtualAccountRepository.CreditAsync(order.SellerId, transferAmount,
                    $"订单收款 {orderId} - 商品: {order.Product?.Title}");

                if (!creditSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError("完成订单：给卖家转账失败，订单 {OrderId}", orderId);
                    return false;
                }

                // 2. 更新订单状态为已完成
                var statusUpdateSuccess = await _orderRepository.UpdateOrderStatusAsync(orderId,
                    Models.Entities.Order.OrderStatus.Completed);

                if (!statusUpdateSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError("完成订单：更新订单状态失败，订单 {OrderId}", orderId);
                    return false;
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("订单 {OrderId} 完成并转账成功，卖家 {SellerId} 收到 {Amount}",
                    orderId, order.SellerId, transferAmount);

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "完成订单失败，订单ID: {OrderId}", orderId);
                return false;
            }
        }

        public async Task<bool> CancelOrderAsync(int orderId, int userId, string? reason = null)
        {
            return await UpdateOrderStatusAsync(orderId, userId, new UpdateOrderStatusRequest
            {
                Status = Models.Entities.Order.OrderStatus.Cancelled,
                Remarks = string.IsNullOrEmpty(reason) ? "订单已取消" : $"订单已取消: {reason}"
            });
        }
        #endregion

        #region 订单超时管理
        public async Task<int> ProcessExpiredOrdersAsync()
        {
            var startTime = DateTime.Now;
            _logger.LogInformation("开始处理过期订单，开始时间: {StartTime}", startTime);

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // 获取所有过期订单
                var expiredOrders = await _orderRepository.GetExpiredOrdersAsync();
                var expiredOrdersList = expiredOrders.ToList();

                _logger.LogInformation("找到 {Count} 个过期订单需要处理", expiredOrdersList.Count);

                var processedCount = 0;
                var failedCount = 0;

                foreach (var order in expiredOrdersList)
                {
                    try
                    {
                        var success = await _orderRepository.UpdateOrderStatusAsync(
                            order.OrderId, Models.Entities.Order.OrderStatus.Cancelled);

                        if (success)
                        {
                            processedCount++;
                            _logger.LogDebug("订单 {OrderId} 因超时被自动取消，买家: {BuyerId}, 过期时间: {ExpireTime}",
                                order.OrderId, order.BuyerId, order.ExpireTime);
                        }
                        else
                        {
                            failedCount++;
                            _logger.LogWarning("订单 {OrderId} 状态更新失败", order.OrderId);
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        _logger.LogError(ex, "处理过期订单 {OrderId} 时发生异常", order.OrderId);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var duration = DateTime.Now.Subtract(startTime);
                _logger.LogInformation("处理过期订单完成，耗时: {Duration}ms, 成功处理: {ProcessedCount}, 失败: {FailedCount}",
                    duration.TotalMilliseconds, processedCount, failedCount);

                return processedCount;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "处理过期订单时发生严重错误");
                throw;
            }
        }

        public async Task<List<OrderDetailResponse>> GetExpiringOrdersAsync(int beforeMinutes = 30)
        {
            var cutoffTime = DateTime.Now.AddMinutes(beforeMinutes);
            _logger.LogDebug("查询即将在 {CutoffTime} 之前过期的订单", cutoffTime);

            try
            {
                // 直接查询即将过期的待付款订单
                var expiringOrders = await _orderRepository.GetExpiringOrdersAsync(cutoffTime);

                var result = new List<OrderDetailResponse>();
                foreach (var order in expiringOrders)
                {
                    var detail = await GetOrderDetailAsync(order.OrderId, order.BuyerId);
                    if (detail != null)
                    {
                        result.Add(detail);
                    }
                }

                _logger.LogInformation("找到 {Count} 个即将过期的订单", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询即将过期订单时发生错误");
                throw;
            }
        }
        #endregion

        #region 订单支付
        public async Task<PaymentResult> PayOrderWithVirtualAccountAsync(int orderId, int userId)
        {
            _logger.LogInformation("用户 {UserId} 开始支付订单 {OrderId}", userId, orderId);

            var order = await _orderRepository.GetOrderWithDetailsAsync(orderId);
            if (order == null)
            {
                return new PaymentResult
                {
                    Success = false,
                    Message = "订单不存在"
                };
            }

            if (order.BuyerId != userId)
            {
                return new PaymentResult
                {
                    Success = false,
                    Message = "无权限支付此订单"
                };
            }

            if (order.Status != Models.Entities.Order.OrderStatus.PendingPayment)
            {
                return new PaymentResult
                {
                    Success = false,
                    Message = $"订单状态不允许支付，当前状态：{order.Status}"
                };
            }

            // 检查订单是否已过期
            if (order.ExpireTime.HasValue && order.ExpireTime.Value < DateTime.UtcNow)
            {
                return new PaymentResult
                {
                    Success = false,
                    Message = "订单已过期"
                };
            }

            var paymentAmount = order.FinalPrice ?? order.TotalAmount ?? 0;
            if (paymentAmount <= 0)
            {
                return new PaymentResult
                {
                    Success = false,
                    Message = "订单金额异常"
                };
            }

            // 检查虚拟账户余额
            var hasBalance = await _virtualAccountRepository.HasSufficientBalanceAsync(userId, paymentAmount);
            if (!hasBalance)
            {
                var currentBalance = await _virtualAccountRepository.GetBalanceAsync(userId);
                return new PaymentResult
                {
                    Success = false,
                    Message = "余额不足，请先充值",
                    RemainingBalance = currentBalance
                };
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // 1. 扣除买家余额
                var debitSuccess = await _virtualAccountRepository.DebitAsync(userId, paymentAmount,
                    $"支付订单 {orderId} - 商品: {order.Product?.Title}");

                if (!debitSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "扣款失败，请重试"
                    };
                }

                // 2. 更新订单状态为已付款
                var statusUpdateSuccess = await _orderRepository.UpdateOrderStatusAsync(orderId,
                    Models.Entities.Order.OrderStatus.Paid);

                if (!statusUpdateSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "更新订单状态失败"
                    };
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var remainingBalance = await _virtualAccountRepository.GetBalanceAsync(userId);

                _logger.LogInformation("订单 {OrderId} 支付成功，用户 {UserId}，金额 {Amount}，余额 {Balance}",
                    orderId, userId, paymentAmount, remainingBalance);

                return new PaymentResult
                {
                    Success = true,
                    Message = "支付成功",
                    Amount = paymentAmount,
                    RemainingBalance = remainingBalance,
                    PaymentTime = DateTime.UtcNow,
                    TransactionId = $"PAY_{orderId}_{DateTime.UtcNow:yyyyMMddHHmmss}"
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "订单 {OrderId} 支付失败，用户 {UserId}", orderId, userId);

                return new PaymentResult
                {
                    Success = false,
                    Message = "支付过程中发生错误，请重试"
                };
            }
        }
        #endregion

        #region 订单验证
        public bool IsValidStatusTransition(string currentStatus, string newStatus, string userRole)
        {
            // 定义状态转换规则
            var transitions = new Dictionary<string, Dictionary<string, string[]>>
            {
                [Models.Entities.Order.OrderStatus.PendingPayment] = new Dictionary<string, string[]>
                {
                    ["buyer"] = new[] { Models.Entities.Order.OrderStatus.Paid, Models.Entities.Order.OrderStatus.Cancelled },
                    ["seller"] = new[] { Models.Entities.Order.OrderStatus.Cancelled }
                },
                [Models.Entities.Order.OrderStatus.Paid] = new Dictionary<string, string[]>
                {
                    ["buyer"] = new[] { Models.Entities.Order.OrderStatus.Cancelled },
                    ["seller"] = new[] { Models.Entities.Order.OrderStatus.Shipped, Models.Entities.Order.OrderStatus.Cancelled }
                },
                [Models.Entities.Order.OrderStatus.Shipped] = new Dictionary<string, string[]>
                {
                    ["buyer"] = new[] { Models.Entities.Order.OrderStatus.Delivered },
                    ["seller"] = new string[0] // 卖家发货后不能再改变状态
                },
                [Models.Entities.Order.OrderStatus.Delivered] = new Dictionary<string, string[]>
                {
                    ["buyer"] = new[] { Models.Entities.Order.OrderStatus.Completed },
                    ["seller"] = new[] { Models.Entities.Order.OrderStatus.Completed }
                },
                [Models.Entities.Order.OrderStatus.Completed] = new Dictionary<string, string[]>
                {
                    ["buyer"] = new string[0],
                    ["seller"] = new string[0]
                },
                [Models.Entities.Order.OrderStatus.Cancelled] = new Dictionary<string, string[]>
                {
                    ["buyer"] = new string[0],
                    ["seller"] = new string[0]
                }
            };

            if (!transitions.ContainsKey(currentStatus))
                return false;

            if (!transitions[currentStatus].ContainsKey(userRole))
                return false;

            return transitions[currentStatus][userRole].Contains(newStatus);
        }

        public async Task<bool> HasOrderPermissionAsync(int orderId, int userId)
        {
            var order = await _orderRepository.GetByPrimaryKeyAsync(orderId);
            return order != null && (order.BuyerId == userId || order.SellerId == userId);
        }

        /// <summary>
        /// 获取下一个订单ID
        /// </summary>
        private async Task<int> GetNextOrderIdAsync()
        {
            try
            {
                // 使用原生SQL查询获取序列值
                var sql = "SELECT ABSTRACT_ORDER_SEQ.NEXTVAL AS Value FROM DUAL";
                var results = await _unitOfWork.ExecuteQueryAsync<SequenceValue>(sql);
                return Convert.ToInt32(results.First().Value);
            }
            catch (Exception)
            {
                // 测试环境使用时间戳生成ID
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var random = new Random().Next(1000, 9999);
                return Convert.ToInt32(timestamp % 1000000) * 10000 + random;
            }
        }

        /// <summary>
        /// 序列值包装类
        /// </summary>
        private class SequenceValue
        {
            public decimal Value { get; set; }
        }
        #endregion
    }
}
