using System.Linq.Expressions;
using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 用户仓储接口（UserRepository Interface）
    /// <para>所属分层：仓储层（Repositories）</para>
    /// <para>用途：定义User实体的数据访问操作，规范所有用户相关的数据持久化方法</para>
    /// <para>继承关系：继承自IRepository&lt;User&gt;</para>
    /// <para>主要职责：仅负责数据访问，不包含任何业务逻辑</para>
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        #region 创建操作
        /// <summary>
        /// 创建新用户
        /// </summary>
        /// <param name="user">要创建的用户实体</param>
        /// <returns>创建后的用户实体</returns>
        Task<User> CreateUserAsync(User user);
        #endregion

        #region 读取操作
        /// <summary>
        /// 根据邮箱查询用户
        /// </summary>
        /// <param name="email">邮箱地址</param>
        /// <returns>匹配的用户实体或null</returns>
        Task<User?> GetByEmailAsync(string email);
        /// <summary>
        /// 根据学号查询用户
        /// </summary>
        /// <param name="studentId">学号</param>
        /// <returns>匹配的用户实体或null</returns>
        Task<User?> GetByStudentIdAsync(string studentId);
        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>匹配的用户实体或null</returns>
        Task<User?> GetByUsernameAsync(string username);
        /// <summary>
        /// 获取所有活跃用户
        /// </summary>
        /// <returns>活跃用户集合</returns>
        Task<IEnumerable<User>> GetActiveUsersAsync();
        /// <summary>
        /// 获取用户详细信息（含导航属性）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户实体或null</returns>
        Task<User?> GetUserWithDetailsAsync(int userId);
        /// <summary>
        /// 根据安全戳获取用户
        /// </summary>
        /// <param name="securityStamp">安全戳</param>
        /// <returns>用户实体或null</returns>
        Task<User?> GetUserBySecurityStampAsync(string securityStamp);
        /// <summary>
        /// 获取密码修改时间
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>密码修改时间</returns>
        Task<DateTime?> GetPasswordChangedAtAsync(int userId);
        /// <summary>
        /// 获取用户总数
        /// </summary>
        /// <returns>用户总数</returns>
        Task<int> GetUserCountAsync();
        /// <summary>
        /// 获取活跃用户总数
        /// </summary>
        /// <returns>活跃用户总数</returns>
        Task<int> GetActiveUserCountAsync();
        /// <summary>
        /// 根据信用分数范围获取用户
        /// </summary>
        /// <param name="minCredit">最小信用分</param>
        /// <param name="maxCredit">最大信用分</param>
        /// <returns>用户集合</returns>
        Task<IEnumerable<User>> GetUsersByCreditRangeAsync(decimal minCredit, decimal maxCredit);
        /// <summary>
        /// 根据注册时间获取用户
        /// </summary>
        /// <param name="days">最近N天</param>
        /// <returns>用户集合</returns>
        Task<IEnumerable<User>> GetRecentRegisteredUsersAsync(int days);
        /// <summary>
        /// 获取低信用用户
        /// </summary>
        /// <param name="threshold">信用分阈值</param>
        /// <returns>用户集合</returns>
        Task<IEnumerable<User>> GetUsersWithLowCreditAsync(decimal threshold);
        /// <summary>
        /// 根据院系分类统计用户数量
        /// </summary>
        /// <returns>院系-用户数字典</returns>
        Task<Dictionary<string, int>> GetUserCountByDepartmentAsync();
        /// <summary>
        /// 根据注册时间统计用户数量
        /// </summary>
        /// <param name="days">最近N天</param>
        /// <returns>日期-用户数字典</returns>
        Task<Dictionary<DateTime, int>> GetUserRegistrationTrendAsync(int days);
        /// <summary>
        /// 获取信用分数最高的用户
        /// </summary>
        /// <param name="count">数量</param>
        /// <returns>用户集合</returns>
        Task<IEnumerable<User>> GetTopUsersByCreditAsync(int count);
        /// <summary>
        /// 获取用户登录日志
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>登录日志集合</returns>
        Task<IEnumerable<LoginLogs>> GetLoginLogsAsync(int userId);
        #endregion

        #region 更新操作
        /// <summary>
        /// 更新用户账号状态
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="isActive">是否活跃</param>
        Task SetUserActiveStatusAsync(int userId, bool isActive);
        /// <summary>
        /// 更新用户最后登录信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="ipAddress">登录IP</param>
        Task UpdateLastLoginAsync(int userId, string ipAddress);
        /// <summary>
        /// 更新用户安全戳
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="newSecurityStamp">新安全戳</param>
        Task UpdateSecurityStampAsync(int userId, string newSecurityStamp);
        /// <summary>
        /// 锁定用户
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="lockoutEnd">锁定截止时间</param>
        Task LockUserAsync(int userId, DateTime? lockoutEnd = null);
        /// <summary>
        /// 解锁用户
        /// </summary>
        /// <param name="userId">用户ID</param>
        Task UnlockUserAsync(int userId);
        /// <summary>
        /// 增加登录失败次数
        /// </summary>
        /// <param name="userId">用户ID</param>
        Task IncrementFailedLoginAttemptsAsync(int userId);
        /// <summary>
        /// 重置登录失败次数
        /// </summary>
        /// <param name="userId">用户ID</param>
        Task ResetFailedLoginAttemptsAsync(int userId);
        /// <summary>
        /// 更新用户密码
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="newPasswordHash">新密码Hash</param>
        Task UpdatePasswordAsync(int userId, string newPasswordHash);
        /// <summary>
        /// 更新用户邮箱验证状态
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="isVerified">是否已验证</param>
        Task SetEmailVerifiedAsync(int userId, bool isVerified);
        /// <summary>
        /// 更新用户邮箱验证令牌
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="token">邮箱验证令牌</param>
        Task UpdateEmailVerificationTokenAsync(int userId, string? token);
        /// <summary>
        /// 更新用户双因子认证状态
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="enabled">是否启用</param>
        Task SetTwoFactorEnabledAsync(int userId, bool enabled);
        #endregion

        #region 删除操作
        #endregion

        #region 关系查询
        /// <summary>
        /// 查询用户与学生信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户实体或null</returns>
        Task<User?> GetUserWithStudentAsync(int userId);
        /// <summary>
        /// 查询用户与虚拟账户信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户实体或null</returns>
        Task<User?> GetUserWithVirtualAccountAsync(int userId);
        /// <summary>
        /// 查询用户与刷新令牌信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户实体或null</returns>
        Task<User?> GetUserWithRefreshTokensAsync(int userId);
        /// <summary>
        /// 查询用户与订单信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户实体或null</returns>
        Task<User?> GetUserWithOrdersAsync(int userId);
        /// <summary>
        /// 查询用户与商品信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户实体或null</returns>
        Task<User?> GetUserWithProductsAsync(int userId);
        /// <summary>
        /// 查询用户与通知信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户实体或null</returns>
        Task<User?> GetUserWithNotificationsAsync(int userId);
        #endregion

        #region 高级查询
        /// <summary>
        /// 高级用户查询，支持多条件筛选与分页
        /// </summary>
        /// <param name="keyword">关键词（用户名或邮箱）</param>
        /// <param name="department">院系</param>
        /// <param name="minCredit">最小信用分</param>
        /// <param name="maxCredit">最大信用分</param>
        /// <param name="isActive">是否活跃</param>
        /// <param name="isLocked">是否锁定</param>
        /// <param name="registeredAfter">注册起始时间</param>
        /// <param name="registeredBefore">注册截止时间</param>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <returns>用户集合及总数</returns>
        Task<(IEnumerable<User> Users, int TotalCount)> SearchUsersAsync(
            string? keyword = null,
            string? department = null,
            decimal? minCredit = null,
            decimal? maxCredit = null,
            bool? isActive = null,
            bool? isLocked = null,
            DateTime? registeredAfter = null,
            DateTime? registeredBefore = null,
            int pageNumber = 1,
            int pageSize = 20);
        #endregion
    }
}
