using CampusTrade.API.Options;
using Microsoft.Extensions.Options;

namespace CampusTrade.API.Services.Interfaces
{

    /// <summary>
    /// 系统配置缓存服务接口
    /// </summary>
    public interface ISystemConfigCacheService
    {
        /// <summary>
        /// 获取系统配置缓存
        /// </summary>
        /// <param name="configName">配置名称</param>
        /// <returns>配置值</returns>
        Task<string?> GetConfigAsync(string configName);

        /// <summary>
        /// 设置系统配置缓存
        /// </summary>
        /// <param name="configName">配置名称</param>
        /// <param name="configValue">配置值</param>
        /// <returns></returns>
        Task SetConfigAsync(string configName, string configValue);


        /// <summary>
        /// 移除系统配置缓存
        /// </summary>
        /// <param name="configName">配置名称</param>
        /// <returns></returns>
        Task RemoveConfigAsync(string configName);

        /// <summary>
        /// 获取JWT配置
        /// </summary>
        /// <returns>JWT配置对象</returns>
        Task<JwtOptions> GetJwtOptionsAsync();

        /// <summary>
        /// 获取缓存配置
        /// </summary>
        /// <returns>缓存配置对象</returns>
        Task<CacheOptions> GetCacheOptionsAsync();

        /// <summary>
        /// 获取应用设置JSON内容
        /// </summary>
        /// <returns>完整的appsettings.json内容</returns>
        Task<string> GetAppSettingsJsonAsync();

        /// <summary>
        /// 获取缓存命中率统计
        /// </summary>
        Task<double> GetHitRate();

        /// <summary>
        /// 失效全部系统配置缓存
        /// </summary>
        Task InvalidateAllConfigsAsync();

        /// <summary>
        /// 刷新JWT配置缓存
        /// </summary>
        Task RefreshJwtOptionsAsync();

        /// <summary>
        /// 刷新缓存配置
        /// </summary>
        Task RefreshCacheOptionsAsync();
    }
}
