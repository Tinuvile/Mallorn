using CampusTrade.API.Models.DTOs;
using CampusTrade.API.Services.File;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusTrade.API.Controllers
{
    /// <summary>
    /// 文件URL请求
    /// </summary>
    public class FileUrlRequest
    {
        public string FileUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// 批量文件URL请求
    /// </summary>
    public class BatchFileUrlRequest
    {
        public List<string> FileUrls { get; set; } = new();
    }

    /// <summary>
    /// 文件管理控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // 暂时禁用授权进行测试
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly ILogger<FileController> _logger;

        public FileController(IFileService fileService, ILogger<FileController> logger)
        {
            _fileService = fileService;
            _logger = logger;
        }

        /// <summary>
        /// 上传商品图片
        /// </summary>
        /// <param name="file">图片文件</param>
        /// <returns>上传结果</returns>
        [HttpPost("upload/product-image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadProductImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "请选择要上传的文件" });
            }

            var result = await _fileService.UploadFileAsync(file, FileType.ProductImage, true);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        fileName = result.FileName,
                        fileUrl = result.FileUrl,
                        thumbnailFileName = result.ThumbnailFileName,
                        thumbnailUrl = result.ThumbnailUrl,
                        fileSize = result.FileSize,
                        contentType = result.ContentType
                    }
                });
            }

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        /// <summary>
        /// 上传举报证据
        /// </summary>
        /// <param name="file">证据文件</param>
        /// <returns>上传结果</returns>
        [HttpPost("upload/report-evidence")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadReportEvidence(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "请选择要上传的文件" });
            }

            var result = await _fileService.UploadFileAsync(file, FileType.ReportEvidence, true);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        fileName = result.FileName,
                        fileUrl = result.FileUrl,
                        thumbnailFileName = result.ThumbnailFileName,
                        thumbnailUrl = result.ThumbnailUrl,
                        fileSize = result.FileSize,
                        contentType = result.ContentType
                    }
                });
            }

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        /// <summary>
        /// 上传用户头像
        /// </summary>
        /// <param name="file">头像文件</param>
        /// <returns>上传结果</returns>
        [HttpPost("upload/avatar")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "请选择要上传的文件" });
            }

            var result = await _fileService.UploadFileAsync(file, FileType.UserAvatar, true);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        fileName = result.FileName,
                        fileUrl = result.FileUrl,
                        thumbnailFileName = result.ThumbnailFileName,
                        thumbnailUrl = result.ThumbnailUrl,
                        fileSize = result.FileSize,
                        contentType = result.ContentType
                    }
                });
            }

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>文件流</returns>
        [HttpGet("download/{fileName}")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            var result = await _fileService.DownloadFileAsync(fileName);

            if (result.Success && result.FileStream != null)
            {
                return File(result.FileStream, result.ContentType, result.FileName);
            }

            return NotFound(new { message = result.ErrorMessage ?? "文件不存在" });
        }

        /// <summary>
        /// 通过URL下载文件
        /// </summary>
        /// <param name="request">包含文件URL的请求</param>
        /// <returns>文件流</returns>
        [HttpPost("download/by-url")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadFileByUrl([FromBody] FileUrlRequest request)
        {
            if (string.IsNullOrEmpty(request.FileUrl))
            {
                return BadRequest(new { message = "文件URL不能为空" });
            }

            var result = await _fileService.DownloadFileByUrlAsync(request.FileUrl);

            if (result.Success && result.FileStream != null)
            {
                return File(result.FileStream, result.ContentType, result.FileName);
            }

            return NotFound(new { message = result.ErrorMessage ?? "文件不存在" });
        }

        /// <summary>
        /// 预览文件（直接访问）
        /// </summary>
        /// <param name="fileType">文件类型</param>
        /// <param name="fileName">文件名</param>
        /// <returns>文件流</returns>
        [HttpGet("files/{fileType}/{fileName}")]
        [AllowAnonymous]
        public async Task<IActionResult> PreviewFile(string fileType, string fileName)
        {
            var result = await _fileService.DownloadFileAsync(fileName);

            if (result.Success && result.FileStream != null)
            {
                // 设置缓存头
                Response.Headers["Cache-Control"] = "public, max-age=3600"; // 1小时缓存
                return File(result.FileStream, result.ContentType);
            }

            return NotFound();
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{fileName}")]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            var result = await _fileService.DeleteFileAsync(fileName);

            if (result)
            {
                return Ok(new { success = true, message = "文件删除成功" });
            }

            return NotFound(new { success = false, message = "文件不存在或删除失败" });
        }

        /// <summary>
        /// 通过URL删除文件
        /// </summary>
        /// <param name="request">包含文件URL的请求</param>
        /// <returns>删除结果</returns>
        [HttpDelete("by-url")]
        public async Task<IActionResult> DeleteFileByUrl([FromBody] FileUrlRequest request)
        {
            if (string.IsNullOrEmpty(request.FileUrl))
            {
                return BadRequest(new { message = "文件URL不能为空" });
            }

            var result = await _fileService.DeleteFileByUrlAsync(request.FileUrl);

            if (result)
            {
                return Ok(new { success = true, message = "文件删除成功" });
            }

            return NotFound(new { success = false, message = "文件不存在或删除失败" });
        }

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否存在</returns>
        [HttpGet("exists/{fileName}")]
        public async Task<IActionResult> CheckFileExists(string fileName)
        {
            var exists = await _fileService.FileExistsAsync(fileName);
            return Ok(new { exists });
        }

        /// <summary>
        /// 通过URL检查文件是否存在
        /// </summary>
        /// <param name="request">包含文件URL的请求</param>
        /// <returns>是否存在</returns>
        [HttpPost("exists/by-url")]
        public async Task<IActionResult> CheckFileExistsByUrl([FromBody] FileUrlRequest request)
        {
            if (string.IsNullOrEmpty(request.FileUrl))
            {
                return BadRequest(new { message = "文件URL不能为空" });
            }

            var exists = await _fileService.FileExistsByUrlAsync(request.FileUrl);
            return Ok(new { exists, url = request.FileUrl });
        }

        /// <summary>
        /// 获取文件信息
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>文件信息</returns>
        [HttpGet("info/{fileName}")]
        public async Task<IActionResult> GetFileInfo(string fileName)
        {
            var fileInfo = await _fileService.GetFileInfoAsync(fileName);

            if (fileInfo != null)
            {
                return Ok(new
                {
                    name = fileInfo.Name,
                    size = fileInfo.Length,
                    createdAt = fileInfo.CreationTime,
                    modifiedAt = fileInfo.LastWriteTime,
                    extension = fileInfo.Extension
                });
            }

            return NotFound(new { message = "文件不存在" });
        }

        /// <summary>
        /// 通过URL获取文件信息
        /// </summary>
        /// <param name="request">包含文件URL的请求</param>
        /// <returns>文件信息</returns>
        [HttpPost("info/by-url")]
        public async Task<IActionResult> GetFileInfoByUrl([FromBody] FileUrlRequest request)
        {
            if (string.IsNullOrEmpty(request.FileUrl))
            {
                return BadRequest(new { message = "文件URL不能为空" });
            }

            var fileInfo = await _fileService.GetFileInfoByUrlAsync(request.FileUrl);

            if (fileInfo != null)
            {
                return Ok(new
                {
                    name = fileInfo.Name,
                    size = fileInfo.Length,
                    createdAt = fileInfo.CreationTime,
                    modifiedAt = fileInfo.LastWriteTime,
                    extension = fileInfo.Extension,
                    url = request.FileUrl
                });
            }

            return NotFound(new { message = "文件不存在" });
        }

        /// <summary>
        /// 批量上传文件
        /// </summary>
        /// <param name="files">文件列表</param>
        /// <param name="fileType">文件类型</param>
        /// <returns>批量上传结果</returns>
        [HttpPost("upload/batch")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> BatchUploadFiles(List<IFormFile> files, [FromForm] string fileType)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { message = "请选择要上传的文件" });
            }

            if (!Enum.TryParse<FileType>(fileType, out var parsedFileType))
            {
                return BadRequest(new { message = "无效的文件类型" });
            }

            var results = new List<object>();

            foreach (var file in files)
            {
                var result = await _fileService.UploadFileAsync(file, parsedFileType, true);

                if (result.Success)
                {
                    results.Add(new
                    {
                        fileName = result.FileName,
                        fileUrl = result.FileUrl,
                        thumbnailFileName = result.ThumbnailFileName,
                        thumbnailUrl = result.ThumbnailUrl,
                        fileSize = result.FileSize,
                        contentType = result.ContentType,
                        success = true
                    });
                }
                else
                {
                    results.Add(new
                    {
                        fileName = file.FileName,
                        error = result.ErrorMessage,
                        success = false
                    });
                }
            }

            return Ok(new
            {
                success = true,
                data = results,
                totalCount = files.Count,
                successCount = results.Count(r => (bool)r.GetType().GetProperty("success")?.GetValue(r)!)
            });
        }

        /// <summary>
        /// 从URL提取文件名
        /// </summary>
        /// <param name="request">包含文件URL的请求</param>
        /// <returns>文件名</returns>
        [HttpPost("extract-filename")]
        [AllowAnonymous]
        public IActionResult ExtractFileNameFromUrl([FromBody] FileUrlRequest request)
        {
            if (string.IsNullOrEmpty(request.FileUrl))
            {
                return BadRequest(new { message = "文件URL不能为空" });
            }

            var fileName = _fileService.ExtractFileNameFromUrl(request.FileUrl);
            var fileType = _fileService.ExtractFileTypeFromUrl(request.FileUrl);

            return Ok(new
            {
                fileName = fileName,
                fileType = fileType?.ToString(),
                originalUrl = request.FileUrl
            });
        }

        /// <summary>
        /// 批量检查文件是否存在（通过URL）
        /// </summary>
        /// <param name="request">包含文件URL列表的请求</param>
        /// <returns>批量检查结果</returns>
        [HttpPost("batch-exists")]
        [AllowAnonymous]
        public async Task<IActionResult> BatchCheckFileExistsByUrl([FromBody] BatchFileUrlRequest request)
        {
            if (request.FileUrls == null || request.FileUrls.Count == 0)
            {
                return BadRequest(new { message = "文件URL列表不能为空" });
            }

            var results = new List<object>();

            foreach (var fileUrl in request.FileUrls)
            {
                var exists = await _fileService.FileExistsByUrlAsync(fileUrl);
                var fileName = _fileService.ExtractFileNameFromUrl(fileUrl);

                results.Add(new
                {
                    url = fileUrl,
                    fileName = fileName,
                    exists = exists
                });
            }

            return Ok(new
            {
                success = true,
                data = results,
                totalCount = request.FileUrls.Count,
                existingCount = results.Count(r => (bool)r.GetType().GetProperty("exists")?.GetValue(r)!)
            });
        }
    }
}
