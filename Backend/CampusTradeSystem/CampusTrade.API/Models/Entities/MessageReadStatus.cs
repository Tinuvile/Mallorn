using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 消息已读状态实体类 - 统一管理所有类型消息的已读状态
    /// </summary>
    [Table("MESSAGE_READ_STATUS")]
    public class MessageReadStatus
    {
        /// <summary>
        /// 消息类型常量
        /// </summary>
        public static class MessageTypes
        {
            public const string Notification = "notification";
            public const string Bargain = "bargain";
            public const string Swap = "swap";
            public const string Exchange = "exchange";
        }

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
        /// 消息类型
        /// </summary>
        [Required]
        [Column("MESSAGE_TYPE", TypeName = "VARCHAR2(20)")]
        [MaxLength(20)]
        public string MessageType { get; set; } = string.Empty;

        /// <summary>
        /// 消息ID（对应各类型消息的主键）
        /// </summary>
        [Required]
        [Column("MESSAGE_ID")]
        public int MessageId { get; set; }

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
