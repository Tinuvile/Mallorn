using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// 工作单元实现类
    /// 实现Repository模式的统一管理和事务控制
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly CampusTradeDbContext _context;
        private IDbContextTransaction? _transaction;

        // Repository实例缓存
        private IUserRepository? _userRepository;
        private IRepository<Student>? _studentRepository;
        private IRefreshTokenRepository? _refreshTokenRepository;
        private ICreditHistoryRepository? _creditHistoryRepository;
        private IRepository<LoginLogs>? _loginLogsRepository;
        private IEmailVerificationRepository? _emailVerificationRepository;
        private ICategoriesRepository? _categoryRepository;
        private IProductRepository? _productRepository;
        private IRepository<ProductImage>? _productImageRepository;
        private IRepository<AbstractOrder>? _abstractOrderRepository;
        private IOrderRepository? _orderRepository;
        private INegotiationsRepository? _negotiationRepository;
        private IExchangeRequestsRepository? _exchangeRequestRepository;
        private IVirtualAccountsRepository? _virtualAccountRepository;
        private IRechargeRecordsRepository? _rechargeRecordRepository;
        private IAdminRepository? _adminRepository;
        private IRepository<AuditLog>? _auditLogRepository;
        private IRepository<NotificationTemplate>? _notificationTemplateRepository;
        private INotificationRepository? _notificationRepository;
        private IReviewsRepository? _reviewRepository;
        private IReportsRepository? _reportsRepository;
        private IRepository<ReportEvidence>? _reportEvidenceRepository;

        public UnitOfWork(CampusTradeDbContext context)
        {
            _context = context;
        }

        #region Repository属性

        public IUserRepository Users => _userRepository ??= new UserRepository(_context);
        public IRepository<Student> Students => _studentRepository ??= new Repository<Student>(_context);
        public IRefreshTokenRepository RefreshTokens => _refreshTokenRepository ??= new RefreshTokenRepository(_context);
        public ICreditHistoryRepository CreditHistory => _creditHistoryRepository ??= new CreditHistoryRepository(_context);
        public IRepository<LoginLogs> LoginLogs => _loginLogsRepository ??= new Repository<LoginLogs>(_context);
        public IEmailVerificationRepository EmailVerifications => _emailVerificationRepository ??= new EmailVerificationRepository(_context);
        public ICategoriesRepository Categories => _categoryRepository ??= new CategoriesRepository(_context);
        public IProductRepository Products => _productRepository ??= new ProductRepository(_context);
        public IRepository<ProductImage> ProductImages => _productImageRepository ??= new Repository<ProductImage>(_context);
        public IRepository<AbstractOrder> AbstractOrders => _abstractOrderRepository ??= new Repository<AbstractOrder>(_context);
        public IOrderRepository Orders => _orderRepository ??= new OrderRepository(_context);
        public INegotiationsRepository Negotiations => _negotiationRepository ??= new NegotiationsRepository(_context);
        public IExchangeRequestsRepository ExchangeRequests => _exchangeRequestRepository ??= new ExchangeRequestsRepository(_context);
        public IVirtualAccountsRepository VirtualAccounts => _virtualAccountRepository ??= new VirtualAccountsRepository(_context);
        public IRechargeRecordsRepository RechargeRecords => _rechargeRecordRepository ??= new RechargeRecordsRepository(_context);
        public IAdminRepository Admins => _adminRepository ??= new AdminRepository(_context);
        public IRepository<AuditLog> AuditLogs => _auditLogRepository ??= new Repository<AuditLog>(_context);
        public IRepository<NotificationTemplate> NotificationTemplates => _notificationTemplateRepository ??= new Repository<NotificationTemplate>(_context);
        public INotificationRepository Notifications => _notificationRepository ??= new NotificationRepository(_context);
        public IReviewsRepository Reviews => _reviewRepository ??= new ReviewsRepository(_context);
        public IReportsRepository Reports => _reportsRepository ??= new ReportsRepository(_context);
        public IRepository<ReportEvidence> ReportEvidences => _reportEvidenceRepository ??= new Repository<ReportEvidence>(_context);

        #endregion

        #region 事务管理

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                await _transaction?.CommitAsync()!;
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            try
            {
                if (_transaction != null)
                {
                    await _transaction.RollbackAsync();
                }
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        #endregion

        #region 批量操作

        public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(string sql, params object[] parameters) where T : class
        {
            return await _context.Set<T>().FromSqlRaw(sql, parameters).ToListAsync();
        }

        public async Task<int> ExecuteCommandAsync(string sql, params object[] parameters)
        {
            return await _context.Database.ExecuteSqlRawAsync(sql, parameters);
        }

        public async Task BulkInsertAsync<T>(IEnumerable<T> entities) where T : class
        {
            await _context.Set<T>().AddRangeAsync(entities);
        }

        public async Task BulkUpdateAsync<T>(IEnumerable<T> entities) where T : class
        {
            _context.Set<T>().UpdateRange(entities);
            await Task.CompletedTask;
        }

        public async Task BulkDeleteAsync<T>(IEnumerable<T> entities) where T : class
        {
            _context.Set<T>().RemoveRange(entities);
            await Task.CompletedTask;
        }

        #endregion

        #region 缓存管理

        public void ClearChangeTracker()
        {
            _context.ChangeTracker.Clear();
        }

        public void DetachEntity<T>(T entity) where T : class
        {
            _context.Entry(entity).State = EntityState.Detached;
        }

        public EntityState GetEntityState<T>(T entity) where T : class
        {
            return _context.Entry(entity).State;
        }

        #endregion

        #region 监控管理

        public int GetPendingChangesCount()
        {
            return _context.ChangeTracker.Entries().Count(e => e.State != EntityState.Unchanged);
        }

        public bool HasPendingChanges()
        {
            return _context.ChangeTracker.HasChanges();
        }

        #endregion

        #region IDisposable

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _context?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
