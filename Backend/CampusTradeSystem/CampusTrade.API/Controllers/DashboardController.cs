using Microsoft.AspNetCore.Mvc;
using CampusTrade.API.Models.DTOs;
using CampusTrade.API.Repositories.Interfaces;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CampusTrade.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUserRepository _userRepository;

        public DashboardController(IOrderRepository orderRepository, IUserRepository userRepository)
        {
            _orderRepository = orderRepository;
            _userRepository = userRepository;
        }

        /// <summary>
        /// 获取数据报表统计数据
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<DashboardStatsDto>> GetDashboardStatistics(int year, int activityDays = 30)
        {
            var stats = new DashboardStatsDto();

            // 获取月度交易数据
            stats.MonthlyTransactions = await _orderRepository.GetMonthlyTransactionsAsync(year);

            // 获取热门商品排行（取前10）
            stats.PopularProducts = await _orderRepository.GetPopularProductsAsync(10);

            // 获取用户活跃度数据（最近N天）
            var registrationTrend = await _userRepository.GetUserRegistrationTrendAsync(activityDays);
            var startDate = DateTime.UtcNow.AddDays(-activityDays).Date;

            // 获取所有用户的登录日志来统计活跃用户
            var allUsers = await _userRepository.GetAllAsync();
            var dailyActiveUsers = new Dictionary<DateTime, int>();

            foreach (var user in allUsers)
            {
                var loginLogs = await _userRepository.GetLoginLogsAsync(user.UserId);
                foreach (var log in loginLogs)
                {
                    var logDate = log.LogTime.Date;
                    if (logDate >= startDate)
                    {
                        if (!dailyActiveUsers.ContainsKey(logDate))
                        {
                            dailyActiveUsers[logDate] = 0;
                        }
                        dailyActiveUsers[logDate]++;
                    }
                }
            }

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

            return Ok(stats);
        }

        /// <summary>
        /// 导出Excel报表
        /// </summary>
        [HttpGet("export/excel")]
        public async Task<IActionResult> ExportToExcel(int year)
        {
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
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"校园交易统计_{year}.xlsx");
            }
        }

        /// <summary>
        /// 导出PDF报表
        /// </summary>
        [HttpGet("export/pdf")]
        public async Task<IActionResult> ExportToPdf(int year)
        {
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
                return File(stream.ToArray(), "application/pdf", $"校园交易统计_{year}.pdf");
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
            for (int i = 0; i < data.Count; i++)
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
            for (int i = 0; i < data.Count; i++)
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
            foreach (var item in data)
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
            foreach (var item in data)
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
