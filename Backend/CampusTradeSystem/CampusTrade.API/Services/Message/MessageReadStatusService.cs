using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Services.Message
{
    /// <summary>
    /// 消息已读状态服务 - 与通知一一对应的已读状态管理
    /// </summary>
    public class MessageReadStatusService : IMessageReadStatusService
    {
        private readonly CampusTradeDbContext _context;
        private readonly ILogger<MessageReadStatusService> _logger;

        public MessageReadStatusService(
            CampusTradeDbContext context,
            ILogger<MessageReadStatusService> logger)
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
                var readStatus = await _context.MessageReadStatuses
                    .FirstOrDefaultAsync(rs => rs.UserId == userId && rs.NotificationId == notificationId);

                return readStatus?.IsReadBool ?? false; // 如果没有记录，默认为未读
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
            return await CreateOrUpdateReadStatusAsync(userId, notificationId, true);
        }

        /// <summary>
        /// 标记通知为未读
        /// </summary>
        public async Task<bool> MarkNotificationAsUnreadAsync(int userId, int notificationId)
        {
            return await CreateOrUpdateReadStatusAsync(userId, notificationId, false);
        }

        /// <summary>
        /// 批量获取通知已读状态
        /// </summary>
        public async Task<Dictionary<int, bool>> GetNotificationsReadStatusAsync(int userId, IEnumerable<int> notificationIds)
        {
            try
            {
                var notificationIdList = notificationIds.ToList();
                var readStatuses = await _context.MessageReadStatuses
                    .Where(rs => rs.UserId == userId && notificationIdList.Contains(rs.NotificationId))
                    .ToDictionaryAsync(rs => rs.NotificationId, rs => rs.IsReadBool);

                // 补充没有记录的通知，默认为未读
                var result = notificationIdList.ToDictionary(id => id, id => false);
                foreach (var kvp in readStatuses)
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
                return await _context.MessageReadStatuses
                    .Where(rs => rs.UserId == userId && rs.IsRead == 0)
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

                // 计算未读数量：在指定列表中但没有已读记录的，或者已读记录标记为未读的
                var readStatuses = await _context.MessageReadStatuses
                    .Where(rs => rs.UserId == userId && notificationIdList.Contains(rs.NotificationId))
                    .ToListAsync();

                var readIds = readStatuses.Where(rs => rs.IsRead == 1).Select(rs => rs.NotificationId).ToHashSet();
                return notificationIdList.Count(id => !readIds.Contains(id));
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
                var unreadNotifications = await _context.MessageReadStatuses
                    .Where(rs => rs.UserId == userId && rs.IsRead == 0)
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
        /// 创建或更新通知已读状态记录
        /// </summary>
        public async Task<bool> CreateOrUpdateReadStatusAsync(int userId, int notificationId, bool isRead)
        {
            try
            {
                var existingStatus = await _context.MessageReadStatuses
                    .FirstOrDefaultAsync(rs => rs.UserId == userId && rs.NotificationId == notificationId);

                if (existingStatus != null)
                {
                    // 更新现有记录
                    if (isRead)
                    {
                        existingStatus.MarkAsRead();
                    }
                    else
                    {
                        existingStatus.MarkAsUnread();
                    }
                }
                else
                {
                    // 创建新记录
                    var newStatus = new MessageReadStatus
                    {
                        UserId = userId,
                        NotificationId = notificationId,
                        IsReadBool = isRead,
                        CreatedAt = DateTime.UtcNow
                    };

                    if (isRead)
                    {
                        newStatus.ReadAt = DateTime.UtcNow;
                    }

                    _context.MessageReadStatuses.Add(newStatus);
                }

                await _context.SaveChangesAsync();

                _logger.LogDebug("通知已读状态已更新，用户ID: {UserId}, 通知ID: {NotificationId}, 已读: {IsRead}",
                    userId, notificationId, isRead);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建或更新通知已读状态失败，用户ID: {UserId}, 通知ID: {NotificationId}",
                    userId, notificationId);
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
                foreach (var notificationId in notificationIdList)
                {
                    await CreateOrUpdateReadStatusAsync(userId, notificationId, true);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量标记通知已读失败，用户ID: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// 清理过期的已读状态记录
        /// </summary>
        public async Task<int> CleanupExpiredReadStatusAsync(DateTime expiredBefore)
        {
            try
            {
                var expiredRecords = await _context.MessageReadStatuses
                    .Where(rs => rs.CreatedAt < expiredBefore)
                    .ToListAsync();

                _context.MessageReadStatuses.RemoveRange(expiredRecords);
                await _context.SaveChangesAsync();

                _logger.LogInformation("清理过期已读状态记录 {Count} 条", expiredRecords.Count);
                return expiredRecords.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期已读状态记录失败");
                return 0;
            }
        }
    }
}
