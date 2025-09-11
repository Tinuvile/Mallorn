using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// 换物请求管理Repository实现
    /// 提供物品交换、匹配等功能
    /// </summary>
    public class ExchangeRequestsRepository : Repository<ExchangeRequest>, IExchangeRequestsRepository
    {
        public ExchangeRequestsRepository(CampusTradeDbContext context) : base(context) { }

        #region 读取操作
        /// <summary>
        /// 根据发起商品ID获取换物请求
        /// </summary>
        public async Task<IEnumerable<ExchangeRequest>> GetByOfferProductIdAsync(int productId)
        {
            return await _context.ExchangeRequests.Include(e => e.OfferProduct).Include(e => e.RequestProduct).Where(e => e.OfferProductId == productId).OrderByDescending(e => e.CreatedAt).ToListAsync();
        }
        /// <summary>
        /// 根据目标商品ID获取换物请求
        /// </summary>
        public async Task<IEnumerable<ExchangeRequest>> GetByRequestProductIdAsync(int productId)
        {
            return await _context.ExchangeRequests.Include(e => e.OfferProduct).Include(e => e.RequestProduct).Where(e => e.RequestProductId == productId).OrderByDescending(e => e.CreatedAt).ToListAsync();
        }
        /// <summary>
        /// 根据用户ID获取换物请求
        /// </summary>
        public async Task<IEnumerable<ExchangeRequest>> GetByUserIdAsync(int userId)
        {
            return await _context.ExchangeRequests.Include(e => e.OfferProduct).Include(e => e.RequestProduct).Where(e => e.OfferProduct.UserId == userId || e.RequestProduct.UserId == userId).OrderByDescending(e => e.CreatedAt).ToListAsync();
        }
        /// <summary>
        /// 分页获取换物请求
        /// </summary>
        public async Task<(IEnumerable<ExchangeRequest> Requests, int TotalCount)> GetPagedRequestsAsync(int pageIndex, int pageSize, string? status = null)
        {
            var query = _context.ExchangeRequests.Include(e => e.OfferProduct).Include(e => e.RequestProduct).AsQueryable();
            if (!string.IsNullOrEmpty(status)) query = query.Where(e => e.Status == status);
            query = query.OrderByDescending(e => e.CreatedAt);
            var totalCount = await query.CountAsync();
            var requests = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();
            return (requests, totalCount);
        }
        /// <summary>
        /// 获取待回应的换物请求
        /// </summary>
        public async Task<IEnumerable<ExchangeRequest>> GetPendingExchangesAsync()
        {
            return await _context.ExchangeRequests.Include(e => e.OfferProduct).Include(e => e.RequestProduct).Where(e => e.Status == "等待回应").OrderByDescending(e => e.CreatedAt).ToListAsync();
        }
        /// <summary>
        /// 查找与指定商品匹配的换物请求
        /// </summary>
        public async Task<IEnumerable<ExchangeRequest>> FindMatchingExchangesAsync(int productId)
        {
            return await _context.ExchangeRequests.Include(e => e.OfferProduct).Include(e => e.RequestProduct).Where(e => e.RequestProductId == productId && e.Status == "等待回应").OrderByDescending(e => e.CreatedAt).ToListAsync();
        }
        /// <summary>
        /// 获取互换机会集合
        /// </summary>
        public async Task<IEnumerable<ExchangeRequest>> GetMutualExchangeOpportunitiesAsync()
        {
            var allRequests = await _context.ExchangeRequests.Include(e => e.OfferProduct).Include(e => e.RequestProduct).Where(e => e.Status == "等待回应").ToListAsync();
            var mutualOpportunities = new List<ExchangeRequest>();
            foreach (var request in allRequests)
            {
                var mutualRequest = allRequests.FirstOrDefault(r => r.ExchangeId != request.ExchangeId && r.OfferProductId == request.RequestProductId && r.RequestProductId == request.OfferProductId);
                if (mutualRequest != null && !mutualOpportunities.Contains(request))
                {
                    mutualOpportunities.Add(request);
                }
            }
            return mutualOpportunities;
        }
        /// <summary>
        /// 检查商品是否有待处理的换物请求
        /// </summary>
        public async Task<bool> HasPendingExchangeAsync(int productId)
        {
            var count = await _context.ExchangeRequests.CountAsync(e => (e.OfferProductId == productId || e.RequestProductId == productId) && e.Status == "等待回应");
            return count > 0;
        }
        /// <summary>
        /// 获取最近N天的换物请求
        /// </summary>
        public async Task<IEnumerable<ExchangeRequest>> GetRecentExchangesAsync(int days = 7)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _context.ExchangeRequests.Include(e => e.OfferProduct).Include(e => e.RequestProduct).Where(e => e.CreatedAt >= cutoffDate).OrderByDescending(e => e.CreatedAt).ToListAsync();
        }
        /// <summary>
        /// 获取热门换物分类
        /// </summary>
        public async Task<IEnumerable<dynamic>> GetPopularExchangeCategoriesAsync()
        {
            return await _context.ExchangeRequests.Include(e => e.OfferProduct).ThenInclude(p => p.Category).GroupBy(e => e.OfferProduct.Category.Name).Select(g => new { Category = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).Take(10).ToListAsync<dynamic>();
        }
        #endregion

        #region 更新操作
        /// <summary>
        /// 更新换物请求状态
        /// </summary>
        public async Task<bool> UpdateExchangeStatusAsync(int exchangeId, string status)
        {
            try
            {
                var exchange = await GetByPrimaryKeyAsync(exchangeId);
                if (exchange == null) return false;
                exchange.Status = status;
                Update(exchange);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region 统计操作
        /// <summary>
        /// 获取成功换物次数
        /// </summary>
        public async Task<int> GetSuccessfulExchangeCountAsync()
        {
            return await _context.ExchangeRequests.CountAsync(e => e.Status == "接受");
        }
        /// <summary>
        /// 获取换物状态统计
        /// </summary>
        public async Task<Dictionary<string, int>> GetExchangeStatusStatisticsAsync()
        {
            return await _context.ExchangeRequests.GroupBy(e => e.Status).ToDictionaryAsync(g => g.Key, g => g.Count());
        }
        #endregion
    }
}
