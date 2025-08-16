using System.ComponentModel.DataAnnotations;

namespace CampusTrade.API.Models.DTOs.Bargain
{
    /// <summary>
    /// 买家发起议价请求时的DTO
    /// </summary>
    public class BargainRequestDto
    {
        /// <summary>
        /// 订单ID
        /// </summary>
        [Required(ErrorMessage = "订单ID不能为空")]
        public int OrderId { get; set; }

        /// <summary>
        /// 提议的议价价格
        /// </summary>
        [Required(ErrorMessage = "议价价格不能为空")]
        [Range(0.01, double.MaxValue, ErrorMessage = "议价价格必须大于零")]
        public decimal ProposedPrice { get; set; }
    }
}
