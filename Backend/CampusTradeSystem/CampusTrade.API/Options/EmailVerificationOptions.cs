using System.ComponentModel.DataAnnotations;

namespace CampusTrade.API.Options
{
    /// <summary>
    /// 邮箱验证配置选项
    /// </summary>
    public class EmailVerificationOptions
    {
        public const string SectionName = "EmailVerification";

        /// <summary>
        /// 验证码过期时间（分钟）
        /// </summary>
        [Range(1, 60, ErrorMessage = "验证码过期时间必须在1-60分钟之间")]
        public int CodeExpirationMinutes { get; set; } = 10;

        /// <summary>
        /// 发送频率限制（分钟）
        /// </summary>
        [Range(1, 10, ErrorMessage = "发送频率限制必须在1-10分钟之间")]
        public int SendFrequencyLimitMinutes { get; set; } = 1;

        /// <summary>
        /// 令牌过期时间（小时）
        /// </summary>
        [Range(1, 72, ErrorMessage = "令牌过期时间必须在1-72小时之间")]
        public int TokenExpirationHours { get; set; } = 24;

        /// <summary>
        /// 验证邮件的基础URL
        /// </summary>
        [Required(ErrorMessage = "验证邮件基础URL不能为空")]
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// 验证端点路径
        /// </summary>
        public string VerifyEndpoint { get; set; } = "/api/auth/verify-email";

        /// <summary>
        /// 获取邮箱验证链接
        /// </summary>
        /// <param name="token">验证令牌</param>
        /// <returns>完整的验证链接</returns>
        public string GetVerifyEmailUrl(string token)
        {
            var baseUrl = BaseUrl.TrimEnd('/');
            var endpoint = VerifyEndpoint.TrimStart('/');
            return $"{baseUrl}/{endpoint}?token={token}";
        }

        /// <summary>
        /// 验证配置选项
        /// </summary>
        /// <returns>验证错误列表</returns>
        public IEnumerable<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (CodeExpirationMinutes < 1 || CodeExpirationMinutes > 60)
                errors.Add("验证码过期时间必须在1-60分钟之间");

            if (SendFrequencyLimitMinutes < 1 || SendFrequencyLimitMinutes > 10)
                errors.Add("发送频率限制必须在1-10分钟之间");

            if (TokenExpirationHours < 1 || TokenExpirationHours > 72)
                errors.Add("令牌过期时间必须在1-72小时之间");

            if (string.IsNullOrWhiteSpace(BaseUrl))
                errors.Add("验证邮件基础URL不能为空");

            if (string.IsNullOrWhiteSpace(VerifyEndpoint))
                errors.Add("验证端点路径不能为空");

            return errors;
        }
    }
}