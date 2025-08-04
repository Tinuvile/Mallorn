using CampusTrade.API.Data;
using CampusTrade.API.Models.DTOs.Review;
using CampusTrade.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using ReviewEntity = CampusTrade.API.Models.Entities.Review;

namespace CampusTrade.API.Services.Review
{
    public class ReviewService : IReviewService
    {
        private readonly CampusTradeDbContext _context;

        public ReviewService(CampusTradeDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 创建新评论（仅允许订单买家，订单必须已完成，且不可重复评论）
        /// </summary>
        /// <param name="dto">评论提交数据（评分、内容等）</param>
        /// <param name="userId">当前登录用户ID（买家）</param>
        /// <returns>是否评论成功</returns>
        public async Task<bool> CreateReviewAsync(CreateReviewDto dto, int userId)
        {
            // 第一步：查找订单
            // 确保订单存在，且评论关联的订单合法
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == dto.OrderId);

            if (order == null)
                throw new Exception("订单不存在");

            // 第二步：校验当前用户是否为订单买家
            if (order.BuyerId != userId)
                throw new UnauthorizedAccessException("只有订单买家才能评论");

            // 第三步：检查订单状态是否已完成（只允许“已完成”状态的订单评论）
            if (order.Status != "已完成")
                throw new InvalidOperationException("订单未完成，无法评论");

            // 第四步：检查是否已评论过（每个订单只能评论一次）
            bool alreadyReviewed = await _context.Reviews
                .AnyAsync(r => r.OrderId == dto.OrderId);

            if (alreadyReviewed)
                throw new InvalidOperationException("该订单已提交评论，无法重复评论");

            // 第五步：创建新的评论实体对象（设置各字段）
            var review = new ReviewEntity
            {
                OrderId = dto.OrderId,
                Rating = dto.Rating,
                DescAccuracy = dto.DescAccuracy,
                ServiceAttitude = dto.ServiceAttitude,
                IsAnonymous = dto.IsAnonymous ? 1 : 0, // bool 转换为 0/1 存储
                Content = dto.Content,
                CreateTime = DateTime.Now // 使用当前时间而不是依赖数据库默认值
            };

            // 第六步：添加评论到数据库上下文并保存
            _context.Reviews.Add(review);
            var saved = await _context.SaveChangesAsync();

            // 第七步：返回是否保存成功（保存记录数大于0）
            return saved > 0;
        }

        /// <summary>
        /// 获取某个商品的所有评论信息（用于商品详情页）
        /// </summary>
        /// <param name="itemId">商品ID</param>
        /// <returns>评论列表，包含评分、评论内容、卖家回复、评论人名称等</returns>
        public async Task<List<ReviewDto>> GetReviewsByItemIdAsync(int itemId)
        {
            // 第一步：查找该商品相关的订单ID列表
            var orderIds = await _context.Orders
                .Where(o => o.ProductId == itemId)
                .Select(o => o.OrderId)
                .ToListAsync();

            // 如果该商品没有任何订单，直接返回空列表
            if (!orderIds.Any())
                return new List<ReviewDto>();

            // 第二步：从评论表中获取这些订单的评论，联查买家信息用于展示昵称
            var reviews = await _context.Reviews
                .Where(r => orderIds.Contains(r.OrderId)) // 筛选出当前商品的相关评论
                .Join(_context.Orders,
                      review => review.OrderId,
                      order => order.OrderId,
                      (review, order) => new { review, order }) // 联结订单表以获取买家ID
                .Join(_context.Users,
                      combined => combined.order.BuyerId,
                      user => user.UserId,
                      (combined, user) => new ReviewDto
                      {
                          // 映射评论字段
                          ReviewId = combined.review.ReviewId,
                          OrderId = combined.review.OrderId,
                          Rating = combined.review.Rating ?? 0, // 可空字段加默认值
                          DescAccuracy = combined.review.DescAccuracy ?? 0,
                          ServiceAttitude = combined.review.ServiceAttitude ?? 0,
                          IsAnonymous = combined.review.IsAnonymous == 1,
                          Content = combined.review.Content,
                          SellerReply = combined.review.SellerReply,
                          CreateTime = combined.review.CreateTime,

                          // 第三步：根据是否匿名，决定显示用户昵称或“匿名用户”
                          ReviewerDisplayName = (combined.review.IsAnonymous == 1) ? "匿名用户" : user.Username
                      })
                .OrderByDescending(r => r.CreateTime) // 第四步：按时间倒序排列（最新在前）
                .ToListAsync();

            return reviews;
        }


        /// <summary>
        /// 获取指定订单的评论详情（用于买家查看自己的评论）
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>评论详情（无评论则返回 null）</returns>
        public async Task<ReviewDto?> GetReviewByOrderIdAsync(int orderId)
        {
            // 第一步：查找该订单对应的评论（最多一条）
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.OrderId == orderId);

            // 没有评论，直接返回 null
            if (review == null)
                return null;

            // 第二步：获取订单信息，拿到买家昵称（或匿名状态）
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                throw new Exception("订单不存在");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == order.BuyerId);

            var reviewerName = (review.IsAnonymous == 1 || user == null)
                ? "匿名用户"
                : user.Username;

            // 第三步：构造 ReviewDto 返回
            return new ReviewDto
            {
                ReviewId = review.ReviewId,
                OrderId = review.OrderId,
                Rating = review.Rating ?? 0,
                DescAccuracy = review.DescAccuracy ?? 0,
                ServiceAttitude = review.ServiceAttitude ?? 0,
                IsAnonymous = review.IsAnonymous == 1,
                Content = review.Content,
                SellerReply = review.SellerReply,
                CreateTime = review.CreateTime,
                ReviewerDisplayName = reviewerName
            };
        }


        /// <summary>
        /// 卖家回复评论（需在评论后48小时内，且为该订单的卖家）
        /// </summary>
        /// <param name="dto">回复内容及评论ID</param>
        /// <param name="sellerId">当前登录卖家ID</param>
        /// <returns>是否成功回复</returns>
        public async Task<bool> ReplyToReviewAsync(ReplyReviewDto dto, int sellerId)
        {
            // 第一步：获取评论
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == dto.ReviewId);

            if (review == null)
                throw new Exception("评论不存在");

            // 第二步：获取订单信息，检查是否为该评论对应订单的卖家
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == review.OrderId);

            if (order == null)
                throw new Exception("关联订单不存在");

            if (order.SellerId != sellerId)
                throw new UnauthorizedAccessException("无权限回复该评论");

            // 第三步：判断是否超出 48 小时回复期限
            var deadline = review.CreateTime.AddHours(48);
            if (DateTime.Now > deadline)
                throw new InvalidOperationException("回复已超出48小时限制，无法修改");

            // 第四步：写入回复内容
            review.SellerReply = dto.SellerReply;

            // 第五步：保存更改
            var updated = await _context.SaveChangesAsync();

            return updated > 0;
        }


        /// <summary>
        /// 买家删除自己的评论（只能删除自己写的，不能删除他人）
        /// </summary>
        /// <param name="reviewId">评论ID</param>
        /// <param name="userId">当前登录用户ID</param>
        /// <returns>是否删除成功</returns>
        public async Task<bool> DeleteReviewAsync(int reviewId, int userId)
        {
            // 第一步：获取评论
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

            if (review == null)
                throw new Exception("评论不存在");

            // 第二步：确认订单是否存在，并且属于当前用户（买家）
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == review.OrderId);

            if (order == null)
                throw new Exception("关联订单不存在");

            if (order.BuyerId != userId)
                throw new UnauthorizedAccessException("只能删除自己写的评论");

            // 第三步：删除评论记录（物理删除）
            _context.Reviews.Remove(review);
            var deleted = await _context.SaveChangesAsync();

            return deleted > 0;
        }

    }
}
