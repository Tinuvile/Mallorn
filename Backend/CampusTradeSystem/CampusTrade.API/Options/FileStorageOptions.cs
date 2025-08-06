namespace CampusTrade.API.Options
{
    /// <summary>
    /// 文件存储配置选项
    /// </summary>
    public class FileStorageOptions
    {
        /// <summary>
        /// 配置节名称
        /// </summary>
        public const string SectionName = "FileStorage";

        /// <summary>
        /// 上传路径
        /// </summary>
        public string UploadPath { get; set; } = "/Storage";

        /// <summary>
        /// 基础URL
        /// </summary>
        public string BaseUrl { get; set; } = "http://localhost:5085";

        /// <summary>
        /// 最大文件大小（字节）
        /// </summary>
        public long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// 支持的图片类型
        /// </summary>
        public string[] ImageTypes { get; set; } = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        /// <summary>
        /// 支持的文档类型
        /// </summary>
        public string[] DocumentTypes { get; set; } = { ".pdf", ".txt", ".doc", ".docx" };

        /// <summary>
        /// 缩略图宽度
        /// </summary>
        public int ThumbnailWidth { get; set; } = 200;

        /// <summary>
        /// 缩略图高度
        /// </summary>
        public int ThumbnailHeight { get; set; } = 200;

        /// <summary>
        /// 缩略图质量(1-100)
        /// </summary>
        public int ThumbnailQuality { get; set; } = 80;

        /// <summary>
        /// 是否启用缩略图
        /// </summary>
        public bool EnableThumbnail { get; set; } = true;

        /// <summary>
        /// 文件清理间隔（小时）
        /// </summary>
        public int CleanupIntervalHours { get; set; } = 24;

        /// <summary>
        /// 孤立文件保留天数
        /// </summary>
        public int OrphanFileRetentionDays { get; set; } = 7;
    }
}
