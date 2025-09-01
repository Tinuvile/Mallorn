using Microsoft.AspNetCore.Http;

namespace CampusTrade.API.Services.File
{
    /// <summary>
    /// 文件服务接口
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="file">文件</param>
        /// <param name="fileType">文件类型</param>
        /// <param name="generateThumbnail">是否生成缩略图</param>
        /// <returns>文件信息</returns>
        Task<FileUploadResult> UploadFileAsync(IFormFile file, FileType fileType, bool generateThumbnail = true);

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>文件流</returns>
        Task<FileDownloadResult> DownloadFileAsync(string fileName);

        /// <summary>
        /// 通过URL下载文件
        /// </summary>
        /// <param name="fileUrl">文件URL</param>
        /// <returns>文件流</returns>
        Task<FileDownloadResult> DownloadFileByUrlAsync(string fileUrl);

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否成功</returns>
        Task<bool> DeleteFileAsync(string fileName);

        /// <summary>
        /// 通过URL删除文件
        /// </summary>
        /// <param name="fileUrl">文件URL</param>
        /// <returns>是否成功</returns>
        Task<bool> DeleteFileByUrlAsync(string fileUrl);

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否存在</returns>
        Task<bool> FileExistsAsync(string fileName);

        /// <summary>
        /// 通过URL检查文件是否存在
        /// </summary>
        /// <param name="fileUrl">文件URL</param>
        /// <returns>是否存在</returns>
        Task<bool> FileExistsByUrlAsync(string fileUrl);

        /// <summary>
        /// 获取文件信息
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>文件信息</returns>
        Task<FileInfo?> GetFileInfoAsync(string fileName);

        /// <summary>
        /// 通过URL获取文件信息
        /// </summary>
        /// <param name="fileUrl">文件URL</param>
        /// <returns>文件信息</returns>
        Task<FileInfo?> GetFileInfoByUrlAsync(string fileUrl);

        /// <summary>
        /// 从文件URL提取文件名
        /// </summary>
        /// <param name="fileUrl">文件URL</param>
        /// <returns>文件名</returns>
        string ExtractFileNameFromUrl(string fileUrl);

        /// <summary>
        /// 从文件URL提取文件类型
        /// </summary>
        /// <param name="fileUrl">文件URL</param>
        /// <returns>文件类型</returns>
        FileType? ExtractFileTypeFromUrl(string fileUrl);

        /// <summary>
        /// 获取缩略图文件名
        /// </summary>
        /// <param name="originalFileName">原始文件名</param>
        /// <returns>缩略图文件名</returns>
        string GetThumbnailFileName(string originalFileName);

        /// <summary>
        /// 生成唯一文件名
        /// </summary>
        /// <param name="originalFileName">原始文件名</param>
        /// <returns>唯一文件名</returns>
        string GenerateUniqueFileName(string originalFileName);

        /// <summary>
        /// 验证文件类型
        /// </summary>
        /// <param name="file">文件</param>
        /// <param name="allowedTypes">允许的文件类型</param>
        /// <returns>是否有效</returns>
        bool ValidateFileType(IFormFile file, string[] allowedTypes);

        /// <summary>
        /// 获取所有文件列表
        /// </summary>
        /// <param name="fileType">文件类型（可选，为null时返回所有类型）</param>
        /// <returns>文件列表结果</returns>
        Task<FileListResult> GetAllFilesAsync(FileType? fileType = null);
    }

    /// <summary>
    /// 文件类型枚举
    /// </summary>
    public enum FileType
    {
        /// <summary>
        /// 商品图片
        /// </summary>
        ProductImage,

        /// <summary>
        /// 举报证据
        /// </summary>
        ReportEvidence,

        /// <summary>
        /// 用户头像
        /// </summary>
        UserAvatar
    }

    /// <summary>
    /// 文件上传结果
    /// </summary>
    public class FileUploadResult
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
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 文件类型
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 文件下载结果
    /// </summary>
    public class FileDownloadResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 文件流
        /// </summary>
        public Stream? FileStream { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 内容类型
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 文件信息项
    /// </summary>
    public class FileInfoItem
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件URL
        /// </summary>
        public string FileUrl { get; set; } = string.Empty;

        /// <summary>
        /// 文件类型
        /// </summary>
        public FileType FileType { get; set; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// 文件扩展名
        /// </summary>
        public string Extension { get; set; } = string.Empty;

        /// <summary>
        /// 缩略图文件名（如果有）
        /// </summary>
        public string? ThumbnailFileName { get; set; }

        /// <summary>
        /// 缩略图URL（如果有）
        /// </summary>
        public string? ThumbnailUrl { get; set; }
    }

    /// <summary>
    /// 文件列表结果
    /// </summary>
    public class FileListResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 文件列表
        /// </summary>
        public List<FileInfoItem> Files { get; set; } = new();

        /// <summary>
        /// 文件总数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 按文件类型分组的统计
        /// </summary>
        public Dictionary<FileType, int> FileTypeStats { get; set; } = new();

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
