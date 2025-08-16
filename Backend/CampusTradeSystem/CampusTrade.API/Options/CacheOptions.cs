using System;

namespace CampusTrade.API.Options
{
    /// <summary>
    /// 缓存系统全局配置
    /// </summary>
    public class CacheOptions
    {
        public const string SectionName = "CacheSettings";

        /// <summary>
        /// 内存缓存配置
        /// </summary>
        public MemoryCacheOptions MemoryCache { get; set; } = new();

        /// <summary>
        /// 默认缓存持续时间
        /// </summary>
        public TimeSpan DefaultCacheDuration { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// 商品相关缓存持续时间
        /// </summary>
        public TimeSpan ProductCacheDuration { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// 用户信息缓存持续时间
        /// </summary>
        public TimeSpan UserCacheDuration { get; set; } = TimeSpan.FromHours(2);

        /// <summary>
        /// 分类信息缓存持续时间
        /// </summary>
        public TimeSpan CategoryCacheDuration { get; set; } = TimeSpan.FromHours(6);

        /// <summary>
        /// 系统配置缓存持续时间
        /// </summary>
        public TimeSpan ConfigCacheDuration { get; set; } = TimeSpan.FromDays(1);

        /// <summary>
        /// 空结果缓存持续时间（防穿透）
        /// </summary>
        public TimeSpan NullResultCacheDuration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 定时刷新间隔时间
        /// </summary>
        public TimeSpan IntervalMinutes { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// 初始延迟时间
        /// </summary>
        public TimeSpan InitialDelaySeconds { get; set; } = TimeSpan.FromSeconds(30);

    }

    /// <summary>
    /// 内存缓存配置
    /// </summary>
    public class MemoryCacheOptions
    {
        /// <summary>
        /// 缓存大小限制（MB）
        /// </summary>
        public int SizeLimit { get; set; } = 1024; // 1GB

        /// <summary>
        /// 内存压缩比例（当达到SizeLimit时）
        /// </summary>
        public double CompactionPercentage { get; set; } = 0.5;

        /// <summary>
        /// 过期扫描频率
        /// </summary>
        public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 单个缓存项最大大小（KB）
        /// </summary>
        public int MaxItemSize { get; set; } = 1024; // 1MB
    }

    /// <summary>
    /// Redis缓存配置
    /// </summary>
    public class RedisOptions
    {
        /// <summary>
        /// 是否启用Redis
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; set; } = "localhost:6379";

        /// <summary>
        /// 实例名前缀
        /// </summary>
        public string InstanceName { get; set; } = "CampusTrade_";

        /// <summary>
        /// 连接超时时间（毫秒）
        /// </summary>
        public int ConnectTimeout { get; set; } = 5000;

        /// <summary>
        /// 同步操作超时时间（毫秒）
        /// </summary>
        public int SyncTimeout { get; set; } = 3000;

        /// <summary>
        /// 是否启用SSL
        /// </summary>
        public bool Ssl { get; set; } = false;

        /// <summary>
        /// 数据库编号
        /// </summary>
        public int Database { get; set; } = 0;
    }
}
