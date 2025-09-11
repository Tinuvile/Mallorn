using CampusTrade.API.infrastructure.Utils;
using CampusTrade.API.Models.DTOs.Bargain;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Interfaces;
using CampusTrade.API.Services.Notification;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.Bargain
{
    /// <summary>
    /// 议价服务实现
    /// </summary>
    public class BargainService : IBargainService
    {
        private readonly INegotiationsRepository _negotiationsRepository;
        private readonly IRepository<Models.Entities.Order> _ordersRepository;
        private readonly IRepository<Models.Entities.Product> _productsRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BargainService> _logger;
        private readonly NotifiService _notificationService;

        public BargainService(
            INegotiationsRepository negotiationsRepository,
            IRepository<Models.Entities.Order> ordersRepository,
            IRepository<Models.Entities.Product> productsRepository,
            IUnitOfWork unitOfWork,
            ILogger<BargainService> logger,
            NotifiService notificationService)
        {
            _negotiationsRepository = negotiationsRepository;
            _ordersRepository = ordersRepository;
            _productsRepository = productsRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _notificationService = notificationService;
        }

        /// <summary>
        /// 创建议价请求
        /// </summary>
        public async Task<(bool Success, string Message, int? NegotiationId)> CreateBargainRequestAsync(BargainRequestDto bargainRequest, int userId)
        {
            _logger.LogInformation("用户 {UserId} 对订单 {OrderId} 发起议价请求，价格：{Price}",
                userId, bargainRequest.OrderId, bargainRequest.ProposedPrice);

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // 1. 验证订单是否存在且可议价
                var order = await _ordersRepository.GetByPrimaryKeyAsync(bargainRequest.OrderId);
                if (order == null)
                {
                    return (false, "订单不存在", null);
                }

                if (order.SellerId == userId)
                {
                    return (false, "不能对自己的商品发起议价", null);
                }

                if (order.Status != Models.Entities.Order.OrderStatus.PendingPayment)
                {
                    return (false, "订单状态不允许议价", null);
                }

                // 2. 检查是否已存在未完成的议价
                var hasActiveNegotiation = await _negotiationsRepository.HasActiveNegotiationAsync(bargainRequest.OrderId);
                if (hasActiveNegotiation)
                {
                    return (false, "已存在未完成的议价请求", null);
                }

                // 3. 创建议价记录
                var negotiation = new Negotiation
                {
                    OrderId = bargainRequest.OrderId,
                    ProposedPrice = bargainRequest.ProposedPrice,
                    Status = "等待回应",
                    CreatedAt = TimeHelper.Now
                };

                await _negotiationsRepository.AddAsync(negotiation);

                // 4. 将订单状态设置为"议价中"
                order.Status = Models.Entities.Order.OrderStatus.Negotiating;
                _ordersRepository.Update(order);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                // 发送议价请求通知给卖家
                try
                {
                    // 获取订单关联的商品信息
                    var product = await _productsRepository.GetByPrimaryKeyAsync(order.ProductId);
                    if (product != null)
                    {
                        // 通知卖家 - 模板ID为14（收到议价请求模板）
                        var notificationParams = new Dictionary<string, object>
                        {
                            ["productTitle"] = product.Title,
                            ["proposedPrice"] = bargainRequest.ProposedPrice.ToString("F2"),
                            ["originalPrice"] = (order.TotalAmount ?? product.BasePrice).ToString("F2")
                        };

                        await _notificationService.CreateNotificationAsync(
                            order.SellerId,
                            14, // 收到议价请求模板ID
                            notificationParams,
                            negotiation.NegotiationId
                        );

                        _logger.LogInformation("议价请求通知已发送，议价ID: {NegotiationId}，买家ID: {BuyerId}，卖家ID: {SellerId}，商品: {ProductTitle}，议价: ￥{ProposedPrice}，原价: ￥{OriginalPrice}",
                            negotiation.NegotiationId, userId, order.SellerId, product.Title, bargainRequest.ProposedPrice, order.TotalAmount ?? product.BasePrice);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "发送议价请求通知失败，议价ID: {NegotiationId}", negotiation.NegotiationId);
                    // 注意：通知发送失败不应该影响议价请求创建结果，所以这里只记录日志
                }

                _logger.LogInformation("议价请求创建成功，议价ID：{NegotiationId}", negotiation.NegotiationId);
                return (true, "议价请求已发送", negotiation.NegotiationId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "创建议价请求失败，用户：{UserId}，订单：{OrderId}", userId, bargainRequest.OrderId);
                return (false, "系统错误，请稍后重试", null);
            }
        }

        /// <summary>
        /// 处理议价回应
        /// </summary>
        public async Task<(bool Success, string Message)> HandleBargainResponseAsync(BargainResponseDto bargainResponse, int userId)
        {
            _logger.LogInformation("用户 {UserId} 回应议价 {NegotiationId}，状态：{Status}",
                userId, bargainResponse.NegotiationId, bargainResponse.Status);

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // 1. 获取议价记录 - 如果传递的记录状态不是"等待回应"，尝试找到对应的等待回应记录
                var negotiation = await _negotiationsRepository.GetByPrimaryKeyAsync(bargainResponse.NegotiationId);
                if (negotiation == null)
                {
                    return (false, "议价记录不存在");
                }

                // 如果当前记录状态不是"等待回应"，尝试找到同订单的最新"等待回应"记录
                if (negotiation.Status != "等待回应")
                {
                    var latestNegotiation = await _negotiationsRepository.GetLatestNegotiationAsync(negotiation.OrderId);
                    if (latestNegotiation != null && latestNegotiation.Status == "等待回应")
                    {
                        negotiation = latestNegotiation;
                        _logger.LogInformation("自动切换到最新的等待回应记录，议价ID: {NegotiationId}", negotiation.NegotiationId);
                    }
                    else
                    {
                        return (false, "议价状态不允许回应");
                    }
                }

                // 通过订单获取买卖双方信息验证权限
                var order = await _ordersRepository.GetByPrimaryKeyAsync(negotiation.OrderId);
                if (order == null)
                {
                    return (false, "订单不存在");
                }

                // 验证用户是否为买家或卖家
                if (order.BuyerId != userId && order.SellerId != userId)
                {
                    return (false, "无权限操作此议价");
                }

                // 根据议价流程验证具体权限：
                // 需要分析当前议价是第几轮，来判断谁有权限回应
                var allNegotiations = await _negotiationsRepository.GetByOrderIdAsync(negotiation.OrderId);
                var orderedNegotiations = allNegotiations.OrderBy(n => n.CreatedAt).ToList();
                var currentIndex = orderedNegotiations.FindIndex(n => n.NegotiationId == negotiation.NegotiationId);

                // 第1轮（index=0）：买家发起，卖家回应
                // 第2轮（index=1）：卖家反报价，买家回应  
                // 第3轮（index=2）：买家再反报价，卖家回应
                // 以此类推：偶数轮买家发起卖家回应，奇数轮卖家发起买家回应
                bool shouldBeSellerResponse = (currentIndex % 2 == 0); // 偶数轮卖家回应
                bool isSellerUser = (order.SellerId == userId);

                if (shouldBeSellerResponse != isSellerUser)
                {
                    return (false, "无权限操作此议价");
                }

                // 2. 根据回应类型处理
                if (bargainResponse.Status == "反报价")
                {
                    // 反报价：创建新的议价记录
                    if (!bargainResponse.ProposedPrice.HasValue)
                    {
                        return (false, "反报价时必须提供新的报价");
                    }

                    // 更新当前议价状态为反报价
                    await _negotiationsRepository.UpdateNegotiationStatusAsync(negotiation.NegotiationId, "反报价");

                    // 创建新的议价记录（角色互换）
                    var counterNegotiation = new Negotiation
                    {
                        OrderId = negotiation.OrderId,
                        ProposedPrice = bargainResponse.ProposedPrice.Value,
                        Status = "等待回应",
                        CreatedAt = TimeHelper.Now
                    };

                    await _negotiationsRepository.AddAsync(counterNegotiation);

                    // 发送反报价通知给买家
                    try
                    {
                        var product = await _productsRepository.GetByPrimaryKeyAsync(order.ProductId);
                        var notificationParams = new Dictionary<string, object>
                        {
                            ["productTitle"] = product?.Title ?? "商品",
                            ["originalPrice"] = negotiation.ProposedPrice,
                            ["counterPrice"] = bargainResponse.ProposedPrice.Value
                        };

                        await _notificationService.CreateNotificationAsync(
                            order.BuyerId,
                            16, // 卖家反报价通知模板ID
                            notificationParams,
                            order.OrderId
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "发送反报价通知失败");
                    }
                }
                else
                {
                    // 接受或拒绝：更新议价状态
                    await _negotiationsRepository.UpdateNegotiationStatusAsync(negotiation.NegotiationId, bargainResponse.Status);

                    // 如果接受议价，更新订单价格和状态
                    if (bargainResponse.Status == "接受")
                    {
                        if (order != null)
                        {
                            order.TotalAmount = negotiation.ProposedPrice;
                            order.Status = Models.Entities.Order.OrderStatus.PendingPayment;
                            _ordersRepository.Update(order);
                        }
                    }
                    // 如果拒绝议价，将订单状态设置为"已取消"
                    else if (bargainResponse.Status == "拒绝")
                    {
                        if (order != null)
                        {
                            order.Status = Models.Entities.Order.OrderStatus.Cancelled;
                            _ordersRepository.Update(order);
                        }
                    }
                }

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("议价回应处理成功，议价ID：{NegotiationId}，状态：{Status}",
                    negotiation.NegotiationId, bargainResponse.Status);
                return (true, "回应已提交");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "处理议价回应失败，用户：{UserId}，议价：{NegotiationId}", userId, bargainResponse.NegotiationId);
                return (false, "系统错误，请稍后重试");
            }
        }

        /// <summary>
        /// 获取用户的议价记录
        /// </summary>
        public async Task<(IEnumerable<NegotiationDto> Negotiations, int TotalCount)> GetUserNegotiationsAsync(int userId, int pageIndex = 1, int pageSize = 10)
        {
            _logger.LogInformation("获取用户 {UserId} 的议价记录，页码：{PageIndex}，页大小：{PageSize}",
                userId, pageIndex, pageSize);

            try
            {
                var allNegotiations = await _negotiationsRepository.GetPendingNegotiationsAsync(userId);
                var totalCount = allNegotiations.Count();

                var pagedNegotiations = allNegotiations
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize);

                var negotiationDtos = pagedNegotiations.Select(n => new NegotiationDto
                {
                    NegotiationId = n.NegotiationId,
                    OrderId = n.OrderId,
                    ProposedPrice = n.ProposedPrice,
                    Status = n.Status,
                    CreatedAt = n.CreatedAt
                });

                return (negotiationDtos, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户议价记录失败，用户：{UserId}", userId);
                return (Enumerable.Empty<NegotiationDto>(), 0);
            }
        }

        /// <summary>
        /// 获取议价详情
        /// </summary>
        public async Task<NegotiationDto?> GetNegotiationDetailsAsync(int negotiationId, int userId)
        {
            _logger.LogInformation("获取议价详情，议价ID：{NegotiationId}，用户：{UserId}", negotiationId, userId);

            try
            {
                var negotiation = await _negotiationsRepository.GetByPrimaryKeyAsync(negotiationId);
                if (negotiation == null)
                {
                    return null;
                }

                // 通过订单验证用户权限
                var order = await _ordersRepository.GetByPrimaryKeyAsync(negotiation.OrderId);
                if (order == null || (order.BuyerId != userId && order.SellerId != userId))
                {
                    return null;
                }

                return new NegotiationDto
                {
                    NegotiationId = negotiation.NegotiationId,
                    OrderId = negotiation.OrderId,
                    ProposedPrice = negotiation.ProposedPrice,
                    Status = negotiation.Status,
                    CreatedAt = negotiation.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取议价详情失败，议价ID：{NegotiationId}", negotiationId);
                return null;
            }
        }
    }
}
