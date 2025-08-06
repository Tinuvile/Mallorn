using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 管理员实体类
    /// </summary>
    [Table("ADMINS")]
    public class Admin
    {
        public static class Roles
        {
            public const string Super = "super";
            public const string CategoryAdmin = "category_admin";
            public const string ReportAdmin = "report_admin";
        }

        /// <summary>
        /// 管理员ID
        /// </summary>
        [Key]
        [Column("ADMIN_ID")]
        public int AdminId { get; set; }

        /// <summary>
        /// 用户ID（外键）
        /// </summary>
        [Required]
        [Column("USER_ID")]
        public int UserId { get; set; }

        /// <summary>
        /// 管理员角色
        /// </summary>
        [Required]
        [Column("ROLE", TypeName = "VARCHAR2(20)")]
        [MaxLength(20)]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// 分配的分类ID（仅category_admin需要）
        /// </summary>
        [Column("ASSIGNED_CATEGORY")]
        public int? AssignedCategory { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 关联的用户
        /// </summary>
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// 分配的分类（仅category_admin）
        /// </summary>
        public virtual Category? Category { get; set; }

        /// <summary>
        /// 管理员操作的审计日志
        /// </summary>
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}
