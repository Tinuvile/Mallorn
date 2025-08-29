using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CampusTrade.API.Models.DTOs.Report;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services;
using CampusTrade.API.Services.Report;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services;

public class ReportServiceTests
{
    private readonly Mock<IReportsRepository> _mockRepo;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<ILogger<ReportService>> _mockLogger;
    private readonly ReportService _service;
    private readonly Mock<ICreditService> _mockCreditService;


    public ReportServiceTests()
    {
        _mockRepo = new Mock<IReportsRepository>();
        _mockUow = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<ReportService>>();
        _mockUow.Setup(x => x.Reports).Returns(_mockRepo.Object);
        _service = new ReportService(_mockRepo.Object, _mockUow.Object, _mockLogger.Object);
        _mockCreditService = new Mock<ICreditService>();
    }

    [Fact]
    public async Task CreateReportAsync_Success()
    {
        _mockRepo.Setup(x => x.GetByOrderIdAsync(1)).ReturnsAsync(new List<Reports>());
        _mockRepo.Setup(x => x.AddAsync(It.IsAny<Reports>())).ReturnsAsync(new Reports { ReportId = 1 });
        _mockUow.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        var (success, message, reportId) = await _service.CreateReportAsync(1, 2, "欺诈", "desc", null);
        Assert.True(success);
        Assert.Contains("举报提交成功", message);
        Assert.NotNull(reportId);
    }

    [Fact]
    public async Task CreateReportAsync_DuplicateReport_Fail()
    {
        _mockRepo.Setup(x => x.GetByOrderIdAsync(1)).ReturnsAsync(new List<Reports> { new Reports { ReporterId = 2, Type = "欺诈" } });
        var (success, message, reportId) = await _service.CreateReportAsync(1, 2, "欺诈", "desc", null);
        Assert.False(success);
        Assert.Contains("已经对此订单提交过举报", message);
    }

    [Fact]
    public async Task CreateReportAsync_InvalidType_Fail()
    {
        _mockRepo.Setup(x => x.GetByOrderIdAsync(1)).ReturnsAsync(new List<Reports>());
        var (success, message, reportId) = await _service.CreateReportAsync(1, 2, "无效类型", "desc", null);
        Assert.False(success);
        Assert.Contains("无效的举报类型", message);
    }

    [Fact]
    public async Task GetUserReportsAsync_ReturnsList()
    {
        _mockRepo.Setup(x => x.GetByReporterIdAsync(2)).ReturnsAsync(new List<Reports> { new Reports { ReportId = 1, ReporterId = 2, Type = "欺诈", Status = "待处理" } });
        var (list, total) = await _service.GetUserReportsAsync(2, 0, 10);
        Assert.Single(list);
        Assert.Equal(1, total);
    }

    [Fact]
    public async Task GetReportDetailsAsync_PermissionDenied()
    {
        _mockRepo.Setup(x => x.GetReportWithDetailsAsync(1)).ReturnsAsync(new Reports { ReportId = 1, ReporterId = 2 });
        var result = await _service.GetReportDetailsAsync(1, 3);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetReportDetailsAsync_Success()
    {
        _mockRepo.Setup(x => x.GetReportWithDetailsAsync(1)).ReturnsAsync(new Reports { ReportId = 1, ReporterId = 2, Type = "欺诈", Status = "待处理", CreateTime = DateTime.Now });
        var result = await _service.GetReportDetailsAsync(1, 2);
        Assert.NotNull(result);
        Assert.Equal(1, result!.ReportId);
    }

    [Fact]
    public async Task AddReportEvidenceAsync_Success()
    {
        _mockRepo.Setup(x => x.GetByPrimaryKeyAsync(1)).ReturnsAsync(new Reports { ReportId = 1, Status = "待处理" });
        _mockRepo.Setup(x => x.AddReportEvidenceAsync(1, "图片", "url")).Returns(Task.CompletedTask);
        _mockUow.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        var files = new List<EvidenceFileInfo> { new EvidenceFileInfo { FileType = "图片", FileUrl = "url" } };
        var (success, message) = await _service.AddReportEvidenceAsync(1, files);
        Assert.True(success);
    }

    [Fact]
    public async Task AddReportEvidenceAsync_ReportNotFound()
    {
        _mockRepo.Setup(x => x.GetReportWithDetailsAsync(1)).ReturnsAsync((Reports?)null);
        var files = new List<EvidenceFileInfo> { new EvidenceFileInfo { FileType = "图片", FileUrl = "url" } };
        var (success, message) = await _service.AddReportEvidenceAsync(1, files);
        Assert.False(success);
        Assert.Contains("举报不存在", message);
    }

    [Fact]
    public async Task CancelReportAsync_Success()
    {
        _mockRepo.Setup(x => x.GetByPrimaryKeyAsync(1)).ReturnsAsync(new Reports { ReportId = 1, ReporterId = 2, Status = "待处理" });
        _mockRepo.Setup(x => x.UpdateReportStatusAsync(1, "已关闭")).ReturnsAsync(true);
        _mockUow.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        var (success, message) = await _service.CancelReportAsync(1, 2);
        Assert.True(success);
    }

    [Fact]
    public async Task CancelReportAsync_PermissionDenied()
    {
        _mockRepo.Setup(x => x.GetByPrimaryKeyAsync(1)).ReturnsAsync(new Reports { ReportId = 1, ReporterId = 2 });
        var (success, message) = await _service.CancelReportAsync(1, 3);
        Assert.False(success);
        Assert.Contains("无权限操作此举报", message);
    }
}
