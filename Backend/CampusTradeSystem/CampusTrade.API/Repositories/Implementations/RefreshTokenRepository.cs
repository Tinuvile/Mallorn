using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// 刷新令牌管理仓储实现类（RefreshTokenRepository Implementation）
    /// </summary>
    public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(CampusTradeDbContext context) : base(context) { }

        #region 读取操作
        /// <summary>
        /// 根据令牌字符串获取刷新令牌
        /// </summary>
        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        /// <summary>
        /// 获取指定用户的所有刷新令牌
        /// </summary>
        public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(int userId)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 获取指定用户的所有有效刷新令牌
        /// </summary>
        public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(int userId)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.IsRevoked == 0 && rt.ExpiryDate > DateTime.UtcNow)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 获取指定设备的所有刷新令牌
        /// </summary>
        public async Task<IEnumerable<RefreshToken>> GetTokensByDeviceAsync(string deviceId)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.DeviceId == deviceId)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 获取可疑刷新令牌集合
        /// </summary>
        public async Task<IEnumerable<RefreshToken>> GetSuspiciousTokensAsync()
        {
            return await _context.RefreshTokens
                .Where(rt => rt.IsRevoked == 0 && rt.ExpiryDate > DateTime.UtcNow)
                .GroupBy(rt => rt.UserId)
                .Where(g => g.Count() > 5)
                .SelectMany(g => g)
                .ToListAsync();
        }
        #endregion

        #region 更新操作
        /// <summary>
        /// 注销指定令牌
        /// </summary>
        public async Task<bool> RevokeTokenAsync(string token, string? reason = null)
        {
            var refreshToken = await GetByTokenAsync(token);
            if (refreshToken == null) return false;
            refreshToken.IsRevoked = 1;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokeReason = reason;
            Update(refreshToken);
            return true;
        }

        /// <summary>
        /// 注销指定用户的所有令牌
        /// </summary>
        public async Task<bool> RevokeAllUserTokensAsync(int userId, string? reason = null)
        {
            var tokens = await GetActiveTokensByUserAsync(userId);
            foreach (var token in tokens)
            {
                token.IsRevoked = 1;
                token.RevokedAt = DateTime.UtcNow;
                token.RevokeReason = reason;
                Update(token);
            }
            return true;
        }

        /// <summary>
        /// 清理过期令牌
        /// </summary>
        public async Task<int> CleanupExpiredTokensAsync()
        {
            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.ExpiryDate < DateTime.UtcNow)
                .ToListAsync();
            _context.RefreshTokens.RemoveRange(expiredTokens);
            return expiredTokens.Count;
        }
        #endregion

        #region 判断
        /// <summary>
        /// 判断令牌是否有效
        /// </summary>
        public async Task<bool> IsTokenValidAsync(string token)
        {
            var refreshToken = await GetByTokenAsync(token);
            return refreshToken != null && refreshToken.IsRevoked == 0 && refreshToken.ExpiryDate > DateTime.UtcNow;
        }
        #endregion
    }
}
