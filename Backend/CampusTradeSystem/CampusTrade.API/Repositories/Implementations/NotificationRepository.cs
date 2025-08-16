using System.Text.Json;
using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// Notification实体的Repository实现类
    /// 继承基础Repository，提供Notification特有的查询和操作方法
    /// </summary>
    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        public NotificationRepository(CampusTradeDbContext context) : base(context) { }

        #region 创建操作
        /// <summary>
        /// 批量创建通知
        /// </summary>
        public async Task<IEnumerable<Notification>> CreateBatchNotificationsAsync(int templateId, List<int> recipientIds, int? orderId = null, Dictionary<string, object>? parameters = null)
        {
            if (!recipientIds.Any()) return new List<Notification>();
            var notifications = new List<Notification>();
            var serializedParams = parameters != null ? System.Text.Json.JsonSerializer.Serialize(parameters) : null;
            foreach (var recipientId in recipientIds)
            {
                var notification = new Notification
                {
                    TemplateId = templateId,
                    RecipientId = recipientId,
                    OrderId = orderId,
                    SendStatus = Notification.SendStatuses.Pending,
                    TemplateParams = serializedParams
                };
                notifications.Add(notification);
            }
            await AddRangeAsync(notifications);
            return notifications;
        }
        /// <summary>
        /// 批量创建订单相关通知
        /// </summary>
        public async Task<IEnumerable<Notification>> CreateBatchOrderNotificationsAsync(int templateId, List<int> recipientIds, int orderId, Dictionary<string, object>? parameters = null)
        {
            return await CreateBatchNotificationsAsync(templateId, recipientIds, orderId, parameters);
        }
        /// <summary>
        /// 批量创建系统通知
        /// </summary>
        public async Task<IEnumerable<Notification>> CreateBatchSystemNotificationsAsync(int templateId, List<int> recipientIds, Dictionary<string, object>? parameters = null)
        {
            return await CreateBatchNotificationsAsync(templateId, recipientIds, null, parameters);
        }
        #endregion

        #region 读取操作
        /// <summary>
        /// 根据接收者ID获取通知集合
        /// </summary>
        public async Task<IEnumerable<Notification>> GetByRecipientIdAsync(int recipientId)
        {
            return await _dbSet.Where(n => n.RecipientId == recipientId).Include(n => n.Template).OrderByDescending(n => n.CreatedAt).ToListAsync();
        }
        /// <summary>
        /// 获取未发送的通知
        /// </summary>
        public async Task<IEnumerable<Notification>> GetUnsentNotificationsAsync()
        {
            return await _dbSet.Where(n => n.SendStatus == Notification.SendStatuses.Pending).Include(n => n.Template).Include(n => n.Recipient).OrderBy(n => n.CreatedAt).ToListAsync();
        }
        /// <summary>
        /// 获取发送失败的通知
        /// </summary>
        public async Task<IEnumerable<Notification>> GetFailedNotificationsAsync()
        {
            return await _dbSet.Where(n => n.SendStatus == Notification.SendStatuses.Failed).Include(n => n.Template).Include(n => n.Recipient).OrderByDescending(n => n.LastAttemptTime).ToListAsync();
        }
        /// <summary>
        /// 获取待重试的通知
        /// </summary>
        public async Task<IEnumerable<Notification>> GetPendingRetryNotificationsAsync()
        {
            var now = DateTime.Now;
            return await _dbSet.Where(n => n.SendStatus == Notification.SendStatuses.Failed && n.RetryCount < Notification.MaxRetryCount && n.LastAttemptTime.AddMinutes(Notification.DefaultRetryIntervalMinutes) <= now).Include(n => n.Template).Include(n => n.Recipient).OrderBy(n => n.LastAttemptTime).ToListAsync();
        }
        /// <summary>
        /// 分页获取用户通知
        /// </summary>
        public async Task<(IEnumerable<Notification> Notifications, int TotalCount)> GetPagedNotificationsByUserAsync(int userId, int pageIndex, int pageSize, string? status = null, string? templateType = null)
        {
            var query = _dbSet.Where(n => n.RecipientId == userId).AsQueryable();
            if (!string.IsNullOrEmpty(status)) query = query.Where(n => n.SendStatus == status);
            if (!string.IsNullOrEmpty(templateType)) query = query.Where(n => n.Template.TemplateType == templateType);
            var totalCount = await query.CountAsync();
            var notifications = await query.Include(n => n.Template).OrderByDescending(n => n.CreatedAt).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return (notifications, totalCount);
        }
        /// <summary>
        /// 获取高优先级通知
        /// </summary>
        public async Task<IEnumerable<Notification>> GetHighPriorityNotificationsAsync()
        {
            return await _dbSet
                .Include(n => n.Template)
                .Where(n => n.Template.Priority >= NotificationTemplate.PriorityLevels.High)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
        /// <summary>
        /// 获取最近的通知
        /// </summary>
        public async Task<IEnumerable<Notification>> GetRecentNotificationsByUserAsync(int userId, int count = 10)
        {
            return await _dbSet.Where(n => n.RecipientId == userId).OrderByDescending(n => n.CreatedAt).Take(count).ToListAsync();
        }
        #endregion

        #region 更新操作
        /// <summary>
        /// 标记通知发送状态
        /// </summary>
        public async Task MarkSendStatusAsync(int notificationId, string sendStatus)
        {
            var notification = await GetByPrimaryKeyAsync(notificationId);
            if (notification != null)
            {
                notification.SendStatus = sendStatus;
                Update(notification);
            }
        }
        /// <summary>
        /// 增加通知重试次数
        /// </summary>
        public async Task IncrementRetryCountAsync(int notificationId)
        {
            await _context.Database.ExecuteSqlRawAsync(
                @"UPDATE NOTIFICATIONS SET RETRY_COUNT = RETRY_COUNT + 1, 
                LAST_ATTEMPT_TIME = CURRENT_TIMESTAMP WHERE NOTIFICATION_ID = {0}",
                notificationId);
        }
        #endregion

        #region 删除操作
        /// <summary>
        /// 清理过期的失败通知
        /// </summary>
        public async Task<int> CleanupExpiredFailedNotificationsAsync(int daysOld = 30)
        {
            var cutoff = DateTime.Now.AddDays(-daysOld);
            var expired = await _dbSet.Where(n => n.SendStatus == Notification.SendStatuses.Failed && n.CreatedAt < cutoff).ToListAsync();
            if (expired.Any())
            {
                _dbSet.RemoveRange(expired);
                await _context.SaveChangesAsync();
            }
            return expired.Count;
        }
        #endregion

        #region 统计与扩展
        /// <summary>
        /// 获取用户未读通知数量
        /// </summary>
        public async Task<int> GetUnreadCountByUserAsync(int userId)
        {
            return await _dbSet.CountAsync(n => n.RecipientId == userId && n.SendStatus == Notification.SendStatuses.Success);
        }
        /// <summary>
        /// 获取通知统计信息
        /// </summary>
        public async Task<Dictionary<string, int>> GetNotificationStatisticsAsync()
        {
            var stats = new Dictionary<string, int>();
            var statusStats = await _dbSet.GroupBy(n => n.SendStatus).Select(g => new { Status = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Status, x => x.Count);
            foreach (var stat in statusStats) stats[stat.Key] = stat.Value;
            var typeStats = await _dbSet.Include(n => n.Template).GroupBy(n => n.Template.TemplateType).Select(g => new { Type = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Type, x => x.Count);
            foreach (var stat in typeStats) stats[$"类型_{stat.Key}"] = stat.Value;
            var retryStats = await _dbSet.Where(n => n.RetryCount > 0).CountAsync();
            stats["需要重试"] = retryStats;
            return stats;
        }
        #endregion
    }
}
