namespace CampusTrade.API.Models.DTOs.VirtualAccount
{
    /// <summary>
    /// 虚拟账户余额响应DTO
    /// </summary>
    public class VirtualAccountBalanceResponse
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 账户余额
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdateTime { get; set; }
    }

    /// <summary>
    /// 虚拟账户详情响应DTO
    /// </summary>
    public class VirtualAccountDetailResponse
    {
        /// <summary>
        /// 账户ID
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 账户余额
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 余额检查响应DTO
    /// </summary>
    public class BalanceCheckResponse
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 请求检查的金额
        /// </summary>
        public decimal RequestAmount { get; set; }

        /// <summary>
        /// 当前余额
        /// </summary>
        public decimal CurrentBalance { get; set; }

        /// <summary>
        /// 是否余额充足
        /// </summary>
        public bool HasSufficientBalance { get; set; }
    }
}
