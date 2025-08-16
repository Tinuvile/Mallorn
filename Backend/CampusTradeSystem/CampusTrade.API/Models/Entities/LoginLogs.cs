using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 登录日志实体 - 对应 Oracle 数据库中的 LOGIN_LOGS 表
    /// 记录用户的登录行为，用于安全审计和风险控制
    /// </summary>
    [Table("LOGIN_LOGS")]
    public class LoginLogs
    {
        /// <summary>
        /// 日志ID - 主键，对应Oracle中的log_id字段，Oracle自增
        /// </summary>
        [Key]
        [Column("LOG_ID")]
        public int LogId { get; set; }

        /// <summary>
        /// 用户ID - 外键，对应Oracle中的user_id字段
        /// </summary>
        [Required]
        [Column("USER_ID")]
        public int UserId { get; set; }

        /// <summary>
        /// IP地址 - 记录登录来源IP，对应Oracle中的ip_address字段
        /// 支持IPv4和IPv6格式，最大长度45字符
        /// </summary>
        [Column("IP_ADDRESS", TypeName = "VARCHAR2(45)")]
        [StringLength(45)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// 登录时间 - 对应Oracle中的log_time字段，默认为当前时间
        /// </summary>
        [Column("LOG_TIME")]
        public DateTime LogTime { get; set; }

        /// <summary>
        /// 设备类型 - 对应Oracle中的device_type字段
        /// 限制值：Mobile、PC、Tablet
        /// </summary>
        [Required]
        [Column("DEVICE_TYPE", TypeName = "VARCHAR2(20)")]
        [StringLength(20)]
        public string DeviceType { get; set; } = string.Empty;

        /// <summary>
        /// 风险等级 - 对应Oracle中的risk_level字段
        /// 0=低风险，1=中风险，2=高风险
        /// </summary>
        [Column("RISK_LEVEL")]
        [Range(0, 2, ErrorMessage = "风险等级必须在0-2之间")]
        public int? RiskLevel { get; set; }

        // 导航属性：关联的用户
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        /// <summary>
        /// 设备类型的有效值 - 与Oracle检查约束保持一致
        /// </summary>
        public static class DeviceTypes
        {
            public const string Mobile = "Mobile";
            public const string PC = "PC";
            public const string Tablet = "Tablet";
        }

        /// <summary>
        /// 风险等级的有效值
        /// </summary>
        public static class RiskLevels
        {
            public const int Low = 0;
            public const int Medium = 1;
            public const int High = 2;
        }
    }
}
