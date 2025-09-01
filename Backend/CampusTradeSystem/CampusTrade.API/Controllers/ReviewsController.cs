using CampusTrade.API.Infrastructure.Extensions;
using CampusTrade.API.Models.DTOs.Review;
using CampusTrade.API.Services.Review;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusTrade.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        /// <summary>
        /// 创建评论（买家）
        /// </summary>
        [HttpPost]
        [Authorize] // 需要身份验证
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
        {
            try
            {
                int userId = User.GetUserId();
                bool result = await _reviewService.CreateReviewAsync(dto, userId);
                return result ? Ok("评论成功") : BadRequest("评论失败");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// 获取某商品的评论列表
        /// </summary>
        [HttpGet("item/{itemId}")]
        public async Task<IActionResult> GetReviewsByItemId(int itemId)
        {
            try
            {
                var reviews = await _reviewService.GetReviewsByItemIdAsync(itemId);
                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// 获取某订单的评论（仅买家可见）
        /// </summary>
        [HttpGet("order/{orderId}")]
        [Authorize]
        public async Task<IActionResult> GetReviewByOrderId(int orderId)
        {
            try
            {
                int userId = User.GetUserId();
                var review = await _reviewService.GetReviewByOrderIdAsync(orderId);
                return review == null ? NotFound("未找到评论") : Ok(review);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// 卖家回复评论（48小时内）
        /// </summary>
        [HttpPut("reply")]
        [Authorize]
        public async Task<IActionResult> ReplyToReview([FromBody] ReplyReviewDto dto)
        {
            try
            {
                int sellerId = User.GetUserId();
                bool result = await _reviewService.ReplyToReviewAsync(dto, sellerId);
                return result ? Ok("回复成功") : BadRequest("回复失败");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// 删除评论（仅作者本人）
        /// </summary>
        [HttpDelete("{reviewId}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            try
            {
                int userId = User.GetUserId();
                bool result = await _reviewService.DeleteReviewAsync(reviewId, userId);
                return result ? Ok("删除成功") : BadRequest("删除失败");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
