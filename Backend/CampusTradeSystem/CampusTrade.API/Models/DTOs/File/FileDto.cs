namespace CampusTrade.API.Models.DTOs.File
{
    /// <summary>
    /// 文件上传请求DTO
    /// </summary>
    public class FileUploadRequestDto
    {
        /// <summary>
        /// 文件类型
        /// </summary>
        public string FileType { get; set; } = string.Empty;

        /// <summary>
        /// 是否生成缩略图
        /// </summary>
        public bool GenerateThumbnail { get; set; } = true;

        /// <summary>
        /// 文件描述
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// 文件上传响应DTO
    /// </summary>
    public class FileUploadResponseDto
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件URL
        /// </summary>
        public string FileUrl { get; set; } = string.Empty;

        /// <summary>
        /// 缩略图文件名
        /// </summary>
        public string? ThumbnailFileName { get; set; }

        /// <summary>
        /// 缩略图URL
        /// </summary>
        public string? ThumbnailUrl { get; set; }

        /// <summary>
        /// 文件大小
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 文件类型
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// 格式化的文件大小
        /// </summary>
        public string FormattedFileSize { get; set; } = string.Empty;

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 文件信息DTO
    /// </summary>
    public class FileInfoDto
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 格式化的文件大小
        /// </summary>
        public string FormattedFileSize { get; set; } = string.Empty;

        /// <summary>
        /// 文件类型
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// 文件URL
        /// </summary>
        public string FileUrl { get; set; } = string.Empty;

        /// <summary>
        /// 缩略图URL
        /// </summary>
        public string? ThumbnailUrl { get; set; }

        /// <summary>
        /// 是否为图片
        /// </summary>
        public bool IsImage { get; set; }

        /// <summary>
        /// 是否为视频
        /// </summary>
        public bool IsVideo { get; set; }

        /// <summary>
        /// 是否为文档
        /// </summary>
        public bool IsDocument { get; set; }
    }

    /// <summary>
    /// 批量文件上传响应DTO
    /// </summary>
    public class BatchFileUploadResponseDto
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 文件上传结果列表
        /// </summary>
        public List<FileUploadResponseDto> Results { get; set; } = new();

        /// <summary>
        /// 总文件数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 成功上传数
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败上传数
        /// </summary>
        public int FailureCount { get; set; }
    }
}
