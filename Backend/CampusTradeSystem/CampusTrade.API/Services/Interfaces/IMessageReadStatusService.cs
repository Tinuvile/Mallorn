using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Services.Interfaces
{
    /// <summary>
    /// 消息已读状态服务接口
    /// </summary>
    public interface IMessageReadStatusService
    {
        /// <summary>
        /// 检查消息是否已读
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="messageId">消息ID</param>
        /// <param name="messageType">消息类型</param>
        /// <returns>是否已读</returns>
        Task<bool> IsMessageReadAsync(int userId, int messageId, string messageType);

        /// <summary>
        /// 标记消息为已读
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="messageType">消息类型</param>
        /// <param name="messageId">消息ID</param>
        /// <returns>操作是否成功</returns>
        Task<bool> MarkMessageAsReadAsync(int userId, string messageType, int messageId);

        /// <summary>
        /// 标记消息为未读
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="messageType">消息类型</param>
        /// <param name="messageId">消息ID</param>
        /// <returns>操作是否成功</returns>
        Task<bool> MarkMessageAsUnreadAsync(int userId, string messageType, int messageId);

        /// <summary>
        /// 批量获取消息已读状态
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="messageType">消息类型</param>
        /// <param name="messageIds">消息ID列表</param>
        /// <returns>消息ID到已读状态的映射</returns>
        Task<Dictionary<int, bool>> GetMessagesReadStatusAsync(int userId, string messageType, IEnumerable<int> messageIds);

        /// <summary>
        /// 批量标记消息为已读
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="messageType">消息类型</param>
        /// <param name="messageIds">消息ID列表</param>
        /// <returns>操作是否成功</returns>
        Task<bool> MarkMessagesAsReadBatchAsync(int userId, string messageType, IEnumerable<int> messageIds);

        /// <summary>
        /// 获取用户未读消息数量
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="messageType">消息类型（可选，如果为空则获取所有类型）</param>
        /// <returns>未读消息数量</returns>
        Task<int> GetUnreadMessageCountAsync(int userId, string? messageType = null);

        /// <summary>
        /// 清理过期的已读状态记录
        /// </summary>
        /// <param name="expiredBefore">过期时间</param>
        /// <returns>清理的记录数量</returns>
        Task<int> CleanupExpiredReadStatusAsync(DateTime expiredBefore);

        /// <summary>
        /// 创建或更新消息读取状态
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="messageType">消息类型</param>
        /// <param name="messageId">消息ID</param>
        /// <param name="isRead">是否已读</param>
        /// <returns>操作是否成功</returns>
        Task<bool> CreateOrUpdateReadStatusAsync(int userId, string messageType, int messageId, bool isRead);
    }
}
