using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 抽象订单实体类 - 对应 Oracle 数据库中的 ABSTRACT_ORDERS 表
    /// 作为订单系统的基础表，支持普通订单和换物请求两种类型
    /// </summary>
    [Table("ABSTRACT_ORDERS")]
    public class AbstractOrder
    {
        /// <summary>
        /// 抽象订单ID - 主键，对应Oracle中的abstract_order_id字段
        /// 由ORDER_SEQ序列生成
        /// </summary>
        [Key]
        [Column("ABSTRACT_ORDER_ID")]
        public int AbstractOrderId { get; set; }

        /// <summary>
        /// 订单类型 - 对应Oracle中的order_type字段
        /// 限制值：normal（普通订单）、exchange（换物请求）
        /// </summary>
        [Required]
        [Column("ORDER_TYPE", TypeName = "VARCHAR2(20)")]
        [StringLength(20)]
        public string OrderType { get; set; } = OrderTypes.Normal;

        /// <summary>
        /// 对应的普通订单 - 一对一关系
        /// 当OrderType为normal时使用
        /// </summary>
        public virtual Order? Order { get; set; }

        /// <summary>
        /// 对应的换物请求 - 一对一关系  
        /// 当OrderType为exchange时使用
        /// </summary>
        public virtual ExchangeRequest? ExchangeRequest { get; set; }

        /// <summary>
        /// 该订单相关的通知集合 - 一对多关系
        /// 记录订单状态变更等相关通知
        /// </summary>
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        /// <summary>
        /// 该订单的评价集合 - 一对多关系
        /// 记录买家对该订单的评价信息
        /// </summary>
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

        /// <summary>
        /// 该订单的举报集合 - 一对多关系
        /// 记录针对该订单的所有举报信息
        /// </summary>
        public virtual ICollection<Reports> Reports { get; set; } = new List<Reports>();

        /// <summary>
        /// 订单类型的有效值 - 与Oracle检查约束保持一致
        /// </summary>
        public static class OrderTypes
        {
            /// <summary>
            /// 普通订单 - 用户购买商品的标准订单
            /// </summary>
            public const string Normal = "normal";

            /// <summary>
            /// 换物请求 - 用户之间商品交换的订单
            /// </summary>
            public const string Exchange = "exchange";
        }
    }
}
