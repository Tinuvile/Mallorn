using System.Text.RegularExpressions;

namespace CampusTrade.API.Services.File
{
    /// <summary>
    /// 文件管理助手类
    /// </summary>
    public static class FileHelper
    {
        /// <summary>
        /// 获取缩略图文件名
        /// </summary>
        /// <param name="originalFileName">原始文件名</param>
        /// <returns>缩略图文件名</returns>
        public static string GetThumbnailFileName(string originalFileName)
        {
            if (string.IsNullOrEmpty(originalFileName))
                return string.Empty;

            var extension = Path.GetExtension(originalFileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            return $"{nameWithoutExtension}_thumb{extension}";
        }

        /// <summary>
        /// 从缩略图文件名获取原始文件名
        /// </summary>
        /// <param name="thumbnailFileName">缩略图文件名</param>
        /// <returns>原始文件名</returns>
        public static string GetOriginalFileName(string thumbnailFileName)
        {
            if (string.IsNullOrEmpty(thumbnailFileName))
                return string.Empty;

            var extension = Path.GetExtension(thumbnailFileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(thumbnailFileName);

            // 移除 _thumb 后缀
            if (nameWithoutExtension.EndsWith("_thumb"))
            {
                nameWithoutExtension = nameWithoutExtension.Substring(0, nameWithoutExtension.Length - 6);
            }

            return $"{nameWithoutExtension}{extension}";
        }

        /// <summary>
        /// 判断是否为缩略图文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否为缩略图</returns>
        public static bool IsThumbnailFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            return nameWithoutExtension.EndsWith("_thumb");
        }

        /// <summary>
        /// 从文件URL中提取文件名
        /// </summary>
        /// <param name="fileUrl">文件URL</param>
        /// <returns>文件名</returns>
        public static string ExtractFileNameFromUrl(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return string.Empty;

            try
            {
                var uri = new Uri(fileUrl);
                return Path.GetFileName(uri.LocalPath);
            }
            catch
            {
                // 如果URL解析失败，尝试从最后一个斜杠后获取文件名
                var lastSlashIndex = fileUrl.LastIndexOf('/');
                if (lastSlashIndex >= 0 && lastSlashIndex < fileUrl.Length - 1)
                {
                    return fileUrl.Substring(lastSlashIndex + 1);
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取文件的MIME类型
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>MIME类型</returns>
        public static string GetMimeType(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "application/octet-stream";

            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                ".svg" => "image/svg+xml",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                ".mov" => "video/quicktime",
                ".wmv" => "video/x-ms-wmv",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".7z" => "application/x-7z-compressed",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// 验证文件名是否安全
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否安全</returns>
        public static bool IsFileNameSafe(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            // 检查文件名长度
            if (fileName.Length > 255)
                return false;

            // 检查是否包含危险字符
            var invalidChars = Path.GetInvalidFileNameChars();
            if (fileName.IndexOfAny(invalidChars) >= 0)
                return false;

            // 检查是否为Windows保留名称
            var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName).ToUpper();
            if (reservedNames.Contains(nameWithoutExtension))
                return false;

            return true;
        }

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        /// <param name="bytes">字节数</param>
        /// <returns>格式化后的文件大小</returns>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// 生成安全的文件名
        /// </summary>
        /// <param name="originalFileName">原始文件名</param>
        /// <returns>安全的文件名</returns>
        public static string GenerateSafeFileName(string originalFileName)
        {
            if (string.IsNullOrEmpty(originalFileName))
                return $"file_{Guid.NewGuid():N}";

            // 获取扩展名
            var extension = Path.GetExtension(originalFileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);

            // 移除或替换不安全字符
            var safeChars = Regex.Replace(nameWithoutExtension, @"[^\w\-_\.]", "_");

            // 限制长度
            if (safeChars.Length > 100)
            {
                safeChars = safeChars.Substring(0, 100);
            }

            // 如果处理后为空，使用默认名称
            if (string.IsNullOrEmpty(safeChars))
            {
                safeChars = "file";
            }

            return $"{safeChars}{extension}";
        }

        /// <summary>
        /// 检查文件是否为图片
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否为图片</returns>
        public static bool IsImageFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLower();
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg" };
            return imageExtensions.Contains(extension);
        }

        /// <summary>
        /// 检查文件是否为视频
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否为视频</returns>
        public static bool IsVideoFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLower();
            var videoExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv" };
            return videoExtensions.Contains(extension);
        }

        /// <summary>
        /// 检查文件是否为文档
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否为文档</returns>
        public static bool IsDocumentFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLower();
            var documentExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt" };
            return documentExtensions.Contains(extension);
        }
    }
}
