using System.ComponentModel.DataAnnotations;

namespace CampusTrade.API.Models.DTOs.Exchange
{
    /// <summary>
    /// 用户发起换物请求时的DTO
    /// </summary>
    public class ExchangeRequestDto
    {
        /// <summary>
        /// 提供商品ID（即用户要交换的商品）
        /// </summary>
        [Required(ErrorMessage = "提供商品ID不能为空")]
        public int OfferProductId { get; set; }

        /// <summary>
        /// 请求商品ID（即用户希望获得的商品）
        /// </summary>
        [Required(ErrorMessage = "请求商品ID不能为空")]
        public int RequestProductId { get; set; }

        /// <summary>
        /// 交换条件说明（如：商品状态要求等）
        /// </summary>
        [Required(ErrorMessage = "交换条件不能为空")]
        [StringLength(500, ErrorMessage = "交换条件不能超过500个字符")]
        public string Terms { get; set; } = string.Empty;
    }
}
