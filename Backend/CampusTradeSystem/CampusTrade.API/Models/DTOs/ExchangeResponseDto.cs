using System.ComponentModel.DataAnnotations;

namespace CampusTrade.API.Models.DTOs.Exchange
{
    /// <summary>
    /// 处理换物请求回应时的DTO
    /// </summary>
    public class ExchangeResponseDto
    {
        /// <summary>
        /// 换物请求ID
        /// </summary>
        [Required(ErrorMessage = "换物请求ID不能为空")]
        public int ExchangeId { get; set; }

        /// <summary>
        /// 卖家的回应状态（接受、拒绝、反报价）
        /// </summary>
        [Required(ErrorMessage = "状态不能为空")]
        [StringLength(20, ErrorMessage = "状态长度不能超过20个字符")]
        public string Status { get; set; }
    }
}
