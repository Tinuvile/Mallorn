using System.Collections.Generic;
using System.Threading.Tasks;
using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 刷新令牌管理仓储接口（RefreshTokenRepository Interface）
    /// </summary>
    public interface IRefreshTokenRepository : IRepository<RefreshToken>
    {
        #region 创建操作
        // 暂无特定创建操作，使用基础仓储接口方法
        #endregion

        #region 读取操作
        /// <summary>
        /// 根据令牌字符串获取刷新令牌
        /// </summary>
        /// <param name="token">令牌字符串</param>
        /// <returns>刷新令牌实体或null</returns>
        Task<RefreshToken?> GetByTokenAsync(string token);
        /// <summary>
        /// 获取指定用户的所有刷新令牌
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>刷新令牌集合</returns>
        Task<IEnumerable<RefreshToken>> GetByUserIdAsync(int userId);
        /// <summary>
        /// 获取指定用户的所有有效刷新令牌
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>刷新令牌集合</returns>
        Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(int userId);
        /// <summary>
        /// 获取指定设备的所有刷新令牌
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>刷新令牌集合</returns>
        Task<IEnumerable<RefreshToken>> GetTokensByDeviceAsync(string deviceId);
        /// <summary>
        /// 获取可疑刷新令牌集合
        /// </summary>
        /// <returns>刷新令牌集合</returns>
        Task<IEnumerable<RefreshToken>> GetSuspiciousTokensAsync();
        #endregion

        #region 更新操作
        /// <summary>
        /// 注销指定令牌
        /// </summary>
        /// <param name="token">令牌字符串</param>
        /// <param name="reason">注销原因</param>
        /// <returns>是否成功</returns>
        Task<bool> RevokeTokenAsync(string token, string? reason = null);
        /// <summary>
        /// 注销指定用户的所有令牌
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="reason">注销原因</param>
        /// <returns>是否成功</returns>
        Task<bool> RevokeAllUserTokensAsync(int userId, string? reason = null);
        /// <summary>
        /// 清理过期令牌
        /// </summary>
        /// <returns>清理数量</returns>
        Task<int> CleanupExpiredTokensAsync();
        #endregion

        #region 删除操作
        // 暂无特定删除操作，使用基础仓储接口方法
        #endregion

        #region 关系查询
        // 暂无特定关系查询，使用基础仓储接口方法
        #endregion

        #region 判断
        /// <summary>
        /// 判断令牌是否有效
        /// </summary>
        /// <param name="token">令牌字符串</param>
        /// <returns>是否有效</returns>
        Task<bool> IsTokenValidAsync(string token);
        #endregion
    }
}
