using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Services.Notification
{
    /// <summary>
    /// 通知已读状态服务 - 直接在通知表中管理已读状态
    /// </summary>
    public class NotificationReadStatusService
    {
        private readonly CampusTradeDbContext _context;
        private readonly ILogger<NotificationReadStatusService> _logger;

        public NotificationReadStatusService(
            CampusTradeDbContext context,
            ILogger<NotificationReadStatusService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 检查通知是否已读
        /// </summary>
        public async Task<bool> IsNotificationReadAsync(int userId, int notificationId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.RecipientId == userId);

                return notification?.IsReadBool ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查通知已读状态失败，用户ID: {UserId}, 通知ID: {NotificationId}", userId, notificationId);
                return false;
            }
        }

        /// <summary>
        /// 标记通知为已读
        /// </summary>
        public async Task<bool> MarkNotificationAsReadAsync(int userId, int notificationId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.RecipientId == userId);

                if (notification == null)
                {
                    _logger.LogWarning("通知不存在或用户无权限，用户ID: {UserId}, 通知ID: {NotificationId}", userId, notificationId);
                    return false;
                }

                if (!notification.IsReadBool)
                {
                    notification.MarkAsRead();
                    await _context.SaveChangesAsync();

                    _logger.LogDebug("通知已标记为已读，用户ID: {UserId}, 通知ID: {NotificationId}", userId, notificationId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标记通知已读失败，用户ID: {UserId}, 通知ID: {NotificationId}", userId, notificationId);
                return false;
            }
        }

        /// <summary>
        /// 标记通知为未读
        /// </summary>
        public async Task<bool> MarkNotificationAsUnreadAsync(int userId, int notificationId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.RecipientId == userId);

                if (notification == null)
                {
                    _logger.LogWarning("通知不存在或用户无权限，用户ID: {UserId}, 通知ID: {NotificationId}", userId, notificationId);
                    return false;
                }

                if (notification.IsReadBool)
                {
                    notification.MarkAsUnread();
                    await _context.SaveChangesAsync();

                    _logger.LogDebug("通知已标记为未读，用户ID: {UserId}, 通知ID: {NotificationId}", userId, notificationId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标记通知未读失败，用户ID: {UserId}, 通知ID: {NotificationId}", userId, notificationId);
                return false;
            }
        }

        /// <summary>
        /// 批量获取通知已读状态
        /// </summary>
        public async Task<Dictionary<int, bool>> GetNotificationsReadStatusAsync(int userId, IEnumerable<int> notificationIds)
        {
            try
            {
                var notificationIdList = notificationIds.ToList();
                var notifications = await _context.Notifications
                    .Where(n => n.RecipientId == userId && notificationIdList.Contains(n.NotificationId))
                    .Select(n => new { n.NotificationId, n.IsRead })
                    .ToDictionaryAsync(n => n.NotificationId, n => n.IsRead == 1);

                // 补充没有记录的通知，默认为未读
                var result = notificationIdList.ToDictionary(id => id, id => false);
                foreach (var kvp in notifications)
                {
                    result[kvp.Key] = kvp.Value;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量获取通知已读状态失败，用户ID: {UserId}", userId);
                return notificationIds.ToDictionary(id => id, id => false);
            }
        }

        /// <summary>
        /// 获取用户未读通知数量
        /// </summary>
        public async Task<int> GetUnreadNotificationCountAsync(int userId)
        {
            try
            {
                return await _context.Notifications
                    .Where(n => n.RecipientId == userId && n.IsRead == 0)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取未读通知数量失败，用户ID: {UserId}", userId);
                return 0;
            }
        }

        /// <summary>
        /// 获取指定通知列表中用户未读通知数量
        /// </summary>
        public async Task<int> GetUnreadNotificationCountAsync(int userId, IEnumerable<int> notificationIds)
        {
            try
            {
                var notificationIdList = notificationIds.ToList();
                if (!notificationIdList.Any())
                {
                    return 0;
                }

                return await _context.Notifications
                    .Where(n => n.RecipientId == userId && notificationIdList.Contains(n.NotificationId) && n.IsRead == 0)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取指定通知列表中未读通知数量失败，用户ID: {UserId}", userId);
                return 0;
            }
        }

        /// <summary>
        /// 标记用户所有通知为已读
        /// </summary>
        public async Task<bool> MarkAllNotificationsAsReadAsync(int userId)
        {
            try
            {
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.RecipientId == userId && n.IsRead == 0)
                    .ToListAsync();

                foreach (var notification in unreadNotifications)
                {
                    notification.MarkAsRead();
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("用户 {UserId} 的所有通知已标记为已读，共 {Count} 条", userId, unreadNotifications.Count);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标记所有通知为已读失败，用户ID: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// 批量标记通知为已读
        /// </summary>
        public async Task<bool> MarkNotificationsAsReadBatchAsync(int userId, IEnumerable<int> notificationIds)
        {
            try
            {
                var notificationIdList = notificationIds.ToList();
                var notifications = await _context.Notifications
                    .Where(n => n.RecipientId == userId && notificationIdList.Contains(n.NotificationId) && n.IsRead == 0)
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.MarkAsRead();
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("批量标记通知已读完成，用户ID: {UserId}, 标记数量: {Count}", userId, notifications.Count);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量标记通知已读失败，用户ID: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// 获取用户通知列表（包含已读状态）
        /// </summary>
        public async Task<List<Models.Entities.Notification>> GetUserNotificationsWithReadStatusAsync(
            int userId, 
            int pageSize = 10, 
            int pageIndex = 0,
            bool? isRead = null)
        {
            try
            {
                var query = _context.Notifications
                    .Include(n => n.Template)
                    .Where(n => n.RecipientId == userId);

                // 根据已读状态过滤
                if (isRead.HasValue)
                {
                    query = query.Where(n => n.IsRead == (isRead.Value ? 1 : 0));
                }

                return await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip(pageIndex * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户通知列表失败，用户ID: {UserId}", userId);
                return new List<Models.Entities.Notification>();
            }
        }

        /// <summary>
        /// 根据模板类型获取用户未读通知数量
        /// </summary>
        public async Task<Dictionary<string, int>> GetUnreadCountByTemplateTypeAsync(int userId)
        {
            try
            {
                return await _context.Notifications
                    .Include(n => n.Template)
                    .Where(n => n.RecipientId == userId && n.IsRead == 0)
                    .GroupBy(n => n.Template.TemplateType)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取按模板类型分组的未读通知数量失败，用户ID: {UserId}", userId);
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// 清理过期的已读通知（可选功能）
        /// </summary>
        public async Task<int> CleanupExpiredReadNotificationsAsync(DateTime expiredBefore)
        {
            try
            {
                var expiredNotifications = await _context.Notifications
                    .Where(n => n.IsRead == 1 && n.ReadAt.HasValue && n.ReadAt.Value < expiredBefore)
                    .ToListAsync();

                _context.Notifications.RemoveRange(expiredNotifications);
                await _context.SaveChangesAsync();

                _logger.LogInformation("清理过期已读通知 {Count} 条", expiredNotifications.Count);
                return expiredNotifications.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期已读通知失败");
                return 0;
            }
        }
    }
}
