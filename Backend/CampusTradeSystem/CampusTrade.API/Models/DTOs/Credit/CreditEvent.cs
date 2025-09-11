using System;
using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Models.DTOs
{
    /// <summary>
    /// 表示一次信用事件，用于信用服务中记录信用分变动
    /// </summary>
    public class CreditEvent
    {
        /// <summary>
        /// 受影响的用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 事件类型（枚举），如：交易完成、好评奖励、举报处罚等
        /// </summary>
        public CreditEventType EventType { get; set; }

        /// <summary>
        /// 事件发生时间（默认为当前时间）
        /// </summary>
        public DateTime OccurredAt { get; set; } = TimeHelper.UtcNow;

        /// <summary>
        /// 可选的事件说明，仅用于日志记录或审计展示
        /// </summary>
        public string? Description { get; set; }
    }
}
