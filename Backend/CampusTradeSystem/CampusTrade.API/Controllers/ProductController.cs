using System.Security.Claims;
using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Models.DTOs.Product;
using CampusTrade.API.Services.File;
using CampusTrade.API.Services.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusTrade.API.Controllers;

/// <summary>
/// 商品管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IFileService _fileService;
    private readonly ILogger<ProductController> _logger;

    public ProductController(
        IProductService productService,
        IFileService fileService,
        ILogger<ProductController> logger)
    {
        _productService = productService;
        _fileService = fileService;
        _logger = logger;
    }

    #region 商品发布与管理

    /// <summary>
    /// 发布商品
    /// </summary>
    /// <param name="createDto">创建商品请求</param>
    /// <returns>商品详情</returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto createDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse.CreateError("用户未登录"));
            }

            var result = await _productService.CreateProductAsync(createDto, userId.Value);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布商品失败");
            return StatusCode(500, ApiResponse.CreateError("发布商品失败，请稍后重试"));
        }
    }

    /// <summary>
    /// 更新商品信息
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="updateDto">更新商品请求</param>
    /// <returns>更新后的商品详情</returns>
    [HttpPut("{productId}")]
    [Authorize]
    public async Task<IActionResult> UpdateProduct(int productId, [FromBody] UpdateProductDto updateDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse.CreateError("用户未登录"));
            }

            var result = await _productService.UpdateProductAsync(productId, updateDto, userId.Value);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新商品失败，ProductId: {ProductId}", productId);
            return StatusCode(500, ApiResponse.CreateError("更新商品失败，请稍后重试"));
        }
    }

    /// <summary>
    /// 删除商品（下架）
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{productId}")]
    [Authorize]
    public async Task<IActionResult> DeleteProduct(int productId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse.CreateError("用户未登录"));
            }

            var result = await _productService.DeleteProductAsync(productId, userId.Value);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除商品失败，ProductId: {ProductId}", productId);
            return StatusCode(500, ApiResponse.CreateError("删除商品失败，请稍后重试"));
        }
    }

    /// <summary>
    /// 获取商品详情
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <returns>商品详情</returns>
    [HttpGet("{productId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductDetail(int productId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _productService.GetProductDetailAsync(productId, userId, true);
            return result.Success ? Ok(result) : NotFound(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取商品详情失败，ProductId: {ProductId}", productId);
            return StatusCode(500, ApiResponse.CreateError("获取商品详情失败，请稍后重试"));
        }
    }

    /// <summary>
    /// 修改商品状态
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="status">新状态</param>
    /// <returns>操作结果</returns>
    [HttpPatch("{productId}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateProductStatus(int productId, [FromBody] string status)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse.CreateError("用户未登录"));
            }

            var result = await _productService.UpdateProductStatusAsync(productId, status, userId.Value);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新商品状态失败，ProductId: {ProductId}, Status: {Status}", productId, status);
            return StatusCode(500, ApiResponse.CreateError("更新商品状态失败，请稍后重试"));
        }
    }

    #endregion

    #region 商品查询与搜索

    /// <summary>
    /// 分页查询商品列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>分页商品列表</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetProducts([FromQuery] ProductQueryDto queryDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _productService.GetProductsAsync(queryDto, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询商品列表失败");
            return StatusCode(500, ApiResponse.CreateError("查询商品列表失败，请稍后重试"));
        }
    }

    /// <summary>
    /// 搜索商品
    /// </summary>
    /// <param name="keyword">搜索关键词</param>
    /// <param name="pageIndex">页索引</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="categoryId">分类ID</param>
    /// <returns>搜索结果</returns>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] string keyword,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? categoryId = null)
    {
        try
        {
            var result = await _productService.SearchProductsAsync(keyword, pageIndex, pageSize, categoryId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索商品失败，Keyword: {Keyword}", keyword);
            return StatusCode(500, ApiResponse.CreateError("搜索商品失败，请稍后重试"));
        }
    }

    /// <summary>
    /// 获取热门商品
    /// </summary>
    /// <param name="count">获取数量</param>
    /// <param name="categoryId">分类ID</param>
    /// <returns>热门商品列表</returns>
    [HttpGet("popular")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPopularProducts(
        [FromQuery] int count = 10,
        [FromQuery] int? categoryId = null)
    {
        try
        {
            var result = await _productService.GetPopularProductsAsync(count, categoryId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取热门商品失败");
            return StatusCode(500, ApiResponse.CreateError("获取热门商品失败，请稍后重试"));
        }
    }

    /// <summary>
    /// 获取用户发布的商品
    /// </summary>
    /// <param name="userId">用户ID（可选，不传则获取当前用户的商品）</param>
    /// <param name="pageIndex">页索引</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="status">商品状态</param>
    /// <returns>用户商品列表</returns>
    [HttpGet("user/{userId?}")]
    [Authorize]
    public async Task<IActionResult> GetUserProducts(
        int? userId = null,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized(ApiResponse.CreateError("用户未登录"));
            }

            // 如果没有传入userId，则使用当前登录用户的ID
            var targetUserId = userId ?? currentUserId.Value;

            var result = await _productService.GetUserProductsAsync(targetUserId, pageIndex, pageSize, status);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户商品失败，UserId: {UserId}", userId);
            return StatusCode(500, ApiResponse.CreateError("获取用户商品失败，请稍后重试"));
        }
    }

    #endregion

    #region 分类管理

    /// <summary>
    /// 获取分类树
    /// </summary>
    /// <param name="includeProductCount">是否包含商品数量</param>
    /// <returns>分类树</returns>
    [HttpGet("categories/tree")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryTree([FromQuery] bool includeProductCount = true)
    {
        try
        {
            var result = await _productService.GetCategoryTreeAsync(includeProductCount);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分类树失败");
            return StatusCode(500, ApiResponse.CreateError("获取分类树失败，请稍后重试"));
        }
    }

    /// <summary>
    /// 获取子分类列表
    /// </summary>
    /// <param name="parentId">父分类ID（null表示获取一级分类）</param>
    /// <returns>子分类列表</returns>
    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSubCategories([FromQuery] int? parentId = null)
    {
        try
        {
            var result = await _productService.GetSubCategoriesAsync(parentId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取子分类失败，ParentId: {ParentId}", parentId);
            return StatusCode(500, ApiResponse.CreateError("获取子分类失败，请稍后重试"));
        }
    }

    /// <summary>
    /// 获取分类面包屑导航
    /// </summary>
    /// <param name="categoryId">分类ID</param>
    /// <returns>面包屑导航</returns>
    [HttpGet("categories/{categoryId}/breadcrumb")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryBreadcrumb(int categoryId)
    {
        try
        {
            var result = await _productService.GetCategoryBreadcrumbAsync(categoryId);
            return result.Success ? Ok(result) : NotFound(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分类面包屑失败，CategoryId: {CategoryId}", categoryId);
            return StatusCode(500, ApiResponse.CreateError("获取分类面包屑失败，请稍后重试"));
        }
    }

    /// <summary>
    /// 获取分类下的商品
    /// </summary>
    /// <param name="categoryId">分类ID</param>
    /// <param name="pageIndex">页索引</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="includeSubCategories">是否包含子分类</param>
    /// <returns>分类商品列表</returns>
    [HttpGet("categories/{categoryId}/products")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductsByCategory(
        int categoryId,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeSubCategories = true)
    {
        try
        {
            var result = await _productService.GetProductsByCategoryAsync(categoryId, pageIndex, pageSize, includeSubCategories);
            return result.Success ? Ok(result) : NotFound(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分类商品失败，CategoryId: {CategoryId}", categoryId);
            return StatusCode(500, ApiResponse.CreateError("获取分类商品失败，请稍后重试"));
        }
    }

    #endregion

    #region 图片管理

    // 注意：商品图片上传功能已在 FileController 中实现
    // 使用以下端点：
    // - 单个上传：POST /api/file/upload/product-image
    // - 批量上传：POST /api/file/upload/batch?fileType=ProductImage

    #endregion

    #region 智能下架管理

    /// <summary>
    /// 获取即将自动下架的商品
    /// </summary>
    /// <param name="days">距离下架天数</param>
    /// <returns>即将下架的商品列表</returns>
    [HttpGet("auto-remove")]
    [Authorize]
    public async Task<IActionResult> GetProductsToAutoRemove([FromQuery] int days = 7)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _productService.GetProductsToAutoRemoveAsync(days, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取即将下架商品失败");
            return StatusCode(500, ApiResponse.CreateError("获取即将下架商品失败，请稍后重试"));
        }
    }

    /// <summary>
    /// 延期商品下架时间
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="extendDays">延期天数</param>
    /// <returns>操作结果</returns>
    [HttpPatch("{productId}/extend")]
    [Authorize]
    public async Task<IActionResult> ExtendProductAutoRemoveTime(int productId, [FromBody] int extendDays)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse.CreateError("用户未登录"));
            }

            var result = await _productService.ExtendProductAutoRemoveTimeAsync(productId, extendDays, userId.Value);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "延期商品下架时间失败，ProductId: {ProductId}", productId);
            return StatusCode(500, ApiResponse.CreateError("延期商品下架时间失败，请稍后重试"));
        }
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 获取当前登录用户ID
    /// </summary>
    /// <returns>用户ID</returns>
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    #endregion
}
