namespace CampusTrade.API.Models.DTOs.Order
{
    /// <summary>
    /// 用户简要信息DTO
    /// </summary>
    public class UserBriefInfo
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 昵称
        /// </summary>
        public string? Nickname { get; set; }

        /// <summary>
        /// 头像URL
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// 信用分数
        /// </summary>
        public decimal? CreditScore { get; set; }
    }

    /// <summary>
    /// 商品简要信息DTO
    /// </summary>
    public class ProductBriefInfo
    {
        /// <summary>
        /// 商品ID
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// 商品标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 商品价格
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 商品主图URL
        /// </summary>
        public string? MainImageUrl { get; set; }

        /// <summary>
        /// 商品状态
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}
