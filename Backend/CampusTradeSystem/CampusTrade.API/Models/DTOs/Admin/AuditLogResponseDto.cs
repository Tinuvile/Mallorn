namespace CampusTrade.API.Models.DTOs.Admin
{
    /// <summary>
    /// 审计日志响应DTO
    /// </summary>
    public class AuditLogResponseDto
    {
        /// <summary>
        /// 日志ID
        /// </summary>
        public int LogId { get; set; }

        /// <summary>
        /// 管理员ID
        /// </summary>
        public int AdminId { get; set; }

        /// <summary>
        /// 管理员用户名
        /// </summary>
        public string? AdminUsername { get; set; }

        /// <summary>
        /// 管理员角色
        /// </summary>
        public string AdminRole { get; set; } = string.Empty;

        /// <summary>
        /// 操作类型
        /// </summary>
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// 目标ID
        /// </summary>
        public int? TargetId { get; set; }

        /// <summary>
        /// 操作详情
        /// </summary>
        public string? LogDetail { get; set; }

        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTime LogTime { get; set; }
    }
}
