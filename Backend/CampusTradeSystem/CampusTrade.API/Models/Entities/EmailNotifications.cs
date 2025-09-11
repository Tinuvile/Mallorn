using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 邮件通知发送实体类
    /// </summary>
    [Table("EMAIL_NOTIFICATIONS")]
    public class EmailNotification
    {
        public static class EmailTypes
        {
            public const string Notification = "通知";
            public const string VerificationCode = "验证码";
        }

        public static class SendStatuses
        {
            public const string Pending = "待发送";
            public const string Success = "成功";
            public const string Failed = "失败";
        }

        // 重试次数限制
        public const int MaxRetryCount = 5;
        public const int DefaultRetryIntervalMinutes = 5;

        /// <summary>
        /// 邮件通知ID
        /// </summary>
        [Key]
        [Column("EMAIL_NOTIFICATION_ID")]
        public int EmailNotificationId { get; set; }

        /// <summary>
        /// 邮件类型（通知/验证码）
        /// </summary>
        [Required]
        [Column("EMAIL_TYPE", TypeName = "VARCHAR2(20)")]
        [MaxLength(20)]
        public string EmailType { get; set; } = string.Empty;

        /// <summary>
        /// 通知ID（外键，验证码类型时可为空）
        /// </summary>
        [Column("NOTIFICATION_ID")]
        public int? NotificationId { get; set; }

        /// <summary>
        /// 收件人邮箱地址
        /// </summary>
        [Required]
        [Column("RECIPIENT_EMAIL", TypeName = "VARCHAR2(100)")]
        [MaxLength(100)]
        [EmailAddress]
        public string RecipientEmail { get; set; } = string.Empty;

        /// <summary>
        /// 邮件主题
        /// </summary>
        [Required]
        [Column("SUBJECT", TypeName = "VARCHAR2(200)")]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// 邮件内容（HTML格式）
        /// </summary>
        [Required]
        [Column("CONTENT", TypeName = "CLOB")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 验证码（仅验证码类型邮件使用）
        /// </summary>
        [Column("VERIFICATION_CODE", TypeName = "VARCHAR2(10)")]
        [MaxLength(10)]
        public string? VerificationCode { get; set; }

        /// <summary>
        /// 验证码过期时间（仅验证码类型邮件使用）
        /// </summary>
        [Column("CODE_EXPIRES_AT")]
        public DateTime? CodeExpiresAt { get; set; }

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
        public DateTime LastAttemptTime { get; set; } = TimeHelper.UtcNow;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; } = TimeHelper.UtcNow;

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
        /// 关联的通知实体（验证码类型时可为空）
        /// </summary>
        public virtual Notification? Notification { get; set; }
    }
}
