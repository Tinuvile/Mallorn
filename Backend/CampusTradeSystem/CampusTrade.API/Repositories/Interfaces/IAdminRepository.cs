using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 管理员仓储接口（AdminRepository Interface）
    /// </summary>
    public interface IAdminRepository : IRepository<Admin>
    {
        #region 创建操作
        // 暂无特定创建操作，使用基础仓储接口方法
        #endregion

        #region 读取操作
        /// <summary>
        /// 根据用户ID获取管理员
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>管理员实体或null</returns>
        Task<Admin?> GetByUserIdAsync(int userId);
        /// <summary>
        /// 根据角色获取管理员列表
        /// </summary>
        /// <param name="role">角色</param>
        /// <returns>管理员集合</returns>
        Task<IEnumerable<Admin>> GetByRoleAsync(string role);
        /// <summary>
        /// 获取所有分类管理员
        /// </summary>
        /// <returns>管理员集合</returns>
        Task<IEnumerable<Admin>> GetCategoryAdminsAsync();
        /// <summary>
        /// 根据分类ID获取分类管理员
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>管理员实体或null</returns>
        Task<Admin?> GetCategoryAdminByCategoryIdAsync(int categoryId);
        /// <summary>
        /// 获取所有活跃管理员
        /// </summary>
        /// <returns>活跃管理员集合</returns>
        Task<IEnumerable<Admin>> GetActiveAdminsAsync();
        /// <summary>
        /// 获取管理员审计日志
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="startDate">起始时间</param>
        /// <param name="endDate">结束时间</param>
        /// <returns>审计日志集合</returns>
        Task<IEnumerable<AuditLog>> GetAuditLogsByAdminAsync(int adminId, DateTime? startDate = null, DateTime? endDate = null);
        /// <summary>
        /// 获取管理员统计信息
        /// </summary>
        /// <returns>统计信息字典</returns>
        Task<Dictionary<string, int>> GetAdminStatisticsAsync();
        #endregion

        #region 更新操作
        /// <summary>
        /// 创建审计日志
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="actionType">操作类型</param>
        /// <param name="targetId">目标ID</param>
        /// <param name="detail">详情</param>
        Task CreateAuditLogAsync(int adminId, string actionType, int? targetId = null, string? detail = null);
        #endregion

        #region 删除操作
        // 暂无特定删除操作，使用基础仓储接口方法
        #endregion

        #region 关系查询
        // 暂无特定关系查询，使用基础仓储接口方法
        #endregion

        #region 高级查询
        // 暂无特定高级查询，使用基础仓储接口方法
        #endregion
    }
}
