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
        #region 常量定义
        public static class SendStatuses
        {
            public const string Pending = "待发送";
            public const string Success = "成功";
            public const string Failed = "失败";
        }

        // 重试次数限制
        public const int MaxRetryCount = 5;
        public const int DefaultRetryIntervalMinutes = 5;
        #endregion

        #region 基本信息
        /// <summary>
        /// 通知ID
        /// </summary>
        [Key]
        [Column("NOTIFICATION_ID", TypeName = "NUMBER")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NotificationId { get; set; }

        /// <summary>
        /// 模板ID（外键）
        /// </summary>
        [Required]
        [Column("TEMPLATE_ID", TypeName = "NUMBER")]
        public int TemplateId { get; set; }

        /// <summary>
        /// 接收者用户ID（外键）
        /// </summary>
        [Required]
        [Column("RECIPIENT_ID", TypeName = "NUMBER")]
        public int RecipientId { get; set; }

        /// <summary>
        /// 订单ID（外键，可选，仅订单相关通知需要）
        /// </summary>
        [Column("ORDER_ID", TypeName = "NUMBER")]
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
        public string SendStatus { get; set; } = SendStatuses.Pending;

        /// <summary>
        /// 重试次数
        /// </summary>
        [Required]
        [Column("RETRY_COUNT", TypeName = "NUMBER")]
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// 最后尝试发送时间
        /// </summary>
        [Required]
        [Column("LAST_ATTEMPT_TIME", TypeName = "TIMESTAMP")]
        public DateTime LastAttemptTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column("CREATED_AT", TypeName = "TIMESTAMP")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 发送成功时间
        /// </summary>
        [Column("SENT_AT", TypeName = "TIMESTAMP")]
        public DateTime? SentAt { get; set; }
        #endregion

        #region 导航属性
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
        #endregion

        #region 计算属性
        /// <summary>
        /// 是否为订单相关通知
        /// </summary>
        [NotMapped]
        public bool IsOrderRelated => OrderId.HasValue;

        /// <summary>
        /// 是否已发送成功
        /// </summary>
        [NotMapped]
        public bool IsSent => SendStatus == SendStatuses.Success && SentAt.HasValue;

        /// <summary>
        /// 发送耗时（如果已发送）
        /// </summary>
        [NotMapped]
        public TimeSpan? SendDuration => IsSent ? SentAt - CreatedAt : null;
        #endregion

        #region 业务方法
        /// <summary>
        /// 是否为待发送状态
        /// </summary>
        public bool IsPending()
        {
            return SendStatus == SendStatuses.Pending;
        }

        /// <summary>
        /// 是否发送成功
        /// </summary>
        public bool IsSuccessful()
        {
            return SendStatus == SendStatuses.Success;
        }

        /// <summary>
        /// 是否发送失败
        /// </summary>
        public bool IsFailed()
        {
            return SendStatus == SendStatuses.Failed;
        }

        /// <summary>
        /// 是否可以重试
        /// </summary>
        public bool CanRetry()
        {
            return !IsSuccessful() && RetryCount < MaxRetryCount;
        }

        /// <summary>
        /// 是否已达到最大重试次数
        /// </summary>
        public bool HasReachedMaxRetries()
        {
            return RetryCount >= MaxRetryCount;
        }

        /// <summary>
        /// 标记为发送成功
        /// </summary>
        public void MarkAsSuccessful()
        {
            SendStatus = SendStatuses.Success;
            SentAt = DateTime.Now;
            LastAttemptTime = DateTime.Now;
        }

        /// <summary>
        /// 标记为发送失败并增加重试次数
        /// </summary>
        public void MarkAsFailed()
        {
            SendStatus = SendStatuses.Failed;
            RetryCount++;
            LastAttemptTime = DateTime.Now;
        }

        /// <summary>
        /// 重置重试状态
        /// </summary>
        public void ResetRetry()
        {
            SendStatus = SendStatuses.Pending;
            RetryCount = 0;
            LastAttemptTime = DateTime.Now;
            SentAt = null;
        }

        /// <summary>
        /// 是否需要立即重试（基于时间间隔）
        /// </summary>
        public bool ShouldRetryNow()
        {
            if (!CanRetry()) return false;

            var nextRetryTime = LastAttemptTime.AddMinutes(DefaultRetryIntervalMinutes * Math.Pow(2, RetryCount));
            return DateTime.Now >= nextRetryTime;
        }

        /// <summary>
        /// 获取下次重试时间
        /// </summary>
        public DateTime GetNextRetryTime()
        {
            if (!CanRetry()) return DateTime.MaxValue;

            return LastAttemptTime.AddMinutes(DefaultRetryIntervalMinutes * Math.Pow(2, RetryCount));
        }

        /// <summary>
        /// 获取发送状态显示名称
        /// </summary>
        public string GetSendStatusDisplayName()
        {
            return SendStatus switch
            {
                SendStatuses.Pending => "等待发送",
                SendStatuses.Success => "发送成功",
                SendStatuses.Failed => "发送失败",
                _ => "未知状态"
            };
        }

        /// <summary>
        /// 获取渲染后的通知内容
        /// </summary>
        public string GetRenderedContent()
        {
            if (Template == null)
                return "模板未加载";

            return Template.RenderContentFromJson(TemplateParams);
        }

        /// <summary>
        /// 设置模板参数
        /// </summary>
        public void SetTemplateParameters(Dictionary<string, object> parameters)
        {
            TemplateParams = JsonSerializer.Serialize(parameters);
        }

        /// <summary>
        /// 获取模板参数
        /// </summary>
        public Dictionary<string, object>? GetTemplateParameters()
        {
            if (string.IsNullOrWhiteSpace(TemplateParams))
                return null;

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(TemplateParams);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 是否为高优先级通知
        /// </summary>
        public bool IsHighPriority()
        {
            return Template?.IsHighPriority ?? false;
        }

        /// <summary>
        /// 获取通知摘要
        /// </summary>
        public string GetNotificationSummary()
        {
            var content = GetRenderedContent();
            const int maxLength = 50;

            if (content.Length <= maxLength)
                return content;

            return content.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// 获取重试信息描述
        /// </summary>
        public string GetRetryInfo()
        {
            if (IsSuccessful())
                return "发送成功";

            if (HasReachedMaxRetries())
                return $"已达到最大重试次数({MaxRetryCount}次)";

            if (CanRetry())
                return $"可重试 ({RetryCount}/{MaxRetryCount})";

            return "无需重试";
        }

        /// <summary>
        /// 获取通知类型（基于模板类型）
        /// </summary>
        public string GetNotificationType()
        {
            return Template?.TemplateType ?? "未知类型";
        }
        #endregion

        #region 静态方法
        /// <summary>
        /// 获取所有可用发送状态
        /// </summary>
        public static List<string> GetAvailableSendStatuses()
        {
            return new List<string> { SendStatuses.Pending, SendStatuses.Success, SendStatuses.Failed };
        }

        /// <summary>
        /// 验证发送状态是否有效
        /// </summary>
        public static bool IsValidSendStatus(string sendStatus)
        {
            return GetAvailableSendStatuses().Contains(sendStatus);
        }

        /// <summary>
        /// 创建订单相关通知
        /// </summary>
        public static Notification CreateOrderNotification(int templateId, int recipientId, int orderId,
            Dictionary<string, object>? parameters = null)
        {
            var notification = new Notification
            {
                TemplateId = templateId,
                RecipientId = recipientId,
                OrderId = orderId,
                SendStatus = SendStatuses.Pending,
                RetryCount = 0,
                CreatedAt = DateTime.Now,
                LastAttemptTime = DateTime.Now
            };

            if (parameters != null)
                notification.SetTemplateParameters(parameters);

            return notification;
        }

        /// <summary>
        /// 创建系统通知
        /// </summary>
        public static Notification CreateSystemNotification(int templateId, int recipientId,
            Dictionary<string, object>? parameters = null)
        {
            var notification = new Notification
            {
                TemplateId = templateId,
                RecipientId = recipientId,
                OrderId = null, // 系统通知不关联订单
                SendStatus = SendStatuses.Pending,
                RetryCount = 0,
                CreatedAt = DateTime.Now,
                LastAttemptTime = DateTime.Now
            };

            if (parameters != null)
                notification.SetTemplateParameters(parameters);

            return notification;
        }

        /// <summary>
        /// 创建商品相关通知
        /// </summary>
        public static Notification CreateProductNotification(int templateId, int recipientId,
            int productId, Dictionary<string, object>? parameters = null)
        {
            var baseParams = new Dictionary<string, object> { { "productId", productId } };

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    baseParams[param.Key] = param.Value;
                }
            }

            var notification = new Notification
            {
                TemplateId = templateId,
                RecipientId = recipientId,
                OrderId = null,
                SendStatus = SendStatuses.Pending,
                RetryCount = 0,
                CreatedAt = DateTime.Now,
                LastAttemptTime = DateTime.Now
            };

            notification.SetTemplateParameters(baseParams);
            return notification;
        }

        /// <summary>
        /// 批量创建通知
        /// </summary>
        public static List<Notification> CreateBatchNotifications(int templateId, List<int> recipientIds,
            int? orderId = null, Dictionary<string, object>? parameters = null)
        {
            var notifications = new List<Notification>();

            foreach (var recipientId in recipientIds)
            {
                var notification = new Notification
                {
                    TemplateId = templateId,
                    RecipientId = recipientId,
                    OrderId = orderId,
                    SendStatus = SendStatuses.Pending,
                    RetryCount = 0,
                    CreatedAt = DateTime.Now,
                    LastAttemptTime = DateTime.Now
                };

                if (parameters != null)
                    notification.SetTemplateParameters(parameters);

                notifications.Add(notification);
            }

            return notifications;
        }
        #endregion
    }
}
