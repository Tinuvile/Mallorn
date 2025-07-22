using System.IO;
using System.Text;
using CampusTrade.API.Controllers;
using CampusTrade.API.Services.File;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Controllers
{
    /// <summary>
    /// 文件控制器单元测试
    /// </summary>
    public class FileControllerTests
    {
        private readonly Mock<IFileService> _mockFileService;
        private readonly Mock<ILogger<FileController>> _mockLogger;
        private readonly FileController _controller;

        public FileControllerTests()
        {
            _mockFileService = new Mock<IFileService>();
            _mockLogger = new Mock<ILogger<FileController>>();
            _controller = new FileController(_mockFileService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task UploadProductImage_WithValidFile_ShouldReturnOk()
        {
            // Arrange
            var mockFile = CreateMockFile("test.jpg", "test content", "image/jpeg");
            var uploadResult = new FileUploadResult
            {
                Success = true,
                FileName = "test_123.jpg",
                FileUrl = "http://localhost:5085/files/products/test_123.jpg",
                ThumbnailFileName = "test_123_thumb.jpg",
                ThumbnailUrl = "http://localhost:5085/files/products/test_123_thumb.jpg",
                FileSize = 12,
                ContentType = "image/jpeg"
            };

            _mockFileService.Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>(), FileType.ProductImage, true))
                .ReturnsAsync(uploadResult);

            // Act
            var result = await _controller.UploadProductImage(mockFile.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task UploadProductImage_WithNullFile_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.UploadProductImage(null!);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task UploadProductImage_WithUploadFailure_ShouldReturnBadRequest()
        {
            // Arrange
            var mockFile = CreateMockFile("test.jpg", "test content", "image/jpeg");
            var uploadResult = new FileUploadResult
            {
                Success = false,
                ErrorMessage = "文件上传失败"
            };

            _mockFileService.Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>(), FileType.ProductImage, true))
                .ReturnsAsync(uploadResult);

            // Act
            var result = await _controller.UploadProductImage(mockFile.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task DownloadFile_WithExistingFile_ShouldReturnFile()
        {
            // Arrange
            var fileName = "test.jpg";
            var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
            var downloadResult = new FileDownloadResult
            {
                Success = true,
                FileStream = fileStream,
                FileName = fileName,
                ContentType = "image/jpeg"
            };

            _mockFileService.Setup(x => x.DownloadFileAsync(fileName))
                .ReturnsAsync(downloadResult);

            // Act
            var result = await _controller.DownloadFile(fileName);

            // Assert
            var fileResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal(fileName, fileResult.FileDownloadName);
            Assert.Equal("image/jpeg", fileResult.ContentType);
        }

        [Fact]
        public async Task DownloadFile_WithNonExistingFile_ShouldReturnNotFound()
        {
            // Arrange
            var fileName = "nonexistent.jpg";
            var downloadResult = new FileDownloadResult
            {
                Success = false,
                ErrorMessage = "文件不存在"
            };

            _mockFileService.Setup(x => x.DownloadFileAsync(fileName))
                .ReturnsAsync(downloadResult);

            // Act
            var result = await _controller.DownloadFile(fileName);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteFile_WithExistingFile_ShouldReturnOk()
        {
            // Arrange
            var fileName = "test.jpg";
            _mockFileService.Setup(x => x.DeleteFileAsync(fileName))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteFile(fileName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task DeleteFile_WithNonExistingFile_ShouldReturnNotFound()
        {
            // Arrange
            var fileName = "nonexistent.jpg";
            _mockFileService.Setup(x => x.DeleteFileAsync(fileName))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteFile(fileName);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task CheckFileExists_WithExistingFile_ShouldReturnTrue()
        {
            // Arrange
            var fileName = "test.jpg";
            _mockFileService.Setup(x => x.FileExistsAsync(fileName))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CheckFileExists(fileName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task CheckFileExists_WithNonExistingFile_ShouldReturnFalse()
        {
            // Arrange
            var fileName = "nonexistent.jpg";
            _mockFileService.Setup(x => x.FileExistsAsync(fileName))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.CheckFileExists(fileName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetFileInfo_WithExistingFile_ShouldReturnFileInfo()
        {
            // Arrange
            var fileName = "test.jpg";
            var fileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), fileName));

            _mockFileService.Setup(x => x.GetFileInfoAsync(fileName))
                .ReturnsAsync(fileInfo);

            // Act
            var result = await _controller.GetFileInfo(fileName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetFileInfo_WithNonExistingFile_ShouldReturnNotFound()
        {
            // Arrange
            var fileName = "nonexistent.jpg";
            _mockFileService.Setup(x => x.GetFileInfoAsync(fileName))
                .ReturnsAsync((FileInfo?)null);

            // Act
            var result = await _controller.GetFileInfo(fileName);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task BatchUploadFiles_WithValidFiles_ShouldReturnOk()
        {
            // Arrange
            var files = new List<IFormFile>
            {
                CreateMockFile("test1.jpg", "content1", "image/jpeg").Object,
                CreateMockFile("test2.jpg", "content2", "image/jpeg").Object
            };

            var uploadResult = new FileUploadResult
            {
                Success = true,
                FileName = "test_123.jpg",
                FileUrl = "http://localhost:5085/files/products/test_123.jpg",
                FileSize = 8,
                ContentType = "image/jpeg"
            };

            _mockFileService.Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>(), FileType.ProductImage, true))
                .ReturnsAsync(uploadResult);

            // Act
            var result = await _controller.BatchUploadFiles(files, "ProductImage");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task BatchUploadFiles_WithInvalidFileType_ShouldReturnBadRequest()
        {
            // Arrange
            var files = new List<IFormFile>
            {
                CreateMockFile("test.jpg", "content", "image/jpeg").Object
            };

            // Act
            var result = await _controller.BatchUploadFiles(files, "InvalidType");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task BatchUploadFiles_WithEmptyFiles_ShouldReturnBadRequest()
        {
            // Arrange
            var files = new List<IFormFile>();

            // Act
            var result = await _controller.BatchUploadFiles(files, "ProductImage");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task DeleteFiles_WithValidFileNames_ShouldReturnOk()
        {
            // Arrange
            var fileNames = new List<string> { "test1.jpg", "test2.png", "test3.pdf" };

            _mockFileService.Setup(x => x.DeleteFileAsync("test1.jpg"))
                .ReturnsAsync(true);
            _mockFileService.Setup(x => x.DeleteFileAsync("test2.png"))
                .ReturnsAsync(true);
            _mockFileService.Setup(x => x.DeleteFileAsync("test3.pdf"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteFiles(fileNames);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // 验证调用次数
            _mockFileService.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Exactly(3));
        }

        [Fact]
        public async Task DeleteFiles_WithMixedResults_ShouldReturnOkWithDetails()
        {
            // Arrange
            var fileNames = new List<string> { "existing.jpg", "nonexistent.png" };

            _mockFileService.Setup(x => x.DeleteFileAsync("existing.jpg"))
                .ReturnsAsync(true);
            _mockFileService.Setup(x => x.DeleteFileAsync("nonexistent.png"))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteFiles(fileNames);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // 验证返回的数据结构
            var response = okResult.Value;
            var successProperty = response?.GetType().GetProperty("success");
            var dataProperty = response?.GetType().GetProperty("data");
            var totalCountProperty = response?.GetType().GetProperty("totalCount");
            var successCountProperty = response?.GetType().GetProperty("successCount");
            var failedCountProperty = response?.GetType().GetProperty("failedCount");

            Assert.NotNull(successProperty);
            Assert.NotNull(dataProperty);
            Assert.NotNull(totalCountProperty);
            Assert.NotNull(successCountProperty);
            Assert.NotNull(failedCountProperty);

            Assert.True((bool)successProperty.GetValue(response)!);
            Assert.Equal(2, (int)totalCountProperty.GetValue(response)!);
            Assert.Equal(1, (int)successCountProperty.GetValue(response)!);
            Assert.Equal(1, (int)failedCountProperty.GetValue(response)!);
        }

        [Fact]
        public async Task DeleteFiles_WithEmptyList_ShouldReturnBadRequest()
        {
            // Arrange
            var fileNames = new List<string>();

            // Act
            var result = await _controller.DeleteFiles(fileNames);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task DeleteFiles_WithNullList_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.DeleteFiles(null!);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task DeleteFiles_WithException_ShouldHandleGracefully()
        {
            // Arrange
            var fileNames = new List<string> { "error.jpg", "normal.png" };

            _mockFileService.Setup(x => x.DeleteFileAsync("error.jpg"))
                .ThrowsAsync(new Exception("文件删除异常"));
            _mockFileService.Setup(x => x.DeleteFileAsync("normal.png"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteFiles(fileNames);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var response = okResult.Value;
            var successCountProperty = response?.GetType().GetProperty("successCount");
            var failedCountProperty = response?.GetType().GetProperty("failedCount");

            Assert.Equal(1, (int)successCountProperty!.GetValue(response)!);
            Assert.Equal(1, (int)failedCountProperty!.GetValue(response)!);
        }

        [Fact]
        public async Task DeleteFilesByUrl_WithValidUrls_ShouldReturnOk()
        {
            // Arrange
            var request = new CampusTrade.API.Controllers.BatchFileUrlRequest
            {
                FileUrls = new List<string>
                {
                    "http://localhost:5085/api/file/files/products/test1.jpg",
                    "http://localhost:5085/api/file/files/avatars/test2.png"
                }
            };

            _mockFileService.Setup(x => x.ExtractFileNameFromUrl(It.IsAny<string>()))
                .Returns((string url) => Path.GetFileName(url));
            _mockFileService.Setup(x => x.DeleteFileByUrlAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteFilesByUrl(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // 验证调用次数
            _mockFileService.Verify(x => x.DeleteFileByUrlAsync(It.IsAny<string>()), Times.Exactly(2));
            _mockFileService.Verify(x => x.ExtractFileNameFromUrl(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task DeleteFilesByUrl_WithMixedResults_ShouldReturnOkWithDetails()
        {
            // Arrange
            var request = new CampusTrade.API.Controllers.BatchFileUrlRequest
            {
                FileUrls = new List<string>
                {
                    "http://localhost:5085/api/file/files/products/existing.jpg",
                    "http://localhost:5085/api/file/files/products/nonexistent.png"
                }
            };

            _mockFileService.Setup(x => x.ExtractFileNameFromUrl(It.IsAny<string>()))
                .Returns((string url) => Path.GetFileName(url));
            _mockFileService.Setup(x => x.DeleteFileByUrlAsync("http://localhost:5085/api/file/files/products/existing.jpg"))
                .ReturnsAsync(true);
            _mockFileService.Setup(x => x.DeleteFileByUrlAsync("http://localhost:5085/api/file/files/products/nonexistent.png"))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteFilesByUrl(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // 验证返回的数据结构
            var response = okResult.Value;
            var successProperty = response?.GetType().GetProperty("success");
            var dataProperty = response?.GetType().GetProperty("data");
            var totalCountProperty = response?.GetType().GetProperty("totalCount");
            var successCountProperty = response?.GetType().GetProperty("successCount");
            var failedCountProperty = response?.GetType().GetProperty("failedCount");

            Assert.NotNull(successProperty);
            Assert.NotNull(dataProperty);
            Assert.NotNull(totalCountProperty);
            Assert.NotNull(successCountProperty);
            Assert.NotNull(failedCountProperty);

            Assert.True((bool)successProperty.GetValue(response)!);
            Assert.Equal(2, (int)totalCountProperty.GetValue(response)!);
            Assert.Equal(1, (int)successCountProperty.GetValue(response)!);
            Assert.Equal(1, (int)failedCountProperty.GetValue(response)!);
        }

        [Fact]
        public async Task DeleteFilesByUrl_WithEmptyUrlList_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new CampusTrade.API.Controllers.BatchFileUrlRequest
            {
                FileUrls = new List<string>()
            };

            // Act
            var result = await _controller.DeleteFilesByUrl(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task DeleteFilesByUrl_WithNullUrls_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new CampusTrade.API.Controllers.BatchFileUrlRequest
            {
                FileUrls = null!
            };

            // Act
            var result = await _controller.DeleteFilesByUrl(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task DeleteFilesByUrl_WithException_ShouldHandleGracefully()
        {
            // Arrange
            var request = new CampusTrade.API.Controllers.BatchFileUrlRequest
            {
                FileUrls = new List<string>
                {
                    "http://localhost:5085/api/file/files/products/error.jpg",
                    "http://localhost:5085/api/file/files/products/normal.png"
                }
            };

            _mockFileService.Setup(x => x.ExtractFileNameFromUrl(It.IsAny<string>()))
                .Returns((string url) => Path.GetFileName(url));
            _mockFileService.Setup(x => x.DeleteFileByUrlAsync("http://localhost:5085/api/file/files/products/error.jpg"))
                .ThrowsAsync(new Exception("URL删除异常"));
            _mockFileService.Setup(x => x.DeleteFileByUrlAsync("http://localhost:5085/api/file/files/products/normal.png"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteFilesByUrl(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var response = okResult.Value;
            var successCountProperty = response?.GetType().GetProperty("successCount");
            var failedCountProperty = response?.GetType().GetProperty("failedCount");

            Assert.Equal(1, (int)successCountProperty!.GetValue(response)!);
            Assert.Equal(1, (int)failedCountProperty!.GetValue(response)!);
        }

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
    }
}
