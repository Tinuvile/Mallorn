namespace CampusTrade.API.Models.DTOs.Payment
{
    /// <summary>
    /// 充值方式枚举
    /// </summary>
    public enum RechargeMethod
    {
        /// <summary>
        /// 模拟充值
        /// </summary>
        Simulation = 1
    }

    /// <summary>
    /// 创建充值请求
    /// </summary>
    public class CreateRechargeRequest
    {
        /// <summary>
        /// 充值金额
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 充值方式
        /// </summary>
        public RechargeMethod Method { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// 充值响应
    /// </summary>
    public class RechargeResponse
    {
        /// <summary>
        /// 充值记录ID
        /// </summary>
        public int RechargeId { get; set; }

        /// <summary>
        /// 充值金额
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 充值方式
        /// </summary>
        public RechargeMethod Method { get; set; }

        /// <summary>
        /// 充值状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime? ExpireTime { get; set; }
    }

    /// <summary>
    /// 充值回调请求
    /// </summary>
    public class RechargeCallbackRequest
    {
        /// <summary>
        /// 充值记录ID
        /// </summary>
        public int RechargeId { get; set; }

        /// <summary>
        /// 第三方交易号
        /// </summary>
        public string? ExternalTransactionId { get; set; }

        /// <summary>
        /// 支付状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 签名
        /// </summary>
        public string? Signature { get; set; }
    }
}
