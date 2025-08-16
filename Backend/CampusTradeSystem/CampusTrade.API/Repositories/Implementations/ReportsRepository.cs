using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// Reports实体的Repository实现类
    /// 继承基础Repository，提供Reports特有的查询和操作方法
    /// </summary>
    public class ReportsRepository : Repository<Reports>, IReportsRepository
    {
        public ReportsRepository(CampusTradeDbContext context) : base(context) { }

        #region 读取操作
        /// <summary>
        /// 根据举报人ID获取举报集合
        /// </summary>
        public async Task<IEnumerable<Reports>> GetByReporterIdAsync(int reporterId)
        {
            return await _dbSet.Where(r => r.ReporterId == reporterId).Include(r => r.AbstractOrder).Include(r => r.Reporter).Include(r => r.Evidences).OrderByDescending(r => r.CreateTime).ToListAsync();
        }
        /// <summary>
        /// 根据订单ID获取举报集合
        /// </summary>
        public async Task<IEnumerable<Reports>> GetByOrderIdAsync(int orderId)
        {
            return await _dbSet.Where(r => r.OrderId == orderId).Include(r => r.Reporter).Include(r => r.Evidences).OrderByDescending(r => r.CreateTime).ToListAsync();
        }
        /// <summary>
        /// 根据状态获取举报集合
        /// </summary>
        public async Task<IEnumerable<Reports>> GetByStatusAsync(string status)
        {
            return await _dbSet.Where(r => r.Status == status).Include(r => r.AbstractOrder).Include(r => r.Reporter).Include(r => r.Evidences).OrderByDescending(r => r.CreateTime).ToListAsync();
        }
        /// <summary>
        /// 获取待处理举报集合
        /// </summary>
        public async Task<IEnumerable<Reports>> GetPendingReportsAsync()
        {
            return await _dbSet.Where(r => r.Status == "待处理").Include(r => r.AbstractOrder).Include(r => r.Reporter).Include(r => r.Evidences).OrderByDescending(r => r.Priority).ThenBy(r => r.CreateTime).ToListAsync();
        }
        /// <summary>
        /// 获取超时未处理举报集合
        /// </summary>
        public async Task<IEnumerable<Reports>> GetOverdueReportsAsync()
        {
            var overdueTime = DateTime.Now.AddHours(-24);
            return await _dbSet.Where(r => r.Status == "待处理" && r.CreateTime < overdueTime).Include(r => r.AbstractOrder).Include(r => r.Reporter).Include(r => r.Evidences).OrderBy(r => r.CreateTime).ToListAsync();
        }
        /// <summary>
        /// 分页获取举报
        /// </summary>
        public async Task<(IEnumerable<Reports> Reports, int TotalCount)> GetPagedReportsAsync(int pageIndex, int pageSize, string? status = null, string? type = null, int? priority = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _dbSet.AsQueryable();
            if (!string.IsNullOrEmpty(status)) query = query.Where(r => r.Status == status);
            if (!string.IsNullOrEmpty(type)) query = query.Where(r => r.Type == type);
            if (priority.HasValue) query = query.Where(r => r.Priority == priority.Value);
            if (startDate.HasValue) query = query.Where(r => r.CreateTime >= startDate.Value);
            if (endDate.HasValue) query = query.Where(r => r.CreateTime <= endDate.Value);
            var totalCount = await query.CountAsync();
            var reports = await query.Include(r => r.AbstractOrder).Include(r => r.Reporter).Include(r => r.Evidences).OrderByDescending(r => r.Priority).ThenByDescending(r => r.CreateTime).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return (reports, totalCount);
        }
        /// <summary>
        /// 获取高优先级举报集合
        /// </summary>
        public async Task<IEnumerable<Reports>> GetHighPriorityReportsAsync()
        {
            return await _dbSet.Where(r => r.Priority >= 7 && r.Status != "已处理" && r.Status != "已关闭").Include(r => r.AbstractOrder).Include(r => r.Reporter).Include(r => r.Evidences).OrderByDescending(r => r.Priority).ThenBy(r => r.CreateTime).ToListAsync();
        }
        /// <summary>
        /// 获取举报详情（包含所有关联信息）
        /// </summary>
        public async Task<Reports?> GetReportWithDetailsAsync(int reportId)
        {
            return await _dbSet.Include(r => r.AbstractOrder).Include(r => r.Reporter).Include(r => r.Evidences).FirstOrDefaultAsync(r => r.ReportId == reportId);
        }
        /// <summary>
        /// 获取举报证据集合
        /// </summary>
        public async Task<IEnumerable<ReportEvidence>> GetReportEvidencesAsync(int reportId)
        {
            return await _context.Set<ReportEvidence>().Where(re => re.ReportId == reportId).OrderBy(re => re.UploadedAt).ToListAsync();
        }
        #endregion

        #region 更新操作
        /// <summary>
        /// 更新举报状态
        /// </summary>
        public async Task<bool> UpdateReportStatusAsync(int reportId, string newStatus)
        {
            var report = await GetByPrimaryKeyAsync(reportId);
            if (report == null) return false;
            report.Status = newStatus;
            Update(report);
            return true;
        }
        /// <summary>
        /// 分配举报优先级
        /// </summary>
        public async Task<bool> AssignPriorityAsync(int reportId, int priority)
        {
            if (priority < 1 || priority > 10) return false;
            var report = await GetByPrimaryKeyAsync(reportId);
            if (report == null) return false;
            report.Priority = priority;
            Update(report);
            return true;
        }
        /// <summary>
        /// 批量更新举报状态
        /// </summary>
        public async Task<int> BulkUpdateReportStatusAsync(List<int> reportIds, string newStatus)
        {
            var reports = await _dbSet.Where(r => reportIds.Contains(r.ReportId)).ToListAsync();
            foreach (var report in reports) report.Status = newStatus;
            UpdateRange(reports);
            return reports.Count;
        }
        /// <summary>
        /// 添加举报证据
        /// </summary>
        public async Task AddReportEvidenceAsync(int reportId, string fileType, string fileUrl)
        {
            var evidence = new ReportEvidence
            {
                ReportId = reportId,
                FileType = fileType,
                FileUrl = fileUrl,
                UploadedAt = DateTime.Now
            };
            await _context.Set<ReportEvidence>().AddAsync(evidence);
        }
        #endregion

        #region 统计操作
        /// <summary>
        /// 获取举报统计信息
        /// </summary>
        public async Task<Dictionary<string, int>> GetReportStatisticsAsync()
        {
            var stats = new Dictionary<string, int>();
            var statusStats = await _dbSet.GroupBy(r => r.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Status, x => x.Count);
            foreach (var stat in statusStats) stats[stat.Key] = stat.Value;
            var typeStats = await _dbSet.GroupBy(r => r.Type).Select(g => new { Type = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Type, x => x.Count);
            foreach (var stat in typeStats) stats[$"类型_{stat.Key}"] = stat.Value;
            var highPriorityCount = await _dbSet.CountAsync(r => r.Priority >= 7);
            stats["高优先级"] = highPriorityCount;
            var overdueCount = await _dbSet.CountAsync(r => r.Status == "待处理" && r.CreateTime < DateTime.Now.AddHours(-24));
            stats["超时未处理"] = overdueCount;
            return stats;
        }
        /// <summary>
        /// 获取指定类型举报数量
        /// </summary>
        public async Task<int> GetReportCountByTypeAsync(string type)
        {
            return await _dbSet.CountAsync(r => r.Type == type);
        }
        /// <summary>
        /// 获取用户举报统计
        /// </summary>
        public async Task<Dictionary<int, int>> GetUserReportStatisticsAsync()
        {
            return await _dbSet.GroupBy(r => r.ReporterId).ToDictionaryAsync(g => g.Key, g => g.Count());
        }
        #endregion
    }
}
