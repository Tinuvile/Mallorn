using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// 评价管理Repository实现
    /// 提供订单评价、统计分析等功能
    /// </summary>
    public class ReviewsRepository : Repository<Review>, IReviewsRepository
    {
        public ReviewsRepository(CampusTradeDbContext context) : base(context) { }

        #region 读取操作
        /// <summary>
        /// 根据订单ID获取评价
        /// </summary>
        public async Task<Review?> GetByOrderIdAsync(int orderId)
        {
            return await _context.Reviews.Include(r => r.AbstractOrder).ThenInclude(ao => ao.Order).ThenInclude(o => o.Product).FirstOrDefaultAsync(r => r.OrderId == orderId);
        }
        /// <summary>
        /// 根据用户ID获取评价集合
        /// </summary>
        public async Task<IEnumerable<Review>> GetByUserIdAsync(int userId)
        {
            return await _context.Reviews.Include(r => r.AbstractOrder).ThenInclude(ao => ao.Order).ThenInclude(o => o.Product).Where(r => r.AbstractOrder.Order != null && r.AbstractOrder.Order.BuyerId == userId).OrderByDescending(r => r.CreateTime).ToListAsync();
        }
        /// <summary>
        /// 分页获取评价
        /// </summary>
        public async Task<(IEnumerable<Review> Reviews, int TotalCount)> GetPagedReviewsAsync(int pageIndex, int pageSize, int? userId = null, int? productId = null)
        {
            var query = _context.Reviews.Include(r => r.AbstractOrder).ThenInclude(ao => ao.Order).ThenInclude(o => o.Product).AsQueryable();
            if (userId.HasValue) query = query.Where(r => r.AbstractOrder.Order != null && r.AbstractOrder.Order.BuyerId == userId.Value);
            if (productId.HasValue) query = query.Where(r => r.AbstractOrder.Order != null && r.AbstractOrder.Order.ProductId == productId.Value);
            query = query.OrderByDescending(r => r.CreateTime);
            var totalCount = await query.CountAsync();
            var reviews = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();
            return (reviews, totalCount);
        }
        /// <summary>
        /// 获取指定商品的评价集合
        /// </summary>
        public async Task<IEnumerable<Review>> GetReviewsByProductIdAsync(int productId)
        {
            return await _context.Reviews.Include(r => r.AbstractOrder).ThenInclude(ao => ao.Order).ThenInclude(o => o.Buyer).Where(r => r.AbstractOrder.Order != null && r.AbstractOrder.Order.ProductId == productId).OrderByDescending(r => r.CreateTime).ToListAsync();
        }
        /// <summary>
        /// 获取指定商品的平均评分
        /// </summary>
        public async Task<decimal> GetProductAverageRatingAsync(int productId)
        {
            return await GetAverageRatingByProductAsync(productId);
        }
        /// <summary>
        /// 获取指定商品的详细评分
        /// </summary>
        public async Task<Dictionary<string, decimal>> GetProductDetailedRatingsAsync(int productId)
        {
            var reviews = await _context.Reviews.Include(r => r.AbstractOrder).ThenInclude(ao => ao.Order).Where(r => r.AbstractOrder.Order != null && r.AbstractOrder.Order.ProductId == productId).ToListAsync();
            var result = new Dictionary<string, decimal>();
            if (reviews.Any())
            {
                var ratingsWithValue = reviews.Where(r => r.Rating.HasValue).ToList();
                var descAccuracyWithValue = reviews.Where(r => r.DescAccuracy.HasValue).ToList();
                var serviceAttitudeWithValue = reviews.Where(r => r.ServiceAttitude.HasValue).ToList();
                result["overall"] = ratingsWithValue.Any() ? (decimal)ratingsWithValue.Average(r => r.Rating.Value) : 0;
                result["desc_accuracy"] = descAccuracyWithValue.Any() ? (decimal)descAccuracyWithValue.Average(r => r.DescAccuracy.Value) : 0;
                result["service_attitude"] = serviceAttitudeWithValue.Any() ? (decimal)serviceAttitudeWithValue.Average(r => r.ServiceAttitude.Value) : 0;
            }
            return result;
        }
        /// <summary>
        /// 获取匿名评价集合
        /// </summary>
        public async Task<IEnumerable<Review>> GetAnonymousReviewsAsync()
        {
            return await _context.Reviews.Include(r => r.AbstractOrder).ThenInclude(ao => ao.Order).ThenInclude(o => o.Product).Where(r => r.IsAnonymous == 1).OrderByDescending(r => r.CreateTime).ToListAsync();
        }
        /// <summary>
        /// 获取匿名评价数量
        /// </summary>
        public async Task<int> GetAnonymousReviewCountAsync()
        {
            return await _context.Reviews.CountAsync(r => r.IsAnonymous == 1);
        }
        /// <summary>
        /// 获取有卖家回复的评价
        /// </summary>
        public async Task<IEnumerable<Review>> GetReviewsWithRepliesAsync()
        {
            return await _context.Reviews.Include(r => r.AbstractOrder).ThenInclude(ao => ao.Order).ThenInclude(o => o.Product).Where(r => !string.IsNullOrEmpty(r.SellerReply)).OrderByDescending(r => r.CreateTime).ToListAsync();
        }
        /// <summary>
        /// 获取指定卖家未回复的评价
        /// </summary>
        public async Task<IEnumerable<Review>> GetReviewsWithoutRepliesAsync(int sellerId)
        {
            return await _context.Reviews.Include(r => r.AbstractOrder).ThenInclude(ao => ao.Order).ThenInclude(o => o.Product).Where(r => r.AbstractOrder.Order != null && r.AbstractOrder.Order.SellerId == sellerId && string.IsNullOrEmpty(r.SellerReply)).OrderByDescending(r => r.CreateTime).ToListAsync();
        }
        #endregion

        #region 统计操作
        /// <summary>
        /// 获取高评分评价
        /// </summary>
        public async Task<IEnumerable<Review>> GetHighRatingReviewsAsync(decimal minRating = 4.0m)
        {
            return await _context.Reviews.Include(r => r.AbstractOrder).ThenInclude(ao => ao.Order).ThenInclude(o => o.Product).Where(r => r.Rating.HasValue && r.Rating.Value >= minRating).OrderByDescending(r => r.Rating).ToListAsync();
        }
        /// <summary>
        /// 获取低评分评价
        /// </summary>
        public async Task<IEnumerable<Review>> GetLowRatingReviewsAsync(decimal maxRating = 2.0m)
        {
            return await _context.Reviews.Include(r => r.AbstractOrder).ThenInclude(ao => ao.Order).ThenInclude(o => o.Product).Where(r => r.Rating.HasValue && r.Rating.Value <= maxRating).OrderBy(r => r.Rating).ToListAsync();
        }
        /// <summary>
        /// 获取最近N天的评价
        /// </summary>
        public async Task<IEnumerable<Review>> GetRecentReviewsAsync(int days = 7)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _context.Reviews.Include(r => r.AbstractOrder).ThenInclude(ao => ao.Order).ThenInclude(o => o.Product).Where(r => r.CreateTime >= cutoffDate).OrderByDescending(r => r.CreateTime).ToListAsync();
        }
        /// <summary>
        /// 获取用户的平均评分
        /// </summary>
        public async Task<decimal> GetAverageRatingByUserAsync(int userId)
        {
            var ratings = await _context.Reviews.Include(r => r.AbstractOrder).ThenInclude(ao => ao.Order).Where(r => r.AbstractOrder.Order != null && r.AbstractOrder.Order.SellerId == userId && r.Rating.HasValue).Select(r => r.Rating.Value).ToListAsync();
            return ratings.Any() ? (decimal)ratings.Average() : 0;
        }
        /// <summary>
        /// 获取商品的平均评分
        /// </summary>
        public async Task<decimal> GetAverageRatingByProductAsync(int productId)
        {
            var ratings = await _context.Reviews.Include(r => r.AbstractOrder).ThenInclude(ao => ao.Order).Where(r => r.AbstractOrder.Order != null && r.AbstractOrder.Order.ProductId == productId && r.Rating.HasValue).Select(r => r.Rating.Value).ToListAsync();
            return ratings.Any() ? (decimal)ratings.Average() : 0;
        }
        /// <summary>
        /// 获取用户的评分分布
        /// </summary>
        public async Task<Dictionary<int, int>> GetRatingDistributionByUserAsync(int userId)
        {
            return await _context.Reviews.Include(r => r.AbstractOrder).ThenInclude(ao => ao.Order).Where(r => r.AbstractOrder.Order != null && r.AbstractOrder.Order.SellerId == userId && r.Rating.HasValue).GroupBy(r => (int)r.Rating.Value).ToDictionaryAsync(g => g.Key, g => g.Count());
        }
        /// <summary>
        /// 获取用户评价总数
        /// </summary>
        public async Task<int> GetReviewCountByUserAsync(int userId)
        {
            return await _context.Reviews.Include(r => r.AbstractOrder).ThenInclude(ao => ao.Order).CountAsync(r => r.AbstractOrder.Order != null && r.AbstractOrder.Order.SellerId == userId);
        }
        #endregion

        #region 更新操作
        /// <summary>
        /// 卖家回复评价
        /// </summary>
        public async Task<bool> AddSellerReplyAsync(int reviewId, string reply)
        {
            try
            {
                var review = await GetByPrimaryKeyAsync(reviewId);
                if (review == null) return false;
                review.SellerReply = reply;
                Update(review);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}
