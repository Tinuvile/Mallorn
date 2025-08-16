using System;

namespace CampusTrade.API.Models.DTOs
{
    /// <summary>
    /// 通知发送结果结构
    /// </summary>
    public class NotificationSendResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public DateTime SendTime { get; set; }
    }
}
