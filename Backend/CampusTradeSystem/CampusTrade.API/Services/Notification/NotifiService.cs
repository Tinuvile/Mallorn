using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusTrade.API.Data;
using CampusTrade.API.Infrastructure.Utils.Notificate;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Services.Background;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Services.Notification
{
    /// <summary>
    /// 通知服务：负责通知的创建和管理
    /// </summary>
    public class NotifiService
    {
        private readonly CampusTradeDbContext _context;

        public NotifiService(CampusTradeDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 创建通知的主接口（只创建不发送）
        /// </summary>
        /// <param name="recipientId">目标用户ID</param>
        /// <param name="templateId">模板ID</param>
        /// <param name="paramDict">参数字典</param>
        /// <param name="orderId">可选，关联订单</param>
        /// <returns>创建结果</returns>
        public async Task<(bool Success, string Message, int? NotificationId)> CreateNotificationAsync(
            int recipientId,
            int templateId,
            Dictionary<string, object> paramDict,
            int? orderId = null)
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
                CreatedAt = DateTime.Now,
                LastAttemptTime = DateTime.Now
            };

            // 6. 保存到数据库
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            Console.WriteLine($"[INFO] 通知已创建 - NotificationId: {notification.NotificationId}, RecipientId: {recipientId}, TemplateId: {templateId}");

            // 7. 立即触发后台服务处理新通知
            NotificationBackgroundService.TriggerProcessing();

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
                var systemNotifications = await _context.Notifications
                    .Include(n => n.Template)
                    .Where(n => n.RecipientId == userId &&
                               (n.Template!.TemplateName.Contains("系统") || n.Template.TemplateName.Contains("管理员")))
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip(pageIndex * pageSize)
                    .Take(pageSize)
                    .Select(n => new
                    {
                        id = n.NotificationId,
                        type = "notification",
                        sender = n.Template!.TemplateName.Contains("系统") ? "系统通知" : "校园管理员",
                        content = GetRenderedContent(n),
                        time = FormatTime(n.CreatedAt),
                        read = n.SendStatus == Models.Entities.Notification.SendStatuses.Success
                    })
                    .ToListAsync();

                messages.AddRange(systemNotifications.Cast<object>());
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
                var replyMessages = await _context.Notifications
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
                    .Select(n => new
                    {
                        id = n.NotificationId,
                        type = "reply",
                        sender = "用户回复", // 这里可以根据具体需求调整
                        content = GetRenderedContent(n),
                        time = FormatTime(n.CreatedAt),
                        read = n.SendStatus == Models.Entities.Notification.SendStatuses.Success
                    })
                    .ToListAsync();

                messages.AddRange(replyMessages.Cast<object>());
            }

            return messages.Take(pageSize).ToList();
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

            // 处理买家发起的议价消息
            foreach (var negotiation in buyerNegotiations)
            {
                var productImage = negotiation.Order.Product.ProductImages
                    .FirstOrDefault()?.ImageUrl ?? "/images/default-product.png";

                string contentText;
                decimal displayPrice = negotiation.ProposedPrice;

                // 如果是反报价状态，需要获取卖家的反报价价格
                if (negotiation.Status == "反报价")
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

                // 判断买家是否可以操作：只有当状态为"反报价"时，买家才可以回应卖家的反报价
                bool canRespond = negotiation.Status == "反报价";

                bargainMessages.Add(new
                {
                    id = negotiation.NegotiationId,
                    type = "bargain",
                    sender = negotiation.Order.Seller?.FullName ?? "卖家",
                    content = contentText,
                    time = FormatTime(negotiation.CreatedAt),
                    read = negotiation.Status != "等待回应",
                    productName = negotiation.Order.Product.Title,
                    productImage = productImage,
                    myOffer = negotiation.ProposedPrice, // 买家自己的报价
                    newOffer = negotiation.Status == "反报价" ? displayPrice : (decimal?)null, // 卖家的反报价
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

                // 判断卖家是否可以操作：只有当状态为"等待回应"时，卖家才可以回应买家的议价
                bool canRespond = negotiation.Status == "等待回应";

                bargainMessages.Add(new
                {
                    id = negotiation.NegotiationId,
                    type = "bargain",
                    sender = negotiation.Order.Buyer?.FullName ?? "买家",
                    content = contentText,
                    time = FormatTime(negotiation.CreatedAt),
                    read = negotiation.Status != "等待回应",
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
                    read = request.Status != "等待回应", // 等待回应状态为未读
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
                    read = true, // 自己发起的请求标记为已读
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
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == messageId && n.RecipientId == userId);

            if (notification != null)
            {
                notification.SendStatus = Models.Entities.Notification.SendStatuses.Success;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
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
            var now = DateTime.Now;
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
    }
}

