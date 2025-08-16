using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 通知实体类 - 基于模板的通知系统
    /// </summary>
    [Table("NOTIFICATIONS")]
    public class Notification
    {
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
        /// 通知ID
        /// </summary>
        [Key]
        [Column("NOTIFICATION_ID")]
        public int NotificationId { get; set; }

        /// <summary>
        /// 模板ID（外键）
        /// </summary>
        [Required]
        [Column("TEMPLATE_ID")]
        public int TemplateId { get; set; }

        /// <summary>
        /// 接收者用户ID（外键）
        /// </summary>
        [Required]
        [Column("RECIPIENT_ID")]
        public int RecipientId { get; set; }

        /// <summary>
        /// 订单ID（外键，可选，仅订单相关通知需要）
        /// </summary>
        [Column("ORDER_ID")]
        public int? OrderId { get; set; }

        /// <summary>
        /// 模板参数（JSON格式）
        /// </summary>
        [Column("TEMPLATE_PARAMS", TypeName = "CLOB")]
        public string? TemplateParams { get; set; }

        /// <summary>
        /// 发送状态
        /// </summary>
        [Required]
        [Column("SEND_STATUS", TypeName = "VARCHAR2(20)")]
        [MaxLength(20)]
        public string SendStatus { get; set; } = string.Empty;

        /// <summary>
        /// 重试次数
        /// </summary>
        [Required]
        [Column("RETRY_COUNT")]
        public int RetryCount { get; set; }

        /// <summary>
        /// 最后尝试发送时间
        /// </summary>
        [Required]
        [Column("LAST_ATTEMPT_TIME")]
        public DateTime LastAttemptTime { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 发送成功时间
        /// </summary>
        [Column("SENT_AT")]
        public DateTime? SentAt { get; set; }

        /// <summary>
        /// 关联的通知模板
        /// </summary>
        public virtual NotificationTemplate Template { get; set; } = null!;

        /// <summary>
        /// 接收通知的用户
        /// </summary>
        public virtual User Recipient { get; set; } = null!;

        /// <summary>
        /// 关联的抽象订单（可选）
        /// </summary>
        public virtual AbstractOrder? AbstractOrder { get; set; }
    }
}
