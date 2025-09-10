using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Services.Interfaces
{
    /// <summary>
    /// 消息已读状态服务接口 - 与通知一一对应的已读状态管理
    /// </summary>
    public interface IMessageReadStatusService
    {
        /// <summary>
        /// 检查通知是否已读
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="notificationId">通知ID</param>
        /// <returns>是否已读</returns>
        Task<bool> IsNotificationReadAsync(int userId, int notificationId);

        /// <summary>
        /// 标记通知为已读
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="notificationId">通知ID</param>
        /// <returns>操作是否成功</returns>
        Task<bool> MarkNotificationAsReadAsync(int userId, int notificationId);

        /// <summary>
        /// 标记通知为未读
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="notificationId">通知ID</param>
        /// <returns>操作是否成功</returns>
        Task<bool> MarkNotificationAsUnreadAsync(int userId, int notificationId);

        /// <summary>
        /// 批量获取通知已读状态
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="notificationIds">通知ID列表</param>
        /// <returns>通知ID到已读状态的映射</returns>
        Task<Dictionary<int, bool>> GetNotificationsReadStatusAsync(int userId, IEnumerable<int> notificationIds);

        /// <summary>
        /// 批量标记通知为已读
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="notificationIds">通知ID列表</param>
        /// <returns>操作是否成功</returns>
        Task<bool> MarkNotificationsAsReadBatchAsync(int userId, IEnumerable<int> notificationIds);

        /// <summary>
        /// 获取用户未读通知数量
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>未读通知数量</returns>
        Task<int> GetUnreadNotificationCountAsync(int userId);

        /// <summary>
        /// 获取指定通知列表中用户未读通知数量
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="notificationIds">通知ID列表</param>
        /// <returns>未读通知数量</returns>
        Task<int> GetUnreadNotificationCountAsync(int userId, IEnumerable<int> notificationIds);

        /// <summary>
        /// 标记用户所有通知为已读
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>操作是否成功</returns>
        Task<bool> MarkAllNotificationsAsReadAsync(int userId);

        /// <summary>
        /// 清理过期的已读状态记录
        /// </summary>
        /// <param name="expiredBefore">过期时间</param>
        /// <returns>清理的记录数量</returns>
        Task<int> CleanupExpiredReadStatusAsync(DateTime expiredBefore);

        /// <summary>
        /// 创建或更新通知读取状态
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="notificationId">通知ID</param>
        /// <param name="isRead">是否已读</param>
        /// <returns>操作是否成功</returns>
        Task<bool> CreateOrUpdateReadStatusAsync(int userId, int notificationId, bool isRead);
    }
}
