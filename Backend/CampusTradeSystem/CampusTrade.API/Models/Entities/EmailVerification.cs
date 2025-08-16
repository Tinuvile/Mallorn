using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 邮箱验证实体 - 对应 Oracle 数据库中的 EMAIL_VERIFICATION 表
    /// 支持两种验证方式：6位数字验证码 和 邮箱验证链接
    /// </summary>
    [Table("EMAIL_VERIFICATION")]
    public class EmailVerification
    {
        /// <summary>
        /// 验证ID - 主键，对应Oracle中的verification_id字段，自增
        /// </summary>
        [Key]
        [Column("VERIFICATION_ID")]
        public int VerificationId { get; set; }

        /// <summary>
        /// 用户ID - 外键，对应Oracle中的user_id字段
        /// </summary>
        [Required]
        [Column("USER_ID")]
        public int UserId { get; set; }

        /// <summary>
        /// 邮箱地址 - 对应Oracle中的email字段
        /// 需要验证的邮箱地址
        /// </summary>
        [Required]
        [Column("EMAIL", TypeName = "VARCHAR2(100)")]
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 6位数字验证码 - 对应Oracle中的verification_code字段
        /// 用户在邮箱中收到的6位数字验证码，例如：123456
        /// 可为空（当使用Token验证链接时）
        /// </summary>
        [Column("VERIFICATION_CODE", TypeName = "VARCHAR2(6)")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "验证码必须是6位数字")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "验证码必须是6位数字")]
        public string? VerificationCode { get; set; }

        /// <summary>
        /// 验证令牌 - 对应Oracle中的token字段
        /// 用于邮箱验证链接的64位令牌，例如：https://domain.com/verify?token=abc123...
        /// 可为空（当使用验证码时）
        /// </summary>
        [Column("TOKEN", TypeName = "VARCHAR2(64)")]
        [StringLength(64)]
        public string? Token { get; set; }

        /// <summary>
        /// 过期时间 - 对应Oracle中的expire_time字段
        /// 验证码或令牌的过期时间
        /// </summary>
        [Column("EXPIRE_TIME")]
        public DateTime? ExpireTime { get; set; }

        /// <summary>
        /// 使用状态 - 对应Oracle中的is_used字段
        /// false=未使用，true=已使用，默认值false
        /// </summary>
        [Column("IS_USED")]
        public int IsUsed { get; set; } = 0;

        /// <summary>
        /// 创建时间 - 对应Oracle中的created_at字段，默认为当前时间
        /// </summary>
        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; }

        // 导航属性：关联的用户
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        /// <summary>
        /// 验证状态的有效值
        /// </summary>
        public static class VerificationStates
        {
            /// <summary>
            /// 未使用 - 验证码/令牌还未被使用
            /// </summary>
            public const bool Unused = false;

            /// <summary>
            /// 已使用 - 验证码/令牌已被使用
            /// </summary>
            public const bool Used = true;
        }

        /// <summary>
        /// 验证类型枚举
        /// </summary>
        public enum VerificationType
        {
            /// <summary>
            /// 验证码方式 - 6位数字验证码
            /// </summary>
            Code,

            /// <summary>
            /// 令牌方式 - 邮箱验证链接
            /// </summary>
            Token
        }
    }
}
