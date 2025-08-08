using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusTrade.API.Services.Interfaces;
using CampusTrade.API.Models.DTOs.Category;
using CampusTrade.API.Models.Common;

namespace CampusTrade.API.Controllers;

/// <summary>
/// 分类管理 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// 获取完整分类树
    /// </summary>
    [HttpGet("tree")]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategoryTree()
    {
        var result = await _categoryService.GetCategoryTreeAsync();
        return Ok(ApiResponse<List<CategoryDto>>.Success(result));
    }

    /// <summary>
    /// 创建新分类
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        var success = await _categoryService.CreateCategoryAsync(dto);
        return Ok(ApiResponse<bool>.Success(success));
    }

    /// <summary>
    /// 更新分类信息
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateCategory([FromBody] UpdateCategoryDto dto)
    {
        var success = await _categoryService.UpdateCategoryAsync(dto);
        return Ok(ApiResponse<bool>.Success(success));
    }

    /// <summary>
    /// 删除分类
    /// </summary>
    [HttpDelete("{categoryId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCategory(int categoryId)
    {
        var success = await _categoryService.DeleteCategoryAsync(categoryId);
        return Ok(ApiResponse<bool>.Success(success));
    }
}
