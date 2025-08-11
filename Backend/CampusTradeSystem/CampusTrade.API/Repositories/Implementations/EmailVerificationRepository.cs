using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// 邮箱验证仓储实现类
    /// </summary>
    public class EmailVerificationRepository : Repository<EmailVerification>, IEmailVerificationRepository
    {
        public EmailVerificationRepository(CampusTradeDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 获取用户的活跃验证记录（未使用且未过期）
        /// </summary>
        public async Task<EmailVerification?> GetActiveVerificationAsync(int userId, string email, string? verificationType = null)
        {
            var query = _dbSet.Where(v => v.UserId == userId
                                        && v.Email == email
                                        && v.IsUsed == 0
                                        && v.ExpireTime >= DateTime.Now);

            // 如果指定了验证类型，添加额外的过滤条件
            if (!string.IsNullOrEmpty(verificationType))
            {
                if (verificationType.Equals("Code", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(v => !string.IsNullOrEmpty(v.VerificationCode));
                }
                else if (verificationType.Equals("Token", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(v => !string.IsNullOrEmpty(v.Token));
                }
            }

            return await query.OrderByDescending(v => v.CreatedAt).FirstOrDefaultAsync();
        }

        /// <summary>
        /// 获取用户最近的验证码记录（用于频率限制检查）
        /// </summary>
        public async Task<EmailVerification?> GetRecentVerificationAsync(int userId, string email, int withinMinutes)
        {
            var cutoffTime = DateTime.Now.AddMinutes(-withinMinutes);

            return await _dbSet
                .Where(v => v.UserId == userId
                         && v.Email == email
                         && v.CreatedAt >= cutoffTime)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// 根据验证码获取验证记录
        /// </summary>
        public async Task<EmailVerification?> GetByVerificationCodeAsync(int userId, string code)
        {
            return await _dbSet
                .FirstOrDefaultAsync(v => v.UserId == userId
                                       && v.VerificationCode == code
                                       && v.IsUsed == 0
                                       && v.ExpireTime >= DateTime.Now);
        }

        /// <summary>
        /// 根据Token获取验证记录
        /// </summary>
        public async Task<EmailVerification?> GetByTokenAsync(string token)
        {
            return await _dbSet
                .FirstOrDefaultAsync(v => v.Token == token
                                       && v.IsUsed == 0
                                       && v.ExpireTime >= DateTime.Now);
        }

        /// <summary>
        /// 标记验证记录为已使用
        /// </summary>
        public async Task<bool> MarkAsUsedAsync(int verificationId)
        {
            var verification = await GetByPrimaryKeyAsync(verificationId);
            if (verification == null)
                return false;

            verification.IsUsed = 1;
            Update(verification);
            return true;
        }

        /// <summary>
        /// 清理过期的验证记录
        /// </summary>
        public async Task<int> CleanupExpiredAsync(DateTime expiredBefore)
        {
            var expiredRecords = await _dbSet
                .Where(v => v.ExpireTime < expiredBefore || v.IsUsed == 1)
                .ToListAsync();

            if (expiredRecords.Any())
            {
                _dbSet.RemoveRange(expiredRecords);
                return expiredRecords.Count;
            }

            return 0;
        }

        /// <summary>
        /// 清理指定用户的旧验证记录（保留最新N条）
        /// </summary>
        public async Task<int> CleanupOldRecordsAsync(int userId, int keepLatestCount = 5)
        {
            var userRecords = await _dbSet
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.CreatedAt)
                .Skip(keepLatestCount)
                .ToListAsync();

            if (userRecords.Any())
            {
                _dbSet.RemoveRange(userRecords);
                return userRecords.Count;
            }

            return 0;
        }

        /// <summary>
        /// 获取用户在指定时间范围内的验证尝试次数
        /// </summary>
        public async Task<int> GetVerificationAttemptsCountAsync(int userId, int withinMinutes)
        {
            var cutoffTime = DateTime.Now.AddMinutes(-withinMinutes);

            return await _dbSet
                .CountAsync(v => v.UserId == userId && v.CreatedAt >= cutoffTime);
        }

        /// <summary>
        /// 获取系统中过期的验证记录数量
        /// </summary>
        public async Task<int> GetExpiredRecordsCountAsync()
        {
            var now = DateTime.Now;
            return await _dbSet
                .CountAsync(v => v.ExpireTime < now || v.IsUsed == 1);
        }
    }
}