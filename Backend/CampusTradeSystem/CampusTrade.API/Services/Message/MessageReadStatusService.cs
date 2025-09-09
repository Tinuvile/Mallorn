using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Services.Message
{
    /// <summary>
    /// 消息已读状态服务 - 统一管理所有类型消息的已读状态
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
        /// 检查消息是否已读
        /// </summary>
        public async Task<bool> IsMessageReadAsync(int userId, int messageId, string messageType)
        {
            try
            {
                var readStatus = await _context.MessageReadStatuses
                    .FirstOrDefaultAsync(rs => rs.UserId == userId && 
                                              rs.MessageType == messageType && 
                                              rs.MessageId == messageId);

                return readStatus?.IsReadBool ?? false; // 如果没有记录，默认为未读
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查消息已读状态失败，用户ID: {UserId}, 消息类型: {MessageType}, 消息ID: {MessageId}", 
                    userId, messageType, messageId);
                return false;
            }
        }

        /// <summary>
        /// 标记消息为已读
        /// </summary>
        public async Task<bool> MarkMessageAsReadAsync(int userId, string messageType, int messageId)
        {
            return await CreateOrUpdateReadStatusAsync(userId, messageType, messageId, true);
        }

        /// <summary>
        /// 标记消息为未读
        /// </summary>
        public async Task<bool> MarkMessageAsUnreadAsync(int userId, string messageType, int messageId)
        {
            return await CreateOrUpdateReadStatusAsync(userId, messageType, messageId, false);
        }

        /// <summary>
        /// 批量获取消息已读状态
        /// </summary>
        public async Task<Dictionary<int, bool>> GetMessagesReadStatusAsync(int userId, string messageType, IEnumerable<int> messageIds)
        {
            try
            {
                var messageIdList = messageIds.ToList();
                var readStatuses = await _context.MessageReadStatuses
                    .Where(rs => rs.UserId == userId && 
                                rs.MessageType == messageType && 
                                messageIdList.Contains(rs.MessageId))
                    .ToDictionaryAsync(rs => rs.MessageId, rs => rs.IsReadBool);

                // 补充没有记录的消息，默认为未读
                var result = messageIdList.ToDictionary(id => id, id => false);
                foreach (var kvp in readStatuses)
                {
                    result[kvp.Key] = kvp.Value;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量获取消息已读状态失败，用户ID: {UserId}, 消息类型: {MessageType}", userId, messageType);
                return messageIds.ToDictionary(id => id, id => false);
            }
        }

        /// <summary>
        /// 获取用户未读消息数量
        /// </summary>
        public async Task<int> GetUnreadCountAsync(int userId, string? messageType = null)
        {
            try
            {
                var query = _context.MessageReadStatuses
                    .Where(rs => rs.UserId == userId && rs.IsRead == 0);

                if (!string.IsNullOrEmpty(messageType))
                {
                    query = query.Where(rs => rs.MessageType == messageType);
                }

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取未读消息数量失败，用户ID: {UserId}, 消息类型: {MessageType}", userId, messageType);
                return 0;
            }
        }

        /// <summary>
        /// 标记用户某类型的所有消息为已读
        /// </summary>
        public async Task<bool> MarkAllAsReadAsync(int userId, string messageType)
        {
            try
            {
                var unreadMessages = await _context.MessageReadStatuses
                    .Where(rs => rs.UserId == userId && 
                                rs.MessageType == messageType && 
                                rs.IsRead == 0)
                    .ToListAsync();

                foreach (var message in unreadMessages)
                {
                    message.MarkAsRead();
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("用户 {UserId} 的 {MessageType} 类型消息已全部标记为已读，共 {Count} 条",
                    userId, messageType, unreadMessages.Count);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标记所有消息为已读失败，用户ID: {UserId}, 消息类型: {MessageType}", userId, messageType);
                return false;
            }
        }

        /// <summary>
        /// 创建或更新消息已读状态记录
        /// </summary>
        public async Task<bool> CreateOrUpdateReadStatusAsync(int userId, string messageType, int messageId, bool isRead)
        {
            try
            {
                var existingStatus = await _context.MessageReadStatuses
                    .FirstOrDefaultAsync(rs => rs.UserId == userId && 
                                              rs.MessageType == messageType && 
                                              rs.MessageId == messageId);

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
                        MessageType = messageType,
                        MessageId = messageId,
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

                _logger.LogDebug("消息已读状态已更新，用户ID: {UserId}, 消息类型: {MessageType}, 消息ID: {MessageId}, 已读: {IsRead}",
                    userId, messageType, messageId, isRead);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建或更新消息已读状态失败，用户ID: {UserId}, 消息类型: {MessageType}, 消息ID: {MessageId}",
                    userId, messageType, messageId);
                return false;
            }
        }

        /// <summary>
        /// 批量标记消息为已读
        /// </summary>
        public async Task<bool> MarkMessagesAsReadBatchAsync(int userId, string messageType, IEnumerable<int> messageIds)
        {
            try
            {
                var messageIdList = messageIds.ToList();
                foreach (var messageId in messageIdList)
                {
                    await CreateOrUpdateReadStatusAsync(userId, messageType, messageId, true);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量标记消息已读失败，用户ID: {UserId}, 消息类型: {MessageType}", userId, messageType);
                return false;
            }
        }

        /// <summary>
        /// 获取用户未读消息数量
        /// </summary>
        public async Task<int> GetUnreadMessageCountAsync(int userId, string? messageType = null)
        {
            try
            {
                var query = _context.MessageReadStatuses.Where(rs => rs.UserId == userId && rs.IsRead == 0);
                
                if (!string.IsNullOrEmpty(messageType))
                {
                    query = query.Where(rs => rs.MessageType == messageType);
                }

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取未读消息数量失败，用户ID: {UserId}, 消息类型: {MessageType}", userId, messageType);
                return 0;
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
