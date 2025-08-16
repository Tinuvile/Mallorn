using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 换物请求管理Repository接口
    /// 提供物品交换、匹配等功能
    /// </summary>
    public interface IExchangeRequestsRepository : IRepository<ExchangeRequest>
    {
        #region 创建操作
        // 换物请求创建由基础仓储 AddAsync 提供
        #endregion

        #region 读取操作
        /// <summary>
        /// 根据发起商品ID获取换物请求
        /// </summary>
        Task<IEnumerable<ExchangeRequest>> GetByOfferProductIdAsync(int productId);
        /// <summary>
        /// 根据目标商品ID获取换物请求
        /// </summary>
        Task<IEnumerable<ExchangeRequest>> GetByRequestProductIdAsync(int productId);
        /// <summary>
        /// 根据用户ID获取换物请求
        /// </summary>
        Task<IEnumerable<ExchangeRequest>> GetByUserIdAsync(int userId);
        /// <summary>
        /// 分页获取换物请求
        /// </summary>
        Task<(IEnumerable<ExchangeRequest> Requests, int TotalCount)> GetPagedRequestsAsync(int pageIndex, int pageSize, string? status = null);
        /// <summary>
        /// 获取待回应的换物请求
        /// </summary>
        Task<IEnumerable<ExchangeRequest>> GetPendingExchangesAsync();
        /// <summary>
        /// 查找与指定商品匹配的换物请求
        /// </summary>
        Task<IEnumerable<ExchangeRequest>> FindMatchingExchangesAsync(int productId);
        /// <summary>
        /// 获取互换机会集合
        /// </summary>
        Task<IEnumerable<ExchangeRequest>> GetMutualExchangeOpportunitiesAsync();
        /// <summary>
        /// 检查商品是否有待处理的换物请求
        /// </summary>
        Task<bool> HasPendingExchangeAsync(int productId);
        /// <summary>
        /// 获取最近N天的换物请求
        /// </summary>
        Task<IEnumerable<ExchangeRequest>> GetRecentExchangesAsync(int days = 7);
        /// <summary>
        /// 获取热门换物分类
        /// </summary>
        Task<IEnumerable<dynamic>> GetPopularExchangeCategoriesAsync();
        #endregion

        #region 更新操作
        /// <summary>
        /// 更新换物请求状态
        /// </summary>
        Task<bool> UpdateExchangeStatusAsync(int exchangeId, string status);
        #endregion

        #region 删除操作
        // 换物请求删除由基础仓储 Delete 提供
        #endregion

        #region 统计操作
        /// <summary>
        /// 获取成功换物次数
        /// </summary>
        Task<int> GetSuccessfulExchangeCountAsync();
        /// <summary>
        /// 获取换物状态统计
        /// </summary>
        Task<Dictionary<string, int>> GetExchangeStatusStatisticsAsync();
        #endregion
    }
}
