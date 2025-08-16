using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 通知模板实体类
    /// </summary>
    [Table("NOTIFICATION_TEMPLATES")]
    public class NotificationTemplate
    {
        public static class TemplateTypes
        {
            public const string ProductRelated = "商品相关";
            public const string TransactionRelated = "交易相关";
            public const string ReviewRelated = "评价相关";
            public const string SystemNotification = "系统通知";
        }

        public static class PriorityLevels
        {
            public const int Low = 1;
            public const int Normal = 2;
            public const int Medium = 3;
            public const int High = 4;
            public const int Critical = 5;
        }

        // 内容长度限制
        public const int MaxTemplateNameLength = 100;
        public const int MaxDescriptionLength = 500;

        /// <summary>
        /// 模板ID
        /// </summary>
        [Key]
        [Column("TEMPLATE_ID")]
        public int TemplateId { get; set; }

        /// <summary>
        /// 模板名称
        /// </summary>
        [Required]
        [Column("TEMPLATE_NAME", TypeName = "VARCHAR2(100)")]
        [MaxLength(MaxTemplateNameLength)]
        public string TemplateName { get; set; } = string.Empty;

        /// <summary>
        /// 模板类型
        /// </summary>
        [Required]
        [Column("TEMPLATE_TYPE", TypeName = "VARCHAR2(20)")]
        [MaxLength(20)]
        public string TemplateType { get; set; } = string.Empty;

        /// <summary>
        /// 模板内容（支持参数占位符）
        /// </summary>
        [Required]
        [Column("TEMPLATE_CONTENT", TypeName = "CLOB")]
        public string TemplateContent { get; set; } = string.Empty;

        /// <summary>
        /// 模板描述
        /// </summary>
        [Column("DESCRIPTION", TypeName = "VARCHAR2(500)")]
        [MaxLength(MaxDescriptionLength)]
        public string? Description { get; set; }

        /// <summary>
        /// 优先级（1-5，数字越大优先级越高）
        /// </summary>
        [Required]
        [Column("PRIORITY")]
        [Range(1, 5, ErrorMessage = "优先级必须在1到5之间")]
        public int Priority { get; set; } = PriorityLevels.Normal;

        /// <summary>
        /// 是否启用
        /// </summary>
        [Required]
        [Column("IS_ACTIVE")]
        public int IsActive { get; set; } = 1;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Column("UPDATED_AT")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 创建者ID
        /// </summary>
        [Column("CREATED_BY")]
        public int? CreatedBy { get; set; }

        /// <summary>
        /// 创建者用户
        /// </summary>
        public virtual User? Creator { get; set; }

        /// <summary>
        /// 使用该模板的通知集合
        /// </summary>
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
