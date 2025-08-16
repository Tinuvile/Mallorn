using System.ComponentModel.DataAnnotations;

namespace CampusTrade.API.Models.DTOs.Review
{
    /// <summary>
    /// 用户提交评论的数据模型
    /// </summary>
    public class CreateReviewDto
    {
        [Required]
        public int OrderId { get; set; }

        [Range(1.0, 5.0)]
        public decimal Rating { get; set; }

        [Range(1, 5)]
        public int DescAccuracy { get; set; }

        [Range(1, 5)]
        public int ServiceAttitude { get; set; }

        public bool IsAnonymous { get; set; } = false;

        [MaxLength(1000)]
        public string? Content { get; set; }
    }
}
