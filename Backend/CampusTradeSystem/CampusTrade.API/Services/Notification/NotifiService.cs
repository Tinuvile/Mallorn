using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusTrade.API.Data;
using CampusTrade.API.infrastructure.Utils;
using CampusTrade.API.Infrastructure.Utils.Notificate;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Services.Background;
using CampusTrade.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.Notification
{
    /// <summary>
    /// 通知服务：负责通知的创建和管理
    /// </summary>
    public class NotifiService
    {
        private readonly CampusTradeDbContext _context;
        private readonly NotificationReadStatusService _notificationReadStatusService;
        private readonly ILogger<NotifiService> _logger;

        public NotifiService(
            CampusTradeDbContext context,
            NotificationReadStatusService notificationReadStatusService,
            ILogger<NotifiService> logger)
        {
            _context = context;
            _notificationReadStatusService = notificationReadStatusService;
            _logger = logger;
        }

        /// <summary>
        /// 创建通知的主接口（只创建不发送）
        /// </summary>
        /// <param name="recipientId">目标用户ID</param>
        /// <param name="templateId">模板ID</param>
        /// <param name="paramDict">参数字典</param>
        /// <param name="orderId">可选，关联订单</param>
        /// <param name="messageType">可选，业务消息类型</param>
        /// <param name="messageId">可选，业务消息ID</param>
        /// <returns>创建结果</returns>
        public async Task<(bool Success, string Message, int? NotificationId)> CreateNotificationAsync(
            int recipientId,
            int templateId,
            Dictionary<string, object> paramDict,
            int? orderId = null,
            string? messageType = null,
            int? messageId = null)
        {
            // 1. 校验接收人是否有效
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == recipientId && u.IsActive == 1);
            if (user == null)
                return (false, $"目标用户不存在或已禁用，用户ID: {recipientId}", null);

            // 2. 校验模板是否有效
            var template = await _context.NotificationTemplates
                .FirstOrDefaultAsync(t => t.TemplateId == templateId && t.IsActive == 1);
            if (template == null)
                return (false, "通知模板不存在或已禁用", null);

            // 3. 参数序列化
            string paramJson = System.Text.Json.JsonSerializer.Serialize(paramDict ?? new Dictionary<string, object>());

            // 4. 参数有效性验证（只验证不渲染）
            try
            {
                // 这里只是验证参数是否能够正确解析和使用，不进行实际渲染
                Notifihelper.ReplaceTemplateParams(template.TemplateContent, paramJson);
            }
            catch (ArgumentException ex)
            {
                return (false, $"模板参数验证失败: {ex.Message}", null);
            }

            // 5. 创建通知实体
            var notification = new Models.Entities.Notification
            {
                TemplateId = templateId,
                RecipientId = recipientId,
                OrderId = orderId,
                TemplateParams = paramJson,
                SendStatus = Models.Entities.Notification.SendStatuses.Pending,
                RetryCount = 0,
                CreatedAt = TimeHelper.Now,
                LastAttemptTime = TimeHelper.Now,
                IsRead = 0, // 默认为未读
                ReadAt = null
            };

            // 6. 保存到数据库
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            Console.WriteLine($"[INFO] 通知已创建 - NotificationId: {notification.NotificationId}, RecipientId: {recipientId}, TemplateId: {templateId}");

            // 7. 延迟触发后台服务处理新通知，避免与当前事务冲突
            _ = Task.Run(async () =>
            {
                // 延迟100ms，确保当前事务有时间完成
                await Task.Delay(100);
                NotificationBackgroundService.TriggerProcessing();
            });

            return (true, "通知已创建并触发发送", notification.NotificationId);
        }

        /// <summary>
        /// 获取通知发送统计
        /// </summary>
        /// <returns>统计信息</returns>
        public async Task<(int Pending, int Success, int Failed)> GetNotificationStatsAsync()
        {
            var pending = await _context.Notifications
                .CountAsync(n => n.SendStatus == Models.Entities.Notification.SendStatuses.Pending);

            var success = await _context.Notifications
                .CountAsync(n => n.SendStatus == Models.Entities.Notification.SendStatuses.Success);

            var failed = await _context.Notifications
                .CountAsync(n => n.SendStatus == Models.Entities.Notification.SendStatuses.Failed);

            return (pending, success, failed);
        }

        /// <summary>
        /// 获取用户的通知历史
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="pageIndex">页索引</param>
        /// <returns>通知列表</returns>
        public async Task<List<Models.Entities.Notification>> GetUserNotificationsAsync(int userId, int pageSize = 10, int pageIndex = 0)
        {
            return await _context.Notifications
                .Include(n => n.Template)
                .Where(n => n.RecipientId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// 获取用户消息列表（包括通知、议价、换物等）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="category">消息分类</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="pageIndex">页索引</param>
        /// <returns>消息列表</returns>
        public async Task<List<object>> GetUserMessagesAsync(int userId, string? category = null, int pageSize = 10, int pageIndex = 0)
        {
            var messages = new List<object>();

            // 获取系统通知
            if (category == null || category == "system")
            {
                var systemMessages = await GetSystemNotificationsAsync(userId, pageSize, pageIndex);
                messages.AddRange(systemMessages);
            }

            // 获取议价消息
            if (category == null || category == "bargain")
            {
                var bargainMessages = await GetBargainMessagesAsync(userId, pageSize, pageIndex);
                messages.AddRange(bargainMessages);
            }

            // 获取换物消息
            if (category == null || category == "swap")
            {
                var swapMessages = await GetSwapMessagesAsync(userId, pageSize, pageIndex);
                messages.AddRange(swapMessages);
            }

            // 获取其他回复消息
            if (category == null || category == "reply")
            {
                var replyMessages = await GetReplyMessagesAsync(userId, pageSize, pageIndex);
                messages.AddRange(replyMessages);
            }

            return messages.Take(pageSize).ToList();
        }

        /// <summary>
        /// 获取系统通知消息
        /// </summary>
        private async Task<List<object>> GetSystemNotificationsAsync(int userId, int pageSize, int pageIndex)
        {
            var systemNotificationsRaw = await _context.Notifications
                .Include(n => n.Template)
                .Where(n => n.RecipientId == userId &&
                           (n.Template!.TemplateName.Contains("系统") || n.Template.TemplateName.Contains("管理员")))
                .OrderByDescending(n => n.CreatedAt)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var messageIds = systemNotificationsRaw.Select(n => n.NotificationId).ToList();
            var readStatuses = await _notificationReadStatusService.GetNotificationsReadStatusAsync(
                userId, messageIds);

            return systemNotificationsRaw.Select(n => new
            {
                id = n.NotificationId,
                type = "notification",
                sender = n.Template!.TemplateName.Contains("系统") ? "系统通知" : "校园管理员",
                content = GetRenderedContent(n),
                time = FormatTime(n.CreatedAt),
                read = readStatuses.GetValueOrDefault(n.NotificationId, false)
            }).Cast<object>().ToList();
        }

        /// <summary>
        /// 获取回复消息
        /// </summary>
        private async Task<List<object>> GetReplyMessagesAsync(int userId, int pageSize, int pageIndex)
        {
            var replyMessagesRaw = await _context.Notifications
                .Include(n => n.Template)
                .Include(n => n.Recipient)
                .Where(n => n.RecipientId == userId &&
                           !n.Template!.TemplateName.Contains("系统") &&
                           !n.Template.TemplateName.Contains("管理员") &&
                           !n.Template.TemplateName.Contains("议价") &&
                           !n.Template.TemplateName.Contains("换物"))
                .OrderByDescending(n => n.CreatedAt)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var messageIds = replyMessagesRaw.Select(n => n.NotificationId).ToList();
            var readStatuses = await _notificationReadStatusService.GetNotificationsReadStatusAsync(
                userId, messageIds);

            return replyMessagesRaw.Select(n => new
            {
                id = n.NotificationId,
                type = "reply",
                sender = "用户回复", // 这里可以根据具体需求调整
                content = GetRenderedContent(n),
                time = FormatTime(n.CreatedAt),
                read = readStatuses.GetValueOrDefault(n.NotificationId, false)
            }).Cast<object>().ToList();
        }

        /// <summary>
        /// 获取议价相关消息
        /// </summary>
        private async Task<List<object>> GetBargainMessagesAsync(int userId, int pageSize, int pageIndex)
        {
            var bargainMessages = new List<object>();

            // 从议价表中查询用户相关的议价记录
            // 1. 用户作为买家发起的议价
            var buyerNegotiations = await _context.Negotiations
                .Include(n => n.Order)
                .ThenInclude(o => o.Product)
                .ThenInclude(p => p.ProductImages)
                .Include(n => n.Order)
                .ThenInclude(o => o.Product)
                .ThenInclude(p => p.User)
                .Include(n => n.Order)
                .ThenInclude(o => o.Seller)
                .Where(n => n.Order.BuyerId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 2. 用户作为卖家收到的议价
            var sellerNegotiations = await _context.Negotiations
                .Include(n => n.Order)
                .ThenInclude(o => o.Product)
                .ThenInclude(p => p.ProductImages)
                .Include(n => n.Order)
                .ThenInclude(o => o.Product)
                .ThenInclude(p => p.User)
                .Include(n => n.Order)
                .ThenInclude(o => o.Buyer)
                .Where(n => n.Order.SellerId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 获取所有议价消息的ID，用于批量查询已读状态
            var allNegotiationIds = buyerNegotiations.Concat(sellerNegotiations)
                .Select(n => n.NegotiationId).ToList();
            var readStatuses = await _notificationReadStatusService.GetNotificationsReadStatusAsync(
                userId, allNegotiationIds);

            // 处理买家发起的议价消息
            foreach (var negotiation in buyerNegotiations)
            {
                var productImage = negotiation.Order.Product.ProductImages
                    .FirstOrDefault()?.ImageUrl ?? "/images/default-product.png";

                string contentText;
                decimal displayPrice = negotiation.ProposedPrice;

                // 判断买家是否可以操作：当状态为"等待回应"且轮到买家回应时
                // 根据议价轮次判断：偶数轮卖家回应，奇数轮买家回应
                var allNegotiationsForOrder = await _context.Negotiations
                    .Where(n => n.OrderId == negotiation.OrderId)
                    .ToListAsync();
                var orderedNegotiations = allNegotiationsForOrder.OrderBy(n => n.CreatedAt).ToList();
                var currentIndex = orderedNegotiations.FindIndex(n => n.NegotiationId == negotiation.NegotiationId);
                bool shouldBuyerRespond = (currentIndex % 2 == 1); // 奇数轮买家回应

                // 如果买家需要响应，说明是卖家反报价，需要显示卖家的反报价金额
                if (shouldBuyerRespond && negotiation.Status == "等待回应")
                {
                    // 当前议价记录就是卖家的反报价，直接使用当前记录的价格
                    displayPrice = negotiation.ProposedPrice;
                    contentText = $"卖家给出反报价：￥{displayPrice}，等待您的回应";
                }
                else if (negotiation.Status == "反报价")
                {
                    var latestNegotiation = await _context.Negotiations
                        .Where(n => n.OrderId == negotiation.OrderId && n.Status == "等待回应")
                        .OrderByDescending(n => n.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (latestNegotiation != null)
                    {
                        displayPrice = latestNegotiation.ProposedPrice;
                        contentText = $"卖家给出反报价：￥{displayPrice}，等待您的回应";
                    }
                    else
                    {
                        contentText = $"卖家给出反报价，等待您的回应";
                    }
                }
                else
                {
                    contentText = negotiation.Status switch
                    {
                        "等待回应" => $"您向卖家提出议价请求，心理价位：￥{negotiation.ProposedPrice}，等待卖家回应",
                        "接受" => $"卖家已接受您的议价！成交价格：￥{negotiation.ProposedPrice}",
                        "拒绝" => $"卖家拒绝了您的议价请求",
                        _ => $"您对商品《{negotiation.Order.Product.Title}》提出了议价 ￥{negotiation.ProposedPrice}，当前状态：{negotiation.Status}"
                    };
                }

                bool canRespond = negotiation.Status == "等待回应" && shouldBuyerRespond;

                bargainMessages.Add(new
                {
                    id = negotiation.NegotiationId,
                    type = "bargain",
                    sender = negotiation.Order.Seller?.FullName ?? "卖家",
                    content = contentText,
                    time = FormatTime(negotiation.CreatedAt),
                    read = readStatuses.GetValueOrDefault(negotiation.NegotiationId, false),
                    productName = negotiation.Order.Product.Title,
                    productImage = productImage,
                    myOffer = negotiation.ProposedPrice, // 买家自己的报价
                    newOffer = shouldBuyerRespond ? displayPrice : (decimal?)null, // 当买家需要回应时显示卖家的反报价
                    originalPrice = negotiation.Order.Product.BasePrice,
                    bargainStatus = GetBargainStatusInEnglish(negotiation.Status),
                    role = "buyer",
                    canRespond = canRespond, // 买家是否可以操作
                    orderId = negotiation.OrderId,
                    negotiationId = negotiation.NegotiationId
                });
            }

            // 处理卖家收到的议价消息
            foreach (var negotiation in sellerNegotiations)
            {
                var productImage = negotiation.Order.Product.ProductImages
                    .FirstOrDefault()?.ImageUrl ?? "/images/default-product.png";

                var contentText = negotiation.Status switch
                {
                    "等待回应" => $"买家提出议价请求，心理价位：￥{negotiation.ProposedPrice}，请您选择回应方式",
                    "接受" => $"您已接受买家的议价！成交价格：￥{negotiation.ProposedPrice}",
                    "拒绝" => $"您已拒绝买家的议价请求",
                    "反报价" => $"您已给出反报价，等待买家回应",
                    _ => $"买家对您的商品《{negotiation.Order.Product.Title}》提出了议价 ￥{negotiation.ProposedPrice}，当前状态：{negotiation.Status}"
                };

                // 判断卖家是否可以操作：当状态为"等待回应"且轮到卖家回应时
                // 根据议价轮次判断：偶数轮卖家回应，奇数轮买家回应
                var allNegotiationsForOrder = await _context.Negotiations
                    .Where(n => n.OrderId == negotiation.OrderId)
                    .ToListAsync();
                var orderedNegotiations = allNegotiationsForOrder.OrderBy(n => n.CreatedAt).ToList();
                var currentIndex = orderedNegotiations.FindIndex(n => n.NegotiationId == negotiation.NegotiationId);
                bool shouldSellerRespond = (currentIndex % 2 == 0); // 偶数轮卖家回应
                bool canRespond = negotiation.Status == "等待回应" && shouldSellerRespond;

                bargainMessages.Add(new
                {
                    id = negotiation.NegotiationId,
                    type = "bargain",
                    sender = negotiation.Order.Buyer?.FullName ?? "买家",
                    content = contentText,
                    time = FormatTime(negotiation.CreatedAt),
                    read = readStatuses.GetValueOrDefault(negotiation.NegotiationId, false),
                    productName = negotiation.Order.Product.Title,
                    productImage = productImage,
                    buyerOffer = negotiation.ProposedPrice,
                    originalPrice = negotiation.Order.Product.BasePrice,
                    bargainStatus = GetBargainStatusInEnglish(negotiation.Status),
                    role = "seller",
                    canRespond = canRespond, // 卖家是否可以操作
                    orderId = negotiation.OrderId,
                    negotiationId = negotiation.NegotiationId
                });
            }

            // 合并并按时间排序
            return bargainMessages
                .OrderByDescending(m => ((dynamic)m).time)
                .Take(pageSize)
                .ToList();
        }

        /// <summary>
        /// 将中文议价状态转换为英文
        /// </summary>
        private string GetBargainStatusInEnglish(string chineseStatus)
        {
            return chineseStatus switch
            {
                "等待回应" => "pending",
                "接受" => "accepted",
                "拒绝" => "rejected",
                "反报价" => "counter_offer",
                _ => "unknown"
            };
        }

        /// <summary>
        /// 获取换物相关消息
        /// </summary>
        private async Task<List<object>> GetSwapMessagesAsync(int userId, int pageSize, int pageIndex)
        {
            var swapMessages = new List<object>();

            // 1. 查询收到的换物请求（别人想换我的商品）
            var receivedRequests = await _context.ExchangeRequests
                .Include(e => e.AbstractOrder)
                .Include(e => e.OfferProduct)
                .ThenInclude(p => p.User)
                .Include(e => e.OfferProduct)
                .ThenInclude(p => p.ProductImages)
                .Include(e => e.RequestProduct)
                .ThenInclude(p => p.User)
                .Where(e => e.RequestProduct.UserId == userId) // 我的商品被请求换物
                .OrderByDescending(e => e.CreatedAt)
                .Take(pageSize * 2) // 取更多数据用于后续合并排序
                .ToListAsync();

            // 2. 查询发起的换物请求（我想换别人的商品）
            var sentRequests = await _context.ExchangeRequests
                .Include(e => e.AbstractOrder)
                .Include(e => e.OfferProduct)
                .ThenInclude(p => p.User)
                .Include(e => e.RequestProduct)
                .ThenInclude(p => p.User)
                .Include(e => e.RequestProduct)
                .ThenInclude(p => p.ProductImages)
                .Where(e => e.OfferProduct.UserId == userId) // 我发起的换物请求
                .OrderByDescending(e => e.CreatedAt)
                .Take(pageSize * 2) // 取更多数据用于后续合并排序
                .ToListAsync();

            // 获取所有换物消息的ID，用于批量查询已读状态
            var allExchangeIds = receivedRequests.Concat(sentRequests)
                .Select(r => r.ExchangeId).ToList();
            var readStatuses = await _notificationReadStatusService.GetNotificationsReadStatusAsync(
                userId, allExchangeIds);

            foreach (var request in receivedRequests)
            {
                var offerProductImage = request.OfferProduct.ProductImages
                    .FirstOrDefault()?.ImageUrl ?? "/images/default-product.png";

                var contentText = request.Status switch
                {
                    "等待回应" => $"用户提出换物请求：希望用《{request.OfferProduct.Title}》换取您的《{request.RequestProduct.Title}》，请选择是否接受",
                    "接受" => $"您已接受换物请求：《{request.OfferProduct.Title}》换《{request.RequestProduct.Title}》",
                    "拒绝" => $"您已拒绝换物请求：《{request.OfferProduct.Title}》换《{request.RequestProduct.Title}》",
                    _ => $"换物请求：{request.OfferProduct.User?.Username ?? "匿名用户"}希望用《{request.OfferProduct.Title}》与您的《{request.RequestProduct.Title}》交换"
                };

                swapMessages.Add(new
                {
                    id = request.ExchangeId,
                    type = "swap",
                    sender = request.OfferProduct.User?.Username ?? "匿名用户",
                    content = contentText,
                    time = FormatTime(request.CreatedAt),
                    read = readStatuses.GetValueOrDefault(request.ExchangeId, false),
                    swapProductName = request.OfferProduct.Title,
                    swapProductImage = offerProductImage,
                    swapProductPrice = request.OfferProduct.BasePrice,
                    swapProductLink = $"/goods/{request.OfferProductId}",
                    swapStatus = GetSwapStatusInEnglish(request.Status),
                    userRole = "receiver", // 用户角色：接收者
                    myProductName = request.RequestProduct.Title,
                    otherProductName = request.OfferProduct.Title
                });
            }

            foreach (var request in sentRequests)
            {
                var requestProductImage = request.RequestProduct.ProductImages
                    .FirstOrDefault()?.ImageUrl ?? "/images/default-product.png";

                var contentText = request.Status switch
                {
                    "等待回应" => $"您发起的换物请求：用《{request.OfferProduct.Title}》换取《{request.RequestProduct.Title}》，等待对方回应",
                    "接受" => $"换物请求已被接受：《{request.OfferProduct.Title}》换《{request.RequestProduct.Title}》",
                    "拒绝" => $"换物请求被拒绝：《{request.OfferProduct.Title}》换《{request.RequestProduct.Title}》",
                    _ => $"换物请求：您向{request.RequestProduct.User?.Username ?? "匿名用户"}发起了换物请求，希望用《{request.OfferProduct.Title}》换取《{request.RequestProduct.Title}》"
                };

                swapMessages.Add(new
                {
                    id = request.ExchangeId,
                    type = "swap",
                    sender = "我的换物请求",
                    content = contentText,
                    time = FormatTime(request.CreatedAt),
                    read = readStatuses.GetValueOrDefault(request.ExchangeId, true), // 自己发起的请求默认已读
                    swapProductName = request.RequestProduct.Title,
                    swapProductImage = requestProductImage,
                    swapProductPrice = request.RequestProduct.BasePrice,
                    swapProductLink = $"/goods/{request.RequestProductId}",
                    swapStatus = GetSwapStatusInEnglish(request.Status),
                    userRole = "sender", // 用户角色：发送者
                    myProductName = request.OfferProduct.Title,
                    otherProductName = request.RequestProduct.Title
                });
            }

            // 3. 合并并按时间排序，返回指定数量的消息
            return swapMessages
                .OrderByDescending(m => ((dynamic)m).time)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToList();
        }

        /// <summary>
        /// 将中文换物状态转换为英文
        /// </summary>
        private string GetSwapStatusInEnglish(string chineseStatus)
        {
            return chineseStatus switch
            {
                "等待回应" => "pending",
                "接受" => "accepted",
                "拒绝" => "rejected",
                _ => "pending"
            };
        }

        /// <summary>
        /// 标记消息为已读
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="messageId">消息ID</param>
        /// <returns>操作结果</returns>
        public async Task<bool> MarkMessageAsReadAsync(int userId, int messageId)
        {
            try
            {
                // 首先尝试标记通知消息为已读
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.NotificationId == messageId && n.RecipientId == userId);

                if (notification != null)
                {
                    return await _notificationReadStatusService.MarkNotificationAsReadAsync(
                        userId, messageId);
                }

                // 然后尝试标记议价消息为已读
                var negotiation = await _context.Negotiations
                    .Include(n => n.Order)
                    .FirstOrDefaultAsync(n => n.NegotiationId == messageId &&
                                             (n.Order.BuyerId == userId || n.Order.SellerId == userId));

                if (negotiation != null)
                {
                    return await _notificationReadStatusService.MarkNotificationAsReadAsync(
                        userId, messageId);
                }

                // 最后尝试标记换物消息为已读
                var exchangeRequest = await _context.ExchangeRequests
                    .Include(e => e.OfferProduct)
                    .Include(e => e.RequestProduct)
                    .FirstOrDefaultAsync(e => e.ExchangeId == messageId &&
                                             (e.OfferProduct.UserId == userId || e.RequestProduct.UserId == userId));

                if (exchangeRequest != null)
                {
                    return await _notificationReadStatusService.MarkNotificationAsReadAsync(
                        userId, messageId);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标记消息为已读失败，用户ID: {UserId}, 消息ID: {MessageId}", userId, messageId);
                return false;
            }
        }

        /// <summary>
        /// 获取渲染后的通知内容
        /// </summary>
        private string GetRenderedContent(Models.Entities.Notification notification)
        {
            try
            {
                if (notification.Template != null)
                {
                    return Notifihelper.ReplaceTemplateParams(
                        notification.Template.TemplateContent,
                        notification.TemplateParams ?? "{}");
                }
                return "通知内容获取失败";
            }
            catch
            {
                return "通知内容解析失败";
            }
        }

        /// <summary>
        /// 获取议价对话历史
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <param name="userId">用户ID（用于权限验证）</param>
        /// <returns>议价对话历史</returns>
        public async Task<List<object>> GetBargainConversationAsync(int orderId, int userId)
        {
            var conversations = new List<object>();

            // 验证用户是否有权限查看此订单的议价记录
            var order = await _context.Orders
                .Include(o => o.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null || (order.BuyerId != userId && order.SellerId != userId))
            {
                return conversations;
            }

            // 获取该订单的所有议价记录，按时间顺序排序
            var negotiations = await _context.Negotiations
                .Include(n => n.Order)
                .ThenInclude(o => o.Buyer)
                .Include(n => n.Order)
                .ThenInclude(o => o.Seller)
                .Include(n => n.Order)
                .ThenInclude(o => o.Product)
                .Where(n => n.OrderId == orderId)
                .OrderBy(n => n.CreatedAt) // 按时间正序排序，展示对话流程
                .ToListAsync();

            // 重新构建对话逻辑：分析议价流程
            var currentUserIsBuyer = userId == order.BuyerId;

            for (int i = 0; i < negotiations.Count; i++)
            {
                var negotiation = negotiations[i];
                var messageContent = "";
                var sender = "";
                var isFromCurrentUser = false;

                // 判断这条记录是谁发起的
                // 第一条记录总是买家发起的
                // 后续记录：如果前一条记录状态是"反报价"，则当前记录是卖家发起的反报价
                var isInitiatedByBuyer = true;
                if (i > 0)
                {
                    var previousNegotiation = negotiations[i - 1];
                    isInitiatedByBuyer = previousNegotiation.Status != "反报价";
                }

                // 根据发起者和当前用户角色确定消息内容
                if (isInitiatedByBuyer)
                {
                    // 买家发起的议价
                    if (currentUserIsBuyer)
                    {
                        messageContent = $"我提出议价：￥{negotiation.ProposedPrice}";
                        sender = "我";
                        isFromCurrentUser = true;
                    }
                    else
                    {
                        messageContent = $"买家提出议价：￥{negotiation.ProposedPrice}";
                        sender = order.Buyer?.FullName ?? "买家";
                        isFromCurrentUser = false;
                    }
                }
                else
                {
                    // 卖家发起的反报价
                    if (currentUserIsBuyer)
                    {
                        messageContent = $"卖家反报价：￥{negotiation.ProposedPrice}";
                        sender = order.Seller?.FullName ?? "卖家";
                        isFromCurrentUser = false;
                    }
                    else
                    {
                        messageContent = $"我反报价：￥{negotiation.ProposedPrice}";
                        sender = "我";
                        isFromCurrentUser = true;
                    }
                }

                // 跳过状态为"反报价"的记录，因为它们只是标记，不代表实际的对话内容
                if (negotiation.Status == "反报价")
                {
                    continue;
                }

                // 处理最终状态（接受/拒绝）
                if (negotiation.Status == "接受")
                {
                    var accepter = isInitiatedByBuyer ?
                        (currentUserIsBuyer ? "卖家" : "我") :
                        (currentUserIsBuyer ? "我" : "买家");
                    messageContent = $"{accepter}接受了议价：￥{negotiation.ProposedPrice}";
                    sender = "系统";
                    isFromCurrentUser = false;
                }
                else if (negotiation.Status == "拒绝")
                {
                    var rejecter = isInitiatedByBuyer ?
                        (currentUserIsBuyer ? "卖家" : "我") :
                        (currentUserIsBuyer ? "我" : "买家");
                    messageContent = $"{rejecter}拒绝了议价";
                    sender = "系统";
                    isFromCurrentUser = false;
                }

                conversations.Add(new
                {
                    id = negotiation.NegotiationId,
                    sender = sender,
                    content = messageContent,
                    time = FormatTime(negotiation.CreatedAt),
                    status = negotiation.Status,
                    price = negotiation.ProposedPrice,
                    isFromCurrentUser = isFromCurrentUser
                });
            }

            return conversations;
        }

        /// <summary>
        /// 格式化时间显示
        /// </summary>
        private string FormatTime(DateTime dateTime)
        {
            var now = TimeHelper.Now;
            var diff = now - dateTime;

            if (diff.TotalDays < 1)
            {
                return $"今天 {dateTime:HH:mm}";
            }
            else if (diff.TotalDays < 2)
            {
                return $"昨天 {dateTime:HH:mm}";
            }
            else if (diff.TotalDays < 30)
            {
                return dateTime.ToString("MM月dd日");
            }
            else
            {
                return dateTime.ToString("yyyy年MM月dd日");
            }
        }

        /// <summary>
        /// 获取用户未读消息数量
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="category">消息分类</param>
        /// <returns>未读消息数量</returns>
        public async Task<int> GetUnreadMessageCountAsync(int userId, string? category = null)
        {
            try
            {
                int unreadCount = 0;

                // 根据分类统计未读消息数量
                if (string.IsNullOrEmpty(category) || category == "all")
                {
                    // 统计所有类型的未读消息

                    // 1. 统计系统通知的未读数量
                    var systemNotificationIds = await _context.Notifications
                        .Where(n => n.RecipientId == userId &&
                                   (n.Template!.TemplateName.Contains("系统") ||
                                    n.Template.TemplateName.Contains("管理员")))
                        .Select(n => n.NotificationId)
                        .ToListAsync();

                    var systemUnread = await _notificationReadStatusService.GetUnreadNotificationCountAsync(userId, systemNotificationIds);

                    // 2. 统计回复消息的未读数量
                    var replyNotificationIds = await _context.Notifications
                        .Where(n => n.RecipientId == userId &&
                                   !n.Template!.TemplateName.Contains("系统") &&
                                   !n.Template.TemplateName.Contains("管理员") &&
                                   !n.Template.TemplateName.Contains("议价") &&
                                   !n.Template.TemplateName.Contains("换物"))
                        .Select(n => n.NotificationId)
                        .ToListAsync();

                    var replyUnread = await _notificationReadStatusService.GetUnreadNotificationCountAsync(userId, replyNotificationIds);

                    // 3. 统计议价消息的未读数量
                    var bargainIds = await _context.Negotiations
                        .Include(n => n.Order)
                        .Where(n => n.Order.BuyerId == userId || n.Order.SellerId == userId)
                        .Select(n => n.NegotiationId)
                        .ToListAsync();

                    var bargainUnread = await _notificationReadStatusService.GetUnreadNotificationCountAsync(userId, bargainIds);

                    // 4. 统计换物消息的未读数量
                    var swapIds = await _context.ExchangeRequests
                        .Include(e => e.OfferProduct)
                        .Include(e => e.RequestProduct)
                        .Where(e => e.OfferProduct.UserId == userId || e.RequestProduct.UserId == userId)
                        .Select(e => e.ExchangeId)
                        .ToListAsync();

                    var swapUnread = await _notificationReadStatusService.GetUnreadNotificationCountAsync(userId, swapIds);

                    unreadCount = systemUnread + replyUnread + bargainUnread + swapUnread;
                }
                else
                {
                    // 根据分类获取对应的消息ID列表并统计未读数量
                    List<int> messageIds = new List<int>();

                    switch (category)
                    {
                        case "system":
                            messageIds = await _context.Notifications
                                .Where(n => n.RecipientId == userId &&
                                           (n.Template!.TemplateName.Contains("系统") ||
                                            n.Template.TemplateName.Contains("管理员")))
                                .Select(n => n.NotificationId)
                                .ToListAsync();
                            break;

                        case "reply":
                            messageIds = await _context.Notifications
                                .Where(n => n.RecipientId == userId &&
                                           !n.Template!.TemplateName.Contains("系统") &&
                                           !n.Template.TemplateName.Contains("管理员") &&
                                           !n.Template.TemplateName.Contains("议价") &&
                                           !n.Template.TemplateName.Contains("换物"))
                                .Select(n => n.NotificationId)
                                .ToListAsync();
                            break;

                        case "bargain":
                            messageIds = await _context.Negotiations
                                .Include(n => n.Order)
                                .Where(n => n.Order.BuyerId == userId || n.Order.SellerId == userId)
                                .Select(n => n.NegotiationId)
                                .ToListAsync();
                            break;

                        case "swap":
                            messageIds = await _context.ExchangeRequests
                                .Include(e => e.OfferProduct)
                                .Include(e => e.RequestProduct)
                                .Where(e => e.OfferProduct.UserId == userId || e.RequestProduct.UserId == userId)
                                .Select(e => e.ExchangeId)
                                .ToListAsync();
                            break;
                    }

                    unreadCount = await _notificationReadStatusService.GetUnreadNotificationCountAsync(userId, messageIds);
                }

                _logger.LogDebug("用户 {UserId} 的未读消息数量: {UnreadCount} (分类: {Category})", userId, unreadCount, category ?? "all");
                return unreadCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户未读消息数量失败，用户ID: {UserId}, 分类: {Category}", userId, category);
                return 0;
            }
        }
    }
}

