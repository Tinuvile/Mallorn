using System.ComponentModel.DataAnnotations;

namespace CampusTrade.API.Models.DTOs
{
    /// <summary>
    /// 创建订单请求DTO
    /// </summary>
    public class CreateOrderRequest
    {
        /// <summary>
        /// 商品ID
        /// </summary>
        [Required(ErrorMessage = "商品ID不能为空")]
        public int ProductId { get; set; }

        /// <summary>
        /// 最终成交价格（用于议价后的订单）
        /// </summary>
        public decimal? FinalPrice { get; set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        [StringLength(500, ErrorMessage = "备注信息不能超过500个字符")]
        public string? Remarks { get; set; }
    }

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
    /// 更新订单状态请求DTO
    /// </summary>
    public class UpdateOrderStatusRequest
    {
        /// <summary>
        /// 新状态
        /// </summary>
        [Required(ErrorMessage = "状态不能为空")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 状态变更原因或备注
        /// </summary>
        [StringLength(500, ErrorMessage = "备注信息不能超过500个字符")]
        public string? Remarks { get; set; }
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

    /// <summary>
    /// 用户简要信息DTO
    /// </summary>
    public class UserBriefInfo
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 昵称
        /// </summary>
        public string? Nickname { get; set; }

        /// <summary>
        /// 头像URL
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// 信用分数
        /// </summary>
        public decimal? CreditScore { get; set; }
    }

    /// <summary>
    /// 商品简要信息DTO
    /// </summary>
    public class ProductBriefInfo
    {
        /// <summary>
        /// 商品ID
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// 商品标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 商品价格
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 商品主图URL
        /// </summary>
        public string? MainImageUrl { get; set; }

        /// <summary>
        /// 商品状态
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}