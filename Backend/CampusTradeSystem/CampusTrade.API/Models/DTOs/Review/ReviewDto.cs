namespace CampusTrade.API.Models.DTOs.Review
{
    /// <summary>
    /// 返回给前端的评论信息模型
    /// </summary>
    public class ReviewDto
    {
        public int ReviewId { get; set; }
        public int OrderId { get; set; }
        public decimal Rating { get; set; }
        public int DescAccuracy { get; set; }
        public int ServiceAttitude { get; set; }
        public bool IsAnonymous { get; set; }
        public string? Content { get; set; }
        public string? SellerReply { get; set; }
        public DateTime CreateTime { get; set; }

        // 可选字段：显示用户名或"匿名用户"
        public string ReviewerDisplayName { get; set; } = "匿名用户";
    }
}
