using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// Notification实体的Repository接口
    /// 继承基础IRepository，提供Notification特有的查询和操作方法
    /// </summary>
    public interface INotificationRepository : IRepository<Notification>
    {
        #region 创建操作
        /// <summary>
        /// 批量创建通知
        /// </summary>
        Task<IEnumerable<Notification>> CreateBatchNotificationsAsync(int templateId, List<int> recipientIds, int? orderId = null, Dictionary<string, object>? parameters = null);
        /// <summary>
        /// 批量创建订单相关通知
        /// </summary>
        Task<IEnumerable<Notification>> CreateBatchOrderNotificationsAsync(int templateId, List<int> recipientIds, int orderId, Dictionary<string, object>? parameters = null);
        /// <summary>
        /// 批量创建系统通知
        /// </summary>
        Task<IEnumerable<Notification>> CreateBatchSystemNotificationsAsync(int templateId, List<int> recipientIds, Dictionary<string, object>? parameters = null);
        #endregion

        #region 读取操作
        /// <summary>
        /// 根据接收者ID获取通知集合
        /// </summary>
        Task<IEnumerable<Notification>> GetByRecipientIdAsync(int recipientId);
        /// <summary>
        /// 获取未发送的通知
        /// </summary>
        Task<IEnumerable<Notification>> GetUnsentNotificationsAsync();
        /// <summary>
        /// 获取发送失败的通知
        /// </summary>
        Task<IEnumerable<Notification>> GetFailedNotificationsAsync();
        /// <summary>
        /// 获取待重试的通知
        /// </summary>
        Task<IEnumerable<Notification>> GetPendingRetryNotificationsAsync();
        /// <summary>
        /// 分页获取用户通知
        /// </summary>
        Task<(IEnumerable<Notification> Notifications, int TotalCount)> GetPagedNotificationsByUserAsync(int userId, int pageIndex, int pageSize, string? status = null, string? templateType = null);
        /// <summary>
        /// 获取高优先级通知
        /// </summary>
        Task<IEnumerable<Notification>> GetHighPriorityNotificationsAsync();
        /// <summary>
        /// 获取最近的通知
        /// </summary>
        Task<IEnumerable<Notification>> GetRecentNotificationsByUserAsync(int userId, int count = 10);
        #endregion

        #region 更新操作
        /// <summary>
        /// 标记通知发送状态
        /// </summary>
        Task MarkSendStatusAsync(int notificationId, string sendStatus);
        /// <summary>
        /// 增加通知重试次数
        /// </summary>
        Task IncrementRetryCountAsync(int notificationId);
        #endregion

        #region 删除操作
        /// <summary>
        /// 清理过期的失败通知
        /// </summary>
        Task<int> CleanupExpiredFailedNotificationsAsync(int daysOld = 30);
        #endregion

        #region 统计与扩展
        /// <summary>
        /// 获取用户未读通知数量
        /// </summary>
        Task<int> GetUnreadCountByUserAsync(int userId);
        /// <summary>
        /// 获取通知统计信息
        /// </summary>
        Task<Dictionary<string, int>> GetNotificationStatisticsAsync();
        #endregion
    }
}
