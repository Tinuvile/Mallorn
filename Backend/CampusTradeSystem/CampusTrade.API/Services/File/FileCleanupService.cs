using System.IO;
using CampusTrade.API.Options;
using CampusTrade.API.Services.File;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CampusTrade.API.Services.File
{
    /// <summary>
    /// 文件清理后台服务
    /// </summary>
    public class FileCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<FileCleanupService> _logger;
        private readonly FileStorageOptions _options;

        public FileCleanupService(
            IServiceProvider serviceProvider,
            ILogger<FileCleanupService> logger,
            IOptions<FileStorageOptions> options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("文件清理服务已启动");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOrphanFilesAsync();
                    await Task.Delay(TimeSpan.FromHours(_options.CleanupIntervalHours), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "文件清理过程中发生错误");
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken); // 出错后等待30分钟再重试
                }
            }
        }

        /// <summary>
        /// 清理孤立文件
        /// </summary>
        private async Task CleanupOrphanFilesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<Data.CampusTradeDbContext>();

            _logger.LogInformation("开始清理孤立文件");

            try
            {
                var uploadPath = _options.UploadPath;
                if (!Directory.Exists(uploadPath))
                {
                    _logger.LogWarning("上传目录不存在: {Path}", uploadPath);
                    return;
                }

                var cutoffDate = DateTime.Now.AddDays(-_options.OrphanFileRetentionDays);
                var cleanupCount = 0;

                // 遍历所有文件类型目录
                var directories = new[] { "products", "reports", "avatars", "others" };

                foreach (var dir in directories)
                {
                    var fullPath = Path.Combine(uploadPath, dir);
                    if (!Directory.Exists(fullPath)) continue;

                    var files = Directory.GetFiles(fullPath);

                    foreach (var filePath in files)
                    {
                        var fileInfo = new FileInfo(filePath);
                        var fileName = fileInfo.Name;

                        // 检查文件是否超过保留期限
                        if (fileInfo.CreationTime > cutoffDate)
                        {
                            continue;
                        }

                        // 检查文件是否在数据库中被引用
                        var isReferenced = await IsFileReferencedAsync(fileName, dbContext);

                        if (!isReferenced)
                        {
                            try
                            {
                                System.IO.File.Delete(filePath);
                                cleanupCount++;
                                _logger.LogInformation("删除孤立文件: {FileName}", fileName);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "删除文件失败: {FileName}", fileName);
                            }
                        }
                    }
                }

                _logger.LogInformation("文件清理完成，删除了 {Count} 个孤立文件", cleanupCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理孤立文件时发生错误");
            }
        }

        /// <summary>
        /// 检查文件是否在数据库中被引用
        /// </summary>
        private async Task<bool> IsFileReferencedAsync(string fileName, Data.CampusTradeDbContext dbContext)
        {
            try
            {
                // 检查商品图片表
                var productImageCount = await dbContext.ProductImages
                    .CountAsync(pi => pi.ImageUrl.Contains(fileName));
                var productImageExists = productImageCount > 0;

                if (productImageExists)
                {
                    return true;
                }

                // 检查举报证据表
                var reportEvidenceCount = await dbContext.ReportEvidences
                    .CountAsync(re => re.FileUrl.Contains(fileName));
                var reportEvidenceExists = reportEvidenceCount > 0;

                if (reportEvidenceExists)
                {
                    return true;
                }

                // 可以继续添加其他表的检查...

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查文件引用时发生错误: {FileName}", fileName);
                return true; // 出错时假设文件被引用，避免误删
            }
        }
    }
}
