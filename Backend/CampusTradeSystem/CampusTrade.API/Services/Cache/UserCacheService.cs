using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CampusTrade.API.Data;
using CampusTrade.API.Infrastructure.Utils.Cache;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Options;
using CampusTrade.API.Services.Cache;
using CampusTrade.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


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

        /// <summary>
        /// 根据用户名或邮箱获取用户信息（带学生信息）
        /// </summary>
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            var key = CacheKeyHelper.UserByUsernameKey(username);

            try
            {
                return await _cache.GetOrCreateAsync(key, async () =>
                {
                    // 支持邮箱或用户名查找
                    var userByEmail = await _context.Users
                        .Include(u => u.Student)
                        .FirstOrDefaultAsync(u => u.Email == username && u.IsActive == 1);

                    if (userByEmail != null)
                    {
                        return userByEmail;
                    }

                    // 按用户名查找
                    var userByUsername = await _context.Users
                        .Include(u => u.Student)
                        .FirstOrDefaultAsync(u => u.Username == username && u.IsActive == 1);

                    return userByUsername ?? NullUser.Instance;
                }, _options.UserCacheDuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户名获取用户缓存失败: {Username}", username);
                // 降级查询
                var userByEmail = await _context.Users
                    .Include(u => u.Student)
                    .FirstOrDefaultAsync(u => u.Email == username && u.IsActive == 1);

                return userByEmail ?? await _context.Users
                    .Include(u => u.Student)
                    .FirstOrDefaultAsync(u => u.Username == username && u.IsActive == 1);
            }
        }

        /// <summary>
        /// 设置根据用户名/邮箱查询的用户缓存
        /// </summary>
        public async Task SetUserByUsernameAsync(string username, User? user)
        {
            var key = CacheKeyHelper.UserByUsernameKey(username);
            await _cache.SetAsync(key, user ?? NullUser.Instance, _options.UserCacheDuration);
            _logger.LogDebug("已设置用户名缓存: {Username}", username);
        }

        /// <summary>
        /// 验证学生身份（学号+姓名）
        /// </summary>
        public async Task<bool> ValidateStudentAsync(string studentId, string name)
        {
            var key = CacheKeyHelper.StudentValidationKey(studentId, name);
            try
            {
                return await _cache.GetOrCreateAsync(key, async () =>
                {
                    // 验证学生信息是否在预存的学生表中
                    var student = await _context.Students
                        .FirstOrDefaultAsync(s => s.StudentId == studentId && s.Name == name);
                    return student != null;
                }, _options.ConfigCacheDuration); // 学生验证结果使用较长的缓存时间
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "学生身份验证缓存失败: StudentId={StudentId}, Name={Name}", studentId, name);
                // 降级查询
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentId == studentId && s.Name == name);
                return student != null;
            }
        }

        /// <summary>
        /// 设置学生验证结果缓存
        /// </summary>
        public async Task SetStudentValidationAsync(string studentId, string name, bool isValid)
        {
            var key = CacheKeyHelper.StudentValidationKey(studentId, name);
            await _cache.SetAsync(key, isValid, _options.ConfigCacheDuration);
            _logger.LogDebug("已设置学生验证缓存: {StudentId}, {Name} -> {IsValid}", studentId, name, isValid);
        }

        /// <summary>
        /// 失效用户名/邮箱查询缓存
        /// </summary>
        public async Task InvalidateUsernameQueryCacheAsync(string username)
        {
            var key = CacheKeyHelper.UserByUsernameKey(username);
            await _cache.RemoveAsync(key);
            _logger.LogInformation("已失效用户名查询缓存: {Username}", username);
        }

        /// <summary>
        /// 失效学生验证缓存
        /// </summary>
        public async Task InvalidateStudentValidationCacheAsync(string studentId, string name)
        {
            var key = CacheKeyHelper.StudentValidationKey(studentId, name);
            await _cache.RemoveAsync(key);
            _logger.LogInformation("已失效学生验证缓存: {StudentId}, {Name}", studentId, name);
        }
    }
}
