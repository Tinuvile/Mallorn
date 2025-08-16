namespace CampusTrade.API.Models.DTOs.Payment
{
    /// <summary>
    /// 支付结果
    /// </summary>
    public class PaymentResult
    {
        /// <summary>
        /// 支付是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 支付消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 支付金额
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 支付后的账户余额
        /// </summary>
        public decimal RemainingBalance { get; set; }

        /// <summary>
        /// 支付时间
        /// </summary>
        public DateTime PaymentTime { get; set; }

        /// <summary>
        /// 交易流水号
        /// </summary>
        public string? TransactionId { get; set; }
    }
}
