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
        #region 基本信息

        /// <summary>
        /// 证据ID - 主键，自增
        /// </summary>
        [Key]
        [Column("EVIDENCE_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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
        [Column("FILE_TYPE")]
        [StringLength(20)]
        public string FileType { get; set; } = string.Empty;

        /// <summary>
        /// 文件URL - 证据文件的访问地址
        /// </summary>
        [Required]
        [Column("FILE_URL")]
        [StringLength(200)]
        public string FileUrl { get; set; } = string.Empty;

        /// <summary>
        /// 上传时间
        /// </summary>
        [Column("UPLOADED_AT")]
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        #endregion

        #region 导航属性

        /// <summary>
        /// 关联的举报信息
        /// </summary>
        public virtual Reports Report { get; set; } = null!;

        #endregion

        #region 业务方法

        /// <summary>
        /// 验证文件类型是否有效
        /// </summary>
        public bool IsValidFileType()
        {
            var validTypes = new[] { "图片", "视频", "文档" };
            return validTypes.Contains(FileType);
        }

        /// <summary>
        /// 检查是否为图片类型
        /// </summary>
        public bool IsImageType()
        {
            return FileType == "图片";
        }

        /// <summary>
        /// 检查是否为视频类型
        /// </summary>
        public bool IsVideoType()
        {
            return FileType == "视频";
        }

        /// <summary>
        /// 检查是否为文档类型
        /// </summary>
        public bool IsDocumentType()
        {
            return FileType == "文档";
        }

        /// <summary>
        /// 验证文件URL格式
        /// </summary>
        public bool IsValidUrl()
        {
            if (string.IsNullOrWhiteSpace(FileUrl))
                return false;

            return Uri.TryCreate(FileUrl, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// 检查URL是否为本地文件路径
        /// </summary>
        public bool IsLocalFile()
        {
            return !string.IsNullOrWhiteSpace(FileUrl)
                && !FileUrl.StartsWith("http://")
                && !FileUrl.StartsWith("https://");
        }

        /// <summary>
        /// 获取文件扩展名
        /// </summary>
        public string GetFileExtension()
        {
            if (string.IsNullOrWhiteSpace(FileUrl))
                return string.Empty;

            try
            {
                var uri = new Uri(FileUrl);
                return Path.GetExtension(uri.LocalPath).ToLowerInvariant();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 验证文件扩展名是否匹配类型
        /// </summary>
        public bool IsFileExtensionValid()
        {
            var extension = GetFileExtension();
            if (string.IsNullOrEmpty(extension))
                return false;

            return FileType switch
            {
                "图片" => new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" }.Contains(extension),
                "视频" => new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm" }.Contains(extension),
                "文档" => new[] { ".pdf", ".doc", ".docx", ".txt", ".rtf" }.Contains(extension),
                _ => false
            };
        }

        /// <summary>
        /// 计算文件上传时长（小时）
        /// </summary>
        public double GetUploadedHours()
        {
            return (DateTime.Now - UploadedAt).TotalHours;
        }

        /// <summary>
        /// 检查是否为最近上传的文件（24小时内）
        /// </summary>
        public bool IsRecentlyUploaded()
        {
            return GetUploadedHours() <= 24;
        }

        /// <summary>
        /// 生成文件显示名称
        /// </summary>
        public string GetDisplayName()
        {
            var extension = GetFileExtension();
            var typeName = FileType switch
            {
                "图片" => "图片",
                "视频" => "视频",
                "文档" => "文档",
                _ => "文件"
            };

            return $"{typeName}证据_{EvidenceId}{extension}";
        }

        /// <summary>
        /// 检查文件大小是否在合理范围内（基于URL长度估算）
        /// </summary>
        public bool IsReasonableSize()
        {
            // 简单的URL长度检查
            return !string.IsNullOrWhiteSpace(FileUrl) && FileUrl.Length <= 200;
        }

        /// <summary>
        /// 获取文件类型的图标CSS类名
        /// </summary>
        public string GetIconClass()
        {
            return FileType switch
            {
                "图片" => "fa-image",
                "视频" => "fa-video",
                "文档" => "fa-file-text",
                _ => "fa-file"
            };
        }

        /// <summary>
        /// 验证证据数据完整性
        /// </summary>
        public bool IsValid()
        {
            return ReportId > 0
                && IsValidFileType()
                && !string.IsNullOrWhiteSpace(FileUrl)
                && FileUrl.Length <= 200
                && IsValidUrl();
        }

        /// <summary>
        /// 设置文件类型（基于文件扩展名自动识别）
        /// </summary>
        public void SetFileTypeByExtension()
        {
            var extension = GetFileExtension();
            if (string.IsNullOrEmpty(extension))
                return;

            if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" }.Contains(extension))
                FileType = "图片";
            else if (new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm" }.Contains(extension))
                FileType = "视频";
            else if (new[] { ".pdf", ".doc", ".docx", ".txt", ".rtf" }.Contains(extension))
                FileType = "文档";
        }

        /// <summary>
        /// 生成证据的摘要描述
        /// </summary>
        public string GetSummary()
        {
            var timeDesc = IsRecentlyUploaded() ? "最近上传" : $"{GetUploadedHours():F1}小时前上传";
            return $"{FileType}证据 - {timeDesc}";
        }

        #endregion
    }
}
