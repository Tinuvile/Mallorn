using System.Text.Json.Serialization;

namespace CampusTrade.API.Models.DTOs.Report
{
    /// <summary>
    /// 举报列表项DTO
    /// </summary>
    public class ReportListItemDto
    {
        /// <summary>
        /// 举报ID
        /// </summary>
        [JsonPropertyName("report_id")]
        public int ReportId { get; set; }

        /// <summary>
        /// 订单ID
        /// </summary>
        [JsonPropertyName("order_id")]
        public int OrderId { get; set; }

        /// <summary>
        /// 举报类型
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 优先级
        /// </summary>
        [JsonPropertyName("priority")]
        public int? Priority { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [JsonPropertyName("create_time")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 证据数量
        /// </summary>
        [JsonPropertyName("evidence_count")]
        public int EvidenceCount { get; set; }
    }

    /// <summary>
    /// 举报详情DTO
    /// </summary>
    public class ReportDetailDto
    {
        /// <summary>
        /// 举报ID
        /// </summary>
        [JsonPropertyName("report_id")]
        public int ReportId { get; set; }

        /// <summary>
        /// 订单ID
        /// </summary>
        [JsonPropertyName("order_id")]
        public int OrderId { get; set; }

        /// <summary>
        /// 举报类型
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 优先级
        /// </summary>
        [JsonPropertyName("priority")]
        public int? Priority { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [JsonPropertyName("create_time")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 举报人信息
        /// </summary>
        [JsonPropertyName("reporter")]
        public ReporterInfoDto? Reporter { get; set; }

        /// <summary>
        /// 证据列表
        /// </summary>
        [JsonPropertyName("evidences")]
        public List<EvidenceDto> Evidences { get; set; } = new();
    }

    /// <summary>
    /// 举报人信息DTO
    /// </summary>
    public class ReporterInfoDto
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
    }

    /// <summary>
    /// 证据DTO
    /// </summary>
    public class EvidenceDto
    {
        /// <summary>
        /// 证据ID
        /// </summary>
        [JsonPropertyName("evidence_id")]
        public int EvidenceId { get; set; }

        /// <summary>
        /// 文件类型
        /// </summary>
        [JsonPropertyName("file_type")]
        public string FileType { get; set; } = string.Empty;

        /// <summary>
        /// 文件URL
        /// </summary>
        [JsonPropertyName("file_url")]
        public string FileUrl { get; set; } = string.Empty;

        /// <summary>
        /// 上传时间
        /// </summary>
        [JsonPropertyName("uploaded_at")]
        public DateTime UploadedAt { get; set; }
    }

    /// <summary>
    /// 举报类型DTO
    /// </summary>
    public class ReportTypeDto
    {
        /// <summary>
        /// 类型值
        /// </summary>
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 显示标签
        /// </summary>
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// 优先级
        /// </summary>
        [JsonPropertyName("priority")]
        public int Priority { get; set; }
    }
}
