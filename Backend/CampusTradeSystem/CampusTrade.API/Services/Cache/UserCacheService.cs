using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Services.Interface;
using CampusTrade.API.Services.Cache;
using CampusTrade.API.Options;
using CampusTrade.API.Utils.Cache;
using CampusTrade.API.Data;


namespace CampusTrade.API.Services.Cache
{
    public class UserCacheService : IUserCacheService
    {
        private readonly ICacheService _cache;
        private readonly CampusTradeDbContext _context;
        private readonly CacheOptions _options;
        private readonly ILogger<UserCacheService> _logger;
        private readonly SemaphoreSlim _userLock = new(1, 1);
        private readonly SemaphoreSlim _securityLock = new(1, 1);
        private readonly SemaphoreSlim _permissionLock = new(1, 1);

        // Memory cache for frequently accessed basic user info
        private static readonly ConcurrentDictionary<int, User> _basicUserCache = new();

        public UserCacheService(
            ICacheService cache,
            CampusTradeDbContext context,
            IOptions<CacheOptions> options,
            ILogger<UserCacheService> logger)
        {
            _cache = cache;
            _context = context;
            _options = options.Value;
            _logger = logger;
        }

        // 修改后的 GetUserAsync 方法
        public async Task<User?> GetUserAsync(int userId)
        {
            var key = CacheKeyHelper.UserKey(userId);

            try
            {
                // 1. 检查内存缓存
                if (_basicUserCache.TryGetValue(userId, out var cachedUser))
                    return cachedUser is NullUser ? null : cachedUser;

                // 2. 直接通过 DbContext 查询数据库
                return await _cache.GetOrCreateAsync(key, async () =>
                {
                    var user = await _context.Users.FindAsync(userId); // 直接查询
                    if (user != null)
                        _basicUserCache[userId] = user; // 更新内存缓存
                    return user ?? NullUser.Instance;
                }, _options.UserCacheDuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user cache for UserId: {UserId}", userId);
                return await _context.Users.FindAsync(userId); // 降级查询
            }
        }

        public async Task SetUserAsync(User user)
        {
            var key = CacheKeyHelper.UserKey(user.UserId);

            await _userLock.WaitAsync();
            try
            {
                // Update memory cache
                _basicUserCache[user.UserId] = user;

                // Update distributed cache
                await _cache.SetAsync(key, user, _options.UserCacheDuration);
            }
            finally
            {
                _userLock.Release();
            }
        }

        public async Task<User?> GetSecurityInfoAsync(int userId)
        {
            var key = CacheKeyHelper.UserSecurityKey(userId);

            await _securityLock.WaitAsync();
            try
            {
                return await _cache.GetOrCreateAsync(key, async () =>
                {
                    var user = await _context.Users
                       .Where(u => u.UserId == userId)
                       .Select(u => new User
                       {
                           UserId = u.UserId,
                           PasswordHash = u.PasswordHash,
                           LastLoginAt = u.LastLoginAt,
                           LastLoginIp = u.LastLoginIp,
                           IsLocked = u.IsLocked,
                           LockoutEnd = u.LockoutEnd,
                           FailedLoginAttempts = u.FailedLoginAttempts,
                           TwoFactorEnabled = u.TwoFactorEnabled,
                           SecurityStamp = u.SecurityStamp
                       })
                       .FirstOrDefaultAsync();

                    return user ?? NullUser.Instance;
                }, TimeSpan.FromMinutes(15)); // Shorter TTL for security info
            }
            finally
            {
                _securityLock.Release();
            }
        }

        public async Task<List<string>> GetPermissionsAsync(int userId)
        {
            // 从Admin表中获取用户的角色信息作为权限
            var admin = await _context.Admins
            .Where(a => a.UserId == userId)
            .Select(a => new
            {
                a.Role,
                a.AssignedCategory
            })
            .FirstOrDefaultAsync();

            if (admin == null)
            {
                return new List<string>(); // 如果不是管理员，返回空列表
            }

            var permissions = new List<string>();

            // 添加基本角色权限
            permissions.Add($"role:{admin.Role}");

            // 如果是分类管理员，添加特定的分类权限
            if (admin.Role == "category_admin" && admin.AssignedCategory.HasValue)
            {
                permissions.Add($"category:{admin.AssignedCategory.Value}");
            }

            return permissions;
        }

        public async Task RefreshPermissionsAsync(int userId)
        {
            var key = CacheKeyHelper.UserPermissionsKey(userId);
            await _cache.RemoveAsync(key);

            // Force reload permissions on next access
            await GetPermissionsAsync(userId);
        }
        
        public async Task RefreshSecurityAsync(int userId)
        {
            var key = CacheKeyHelper.UserSecurityKey(userId);
            await _cache.RemoveAsync(key);

            // Force reload permissions on next access
            await GetSecurityInfoAsync(userId);
        }
        public async Task RefreshUserAsync(int userId)
        {
            var key = CacheKeyHelper.UserKey(userId);
            await _cache.RemoveAsync(key);

            // Force reload permissions on next access
            await GetUserAsync(userId);
        }
        public async Task RemoveAllUserDataAsync(int userId)
        {
            var tasks = new List<Task>
            {
                _cache.RemoveAsync(CacheKeyHelper.UserKey(userId)),
                _cache.RemoveAsync(CacheKeyHelper.UserSecurityKey(userId)),
                _cache.RemoveAsync(CacheKeyHelper.UserPermissionsKey(userId))
            };

            // Remove from memory cache
            _basicUserCache.TryRemove(userId, out _);

            await Task.WhenAll(tasks);
            _logger.LogInformation("Cleared all cache data for UserId: {UserId}", userId);
        }

        public async Task<Dictionary<int, User>> GetUsersAsync(IEnumerable<int> userIds)
        {
            var result = new Dictionary<int, User>();
            var missingIds = new List<int>();

            // First check memory cache
            foreach (var userId in userIds)
            {
                if (_basicUserCache.TryGetValue(userId, out var user) && user is not NullUser)
                {
                    result[userId] = user;
                }
                else
                {
                    missingIds.Add(userId);
                }
            }

            if (missingIds.Count == 0)
            {
                return result;
            }

            // Then check distributed cache for remaining users
            var cacheKeys = missingIds.Select(CacheKeyHelper.UserKey).ToList();
            var cachedUsers = await _cache.GetAllAsync<User>(cacheKeys);

            foreach (var pair in cachedUsers)
            {
                var key = pair.Key;
                var user = pair.Value;

                if (user is not NullUser)
                {
                    var userId = int.Parse(key.Split(':').Last());
                    result[userId] = user;
                    _basicUserCache[userId] = user;
                    missingIds.Remove(userId);
                }
            }

            if (missingIds.Count == 0)
            {
                return result;
            }

            // Finally get remaining users from database
            var dbUsers = await _context.Users
               .Where(u => missingIds.Contains(u.UserId))
               .ToListAsync();

            foreach (var user in dbUsers)
            {
                result[user.UserId] = user;
                _basicUserCache[user.UserId] = user; // Populate memory cache
                await SetUserAsync(user); // Populate distributed cache
            }

            return result;
        }

        public async Task<User?> GetBasicInfoAsync(int userId)
        {
            // Basic info is the same as full user info in our case, but without security fields
            // We could optimize this further by storing a subset of fields
            return await GetUserAsync(userId);
        }

        public async Task<double> GetHitRate()
        {
            return await _cache.GetHitRate();
        }

        // Null object pattern for user
        private class NullUser : User
        {
            public static readonly NullUser Instance = new();
            private NullUser() { }
        }
        
        // UserCacheService.cs
        public async Task InvalidateUserCacheAsync(int userId)
        {
            await _userLock.WaitAsync();
            try
            {
                // 1. 移除内存缓存
                _basicUserCache.TryRemove(userId, out _);
        
                // 2. 移除分布式缓存中的各种用户数据
                var tasks = new List<Task>
                {
                    _cache.RemoveAsync(CacheKeyHelper.UserKey(userId)),
                    _cache.RemoveAsync(CacheKeyHelper.UserSecurityKey(userId)),
                    _cache.RemoveAsync(CacheKeyHelper.UserPermissionsKey(userId))
                };
        
                await Task.WhenAll(tasks);
        
                _logger.LogInformation("已失效用户 {UserId} 的所有缓存数据", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "失效用户 {UserId} 缓存时出错", userId);
                throw; // 根据业务需求决定是否抛出异常
            }
            finally
            {
                _userLock.Release();
            }
        }

        public async Task InvalidateUsersCacheAsync(IEnumerable<int> userIds)
        {
            // 批量失效，减少锁竞争
            var tasks = userIds.Select(async userId => 
            {
                await InvalidateUserCacheAsync(userId);
                _logger.LogInformation("已失效用户 {UserId} 的缓存", userId);
            });
            await Task.WhenAll(tasks);
        }


        public async Task InvalidateUserSecurityCacheAsync(int userId)
        {
            await _securityLock.WaitAsync();
            try
            {
                // 只移除安全信息相关缓存
                await _cache.RemoveAsync(CacheKeyHelper.UserSecurityKey(userId));
                _logger.LogInformation("已失效用户 {UserId} 的安全信息缓存", userId);
            }
            finally
            {
                _securityLock.Release();
            }
        }

        public async Task InvalidateUserPermissionsCacheAsync(int userId)
        {
            await _permissionLock.WaitAsync();
            try
            {
                // 只移除权限相关缓存
                await _cache.RemoveAsync(CacheKeyHelper.UserPermissionsKey(userId));
                _logger.LogInformation("已失效用户 {UserId} 的权限缓存", userId);
            }
            finally
            {
                _permissionLock.Release();
            }
        }
    }
}