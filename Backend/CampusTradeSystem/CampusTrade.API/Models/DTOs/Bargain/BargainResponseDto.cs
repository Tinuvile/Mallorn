using System.ComponentModel.DataAnnotations;

namespace CampusTrade.API.Models.DTOs.Bargain
{
    /// <summary>
    /// 卖家回应议价请求时的DTO
    /// </summary>
    public class BargainResponseDto
    {
        /// <summary>
        /// 议价记录ID
        /// </summary>
        [Required(ErrorMessage = "议价记录ID不能为空")]
        public int NegotiationId { get; set; }

        /// <summary>
        /// 卖家的回应状态（接受、拒绝、反报价）
        /// </summary>
        [Required(ErrorMessage = "状态不能为空")]
        [StringLength(20, ErrorMessage = "状态长度不能超过20个字符")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 如果卖家反报价，需要提供新报价
        /// </summary>
        public decimal? ProposedPrice { get; set; }
    }
}
