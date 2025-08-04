using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 商品实体类 - 对应 Oracle 数据库中的 PRODUCTS 表
    /// 用于存储校园交易平台的商品信息
    /// </summary>
    [Table("PRODUCTS")]
    public class Product
    {
        /// <summary>
        /// 商品ID - 主键，对应Oracle中的product_id字段
        /// 由序列生成，非自增
        /// </summary>
        [Key]
        [Column("PRODUCT_ID", TypeName = "NUMBER")]
        public int ProductId { get; set; }

        /// <summary>
        /// 发布用户ID - 外键，对应Oracle中的user_id字段
        /// 关联到users表，标识商品发布者
        /// </summary>
        [Required]
        [Column("USER_ID", TypeName = "NUMBER")]
        public int UserId { get; set; }

        /// <summary>
        /// 商品分类ID - 外键，对应Oracle中的category_id字段
        /// 关联到categories表，商品所属分类
        /// </summary>
        [Required]
        [Column("CATEGORY_ID", TypeName = "NUMBER")]
        public int CategoryId { get; set; }

        /// <summary>
        /// 商品标题 - 对应Oracle中的title字段
        /// 商品的主要描述标题，最大长度100字符
        /// </summary>
        [Required]
        [Column("TITLE", TypeName = "VARCHAR2(100)")]
        [StringLength(100, ErrorMessage = "商品标题不能超过100个字符")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 商品详细描述 - 对应Oracle中的description字段
        /// 使用CLOB类型支持长文本描述，可为空
        /// </summary>
        [Column("DESCRIPTION", TypeName = "CLOB")]
        public string? Description { get; set; }

        /// <summary>
        /// 基础价格 - 对应Oracle中的base_price字段
        /// NUMBER(10,2)类型，支持最大8位整数2位小数
        /// </summary>
        [Required]
        [Column("BASE_PRICE", TypeName = "NUMBER(10,2)")]
        [Range(0.01, 99999999.99, ErrorMessage = "价格必须大于0且不超过99999999.99")]
        public decimal BasePrice { get; set; }

        /// <summary>
        /// 发布时间 - 对应Oracle中的publish_time字段
        /// 默认为当前时间戳
        /// </summary>
        [Column("PUBLISH_TIME", TypeName = "TIMESTAMP")]
        public DateTime PublishTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 浏览次数 - 对应Oracle中的view_count字段
        /// 记录商品被查看的次数，默认值0
        /// </summary>
        [Column("VIEW_COUNT", TypeName = "NUMBER")]
        [Range(0, int.MaxValue, ErrorMessage = "浏览次数不能为负数")]
        public int ViewCount { get; set; } = 0;

        /// <summary>
        /// 自动下架时间 - 对应Oracle中的auto_remove_time字段
        /// 可为空，到期自动下架商品
        /// </summary>
        [Column("AUTO_REMOVE_TIME", TypeName = "TIMESTAMP")]
        public DateTime? AutoRemoveTime { get; set; }

        /// <summary>
        /// 商品状态 - 对应Oracle中的status字段
        /// 限制值：在售、已下架、交易中，默认值"在售"
        /// </summary>
        [Required]
        [Column("STATUS", TypeName = "VARCHAR2(20)")]
        [StringLength(20)]
        public string Status { get; set; } = ProductStatus.OnSale;

        #region 导航属性

        /// <summary>
        /// 商品发布者 - 一对一关系
        /// 通过UserId外键关联Users表
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        /// <summary>
        /// 商品分类 - 多对一关系
        /// 通过CategoryId外键关联Categories表
        /// </summary>
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        /// <summary>
        /// 商品图片集合 - 一对多关系
        /// 一个商品可以有多张图片
        /// </summary>
        public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

        /// <summary>
        /// 作为提供商品的换物请求集合 - 一对多关系
        /// 该商品作为发起者愿意交换出的商品的所有换物请求
        /// </summary>
        public virtual ICollection<ExchangeRequest> OfferExchangeRequests { get; set; } = new List<ExchangeRequest>();

        /// <summary>
        /// 作为请求商品的换物请求集合 - 一对多关系
        /// 该商品作为发起者想要获得的商品的所有换物请求
        /// </summary>
        public virtual ICollection<ExchangeRequest> RequestExchangeRequests { get; set; } = new List<ExchangeRequest>();

        #endregion

        #region 静态常量和枚举

        /// <summary>
        /// 商品状态的有效值 - 与Oracle检查约束保持一致
        /// </summary>
        public static class ProductStatus
        {
            /// <summary>
            /// 在售 - 商品正常销售中
            /// </summary>
            public const string OnSale = "在售";

            /// <summary>
            /// 已下架 - 商品已被下架，不可购买
            /// </summary>
            public const string OffShelf = "已下架";

            /// <summary>
            /// 交易中 - 商品正在交易过程中
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

        #endregion

        #region 业务方法

        /// <summary>
        /// 检查商品状态是否有效
        /// </summary>
        /// <param name="status">商品状态</param>
        /// <returns>是否有效</returns>
        public static bool IsValidStatus(string status)
        {
            return status == ProductStatus.OnSale ||
                   status == ProductStatus.OffShelf ||
                   status == ProductStatus.InTransaction;
        }

        /// <summary>
        /// 验证当前实例的状态是否有效
        /// </summary>
        /// <returns>是否有效</returns>
        public bool IsValidStatus()
        {
            return IsValidStatus(Status);
        }

        /// <summary>
        /// 判断商品是否已过期（需要自动下架）
        /// </summary>
        /// <returns>是否过期</returns>
        public bool IsExpired()
        {
            return AutoRemoveTime.HasValue && AutoRemoveTime.Value < DateTime.Now;
        }

        /// <summary>
        /// 判断商品是否可购买
        /// </summary>
        /// <returns>是否可购买</returns>
        public bool IsAvailableForPurchase()
        {
            return Status == ProductStatus.OnSale && !IsExpired();
        }

        /// <summary>
        /// 增加浏览次数
        /// </summary>
        public void IncrementViewCount()
        {
            ViewCount++;
        }

        /// <summary>
        /// 更新商品状态
        /// </summary>
        /// <param name="newStatus">新状态</param>
        /// <exception cref="ArgumentException">状态无效时抛出异常</exception>
        public void UpdateStatus(string newStatus)
        {
            if (!IsValidStatus(newStatus))
                throw new ArgumentException($"无效的商品状态: {newStatus}");

            Status = newStatus;
        }

        /// <summary>
        /// 根据价格获取价格范围
        /// </summary>
        /// <returns>价格范围枚举</returns>
        public PriceRange GetPriceRange()
        {
            return BasePrice switch
            {
                < 50 => PriceRange.Low,
                < 200 => PriceRange.Medium,
                < 500 => PriceRange.High,
                _ => PriceRange.VeryHigh
            };
        }

        /// <summary>
        /// 设置自动下架时间（从当前时间开始计算）
        /// </summary>
        /// <param name="days">天数</param>
        public void SetAutoRemoveTime(int days)
        {
            if (days <= 0)
                throw new ArgumentException("天数必须大于0");

            AutoRemoveTime = DateTime.Now.AddDays(days);
        }

        #endregion
    }
}
