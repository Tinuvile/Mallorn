using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 换物请求实体类
    /// </summary>
    public class ExchangeRequest
    {
        /// <summary>
        /// 换物请求ID - 主键，外键
        /// </summary>
        [Key]
        [Column("EXCHANGE_ID")]
        public int ExchangeId { get; set; }

        /// <summary>
        /// 提供商品ID - 外键
        /// </summary>
        [Required]
        [Column("OFFER_PRODUCT_ID")]
        public int OfferProductId { get; set; }

        /// <summary>
        /// 请求商品ID - 外键
        /// </summary>
        [Required]
        [Column("REQUEST_PRODUCT_ID")]
        public int RequestProductId { get; set; }

        /// <summary>
        /// 交换条件
        /// </summary>
        [Column("TERMS", TypeName = "CLOB")]
        public string? Terms { get; set; }

        /// <summary>
        /// 交换状态
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
        /// 关联的抽象订单
        /// 外键关系：exchange_requests.exchange_id -> abstract_orders.abstract_order_id
        /// </summary>
        public virtual AbstractOrder AbstractOrder { get; set; } = null!;

        /// <summary>
        /// 提供的商品
        /// 外键关系：exchange_requests.offer_product_id -> products.product_id
        /// </summary>
        public virtual Product OfferProduct { get; set; } = null!;

        /// <summary>
        /// 请求的商品
        /// 外键关系：exchange_requests.request_product_id -> products.product_id
        /// </summary>
        public virtual Product RequestProduct { get; set; } = null!;
    }
}
