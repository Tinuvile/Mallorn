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
    /// 充值记录管理仓储实现类（RechargeRecordsRepository Implementation）
    /// </summary>
    public class RechargeRecordsRepository : Repository<RechargeRecord>, IRechargeRecordsRepository
    {
        public RechargeRecordsRepository(CampusTradeDbContext context) : base(context) { }

        #region 读取操作
        /// <summary>
        /// 分页获取用户充值记录
        /// </summary>
        public async Task<(IEnumerable<RechargeRecord> Records, int TotalCount)> GetByUserIdAsync(int userId, int pageIndex = 0, int pageSize = 10)
        {
            var query = _context.RechargeRecords
                .Include(r => r.User)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreateTime);
            var totalCount = await query.CountAsync();
            var records = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (records, totalCount);
        }

        /// <summary>
        /// 获取用户待处理充值记录
        /// </summary>
        public async Task<IEnumerable<RechargeRecord>> GetPendingRechargesAsync(int userId)
        {
            return await _context.RechargeRecords
                .Include(r => r.User)
                .Where(r => r.UserId == userId && r.Status == "处理中")
                .OrderBy(r => r.CreateTime)
                .ToListAsync();
        }

        /// <summary>
        /// 获取用户充值总额
        /// </summary>
        public async Task<decimal> GetTotalRechargeAmountByUserAsync(int userId)
        {
            return await _context.RechargeRecords
                .Where(r => r.UserId == userId && r.Status == "成功")
                .SumAsync(r => r.Amount);
        }

        /// <summary>
        /// 根据状态获取充值记录
        /// </summary>
        public async Task<IEnumerable<RechargeRecord>> GetRecordsByStatusAsync(string status)
        {
            return await _context.RechargeRecords
                .Include(r => r.User)
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.CreateTime)
                .ToListAsync();
        }

        /// <summary>
        /// 获取已过期充值记录
        /// </summary>
        public async Task<IEnumerable<RechargeRecord>> GetExpiredRechargesAsync(TimeSpan expiration)
        {
            var cutoffTime = DateTime.UtcNow - expiration;
            return await _context.RechargeRecords
                .Where(r => r.Status == "处理中" && r.CreateTime < cutoffTime)
                .OrderBy(r => r.CreateTime)
                .ToListAsync();
        }

        /// <summary>
        /// 获取指定时间段内充值总额
        /// </summary>
        public async Task<decimal> GetTotalRechargeAmountAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.RechargeRecords.Where(r => r.Status == "成功");
            if (startDate.HasValue)
                query = query.Where(r => r.CreateTime >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(r => r.CreateTime <= endDate.Value);
            return await query.SumAsync(r => r.Amount);
        }

        /// <summary>
        /// 获取指定时间段内充值次数
        /// </summary>
        public async Task<int> GetRechargeCountAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.RechargeRecords.AsQueryable();
            if (startDate.HasValue)
                query = query.Where(r => r.CreateTime >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(r => r.CreateTime <= endDate.Value);
            return await query.CountAsync();
        }

        /// <summary>
        /// 获取充值状态统计
        /// </summary>
        public async Task<Dictionary<string, int>> GetRechargeStatusStatisticsAsync()
        {
            return await _context.RechargeRecords
                .GroupBy(r => r.Status)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// 获取每日充值统计
        /// </summary>
        public async Task<IEnumerable<dynamic>> GetDailyRechargeStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.RechargeRecords
                .Where(r => r.CreateTime >= startDate && r.CreateTime <= endDate)
                .GroupBy(r => new { Date = r.CreateTime.Date, r.Status })
                .Select(g => new
                {
                    Date = g.Key.Date,
                    Status = g.Key.Status,
                    Count = g.Count(),
                    Amount = g.Sum(r => r.Amount)
                })
                .OrderBy(x => x.Date)
                .ToListAsync<dynamic>();
        }

        /// <summary>
        /// 获取大额充值记录
        /// </summary>
        public async Task<IEnumerable<RechargeRecord>> GetLargeAmountRechargesAsync(decimal minAmount)
        {
            return await _context.RechargeRecords
                .Include(r => r.User)
                .Where(r => r.Amount >= minAmount)
                .OrderByDescending(r => r.Amount)
                .ToListAsync();
        }

        /// <summary>
        /// 获取频繁充值记录
        /// </summary>
        public async Task<IEnumerable<RechargeRecord>> GetFrequentRechargesAsync(int userId, TimeSpan timeSpan, int minCount)
        {
            var cutoffTime = DateTime.UtcNow - timeSpan;
            var recharges = await _context.RechargeRecords
                .Where(r => r.UserId == userId && r.CreateTime >= cutoffTime)
                .OrderByDescending(r => r.CreateTime)
                .ToListAsync();
            return recharges.Count >= minCount ? recharges : new List<RechargeRecord>();
        }
        #endregion

        #region 更新操作
        /// <summary>
        /// 更新充值状态
        /// </summary>
        public async Task<bool> UpdateRechargeStatusAsync(int rechargeId, string status, DateTime? completeTime = null)
        {
            var record = await GetByPrimaryKeyAsync(rechargeId);
            if (record == null) return false;
            record.Status = status;
            if (completeTime.HasValue)
            {
                record.CompleteTime = completeTime.Value;
            }
            else if (status == "成功" || status == "失败")
            {
                record.CompleteTime = DateTime.UtcNow;
            }
            Update(record);
            return true;
        }
        #endregion

        #region 关系查询
        /// <summary>
        /// 判断用户在指定时间段内是否有失败充值
        /// </summary>
        public async Task<bool> HasRecentFailedRechargesAsync(int userId, TimeSpan timeSpan, int maxFailures)
        {
            var cutoffTime = DateTime.UtcNow - timeSpan;
            var failedCount = await _context.RechargeRecords
                .CountAsync(r => r.UserId == userId && r.Status == "失败" && r.CreateTime >= cutoffTime);
            return failedCount >= maxFailures;
        }
        #endregion
    }
}
