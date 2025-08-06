using System.ComponentModel.DataAnnotations;

namespace CampusTrade.API.Models.DTOs.Order
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
    /// 发货请求DTO
    /// </summary>
    public class ShipOrderRequest
    {
        /// <summary>
        /// 物流信息
        /// </summary>
        public string? TrackingInfo { get; set; }
    }

    /// <summary>
    /// 取消订单请求DTO
    /// </summary>
    public class CancelOrderRequest
    {
        /// <summary>
        /// 取消原因
        /// </summary>
        public string? Reason { get; set; }
    }
}
