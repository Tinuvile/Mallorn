using CampusTrade.API.Models.DTOs.Bargain;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Interfaces;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BargainService> _logger;

        public BargainService(
            INegotiationsRepository negotiationsRepository,
            IRepository<Models.Entities.Order> ordersRepository,
            IUnitOfWork unitOfWork,
            ILogger<BargainService> logger)
        {
            _negotiationsRepository = negotiationsRepository;
            _ordersRepository = ordersRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
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

                if (order.BuyerId == userId)
                {
                    return (false, "不能对自己的订单发起议价", null);
                }

                if (order.Status != "待付款")
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
                    CreatedAt = DateTime.UtcNow
                };

                await _negotiationsRepository.AddAsync(negotiation);
                await _unitOfWork.CommitTransactionAsync();

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

                // 1. 获取议价记录
                var negotiation = await _negotiationsRepository.GetByPrimaryKeyAsync(bargainResponse.NegotiationId);
                if (negotiation == null)
                {
                    return (false, "议价记录不存在");
                }

                // 通过订单获取卖家信息验证权限
                var order = await _ordersRepository.GetByPrimaryKeyAsync(negotiation.OrderId);
                if (order == null || order.SellerId != userId)
                {
                    return (false, "无权限操作此议价");
                }

                if (negotiation.Status != "等待回应")
                {
                    return (false, "议价状态不允许回应");
                }

                // 2. 更新议价状态
                await _negotiationsRepository.UpdateNegotiationStatusAsync(negotiation.NegotiationId, bargainResponse.Status);

                // 3. 如果接受议价，更新订单价格
                if (bargainResponse.Status == "接受")
                {
                    if (order != null)
                    {
                        order.TotalAmount = negotiation.ProposedPrice;
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
