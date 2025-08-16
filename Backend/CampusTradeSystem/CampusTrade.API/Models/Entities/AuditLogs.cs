using System;
using System.Collections.Generic; // Added for List
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 审计日志实体类
    /// </summary>
    [Table("AUDIT_LOGS")]
    public class AuditLog
    {
        public static class ActionTypes
        {
            public const string BanUser = "封禁用户";
            public const string ModifyPermission = "修改权限";
            public const string HandleReport = "处理举报";
        }

        /// <summary>
        /// 日志ID
        /// </summary>
        [Key]
        [Column("LOG_ID")]
        public int LogId { get; set; }

        /// <summary>
        /// 管理员ID（外键）
        /// </summary>
        [Required]
        [Column("ADMIN_ID")]
        public int AdminId { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        [Required]
        [Column("ACTION_TYPE", TypeName = "VARCHAR2(20)")]
        [MaxLength(20)]
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// 目标ID（如用户ID、举报ID等）
        /// </summary>
        [Column("TARGET_ID")]
        public int? TargetId { get; set; }

        /// <summary>
        /// 操作详情
        /// </summary>
        [Column("LOG_DETAIL", TypeName = "CLOB")]
        public string? LogDetail { get; set; }

        /// <summary>
        /// 操作时间
        /// </summary>
        [Required]
        [Column("LOG_TIME")]
        public DateTime LogTime { get; set; }

        /// <summary>
        /// 执行操作的管理员
        /// </summary>
        public virtual Admin Admin { get; set; } = null!;
    }
}
