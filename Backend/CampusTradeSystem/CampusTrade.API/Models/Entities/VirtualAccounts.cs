using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 虚拟账户实体 - 对应 Oracle 数据库中的 VIRTUAL_ACCOUNTS 表
    /// 管理用户的虚拟余额
    /// </summary>
    [Table("VIRTUAL_ACCOUNTS")]
    public class VirtualAccount
    {
        /// <summary>
        /// 账户ID - 主键，对应Oracle中的account_id字段，由序列和触发器自增
        /// </summary>
        [Key]
        [Column("ACCOUNT_ID")]
        public int AccountId { get; set; }

        /// <summary>
        /// 用户ID - 外键，对应Oracle中的user_id字段
        /// </summary>
        [Required]
        [Column("USER_ID")]
        public int UserId { get; set; }

        /// <summary>
        /// 账户余额 - 对应Oracle中的balance字段，精度为10位数字2位小数，默认值0.00（由Oracle处理）
        /// </summary>
        [Required]
        [Column("BALANCE", TypeName = "NUMBER(10,2)")]
        [Range(0, 99999999.99, ErrorMessage = "余额不能为负数且不能超过99999999.99")]
        public decimal Balance { get; set; }

        /// <summary>
        /// 创建时间 - 对应Oracle中的created_at字段，默认为当前时间（由Oracle处理）
        /// </summary>
        [Required]
        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 关联的用户 - 一对一关系
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// 充值记录集合 - 一对多关系
        /// </summary>
        public virtual ICollection<RechargeRecord> RechargeRecords { get; set; } = new List<RechargeRecord>();

        /// <summary>
        /// 最大余额限制
        /// </summary>
        public const decimal MaxBalance = 99999999.99m;

        /// <summary>
        /// 最小充值金额
        /// </summary>
        public const decimal MinRechargeAmount = 0.01m;

        /// <summary>
        /// 最大充值金额
        /// </summary>
        public const decimal MaxRechargeAmount = 50000.00m;
    }
}
