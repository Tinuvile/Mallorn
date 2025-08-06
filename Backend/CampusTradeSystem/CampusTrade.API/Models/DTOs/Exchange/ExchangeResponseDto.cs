using System.ComponentModel.DataAnnotations;

namespace CampusTrade.API.Models.DTOs.Exchange
{
    /// <summary>
    /// 用户回应换物请求时的DTO
    /// </summary>
    public class ExchangeResponseDto
    {
        /// <summary>
        /// 换物请求ID
        /// </summary>
        [Required(ErrorMessage = "换物请求ID不能为空")]
        public int ExchangeRequestId { get; set; }

        /// <summary>
        /// 回应状态（同意、拒绝、协商）
        /// </summary>
        [Required(ErrorMessage = "状态不能为空")]
        [StringLength(20, ErrorMessage = "状态长度不能超过20个字符")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 回应说明
        /// </summary>
        [StringLength(300, ErrorMessage = "回应说明不能超过300个字符")]
        public string? ResponseMessage { get; set; }
    }

    /// <summary>
    /// 换物请求信息DTO
    /// </summary>
    public class ExchangeRequestInfoDto
    {
        /// <summary>
        /// 换物请求ID
        /// </summary>
        public int ExchangeRequestId { get; set; }

        /// <summary>
        /// 提供商品ID
        /// </summary>
        public int OfferProductId { get; set; }

        /// <summary>
        /// 请求商品ID
        /// </summary>
        public int RequestProductId { get; set; }

        /// <summary>
        /// 交换条件
        /// </summary>
        public string Terms { get; set; } = string.Empty;

        /// <summary>
        /// 请求状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
