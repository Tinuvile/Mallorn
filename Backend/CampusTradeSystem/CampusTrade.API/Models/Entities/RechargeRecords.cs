using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 充值记录实体类
    /// </summary>
    public class RechargeRecord
    {
        /// <summary>
        /// 充值记录ID - 主键，由序列和触发器自增
        /// </summary>
        [Key]
        [Column("RECHARGE_ID")]
        public int RechargeId { get; set; }

        /// <summary>
        /// 用户ID - 外键
        /// </summary>
        [Required]
        [Column("USER_ID")]
        public int UserId { get; set; }

        /// <summary>
        /// 充值金额
        /// </summary>
        [Required]
        [Column("AMOUNT", TypeName = "decimal(10,2)")]
        [Range(0.01, 99999999.99, ErrorMessage = "充值金额必须在0.01到99999999.99之间")]
        public decimal Amount { get; set; }

        /// <summary>
        /// 充值状态（默认值由Oracle处理）
        /// </summary>
        [Required]
        [Column("STATUS", TypeName = "VARCHAR2(20)")]
        [MaxLength(20)]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间（由Oracle DEFAULT处理）
        /// </summary>
        [Required]
        [Column("CREATE_TIME")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        [Column("COMPLETE_TIME")]
        public DateTime? CompleteTime { get; set; }

        /// <summary>
        /// 关联的用户
        /// 外键关系：recharge_records.user_id -> users.user_id
        /// </summary>
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// 关联的虚拟账户
        /// 通过用户ID间接关联
        /// </summary>
        public virtual VirtualAccount? VirtualAccount { get; set; }
    }
}
