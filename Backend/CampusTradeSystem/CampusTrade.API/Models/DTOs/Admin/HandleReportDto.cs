using System.ComponentModel.DataAnnotations;

namespace CampusTrade.API.Models.DTOs.Admin
{
    /// <summary>
    /// 处理举报请求DTO
    /// </summary>
    public class HandleReportDto
    {
        /// <summary>
        /// 处理结果
        /// 通过/驳回/需要更多信息
        /// </summary>
        [Required(ErrorMessage = "处理结果不能为空")]
        [RegularExpression("^(通过|驳回|需要更多信息)$", ErrorMessage = "处理结果只能是通过、驳回或需要更多信息")]
        public string HandleResult { get; set; } = string.Empty;

        /// <summary>
        /// 处理备注
        /// </summary>
        [StringLength(500, ErrorMessage = "处理备注不能超过500个字符")]
        public string? HandleNote { get; set; }

        /// <summary>
        /// 是否对被举报方进行处罚
        /// </summary>
        public bool ApplyPenalty { get; set; } = false;

        /// <summary>
        /// 处罚类型（如果需要处罚）
        /// 轻度处罚/中度处罚/重度处罚
        /// </summary>
        [RegularExpression("^(轻度处罚|中度处罚|重度处罚)$", ErrorMessage = "处罚类型只能是轻度处罚、中度处罚或重度处罚")]
        public string? PenaltyType { get; set; }

        /// <summary>
        /// 处罚时长（天数，如果是封号或禁言）
        /// </summary>
        [Range(1, 365, ErrorMessage = "处罚时长必须在1-365天之间")]
        public int? PenaltyDuration { get; set; }
    }
}
