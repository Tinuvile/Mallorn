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
        #region 常量定义
        public static class Roles
        {
            public const string Super = "super";
            public const string CategoryAdmin = "category_admin";
            public const string ReportAdmin = "report_admin";
        }
        #endregion

        #region 基本信息
        /// <summary>
        /// 管理员ID
        /// </summary>
        [Key]
        [Column("ADMIN_ID", TypeName = "NUMBER")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AdminId { get; set; }

        /// <summary>
        /// 用户ID（外键）
        /// </summary>
        [Required]
        [Column("USER_ID", TypeName = "NUMBER")]
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
        [Column("ASSIGNED_CATEGORY", TypeName = "NUMBER")]
        public int? AssignedCategory { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column("CREATED_AT", TypeName = "TIMESTAMP")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        #endregion

        #region 导航属性
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
        #endregion

        #region 业务方法
        /// <summary>
        /// 是否为超级管理员
        /// </summary>
        public bool IsSuperAdmin()
        {
            return Role == Roles.Super;
        }

        /// <summary>
        /// 是否为分类管理员
        /// </summary>
        public bool IsCategoryAdmin()
        {
            return Role == Roles.CategoryAdmin;
        }

        /// <summary>
        /// 是否为举报管理员
        /// </summary>
        public bool IsReportAdmin()
        {
            return Role == Roles.ReportAdmin;
        }

        /// <summary>
        /// 是否有权限管理指定分类
        /// </summary>
        public bool CanManageCategory(int categoryId)
        {
            return IsSuperAdmin() || (IsCategoryAdmin() && AssignedCategory == categoryId);
        }

        /// <summary>
        /// 是否有权限处理举报
        /// </summary>
        public bool CanHandleReports()
        {
            return IsSuperAdmin() || IsReportAdmin();
        }

        /// <summary>
        /// 是否有权限修改用户权限
        /// </summary>
        public bool CanModifyUserPermissions()
        {
            return IsSuperAdmin();
        }

        /// <summary>
        /// 验证角色和分配分类的有效性
        /// </summary>
        public bool IsValidRoleAssignment()
        {
            return Role switch
            {
                Roles.CategoryAdmin => AssignedCategory.HasValue,
                Roles.Super or Roles.ReportAdmin => !AssignedCategory.HasValue,
                _ => false
            };
        }

        /// <summary>
        /// 获取角色显示名称
        /// </summary>
        public string GetRoleDisplayName()
        {
            return Role switch
            {
                Roles.Super => "超级管理员",
                Roles.CategoryAdmin => "分类管理员",
                Roles.ReportAdmin => "举报管理员",
                _ => "未知角色"
            };
        }

        /// <summary>
        /// 获取权限描述
        /// </summary>
        public string GetPermissionDescription()
        {
            return Role switch
            {
                Roles.Super => "拥有所有权限，可以管理所有分类、处理举报、修改用户权限",
                Roles.CategoryAdmin => $"负责管理分类ID: {AssignedCategory} 的相关事务",
                Roles.ReportAdmin => "负责处理用户举报和投诉",
                _ => "无权限描述"
            };
        }
        #endregion

        #region 静态方法
        /// <summary>
        /// 获取所有可用角色
        /// </summary>
        public static List<string> GetAvailableRoles()
        {
            return new List<string> { Roles.Super, Roles.CategoryAdmin, Roles.ReportAdmin };
        }

        /// <summary>
        /// 验证角色是否有效
        /// </summary>
        public static bool IsValidRole(string role)
        {
            return GetAvailableRoles().Contains(role);
        }

        /// <summary>
        /// 创建分类管理员
        /// </summary>
        public static Admin CreateCategoryAdmin(int userId, int categoryId)
        {
            return new Admin
            {
                UserId = userId,
                Role = Roles.CategoryAdmin,
                AssignedCategory = categoryId,
                CreatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// 创建举报管理员
        /// </summary>
        public static Admin CreateReportAdmin(int userId)
        {
            return new Admin
            {
                UserId = userId,
                Role = Roles.ReportAdmin,
                AssignedCategory = null,
                CreatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// 创建超级管理员
        /// </summary>
        public static Admin CreateSuperAdmin(int userId)
        {
            return new Admin
            {
                UserId = userId,
                Role = Roles.Super,
                AssignedCategory = null,
                CreatedAt = DateTime.Now
            };
        }
        #endregion
    }
}
