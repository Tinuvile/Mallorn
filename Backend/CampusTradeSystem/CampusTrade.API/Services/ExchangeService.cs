using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Exchange;
using CampusTrade.API.Services.Notification;
using System;
using System.Threading.Tasks;

namespace CampusTrade.API.Services.Exchange
{
    public interface IExchangeService
    {
        Task<ExchangeRequest> CreateExchangeRequest(ExchangeRequestDto exchangeRequest);
        Task<bool> HandleExchangeResponse(int exchangeId, string status);
    }

    public class ExchangeService : IExchangeService
    {
        private readonly IExchangeRepository _exchangeRepository;
        private readonly INotificationService _notificationService;

        // 构造函数，通过依赖注入获取 ExchangeRepository 和 NotificationService
        public ExchangeService(IExchangeRepository exchangeRepository, INotificationService notificationService)
        {
            _exchangeRepository = exchangeRepository;
            _notificationService = notificationService;
        }

        // 创建换物请求
        public async Task<ExchangeRequest> CreateExchangeRequest(ExchangeRequestDto exchangeRequest)
        {
            var request = new ExchangeRequest
            {
                OfferProductId = exchangeRequest.OfferProductId,
                RequestProductId = exchangeRequest.RequestProductId,
                Terms = exchangeRequest.Terms,
                Status = "等待回应", // 初始状态为等待回应
                CreatedAt = DateTime.Now
            };

            // 将换物请求保存到数据库
            await _exchangeRepository.AddAsync(request);

            // 通知对方有新的换物请求
            await _notificationService.SendExchangeNotification(exchangeRequest.OfferProductId, "新的换物请求");

            return request;
        }

        // 处理换物请求回应
        public async Task<bool> HandleExchangeResponse(int exchangeId, string status)
        {
            var exchangeRequest = await _exchangeRepository.GetByIdAsync(exchangeId);
            if (exchangeRequest == null)
            {
                return false;
            }

            exchangeRequest.Status = status;
            await _exchangeRepository.UpdateAsync(exchangeRequest);
            return true;
        }
    }
}
