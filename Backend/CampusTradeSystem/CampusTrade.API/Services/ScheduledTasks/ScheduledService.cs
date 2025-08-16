using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.ScheduledTasks
{
    public abstract class ScheduledService : IHostedService, IDisposable
    {
        protected readonly ILogger _logger;
        private Timer? _timer;
        private bool _disposed = false;
        private readonly SemaphoreSlim _executionSemaphore = new(1, 1);

        // 任务状态跟踪
        public DateTime? LastExecutionTime { get; private set; }
        public DateTime? NextExecutionTime { get; private set; }
        public bool IsExecuting { get; private set; }
        public int ExecutionCount { get; private set; }
        public Exception? LastError { get; private set; }

        protected abstract TimeSpan Interval { get; }

        public ScheduledService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            var taskName = GetType().Name;
            _logger.LogInformation("定时任务 {TaskName} 开始启动", taskName);

            NextExecutionTime = DateTime.UtcNow.Add(Interval);
            _timer = new Timer(ExecuteTaskWrapper, null, TimeSpan.Zero, Interval);

            _logger.LogInformation("定时任务 {TaskName} 启动完成，执行间隔: {Interval}", taskName, Interval);
            return Task.CompletedTask;
        }

        private async void ExecuteTaskWrapper(object? state)
        {
            // 防止并发执行
            if (!await _executionSemaphore.WaitAsync(100))
            {
                _logger.LogWarning("定时任务 {TaskName} 已在执行中，跳过本次执行", GetType().Name);
                return;
            }

            try
            {
                IsExecuting = true;
                var taskName = GetType().Name;
                var startTime = DateTime.UtcNow;

                _logger.LogInformation("定时任务 {TaskName} 开始执行 (第 {ExecutionCount} 次)", taskName, ExecutionCount + 1);

                try
                {
                    await ExecuteTaskAsync();

                    LastExecutionTime = startTime;
                    NextExecutionTime = DateTime.UtcNow.Add(Interval);
                    ExecutionCount++;
                    LastError = null;

                    var duration = DateTime.UtcNow - startTime;
                    _logger.LogInformation("定时任务 {TaskName} 执行完成，耗时: {Duration}ms",
                        taskName, duration.TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    LastError = ex;
                    NextExecutionTime = DateTime.UtcNow.Add(Interval);

                    _logger.LogError(ex, "定时任务 {TaskName} 执行出错", taskName);

                    // 可以在这里添加重试逻辑或错误通知
                    await OnTaskErrorAsync(ex);
                }
            }
            finally
            {
                IsExecuting = false;
                _executionSemaphore.Release();
            }
        }

        protected abstract Task ExecuteTaskAsync();

        /// <summary>
        /// 任务执行出错时的处理方法，子类可重写
        /// </summary>
        protected virtual Task OnTaskErrorAsync(Exception exception)
        {
            // 默认空实现，子类可以重写来处理特定的错误逻辑
            return Task.CompletedTask;
        }

        /// <summary>
        /// 获取任务状态信息
        /// </summary>
        public virtual object GetTaskStatus()
        {
            return new
            {
                TaskName = GetType().Name,
                IsExecuting,
                LastExecutionTime,
                NextExecutionTime,
                ExecutionCount,
                Interval = Interval.ToString(),
                LastError = LastError?.Message,
                Status = IsExecuting ? "执行中" : (LastError != null ? "错误" : "正常")
            };
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            var taskName = GetType().Name;
            _logger.LogInformation("定时任务 {TaskName} 开始停止", taskName);

            _timer?.Change(Timeout.Infinite, 0);

            _logger.LogInformation("定时任务 {TaskName} 已停止", taskName);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _timer?.Dispose();
                _executionSemaphore?.Dispose();
                _disposed = true;

                _logger.LogInformation("定时任务 {TaskName} 资源已释放", GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "释放定时任务 {TaskName} 资源时发生错误", GetType().Name);
            }
        }
    }
}
