using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CampusTrade.API.Infrastructure.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        // 分组推送支持：根据用户ID，将连接加入组（Group）方便单用户/多端推送
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"NotificationHub - 客户端连接 - ConnectionID: {Context.ConnectionId}");

            var userId = Context.UserIdentifier ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation($"NotificationHub - 用户 {userId} 加入组 user_{userId}");
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            else
            {
                _logger.LogWarning($"NotificationHub - 连接 {Context.ConnectionId} 没有用户标识");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"NotificationHub - 客户端断开连接 - ConnectionID: {Context.ConnectionId}, 异常: {exception?.Message ?? "无"}");

            var userId = Context.UserIdentifier ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation($"NotificationHub - 用户 {userId} 离开组 user_{userId}");
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // 提供一个简单的测试方法，客户端可以调用此方法来验证连接是否正常
        public async Task SendTestMessage(string message)
        {
            var userId = Context.UserIdentifier ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation($"NotificationHub - 收到测试消息 - 用户: {userId ?? "未知"}, 消息: {message}");

            // 发送回客户端确认
            await Clients.Caller.SendAsync("ReceiveTestResponse", $"服务器收到消息: {message}，时间: {DateTime.Now}");
        }
    }
}
