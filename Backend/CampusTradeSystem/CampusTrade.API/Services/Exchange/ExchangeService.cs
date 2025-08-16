using CampusTrade.API.Models.DTOs.Exchange;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.Exchange
{
    /// <summary>
    /// 换物服务实现
    /// </summary>
    public class ExchangeService : IExchangeService
    {
        private readonly IExchangeRequestsRepository _exchangeRequestsRepository;
        private readonly IRepository<Models.Entities.Product> _productsRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ExchangeService> _logger;

        public ExchangeService(
            IExchangeRequestsRepository exchangeRequestsRepository,
            IRepository<Models.Entities.Product> productsRepository,
            IUnitOfWork unitOfWork,
            ILogger<ExchangeService> logger)
        {
            _exchangeRequestsRepository = exchangeRequestsRepository;
            _productsRepository = productsRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// 创建换物请求
        /// </summary>
        public async Task<(bool Success, string Message, int? ExchangeRequestId)> CreateExchangeRequestAsync(ExchangeRequestDto exchangeRequest, int userId)
        {
            _logger.LogInformation("用户 {UserId} 发起换物请求，提供商品：{OfferProductId}，请求商品：{RequestProductId}",
                userId, exchangeRequest.OfferProductId, exchangeRequest.RequestProductId);

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // 1. 验证提供的商品是否属于当前用户
                var offerProduct = await _productsRepository.GetByPrimaryKeyAsync(exchangeRequest.OfferProductId);
                if (offerProduct == null || offerProduct.UserId != userId)
                {
                    return (false, "提供的商品不存在或不属于您", null);
                }

                if (offerProduct.Status != Models.Entities.Product.ProductStatus.OnSale)
                {
                    return (false, "提供的商品状态不允许交换", null);
                }

                // 2. 验证请求的商品是否存在且可交换
                var requestProduct = await _productsRepository.GetByPrimaryKeyAsync(exchangeRequest.RequestProductId);
                if (requestProduct == null)
                {
                    return (false, "请求的商品不存在", null);
                }

                if (requestProduct.UserId == userId)
                {
                    return (false, "不能请求交换自己的商品", null);
                }

                if (requestProduct.Status != Models.Entities.Product.ProductStatus.OnSale)
                {
                    return (false, "请求的商品状态不允许交换", null);
                }

                // 3. 检查是否已存在相同的换物请求
                var hasExistingRequest = await _exchangeRequestsRepository.HasPendingExchangeAsync(exchangeRequest.OfferProductId);
                if (hasExistingRequest)
                {
                    return (false, "该商品已有待处理的换物请求", null);
                }

                // 4. 创建换物请求
                var request = new ExchangeRequest
                {
                    OfferProductId = exchangeRequest.OfferProductId,
                    RequestProductId = exchangeRequest.RequestProductId,
                    Terms = exchangeRequest.Terms,
                    Status = "待回应",
                    CreatedAt = DateTime.UtcNow
                };

                await _exchangeRequestsRepository.AddAsync(request);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("换物请求创建成功，请求ID：{ExchangeId}", request.ExchangeId);
                return (true, "换物请求已发送", request.ExchangeId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "创建换物请求失败，用户：{UserId}", userId);
                return (false, "系统错误，请稍后重试", null);
            }
        }

        /// <summary>
        /// 处理换物回应
        /// </summary>
        public async Task<(bool Success, string Message)> HandleExchangeResponseAsync(ExchangeResponseDto exchangeResponse, int userId)
        {
            _logger.LogInformation("用户 {UserId} 回应换物请求 {ExchangeRequestId}，状态：{Status}",
                userId, exchangeResponse.ExchangeRequestId, exchangeResponse.Status);

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // 1. 获取换物请求
                var request = await _exchangeRequestsRepository.GetByPrimaryKeyAsync(exchangeResponse.ExchangeRequestId);
                if (request == null)
                {
                    return (false, "换物请求不存在");
                }

                // 2. 验证权限（只有被请求商品的所有者可以回应）
                var requestProduct = await _productsRepository.GetByPrimaryKeyAsync(request.RequestProductId);
                if (requestProduct == null || requestProduct.UserId != userId)
                {
                    return (false, "无权限操作此换物请求");
                }

                if (request.Status != "待回应")
                {
                    return (false, "换物请求状态不允许回应");
                }

                // 3. 更新换物请求状态
                await _exchangeRequestsRepository.UpdateExchangeStatusAsync(request.ExchangeId, exchangeResponse.Status);

                // 4. 如果同意换物，更新商品状态
                if (exchangeResponse.Status == "同意")
                {
                    var offerProduct = await _productsRepository.GetByPrimaryKeyAsync(request.OfferProductId);
                    if (offerProduct != null && requestProduct != null)
                    {
                        // 标记两个商品为交易中
                        offerProduct.Status = Models.Entities.Product.ProductStatus.InTransaction;
                        requestProduct.Status = Models.Entities.Product.ProductStatus.InTransaction;

                        // 标记实体为已修改
                        _productsRepository.Update(offerProduct);
                        _productsRepository.Update(requestProduct);
                    }
                }

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("换物回应处理成功，请求ID：{ExchangeId}，状态：{Status}",
    request.ExchangeId, exchangeResponse.Status);
                return (true, "回应已提交");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "处理换物回应失败，用户：{UserId}，请求：{ExchangeRequestId}",
                    userId, exchangeResponse.ExchangeRequestId);
                return (false, "系统错误，请稍后重试");
            }
        }

        /// <summary>
        /// 获取用户的换物请求记录
        /// </summary>
        public async Task<(IEnumerable<ExchangeRequestInfoDto> ExchangeRequests, int TotalCount)> GetUserExchangeRequestsAsync(int userId, int pageIndex = 1, int pageSize = 10)
        {
            _logger.LogInformation("获取用户 {UserId} 的换物请求记录，页码：{PageIndex}，页大小：{PageSize}",
                userId, pageIndex, pageSize);

            try
            {
                var allRequests = await _exchangeRequestsRepository.GetByUserIdAsync(userId);
                var totalCount = allRequests.Count();

                var pagedRequests = allRequests
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize);

                var requestDtos = pagedRequests.Select(r => new ExchangeRequestInfoDto
                {
                    ExchangeRequestId = r.ExchangeId,
                    OfferProductId = r.OfferProductId,
                    RequestProductId = r.RequestProductId,
                    Terms = r.Terms ?? string.Empty,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                });

                return (requestDtos, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户换物请求记录失败，用户：{UserId}", userId);
                return (Enumerable.Empty<ExchangeRequestInfoDto>(), 0);
            }
        }

        /// <summary>
        /// 获取换物请求详情
        /// </summary>
        public async Task<ExchangeRequestInfoDto?> GetExchangeRequestDetailsAsync(int exchangeRequestId, int userId)
        {
            _logger.LogInformation("获取换物请求详情，请求ID：{ExchangeRequestId}，用户：{UserId}", exchangeRequestId, userId);

            try
            {
                var request = await _exchangeRequestsRepository.GetByPrimaryKeyAsync(exchangeRequestId);
                if (request == null)
                {
                    return null;
                }

                // 验证权限（提供商品或请求商品的所有者可以查看）
                var offerProduct = await _productsRepository.GetByPrimaryKeyAsync(request.OfferProductId);
                var requestProduct = await _productsRepository.GetByPrimaryKeyAsync(request.RequestProductId);

                if ((offerProduct == null || offerProduct.UserId != userId) &&
                    (requestProduct == null || requestProduct.UserId != userId))
                {
                    return null;
                }

                return new ExchangeRequestInfoDto
                {
                    ExchangeRequestId = request.ExchangeId,
                    OfferProductId = request.OfferProductId,
                    RequestProductId = request.RequestProductId,
                    Terms = request.Terms ?? string.Empty,
                    Status = request.Status,
                    CreatedAt = request.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取换物请求详情失败，请求ID：{ExchangeRequestId}", exchangeRequestId);
                return null;
            }
        }
    }
}
