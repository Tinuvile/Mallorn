using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 举报证据实体类 - 存储举报的证据文件信息
    /// </summary>
    [Table("REPORT_EVIDENCE")]
    public class ReportEvidence
    {
        /// <summary>
        /// 证据ID - 主键，由序列和触发器自增
        /// </summary>
        [Key]
        [Column("EVIDENCE_ID")]
        public int EvidenceId { get; set; }

        /// <summary>
        /// 举报ID - 外键，关联举报表
        /// </summary>
        [Required]
        [Column("REPORT_ID")]
        public int ReportId { get; set; }

        /// <summary>
        /// 文件类型 - 图片/视频/文档
        /// </summary>
        [Required]
        [Column("FILE_TYPE", TypeName = "VARCHAR2(20)")]
        [StringLength(20)]
        public string FileType { get; set; } = string.Empty;

        /// <summary>
        /// 文件URL - 证据文件的访问地址
        /// </summary>
        [Required]
        [Column("FILE_URL", TypeName = "VARCHAR2(200)")]
        [StringLength(200)]
        public string FileUrl { get; set; } = string.Empty;

        /// <summary>
        /// 上传时间（由Oracle DEFAULT处理）
        /// </summary>
        [Column("UPLOADED_AT")]
        public DateTime UploadedAt { get; set; }

        /// <summary>
        /// 关联的举报信息
        /// </summary>
        public virtual Reports Report { get; set; } = null!;
    }
}
