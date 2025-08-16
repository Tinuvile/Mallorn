using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 信用记录管理Repository接口
    /// 提供信用变更跟踪等功能
    /// </summary>
    public interface ICreditHistoryRepository : IRepository<CreditHistory>
    {
        #region 创建操作
        // 信用记录创建由基础仓储 AddAsync 提供
        #endregion

        #region 读取操作
        /// <summary>
        /// 根据用户ID获取信用记录集合
        /// </summary>
        Task<IEnumerable<CreditHistory>> GetByUserIdAsync(int userId);
        /// <summary>
        /// 分页获取用户信用记录
        /// </summary>
        Task<(IEnumerable<CreditHistory> Records, int TotalCount)> GetPagedByUserIdAsync(int userId, int pageIndex = 0, int pageSize = 10);
        /// <summary>
        /// 根据变更类型获取信用记录
        /// </summary>
        Task<IEnumerable<CreditHistory>> GetByChangeTypeAsync(string changeType);
        /// <summary>
        /// 获取最近N天的信用变更记录
        /// </summary>
        Task<IEnumerable<CreditHistory>> GetRecentChangesAsync(int days = 30);
        #endregion

        #region 统计操作
        /// <summary>
        /// 获取用户信用总变更值
        /// </summary>
        Task<decimal> GetTotalCreditChangeAsync(int userId, string? changeType = null);
        /// <summary>
        /// 获取各变更类型的统计
        /// </summary>
        Task<Dictionary<string, int>> GetChangeTypeStatisticsAsync();
        /// <summary>
        /// 获取用户信用趋势
        /// </summary>
        Task<IEnumerable<dynamic>> GetCreditTrendsAsync(int userId, int days = 30);
        #endregion
    }
}
