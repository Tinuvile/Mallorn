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
        #region 常量定义
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
        #endregion

        #region 基本信息
        /// <summary>
        /// 模板ID
        /// </summary>
        [Key]
        [Column("TEMPLATE_ID", TypeName = "NUMBER")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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
        [Column("PRIORITY", TypeName = "NUMBER")]
        [Range(1, 5, ErrorMessage = "优先级必须在1到5之间")]
        public int Priority { get; set; } = PriorityLevels.Normal;

        /// <summary>
        /// 是否启用
        /// </summary>
        [Required]
        [Column("IS_ACTIVE", TypeName = "NUMBER")]
        public int IsActive { get; set; } = 1;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column("CREATED_AT", TypeName = "TIMESTAMP")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        [Column("UPDATED_AT", TypeName = "TIMESTAMP")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 创建者ID
        /// </summary>
        [Column("CREATED_BY", TypeName = "NUMBER")]
        public int? CreatedBy { get; set; }
        #endregion

        #region 导航属性
        /// <summary>
        /// 创建者用户
        /// </summary>
        public virtual User? Creator { get; set; }

        /// <summary>
        /// 使用该模板的通知集合
        /// </summary>
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        #endregion

        #region 计算属性
        /// <summary>
        /// 是否启用（Boolean形式）
        /// </summary>
        [NotMapped]
        public bool IsActiveTemplate => IsActive == 1;

        /// <summary>
        /// 是否为高优先级模板
        /// </summary>
        [NotMapped]
        public bool IsHighPriority => Priority >= PriorityLevels.High;

        /// <summary>
        /// 是否为系统模板（无创建者）
        /// </summary>
        [NotMapped]
        public bool IsSystemTemplate => !CreatedBy.HasValue;
        #endregion

        #region 业务方法
        /// <summary>
        /// 设置启用状态
        /// </summary>
        public void SetActive(bool isActive)
        {
            IsActive = isActive ? 1 : 0;
            UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// 更新模板内容
        /// </summary>
        public void UpdateContent(string content, int? updatedBy = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("模板内容不能为空", nameof(content));

            TemplateContent = content;
            UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// 渲染模板内容（替换参数占位符）
        /// </summary>
        public string RenderContent(Dictionary<string, object>? parameters = null)
        {
            var content = TemplateContent;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    var placeholder = $"{{{param.Key}}}";
                    content = content.Replace(placeholder, param.Value?.ToString() ?? "");
                }
            }

            return content;
        }

        /// <summary>
        /// 从JSON字符串渲染模板
        /// </summary>
        public string RenderContentFromJson(string? jsonParams)
        {
            if (string.IsNullOrWhiteSpace(jsonParams))
                return TemplateContent;

            try
            {
                var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonParams);
                return RenderContent(parameters);
            }
            catch
            {
                return TemplateContent; // 如果解析失败，返回原始模板
            }
        }

        /// <summary>
        /// 获取模板中的参数占位符
        /// </summary>
        public List<string> GetParameterPlaceholders()
        {
            var placeholders = new List<string>();
            var content = TemplateContent;
            var startIndex = 0;

            while (true)
            {
                var openIndex = content.IndexOf('{', startIndex);
                if (openIndex == -1) break;

                var closeIndex = content.IndexOf('}', openIndex);
                if (closeIndex == -1) break;

                var placeholder = content.Substring(openIndex + 1, closeIndex - openIndex - 1);
                if (!placeholders.Contains(placeholder))
                    placeholders.Add(placeholder);

                startIndex = closeIndex + 1;
            }

            return placeholders;
        }

        /// <summary>
        /// 验证模板内容是否有效
        /// </summary>
        public bool IsValidTemplate()
        {
            try
            {
                // 检查大括号是否配对
                var openCount = 0;
                foreach (var c in TemplateContent)
                {
                    if (c == '{') openCount++;
                    else if (c == '}') openCount--;

                    if (openCount < 0) return false;
                }
                return openCount == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取优先级显示名称
        /// </summary>
        public string GetPriorityDisplayName()
        {
            return Priority switch
            {
                PriorityLevels.Critical => "紧急",
                PriorityLevels.High => "高",
                PriorityLevels.Medium => "中",
                PriorityLevels.Normal => "普通",
                PriorityLevels.Low => "低",
                _ => "未知"
            };
        }

        /// <summary>
        /// 获取模板类型显示名称
        /// </summary>
        public string GetTemplateTypeDisplayName()
        {
            return TemplateType switch
            {
                TemplateTypes.ProductRelated => "商品相关",
                TemplateTypes.TransactionRelated => "交易相关",
                TemplateTypes.ReviewRelated => "评价相关",
                TemplateTypes.SystemNotification => "系统通知",
                _ => "未知类型"
            };
        }

        /// <summary>
        /// 获取模板内容预览（截取前100个字符）
        /// </summary>
        public string GetContentPreview()
        {
            if (string.IsNullOrWhiteSpace(TemplateContent))
                return "无内容";

            const int maxLength = 100;
            return TemplateContent.Length <= maxLength
                ? TemplateContent
                : TemplateContent.Substring(0, maxLength) + "...";
        }
        #endregion

        #region 静态方法
        /// <summary>
        /// 获取所有可用模板类型
        /// </summary>
        public static List<string> GetAvailableTemplateTypes()
        {
            return new List<string>
            {
                TemplateTypes.ProductRelated,
                TemplateTypes.TransactionRelated,
                TemplateTypes.ReviewRelated,
                TemplateTypes.SystemNotification
            };
        }

        /// <summary>
        /// 验证模板类型是否有效
        /// </summary>
        public static bool IsValidTemplateType(string templateType)
        {
            return GetAvailableTemplateTypes().Contains(templateType);
        }

        /// <summary>
        /// 验证优先级是否有效
        /// </summary>
        public static bool IsValidPriority(int priority)
        {
            return priority >= PriorityLevels.Low && priority <= PriorityLevels.Critical;
        }

        /// <summary>
        /// 创建商品相关模板
        /// </summary>
        public static NotificationTemplate CreateProductTemplate(string name, string content,
            string? description = null, int priority = PriorityLevels.Normal, int? createdBy = null)
        {
            return new NotificationTemplate
            {
                TemplateName = name,
                TemplateType = TemplateTypes.ProductRelated,
                TemplateContent = content,
                Description = description,
                Priority = priority,
                CreatedBy = createdBy,
                CreatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// 创建交易相关模板
        /// </summary>
        public static NotificationTemplate CreateTransactionTemplate(string name, string content,
            string? description = null, int priority = PriorityLevels.Normal, int? createdBy = null)
        {
            return new NotificationTemplate
            {
                TemplateName = name,
                TemplateType = TemplateTypes.TransactionRelated,
                TemplateContent = content,
                Description = description,
                Priority = priority,
                CreatedBy = createdBy,
                CreatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// 创建系统通知模板
        /// </summary>
        public static NotificationTemplate CreateSystemTemplate(string name, string content,
            string? description = null, int priority = PriorityLevels.Normal)
        {
            return new NotificationTemplate
            {
                TemplateName = name,
                TemplateType = TemplateTypes.SystemNotification,
                TemplateContent = content,
                Description = description,
                Priority = priority,
                CreatedBy = null, // 系统模板无创建者
                CreatedAt = DateTime.Now
            };
        }
        #endregion
    }
}
