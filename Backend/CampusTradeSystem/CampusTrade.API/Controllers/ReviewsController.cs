using CampusTrade.API.Infrastructure.Extensions;
using CampusTrade.API.Models.DTOs.Review;
using CampusTrade.API.Models.DTOs.Common;
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
        public async Task<ActionResult<ApiResponse<object>>> CreateReview([FromBody] CreateReviewDto dto)
        {
            try
            {
                int userId = User.GetUserId();
                bool result = await _reviewService.CreateReviewAsync(dto, userId);

                if (result)
                {
                    return Ok(ApiResponse<object>.CreateSuccess(null, "评论成功"));
                }
                else
                {
                    return BadRequest(ApiResponse<object>.CreateError("评论失败"));
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<object>.CreateError(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.CreateError("评论失败，请稍后重试"));
            }
        }

        /// <summary>
        /// 获取某商品的评论列表
        /// </summary>
        [HttpGet("item/{itemId}")]
        public async Task<ActionResult<ApiResponse<object>>> GetReviewsByItemId(int itemId)
        {
            try
            {
                var reviews = await _reviewService.GetReviewsByItemIdAsync(itemId);
                return Ok(ApiResponse<object>.CreateSuccess(reviews, "获取评论列表成功"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.CreateError("获取评论列表失败，请稍后重试"));
            }
        }

        /// <summary>
        /// 获取某订单的评论（仅买家可见）
        /// </summary>
        [HttpGet("order/{orderId}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> GetReviewByOrderId(int orderId)
        {
            try
            {
                int userId = User.GetUserId();
                var review = await _reviewService.GetReviewByOrderIdAsync(orderId);

                if (review == null)
                {
                    return NotFound(ApiResponse<object>.CreateError("未找到评论"));
                }

                return Ok(ApiResponse<object>.CreateSuccess(review, "获取评论成功"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.CreateError("获取评论失败，请稍后重试"));
            }
        }

        /// <summary>
        /// 卖家回复评论（48小时内）
        /// </summary>
        [HttpPost("response")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> ReplyToReview([FromBody] ReplyReviewDto dto)
        {
            try
            {
                int sellerId = User.GetUserId();
                bool result = await _reviewService.ReplyToReviewAsync(dto, sellerId);

                if (result)
                {
                    return Ok(ApiResponse<object>.CreateSuccess(null, "回复成功"));
                }
                else
                {
                    return BadRequest(ApiResponse<object>.CreateError("回复失败"));
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<object>.CreateError(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.CreateError("回复失败，请稍后重试"));
            }
        }

        /// <summary>
        /// 删除评论（仅作者本人）
        /// </summary>
        [HttpDelete("{reviewId}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> DeleteReview(int reviewId)
        {
            try
            {
                int userId = User.GetUserId();
                bool result = await _reviewService.DeleteReviewAsync(reviewId, userId);

                if (result)
                {
                    return Ok(ApiResponse<object>.CreateSuccess(null, "删除成功"));
                }
                else
                {
                    return BadRequest(ApiResponse<object>.CreateError("删除失败"));
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<object>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.CreateError("删除失败，请稍后重试"));
            }
        }
    }
}
