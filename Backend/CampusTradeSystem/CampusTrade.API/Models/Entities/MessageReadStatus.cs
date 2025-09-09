using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 消息已读状态实体类 - 与通知一一对应的已读状态管理
    /// </summary>
    [Table("MESSAGE_READ_STATUS")]
    public class MessageReadStatus
    {
        /// <summary>
        /// 已读状态ID
        /// </summary>
        [Key]
        [Column("READ_STATUS_ID")]
        public int ReadStatusId { get; set; }

        /// <summary>
        /// 用户ID（外键）
        /// </summary>
        [Required]
        [Column("USER_ID")]
        public int UserId { get; set; }

        /// <summary>
        /// 通知ID（对应通知表的主键）
        /// </summary>
        [Required]
        [Column("NOTIFICATION_ID")]
        public int NotificationId { get; set; }

        /// <summary>
        /// 是否已读（0=未读, 1=已读）
        /// </summary>
        [Required]
        [Column("IS_READ")]
        public int IsRead { get; set; } = 0;

        /// <summary>
        /// 已读时间
        /// </summary>
        [Column("READ_AT")]
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 关联的用户
        /// </summary>
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// 关联的通知
        /// </summary>
        public virtual Notification Notification { get; set; } = null!;

        /// <summary>
        /// 是否已读（布尔属性，便于使用）
        /// </summary>
        [NotMapped]
        public bool IsReadBool
        {
            get => IsRead == 1;
            set => IsRead = value ? 1 : 0;
        }

        /// <summary>
        /// 标记为已读
        /// </summary>
        public void MarkAsRead()
        {
            IsRead = 1;
            ReadAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 标记为未读
        /// </summary>
        public void MarkAsUnread()
        {
            IsRead = 0;
            ReadAt = null;
        }
    }
}
