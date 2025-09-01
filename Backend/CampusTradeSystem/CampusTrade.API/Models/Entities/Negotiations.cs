using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 议价实体类
    /// </summary>
    public class Negotiation
    {
        /// <summary>
        /// 议价ID - 主键，自增
        /// </summary>
        [Key]
        [Column("NEGOTIATION_ID")]
        public int NegotiationId { get; set; }

        /// <summary>
        /// 订单ID - 外键
        /// </summary>
        [Required]
        [Column("ORDER_ID")]
        public int OrderId { get; set; }

        /// <summary>
        /// 提议价格
        /// </summary>
        [Required]
        [Column("PROPOSED_PRICE", TypeName = "decimal(10,2)")]
        [Range(0.01, 99999999.99, ErrorMessage = "提议价格必须在0.01到99999999.99之间")]
        public decimal ProposedPrice { get; set; }

        /// <summary>
        /// 议价状态
        /// </summary>
        [Required]
        [Column("STATUS", TypeName = "VARCHAR2(20)")]
        [MaxLength(20)]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 关联的订单
        /// 外键关系：negotiations.order_id -> orders.order_id
        /// </summary>
        public virtual Order Order { get; set; } = null!;

        /// <summary>
        /// 有效的议价状态
        /// </summary>
        public static readonly HashSet<string> ValidStatuses = new() { "等待回应", "接受", "拒绝", "反报价" };

        /// <summary>
        /// 默认超时时间（小时）
        /// </summary>
        public const int DefaultTimeoutHours = 24;

        /// <summary>
        /// 最大议价时间（小时）
        /// </summary>
        public const int MaxNegotiationHours = 168;

        /// <summary>
        /// 最小折扣率
        /// </summary>
        public const decimal MinDiscountRate = 0.5m;

        /// <summary>
        /// 最大加价率
        /// </summary>
        public const decimal MaxMarkupRate = 1.5m;
    }
}
