using System.IO;
using CampusTrade.API.Options;
using Microsoft.Extensions.Options;

namespace CampusTrade.API.Services.File
{
    /// <summary>
    /// 文件服务实现
    /// </summary>
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;
        private readonly IThumbnailService _thumbnailService;
        private readonly FileStorageOptions _options;
        private readonly string _uploadPath;

        public FileService(
            ILogger<FileService> logger,
            IThumbnailService thumbnailService,
            IOptions<FileStorageOptions> options)
        {
            _logger = logger;
            _thumbnailService = thumbnailService;
            _options = options.Value;
            _uploadPath = _options.UploadPath;

            // 确保上传目录存在
            EnsureDirectoryExists(_uploadPath);
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        public async Task<FileUploadResult> UploadFileAsync(IFormFile file, FileType fileType, bool generateThumbnail = true)
        {
            try
            {
                // 验证文件
                var validationResult = ValidateFile(file, fileType);
                if (!validationResult.IsValid)
                {
                    return new FileUploadResult
                    {
                        Success = false,
                        ErrorMessage = validationResult.ErrorMessage
                    };
                }

                // 生成唯一文件名
                var uniqueFileName = GenerateUniqueFileName(file.FileName);
                var filePath = Path.Combine(_uploadPath, GetFileTypeFolder(fileType), uniqueFileName);

                // 确保目录存在
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    EnsureDirectoryExists(directory);
                }

                // 保存文件
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var result = new FileUploadResult
                {
                    Success = true,
                    FileName = uniqueFileName,
                    FileUrl = GetFileUrl(fileType, uniqueFileName),
                    FileSize = file.Length,
                    ContentType = file.ContentType
                };

                // 如果是图片且需要生成缩略图
                if (generateThumbnail && FileHelper.IsImageFile(file.FileName))
                {
                    var thumbnailFileName = GetThumbnailFileName(uniqueFileName);
                    var thumbnailPath = Path.Combine(_uploadPath, GetFileTypeFolder(fileType), thumbnailFileName);

                    if (await _thumbnailService.GenerateThumbnailAsync(filePath, thumbnailPath,
                        _options.ThumbnailWidth, _options.ThumbnailHeight, _options.ThumbnailQuality))
                    {
                        result.ThumbnailFileName = thumbnailFileName;
                        result.ThumbnailUrl = GetFileUrl(fileType, thumbnailFileName);
                    }
                }

                _logger.LogInformation("文件上传成功: {FileName}, 大小: {Size}字节", uniqueFileName, file.Length);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文件上传失败: {FileName}", file.FileName);
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = "文件上传失败"
                };
            }
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        public Task<FileDownloadResult> DownloadFileAsync(string fileName)
        {
            try
            {
                var filePath = FindFilePath(fileName);
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                {
                    return Task.FromResult(new FileDownloadResult
                    {
                        Success = false,
                        ErrorMessage = "文件不存在"
                    });
                }

                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var contentType = GetContentType(fileName);

                return Task.FromResult(new FileDownloadResult
                {
                    Success = true,
                    FileStream = fileStream,
                    FileName = fileName,
                    ContentType = contentType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文件下载失败: {FileName}", fileName);
                return Task.FromResult(new FileDownloadResult
                {
                    Success = false,
                    ErrorMessage = "文件下载失败"
                });
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        public Task<bool> DeleteFileAsync(string fileName)
        {
            try
            {
                var filePath = FindFilePath(fileName);
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                {
                    return Task.FromResult(false);
                }

                System.IO.File.Delete(filePath);

                // 如果存在缩略图，也一起删除
                var thumbnailFileName = GetThumbnailFileName(fileName);
                var thumbnailPath = FindFilePath(thumbnailFileName);
                if (!string.IsNullOrEmpty(thumbnailPath) && System.IO.File.Exists(thumbnailPath))
                {
                    System.IO.File.Delete(thumbnailPath);
                }

                _logger.LogInformation("文件删除成功: {FileName}", fileName);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文件删除失败: {FileName}", fileName);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        public Task<bool> FileExistsAsync(string fileName)
        {
            var filePath = FindFilePath(fileName);
            return Task.FromResult(!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath));
        }

        /// <summary>
        /// 获取文件信息
        /// </summary>
        public Task<FileInfo?> GetFileInfoAsync(string fileName)
        {
            try
            {
                var filePath = FindFilePath(fileName);
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                {
                    return Task.FromResult<FileInfo?>(null);
                }

                return Task.FromResult<FileInfo?>(new FileInfo(filePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取文件信息失败: {FileName}", fileName);
                return Task.FromResult<FileInfo?>(null);
            }
        }

        /// <summary>
        /// 获取缩略图文件名
        /// </summary>
        public string GetThumbnailFileName(string originalFileName)
        {
            return FileHelper.GetThumbnailFileName(originalFileName);
        }

        /// <summary>
        /// 生成唯一文件名
        /// </summary>
        public string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var guid = Guid.NewGuid().ToString("N")[..8];
            return $"{timestamp}_{guid}{extension}";
        }

        /// <summary>
        /// 验证文件类型
        /// </summary>
        public bool ValidateFileType(IFormFile file, string[] allowedTypes)
        {
            var extension = Path.GetExtension(file.FileName).ToLower();
            return allowedTypes.Contains(extension);
        }

        /// <summary>
        /// 通过URL下载文件
        /// </summary>
        public async Task<FileDownloadResult> DownloadFileByUrlAsync(string fileUrl)
        {
            var fileName = ExtractFileNameFromUrl(fileUrl);
            return await DownloadFileAsync(fileName);
        }

        /// <summary>
        /// 通过URL删除文件
        /// </summary>
        public async Task<bool> DeleteFileByUrlAsync(string fileUrl)
        {
            var fileName = ExtractFileNameFromUrl(fileUrl);
            return await DeleteFileAsync(fileName);
        }

        /// <summary>
        /// 通过URL检查文件是否存在
        /// </summary>
        public async Task<bool> FileExistsByUrlAsync(string fileUrl)
        {
            var fileName = ExtractFileNameFromUrl(fileUrl);
            return await FileExistsAsync(fileName);
        }

        /// <summary>
        /// 通过URL获取文件信息
        /// </summary>
        public async Task<FileInfo?> GetFileInfoByUrlAsync(string fileUrl)
        {
            var fileName = ExtractFileNameFromUrl(fileUrl);
            return await GetFileInfoAsync(fileName);
        }

        /// <summary>
        /// 从文件URL提取文件名
        /// </summary>
        public string ExtractFileNameFromUrl(string fileUrl)
        {
            return FileHelper.ExtractFileNameFromUrl(fileUrl);
        }

        /// <summary>
        /// 从文件URL提取文件类型
        /// </summary>
        public FileType? ExtractFileTypeFromUrl(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return null;
            }

            try
            {
                // 从URL路径中提取文件类型文件夹
                var uri = new Uri(fileUrl);
                var pathSegments = uri.LocalPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                // 查找files/后面的文件夹名
                var filesIndex = Array.IndexOf(pathSegments, "files");
                if (filesIndex >= 0 && filesIndex + 1 < pathSegments.Length)
                {
                    var folder = pathSegments[filesIndex + 1];
                    return folder switch
                    {
                        "products" => FileType.ProductImage,
                        "reports" => FileType.ReportEvidence,
                        "avatars" => FileType.UserAvatar,
                        _ => null
                    };
                }
            }
            catch
            {
                // 如果解析失败，返回null
            }

            return null;
        }

        /// <summary>
        /// 获取所有文件列表
        /// </summary>
        public Task<FileListResult> GetAllFilesAsync(FileType? fileType = null)
        {
            try
            {
                var result = new FileListResult { Success = true };
                var folders = fileType.HasValue
                    ? new[] { GetFileTypeFolder(fileType.Value) }
                    : new[] { "products", "reports", "avatars", "others" };

                foreach (var folder in folders)
                {
                    var folderPath = Path.Combine(_uploadPath, folder);
                    if (!Directory.Exists(folderPath))
                        continue;

                    var files = Directory.GetFiles(folderPath, "*", SearchOption.TopDirectoryOnly);

                    foreach (var filePath in files)
                    {
                        var fileName = Path.GetFileName(filePath);

                        // 跳过缩略图文件，避免重复
                        if (FileHelper.IsThumbnailFile(fileName))
                            continue;

                        var fileInfo = new FileInfo(filePath);
                        var detectedFileType = DetermineFileTypeFromFolder(folder);

                        var fileInfoItem = new FileInfoItem
                        {
                            FileName = fileName,
                            FileUrl = GetFileUrl(detectedFileType, fileName),
                            FileType = detectedFileType,
                            FileSize = fileInfo.Length,
                            CreatedAt = fileInfo.CreationTime,
                            ModifiedAt = fileInfo.LastWriteTime,
                            Extension = fileInfo.Extension
                        };

                        // 检查是否有缩略图
                        var thumbnailFileName = FileHelper.GetThumbnailFileName(fileName);
                        var thumbnailPath = Path.Combine(folderPath, thumbnailFileName);
                        if (System.IO.File.Exists(thumbnailPath))
                        {
                            fileInfoItem.ThumbnailFileName = thumbnailFileName;
                            fileInfoItem.ThumbnailUrl = GetFileUrl(detectedFileType, thumbnailFileName);
                        }

                        result.Files.Add(fileInfoItem);
                    }
                }

                // 统计信息
                result.TotalCount = result.Files.Count;
                result.FileTypeStats = result.Files
                    .GroupBy(f => f.FileType)
                    .ToDictionary(g => g.Key, g => g.Count());

                _logger.LogInformation("获取文件列表成功，共找到 {Count} 个文件", result.TotalCount);
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取文件列表失败");
                return Task.FromResult(new FileListResult
                {
                    Success = false,
                    ErrorMessage = "获取文件列表失败"
                });
            }
        }

        #region 私有方法

        /// <summary>
        /// 验证文件
        /// </summary>
        private FileValidationResult ValidateFile(IFormFile file, FileType fileType)
        {
            if (file == null || file.Length == 0)
            {
                return new FileValidationResult { IsValid = false, ErrorMessage = "文件不能为空" };
            }

            if (file.Length > _options.MaxFileSize)
            {
                return new FileValidationResult { IsValid = false, ErrorMessage = $"文件大小不能超过{_options.MaxFileSize / 1024 / 1024}MB" };
            }

            var allowedTypes = GetAllowedFileTypes(fileType);
            if (!ValidateFileType(file, allowedTypes))
            {
                return new FileValidationResult { IsValid = false, ErrorMessage = $"不支持的文件类型，支持的类型：{string.Join(", ", allowedTypes)}" };
            }

            return new FileValidationResult { IsValid = true };
        }

        /// <summary>
        /// 获取允许的文件类型
        /// </summary>
        private string[] GetAllowedFileTypes(FileType fileType)
        {
            return fileType switch
            {
                FileType.ProductImage => _options.ImageTypes,
                FileType.ReportEvidence => _options.ImageTypes.Concat(_options.DocumentTypes).ToArray(),
                FileType.UserAvatar => _options.ImageTypes,
                _ => _options.ImageTypes
            };
        }

        /// <summary>
        /// 获取文件类型文件夹
        /// </summary>
        private string GetFileTypeFolder(FileType fileType)
        {
            return fileType switch
            {
                FileType.ProductImage => "products",
                FileType.ReportEvidence => "reports",
                FileType.UserAvatar => "avatars",
                _ => "others"
            };
        }

        /// <summary>
        /// 获取文件URL
        /// </summary>
        private string GetFileUrl(FileType fileType, string fileName)
        {
            return $"{_options.BaseUrl}/files/{GetFileTypeFolder(fileType)}/{fileName}";
        }

        /// <summary>
        /// 查找文件路径
        /// </summary>
        private string FindFilePath(string fileName)
        {
            var folders = new[] { "products", "reports", "avatars", "others" };

            foreach (var folder in folders)
            {
                var filePath = Path.Combine(_uploadPath, folder, fileName);
                if (System.IO.File.Exists(filePath))
                {
                    return filePath;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// 获取内容类型
        /// </summary>
        private string GetContentType(string fileName)
        {
            return FileHelper.GetMimeType(fileName);
        }

        /// <summary>
        /// 根据文件夹名确定文件类型
        /// </summary>
        private FileType DetermineFileTypeFromFolder(string folder)
        {
            return folder switch
            {
                "products" => FileType.ProductImage,
                "reports" => FileType.ReportEvidence,
                "avatars" => FileType.UserAvatar,
                _ => FileType.ProductImage // 默认值
            };
        }

        #endregion
    }

    /// <summary>
    /// 文件验证结果
    /// </summary>
    internal class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
