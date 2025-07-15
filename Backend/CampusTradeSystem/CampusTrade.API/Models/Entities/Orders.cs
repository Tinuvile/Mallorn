using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 订单实体类 - 对应 Oracle 数据库中的 ORDERS 表
    /// 表示普通的商品购买订单，继承自抽象订单
    /// </summary>
    [Table("ORDERS")]
    public class Order
    {
        /// <summary>
        /// 订单ID - 主键，对应Oracle中的order_id字段
        /// 同时也是外键，引用abstract_orders表的abstract_order_id
        /// 由ORDER_SEQ序列和触发器生成
        /// </summary>
        [Key]
        [Column("ORDER_ID", TypeName = "NUMBER")]
        public int OrderId { get; set; }

        /// <summary>
        /// 买家用户ID - 外键，对应Oracle中的buyer_id字段
        /// 关联到users表，标识订单的买家
        /// </summary>
        [Required]
        [Column("BUYER_ID", TypeName = "NUMBER")]
        public int BuyerId { get; set; }

        /// <summary>
        /// 卖家用户ID - 外键，对应Oracle中的seller_id字段
        /// 关联到users表，标识订单的卖家
        /// </summary>
        [Required]
        [Column("SELLER_ID", TypeName = "NUMBER")]
        public int SellerId { get; set; }

        /// <summary>
        /// 商品ID - 外键，对应Oracle中的product_id字段
        /// 关联到products表，标识订单的商品
        /// </summary>
        [Required]
        [Column("PRODUCT_ID", TypeName = "NUMBER")]
        public int ProductId { get; set; }

        /// <summary>
        /// 订单总金额 - 对应Oracle中的total_amount字段
        /// NUMBER(10,2)类型，可为空（议价订单可能暂时没有确定金额）
        /// </summary>
        [Column("TOTAL_AMOUNT", TypeName = "NUMBER(10,2)")]
        [Range(0, 99999999.99, ErrorMessage = "订单金额必须大于等于0且不超过99999999.99")]
        public decimal? TotalAmount { get; set; }

        /// <summary>
        /// 订单状态 - 对应Oracle中的status字段
        /// 限制值：待付款、已付款、已发货、已送达、已完成、已取消，默认值"待付款"
        /// </summary>
        [Required]
        [Column("STATUS", TypeName = "VARCHAR2(20)")]
        [StringLength(20)]
        public string Status { get; set; } = OrderStatus.PendingPayment;

        /// <summary>
        /// 订单创建时间 - 对应Oracle中的create_time字段
        /// 默认为当前时间戳
        /// </summary>
        [Column("CREATE_TIME", TypeName = "TIMESTAMP")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 订单过期时间 - 对应Oracle中的expire_time字段
        /// 触发器自动设置为创建时间+30分钟，可为空
        /// </summary>
        [Column("EXPIRE_TIME", TypeName = "TIMESTAMP")]
        public DateTime? ExpireTime { get; set; }

        /// <summary>
        /// 最终成交价格 - 对应Oracle中的final_price字段
        /// 经过议价后的最终价格，可为空
        /// </summary>
        [Column("FINAL_PRICE", TypeName = "NUMBER(10,2)")]
        [Range(0, 99999999.99, ErrorMessage = "最终价格必须大于等于0且不超过99999999.99")]
        public decimal? FinalPrice { get; set; }

        #region 导航属性

        /// <summary>
        /// 对应的抽象订单 - 一对一关系
        /// 通过OrderId外键关联AbstractOrders表
        /// </summary>
        [ForeignKey("OrderId")]
        public virtual AbstractOrder? AbstractOrder { get; set; }

        /// <summary>
        /// 买家信息 - 多对一关系
        /// 通过BuyerId外键关联Users表
        /// </summary>
        [ForeignKey("BuyerId")]
        public virtual User? Buyer { get; set; }

        /// <summary>
        /// 卖家信息 - 多对一关系
        /// 通过SellerId外键关联Users表
        /// </summary>
        [ForeignKey("SellerId")]
        public virtual User? Seller { get; set; }

        /// <summary>
        /// 订单商品 - 多对一关系
        /// 通过ProductId外键关联Products表
        /// </summary>
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        /// <summary>
        /// 该订单的议价记录集合 - 一对多关系
        /// 一个订单可以有多个议价记录
        /// </summary>
        public virtual ICollection<Negotiation> Negotiations { get; set; } = new List<Negotiation>();

        #endregion

        #region 静态常量

        /// <summary>
        /// 订单状态的有效值 - 与Oracle检查约束保持一致
        /// </summary>
        public static class OrderStatus
        {
            /// <summary>
            /// 待付款 - 订单已创建，等待买家付款
            /// </summary>
            public const string PendingPayment = "待付款";

            /// <summary>
            /// 已付款 - 买家已完成付款，等待卖家发货
            /// </summary>
            public const string Paid = "已付款";

            /// <summary>
            /// 已发货 - 卖家已发货，商品在运输途中
            /// </summary>
            public const string Shipped = "已发货";

            /// <summary>
            /// 已送达 - 商品已送达买家，等待确认收货
            /// </summary>
            public const string Delivered = "已送达";

            /// <summary>
            /// 已完成 - 交易完成，买家已确认收货
            /// </summary>
            public const string Completed = "已完成";

            /// <summary>
            /// 已取消 - 订单被取消（超时未付款、主动取消等）
            /// </summary>
            public const string Cancelled = "已取消";
        }

        /// <summary>
        /// 订单状态流转图
        /// </summary>
        public static class OrderStatusFlow
        {
            /// <summary>
            /// 获取指定状态的下一个可能状态
            /// </summary>
            /// <param name="currentStatus">当前状态</param>
            /// <returns>可能的下一个状态列表</returns>
            public static List<string> GetNextPossibleStatuses(string currentStatus)
            {
                return currentStatus switch
                {
                    OrderStatus.PendingPayment => new List<string> { OrderStatus.Paid, OrderStatus.Cancelled },
                    OrderStatus.Paid => new List<string> { OrderStatus.Shipped, OrderStatus.Cancelled },
                    OrderStatus.Shipped => new List<string> { OrderStatus.Delivered },
                    OrderStatus.Delivered => new List<string> { OrderStatus.Completed },
                    OrderStatus.Completed => new List<string>(), // 最终状态
                    OrderStatus.Cancelled => new List<string>(), // 最终状态
                    _ => new List<string>()
                };
            }
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 检查订单状态是否有效
        /// </summary>
        /// <param name="status">订单状态</param>
        /// <returns>是否有效</returns>
        public static bool IsValidStatus(string status)
        {
            return status == OrderStatus.PendingPayment ||
                   status == OrderStatus.Paid ||
                   status == OrderStatus.Shipped ||
                   status == OrderStatus.Delivered ||
                   status == OrderStatus.Completed ||
                   status == OrderStatus.Cancelled;
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
        /// 判断订单是否已过期
        /// </summary>
        /// <returns>是否过期</returns>
        public bool IsExpired()
        {
            return ExpireTime.HasValue && ExpireTime.Value < DateTime.Now;
        }

        /// <summary>
        /// 判断订单是否可以取消
        /// </summary>
        /// <returns>是否可以取消</returns>
        public bool CanCancel()
        {
            return Status == OrderStatus.PendingPayment || Status == OrderStatus.Paid;
        }

        /// <summary>
        /// 判断订单是否可以付款
        /// </summary>
        /// <returns>是否可以付款</returns>
        public bool CanPay()
        {
            return Status == OrderStatus.PendingPayment && !IsExpired();
        }

        /// <summary>
        /// 判断订单是否可以发货
        /// </summary>
        /// <returns>是否可以发货</returns>
        public bool CanShip()
        {
            return Status == OrderStatus.Paid;
        }

        /// <summary>
        /// 判断订单是否可以确认收货
        /// </summary>
        /// <returns>是否可以确认收货</returns>
        public bool CanConfirmDelivery()
        {
            return Status == OrderStatus.Delivered;
        }

        /// <summary>
        /// 判断订单是否为最终状态（已完成或已取消）
        /// </summary>
        /// <returns>是否为最终状态</returns>
        public bool IsFinalStatus()
        {
            return Status == OrderStatus.Completed || Status == OrderStatus.Cancelled;
        }

        /// <summary>
        /// 更新订单状态
        /// </summary>
        /// <param name="newStatus">新状态</param>
        /// <exception cref="ArgumentException">状态无效或状态流转不合法时抛出异常</exception>
        public void UpdateStatus(string newStatus)
        {
            if (!IsValidStatus(newStatus))
                throw new ArgumentException($"无效的订单状态: {newStatus}");

            var nextPossibleStatuses = OrderStatusFlow.GetNextPossibleStatuses(Status);
            if (!nextPossibleStatuses.Contains(newStatus))
                throw new ArgumentException($"不能从状态 '{Status}' 转换到状态 '{newStatus}'");

            Status = newStatus;
        }

        /// <summary>
        /// 计算订单剩余过期时间
        /// </summary>
        /// <returns>剩余时间，如果已过期或无过期时间则返回null</returns>
        public TimeSpan? GetRemainingTime()
        {
            if (!ExpireTime.HasValue)
                return null;

            var remaining = ExpireTime.Value - DateTime.Now;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        /// <summary>
        /// 设置订单过期时间
        /// </summary>
        /// <param name="minutes">从当前时间开始的分钟数</param>
        public void SetExpireTime(int minutes)
        {
            if (minutes <= 0)
                throw new ArgumentException("过期时间必须大于0分钟");

            ExpireTime = DateTime.Now.AddMinutes(minutes);
        }

        /// <summary>
        /// 获取订单的显示价格（优先使用最终价格，否则使用总金额）
        /// </summary>
        /// <returns>显示价格</returns>
        public decimal? GetDisplayPrice()
        {
            return FinalPrice ?? TotalAmount;
        }

        /// <summary>
        /// 检查是否存在议价差异
        /// </summary>
        /// <returns>是否存在议价</returns>
        public bool HasPriceNegotiation()
        {
            return FinalPrice.HasValue && TotalAmount.HasValue && FinalPrice.Value != TotalAmount.Value;
        }

        #endregion

        /// <summary>
        /// 重写ToString方法，返回订单信息
        /// </summary>
        /// <returns>订单的基本信息</returns>
        public override string ToString()
        {
            return $"Order[{OrderId}] - Status: {Status}, Amount: {GetDisplayPrice():C}";
        }
    }
}
