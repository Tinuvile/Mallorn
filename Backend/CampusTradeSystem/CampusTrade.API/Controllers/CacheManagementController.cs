using CampusTrade.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace CampusTrade.API.Controllers;

/// <summary>
/// 缓存管理访问API
/// 包含缓存清理的多方位控制
/// </summary>
[ApiController]
[Route("api/[controller]")]
// [Authorize(Roles = "Admin")] // 临时注释，用于测试 - 生产环境请取消注释
public class CacheManagementController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly IUserCacheService _userCacheService;
    private readonly IProductCacheService _productCacheService;
    private readonly ICategoryCacheService _categoryCacheService;
    private readonly ISystemConfigCacheService _configCacheService;
    private readonly ILogger<CacheManagementController> _logger;

    public CacheManagementController(
        ICacheService cacheService,
        IUserCacheService userCacheService,
        IProductCacheService productCacheService,
        ICategoryCacheService categoryCacheService,
        ISystemConfigCacheService configCacheService,
        ILogger<CacheManagementController> logger)
    {
        _cacheService = cacheService;
        _userCacheService = userCacheService;
        _productCacheService = productCacheService;
        _categoryCacheService = categoryCacheService;
        _configCacheService = configCacheService;
        _logger = logger;
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetCacheStats()
    {
        var stats = new
        {
            UserCacheHitRate = await _userCacheService.GetHitRate(),
            ProductCacheHitRate = await _productCacheService.GetHitRate(),
            ConfigCacheHitRate = await _configCacheService.GetHitRate()
        };

        return Ok(stats);
    }

    /// <summary>
    /// 清除指定缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    [HttpDelete("item/{key}")]
    public async Task<IActionResult> RemoveCacheItem(string key)
    {
        await _cacheService.RemoveAsync(key);
        _logger.LogInformation("管理员 {User} 清除了缓存项: {Key}", User.Identity?.Name, key);
        return NoContent();
    }

    /// <summary>
    /// 清除用户缓存
    /// </summary>
    /// <param name="userId">用户ID</param>
    [HttpDelete("user/{userId}")]
    public async Task<IActionResult> RemoveUserCache(int userId)
    {
        await _userCacheService.RemoveAllUserDataAsync(userId);
        _logger.LogInformation("管理员 {User} 清除了用户 {UserId} 的缓存",
            User.Identity?.Name, userId);
        return NoContent();
    }

    /// <summary>
    /// 清除商品缓存
    /// </summary>
    /// <param name="productId">商品ID</param>
    [HttpDelete("product/{productId}")]
    public async Task<IActionResult> RemoveProductCache(int productId)
    {
        await _productCacheService.InvalidateProductCacheAsync(productId);
        _logger.LogInformation("管理员 {User} 清除了商品 {ProductId} 的缓存",
            User.Identity?.Name, productId);
        return NoContent();
    }

    /// <summary>
    /// 清除分类缓存
    /// </summary>
    [HttpDelete("category")]
    public async Task<IActionResult> RemoveCategoryCache()
    {
        await _categoryCacheService.InvalidateCategoryTreeCacheAsync();
        _logger.LogInformation("管理员 {User} 清除了分类树缓存", User.Identity?.Name);
        return NoContent();
    }

    /// <summary>
    /// 清除系统配置缓存
    /// </summary>
    [HttpDelete("config")]
    public async Task<IActionResult> RemoveConfigCache()
    {
        await _configCacheService.InvalidateAllConfigsAsync();
        _logger.LogInformation("管理员 {User} 清除了系统配置缓存", User.Identity?.Name);
        return NoContent();
    }

    /// <summary>
    /// 按前缀清除缓存
    /// </summary>
    /// <param name="prefix">缓存键前缀</param>
    [HttpDelete("prefix/{prefix}")]
    public async Task<IActionResult> RemoveByPrefix(string prefix)
    {
        await _cacheService.RemoveByPrefixAsync(prefix);
        _logger.LogInformation("管理员 {User} 清除了前缀为 {Prefix} 的缓存",
            User.Identity?.Name, prefix);
        return NoContent();
    }

    /// <summary>
    /// 清除所有缓存（谨慎使用）
    /// </summary>
    [HttpDelete("all")]
    public async Task<IActionResult> ClearAllCache()
    {
        // 按业务顺序清除缓存
        await _configCacheService.InvalidateAllConfigsAsync();
        await _categoryCacheService.InvalidateCategoryTreeCacheAsync();
        await _productCacheService.RefreshAllActiveProductsAsync();

        // 最后清除基础缓存
        await _cacheService.ClearAllAsync();

        _logger.LogWarning("管理员 {User} 执行了清除所有缓存的操作", User.Identity?.Name);
        return NoContent();
    }

    /// <summary>
    /// 手动刷新分类缓存
    /// </summary>
    [HttpPost("refresh/category")]
    public async Task<IActionResult> RefreshCategoryCache()
    {
        await _categoryCacheService.RefreshCategoryTreeAsync();
        return Ok(new { Message = "分类缓存已刷新" });
    }

    /// <summary>
    /// 手动刷新商品缓存
    /// </summary>
    [HttpPost("refresh/product")]
    public async Task<IActionResult> RefreshProductCache()
    {
        await _productCacheService.RefreshAllActiveProductsAsync();
        return Ok(new { Message = "活跃商品缓存已刷新" });
    }

    /// <summary>
    /// 手动刷新配置缓存
    /// </summary>
    [HttpPost("refresh/config")]
    public async Task<IActionResult> RefreshConfigCache()
    {
        await _configCacheService.RefreshCacheOptionsAsync();
        await _configCacheService.RefreshJwtOptionsAsync();
        return Ok(new { Message = "系统配置缓存已刷新" });
    }
}
