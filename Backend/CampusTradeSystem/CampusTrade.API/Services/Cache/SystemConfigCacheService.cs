using System.Text.Json;
using CampusTrade.API.Infrastructure.Utils.Cache;
using CampusTrade.API.Options;
using CampusTrade.API.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace CampusTrade.API.Services.Cache;

/// <summary>
/// 系统配置缓存服务实现
/// </summary>
public class SystemConfigCacheService : ISystemConfigCacheService
{
    private readonly ICacheService _cache;
    private readonly IOptionsMonitor<JwtOptions> _jwtOptions;
    private readonly IOptionsMonitor<CacheOptions> _cacheOptions;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SystemConfigCacheService> _logger;

    public SystemConfigCacheService(
        ICacheService cache,
        IOptionsMonitor<JwtOptions> jwtOptions,
        IOptionsMonitor<CacheOptions> cacheOptions,
        ILogger<SystemConfigCacheService> logger,
        IConfiguration configuration)
    {
        _cache = cache;
        _jwtOptions = jwtOptions;
        _cacheOptions = cacheOptions;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> GetConfigAsync(string configName)
    {
        string cacheKey = CacheKeyHelper.ConfigKey(configName);
        return await _cache.GetAsync<string>(cacheKey);
    }

    public async Task SetConfigAsync(string configName, string configValue)
    {
        string cacheKey = CacheKeyHelper.ConfigKey(configName);
        await _cache.SetAsync(cacheKey, configValue, _cacheOptions.CurrentValue.ConfigCacheDuration);
    }

    public async Task RemoveConfigAsync(string configName)
    {
        string cacheKey = CacheKeyHelper.ConfigKey(configName);
        await _cache.RemoveAsync(cacheKey);
        _logger.LogInformation("配置项 {ConfigName} 缓存已失效", configName);
    }

    public async Task<JwtOptions> GetJwtOptionsAsync()
    {
        string cacheKey = CacheKeyHelper.ConfigKey("JwtOptions");
        var cachedValue = await _cache.GetAsync<JwtOptions>(cacheKey);

        if (cachedValue != null)
        {
            return cachedValue;
        }

        var jwtOptions = _jwtOptions.CurrentValue;
        await SetJwtOptionsAsync(jwtOptions);
        return jwtOptions;
    }

    private async Task SetJwtOptionsAsync(JwtOptions jwtOptions)
    {
        string cacheKey = CacheKeyHelper.ConfigKey("JwtOptions");
        await _cache.SetAsync(cacheKey, jwtOptions, _cacheOptions.CurrentValue.ConfigCacheDuration);
    }

    public async Task<CacheOptions> GetCacheOptionsAsync()
    {
        string cacheKey = CacheKeyHelper.ConfigKey("CacheOptions");
        var cachedValue = await _cache.GetAsync<CacheOptions>(cacheKey);

        if (cachedValue != null)
        {
            return cachedValue;
        }

        var cacheOptions = _cacheOptions.CurrentValue;
        await SetCacheOptionsAsync(cacheOptions);
        return cacheOptions;
    }

    private async Task SetCacheOptionsAsync(CacheOptions cacheOptions)
    {
        string cacheKey = CacheKeyHelper.ConfigKey("CacheOptions");
        await _cache.SetAsync(cacheKey, cacheOptions, cacheOptions.ConfigCacheDuration);
    }

    public async Task<string> GetAppSettingsJsonAsync()
    {
        string cacheKey = CacheKeyHelper.ConfigKey("AppSettingsJson");
        var cachedValue = await _cache.GetAsync<string>(cacheKey);

        if (cachedValue != null)
        {
            return cachedValue;
        }

        var appSettingsJson = JsonSerializer.Serialize(_configuration.Get<object>());
        await _cache.SetAsync(cacheKey, appSettingsJson, _cacheOptions.CurrentValue.ConfigCacheDuration);
        return appSettingsJson;
    }

    public async Task<double> GetHitRate()
    {
        return await _cache.GetHitRate();
    }
    public async Task InvalidateAllConfigsAsync()
    {
        // 获取所有配置项前缀的缓存键
        var configKeys = await _cache.GetKeysByPrefixAsync(CacheKeyHelper.GetConfigKeysPrefix());
        await _cache.RemoveAllAsync(configKeys);

        // 特殊处理JWT和CacheOptions
        await _cache.RemoveAsync(CacheKeyHelper.ConfigKey("JwtOptions"));
        await _cache.RemoveAsync(CacheKeyHelper.ConfigKey("CacheOptions"));

        _logger.LogInformation("所有系统配置缓存已失效");
    }

    public async Task RefreshJwtOptionsAsync()
    {
        // 立即重新加载并缓存
        var jwtOptions = _jwtOptions.CurrentValue;
        await SetJwtOptionsAsync(jwtOptions);
    }

    public async Task RefreshCacheOptionsAsync()
    {
        // 立即重新加载并缓存
        var cacheOptions = _cacheOptions.CurrentValue;
        await SetCacheOptionsAsync(cacheOptions);
    }
}
