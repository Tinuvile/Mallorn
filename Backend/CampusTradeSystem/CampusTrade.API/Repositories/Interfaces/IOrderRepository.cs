using CampusTrade.API.Models.DTOs;
using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// Order实体的Repository接口
    /// 继承基础IRepository，提供Order特有的查询和操作方法
    /// </summary>
    public interface IOrderRepository : IRepository<Order>
    {
        #region 创建操作
        // 订单创建由基础仓储 AddAsync 提供
        #endregion

        #region 读取操作
        /// <summary>
        /// 根据买家ID获取订单集合
        /// </summary>
        Task<IEnumerable<Order>> GetByBuyerIdAsync(int buyerId);

        /// <summary>
        /// 根据卖家ID获取订单集合
        /// </summary>
        Task<IEnumerable<Order>> GetBySellerIdAsync(int sellerId);

        /// <summary>
        /// 根据商品ID获取订单集合
        /// </summary>
        Task<IEnumerable<Order>> GetByProductIdAsync(int productId);

        /// <summary>
        /// 获取订单总数
        /// </summary>
        Task<int> GetTotalOrdersNumberAsync();

        /// <summary>
        /// 分页多条件查询订单
        /// </summary>
        Task<(IEnumerable<Order> Orders, int TotalCount)> GetPagedOrdersAsync(
            int pageIndex,
            int pageSize,
            string? status = null,
            int? buyerId = null,
            int? sellerId = null,
            DateTime? startDate = null,
            DateTime? endDate = null);

        /// <summary>
        /// 获取订单详情（包含所有关联信息）
        /// </summary>
        Task<Order?> GetOrderWithDetailsAsync(int orderId);

        /// <summary>
        /// 获取过期的订单
        /// </summary>
        Task<IEnumerable<Order>> GetExpiredOrdersAsync();

        /// <summary>
        /// 获取即将过期的订单
        /// </summary>
        /// <param name="cutoffTime">截止时间</param>
        /// <returns>即将过期的订单列表</returns>
        Task<IEnumerable<Order>> GetExpiringOrdersAsync(DateTime cutoffTime);
        /// <summary>
        /// 获取用户的订单统计（买家/卖家分组）
        /// </summary>
        Task<Dictionary<string, int>> GetOrderStatisticsByUserAsync(int userId);

        /// <summary>
        /// 获取订单总金额统计
        /// </summary>
        Task<decimal> GetTotalOrderAmountAsync(string? status = null, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// 获取热门商品（根据订单数量）
        /// </summary>
        Task<List<PopularProductDto>> GetPopularProductsAsync(int count);

        /// <summary>
        /// 获取月度交易数据
        /// </summary>
        Task<List<MonthlyTransactionDto>> GetMonthlyTransactionsAsync(int year);
        #endregion

        #region 更新操作
        /// <summary>
        /// 更新订单状态
        /// </summary>
        Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus);
        #endregion

        #region 删除操作
        // 订单删除由基础仓储 Delete 提供
        #endregion

        #region 关系查询
        // 订单与买家、卖家、商品、议价等关联由 GetOrderWithDetailsAsync 提供
        #endregion

        #region 高级查询
        // 预留扩展
        #endregion
    }
}
