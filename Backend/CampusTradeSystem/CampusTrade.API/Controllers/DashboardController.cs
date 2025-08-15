using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CampusTrade.API.Models.DTOs;
using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Repositories.Interfaces;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

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
                    var writer = new PdfWriter(stream);
                    var pdf = new PdfDocument(writer);
                    var document = new Document(pdf);

                    // 创建中文字体
                    PdfFont chineseFont;
                    try
                    {
                        // 尝试使用 iText7 的亚洲字体支持
                        chineseFont = PdfFontFactory.CreateFont("STSong-Light", "UniGB-UCS2-H");
                    }
                    catch
                    {
                        try
                        {
                            // 备选方案：使用内置字体
                            chineseFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                        }
                        catch
                        {
                            // 最后备选
                            chineseFont = PdfFontFactory.CreateFont();
                        }
                    }

                    // 添加标题
                    var title = new Paragraph($"校园交易平台统计报表 - {year}")
                        .SetFont(chineseFont)
                        .SetFontSize(18)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetBold();
                    document.Add(title);
                    document.Add(new Paragraph(""));

                    // 添加月度交易数据
                    AddMonthlyTransactionsToPdf(document, stats.MonthlyTransactions, chineseFont);

                    // 添加热门商品数据
                    AddPopularProductsToPdf(document, stats.PopularProducts, chineseFont);

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

            // 手动设置列宽（避免AutoSizeColumn在Docker环境中的字体问题）
            sheet.SetColumnWidth(0, 3000);  // 月份列
            sheet.SetColumnWidth(1, 3000);  // 订单数量列
            sheet.SetColumnWidth(2, 4000);  // 交易总金额列
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

            // 手动设置列宽（避免AutoSizeColumn在Docker环境中的字体问题）
            sheet.SetColumnWidth(0, 3000);  // 商品ID列
            sheet.SetColumnWidth(1, 6000);  // 商品名称列
            sheet.SetColumnWidth(2, 3000);  // 订单数量列
        }

        private void AddMonthlyTransactionsToPdf(Document document, List<MonthlyTransactionDto> data, PdfFont font)
        {
            document.Add(new Paragraph("月度交易数据")
                .SetFont(font)
                .SetFontSize(14)
                .SetBold());

            var table = new iText.Layout.Element.Table(3);
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // 添加表头
            table.AddHeaderCell(new Cell().Add(new Paragraph("月份").SetFont(font)).SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY));
            table.AddHeaderCell(new Cell().Add(new Paragraph("订单数量").SetFont(font)).SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY));
            table.AddHeaderCell(new Cell().Add(new Paragraph("交易总金额").SetFont(font)).SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY));

            // 添加数据
            foreach (var item in data ?? Enumerable.Empty<MonthlyTransactionDto>())
            {
                table.AddCell(new Cell().Add(new Paragraph(item.Month).SetFont(font)));
                table.AddCell(new Cell().Add(new Paragraph(item.OrderCount.ToString()).SetFont(font)));
                table.AddCell(new Cell().Add(new Paragraph(item.TotalAmount.ToString("C")).SetFont(font)));
            }

            document.Add(table);
            document.Add(new Paragraph(""));
        }

        private void AddPopularProductsToPdf(Document document, List<PopularProductDto> data, PdfFont font)
        {
            document.Add(new Paragraph("热门商品排行")
                .SetFont(font)
                .SetFontSize(14)
                .SetBold());

            var table = new iText.Layout.Element.Table(3);
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // 添加表头
            table.AddHeaderCell(new Cell().Add(new Paragraph("商品ID").SetFont(font)).SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY));
            table.AddHeaderCell(new Cell().Add(new Paragraph("商品名称").SetFont(font)).SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY));
            table.AddHeaderCell(new Cell().Add(new Paragraph("订单数量").SetFont(font)).SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY));

            // 添加数据
            foreach (var item in data ?? Enumerable.Empty<PopularProductDto>())
            {
                table.AddCell(new Cell().Add(new Paragraph(item.ProductId.ToString()).SetFont(font)));
                table.AddCell(new Cell().Add(new Paragraph(item.ProductTitle ?? "").SetFont(font)));
                table.AddCell(new Cell().Add(new Paragraph(item.OrderCount.ToString()).SetFont(font)));
            }

            document.Add(table);
        }
        #endregion
    }
}
