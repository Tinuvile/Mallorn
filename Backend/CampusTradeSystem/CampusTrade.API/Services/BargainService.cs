using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Bargain;
using CampusTrade.API.Services.Notification;
using System;
using System.Threading.Tasks;

namespace CampusTrade.API.Services.Bargain
{
    public interface IBargainService
    {
        Task<Negotiation> CreateBargainRequest(BargainRequestDto bargainRequest);
        Task<bool> HandleBargainResponse(int negotiationId, string status);
    }

    public class BargainService : IBargainService
    {
        private readonly IBargainRepository _bargainRepository;
        private readonly INotificationService _notificationService;

        // 构造函数，通过依赖注入获取 BargainRepository 和 NotificationService
        public BargainService(IBargainRepository bargainRepository, INotificationService notificationService)
        {
            _bargainRepository = bargainRepository;
            _notificationService = notificationService;
        }

        // 创建议价请求
        public async Task<Negotiation> CreateBargainRequest(BargainRequestDto bargainRequest)
        {
            var negotiation = new Negotiation
            {
                OrderId = bargainRequest.OrderId,
                ProposedPrice = bargainRequest.ProposedPrice,
                Status = "等待回应", // 初始状态为等待回应
                CreatedAt = DateTime.Now
            };

            // 将议价请求保存到数据库
            await _bargainRepository.AddAsync(negotiation);

            // 通知卖家有新的议价请求
            await _notificationService.SendBargainNotification(bargainRequest.OrderId, "新的议价请求");

            return negotiation;
        }

        // 处理卖家的议价回应
        public async Task<bool> HandleBargainResponse(int negotiationId, string status)
        {
            var negotiation = await _bargainRepository.GetByIdAsync(negotiationId);
            if (negotiation == null)
            {
                return false;
            }

            negotiation.Status = status;
            if (status == "接受")
            {
                // 如果接受议价，可以更新订单价格
                // 例如：更新订单的价格为 proposedPrice
            }

            await _bargainRepository.UpdateAsync(negotiation);
            return true;
        }
    }
}
