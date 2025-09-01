using System.ComponentModel.DataAnnotations;

namespace CampusTrade.API.Models.DTOs.Review
{
    /// <summary>
    /// 卖家回复评论的数据模型
    /// </summary>
    public class ReplyReviewDto
    {
        [Required]
        public int ReviewId { get; set; }

        [Required]
        [MaxLength(500)]
        public string SellerReply { get; set; } = null!;
    }
}
