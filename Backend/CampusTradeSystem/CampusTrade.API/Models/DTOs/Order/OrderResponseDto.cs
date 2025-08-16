namespace CampusTrade.API.Models.DTOs.Order
{
    /// <summary>
    /// 订单详情响应DTO
    /// </summary>
    public class OrderDetailResponse
    {
        /// <summary>
        /// 订单ID
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// 买家ID
        /// </summary>
        public int BuyerId { get; set; }

        /// <summary>
        /// 买家信息
        /// </summary>
        public UserBriefInfo? Buyer { get; set; }

        /// <summary>
        /// 卖家ID
        /// </summary>
        public int SellerId { get; set; }

        /// <summary>
        /// 卖家信息
        /// </summary>
        public UserBriefInfo? Seller { get; set; }

        /// <summary>
        /// 商品ID
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// 商品信息
        /// </summary>
        public ProductBriefInfo? Product { get; set; }

        /// <summary>
        /// 订单总金额
        /// </summary>
        public decimal? TotalAmount { get; set; }

        /// <summary>
        /// 最终成交价格
        /// </summary>
        public decimal? FinalPrice { get; set; }

        /// <summary>
        /// 订单状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 订单创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime? ExpireTime { get; set; }

        /// <summary>
        /// 是否已过期
        /// </summary>
        public bool IsExpired => ExpireTime.HasValue && ExpireTime.Value < DateTime.Now;

        /// <summary>
        /// 剩余支付时间（分钟）
        /// </summary>
        public int? RemainingMinutes
        {
            get
            {
                if (!ExpireTime.HasValue || Status != "待付款") return null;
                var remaining = ExpireTime.Value - DateTime.Now;
                return remaining.TotalMinutes > 0 ? (int)remaining.TotalMinutes : 0;
            }
        }
    }

    /// <summary>
    /// 订单列表项响应DTO
    /// </summary>
    public class OrderListResponse
    {
        /// <summary>
        /// 订单ID
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// 商品信息
        /// </summary>
        public ProductBriefInfo? Product { get; set; }

        /// <summary>
        /// 对方用户信息（买家视角显示卖家，卖家视角显示买家）
        /// </summary>
        public UserBriefInfo? OtherUser { get; set; }

        /// <summary>
        /// 订单总金额
        /// </summary>
        public decimal? TotalAmount { get; set; }

        /// <summary>
        /// 最终成交价格
        /// </summary>
        public decimal? FinalPrice { get; set; }

        /// <summary>
        /// 订单状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 订单创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 用户角色（buyer/seller）
        /// </summary>
        public string UserRole { get; set; } = string.Empty;

        /// <summary>
        /// 是否已过期
        /// </summary>
        public bool IsExpired { get; set; }
    }

    /// <summary>
    /// 订单统计响应DTO
    /// </summary>
    public class OrderStatisticsResponse
    {
        /// <summary>
        /// 总订单数
        /// </summary>
        public int TotalOrders { get; set; }

        /// <summary>
        /// 待付款订单数
        /// </summary>
        public int PendingPaymentOrders { get; set; }

        /// <summary>
        /// 已付款订单数
        /// </summary>
        public int PaidOrders { get; set; }

        /// <summary>
        /// 已发货订单数
        /// </summary>
        public int ShippedOrders { get; set; }

        /// <summary>
        /// 已完成订单数
        /// </summary>
        public int CompletedOrders { get; set; }

        /// <summary>
        /// 已取消订单数
        /// </summary>
        public int CancelledOrders { get; set; }

        /// <summary>
        /// 总交易金额
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 本月订单数
        /// </summary>
        public int MonthlyOrders { get; set; }

        /// <summary>
        /// 本月交易金额
        /// </summary>
        public decimal MonthlyAmount { get; set; }
    }
}
