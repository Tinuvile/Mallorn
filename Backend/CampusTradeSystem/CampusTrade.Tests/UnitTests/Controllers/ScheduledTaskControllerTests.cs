using CampusTrade.API.Controllers;
using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Services.ScheduledTasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Controllers
{
    /// <summary>
    /// ScheduledTaskController单元测试
    /// </summary>
    public class ScheduledTaskControllerTests : IDisposable
    {
        private readonly Mock<ILogger<ScheduledTaskController>> _mockLogger;
        private readonly List<IHostedService> _mockHostedServices;
        private readonly ScheduledTaskController _controller;

        public ScheduledTaskControllerTests()
        {
            _mockLogger = new Mock<ILogger<ScheduledTaskController>>();

            // 创建模拟的定时任务服务
            _mockHostedServices = new List<IHostedService>();

            // 添加一些模拟的定时任务
            var mockTokenCleanupTask = new Mock<TokenCleanupTask>(
                Mock.Of<ILogger<TokenCleanupTask>>(),
                Mock.Of<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>());
            mockTokenCleanupTask.Setup(x => x.GetType().Name).Returns("TokenCleanupTask");
            mockTokenCleanupTask.Setup(x => x.GetType().Namespace).Returns("CampusTrade.API.Services.ScheduledTasks");

            var mockLogCleanupTask = new Mock<LogCleanupTask>(
                Mock.Of<ILogger<LogCleanupTask>>(),
                Mock.Of<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>());
            mockLogCleanupTask.Setup(x => x.GetType().Name).Returns("LogCleanupTask");
            mockLogCleanupTask.Setup(x => x.GetType().Namespace).Returns("CampusTrade.API.Services.ScheduledTasks");

            _mockHostedServices.Add(mockTokenCleanupTask.Object);
            _mockHostedServices.Add(mockLogCleanupTask.Object);

            _controller = new ScheduledTaskController(_mockLogger.Object, _mockHostedServices);
        }

        #region GetTaskStatus Tests

        [Fact]
        public void GetTaskStatus_ShouldReturnOkResult_WithTaskStatusList()
        {
            // Act
            var result = _controller.GetTaskStatus();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeOfType<ApiResponse<object>>();

            var apiResponse = okResult.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Message.Should().Contain("获取定时任务状态成功");
            apiResponse.Data.Should().NotBeNull();
        }

        [Fact]
        public void GetTaskStatus_ShouldIncludeCorrectTaskCount()
        {
            // Act
            var result = _controller.GetTaskStatus();

            // Assert
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<object>;
            var data = apiResponse!.Data as dynamic;

            // 验证返回的任务数量与模拟的服务数量匹配
            Assert.Equal(2, data!.TotalTasks);
        }

        #endregion

        #region GetTaskDetail Tests

        [Fact]
        public void GetTaskDetail_WithValidTaskName_ShouldReturnOkResult()
        {
            // Arrange
            var taskName = "TokenCleanupTask";

            // Act
            var result = _controller.GetTaskDetail(taskName);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeOfType<ApiResponse<object>>();

            var apiResponse = okResult.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Message.Should().Contain("获取任务详细信息成功");
            apiResponse.Data.Should().NotBeNull();
        }

        [Fact]
        public void GetTaskDetail_WithInvalidTaskName_ShouldReturnNotFound()
        {
            // Arrange
            var taskName = "NonExistentTask";

            // Act
            var result = _controller.GetTaskDetail(taskName);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().BeOfType<ApiResponse>();

            var apiResponse = notFoundResult.Value as ApiResponse;
            apiResponse!.Success.Should().BeFalse();
            apiResponse.ErrorCode.Should().Be("TASK_NOT_FOUND");
            apiResponse.Message.Should().Contain("未找到名为");
        }

        [Fact]
        public void GetTaskDetail_ShouldBeCaseInsensitive()
        {
            // Arrange
            var taskName = "tokencleanuptask"; // 小写

            // Act
            var result = _controller.GetTaskDetail(taskName);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        #endregion

        #region GetSystemHealth Tests

        [Fact]
        public void GetSystemHealth_ShouldReturnOkResult_WithHealthStatus()
        {
            // Act
            var result = _controller.GetSystemHealth();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeOfType<ApiResponse<object>>();

            var apiResponse = okResult.Value as ApiResponse<object>;
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Message.Should().Contain("系统状态正常");
            apiResponse.Data.Should().NotBeNull();
        }

        [Fact]
        public void GetSystemHealth_ShouldIncludeSystemInfo()
        {
            // Act
            var result = _controller.GetSystemHealth();

            // Assert
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<object>;
            var data = apiResponse!.Data;

            // 检查返回的数据结构
            data.Should().NotBeNull();
            var healthData = data as dynamic;
            Assert.NotNull(healthData!.SystemInfo);
        }

        #endregion

        #region Private Method Tests

        [Theory]
        [InlineData("TokenCleanupTask", "清理过期的刷新令牌")]
        [InlineData("LogCleanupTask", "清理过期的系统日志")]
        [InlineData("ProductManagementTask", "管理商品状态，自动下架过期商品")]
        [InlineData("OrderProcessingTask", "处理订单状态，自动取消超时订单")]
        [InlineData("UnknownTask", "定时任务")]
        public void GetTaskDescription_ShouldReturnCorrectDescription(string taskName, string expectedDescription)
        {
            // 这里假设有一个私有方法 _controller.GetTaskDescription(string) 可用
            // 如果没有，需要用反射调用
            var method = typeof(ScheduledTaskController).GetMethod("GetTaskDescription", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var actualDescription = method?.Invoke(_controller, new object[] { taskName }) as string;
            Assert.Equal(expectedDescription, actualDescription);
        }

        [Fact]
        public void GetNextRunTime_ShouldReturnFutureTime()
        {
            // Act
            var result = _controller.GetTaskStatus();

            // Assert
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<object>;
            var data = apiResponse!.Data as dynamic;
            var tasks = data!.Tasks as IEnumerable<object>;

            // 验证所有任务的下次执行时间都是未来时间
            foreach (var task in tasks!)
            {
                var taskData = task as dynamic;
                var nextRun = (DateTime)taskData!.NextRun;
                nextRun.Should().BeAfter(DateTime.UtcNow);
            }
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ScheduledTaskController(null!, _mockHostedServices));
        }

        [Fact]
        public void Constructor_WithNullHostedServices_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ScheduledTaskController(_mockLogger.Object, null!));
        }

        [Fact]
        public void GetTaskStatus_WithEmptyHostedServices_ShouldReturnEmptyList()
        {
            // Arrange
            var emptyServices = new List<IHostedService>();
            var controller = new ScheduledTaskController(_mockLogger.Object, emptyServices);

            // Act
            var result = controller.GetTaskStatus();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult!.Value as ApiResponse<object>;
            var data = apiResponse!.Data as dynamic;

            Assert.Equal(0, data!.TotalTasks);
        }

        [Fact]
        public void GetTaskDetail_WithEmptyString_ShouldReturnNotFound()
        {
            // Act
            var result = _controller.GetTaskDetail(string.Empty);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        #endregion

        public void Dispose()
        {
            // 清理资源
        }
    }
}
