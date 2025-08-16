using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 评价管理Repository接口
    /// 提供订单评价、统计分析等功能
    /// </summary>
    public interface IReviewsRepository : IRepository<Review>
    {
        #region 创建操作
        // 评价创建由基础仓储 AddAsync 提供
        #endregion

        #region 读取操作
        /// <summary>
        /// 根据订单ID获取评价
        /// </summary>
        Task<Review?> GetByOrderIdAsync(int orderId);
        /// <summary>
        /// 根据用户ID获取评价集合
        /// </summary>
        Task<IEnumerable<Review>> GetByUserIdAsync(int userId);
        /// <summary>
        /// 分页获取评价
        /// </summary>
        Task<(IEnumerable<Review> Reviews, int TotalCount)> GetPagedReviewsAsync(int pageIndex, int pageSize, int? userId = null, int? productId = null);
        /// <summary>
        /// 获取指定商品的评价集合
        /// </summary>
        Task<IEnumerable<Review>> GetReviewsByProductIdAsync(int productId);
        /// <summary>
        /// 获取指定商品的平均评分
        /// </summary>
        Task<decimal> GetProductAverageRatingAsync(int productId);
        /// <summary>
        /// 获取指定商品的详细评分
        /// </summary>
        Task<Dictionary<string, decimal>> GetProductDetailedRatingsAsync(int productId);
        /// <summary>
        /// 获取匿名评价集合
        /// </summary>
        Task<IEnumerable<Review>> GetAnonymousReviewsAsync();
        /// <summary>
        /// 获取匿名评价数量
        /// </summary>
        Task<int> GetAnonymousReviewCountAsync();
        /// <summary>
        /// 获取有卖家回复的评价
        /// </summary>
        Task<IEnumerable<Review>> GetReviewsWithRepliesAsync();
        /// <summary>
        /// 获取指定卖家未回复的评价
        /// </summary>
        Task<IEnumerable<Review>> GetReviewsWithoutRepliesAsync(int sellerId);
        #endregion

        #region 统计操作
        /// <summary>
        /// 获取高评分评价
        /// </summary>
        Task<IEnumerable<Review>> GetHighRatingReviewsAsync(decimal minRating = 4.0m);
        /// <summary>
        /// 获取低评分评价
        /// </summary>
        Task<IEnumerable<Review>> GetLowRatingReviewsAsync(decimal maxRating = 2.0m);
        /// <summary>
        /// 获取最近N天的评价
        /// </summary>
        Task<IEnumerable<Review>> GetRecentReviewsAsync(int days = 7);
        /// <summary>
        /// 获取用户的平均评分
        /// </summary>
        Task<decimal> GetAverageRatingByUserAsync(int userId);
        /// <summary>
        /// 获取商品的平均评分
        /// </summary>
        Task<decimal> GetAverageRatingByProductAsync(int productId);
        /// <summary>
        /// 获取用户的评分分布
        /// </summary>
        Task<Dictionary<int, int>> GetRatingDistributionByUserAsync(int userId);
        /// <summary>
        /// 获取用户评价总数
        /// </summary>
        Task<int> GetReviewCountByUserAsync(int userId);
        #endregion

        #region 更新操作
        /// <summary>
        /// 卖家回复评价
        /// </summary>
        Task<bool> AddSellerReplyAsync(int reviewId, string reply);
        #endregion

        #region 删除操作
        // 评价删除由基础仓储 Delete 提供
        #endregion
    }
}
