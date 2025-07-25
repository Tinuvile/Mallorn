using System.IO;
using System.Text;
using CampusTrade.API.Options;
using CampusTrade.API.Services.File;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services
{
    /// <summary>
    /// 文件服务单元测试
    /// </summary>
    public class FileServiceTests
    {
        private readonly Mock<ILogger<FileService>> _mockLogger;
        private readonly Mock<IThumbnailService> _mockThumbnailService;
        private readonly FileStorageOptions _options;
        private readonly FileService _fileService;
        private readonly string _testUploadPath;

        public FileServiceTests()
        {
            _mockLogger = new Mock<ILogger<FileService>>();
            _mockThumbnailService = new Mock<IThumbnailService>();

            _testUploadPath = Path.Combine(Path.GetTempPath(), "file-service-tests");
            _options = new FileStorageOptions
            {
                UploadPath = _testUploadPath,
                BaseUrl = "http://localhost:5085",
                MaxFileSize = 10 * 1024 * 1024,
                ImageTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" },
                DocumentTypes = new[] { ".pdf", ".txt", ".doc", ".docx" },
                ThumbnailWidth = 200,
                ThumbnailHeight = 200,
                ThumbnailQuality = 80
            };

            var mockOptions = new Mock<IOptions<FileStorageOptions>>();
            mockOptions.Setup(x => x.Value).Returns(_options);

            _fileService = new FileService(_mockLogger.Object, _mockThumbnailService.Object, mockOptions.Object);
        }

        [Fact]
        public void GenerateUniqueFileName_ShouldReturnValidFileName()
        {
            // Arrange
            var originalFileName = "test.jpg";

            // Act
            var result = _fileService.GenerateUniqueFileName(originalFileName);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(originalFileName, result);
            Assert.EndsWith(".jpg", result);
            Assert.Contains("_", result);
        }

        [Fact]
        public void GetThumbnailFileName_ShouldReturnCorrectFormat()
        {
            // Arrange
            var originalFileName = "test.jpg";
            var expected = "test_thumb.jpg";

            // Act
            var result = _fileService.GetThumbnailFileName(originalFileName);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("test.jpg", new[] { ".jpg", ".png" }, true)]
        [InlineData("test.pdf", new[] { ".jpg", ".png" }, false)]
        [InlineData("test.PNG", new[] { ".jpg", ".png" }, true)]
        public void ValidateFileType_ShouldReturnExpectedResult(string fileName, string[] allowedTypes, bool expected)
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);

            // Act
            var result = _fileService.ValidateFileType(mockFile.Object, allowedTypes);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task UploadFileAsync_WithValidImage_ShouldReturnSuccess()
        {
            // Arrange
            var fileName = "test.jpg";
            var fileContent = "test image content";
            var mockFile = CreateMockFile(fileName, fileContent, "image/jpeg");

            _mockThumbnailService.Setup(x => x.GenerateThumbnailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(true);

            // Act
            var result = await _fileService.UploadFileAsync(mockFile.Object, FileType.ProductImage, true);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FileName);
            Assert.NotNull(result.FileUrl);
            Assert.NotNull(result.ThumbnailFileName);
            Assert.NotNull(result.ThumbnailUrl);
            Assert.Equal(fileContent.Length, result.FileSize);
            Assert.Equal("image/jpeg", result.ContentType);

            // Cleanup
            CleanupTestFiles();
        }

        [Fact]
        public async Task UploadFileAsync_WithLargeFile_ShouldReturnFailure()
        {
            // Arrange
            var fileName = "large.jpg";
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            mockFile.Setup(f => f.Length).Returns(_options.MaxFileSize + 1);

            // Act
            var result = await _fileService.UploadFileAsync(mockFile.Object, FileType.ProductImage);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("文件大小不能超过", result.ErrorMessage);
        }

        [Fact]
        public async Task UploadFileAsync_WithInvalidFileType_ShouldReturnFailure()
        {
            // Arrange
            var fileName = "test.exe";
            var mockFile = CreateMockFile(fileName, "content", "application/octet-stream");

            // Act
            var result = await _fileService.UploadFileAsync(mockFile.Object, FileType.ProductImage);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("不支持的文件类型", result.ErrorMessage);
        }

        [Fact]
        public async Task DeleteFileAsync_WithExistingFile_ShouldReturnTrue()
        {
            // Arrange
            var fileName = "test.jpg";
            var filePath = Path.Combine(_testUploadPath, "products", fileName);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(filePath, "test content");

            // Act
            var result = await _fileService.DeleteFileAsync(fileName);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(filePath));

            // Cleanup
            CleanupTestFiles();
        }

        [Fact]
        public async Task DeleteFileAsync_WithNonExistingFile_ShouldReturnFalse()
        {
            // Arrange
            var fileName = "nonexistent.jpg";

            // Act
            var result = await _fileService.DeleteFileAsync(fileName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task FileExistsAsync_WithExistingFile_ShouldReturnTrue()
        {
            // Arrange
            var fileName = "test.jpg";
            var filePath = Path.Combine(_testUploadPath, "products", fileName);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(filePath, "test content");

            // Act
            var result = await _fileService.FileExistsAsync(fileName);

            // Assert
            Assert.True(result);

            // Cleanup
            CleanupTestFiles();
        }

        [Fact]
        public async Task FileExistsAsync_WithNonExistingFile_ShouldReturnFalse()
        {
            // Arrange
            var fileName = "nonexistent.jpg";

            // Act
            var result = await _fileService.FileExistsAsync(fileName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DownloadFileAsync_WithExistingFile_ShouldReturnSuccess()
        {
            // Arrange
            var fileName = "test.jpg";
            var content = "test content";
            var filePath = Path.Combine(_testUploadPath, "products", fileName);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(filePath, content);

            // Act
            var result = await _fileService.DownloadFileAsync(fileName);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FileStream);
            Assert.Equal(fileName, result.FileName);
            Assert.Equal("image/jpeg", result.ContentType);

            // Cleanup
            result.FileStream?.Dispose();
            CleanupTestFiles();
        }

        [Fact]
        public async Task DownloadFileAsync_WithNonExistingFile_ShouldReturnFailure()
        {
            // Arrange
            var fileName = "nonexistent.jpg";

            // Act
            var result = await _fileService.DownloadFileAsync(fileName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("文件不存在", result.ErrorMessage);
        }

        #region URL参数方法测试

        [Fact]
        public async Task DownloadFileByUrlAsync_WithValidUrl_ShouldReturnSuccess()
        {
            // Arrange
            var fileName = "test.jpg";
            var content = "test content";
            var filePath = Path.Combine(_testUploadPath, "products", fileName);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(filePath, content);

            var fileUrl = $"{_options.BaseUrl}/api/file/files/products/{fileName}";

            // Act
            var result = await _fileService.DownloadFileByUrlAsync(fileUrl);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FileStream);
            Assert.Equal(fileName, result.FileName);
            Assert.Equal("image/jpeg", result.ContentType);

            // Cleanup
            result.FileStream?.Dispose();
            CleanupTestFiles();
        }

        [Fact]
        public async Task DownloadFileByUrlAsync_WithInvalidUrl_ShouldReturnFailure()
        {
            // Arrange
            var fileUrl = $"{_options.BaseUrl}/api/file/files/products/nonexistent.jpg";

            // Act
            var result = await _fileService.DownloadFileByUrlAsync(fileUrl);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("文件不存在", result.ErrorMessage);
        }

        [Fact]
        public async Task DeleteFileByUrlAsync_WithExistingFile_ShouldReturnTrue()
        {
            // Arrange
            var fileName = "test.jpg";
            var filePath = Path.Combine(_testUploadPath, "products", fileName);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(filePath, "test content");

            var fileUrl = $"{_options.BaseUrl}/api/file/files/products/{fileName}";

            // Act
            var result = await _fileService.DeleteFileByUrlAsync(fileUrl);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(filePath));

            // Cleanup
            CleanupTestFiles();
        }

        [Fact]
        public async Task DeleteFileByUrlAsync_WithNonExistingFile_ShouldReturnFalse()
        {
            // Arrange
            var fileUrl = $"{_options.BaseUrl}/api/file/files/products/nonexistent.jpg";

            // Act
            var result = await _fileService.DeleteFileByUrlAsync(fileUrl);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task FileExistsByUrlAsync_WithExistingFile_ShouldReturnTrue()
        {
            // Arrange
            var fileName = "test.jpg";
            var filePath = Path.Combine(_testUploadPath, "products", fileName);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(filePath, "test content");

            var fileUrl = $"{_options.BaseUrl}/api/file/files/products/{fileName}";

            // Act
            var result = await _fileService.FileExistsByUrlAsync(fileUrl);

            // Assert
            Assert.True(result);

            // Cleanup
            CleanupTestFiles();
        }

        [Fact]
        public async Task FileExistsByUrlAsync_WithNonExistingFile_ShouldReturnFalse()
        {
            // Arrange
            var fileUrl = $"{_options.BaseUrl}/api/file/files/products/nonexistent.jpg";

            // Act
            var result = await _fileService.FileExistsByUrlAsync(fileUrl);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetFileInfoByUrlAsync_WithExistingFile_ShouldReturnFileInfo()
        {
            // Arrange
            var fileName = "test.jpg";
            var content = "test content";
            var filePath = Path.Combine(_testUploadPath, "products", fileName);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(filePath, content);

            var fileUrl = $"{_options.BaseUrl}/api/file/files/products/{fileName}";

            // Act
            var result = await _fileService.GetFileInfoByUrlAsync(fileUrl);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(fileName, result.Name);
            Assert.Equal(content.Length, result.Length);
            Assert.Equal(".jpg", result.Extension);

            // Cleanup
            CleanupTestFiles();
        }

        [Fact]
        public async Task GetFileInfoByUrlAsync_WithNonExistingFile_ShouldReturnNull()
        {
            // Arrange
            var fileUrl = $"{_options.BaseUrl}/api/file/files/products/nonexistent.jpg";

            // Act
            var result = await _fileService.GetFileInfoByUrlAsync(fileUrl);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("http://localhost:5085/api/file/files/products/test.jpg", "test.jpg")]
        [InlineData("http://localhost:5085/api/file/files/avatars/user.png", "user.png")]
        [InlineData("http://localhost:5085/api/file/files/reports/evidence.pdf", "evidence.pdf")]
        [InlineData("/api/file/files/products/image.gif", "image.gif")]
        [InlineData("files/products/document.docx", "document.docx")]
        [InlineData("", "")]
        public void ExtractFileNameFromUrl_WithVariousUrls_ShouldReturnCorrectFileName(string url, string expectedFileName)
        {
            // Act
            var result = _fileService.ExtractFileNameFromUrl(url);

            // Assert
            Assert.Equal(expectedFileName, result);
        }

        [Theory]
        [InlineData("http://localhost:5085/api/file/files/products/test.jpg", FileType.ProductImage)]
        [InlineData("http://localhost:5085/api/file/files/avatars/user.png", FileType.UserAvatar)]
        [InlineData("http://localhost:5085/api/file/files/reports/evidence.pdf", FileType.ReportEvidence)]
        [InlineData("files/others/document.docx", null)]
        [InlineData("/api/file/files/products/image.gif", null)] // 相对路径，Uri构造会失败
        [InlineData("/api/file/files/avatars/avatar.jpg", null)] // 相对路径，Uri构造会失败
        [InlineData("/api/file/files/reports/report.pdf", null)] // 相对路径，Uri构造会失败
        [InlineData("invalid-url", null)]
        [InlineData("", null)]
        public void ExtractFileTypeFromUrl_WithVariousUrls_ShouldReturnCorrectFileType(string url, FileType? expectedFileType)
        {
            // Act
            var result = _fileService.ExtractFileTypeFromUrl(url);

            // Assert
            Assert.Equal(expectedFileType, result);
        }

        [Fact]
        public void ExtractFileNameFromUrl_WithComplexUrl_ShouldHandleCorrectly()
        {
            // Arrange
            var url = "http://localhost:5085/api/file/files/products/20250716154636_a1b2c3d4.jpg";
            var expectedFileName = "20250716154636_a1b2c3d4.jpg";

            // Act
            var result = _fileService.ExtractFileNameFromUrl(url);

            // Assert
            Assert.Equal(expectedFileName, result);
        }

        [Fact]
        public void ExtractFileNameFromUrl_WithQueryParameters_ShouldIgnoreQueryString()
        {
            // Arrange
            var url = "http://localhost:5085/api/file/files/products/test.jpg?v=1&cache=false";
            var expectedFileName = "test.jpg";

            // Act
            var result = _fileService.ExtractFileNameFromUrl(url);

            // Assert
            Assert.Equal(expectedFileName, result);
        }

        [Fact]
        public void ExtractFileTypeFromUrl_WithMalformedUrl_ShouldReturnNull()
        {
            // Arrange
            var url = "not-a-valid-url";

            // Act
            var result = _fileService.ExtractFileTypeFromUrl(url);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DownloadFileByUrlAsync_WithEmptyUrl_ShouldReturnFailure()
        {
            // Arrange
            var fileUrl = "";

            // Act
            var result = await _fileService.DownloadFileByUrlAsync(fileUrl);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("文件不存在", result.ErrorMessage);
        }

        [Fact]
        public async Task DeleteFileByUrlAsync_WithEmptyUrl_ShouldReturnFalse()
        {
            // Arrange
            var fileUrl = "";

            // Act
            var result = await _fileService.DeleteFileByUrlAsync(fileUrl);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task FileExistsByUrlAsync_WithEmptyUrl_ShouldReturnFalse()
        {
            // Arrange
            var fileUrl = "";

            // Act
            var result = await _fileService.FileExistsByUrlAsync(fileUrl);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetFileInfoByUrlAsync_WithEmptyUrl_ShouldReturnNull()
        {
            // Arrange
            var fileUrl = "";

            // Act
            var result = await _fileService.GetFileInfoByUrlAsync(fileUrl);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UrlMethods_ConsistencyWithFileNameMethods_ShouldReturnSameResults()
        {
            // Arrange
            var fileName = "consistency-test.jpg";
            var content = "consistency test content";
            var filePath = Path.Combine(_testUploadPath, "products", fileName);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(filePath, content);

            var fileUrl = $"{_options.BaseUrl}/api/file/files/products/{fileName}";

            // Act & Assert - 检查文件存在
            var existsByName = await _fileService.FileExistsAsync(fileName);
            var existsByUrl = await _fileService.FileExistsByUrlAsync(fileUrl);
            Assert.Equal(existsByName, existsByUrl);

            // Act & Assert - 获取文件信息
            var infoByName = await _fileService.GetFileInfoAsync(fileName);
            var infoByUrl = await _fileService.GetFileInfoByUrlAsync(fileUrl);
            Assert.Equal(infoByName?.Name, infoByUrl?.Name);
            Assert.Equal(infoByName?.Length, infoByUrl?.Length);
            Assert.Equal(infoByName?.Extension, infoByUrl?.Extension);

            // Act & Assert - 下载文件
            var downloadByName = await _fileService.DownloadFileAsync(fileName);
            var downloadByUrl = await _fileService.DownloadFileByUrlAsync(fileUrl);
            Assert.Equal(downloadByName.Success, downloadByUrl.Success);
            Assert.Equal(downloadByName.FileName, downloadByUrl.FileName);
            Assert.Equal(downloadByName.ContentType, downloadByUrl.ContentType);

            // Cleanup
            downloadByName.FileStream?.Dispose();
            downloadByUrl.FileStream?.Dispose();
            CleanupTestFiles();
        }

        [Theory]
        [InlineData("products")]
        [InlineData("avatars")]
        [InlineData("reports")]
        [InlineData("others")]
        public async Task UrlMethods_WithDifferentFileTypes_ShouldWorkCorrectly(string fileTypeFolder)
        {
            // Arrange
            var fileName = $"test-{fileTypeFolder}.jpg";
            var content = "test content";
            var filePath = Path.Combine(_testUploadPath, fileTypeFolder, fileName);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(filePath, content);

            var fileUrl = $"{_options.BaseUrl}/api/file/files/{fileTypeFolder}/{fileName}";

            // Act
            var exists = await _fileService.FileExistsByUrlAsync(fileUrl);
            var fileInfo = await _fileService.GetFileInfoByUrlAsync(fileUrl);
            var downloadResult = await _fileService.DownloadFileByUrlAsync(fileUrl);

            // Assert
            Assert.True(exists);
            Assert.NotNull(fileInfo);
            Assert.Equal(fileName, fileInfo.Name);
            Assert.True(downloadResult.Success);
            Assert.Equal(fileName, downloadResult.FileName);

            // Cleanup
            downloadResult.FileStream?.Dispose();
            CleanupTestFiles();
        }

        #endregion

        private Mock<IFormFile> CreateMockFile(string fileName, string content, string contentType)
        {
            var mockFile = new Mock<IFormFile>();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.ContentType).Returns(contentType);
            mockFile.Setup(f => f.Length).Returns(stream.Length);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                .Returns((Stream target, CancellationToken token) =>
                {
                    stream.Position = 0;
                    return stream.CopyToAsync(target, token);
                });

            return mockFile;
        }

        private void CleanupTestFiles()
        {
            if (Directory.Exists(_testUploadPath))
            {
                Directory.Delete(_testUploadPath, true);
            }
        }

        #region 批量删除功能测试

        [Fact]
        public async Task DeleteFiles_WithMultipleFileNames_ShouldDeleteAllFiles()
        {
            // Arrange
            var fileNames = new List<string> { "test1.jpg", "test2.png", "test3.pdf" };
            var testFiles = new List<string>();

            // 创建测试文件
            foreach (var fileName in fileNames)
            {
                var filePath = Path.Combine(_testUploadPath, "products", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                await File.WriteAllTextAsync(filePath, "test content");
                testFiles.Add(filePath);
            }

            // Act
            var results = new List<bool>();
            foreach (var fileName in fileNames)
            {
                var result = await _fileService.DeleteFileAsync(fileName);
                results.Add(result);
            }

            // Assert
            Assert.All(results, result => Assert.True(result));
            Assert.All(testFiles, filePath => Assert.False(File.Exists(filePath)));
        }

        [Fact]
        public async Task DeleteFiles_WithNonExistentFiles_ShouldReturnFalse()
        {
            // Arrange
            var fileNames = new List<string> { "nonexistent1.jpg", "nonexistent2.png" };

            // Act
            var results = new List<bool>();
            foreach (var fileName in fileNames)
            {
                var result = await _fileService.DeleteFileAsync(fileName);
                results.Add(result);
            }

            // Assert
            Assert.All(results, result => Assert.False(result));
        }

        [Fact]
        public async Task DeleteFiles_WithMixedExistentAndNonExistentFiles_ShouldReturnMixedResults()
        {
            // Arrange
            var existentFile = "existent.jpg";
            var nonExistentFile = "nonexistent.jpg";
            var fileNames = new List<string> { existentFile, nonExistentFile };

            // 创建一个存在的文件
            var existentFilePath = Path.Combine(_testUploadPath, "products", existentFile);
            Directory.CreateDirectory(Path.GetDirectoryName(existentFilePath)!);
            await File.WriteAllTextAsync(existentFilePath, "test content");

            // Act
            var results = new List<bool>();
            foreach (var fileName in fileNames)
            {
                var result = await _fileService.DeleteFileAsync(fileName);
                results.Add(result);
            }

            // Assert
            Assert.True(results[0]); // 存在的文件应该删除成功
            Assert.False(results[1]); // 不存在的文件应该删除失败
            Assert.False(File.Exists(existentFilePath)); // 文件应该被删除
        }

        [Fact]
        public async Task DeleteFilesByUrl_WithMultipleUrls_ShouldDeleteAllFiles()
        {
            // Arrange
            var fileUrls = new List<string>
            {
                "http://localhost:5085/api/file/files/products/test1.jpg",
                "http://localhost:5085/api/file/files/avatars/test2.png",
                "http://localhost:5085/api/file/files/reports/test3.pdf"
            };

            // 创建对应的测试文件
            var testFiles = new List<string>();
            foreach (var fileUrl in fileUrls)
            {
                var fileName = _fileService.ExtractFileNameFromUrl(fileUrl);
                var fileType = _fileService.ExtractFileTypeFromUrl(fileUrl);
                var folder = fileType switch
                {
                    FileType.ProductImage => "products",
                    FileType.UserAvatar => "avatars",
                    FileType.ReportEvidence => "reports",
                    _ => "others"
                };

                var filePath = Path.Combine(_testUploadPath, folder, fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                await File.WriteAllTextAsync(filePath, "test content");
                testFiles.Add(filePath);
            }

            // Act
            var results = new List<bool>();
            foreach (var fileUrl in fileUrls)
            {
                var result = await _fileService.DeleteFileByUrlAsync(fileUrl);
                results.Add(result);
            }

            // Assert
            Assert.All(results, result => Assert.True(result));
            Assert.All(testFiles, filePath => Assert.False(File.Exists(filePath)));
        }

        [Fact]
        public async Task DeleteFilesByUrl_WithNonExistentUrls_ShouldReturnFalse()
        {
            // Arrange
            var fileUrls = new List<string>
            {
                "http://localhost:5085/api/file/files/products/nonexistent1.jpg",
                "http://localhost:5085/api/file/files/avatars/nonexistent2.png"
            };

            // Act
            var results = new List<bool>();
            foreach (var fileUrl in fileUrls)
            {
                var result = await _fileService.DeleteFileByUrlAsync(fileUrl);
                results.Add(result);
            }

            // Assert
            Assert.All(results, result => Assert.False(result));
        }

        [Fact]
        public async Task DeleteFilesByUrl_WithInvalidUrls_ShouldReturnFalse()
        {
            // Arrange
            var invalidUrls = new List<string>
            {
                "invalid-url",
                "",
                "http://localhost:5085/api/file/invalid-path/test.jpg"
            };

            // Act
            var results = new List<bool>();
            foreach (var url in invalidUrls)
            {
                var result = await _fileService.DeleteFileByUrlAsync(url);
                results.Add(result);
            }

            // Assert
            Assert.All(results, result => Assert.False(result));
        }

        [Fact]
        public async Task DeleteFilesByUrl_WithThumbnailFiles_ShouldDeleteBothOriginalAndThumbnail()
        {
            // Arrange
            var originalFileName = "test-image.jpg";
            var thumbnailFileName = "test-image_thumb.jpg";
            var fileUrl = "http://localhost:5085/api/file/files/products/test-image.jpg";

            // 创建原始文件和缩略图
            var originalFilePath = Path.Combine(_testUploadPath, "products", originalFileName);
            var thumbnailFilePath = Path.Combine(_testUploadPath, "products", thumbnailFileName);

            Directory.CreateDirectory(Path.GetDirectoryName(originalFilePath)!);
            await File.WriteAllTextAsync(originalFilePath, "original content");
            await File.WriteAllTextAsync(thumbnailFilePath, "thumbnail content");

            // Act
            var result = await _fileService.DeleteFileByUrlAsync(fileUrl);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(originalFilePath));
            Assert.False(File.Exists(thumbnailFilePath)); // 缩略图也应该被删除
        }

        #endregion

        #region 文件列表功能测试

        [Fact]
        public async Task GetAllFilesAsync_WithoutFileTypeFilter_ShouldReturnAllFiles()
        {
            // Arrange & Act
            var result = await _fileService.GetAllFilesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Files);
            Assert.NotNull(result.FileTypeStats);
            Assert.Equal(result.Files.Count, result.TotalCount);
        }

        [Theory]
        [InlineData(FileType.ProductImage)]
        [InlineData(FileType.UserAvatar)]
        [InlineData(FileType.ReportEvidence)]
        public async Task GetAllFilesAsync_WithFileTypeFilter_ShouldReturnFilteredFiles(FileType fileType)
        {
            // Arrange & Act
            var result = await _fileService.GetAllFilesAsync(fileType);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Files);
            Assert.All(result.Files, file => Assert.Equal(fileType, file.FileType));
        }

        [Fact]
        public async Task GetAllFilesAsync_ShouldNotIncludeThumbnailsInMainList()
        {
            // Arrange & Act
            var result = await _fileService.GetAllFilesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.All(result.Files, file =>
                Assert.False(file.FileName.Contains("_thumb"),
                    $"Main file list should not contain thumbnail files, but found: {file.FileName}"));
        }

        [Fact]
        public async Task GetAllFilesAsync_ShouldIncludeCorrectFileInformation()
        {
            // Arrange & Act
            var result = await _fileService.GetAllFilesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);

            foreach (var file in result.Files)
            {
                Assert.NotEmpty(file.FileName);
                Assert.NotEmpty(file.FileUrl);
                Assert.NotEmpty(file.Extension);
                Assert.True(file.FileSize >= 0);
                Assert.NotEqual(default(DateTime), file.CreatedAt);
                Assert.NotEqual(default(DateTime), file.ModifiedAt);
            }
        }

        #endregion
    }
}
