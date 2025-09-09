using System.Collections.Generic;
using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// 虚拟账户管理Repository实现
    /// 提供账户余额管理、交易处理等功能，保证线程安全
    /// </summary>
    public class VirtualAccountsRepository : Repository<VirtualAccount>, IVirtualAccountsRepository
    {
        private readonly ILogger<VirtualAccountsRepository>? _logger;
        private readonly CampusTrade.API.Services.Notification.NotifiService? _notificationService;

        // 向后兼容的构造函数（用于UnitOfWork）
        public VirtualAccountsRepository(CampusTradeDbContext context) : base(context)
        {
            _logger = null;
            _notificationService = null;
        }

        // 完整功能的构造函数（用于依赖注入）
        public VirtualAccountsRepository(CampusTradeDbContext context, ILogger<VirtualAccountsRepository> logger, CampusTrade.API.Services.Notification.NotifiService notificationService) : base(context)
        {
            _logger = logger;
            _notificationService = notificationService;
        }

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
                        await _context.SaveChangesAsync(); // 修复：添加SaveChanges
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
                    // 使用现有事务 - 让外层UnitOfWork负责SaveChanges
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

            // 更新账户余额 - 强化实体追踪
            var originalBalance = account.Balance;
            account.Balance -= amount;

            // 确保EF正确追踪此修改
            _context.Entry(account).Property(x => x.Balance).IsModified = true;
            _context.VirtualAccounts.Update(account);

            // 添加日志以便调试
            Console.WriteLine($"DebitInternalAsync: 用户{userId}, 原余额{originalBalance}, 扣减{amount}, 新余额{account.Balance}");

            // 发送余额变动通知
            if (_notificationService != null)
            {
                try
                {
                    var notificationParams = new Dictionary<string, object>
                    {
                        ["changeType"] = "扣减",
                        ["amount"] = amount.ToString("F2"),
                        ["currentBalance"] = account.Balance.ToString("F2"),
                        ["transactionTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };

                    await _notificationService.CreateNotificationAsync(
                        userId,
                        30, // 账户余额变动模板ID
                        notificationParams
                    );

                    _logger?.LogInformation("账户余额扣减通知已发送，用户ID: {UserId}，扣减金额: {Amount}，当前余额: {Balance}，原因: {Reason}",
                        userId, amount, account.Balance, reason);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "发送账户余额扣减通知失败，用户ID: {UserId}，扣减金额: {Amount}", userId, amount);
                    // 注意：通知发送失败不应该影响余额扣减操作，所以这里只记录日志
                }
            }

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
            bool isNewAccount = false;
            decimal originalBalance = 0;

            if (account == null)
            {
                // 创建新账户
                account = new VirtualAccount { UserId = userId, Balance = amount, CreatedAt = DateTime.UtcNow };
                await AddAsync(account);
                isNewAccount = true;
            }
            else
            {
                // 更新现有账户余额 - 强化实体追踪
                originalBalance = account.Balance;
                account.Balance += amount;

                // 确保EF正确追踪此修改
                _context.Entry(account).Property(x => x.Balance).IsModified = true;
                _context.VirtualAccounts.Update(account);

                // 添加日志以便调试
                Console.WriteLine($"CreditInternalAsync: 用户{userId}, 原余额{originalBalance}, 增加{amount}, 新余额{account.Balance}");
            }

            // 发送余额变动通知
            if (_notificationService != null)
            {
                try
                {
                    // 判断是否为充值操作（通过reason字符串判断）
                    bool isRecharge = reason.Contains("充值") || reason.Contains("模拟充值");
                    int templateId = isRecharge ? 31 : 30; // 充值成功通知或账户余额变动

                    if (isRecharge)
                    {
                        // 充值成功通知
                        var rechargeParams = new Dictionary<string, object>
                        {
                            ["amount"] = amount.ToString("F2"),
                            ["currentBalance"] = account.Balance.ToString("F2"),
                            ["transactionId"] = reason.Contains("-") ? reason.Split('-').LastOrDefault()?.Trim() ?? "未知" : "未知"
                        };

                        await _notificationService.CreateNotificationAsync(
                            userId,
                            templateId,
                            rechargeParams
                        );

                        _logger?.LogInformation("充值成功通知已发送，用户ID: {UserId}，充值金额: {Amount}，当前余额: {Balance}",
                            userId, amount, account.Balance);
                    }
                    else
                    {
                        // 普通余额变动通知
                        var notificationParams = new Dictionary<string, object>
                        {
                            ["changeType"] = "增加",
                            ["amount"] = amount.ToString("F2"),
                            ["currentBalance"] = account.Balance.ToString("F2"),
                            ["transactionTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };

                        await _notificationService.CreateNotificationAsync(
                            userId,
                            templateId,
                            notificationParams
                        );

                        _logger?.LogInformation("账户余额增加通知已发送，用户ID: {UserId}，增加金额: {Amount}，当前余额: {Balance}，原因: {Reason}",
                            userId, amount, account.Balance, reason);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "发送账户余额增加通知失败，用户ID: {UserId}，增加金额: {Amount}", userId, amount);
                    // 注意：通知发送失败不应该影响余额增加操作，所以这里只记录日志
                }
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
