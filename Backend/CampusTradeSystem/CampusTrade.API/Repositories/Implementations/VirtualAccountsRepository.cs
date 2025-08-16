using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// 虚拟账户管理Repository实现
    /// 提供账户余额管理、交易处理等功能，保证线程安全
    /// </summary>
    public class VirtualAccountsRepository : Repository<VirtualAccount>, IVirtualAccountsRepository
    {
        public VirtualAccountsRepository(CampusTradeDbContext context) : base(context) { }

        #region 读取操作
        /// <summary>
        /// 根据用户ID获取虚拟账户
        /// </summary>
        public async Task<VirtualAccount?> GetByUserIdAsync(int userId)
        {
            return await _context.VirtualAccounts.FirstOrDefaultAsync(va => va.UserId == userId);
        }
        /// <summary>
        /// 获取用户余额
        /// </summary>
        public async Task<decimal> GetBalanceAsync(int userId)
        {
            var account = await GetByUserIdAsync(userId);
            return account?.Balance ?? 0;
        }
        /// <summary>
        /// 检查余额是否充足
        /// </summary>
        public async Task<bool> HasSufficientBalanceAsync(int userId, decimal amount)
        {
            var balance = await GetBalanceAsync(userId);
            return balance >= amount;
        }
        /// <summary>
        /// 获取系统总余额
        /// </summary>
        public async Task<decimal> GetTotalSystemBalanceAsync()
        {
            return await _context.VirtualAccounts.SumAsync(va => va.Balance);
        }
        /// <summary>
        /// 获取余额大于指定值的账户
        /// </summary>
        public async Task<IEnumerable<VirtualAccount>> GetAccountsWithBalanceAboveAsync(decimal minBalance)
        {
            return await _context.VirtualAccounts.Include(va => va.User).Where(va => va.Balance >= minBalance).OrderByDescending(va => va.Balance).ToListAsync();
        }
        /// <summary>
        /// 获取余额排名前N的账户
        /// </summary>
        public async Task<IEnumerable<VirtualAccount>> GetTopBalanceAccountsAsync(int count)
        {
            return await _context.VirtualAccounts.Include(va => va.User).OrderByDescending(va => va.Balance).Take(count).ToListAsync();
        }
        /// <summary>
        /// 根据用户ID集合批量获取账户
        /// </summary>
        public async Task<IEnumerable<VirtualAccount>> GetAccountsByUserIdsAsync(IEnumerable<int> userIds)
        {
            return await _context.VirtualAccounts.Include(va => va.User).Where(va => userIds.Contains(va.UserId)).ToListAsync();
        }
        #endregion

        #region 更新操作
        /// <summary>
        /// 扣减余额
        /// </summary>
        public async Task<bool> DebitAsync(int userId, decimal amount, string reason)
        {
            if (amount <= 0) return false;
            try
            {
                // 检查是否已有活动事务，如果有则不创建新事务
                var hasActiveTransaction = _context.Database.CurrentTransaction != null;

                if (!hasActiveTransaction)
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    var result = await DebitInternalAsync(userId, amount, reason);
                    if (result)
                    {
                        await transaction.CommitAsync();
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                    }
                    return result;
                }
                else
                {
                    // 使用现有事务
                    return await DebitInternalAsync(userId, amount, reason);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 内部扣减余额方法（不管理事务）
        /// </summary>
        private async Task<bool> DebitInternalAsync(int userId, decimal amount, string reason)
        {
            var account = await GetByUserIdAsync(userId);
            if (account == null || account.Balance < amount)
            {
                return false;
            }
            account.Balance -= amount;
            _context.VirtualAccounts.Update(account);
            return true;
        }
        /// <summary>
        /// 增加余额（线程安全）
        /// </summary>
        public async Task<bool> CreditAsync(int userId, decimal amount, string reason)
        {
            if (amount <= 0) return false;
            try
            {
                // 检查是否已有活动事务，如果有则不创建新事务
                var hasActiveTransaction = _context.Database.CurrentTransaction != null;

                if (!hasActiveTransaction)
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    var result = await CreditInternalAsync(userId, amount, reason);
                    if (result)
                    {
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                    }
                    return result;
                }
                else
                {
                    // 使用现有事务
                    return await CreditInternalAsync(userId, amount, reason);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 内部增加余额方法（不管理事务）
        /// </summary>
        private async Task<bool> CreditInternalAsync(int userId, decimal amount, string reason)
        {
            var account = await GetByUserIdAsync(userId);
            if (account == null)
            {
                account = new VirtualAccount { UserId = userId, Balance = amount, CreatedAt = DateTime.UtcNow };
                await AddAsync(account);
            }
            else
            {
                account.Balance += amount;
                _context.VirtualAccounts.Update(account);
            }
            return true;
        }
        /// <summary>
        /// 批量更新余额
        /// </summary>
        public async Task<bool> BatchUpdateBalancesAsync(Dictionary<int, decimal> balanceChanges)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                foreach (var change in balanceChanges)
                {
                    var userId = change.Key;
                    var amount = change.Value;
                    if (amount > 0)
                    {
                        await CreditAsync(userId, amount, "批量更新");
                    }
                    else if (amount < 0)
                    {
                        var success = await DebitAsync(userId, Math.Abs(amount), "批量更新");
                        if (!success)
                        {
                            await transaction.RollbackAsync();
                            return false;
                        }
                    }
                }
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}
