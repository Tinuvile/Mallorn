using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 邮箱验证仓储接口
    /// 提供邮箱验证相关的专用数据访问方法
    /// </summary>
    public interface IEmailVerificationRepository : IRepository<EmailVerification>
    {
        /// <summary>
        /// 获取用户的活跃验证记录（未使用且未过期）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="email">邮箱地址</param>
        /// <param name="verificationType">验证类型（Code或Token）</param>
        /// <returns>活跃的验证记录</returns>
        Task<EmailVerification?> GetActiveVerificationAsync(int userId, string email, string? verificationType = null);

        /// <summary>
        /// 获取用户最近的验证码记录（用于频率限制检查）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="email">邮箱地址</param>
        /// <param name="withinMinutes">时间范围（分钟）</param>
        /// <returns>最近的验证记录</returns>
        Task<EmailVerification?> GetRecentVerificationAsync(int userId, string email, int withinMinutes);

        /// <summary>
        /// 根据验证码获取验证记录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="code">验证码</param>
        /// <returns>匹配的验证记录</returns>
        Task<EmailVerification?> GetByVerificationCodeAsync(int userId, string code);

        /// <summary>
        /// 根据Token获取验证记录
        /// </summary>
        /// <param name="token">验证令牌</param>
        /// <returns>匹配的验证记录</returns>
        Task<EmailVerification?> GetByTokenAsync(string token);

        /// <summary>
        /// 标记验证记录为已使用
        /// </summary>
        /// <param name="verificationId">验证记录ID</param>
        /// <returns>是否标记成功</returns>
        Task<bool> MarkAsUsedAsync(int verificationId);

        /// <summary>
        /// 清理过期的验证记录
        /// </summary>
        /// <param name="expiredBefore">过期时间点</param>
        /// <returns>清理的记录数</returns>
        Task<int> CleanupExpiredAsync(DateTime expiredBefore);

        /// <summary>
        /// 清理指定用户的旧验证记录（保留最新N条）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="keepLatestCount">保留最新记录数量</param>
        /// <returns>清理的记录数</returns>
        Task<int> CleanupOldRecordsAsync(int userId, int keepLatestCount = 5);

        /// <summary>
        /// 获取用户在指定时间范围内的验证尝试次数
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="withinMinutes">时间范围（分钟）</param>
        /// <returns>尝试次数</returns>
        Task<int> GetVerificationAttemptsCountAsync(int userId, int withinMinutes);

        /// <summary>
        /// 获取系统中过期的验证记录数量
        /// </summary>
        /// <returns>过期记录数量</returns>
        Task<int> GetExpiredRecordsCountAsync();
    }
}