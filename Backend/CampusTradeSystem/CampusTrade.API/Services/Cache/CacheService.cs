using System;
using System.Collections;
using System.Collections.Generic; // Added for Dictionary
using System.Linq; // Added for Where
using System.Reflection;
using System.Threading; // Added for Interlocked
using System.Threading.Tasks;
using CampusTrade.API.Options;
using CampusTrade.API.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging; // Added for ILogger
using Microsoft.Extensions.Options;

namespace CampusTrade.API.Services.Cache
{
    /// <summary>
    /// 内存缓存服务实现
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly CacheOptions _options;
        private readonly ILogger<CacheService> _logger;

        // 缓存命中统计（线程安全）
        private long _totalRequests = 0;
        private long _hits = 0;

        public CacheService(
            IMemoryCache memoryCache,
            IOptions<CacheOptions> options,
            ILogger<CacheService> logger)
        {
            _memoryCache = memoryCache;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            var actualExpiration = expiration ?? _options.DefaultCacheDuration;

            Interlocked.Increment(ref _totalRequests);
            if (_memoryCache.TryGetValue(key, out T? cachedValue))
            {
                Interlocked.Increment(ref _hits);
                return cachedValue;
            }

            try
            {
                var result = await factory().ConfigureAwait(false);

                // 空结果使用独立配置时间
                var finalExpiration = result == null ? _options.NullResultCacheDuration : actualExpiration;
                await SetAsync(key, result, finalExpiration);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create cache value for key: {Key}", key);
                throw; // 重新抛出异常，让调用者处理
            }
        }

        public Task<T?> GetAsync<T>(string key)
        {
            Interlocked.Increment(ref _totalRequests);

            if (_memoryCache.TryGetValue(key, out T? value))
            {
                Interlocked.Increment(ref _hits);
                return Task.FromResult<T?>(value);
            }

            return Task.FromResult<T?>(default);
        }

        public Task<Dictionary<string, T>> GetAllAsync<T>(IEnumerable<string> keys)
        {
            var result = new Dictionary<string, T>();

            foreach (var key in keys)
            {
                if (_memoryCache.TryGetValue(key, out T? value) && value != null)
                {
                    result[key] = value;
                    Interlocked.Increment(ref _hits);
                }
                Interlocked.Increment(ref _totalRequests);
            }
            return Task.FromResult(result);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var options = new MemoryCacheEntryOptions
            {
                Size = 1,
                AbsoluteExpirationRelativeToNow = expiration ?? _options.DefaultCacheDuration
            };

            _memoryCache.Set(key, value, options);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _memoryCache.Remove(key);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key)
        {
            return Task.FromResult(_memoryCache.TryGetValue(key, out _));
        }

        public Task ClearAllAsync()
        {
            if (_memoryCache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0); // 压缩100% = 清空
            }
            return Task.CompletedTask;
        }

        public Task<double> GetHitRate()
        {
            return Task.FromResult(_totalRequests == 0 ? 0 : (double)_hits / _totalRequests);
        }

        public Task<Dictionary<string, DateTime?>> GetExpirationInfo(params string[] keys)
        {
            var result = new Dictionary<string, DateTime?>();

            try
            {
                if (_memoryCache is MemoryCache memoryCache)
                {
                    // 通过反射获取 MemoryCache 的私有条目集合
                    var entriesField = typeof(MemoryCache).GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (entriesField?.GetValue(memoryCache) is IDictionary cacheEntries)
                    {
                        foreach (var key in keys)
                        {
                            try
                            {
                                if (cacheEntries.Contains(key))
                                {
                                    // 获取缓存条目并提取过期时间
                                    var entry = cacheEntries[key];
                                    var expirationField = entry?.GetType().GetProperty("AbsoluteExpiration");
                                    var expirationValue = expirationField?.GetValue(entry);

                                    if (expirationValue is DateTimeOffset offset)
                                    {
                                        result[key] = offset.LocalDateTime;
                                    }
                                    else if (expirationValue is DateTime time)
                                    {
                                        result[key] = time;
                                    }
                                    else
                                    {
                                        result[key] = null; // 无过期时间（永不过期）
                                    }
                                }
                                else
                                {
                                    result[key] = null; // 键不存在
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to get expiration info for key: {Key}", key);
                                result[key] = null; // 出错时返回null
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to access MemoryCache entries via reflection");
                // 如果反射失败，为所有键返回null
                foreach (var key in keys)
                {
                    result[key] = null;
                }
            }

            return Task.FromResult(result);
        }

        // 在CacheService类中添加
        public Task RemoveByPrefixAsync(string prefix)
        {
            if (_memoryCache is MemoryCache memCache)
            {
                // 内存缓存前缀清理
                var keys = memCache.GetKeys<string>().Where(k => k.StartsWith(prefix));
                foreach (var key in keys)
                {
                    _memoryCache.Remove(key);
                }
            }
            return Task.CompletedTask;
        }

        public async Task RefreshAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            var value = await factory();
            await SetAsync(key, value, expiration);
        }
        // CacheService.cs 新增方法
        public Task RemoveAllAsync(IEnumerable<string> keys)
        {
            if (_memoryCache is MemoryCache memoryCache)
            {
                // 内存缓存批量删除
                foreach (var key in keys)
                {
                    memoryCache.Remove(key);
                }
            }
            return Task.CompletedTask;
            // 如果是分布式缓存实现，可以添加相应的处理
        }

        public Task<IEnumerable<string>> GetKeysByPrefixAsync(string prefix)
        {
            var keys = new List<string>();

            if (_memoryCache is MemoryCache memoryCache)
            {
                // 使用我们已有的 MemoryCacheExtensions
                keys = memoryCache.GetKeys<string>()
                .Where(k => k.StartsWith(prefix))
                .ToList();
            }

            return Task.FromResult<IEnumerable<string>>(keys);
        }
    }


    /// <summary>
    /// MemoryCache扩展方法（用于获取所有Key）
    /// </summary>
    internal static class MemoryCacheExtensions
    {
        private static readonly FieldInfo _entriesField =
            typeof(MemoryCache).GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance)!;

        public static IEnumerable<T> GetKeys<T>(this IMemoryCache memoryCache)
        {
            if (_entriesField?.GetValue(memoryCache) is not IDictionary cacheEntries)
                yield break;

            foreach (DictionaryEntry entry in cacheEntries)
            {
                yield return (T)entry.Key;
            }
        }
    }
}
