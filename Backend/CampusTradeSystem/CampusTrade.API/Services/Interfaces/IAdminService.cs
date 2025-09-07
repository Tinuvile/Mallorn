using CampusTrade.API.Models.DTOs.Admin;
using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Services.Interfaces
{
    /// <summary>
    /// 管理员服务接口
    /// </summary>
    public interface IAdminService
    {
        #region 管理员管理
        /// <summary>
        /// 创建管理员
        /// </summary>
        /// <param name="createDto">创建管理员DTO</param>
        /// <param name="operatorAdminId">操作员管理员ID</param>
        /// <returns>创建结果</returns>
        Task<(bool Success, string Message, int? AdminId)> CreateAdminAsync(CreateAdminDto createDto, int operatorAdminId);

        /// <summary>
        /// 通过用户名创建管理员
        /// </summary>
        /// <param name="createDto">创建管理员DTO</param>
        /// <param name="operatorAdminId">操作员管理员ID</param>
        /// <returns>创建结果</returns>
        Task<(bool Success, string Message, int? AdminId)> CreateAdminByUsernameAsync(CreateAdminByUsernameDto createDto, int operatorAdminId);

        /// <summary>
        /// 更新管理员信息
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="updateDto">更新DTO</param>
        /// <param name="operatorAdminId">操作员管理员ID</param>
        /// <returns>更新结果</returns>
        Task<(bool Success, string Message)> UpdateAdminAsync(int adminId, UpdateAdminDto updateDto, int operatorAdminId);

        /// <summary>
        /// 删除管理员
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="operatorAdminId">操作员管理员ID</param>
        /// <returns>删除结果</returns>
        Task<(bool Success, string Message)> DeleteAdminAsync(int adminId, int operatorAdminId);

        /// <summary>
        /// 获取管理员详情
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <returns>管理员详情</returns>
        Task<AdminResponseDto?> GetAdminDetailsAsync(int adminId);

        /// <summary>
        /// 分页获取管理员列表
        /// </summary>
        /// <param name="pageIndex">页索引</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="role">角色筛选</param>
        /// <param name="searchKeyword">搜索关键字</param>
        /// <returns>管理员列表</returns>
        Task<(IEnumerable<AdminResponseDto> Admins, int TotalCount)> GetAdminsAsync(
            int pageIndex,
            int pageSize,
            string? role = null,
            string? searchKeyword = null);

        /// <summary>
        /// 根据用户ID获取管理员信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>管理员信息</returns>
        Task<AdminResponseDto?> GetAdminByUserIdAsync(int userId);
        #endregion

        #region 举报处理
        /// <summary>
        /// 处理举报
        /// </summary>
        /// <param name="reportId">举报ID</param>
        /// <param name="handleDto">处理DTO</param>
        /// <param name="adminId">管理员ID</param>
        /// <returns>处理结果</returns>
        Task<(bool Success, string Message)> HandleReportAsync(int reportId, HandleReportDto handleDto, int adminId);

        /// <summary>
        /// 获取管理员负责的举报列表
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="pageIndex">页索引</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="status">状态筛选</param>
        /// <returns>举报列表</returns>
        Task<(IEnumerable<Reports> Reports, int TotalCount)> GetAdminReportsAsync(
            int adminId,
            int pageIndex,
            int pageSize,
            string? status = null);

        /// <summary>
        /// 获取举报详情（管理员专用）
        /// </summary>
        /// <param name="reportId">举报ID</param>
        /// <param name="adminId">管理员ID</param>
        /// <returns>举报详情</returns>
        Task<object?> GetReportDetailAsync(int reportId, int adminId);

        /// <summary>
        /// 获取举报审核历史
        /// </summary>
        /// <param name="reportId">举报ID</param>
        /// <param name="adminId">管理员ID</param>
        /// <returns>审核历史列表</returns>
        Task<IEnumerable<object>?> GetReportAuditHistoryAsync(int reportId, int adminId);
        #endregion

        #region 权限验证
        /// <summary>
        /// 验证管理员权限
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="requiredRole">所需角色</param>
        /// <returns>是否有权限</returns>
        Task<bool> ValidateAdminPermissionAsync(int adminId, string requiredRole);

        /// <summary>
        /// 验证分类管理权限
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="categoryId">分类ID</param>
        /// <returns>是否有权限</returns>
        Task<bool> ValidateCategoryPermissionAsync(int adminId, int categoryId);

        /// <summary>
        /// 验证举报处理权限
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="reportId">举报ID</param>
        /// <returns>是否有权限</returns>
        Task<bool> ValidateReportPermissionAsync(int adminId, int reportId);
        #endregion

        #region 审计日志
        /// <summary>
        /// 获取管理员操作日志
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="pageIndex">页索引</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="startDate">起始时间</param>
        /// <param name="endDate">结束时间</param>
        /// <returns>操作日志列表</returns>
        Task<(IEnumerable<AuditLogResponseDto> Logs, int TotalCount)> GetAdminAuditLogsAsync(
            int adminId,
            int pageIndex,
            int pageSize,
            DateTime? startDate = null,
            DateTime? endDate = null);

        /// <summary>
        /// 获取所有审计日志（仅系统管理员）
        /// </summary>
        /// <param name="requestAdminId">请求的管理员ID</param>
        /// <param name="pageIndex">页索引</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="targetAdminId">目标管理员ID筛选</param>
        /// <param name="actionType">操作类型筛选</param>
        /// <param name="categoryId">分类ID（筛选超级管理员和该分类的模块管理员）</param>
        /// <param name="startDate">起始时间</param>
        /// <param name="endDate">结束时间</param>
        /// <returns>审计日志列表</returns>
        Task<(IEnumerable<AuditLogResponseDto> Logs, int TotalCount)> GetAllAuditLogsAsync(
            int requestAdminId,
            int pageIndex,
            int pageSize,
            int? targetAdminId = null,
            string? actionType = null,
            int? categoryId = null,
            DateTime? startDate = null,
            DateTime? endDate = null);

        /// <summary>
        /// 记录管理员操作
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="actionType">操作类型</param>
        /// <param name="targetId">目标ID</param>
        /// <param name="detail">操作详情</param>
        /// <returns>操作结果</returns>
        Task<bool> LogAdminActionAsync(int adminId, string actionType, int? targetId = null, string? detail = null);
        #endregion

        #region 商品管理
        /// <summary>
        /// 获取管理员可管理的商品列表
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="queryDto">查询条件</param>
        /// <returns>商品列表</returns>
        Task<(IEnumerable<Models.DTOs.Product.ProductListDto> Products, int TotalCount)> GetManagedProductsAsync(
            int adminId,
            AdminProductQueryDto queryDto);

        /// <summary>
        /// 获取商品详情（管理员视角）
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="productId">商品ID</param>
        /// <returns>商品详情</returns>
        Task<AdminProductDetailDto?> GetProductDetailForAdminAsync(int adminId, int productId);

        /// <summary>
        /// 更新商品信息（管理员操作）
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="productId">商品ID</param>
        /// <param name="updateDto">更新DTO</param>
        /// <returns>更新结果</returns>
        Task<(bool Success, string Message)> UpdateProductAsAdminAsync(int adminId, int productId, AdminUpdateProductDto updateDto);

        /// <summary>
        /// 删除商品（管理员操作）
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="productId">商品ID</param>
        /// <param name="reason">删除原因</param>
        /// <returns>删除结果</returns>
        Task<(bool Success, string Message)> DeleteProductAsAdminAsync(int adminId, int productId, string? reason = null);

        /// <summary>
        /// 批量操作商品
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="batchDto">批量操作DTO</param>
        /// <returns>操作结果</returns>
        Task<(bool Success, string Message, Dictionary<int, string> FailedProducts)> BatchOperateProductsAsync(
            int adminId,
            BatchProductOperationDto batchDto);

        /// <summary>
        /// 验证管理员对商品的管理权限
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <param name="productId">商品ID</param>
        /// <returns>是否有权限</returns>
        Task<bool> ValidateProductPermissionAsync(int adminId, int productId);

        /// <summary>
        /// 获取管理员可管理的分类ID列表
        /// </summary>
        /// <param name="adminId">管理员ID</param>
        /// <returns>分类ID列表</returns>
        Task<List<int>> GetManagedCategoryIdsAsync(int adminId);
        #endregion

        #region 统计信息
        /// <summary>
        /// 获取管理员统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        Task<Dictionary<string, object>> GetAdminStatisticsAsync();
        #endregion
    }
}
