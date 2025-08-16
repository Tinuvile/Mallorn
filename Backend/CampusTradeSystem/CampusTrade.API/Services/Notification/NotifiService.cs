using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusTrade.API.Data;
using CampusTrade.API.Infrastructure.Utils.Notificate;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Services.Background;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Services.Auth
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
                return (false, "目标用户不存在或已禁用", null);

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
            var notification = new Notification
            {
                TemplateId = templateId,
                RecipientId = recipientId,
                OrderId = orderId,
                TemplateParams = paramJson,
                SendStatus = Notification.SendStatuses.Pending,
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
                .CountAsync(n => n.SendStatus == Notification.SendStatuses.Pending);

            var success = await _context.Notifications
                .CountAsync(n => n.SendStatus == Notification.SendStatuses.Success);

            var failed = await _context.Notifications
                .CountAsync(n => n.SendStatus == Notification.SendStatuses.Failed);

            return (pending, success, failed);
        }

        /// <summary>
        /// 获取用户的通知历史
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="pageIndex">页索引</param>
        /// <returns>通知列表</returns>
        public async Task<List<Notification>> GetUserNotificationsAsync(int userId, int pageSize = 10, int pageIndex = 0)
        {
            return await _context.Notifications
                .Include(n => n.Template)
                .Where(n => n.RecipientId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
