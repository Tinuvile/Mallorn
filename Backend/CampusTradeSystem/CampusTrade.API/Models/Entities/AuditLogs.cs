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
        #region 常量定义
        public static class ActionTypes
        {
            public const string BanUser = "封禁用户";
            public const string ModifyPermission = "修改权限";
            public const string HandleReport = "处理举报";
        }
        #endregion

        #region 基本信息
        /// <summary>
        /// 日志ID
        /// </summary>
        [Key]
        [Column("LOG_ID", TypeName = "NUMBER")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LogId { get; set; }

        /// <summary>
        /// 管理员ID（外键）
        /// </summary>
        [Required]
        [Column("ADMIN_ID", TypeName = "NUMBER")]
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
        [Column("TARGET_ID", TypeName = "NUMBER")]
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
        [Column("LOG_TIME", TypeName = "TIMESTAMP")]
        public DateTime LogTime { get; set; } = DateTime.Now;
        #endregion

        #region 导航属性
        /// <summary>
        /// 执行操作的管理员
        /// </summary>
        public virtual Admin Admin { get; set; } = null!;
        #endregion

        #region 业务方法
        /// <summary>
        /// 是否为用户封禁操作
        /// </summary>
        public bool IsUserBanAction()
        {
            return ActionType == ActionTypes.BanUser;
        }

        /// <summary>
        /// 是否为权限修改操作
        /// </summary>
        public bool IsPermissionModificationAction()
        {
            return ActionType == ActionTypes.ModifyPermission;
        }

        /// <summary>
        /// 是否为举报处理操作
        /// </summary>
        public bool IsReportHandlingAction()
        {
            return ActionType == ActionTypes.HandleReport;
        }

        /// <summary>
        /// 获取操作类型显示名称
        /// </summary>
        public string GetActionTypeDisplayName()
        {
            return ActionType switch
            {
                ActionTypes.BanUser => "用户封禁",
                ActionTypes.ModifyPermission => "权限修改",
                ActionTypes.HandleReport => "举报处理",
                _ => "未知操作"
            };
        }

        /// <summary>
        /// 获取操作摘要
        /// </summary>
        public string GetActionSummary()
        {
            var targetInfo = TargetId.HasValue ? $"目标ID: {TargetId}" : "无目标";
            return $"{GetActionTypeDisplayName()} - {targetInfo}";
        }

        /// <summary>
        /// 是否为最近的操作（24小时内）
        /// </summary>
        public bool IsRecentAction()
        {
            return LogTime >= DateTime.Now.AddDays(-1);
        }

        /// <summary>
        /// 是否为高风险操作
        /// </summary>
        public bool IsHighRiskAction()
        {
            return ActionType == ActionTypes.BanUser || ActionType == ActionTypes.ModifyPermission;
        }

        /// <summary>
        /// 获取操作时长描述
        /// </summary>
        public string GetTimeSinceAction()
        {
            var timeDiff = DateTime.Now - LogTime;

            if (timeDiff.TotalMinutes < 1)
                return "刚刚";
            if (timeDiff.TotalMinutes < 60)
                return $"{(int)timeDiff.TotalMinutes}分钟前";
            if (timeDiff.TotalHours < 24)
                return $"{(int)timeDiff.TotalHours}小时前";
            if (timeDiff.TotalDays < 30)
                return $"{(int)timeDiff.TotalDays}天前";
            return LogTime.ToString("yyyy-MM-dd");
        }
        #endregion

        #region 静态方法
        /// <summary>
        /// 获取所有可用操作类型
        /// </summary>
        public static List<string> GetAvailableActionTypes()
        {
            return new List<string> { ActionTypes.BanUser, ActionTypes.ModifyPermission, ActionTypes.HandleReport };
        }

        /// <summary>
        /// 验证操作类型是否有效
        /// </summary>
        public static bool IsValidActionType(string actionType)
        {
            return GetAvailableActionTypes().Contains(actionType);
        }

        /// <summary>
        /// 创建用户封禁日志
        /// </summary>
        public static AuditLog CreateUserBanLog(int adminId, int userId, string reason)
        {
            return new AuditLog
            {
                AdminId = adminId,
                ActionType = ActionTypes.BanUser,
                TargetId = userId,
                LogDetail = $"封禁原因: {reason}",
                LogTime = DateTime.Now
            };
        }

        /// <summary>
        /// 创建权限修改日志
        /// </summary>
        public static AuditLog CreatePermissionModificationLog(int adminId, int targetUserId, string details)
        {
            return new AuditLog
            {
                AdminId = adminId,
                ActionType = ActionTypes.ModifyPermission,
                TargetId = targetUserId,
                LogDetail = details,
                LogTime = DateTime.Now
            };
        }

        /// <summary>
        /// 创建举报处理日志
        /// </summary>
        public static AuditLog CreateReportHandlingLog(int adminId, int reportId, string result)
        {
            return new AuditLog
            {
                AdminId = adminId,
                ActionType = ActionTypes.HandleReport,
                TargetId = reportId,
                LogDetail = $"处理结果: {result}",
                LogTime = DateTime.Now
            };
        }

        /// <summary>
        /// 创建通用操作日志
        /// </summary>
        public static AuditLog CreateLog(int adminId, string actionType, int? targetId = null, string? detail = null)
        {
            return new AuditLog
            {
                AdminId = adminId,
                ActionType = actionType,
                TargetId = targetId,
                LogDetail = detail,
                LogTime = DateTime.Now
            };
        }
        #endregion
    }
}
