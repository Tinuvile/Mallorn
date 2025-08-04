using System.Collections.Generic;
using System.Threading.Tasks;
using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 议价管理仓储接口（NegotiationsRepository Interface）
    /// </summary>
    public interface INegotiationsRepository : IRepository<Negotiation>
    {
        #region 创建操作
        // 暂无特定创建操作，使用基础仓储接口方法
        #endregion

        #region 读取操作
        /// <summary>
        /// 根据订单ID获取议价记录
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>议价集合</returns>
        Task<IEnumerable<Negotiation>> GetByOrderIdAsync(int orderId);
        /// <summary>
        /// 获取订单最新议价
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>议价实体或null</returns>
        Task<Negotiation?> GetLatestNegotiationAsync(int orderId);
        /// <summary>
        /// 获取用户待处理议价
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>议价集合</returns>
        Task<IEnumerable<Negotiation>> GetPendingNegotiationsAsync(int userId);
        /// <summary>
        /// 根据状态获取议价集合
        /// </summary>
        /// <param name="status">议价状态</param>
        /// <returns>议价集合</returns>
        Task<IEnumerable<Negotiation>> GetNegotiationsByStatusAsync(string status);
        /// <summary>
        /// 获取订单议价历史
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>议价集合</returns>
        Task<IEnumerable<Negotiation>> GetNegotiationHistoryAsync(int orderId);
        /// <summary>
        /// 获取订单议价次数
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>议价次数</returns>
        Task<int> GetNegotiationCountByOrderAsync(int orderId);
        /// <summary>
        /// 获取指定天数内的最新议价
        /// </summary>
        /// <param name="days">天数</param>
        /// <returns>议价集合</returns>
        Task<IEnumerable<Negotiation>> GetRecentNegotiationsAsync(int days = 7);
        #endregion

        #region 更新操作
        /// <summary>
        /// 更新议价状态
        /// </summary>
        /// <param name="negotiationId">议价ID</param>
        /// <param name="status">新状态</param>
        /// <returns>是否成功</returns>
        Task<bool> UpdateNegotiationStatusAsync(int negotiationId, string status);
        #endregion

        #region 删除操作
        // 暂无特定删除操作，使用基础仓储接口方法
        #endregion

        #region 关系查询
        /// <summary>
        /// 判断订单是否有活跃议价
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>是否有活跃议价</returns>
        Task<bool> HasActiveNegotiationAsync(int orderId);
        #endregion

        #region 高级查询
        /// <summary>
        /// 获取议价平均折扣率
        /// </summary>
        /// <returns>平均折扣率</returns>
        Task<decimal> GetAverageNegotiationRateAsync();
        /// <summary>
        /// 获取成功议价总数
        /// </summary>
        /// <returns>成功议价数</returns>
        Task<int> GetSuccessfulNegotiationCountAsync();
        /// <summary>
        /// 获取议价统计信息
        /// </summary>
        /// <returns>统计信息集合</returns>
        Task<IEnumerable<dynamic>> GetNegotiationStatisticsAsync();
        #endregion
    }
}
