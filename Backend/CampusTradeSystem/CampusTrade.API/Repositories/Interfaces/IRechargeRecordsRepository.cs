using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 充值记录管理仓储接口（RechargeRecordsRepository Interface）
    /// </summary>
    public interface IRechargeRecordsRepository : IRepository<RechargeRecord>
    {
        #region 创建操作
        // 暂无特定创建操作，使用基础仓储接口方法
        #endregion

        #region 读取操作
        /// <summary>
        /// 分页获取用户充值记录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <returns>充值记录集合及总数</returns>
        Task<(IEnumerable<RechargeRecord> Records, int TotalCount)> GetByUserIdAsync(int userId, int pageIndex = 0, int pageSize = 10);
        /// <summary>
        /// 获取用户待处理充值记录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>充值记录集合</returns>
        Task<IEnumerable<RechargeRecord>> GetPendingRechargesAsync(int userId);
        /// <summary>
        /// 获取用户充值总额
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>充值总额</returns>
        Task<decimal> GetTotalRechargeAmountByUserAsync(int userId);
        /// <summary>
        /// 根据状态获取充值记录
        /// </summary>
        /// <param name="status">充值状态</param>
        /// <returns>充值记录集合</returns>
        Task<IEnumerable<RechargeRecord>> GetRecordsByStatusAsync(string status);
        /// <summary>
        /// 获取已过期充值记录
        /// </summary>
        /// <param name="expiration">过期时长</param>
        /// <returns>充值记录集合</returns>
        Task<IEnumerable<RechargeRecord>> GetExpiredRechargesAsync(TimeSpan expiration);
        /// <summary>
        /// 获取指定时间段内充值总额
        /// </summary>
        /// <param name="startDate">起始时间</param>
        /// <param name="endDate">结束时间</param>
        /// <returns>充值总额</returns>
        Task<decimal> GetTotalRechargeAmountAsync(DateTime? startDate = null, DateTime? endDate = null);
        /// <summary>
        /// 获取指定时间段内充值次数
        /// </summary>
        /// <param name="startDate">起始时间</param>
        /// <param name="endDate">结束时间</param>
        /// <returns>充值次数</returns>
        Task<int> GetRechargeCountAsync(DateTime? startDate = null, DateTime? endDate = null);
        /// <summary>
        /// 获取充值状态统计
        /// </summary>
        /// <returns>状态-数量字典</returns>
        Task<Dictionary<string, int>> GetRechargeStatusStatisticsAsync();
        /// <summary>
        /// 获取每日充值统计
        /// </summary>
        /// <param name="startDate">起始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>每日统计集合</returns>
        Task<IEnumerable<dynamic>> GetDailyRechargeStatisticsAsync(DateTime startDate, DateTime endDate);
        /// <summary>
        /// 获取大额充值记录
        /// </summary>
        /// <param name="minAmount">最小金额</param>
        /// <returns>充值记录集合</returns>
        Task<IEnumerable<RechargeRecord>> GetLargeAmountRechargesAsync(decimal minAmount);
        /// <summary>
        /// 获取频繁充值记录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="timeSpan">时间范围</param>
        /// <param name="minCount">最小次数</param>
        /// <returns>充值记录集合</returns>
        Task<IEnumerable<RechargeRecord>> GetFrequentRechargesAsync(int userId, TimeSpan timeSpan, int minCount);
        #endregion

        #region 更新操作
        /// <summary>
        /// 更新充值状态
        /// </summary>
        /// <param name="rechargeId">充值ID</param>
        /// <param name="status">新状态</param>
        /// <param name="completeTime">完成时间</param>
        /// <returns>是否成功</returns>
        Task<bool> UpdateRechargeStatusAsync(int rechargeId, string status, DateTime? completeTime = null);
        #endregion

        #region 删除操作
        // 暂无特定删除操作，使用基础仓储接口方法
        #endregion

        #region 关系查询
        /// <summary>
        /// 判断用户在指定时间段内是否有失败充值
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="timeSpan">时间范围</param>
        /// <param name="maxFailures">最大失败次数</param>
        /// <returns>是否有失败充值</returns>
        Task<bool> HasRecentFailedRechargesAsync(int userId, TimeSpan timeSpan, int maxFailures);
        #endregion
    }
}
