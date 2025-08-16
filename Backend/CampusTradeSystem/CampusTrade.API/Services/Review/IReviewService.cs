using CampusTrade.API.Models.DTOs.Review;
using CampusTrade.API.Models.Entities;
namespace CampusTrade.API.Services.Review
{
    public interface IReviewService
    {
        /// <summary>
        /// 创建新的评论
        /// </summary>
        /// <param name="dto">评论数据</param>
        /// <param name="userId">当前登录用户ID（用于身份校验）</param>
        Task<bool> CreateReviewAsync(CreateReviewDto dto, int userId);

        /// <summary>
        /// 获取某个商品的全部评论（用于商品详情页）
        /// </summary>
        /// <param name="itemId">商品ID</param>
        Task<List<ReviewDto>> GetReviewsByItemIdAsync(int itemId);

        /// <summary>
        /// 获取订单下的评论（用于买家查看自己评论）
        /// </summary>
        /// <param name="orderId">订单ID</param>
        Task<ReviewDto?> GetReviewByOrderIdAsync(int orderId);

        /// <summary>
        /// 卖家回复评论
        /// </summary>
        /// <param name="dto">回复内容</param>
        /// <param name="sellerId">当前登录卖家ID（验证权限）</param>
        Task<bool> ReplyToReviewAsync(ReplyReviewDto dto, int sellerId);

        /// <summary>
        /// 删除评论
        /// </summary>
        /// <param name="reviewId">评论ID</param>
        /// <param name="userId">当前登录用户ID（验证权限）</param>
        Task<bool> DeleteReviewAsync(int reviewId, int userId);
    }
}
