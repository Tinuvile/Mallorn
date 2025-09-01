using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// SignalR通知发送实体类
    /// </summary>
    [Table("SIGNALR_NOTIFICATIONS")]
    public class SignalRNotification
    {
        public static class SendStatuses
        {
            public const string Pending = "待发送";
            public const string Success = "成功";
            public const string Failed = "失败";
        }

        // 重试次数限制
        public const int MaxRetryCount = 3;
        public const int DefaultRetryIntervalMinutes = 1;

        /// <summary>
        /// SignalR通知ID
        /// </summary>
        [Key]
        [Column("SIGNALR_NOTIFICATION_ID")]
        public int SignalRNotificationId { get; set; }

        /// <summary>
        /// 通知ID（外键）
        /// </summary>
        [Required]
        [Column("NOTIFICATION_ID")]
        public int NotificationId { get; set; }

        /// <summary>
        /// 连接ID（SignalR连接标识）
        /// </summary>
        [Column("CONNECTION_ID", TypeName = "VARCHAR2(100)")]
        [MaxLength(100)]
        public string? ConnectionId { get; set; }

        /// <summary>
        /// 用户组标识（用于群发）
        /// </summary>
        [Column("GROUP_NAME", TypeName = "VARCHAR2(50)")]
        [MaxLength(50)]
        public string? GroupName { get; set; }

        /// <summary>
        /// 发送状态
        /// </summary>
        [Required]
        [Column("SEND_STATUS", TypeName = "VARCHAR2(20)")]
        [MaxLength(20)]
        public string SendStatus { get; set; } = SendStatuses.Pending;

        /// <summary>
        /// 重试次数
        /// </summary>
        [Required]
        [Column("RETRY_COUNT")]
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// 最后尝试发送时间
        /// </summary>
        [Required]
        [Column("LAST_ATTEMPT_TIME")]
        public DateTime LastAttemptTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 发送成功时间
        /// </summary>
        [Column("SENT_AT")]
        public DateTime? SentAt { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        [Column("ERROR_MESSAGE", TypeName = "VARCHAR2(500)")]
        [MaxLength(500)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 关联的通知实体
        /// </summary>
        public virtual Notification Notification { get; set; } = null!;
    }
}
