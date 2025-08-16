using System.Collections.Generic;
using System.Threading.Tasks;
using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Services.Interfaces
{
    /// <summary>
    /// 用户缓存服务接口
    /// </summary>
    public interface IUserCacheService
    {
        /// <summary>
        /// 获取完整用户信息缓存
        /// </summary>
        Task<User?> GetUserAsync(int userId);

        /// <summary>
        /// 根据用户名或邮箱获取用户信息（带学生信息）
        /// </summary>
        Task<User?> GetUserByUsernameAsync(string username);

        /// <summary>
        /// 设置用户信息缓存
        /// </summary>
        Task SetUserAsync(User user);

        /// <summary>
        /// 设置根据用户名/邮箱查询的用户缓存
        /// </summary>
        Task SetUserByUsernameAsync(string username, User? user);

        /// <summary>
        /// 验证学生身份（带缓存）
        /// </summary>
        Task<bool> ValidateStudentAsync(string studentId, string name);

        /// <summary>
        /// 设置学生验证结果缓存
        /// </summary>
        Task SetStudentValidationAsync(string studentId, string name, bool isValid);

        /// <summary>
        /// 获取用户安全信息缓存（包含登录状态等敏感字段）
        /// </summary>
        Task<User?> GetSecurityInfoAsync(int userId);

        /// <summary>
        /// 获取用户权限缓存
        /// </summary>
        Task<List<string>> GetPermissionsAsync(int userId);

        /// <summary>
        /// 刷新用户权限缓存
        /// </summary>
        Task RefreshPermissionsAsync(int userId);

        /// <summary>
        /// 刷新用户安全信息缓存
        /// </summary>
        Task RefreshSecurityAsync(int userId);

        /// <summary>
        /// 刷新用户信息缓存
        /// </summary>
        Task RefreshUserAsync(int userId);

        /// <summary>
        /// 移除用户所有相关缓存
        /// </summary>
        Task RemoveAllUserDataAsync(int userId);

        /// <summary>
        /// 批量获取用户缓存
        /// </summary>
        Task<Dictionary<int, User>> GetUsersAsync(IEnumerable<int> userIds);

        /// <summary>
        /// 获取用户基础信息（不包含敏感字段）
        /// </summary>
        Task<User?> GetBasicInfoAsync(int userId);

        /// <summary>
        /// 获取缓存命中率统计
        /// </summary>
        Task<double> GetHitRate();

        /// <summary>
        /// 失效指定用户的所有缓存数据
        /// </summary>
        /// <param name="userId">用户ID</param>
        Task InvalidateUserCacheAsync(int userId);

        /// <summary>
        /// 批量失效多个用户的缓存数据
        /// </summary>
        /// <param name="userIds">用户ID集合</param>
        Task InvalidateUsersCacheAsync(IEnumerable<int> userIds);

        /// <summary>
        /// 失效用户安全信息缓存
        /// </summary>
        /// <param name="userId">用户ID</param>
        Task InvalidateUserSecurityCacheAsync(int userId);

        /// <summary>
        /// 失效用户权限缓存
        /// </summary>
        /// <param name="userId">用户ID</param>
        Task InvalidateUserPermissionsCacheAsync(int userId);

        /// <summary>
        /// 失效用户名/邮箱查询缓存
        /// </summary>
        /// <param name="username">用户名或邮箱</param>
        Task InvalidateUsernameQueryCacheAsync(string username);

        /// <summary>
        /// 失效学生验证缓存
        /// </summary>
        /// <param name="studentId">学号</param>
        /// <param name="name">姓名</param>
        Task InvalidateStudentValidationCacheAsync(string studentId, string name);
    }
}
