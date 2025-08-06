using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 用户实体类 - 对应 Oracle 数据库中的 USERS 表
    /// 包含用户基本信息、安全信息、JWT认证相关字段和导航属性
    /// </summary>
    [Table("USERS")]
    public class User
    {
        /// <summary>
        /// 用户ID - 主键，自增（由Oracle触发器处理）
        /// 对应Oracle中的user_id字段
        /// </summary>
        [Key]
        [Column("USER_ID")]
        public int UserId { get; set; }

        /// <summary>
        /// 邮箱地址 - 必填，唯一索引
        /// 对应Oracle中的email字段，最大长度100字符
        /// 用于用户登录和通知发送
        /// </summary>
        [Required]
        [Column("EMAIL", TypeName = "VARCHAR2(100)")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 信用分数 - 用户信誉评级
        /// 对应Oracle中的credit_score字段，NUMBER(3,1)类型
        /// 范围：0-100，默认值60.0
        /// </summary>
        [Column("CREDIT_SCORE", TypeName = "NUMBER(3,1)")]
        [Range(0, 100)]
        public decimal CreditScore { get; set; }

        /// <summary>
        /// 密码哈希值 - 必填，存储BCrypt加密后的密码
        /// 对应Oracle中的password_hash字段，最大长度128字符
        /// </summary>
        [Required]
        [Column("PASSWORD_HASH", TypeName = "VARCHAR2(128)")]
        [StringLength(128)]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// 学生ID - 必填，唯一索引，外键关联students表
        /// 对应Oracle中的student_id字段，最大长度20字符
        /// </summary>
        [Required]
        [Column("STUDENT_ID", TypeName = "VARCHAR2(20)")]
        [StringLength(20)]
        public string StudentId { get; set; } = string.Empty;

        /// <summary>
        /// 用户名 - 可选，用于显示和识别
        /// 对应Oracle中的username字段，最大长度50字符
        /// </summary>
        [Column("USERNAME", TypeName = "VARCHAR2(50)")]
        [StringLength(50)]
        public string? Username { get; set; }

        /// <summary>
        /// 完整姓名 - 可选，用户真实姓名
        /// 对应Oracle中的full_name字段，最大长度100字符
        /// </summary>
        [Column("FULL_NAME", TypeName = "VARCHAR2(100)")]
        [StringLength(100)]
        public string? FullName { get; set; }

        /// <summary>
        /// 手机号码 - 可选，用于联系和验证
        /// 对应Oracle中的phone字段，最大长度20字符
        /// </summary>
        [Column("PHONE", TypeName = "VARCHAR2(20)")]
        [StringLength(20)]
        public string? Phone { get; set; }

        /// <summary>
        /// 账户创建时间 - 记录用户注册时间
        /// 对应Oracle中的created_at字段，TIMESTAMP类型，默认为当前时间
        /// </summary>
        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 账户更新时间 - 记录最后修改时间
        /// 对应Oracle中的updated_at字段，TIMESTAMP类型，触发器自动更新
        /// </summary>
        [Column("UPDATED_AT")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 账户激活状态 - 标识用户是否可用
        /// 对应Oracle中的is_active字段，NUMBER(1)类型
        /// 0=未激活，1=已激活，默认值1
        /// </summary>
        [Column("IS_ACTIVE")]
        public int IsActive { get; set; } = 1;

        /// <summary>
        /// 最后登录时间 - 记录用户最近一次成功登录的时间
        /// 对应Oracle中的last_login_at字段，TIMESTAMP类型，可为空
        /// </summary>
        [Column("LAST_LOGIN_AT")]
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// 最后登录IP地址 - 记录用户最近一次登录的IP
        /// 对应Oracle中的last_login_ip字段，VARCHAR2(45)类型
        /// 支持IPv4和IPv6地址格式
        /// </summary>
        [Column("LAST_LOGIN_IP", TypeName = "VARCHAR2(45)")]
        [StringLength(45)] // IPv6最大长度
        public string? LastLoginIp { get; set; }

        /// <summary>
        /// 登录次数统计 - 累计登录成功次数（默认值0由Oracle处理）
        /// 对应Oracle中的login_count字段，NUMBER类型，默认值0
        /// </summary>
        [Column("LOGIN_COUNT")]
        public int LoginCount { get; set; }

        /// <summary>
        /// 账户锁定状态 - 标识账户是否被锁定（默认值0由Oracle处理）
        /// 对应Oracle中的is_locked字段，NUMBER(1)类型
        /// 0=未锁定，1=已锁定，默认值0
        /// </summary>
        [Column("IS_LOCKED")]
        public int IsLocked { get; set; } = 0;

        /// <summary>
        /// 锁定结束时间 - 临时锁定的解锁时间
        /// 对应Oracle中的lockout_end字段，TIMESTAMP类型，可为空
        /// </summary>
        [Column("LOCKOUT_END")]
        public DateTime? LockoutEnd { get; set; }

        /// <summary>
        /// 失败登录尝试次数 - 连续登录失败的次数（默认值0由Oracle处理）
        /// 对应Oracle中的failed_login_attempts字段，NUMBER类型，默认值0
        /// 达到限制会触发账户锁定
        /// </summary>
        [Column("FAILED_LOGIN_ATTEMPTS")]
        public int FailedLoginAttempts { get; set; }

        /// <summary>
        /// 双因子认证启用状态 - 是否启用2FA（默认值0由Oracle处理）
        /// 对应Oracle中的two_factor_enabled字段，NUMBER(1)类型
        /// 0=未启用，1=已启用，默认值0
        /// </summary>
        [Column("TWO_FACTOR_ENABLED")]
        public int TwoFactorEnabled { get; set; } = 0;

        /// <summary>
        /// 密码修改时间 - 记录最后一次密码变更时间
        /// 对应Oracle中的password_changed_at字段，TIMESTAMP类型，可为空
        /// 用于密码过期策略
        /// </summary>
        [Column("PASSWORD_CHANGED_AT")]
        public DateTime? PasswordChangedAt { get; set; }

        /// <summary>
        /// 安全戳 - 用于JWT Token验证和安全检查
        /// 对应Oracle中的security_stamp字段，VARCHAR2(256)类型
        /// 每次重要操作后会更新，使旧Token失效
        /// </summary>
        [Column("SECURITY_STAMP", TypeName = "VARCHAR2(256)")]
        [StringLength(256)]
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 邮箱验证状态 - 标识邮箱是否已验证（默认值0由Oracle处理）
        /// 对应Oracle中的email_verified字段，NUMBER(1)类型
        /// 0=未验证，1=已验证，默认值0
        /// </summary>
        [Column("EMAIL_VERIFIED")]
        public int EmailVerified { get; set; } = 0;

        /// <summary>
        /// 邮箱验证令牌 - 用于邮箱验证的临时令牌
        /// 对应Oracle中的email_verification_token字段，VARCHAR2(256)类型，可为空
        /// </summary>
        [Column("EMAIL_VERIFICATION_TOKEN", TypeName = "VARCHAR2(256)")]
        [StringLength(256)]
        public string? EmailVerificationToken { get; set; }

        /// <summary>
        /// 关联的学生信息
        /// </summary>
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        /// <summary>
        /// 用户的刷新令牌集合
        /// </summary>
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        /// <summary>
        /// 用户的信用变更记录集合
        /// </summary>
        public virtual ICollection<CreditHistory> CreditHistories { get; set; } = new List<CreditHistory>();

        /// <summary>
        /// 用户的登录日志集合
        /// </summary>
        public virtual ICollection<LoginLogs> LoginLogs { get; set; } = new List<LoginLogs>();

        /// <summary>
        /// 用户的邮箱验证记录集合
        /// </summary>
        public virtual ICollection<EmailVerification> EmailVerifications { get; set; } = new List<EmailVerification>();

        /// <summary>
        /// 用户发布的商品集合
        /// </summary>
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();

        /// <summary>
        /// 用户作为买家的订单集合
        /// </summary>
        public virtual ICollection<Order> BuyerOrders { get; set; } = new List<Order>();

        /// <summary>
        /// 用户作为卖家的订单集合
        /// </summary>
        public virtual ICollection<Order> SellerOrders { get; set; } = new List<Order>();

        /// <summary>
        /// 用户的虚拟账户
        /// </summary>
        public virtual VirtualAccount? VirtualAccount { get; set; }

        /// <summary>
        /// 用户的充值记录集合
        /// </summary>
        public virtual ICollection<RechargeRecord> RechargeRecords { get; set; } = new List<RechargeRecord>();

        /// <summary>
        /// 管理员信息
        /// </summary>
        public virtual Admin? Admin { get; set; }

        /// <summary>
        /// 接收的通知集合
        /// </summary>
        public virtual ICollection<Notification> ReceivedNotifications { get; set; } = new List<Notification>();

        /// <summary>
        /// 用户提交的举报集合
        /// </summary>
        public virtual ICollection<Reports> Reports { get; set; } = new List<Reports>();
    }
}
