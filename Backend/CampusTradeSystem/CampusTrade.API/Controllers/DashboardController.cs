using Microsoft.AspNetCore.Mvc;
using CampusTrade.API.Models.DTOs;
using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Repositories.Interfaces;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;

namespace CampusTrade.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<DashboardController> _logger;

        // 注入日志服务
        public DashboardController(
            IOrderRepository orderRepository,
            IUserRepository userRepository,
            ILogger<DashboardController> logger)
        {
            _orderRepository = orderRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        /// <summary>
        /// 获取数据报表统计数据
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStatistics(int year, int activityDays = 30)
        {
            try
            {
                _logger.LogInformation("开始获取报表统计数据，年份：{Year}，活跃天数：{ActivityDays}", year, activityDays);
                var stats = new DashboardStatsDto();

                // 批量获取月度交易数据
                stats.MonthlyTransactions = await _orderRepository.GetMonthlyTransactionsAsync(year);
                _logger.LogDebug("已获取月度交易数据，记录数：{Count}", stats.MonthlyTransactions?.Count ?? 0);

                // 批量获取热门商品
                stats.PopularProducts = await _orderRepository.GetPopularProductsAsync(10);
                _logger.LogDebug("已获取热门商品数据，记录数：{Count}", stats.PopularProducts?.Count ?? 0);

                // 获取用户活跃度数据
                var startDate = DateTime.UtcNow.AddDays(-activityDays).Date;
                // 优化点：通过仓储一次性获取日期范围内的活跃用户统计，避免循环查询
                var dailyActiveUsers = await _userRepository.GetDailyActiveUsersAsync(startDate, DateTime.UtcNow.Date);
                // 获取注册趋势
                var registrationTrend = await _userRepository.GetUserRegistrationTrendAsync(activityDays);

                // 填充用户活跃度数据
                foreach (var item in registrationTrend)
                {
                    stats.UserActivities.Add(new UserActivityDto
                    {
                        Date = item.Key,
                        ActiveUserCount = dailyActiveUsers.TryGetValue(item.Key, out int count) ? count : 0,
                        NewUserCount = item.Value
                    });
                }

                _logger.LogInformation("报表统计数据获取成功，年份：{Year}", year);
                return Ok(ApiResponse<DashboardStatsDto>.CreateSuccess(stats, "报表统计数据获取成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取报表统计数据失败，年份：{Year}，活跃天数：{ActivityDays}", year, activityDays);
                return StatusCode(500, ApiResponse<DashboardStatsDto>.CreateError("获取统计数据失败", "DASHBOARD_STATS_ERROR"));
            }
        }

        /// <summary>
        /// 导出Excel报表
        /// </summary>
        [HttpGet("export/excel")]
        public async Task<IActionResult> ExportToExcel(int year)
        {
            try
            {
                _logger.LogInformation("开始导出Excel报表，年份：{Year}", year);
                var stats = new DashboardStatsDto
                {
                    MonthlyTransactions = await _orderRepository.GetMonthlyTransactionsAsync(year),
                    PopularProducts = await _orderRepository.GetPopularProductsAsync(10)
                };

                using (var stream = new MemoryStream())
                {
                    var workbook = new XSSFWorkbook();

                    // 创建月度交易数据工作表
                    var monthlySheet = workbook.CreateSheet("月度交易数据");
                    CreateMonthlyTransactionsSheet(monthlySheet, stats.MonthlyTransactions);

                    // 创建热门商品工作表
                    var popularSheet = workbook.CreateSheet("热门商品排行");
                    CreatePopularProductsSheet(popularSheet, stats.PopularProducts);

                    workbook.Write(stream);
                    _logger.LogInformation("Excel报表导出成功，年份：{Year}", year);
                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"校园交易统计_{year}.xlsx");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出Excel报表失败，年份：{Year}", year);
                return BadRequest(ApiResponse.CreateError("Excel导出失败", "EXCEL_EXPORT_ERROR"));
            }
        }

        /// <summary>
        /// 导出PDF报表
        /// </summary>
        [HttpGet("export/pdf")]
        public async Task<IActionResult> ExportToPdf(int year)
        {
            try
            {
                _logger.LogInformation("开始导出PDF报表，年份：{Year}", year);
                var stats = new DashboardStatsDto
                {
                    MonthlyTransactions = await _orderRepository.GetMonthlyTransactionsAsync(year),
                    PopularProducts = await _orderRepository.GetPopularProductsAsync(10)
                };

                using (var stream = new MemoryStream())
                {
                    var document = new Document(PageSize.A4, 50, 50, 25, 25);
                    var writer = PdfWriter.GetInstance(document, stream);

                    document.Open();

                    // 添加标题
                    var titleFont = FontFactory.GetFont("Arial", 18, Font.BOLD);
                    var title = new Paragraph($"校园交易平台统计报表 - {year}", titleFont);
                    title.Alignment = Element.ALIGN_CENTER;
                    document.Add(title);
                    document.Add(Chunk.NEWLINE);

                    // 添加月度交易数据
                    AddMonthlyTransactionsToPdf(document, stats.MonthlyTransactions);

                    // 添加热门商品数据
                    AddPopularProductsToPdf(document, stats.PopularProducts);

                    document.Close();
                    _logger.LogInformation("PDF报表导出成功，年份：{Year}", year);
                    return File(
                        stream.ToArray(),
                        "application/pdf",
                        $"校园交易统计_{year}.pdf");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出PDF报表失败，年份：{Year}", year);
                return BadRequest(ApiResponse.CreateError("PDF导出失败", "PDF_EXPORT_ERROR"));
            }
        }

        #region 私有辅助方法
        private void CreateMonthlyTransactionsSheet(ISheet sheet, List<MonthlyTransactionDto> data)
        {
            // 创建表头
            var headerRow = sheet.CreateRow(0);
            headerRow.CreateCell(0).SetCellValue("月份");
            headerRow.CreateCell(1).SetCellValue("订单数量");
            headerRow.CreateCell(2).SetCellValue("交易总金额");

            // 填充数据
            for (int i = 0; i < data?.Count; i++)
            {
                var row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(data[i].Month);
                row.CreateCell(1).SetCellValue(data[i].OrderCount);
                row.CreateCell(2).SetCellValue((double)data[i].TotalAmount);
            }

            // 自动调整列宽
            for (int i = 0; i < 3; i++)
            {
                sheet.AutoSizeColumn(i);
            }
        }

        private void CreatePopularProductsSheet(ISheet sheet, List<PopularProductDto> data)
        {
            // 创建表头
            var headerRow = sheet.CreateRow(0);
            headerRow.CreateCell(0).SetCellValue("商品ID");
            headerRow.CreateCell(1).SetCellValue("商品名称");
            headerRow.CreateCell(2).SetCellValue("订单数量");

            // 填充数据
            for (int i = 0; i < data?.Count; i++)
            {
                var row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(data[i].ProductId);
                row.CreateCell(1).SetCellValue(data[i].ProductTitle);
                row.CreateCell(2).SetCellValue(data[i].OrderCount);
            }

            // 自动调整列宽
            for (int i = 0; i < 3; i++)
            {
                sheet.AutoSizeColumn(i);
            }
        }

        private void AddMonthlyTransactionsToPdf(Document document, List<MonthlyTransactionDto> data)
        {
            var font = FontFactory.GetFont("Arial", 14, Font.BOLD);
            document.Add(new Paragraph("月度交易数据", font));

            var table = new PdfPTable(3);
            table.WidthPercentage = 100;

            // 添加表头
            table.AddCell("月份");
            table.AddCell("订单数量");
            table.AddCell("交易总金额");

            // 添加数据
            foreach (var item in data ?? Enumerable.Empty<MonthlyTransactionDto>())
            {
                table.AddCell(item.Month);
                table.AddCell(item.OrderCount.ToString());
                table.AddCell(item.TotalAmount.ToString("C"));
            }

            document.Add(table);
            document.Add(Chunk.NEWLINE);
        }

        private void AddPopularProductsToPdf(Document document, List<PopularProductDto> data)
        {
            var font = FontFactory.GetFont("Arial", 14, Font.BOLD);
            document.Add(new Paragraph("热门商品排行", font));

            var table = new PdfPTable(3);
            table.WidthPercentage = 100;

            // 添加表头
            table.AddCell("商品ID");
            table.AddCell("商品名称");
            table.AddCell("订单数量");

            // 添加数据
            foreach (var item in data ?? Enumerable.Empty<PopularProductDto>())
            {
                table.AddCell(item.ProductId.ToString());
                table.AddCell(item.ProductTitle);
                table.AddCell(item.OrderCount.ToString());
            }

            document.Add(table);
        }
        #endregion
    }
}
