using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CampusTrade.API.Models.DTOs
{
    /// <summary>
    /// 发起议价请求的数据传输对象
    /// </summary>
    public class BargainRequestDto
    {
        /// <summary>
        /// 买家ID
        /// </summary>
        [Required(ErrorMessage = "买家ID不能为空")]
        [JsonPropertyName("buyer_id")]
        public int BuyerId { get; set; }

        /// <summary>
        /// 卖家ID
        /// </summary>
        [Required(ErrorMessage = "卖家ID不能为空")]
        [JsonPropertyName("seller_id")]
        public int SellerId { get; set; }

        /// <summary>
        /// 买家提出的价格
        /// </summary>
        [Required(ErrorMessage = "报价不能为空")]
        [Range(0, double.MaxValue, ErrorMessage = "报价必须为正数")]
        [JsonPropertyName("offer_price")]
        public decimal OfferPrice { get; set; }
    }
}
