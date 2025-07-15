using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 虚拟账户实体类
    /// </summary>
    public class VirtualAccount
    {
        /// <summary>
        /// 账户ID - 主键，自增
        /// </summary>
        [Key]
        [Column("ACCOUNT_ID", TypeName = "NUMBER")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AccountId { get; set; }

        /// <summary>
        /// 用户ID - 外键，唯一
        /// </summary>
        [Required]
        [Column("USER_ID", TypeName = "NUMBER")]
        public int UserId { get; set; }

        /// <summary>
        /// 账户余额
        /// </summary>
        [Required]
        [Column("BALANCE", TypeName = "decimal(10,2)")]
        [Range(0, 99999999.99, ErrorMessage = "余额必须在0到99999999.99之间")]
        public decimal Balance { get; set; } = 0.00m;

        /// <summary>
        /// 账户创建时间
        /// </summary>
        [Required]
        [Column("CREATED_AT", TypeName = "TIMESTAMP")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        #region 导航属性

        /// <summary>
        /// 关联的用户
        /// </summary>
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// 该账户的所有充值记录
        /// 一对多关系：一个虚拟账户可以有多个充值记录
        /// </summary>
        public virtual ICollection<RechargeRecord> RechargeRecords { get; set; } = new List<RechargeRecord>();

        #endregion

        #region 业务方法

        /// <summary>
        /// 检查账户余额是否足够支付指定金额
        /// </summary>
        /// <param name="amount">需要支付的金额</param>
        /// <returns>如果余额足够返回true，否则返回false</returns>
        public bool HasSufficientBalance(decimal amount)
        {
            if (amount < 0)
            {
                throw new ArgumentException("支付金额不能为负数", nameof(amount));
            }
            return Balance >= amount;
        }

        /// <summary>
        /// 增加账户余额（充值）
        /// </summary>
        /// <param name="amount">充值金额</param>
        /// <exception cref="ArgumentException">当充值金额无效时抛出</exception>
        public void AddBalance(decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("充值金额必须大于0", nameof(amount));
            }

            var newBalance = Balance + amount;
            if (newBalance > 99999999.99m)
            {
                throw new InvalidOperationException("充值后余额不能超过99999999.99");
            }

            Balance = newBalance;
        }

        /// <summary>
        /// 减少账户余额（支付）
        /// </summary>
        /// <param name="amount">支付金额</param>
        /// <exception cref="ArgumentException">当支付金额无效时抛出</exception>
        /// <exception cref="InvalidOperationException">当余额不足时抛出</exception>
        public void DeductBalance(decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("支付金额必须大于0", nameof(amount));
            }

            if (!HasSufficientBalance(amount))
            {
                throw new InvalidOperationException($"余额不足。当前余额：{Balance:F2}，需要支付：{amount:F2}");
            }

            Balance -= amount;
        }

        /// <summary>
        /// 冻结指定金额（预留余额用于待付款订单）
        /// </summary>
        /// <param name="amount">冻结金额</param>
        /// <returns>如果成功冻结返回true，否则返回false</returns>
        public bool TryFreezeAmount(decimal amount)
        {
            if (amount <= 0) return false;
            if (!HasSufficientBalance(amount)) return false;

            // 这里可以扩展为真正的冻结逻辑
            // 目前简化为直接扣除余额
            Balance -= amount;
            return true;
        }

        /// <summary>
        /// 解冻指定金额（取消订单时释放预留余额）
        /// </summary>
        /// <param name="amount">解冻金额</param>
        public void UnfreezeAmount(decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("解冻金额必须大于0", nameof(amount));
            }

            AddBalance(amount);
        }

        /// <summary>
        /// 获取格式化的余额字符串
        /// </summary>
        /// <returns>格式化的余额字符串（保留2位小数）</returns>
        public string GetFormattedBalance()
        {
            return $"¥{Balance:F2}";
        }

        /// <summary>
        /// 检查账户是否为新创建的账户（余额为0且无交易记录）
        /// </summary>
        /// <returns>如果是新账户返回true</returns>
        public bool IsNewAccount()
        {
            return Balance == 0.00m && (!RechargeRecords?.Any() ?? true);
        }

        /// <summary>
        /// 计算总充值金额
        /// </summary>
        /// <returns>所有成功充值记录的总金额</returns>
        public decimal GetTotalRechargeAmount()
        {
            return RechargeRecords?.Where(r => r.Status == "成功")
                                  .Sum(r => r.Amount) ?? 0m;
        }

        /// <summary>
        /// 获取最近的充值记录
        /// </summary>
        /// <param name="count">返回记录数量，默认10条</param>
        /// <returns>最近的充值记录列表</returns>
        public IEnumerable<RechargeRecord> GetRecentRecharges(int count = 10)
        {
            return RechargeRecords?.OrderByDescending(r => r.CreateTime)
                                  .Take(count) ?? Enumerable.Empty<RechargeRecord>();
        }

        #endregion

        #region 静态方法

        /// <summary>
        /// 为指定用户创建新的虚拟账户
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>新创建的虚拟账户实例</returns>
        public static VirtualAccount CreateForUser(int userId)
        {
            return new VirtualAccount
            {
                UserId = userId,
                Balance = 0.00m,
                CreatedAt = DateTime.Now
            };
        }

        #endregion

        #region 常量定义

        /// <summary>
        /// 最大账户余额限制
        /// </summary>
        public const decimal MaxBalance = 99999999.99m;

        /// <summary>
        /// 最小充值金额
        /// </summary>
        public const decimal MinRechargeAmount = 0.01m;

        /// <summary>
        /// 单次最大充值金额
        /// </summary>
        public const decimal MaxRechargeAmount = 50000.00m;

        #endregion
    }
}
