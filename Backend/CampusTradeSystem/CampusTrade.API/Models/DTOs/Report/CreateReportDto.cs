using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CampusTrade.API.Models.DTOs.Report
{
    /// <summary>
    /// 创建举报请求DTO
    /// </summary>
    public class CreateReportDto
    {
        /// <summary>
        /// 订单ID
        /// </summary>
        [Required(ErrorMessage = "订单ID不能为空")]
        [JsonPropertyName("order_id")]
        public int OrderId { get; set; }

        /// <summary>
        /// 举报类型
        /// </summary>
        [Required(ErrorMessage = "举报类型不能为空")]
        [StringLength(50, ErrorMessage = "举报类型长度不能超过50个字符")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 举报描述
        /// </summary>
        [StringLength(2000, ErrorMessage = "举报描述长度不能超过2000个字符")]
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 证据文件列表
        /// </summary>
        [JsonPropertyName("evidence_files")]
        public List<EvidenceFileDto>? EvidenceFiles { get; set; }
    }

    /// <summary>
    /// 证据文件DTO
    /// </summary>
    public class EvidenceFileDto
    {
        /// <summary>
        /// 文件类型
        /// </summary>
        [Required(ErrorMessage = "文件类型不能为空")]
        [StringLength(20, ErrorMessage = "文件类型长度不能超过20个字符")]
        [JsonPropertyName("file_type")]
        public string FileType { get; set; } = string.Empty;

        /// <summary>
        /// 文件URL
        /// </summary>
        [Required(ErrorMessage = "文件URL不能为空")]
        [StringLength(200, ErrorMessage = "文件URL长度不能超过200个字符")]
        [JsonPropertyName("file_url")]
        public string FileUrl { get; set; } = string.Empty;
    }
}
