using System.IO;
using CampusTrade.API.Services.File;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services
{
    /// <summary>
    /// 缩略图服务单元测试
    /// </summary>
    public class ThumbnailServiceTests
    {
        private readonly Mock<ILogger<ThumbnailService>> _mockLogger;
        private readonly ThumbnailService _thumbnailService;
        private readonly string _testPath;

        public ThumbnailServiceTests()
        {
            _mockLogger = new Mock<ILogger<ThumbnailService>>();
            _thumbnailService = new ThumbnailService(_mockLogger.Object);
            _testPath = Path.Combine(Path.GetTempPath(), "thumbnail-tests");
            Directory.CreateDirectory(_testPath);
        }

        [Theory]
        [InlineData("test.jpg", true)]
        [InlineData("test.jpeg", true)]
        [InlineData("test.png", true)]
        [InlineData("test.gif", true)]
        [InlineData("test.webp", true)]
        [InlineData("test.txt", false)]
        [InlineData("test.pdf", false)]
        [InlineData("test", false)]
        public void IsImageFormat_ShouldReturnExpectedResult(string fileName, bool expected)
        {
            // Act
            var result = _thumbnailService.IsImageFormat(fileName);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GenerateThumbnailAsync_WithNonExistentFile_ShouldReturnFalse()
        {
            // Arrange
            var originalFilePath = Path.Combine(_testPath, "nonexistent.jpg");
            var thumbnailFilePath = Path.Combine(_testPath, "nonexistent_thumb.jpg");

            // Act
            var result = await _thumbnailService.GenerateThumbnailAsync(originalFilePath, thumbnailFilePath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GenerateThumbnailAsync_WithUnsupportedFormat_ShouldReturnFalse()
        {
            // Arrange
            var originalFilePath = Path.Combine(_testPath, "test.txt");
            var thumbnailFilePath = Path.Combine(_testPath, "test_thumb.txt");

            // Create a text file
            await File.WriteAllTextAsync(originalFilePath, "This is a text file");

            // Act
            var result = await _thumbnailService.GenerateThumbnailAsync(originalFilePath, thumbnailFilePath);

            // Assert
            Assert.False(result);

            // Cleanup
            CleanupTestFiles();
        }

        [Fact]
        public async Task GenerateThumbnailFromStreamAsync_WithUnsupportedFormat_ShouldReturnFalse()
        {
            // Arrange
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test content"));
            var thumbnailFilePath = Path.Combine(_testPath, "test_thumb.txt");

            // Act
            var result = await _thumbnailService.GenerateThumbnailFromStreamAsync(stream, thumbnailFilePath);

            // Assert
            Assert.False(result);

            // Cleanup
            CleanupTestFiles();
        }

        private void CleanupTestFiles()
        {
            if (Directory.Exists(_testPath))
            {
                Directory.Delete(_testPath, true);
            }
        }
    }
}
