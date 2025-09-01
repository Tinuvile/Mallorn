using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CampusTrade.API.Controllers;
using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Models.DTOs.Report;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Services.Interfaces;
using CampusTrade.API.Services.Report;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Controllers;

public class ReportControllerTests
{
    private readonly Mock<IReportService> _mockReportService;
    private readonly Mock<ILogger<ReportController>> _mockLogger;
    private readonly ReportController _controller;

    public ReportControllerTests()
    {
        _mockReportService = new Mock<IReportService>();
        _mockLogger = new Mock<ILogger<ReportController>>();
        _controller = new ReportController(_mockReportService.Object, _mockLogger.Object);
        // 设置模拟用户身份
        var claims = new List<Claim> { new Claim("userId", "1") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task CreateReport_Success_ReturnsOk()
    {
        var dto = new CreateReportDto { OrderId = 1, Type = "欺诈", Description = "test" };
        _mockReportService.Setup(x => x.CreateReportAsync(1, 1, "欺诈", "test", null))
            .ReturnsAsync((true, "举报成功", 123));
        var result = await _controller.CreateReport(dto);
        result.Should().BeOfType<OkObjectResult>();
        var ok = result as OkObjectResult;
        var api = ok!.Value as ApiResponse<object>;
        api!.Success.Should().BeTrue();
        api.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateReport_Fail_ReturnsBadRequest()
    {
        var dto = new CreateReportDto { OrderId = 1, Type = "欺诈", Description = "test" };
        _mockReportService.Setup(x => x.CreateReportAsync(1, 1, "欺诈", "test", null))
            .ReturnsAsync((false, "举报失败", 0));
        var result = await _controller.CreateReport(dto);
        result.Should().BeOfType<BadRequestObjectResult>();
        var bad = result as BadRequestObjectResult;
        var api = bad!.Value as ApiResponse<object>;
        api!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateReport_Unauthorized_ReturnsUnauthorized()
    {
        // 模拟无userId
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();
        var dto = new CreateReportDto { OrderId = 1, Type = "欺诈" };
        var result = await _controller.CreateReport(dto);
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetMyReports_ReturnsList()
    {
        var reports = new List<Reports> {
            new Reports { ReportId = 1, OrderId = 1, Type = "欺诈", Status = "待处理" }
        };
        _mockReportService.Setup(x => x.GetUserReportsAsync(1, 0, 10))
            .ReturnsAsync((reports, 1));
        var result = await _controller.GetMyReports();
        result.Should().BeOfType<OkObjectResult>();
        var ok = result as OkObjectResult;
        var api = ok!.Value as ApiResponse<object>;
        api!.Success.Should().BeTrue();
        api.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetReportDetails_ReturnsDetail()
    {
        var detail = new Reports { ReportId = 1, OrderId = 1, Type = "欺诈", Status = "待处理" };
        _mockReportService.Setup(x => x.GetReportDetailsAsync(1, 1)).ReturnsAsync(detail);
        var result = await _controller.GetReportDetails(1);
        result.Should().BeOfType<OkObjectResult>();
        var ok = result as OkObjectResult;
        var api = ok!.Value as ApiResponse<object>;
        api!.Success.Should().BeTrue();
        api.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetReportDetails_NotFound()
    {
        _mockReportService.Setup(x => x.GetReportDetailsAsync(1, 1)).ReturnsAsync((Reports?)null);
        var result = await _controller.GetReportDetails(1);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddReportEvidence_Success()
    {
        var dto = new CreateReportDto { EvidenceFiles = new List<EvidenceFileDto> { new EvidenceFileDto { FileType = "图片", FileUrl = "url" } } };
        _mockReportService.Setup(x => x.AddReportEvidenceAsync(1, It.IsAny<List<EvidenceFileInfo>>()))
            .ReturnsAsync((true, "添加成功"));
        var result = await _controller.AddReportEvidence(1, dto);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddReportEvidence_Fail()
    {
        var dto = new CreateReportDto { EvidenceFiles = new List<EvidenceFileDto> { new EvidenceFileDto { FileType = "图片", FileUrl = "url" } } };
        _mockReportService.Setup(x => x.AddReportEvidenceAsync(1, It.IsAny<List<EvidenceFileInfo>>()))
            .ReturnsAsync((false, "添加失败"));
        var result = await _controller.AddReportEvidence(1, dto);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CancelReport_Success()
    {
        _mockReportService.Setup(x => x.CancelReportAsync(1, 1)).ReturnsAsync((true, "撤销成功"));
        var result = await _controller.CancelReport(1);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CancelReport_Fail()
    {
        _mockReportService.Setup(x => x.CancelReportAsync(1, 1)).ReturnsAsync((false, "撤销失败"));
        var result = await _controller.CancelReport(1);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void GetReportTypes_ReturnsTypes()
    {
        var result = _controller.GetReportTypes();
        result.Should().BeOfType<OkObjectResult>();
    }
}
