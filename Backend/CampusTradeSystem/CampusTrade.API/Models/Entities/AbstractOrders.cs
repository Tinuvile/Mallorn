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
        [Column("ABSTRACT_ORDER_ID", TypeName = "NUMBER")]
        public int AbstractOrderId { get; set; }

        /// <summary>
        /// 订单类型 - 对应Oracle中的order_type字段
        /// 限制值：normal（普通订单）、exchange（换物请求）
        /// </summary>
        [Required]
        [Column("ORDER_TYPE", TypeName = "VARCHAR2(20)")]
        [StringLength(20)]
        public string OrderType { get; set; } = OrderTypes.Normal;

        #region 导航属性

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

        #endregion

        #region 静态常量

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

        #endregion

        #region 业务方法

        /// <summary>
        /// 检查订单类型是否有效
        /// </summary>
        /// <param name="orderType">订单类型</param>
        /// <returns>是否有效</returns>
        public static bool IsValidOrderType(string orderType)
        {
            return orderType == OrderTypes.Normal || orderType == OrderTypes.Exchange;
        }

        /// <summary>
        /// 验证当前实例的订单类型是否有效
        /// </summary>
        /// <returns>是否有效</returns>
        public bool IsValidOrderType()
        {
            return IsValidOrderType(OrderType);
        }

        /// <summary>
        /// 判断是否为普通订单
        /// </summary>
        /// <returns>是否为普通订单</returns>
        public bool IsNormalOrder()
        {
            return OrderType == OrderTypes.Normal;
        }

        /// <summary>
        /// 判断是否为换物请求
        /// </summary>
        /// <returns>是否为换物请求</returns>
        public bool IsExchangeRequest()
        {
            return OrderType == OrderTypes.Exchange;
        }

        /// <summary>
        /// 更新订单类型
        /// </summary>
        /// <param name="newOrderType">新的订单类型</param>
        /// <exception cref="ArgumentException">订单类型无效时抛出异常</exception>
        public void UpdateOrderType(string newOrderType)
        {
            if (!IsValidOrderType(newOrderType))
                throw new ArgumentException($"无效的订单类型: {newOrderType}");

            OrderType = newOrderType;
        }

        #endregion

        /// <summary>
        /// 重写ToString方法，返回订单信息
        /// </summary>
        /// <returns>订单的基本信息</returns>
        public override string ToString()
        {
            return $"AbstractOrder[{AbstractOrderId}] - Type: {OrderType}";
        }
    }
}
