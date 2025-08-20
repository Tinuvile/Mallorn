using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace CampusTrade.API.Services.File
{
    /// <summary>
    /// 缩略图生成服务接口
    /// </summary>
    public interface IThumbnailService
    {
        /// <summary>
        /// 生成缩略图
        /// </summary>
        /// <param name="originalFilePath">原始文件路径</param>
        /// <param name="thumbnailFilePath">缩略图文件路径</param>
        /// <param name="maxWidth">最大宽度</param>
        /// <param name="maxHeight">最大高度</param>
        /// <param name="quality">质量(1-100)</param>
        /// <returns>是否成功</returns>
        Task<bool> GenerateThumbnailAsync(string originalFilePath, string thumbnailFilePath, int maxWidth = 200, int maxHeight = 200, int quality = 80);

        /// <summary>
        /// 从流生成缩略图
        /// </summary>
        /// <param name="originalStream">原始文件流</param>
        /// <param name="thumbnailFilePath">缩略图文件路径</param>
        /// <param name="maxWidth">最大宽度</param>
        /// <param name="maxHeight">最大高度</param>
        /// <param name="quality">质量(1-100)</param>
        /// <returns>是否成功</returns>
        Task<bool> GenerateThumbnailFromStreamAsync(Stream originalStream, string thumbnailFilePath, int maxWidth = 200, int maxHeight = 200, int quality = 80);

        /// <summary>
        /// 检查是否为支持的图片格式
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否支持</returns>
        bool IsImageFormat(string fileName);
    }

    /// <summary>
    /// 缩略图生成服务实现
    /// </summary>
    public class ThumbnailService : IThumbnailService
    {
        private readonly ILogger<ThumbnailService> _logger;
        private readonly string[] _supportedFormats = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public ThumbnailService(ILogger<ThumbnailService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 生成缩略图
        /// </summary>
        public async Task<bool> GenerateThumbnailAsync(string originalFilePath, string thumbnailFilePath, int maxWidth = 200, int maxHeight = 200, int quality = 80)
        {
            try
            {
                if (!System.IO.File.Exists(originalFilePath))
                {
                    _logger.LogError("原始文件不存在: {FilePath}", originalFilePath);
                    return false;
                }

                if (!IsImageFormat(originalFilePath))
                {
                    _logger.LogWarning("不支持的图片格式: {FilePath}", originalFilePath);
                    return false;
                }

                // 确保目标目录存在
                var thumbnailDir = Path.GetDirectoryName(thumbnailFilePath);
                if (!string.IsNullOrEmpty(thumbnailDir) && !Directory.Exists(thumbnailDir))
                {
                    Directory.CreateDirectory(thumbnailDir);
                }

                using var image = await Image.LoadAsync(originalFilePath);
                return await GenerateThumbnailInternal(image, thumbnailFilePath, maxWidth, maxHeight, quality);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成缩略图失败: {OriginalPath} -> {ThumbnailPath}", originalFilePath, thumbnailFilePath);
                return false;
            }
        }

        /// <summary>
        /// 从流生成缩略图
        /// </summary>
        public async Task<bool> GenerateThumbnailFromStreamAsync(Stream originalStream, string thumbnailFilePath, int maxWidth = 200, int maxHeight = 200, int quality = 80)
        {
            try
            {
                if (!IsImageFormat(thumbnailFilePath))
                {
                    _logger.LogWarning("不支持的图片格式: {FilePath}", thumbnailFilePath);
                    return false;
                }

                // 确保目标目录存在
                var thumbnailDir = Path.GetDirectoryName(thumbnailFilePath);
                if (!string.IsNullOrEmpty(thumbnailDir) && !Directory.Exists(thumbnailDir))
                {
                    Directory.CreateDirectory(thumbnailDir);
                }

                originalStream.Position = 0;
                using var image = await Image.LoadAsync(originalStream);
                return await GenerateThumbnailInternal(image, thumbnailFilePath, maxWidth, maxHeight, quality);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从流生成缩略图失败: {ThumbnailPath}", thumbnailFilePath);
                return false;
            }
        }

        /// <summary>
        /// 内部缩略图生成逻辑
        /// </summary>
        private async Task<bool> GenerateThumbnailInternal(Image image, string thumbnailFilePath, int maxWidth, int maxHeight, int quality)
        {
            // 计算缩略图尺寸（保持宽高比）
            var (targetWidth, targetHeight) = CalculateThumbnailSize(image.Width, image.Height, maxWidth, maxHeight);

            // 调整图片大小
            image.Mutate(x => x.Resize(targetWidth, targetHeight));

            // 根据文件扩展名选择编码器
            var extension = Path.GetExtension(thumbnailFilePath).ToLower();
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    await image.SaveAsJpegAsync(thumbnailFilePath, new JpegEncoder { Quality = quality });
                    break;
                case ".png":
                    await image.SaveAsPngAsync(thumbnailFilePath);
                    break;
                case ".gif":
                    await image.SaveAsGifAsync(thumbnailFilePath);
                    break;
                case ".webp":
                    await image.SaveAsWebpAsync(thumbnailFilePath, new WebpEncoder { Quality = quality });
                    break;
                default:
                    // 默认保存为JPEG
                    await image.SaveAsJpegAsync(thumbnailFilePath, new JpegEncoder { Quality = quality });
                    break;
            }

            _logger.LogInformation("缩略图生成成功: {ThumbnailPath}, 尺寸: {Width}x{Height}", thumbnailFilePath, targetWidth, targetHeight);
            return true;
        }

        /// <summary>
        /// 计算缩略图尺寸
        /// </summary>
        private (int width, int height) CalculateThumbnailSize(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
        {
            // 如果原图比最大尺寸小，则保持原尺寸
            if (originalWidth <= maxWidth && originalHeight <= maxHeight)
            {
                return (originalWidth, originalHeight);
            }

            // 计算缩放比例
            var widthRatio = (double)maxWidth / originalWidth;
            var heightRatio = (double)maxHeight / originalHeight;
            var ratio = Math.Min(widthRatio, heightRatio);

            // 计算目标尺寸
            var targetWidth = (int)(originalWidth * ratio);
            var targetHeight = (int)(originalHeight * ratio);

            return (targetWidth, targetHeight);
        }

        /// <summary>
        /// 检查是否为支持的图片格式
        /// </summary>
        public bool IsImageFormat(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return _supportedFormats.Contains(extension);
        }
    }
}
