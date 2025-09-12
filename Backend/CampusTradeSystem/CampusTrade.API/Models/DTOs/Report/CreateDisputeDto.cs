using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CampusTrade.API.Models.DTOs.Report
{
    /// <summary>
    /// 创建争议评价请求DTO
    /// </summary>
    public class CreateDisputeDto
    {
        /// <summary>
        /// 订单ID
        /// </summary>
        [Required(ErrorMessage = "订单ID不能为空")]
        [JsonPropertyName("order_id")]
        public int OrderId { get; set; }

        /// <summary>
        /// 争议原因
        /// </summary>
        [Required(ErrorMessage = "争议原因不能为空")]
        [StringLength(100, ErrorMessage = "争议原因长度不能超过100个字符")]
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 详细描述
        /// </summary>
        [Required(ErrorMessage = "详细描述不能为空")]
        [StringLength(1000, ErrorMessage = "详细描述长度不能超过1000个字符")]
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 证据文件列表
        /// </summary>
        [JsonPropertyName("evidence_files")]
        public List<EvidenceFileDto>? EvidenceFiles { get; set; }
    }
}
