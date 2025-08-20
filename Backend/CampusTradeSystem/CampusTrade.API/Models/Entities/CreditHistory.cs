using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 信用变更记录实体 - 对应 Oracle 数据库中的 CREDIT_HISTORY 表
    /// </summary>
    [Table("CREDIT_HISTORY")]
    public class CreditHistory
    {
        /// <summary>
        /// 日志ID - 主键，对应Oracle中的log_id字段，自增（在Oracle中进行）
        /// </summary>
        [Key]
        [Column("LOG_ID")]
        public int LogId { get; set; }

        /// <summary>
        /// 用户ID - 外键，对应Oracle中的user_id字段
        /// </summary>
        [Required]
        [Column("USER_ID")]
        public int UserId { get; set; }

        /// <summary>
        /// 变更类型 - 对应Oracle中的change_type字段
        /// 限制值：交易完成、举报处罚、好评奖励
        /// </summary>
        [Required]
        [Column("CHANGE_TYPE", TypeName = "VARCHAR2(20)")]
        [StringLength(20)]
        public string ChangeType { get; set; } = string.Empty;

        /// <summary>
        /// 新信用分数 - 对应Oracle中的new_score字段，精度为3位数字1位小数
        /// </summary>
        [Required]
        [Column("NEW_SCORE", TypeName = "NUMBER(3,1)")]
        [Range(0.0, 100.0, ErrorMessage = "信用分数必须在0-100之间")]
        public decimal NewScore { get; set; }

        /// <summary>
        /// 创建时间 - 对应Oracle中的created_at字段，默认为当前时间（由Oracle Default处理）
        /// </summary>
        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; }

        // 导航属性：关联的用户
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        /// <summary>
        /// 变更类型的有效值
        /// </summary>
        public static class ChangeTypes
        {
            public const string TransactionCompleted = "交易完成";
            public const string ReportPenalty = "举报处罚";
            public const string PositiveReviewReward = "好评奖励";
        }
    }
}
