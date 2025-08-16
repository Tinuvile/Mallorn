using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 工作单元接口
    /// 实现Repository模式的统一管理和事务控制
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        #region Repository属性

        // 用户管理相关
        IUserRepository Users { get; }
        IRepository<Student> Students { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        ICreditHistoryRepository CreditHistory { get; }
        IRepository<LoginLogs> LoginLogs { get; }
        IEmailVerificationRepository EmailVerifications { get; }

        // 商品管理相关
        ICategoriesRepository Categories { get; }
        IProductRepository Products { get; }
        IRepository<ProductImage> ProductImages { get; }

        // 订单管理相关
        IRepository<AbstractOrder> AbstractOrders { get; }
        IOrderRepository Orders { get; }
        INegotiationsRepository Negotiations { get; }
        IExchangeRequestsRepository ExchangeRequests { get; }

        // 财务管理相关
        IVirtualAccountsRepository VirtualAccounts { get; }
        IRechargeRecordsRepository RechargeRecords { get; }

        // 管理员相关
        IAdminRepository Admins { get; }
        IRepository<AuditLog> AuditLogs { get; }

        // 通知系统相关
        IRepository<NotificationTemplate> NotificationTemplates { get; }
        INotificationRepository Notifications { get; }

        // 评价和举报相关
        IReviewsRepository Reviews { get; }
        IReportsRepository Reports { get; }
        IRepository<ReportEvidence> ReportEvidences { get; }

        #endregion

        #region 事务管理

        /// <summary>
        /// 保存所有更改
        /// </summary>
        /// <returns>受影响的行数</returns>
        Task<int> SaveChangesAsync();

        /// <summary>
        /// 开始事务
        /// </summary>
        /// <returns>事务对象</returns>
        Task BeginTransactionAsync();

        /// <summary>
        /// 提交事务
        /// </summary>
        Task CommitTransactionAsync();

        /// <summary>
        /// 回滚事务
        /// </summary>
        Task RollbackTransactionAsync();

        #endregion

        #region 批量操作

        /// <summary>
        /// 执行原生SQL查询
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="parameters">参数</param>
        /// <returns>查询结果</returns>
        Task<IEnumerable<T>> ExecuteQueryAsync<T>(string sql, params object[] parameters) where T : class;

        /// <summary>
        /// 执行原生SQL命令
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="parameters">参数</param>
        /// <returns>受影响的行数</returns>
        Task<int> ExecuteCommandAsync(string sql, params object[] parameters);

        /// <summary>
        /// 批量插入数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">实体集合</param>
        Task BulkInsertAsync<T>(IEnumerable<T> entities) where T : class;

        /// <summary>
        /// 批量更新数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">实体集合</param>
        Task BulkUpdateAsync<T>(IEnumerable<T> entities) where T : class;

        /// <summary>
        /// 批量删除数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">实体集合</param>
        Task BulkDeleteAsync<T>(IEnumerable<T> entities) where T : class;

        #endregion

        #region 缓存管理

        /// <summary>
        /// 清除实体框架缓存
        /// </summary>
        void ClearChangeTracker();

        /// <summary>
        /// 分离实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体对象</param>
        void DetachEntity<T>(T entity) where T : class;

        /// <summary>
        /// 获取实体状态
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体对象</param>
        /// <returns>实体状态</returns>
        Microsoft.EntityFrameworkCore.EntityState GetEntityState<T>(T entity) where T : class;

        #endregion

        #region 监控管理

        /// <summary>
        /// 获取待处理的更改数量
        /// </summary>
        /// <returns>更改数量</returns>
        int GetPendingChangesCount();

        /// <summary>
        /// 检查是否有待处理的更改
        /// </summary>
        /// <returns>是否有更改</returns>
        bool HasPendingChanges();

        #endregion
    }
}
