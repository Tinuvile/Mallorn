using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// 用户仓储实现类（UserRepository Implementation）
    /// </summary>
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(CampusTradeDbContext context) : base(context)
        {
        }

        #region 创建操作
        /// <summary>
        /// 创建用户
        /// </summary>
        /// <param name="user">用户实体</param>
        /// <returns>创建后的用户实体</returns>
        public async Task<User> CreateUserAsync(User user)
        {
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.CreditScore = 60.0m; // 默认信用分数
            user.IsActive = 1;

            await AddAsync(user);
            return user;
        }
        #endregion

        #region 读取操作
        /// <summary>
        /// 根据邮箱获取用户
        /// </summary>
        /// <param name="email">邮箱</param>
        /// <returns>用户实体</returns>
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        /// <summary>
        /// 根据学号获取用户
        /// </summary>
        /// <param name="studentId">学号</param>
        /// <returns>用户实体</returns>
        public async Task<User?> GetByStudentIdAsync(string studentId)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.StudentId == studentId);
        }

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>用户实体</returns>
        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
        }

        /// <summary>
        /// 获取所有活跃用户
        /// </summary>
        /// <returns>活跃用户集合</returns>
        public async Task<IEnumerable<User>> GetActiveUsersAsync()
        {
            return await _dbSet
                .Where(u => u.IsActive == 1)
                .ToListAsync();
        }

        /// <summary>
        /// 获取用户详细信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户实体</returns>
        public async Task<User?> GetUserWithDetailsAsync(int userId)
        {
            return await _dbSet
                .Include(u => u.Student)
                .Include(u => u.VirtualAccount)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        /// <summary>
        /// 根据安全戳获取用户
        /// </summary>
        /// <param name="securityStamp">安全戳</param>
        /// <returns>用户实体</returns>
        public async Task<User?> GetUserBySecurityStampAsync(string securityStamp)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.SecurityStamp == securityStamp);
        }

        /// <summary>
        /// 获取用户密码修改时间
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>密码修改时间</returns>
        public async Task<DateTime?> GetPasswordChangedAtAsync(int userId)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            return user?.PasswordChangedAt;
        }

        /// <summary>
        /// 获取用户总数
        /// </summary>
        /// <returns>用户总数</returns>
        public async Task<int> GetUserCountAsync()
        {
            return await _dbSet.CountAsync();
        }

        /// <summary>
        /// 获取活跃用户总数
        /// </summary>
        /// <returns>活跃用户总数</returns>
        public async Task<int> GetActiveUserCountAsync()
        {
            return await _dbSet.CountAsync(u => u.IsActive == 1);
        }

        /// <summary>
        /// 获取信用分数范围内的用户
        /// </summary>
        /// <param name="minCredit">最小信用分数</param>
        /// <param name="maxCredit">最大信用分数</param>
        /// <returns>信用分数范围内的用户</returns>
        public async Task<IEnumerable<User>> GetUsersByCreditRangeAsync(decimal minCredit, decimal maxCredit)
        {
            return await _dbSet
                .Where(u => u.CreditScore >= minCredit && u.CreditScore <= maxCredit)
                .ToListAsync();
        }

        /// <summary>
        /// 获取最近注册的用户
        /// </summary>
        /// <param name="days">最近注册的天数</param>
        /// <returns>最近注册的用户</returns>
        public async Task<IEnumerable<User>> GetRecentRegisteredUsersAsync(int days)
        {
            var sinceDate = DateTime.UtcNow.AddDays(-days);
            return await _dbSet
                .Where(u => u.CreatedAt >= sinceDate)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 获取信用分数低于阈值的用户
        /// </summary>
        /// <param name="threshold">阈值</param>
        /// <returns>信用分数低于阈值的用户</returns>
        public async Task<IEnumerable<User>> GetUsersWithLowCreditAsync(decimal threshold)
        {
            return await _dbSet
                .Where(u => u.CreditScore < threshold)
                .OrderBy(u => u.CreditScore)
                .ToListAsync();
        }

        /// <summary>
        /// 获取用户按院系统计的数量
        /// </summary>
        /// <returns>用户按院系统计的数量</returns>
        public async Task<Dictionary<string, int>> GetUserCountByDepartmentAsync()
        {
            return await _dbSet
                .Include(u => u.Student)
                .Where(u => u.Student != null && u.Student.Department != null)
                .GroupBy(u => u.Student!.Department!)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Department, x => x.Count);
        }

        /// <summary>
        /// 获取用户注册趋势
        /// </summary>
        /// <param name="days">最近注册的天数</param>
        /// <returns>用户注册趋势</returns>
        public async Task<Dictionary<DateTime, int>> GetUserRegistrationTrendAsync(int days)
        {
            var startDate = DateTime.UtcNow.AddDays(-days).Date;
            return await _dbSet
                .Where(u => u.CreatedAt >= startDate)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count);
        }

        /// <summary>
        /// 获取信用分数最高的用户
        /// </summary>
        /// <param name="count">数量</param>
        /// <returns>信用分数最高的用户</returns>
        public async Task<IEnumerable<User>> GetTopUsersByCreditAsync(int count)
        {
            return await _dbSet
                .OrderByDescending(u => u.CreditScore)
                .Take(count)
                .ToListAsync();
        }

        /// <summary>
        /// 获取用户登录日志
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>登录日志集合</returns>
        public async Task<IEnumerable<LoginLogs>> GetLoginLogsAsync(int userId)
        {
            return await _context.LoginLogs.Where(l => l.UserId == userId).ToListAsync();
        }
        #endregion

        #region 更新操作
        /// <summary>
        /// 设置用户活跃状态
        /// </summary>
        /// <param name="userId">用户ID</param>
        ///
        public async Task SetUserActiveStatusAsync(int userId, bool isActive)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.IsActive = isActive ? 1 : 0;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        /// <summary>
        /// 更新用户最后登录时间
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="ipAddress">IP地址</param>
        public async Task UpdateLastLoginAsync(int userId, string ipAddress)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                user.LastLoginIp = ipAddress;
                user.LoginCount++;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        /// <summary>
        /// 更新用户安全戳
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="newSecurityStamp">新安全戳</param>
        public async Task UpdateSecurityStampAsync(int userId, string newSecurityStamp)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.SecurityStamp = newSecurityStamp;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        /// <summary>
        /// 锁定用户
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="lockoutEnd">锁定结束时间</param>
        public async Task LockUserAsync(int userId, DateTime? lockoutEnd = null)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.IsLocked = 1;
                user.LockoutEnd = lockoutEnd;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        /// <summary>
        /// 解锁用户
        /// </summary>
        /// <param name="userId">用户ID</param>
        public async Task UnlockUserAsync(int userId)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.IsLocked = 0;
                user.LockoutEnd = null;
                user.FailedLoginAttempts = 0;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        /// <summary>
        /// 增加用户失败登录次数
        /// </summary>
        /// <param name="userId">用户ID</param>
        public async Task IncrementFailedLoginAttemptsAsync(int userId)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.FailedLoginAttempts++;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        /// <summary>
        /// 重置用户失败登录次数
        /// </summary>
        /// <param name="userId">用户ID</param>
        public async Task ResetFailedLoginAttemptsAsync(int userId)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.FailedLoginAttempts = 0;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        /// <summary>
        /// 更新用户密码
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="newPasswordHash">新密码哈希</param>
        public async Task UpdatePasswordAsync(int userId, string newPasswordHash)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.PasswordHash = newPasswordHash;
                user.PasswordChangedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        /// <summary>
        /// 设置用户邮箱验证状态
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="isVerified">是否验证</param>
        public async Task SetEmailVerifiedAsync(int userId, bool isVerified)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.EmailVerified = isVerified ? 1 : 0;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        /// <summary>
        /// 更新用户邮箱验证令牌
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="token">令牌</param>
        public async Task UpdateEmailVerificationTokenAsync(int userId, string? token)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.EmailVerificationToken = token;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        /// <summary>
        /// 设置用户两步验证状态
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="enabled">是否启用</param>
        public async Task SetTwoFactorEnabledAsync(int userId, bool enabled)
        {
            var user = await GetByPrimaryKeyAsync(userId);
            if (user != null)
            {
                user.TwoFactorEnabled = enabled ? 1 : 0;
                user.UpdatedAt = DateTime.UtcNow;
                Update(user);
            }
        }

        #endregion

        #region 删除操作
        #endregion

        #region 关系查询
        /// <summary>
        /// 获取用户详细信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户实体</returns>
        public async Task<User?> GetUserWithStudentAsync(int userId)
        {
            return await _dbSet
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        /// <summary>
        /// 获取用户虚拟账户信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户实体</returns>
        public async Task<User?> GetUserWithVirtualAccountAsync(int userId)
        {
            return await _dbSet
                .Include(u => u.VirtualAccount)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        /// <summary>
        /// 获取用户刷新令牌信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户实体</returns>
        public async Task<User?> GetUserWithRefreshTokensAsync(int userId)
        {
            return await _dbSet
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        /// <summary>
        /// 获取用户订单信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户实体</returns>
        public async Task<User?> GetUserWithOrdersAsync(int userId)
        {
            return await _dbSet
                .Include(u => u.BuyerOrders)
                .Include(u => u.SellerOrders)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        /// <summary>
        /// 获取用户商品信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户实体</returns>
        public async Task<User?> GetUserWithProductsAsync(int userId)
        {
            return await _dbSet
                .Include(u => u.Products)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        /// <summary>
        /// 获取用户通知信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户实体</returns>
        public async Task<User?> GetUserWithNotificationsAsync(int userId)
        {
            return await _dbSet
                .Include(u => u.ReceivedNotifications)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        #endregion

        #region 高级查询
        /// <summary>
        /// 搜索用户
        /// </summary>
        /// <param name="keyword">关键字</param>
        /// <param name="department">院系</param>
        /// <param name="minCredit">最小信用分数</param>
        /// <param name="maxCredit">最大信用分数</param>
        public async Task<(IEnumerable<User> Users, int TotalCount)> SearchUsersAsync(
            string? keyword = null,
            string? department = null,
            decimal? minCredit = null,
            decimal? maxCredit = null,
            bool? isActive = null,
            bool? isLocked = null,
            DateTime? registeredAfter = null,
            DateTime? registeredBefore = null,
            int pageNumber = 1,
            int pageSize = 20)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(u => u.Username.Contains(keyword) || u.Email.Contains(keyword));
            }

            if (department != null)
            {
                query = query.Include(u => u.Student)
                             .Where(u => u.Student != null && u.Student.Department == department);
            }

            if (minCredit.HasValue)
            {
                query = query.Where(u => u.CreditScore >= minCredit.Value);
            }

            if (maxCredit.HasValue)
            {
                query = query.Where(u => u.CreditScore <= maxCredit.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == (isActive.Value ? 1 : 0));
            }

            if (isLocked.HasValue)
            {
                query = query.Where(u => u.IsLocked == (isLocked.Value ? 1 : 0));
            }

            if (registeredAfter.HasValue)
            {
                query = query.Where(u => u.CreatedAt >= registeredAfter.Value);
            }

            if (registeredBefore.HasValue)
            {
                query = query.Where(u => u.CreatedAt <= registeredBefore.Value);
            }

            var totalCount = await query.CountAsync();
            var users = await query.Skip((pageNumber - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            return (users, totalCount);
        }
        #endregion

        #region 检查与状态判断
        // 静态方法已迁移至 Utils/UserUtils
        #endregion
    }
}
