using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// Order实体的Repository实现类
    /// 继承基础Repository，提供Order特有的查询和操作方法
    /// </summary>
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(CampusTradeDbContext context) : base(context) { }

        #region 读取操作
        /// <summary>
        /// 根据买家ID获取订单集合
        /// </summary>
        public async Task<IEnumerable<Order>> GetByBuyerIdAsync(int buyerId)
        {
            return await _dbSet.Where(o => o.BuyerId == buyerId).Include(o => o.Product).Include(o => o.Seller).Include(o => o.Negotiations).OrderByDescending(o => o.CreateTime).ToListAsync();
        }
        /// <summary>
        /// 根据卖家ID获取订单集合
        /// </summary>
        public async Task<IEnumerable<Order>> GetBySellerIdAsync(int sellerId)
        {
            return await _dbSet.Where(o => o.SellerId == sellerId).Include(o => o.Product).Include(o => o.Buyer).Include(o => o.Negotiations).OrderByDescending(o => o.CreateTime).ToListAsync();
        }
        /// <summary>
        /// 根据商品ID获取订单集合
        /// </summary>
        public async Task<IEnumerable<Order>> GetByProductIdAsync(int productId)
        {
            return await _dbSet.Where(o => o.ProductId == productId).Include(o => o.Buyer).Include(o => o.Seller).Include(o => o.Negotiations).OrderByDescending(o => o.CreateTime).ToListAsync();
        }
        /// <summary>
        /// 获取订单总数
        /// </summary>
        public async Task<int> GetTotalOrdersNumberAsync()
        {
            return await _dbSet.CountAsync();
        }
        /// <summary>
        /// 分页多条件查询订单
        /// </summary>
        public async Task<(IEnumerable<Order> Orders, int TotalCount)> GetPagedOrdersAsync(
            int pageIndex,
            int pageSize,
            string? status = null,
            int? buyerId = null,
            int? sellerId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _dbSet.AsQueryable();
            if (!string.IsNullOrEmpty(status)) query = query.Where(o => o.Status == status);
            if (buyerId.HasValue) query = query.Where(o => o.BuyerId == buyerId.Value);
            if (sellerId.HasValue) query = query.Where(o => o.SellerId == sellerId.Value);
            if (startDate.HasValue) query = query.Where(o => o.CreateTime >= startDate.Value);
            if (endDate.HasValue) query = query.Where(o => o.CreateTime <= endDate.Value);
            var totalCount = await query.CountAsync();
            var orders = await query.Include(o => o.Product).Include(o => o.Buyer).Include(o => o.Seller).OrderByDescending(o => o.CreateTime).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return (orders, totalCount);
        }
        /// <summary>
        /// 获取订单详情（包含所有关联信息）
        /// </summary>
        public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
        {
            return await _dbSet.Include(o => o.Product).ThenInclude(p => p.ProductImages)
                .Include(o => o.Buyer)
                .Include(o => o.Seller)
                .Include(o => o.Negotiations)
                .Include(o => o.AbstractOrder).ThenInclude(ao => ao!.Reviews)
                .Include(o => o.AbstractOrder).ThenInclude(ao => ao!.Reports)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }
        /// <summary>
        /// 获取过期的订单
        /// </summary>
        public async Task<IEnumerable<Order>> GetExpiredOrdersAsync()
        {
            return await _dbSet.Where(o => o.ExpireTime.HasValue && o.ExpireTime.Value < DateTime.Now && o.Status == Order.OrderStatus.PendingPayment).Include(o => o.Product).Include(o => o.Buyer).ToListAsync();
        }
        /// <summary>
        /// 获取用户的订单统计（买家/卖家分组）
        /// </summary>
        public async Task<Dictionary<string, int>> GetOrderStatisticsByUserAsync(int userId)
        {
            var buyerStats = await _dbSet.Where(o => o.BuyerId == userId).GroupBy(o => o.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToDictionaryAsync(x => $"买家_{x.Status}", x => x.Count);
            var sellerStats = await _dbSet.Where(o => o.SellerId == userId).GroupBy(o => o.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToDictionaryAsync(x => $"卖家_{x.Status}", x => x.Count);
            var result = new Dictionary<string, int>();
            foreach (var item in buyerStats) result[item.Key] = item.Value;
            foreach (var item in sellerStats) result[item.Key] = item.Value;
            return result;
        }
        /// <summary>
        /// 获取订单总金额统计
        /// </summary>
        public async Task<decimal> GetTotalOrderAmountAsync(string? status = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _dbSet.AsQueryable();
            if (!string.IsNullOrEmpty(status)) query = query.Where(o => o.Status == status);
            if (startDate.HasValue) query = query.Where(o => o.CreateTime >= startDate.Value);
            if (endDate.HasValue) query = query.Where(o => o.CreateTime <= endDate.Value);
            return await query.Where(o => o.TotalAmount.HasValue).SumAsync(o => o.TotalAmount!.Value);
        }
        /// <summary>
        /// 获取热门商品（根据订单数量）
        /// </summary>
        public async Task<IEnumerable<(int ProductId, string ProductTitle, int OrderCount)>> GetPopularProductsAsync(int count)
        {
            return await _dbSet.Include(o => o.Product)
            .GroupBy(o => new { o.ProductId, o.Product!.Title })
            .Select(g => new { g.Key.ProductId, g.Key.Title, Count = g.Count() })
            .OrderByDescending(x => x.Count).Take(count).Select(x => new ValueTuple<int, string, int>(x.ProductId, x.Title, x.Count))
            .ToListAsync();
        }
        #endregion

        #region 更新操作
        /// <summary>
        /// 更新订单状态
        /// </summary>
        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            var order = await GetByPrimaryKeyAsync(orderId);
            if (order == null) return false;
            order.Status = newStatus;
            Update(order);
            return true;
        }
        #endregion
    }
}
