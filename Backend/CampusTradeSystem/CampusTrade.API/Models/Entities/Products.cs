using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 商品实体类 - 对应 Oracle 数据库中的 PRODUCTS 表
    /// </summary>
    [Table("PRODUCTS")]
    public class Product
    {
        /// <summary>
        /// 商品ID - 主键，对应Oracle中的product_id字段
        /// </summary>
        [Key]
        [Column("PRODUCT_ID")]
        public int ProductId { get; set; }

        /// <summary>
        /// 发布用户ID - 外键，对应Oracle中的user_id字段
        /// 关联到users表，标识商品发布者
        /// </summary>
        [Required]
        [Column("USER_ID")]
        public int UserId { get; set; }

        /// <summary>
        /// 商品分类ID - 外键，对应Oracle中的category_id字段
        /// 关联到categories表，商品所属分类
        /// </summary>
        [Required]
        [Column("CATEGORY_ID")]
        public int CategoryId { get; set; }

        /// <summary>
        /// 商品标题 - 对应Oracle中的title字段
        /// </summary>
        [Required]
        [Column("TITLE", TypeName = "VARCHAR2(100)")]
        [StringLength(100, ErrorMessage = "商品标题不能超过100个字符")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 商品描述 - 对应Oracle中的description字段
        /// 详细的商品描述信息，可以为空
        /// </summary>
        [Column("DESCRIPTION", TypeName = "CLOB")]
        public string? Description { get; set; }

        /// <summary>
        /// 基础价格 - 对应Oracle中的base_price字段，精度为10位数字2位小数
        /// </summary>
        [Required]
        [Column("BASE_PRICE", TypeName = "NUMBER(10,2)")]
        [Range(0.01, 999999.99, ErrorMessage = "商品价格必须在合理范围内")]
        public decimal BasePrice { get; set; }

        /// <summary>
        /// 发布时间 - 对应Oracle中的publish_time字段，默认为当前时间=
        /// </summary>
        [Column("PUBLISH_TIME")]
        public DateTime PublishTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 浏览次数 - 对应Oracle中的view_count字段，默认为0（由Oracle处理）
        /// 记录商品被查看的次数
        /// </summary>
        [Column("VIEW_COUNT")]
        [Range(0, int.MaxValue, ErrorMessage = "浏览次数不能为负数")]
        public int ViewCount { get; set; }

        /// <summary>
        /// 自动下架时间 - 对应Oracle中的auto_remove_time字段
        /// 可以为空，如果设置了时间，到期后自动下架
        /// </summary>
        [Column("AUTO_REMOVE_TIME")]
        public DateTime? AutoRemoveTime { get; set; }

        /// <summary>
        /// 商品状态 - 对应Oracle中的status字段
        /// 限制值：在售、已下架、交易中
        /// </summary>
        [Required]
        [Column("STATUS", TypeName = "VARCHAR2(20)")]
        [StringLength(20)]
        public string Status { get; set; } = ProductStatus.OnSale;

        /// <summary>
        /// 发布商品的用户 - 多对一关系
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// 商品所属分类 - 多对一关系
        /// </summary>
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; } = null!;

        /// <summary>
        /// 商品图片集合
        /// </summary>
        public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

        /// <summary>
        /// 提供该商品的换物请求集合
        /// </summary>
        public virtual ICollection<ExchangeRequest> OfferExchangeRequests { get; set; } = new List<ExchangeRequest>();

        /// <summary>
        /// 请求该商品的换物请求集合
        /// </summary>
        public virtual ICollection<ExchangeRequest> RequestExchangeRequests { get; set; } = new List<ExchangeRequest>();

        /// <summary>
        /// 商品状态的有效值
        /// </summary>
        public static class ProductStatus
        {
            /// <summary>
            /// 在售 - 商品正在销售中
            /// </summary>
            public const string OnSale = "在售";

            /// <summary>
            /// 已下架 - 商品已下架
            /// </summary>
            public const string OffShelf = "已下架";

            /// <summary>
            /// 交易中 - 商品正在交易中
            /// </summary>
            public const string InTransaction = "交易中";
        }

        /// <summary>
        /// 价格范围枚举（用于筛选）
        /// </summary>
        public enum PriceRange
        {
            /// <summary>
            /// 0-50元
            /// </summary>
            Low = 0,

            /// <summary>
            /// 50-200元
            /// </summary>
            Medium = 1,

            /// <summary>
            /// 200-500元
            /// </summary>
            High = 2,

            /// <summary>
            /// 500元以上
            /// </summary>
            VeryHigh = 3
        }
    }
}
