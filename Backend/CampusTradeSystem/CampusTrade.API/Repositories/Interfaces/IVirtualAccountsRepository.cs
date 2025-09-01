using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 虚拟账户管理Repository接口
    /// 提供账户余额管理、交易处理等功能
    /// </summary>
    public interface IVirtualAccountsRepository : IRepository<VirtualAccount>
    {
        #region 创建操作
        // 虚拟账户创建由基础仓储 AddAsync 提供
        #endregion

        #region 读取操作
        /// <summary>
        /// 根据用户ID获取虚拟账户
        /// </summary>
        Task<VirtualAccount?> GetByUserIdAsync(int userId);
        /// <summary>
        /// 获取用户余额
        /// </summary>
        Task<decimal> GetBalanceAsync(int userId);
        /// <summary>
        /// 检查余额是否充足
        /// </summary>
        Task<bool> HasSufficientBalanceAsync(int userId, decimal amount);
        /// <summary>
        /// 获取系统总余额
        /// </summary>
        Task<decimal> GetTotalSystemBalanceAsync();
        /// <summary>
        /// 获取余额大于指定值的账户
        /// </summary>
        Task<IEnumerable<VirtualAccount>> GetAccountsWithBalanceAboveAsync(decimal minBalance);
        /// <summary>
        /// 获取余额排名前N的账户
        /// </summary>
        Task<IEnumerable<VirtualAccount>> GetTopBalanceAccountsAsync(int count);
        /// <summary>
        /// 根据用户ID集合批量获取账户
        /// </summary>
        Task<IEnumerable<VirtualAccount>> GetAccountsByUserIdsAsync(IEnumerable<int> userIds);
        #endregion

        #region 更新操作
        /// <summary>
        /// 扣减余额
        /// </summary>
        Task<bool> DebitAsync(int userId, decimal amount, string reason);
        /// <summary>
        /// 增加余额
        /// </summary>
        Task<bool> CreditAsync(int userId, decimal amount, string reason);
        /// <summary>
        /// 批量更新余额
        /// </summary>
        Task<bool> BatchUpdateBalancesAsync(Dictionary<int, decimal> balanceChanges);
        #endregion

        #region 删除操作
        // 虚拟账户删除由基础仓储 Delete 提供
        #endregion
    }
}
