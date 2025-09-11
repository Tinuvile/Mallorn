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
    /// 议价管理仓储实现类（NegotiationsRepository Implementation）
    /// </summary>
    public class NegotiationsRepository : Repository<Negotiation>, INegotiationsRepository
    {
        public NegotiationsRepository(CampusTradeDbContext context) : base(context) { }

        #region 创建操作
        // 暂无特定创建操作，使用基础仓储接口方法
        #endregion

        #region 读取操作
        /// <summary>
        /// 根据订单ID获取议价记录
        /// </summary>
        public async Task<IEnumerable<Negotiation>> GetByOrderIdAsync(int orderId)
        {
            return await _context.Negotiations
                .Include(n => n.Order)
                .Where(n => n.OrderId == orderId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 获取订单最新议价
        /// </summary>
        public async Task<Negotiation?> GetLatestNegotiationAsync(int orderId)
        {
            return await _context.Negotiations
                .Include(n => n.Order)
                .Where(n => n.OrderId == orderId)
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// 获取用户待处理议价
        /// </summary>
        public async Task<IEnumerable<Negotiation>> GetPendingNegotiationsAsync(int userId)
        {
            return await _context.Negotiations
                .Include(n => n.Order)
                .ThenInclude(o => o.Product)
                .Where(n => n.Status == "等待回应" && (n.Order.BuyerId == userId || n.Order.SellerId == userId))
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 根据状态获取议价集合
        /// </summary>
        public async Task<IEnumerable<Negotiation>> GetNegotiationsByStatusAsync(string status)
        {
            return await _context.Negotiations
                .Include(n => n.Order)
                .ThenInclude(o => o.Product)
                .Where(n => n.Status == status)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 获取订单议价历史
        /// </summary>
        public async Task<IEnumerable<Negotiation>> GetNegotiationHistoryAsync(int orderId)
        {
            return await GetByOrderIdAsync(orderId);
        }

        /// <summary>
        /// 获取订单议价次数
        /// </summary>
        public async Task<int> GetNegotiationCountByOrderAsync(int orderId)
        {
            return await _context.Negotiations
                .CountAsync(n => n.OrderId == orderId);
        }

        /// <summary>
        /// 获取指定天数内的最新议价
        /// </summary>
        public async Task<IEnumerable<Negotiation>> GetRecentNegotiationsAsync(int days = 7)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _context.Negotiations
                .Include(n => n.Order)
                .ThenInclude(o => o.Product)
                .Where(n => n.CreatedAt >= cutoffDate)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
        #endregion

        #region 更新操作
        /// <summary>
        /// 更新议价状态
        /// </summary>
        public async Task<bool> UpdateNegotiationStatusAsync(int negotiationId, string status)
        {
            var negotiation = await GetByPrimaryKeyAsync(negotiationId);
            if (negotiation == null) return false;
            negotiation.Status = status;
            Update(negotiation);
            return true;
        }
        #endregion

        #region 删除操作
        // 暂无特定删除操作，使用基础仓储接口方法
        #endregion

        #region 关系查询
        /// <summary>
        /// 判断订单是否有活跃议价
        /// </summary>
        public async Task<bool> HasActiveNegotiationAsync(int orderId)
        {
            var count = await _context.Negotiations
                .CountAsync(n => n.OrderId == orderId && (n.Status == "等待回应" || n.Status == "反报价"));
            return count > 0;
        }
        #endregion

        #region 高级查询
        /// <summary>
        /// 获取议价平均折扣率
        /// </summary>
        public async Task<decimal> GetAverageNegotiationRateAsync()
        {
            var negotiations = await _context.Negotiations
                .Include(n => n.Order)
                .ThenInclude(o => o.Product)
                .Where(n => n.Order.Product != null && n.Order.Product.BasePrice > 0)
                .Select(n => new { n.ProposedPrice, BasePrice = n.Order.Product.BasePrice })
                .ToListAsync();
            if (!negotiations.Any()) return 0;
            var rates = negotiations.Select(n => n.ProposedPrice / n.BasePrice).ToList();
            return rates.Average();
        }

        /// <summary>
        /// 获取成功议价总数
        /// </summary>
        public async Task<int> GetSuccessfulNegotiationCountAsync()
        {
            return await _context.Negotiations.CountAsync(n => n.Status == "接受");
        }

        /// <summary>
        /// 获取议价统计信息
        /// </summary>
        public async Task<IEnumerable<dynamic>> GetNegotiationStatisticsAsync()
        {
            return await _context.Negotiations
                .GroupBy(n => n.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count(),
                    AveragePrice = g.Average(n => n.ProposedPrice)
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync<dynamic>();
        }
        #endregion
    }
}
