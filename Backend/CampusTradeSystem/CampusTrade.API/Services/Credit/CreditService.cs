using System;
using System.Threading.Tasks;
using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Models.DTOs;      // 引入 CreditEvent
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services
{
    /// <summary>
    /// 信用分服务实现类
    /// 处理用户信用分数的增减逻辑，并记录历史
    /// </summary>
    public class CreditService : ICreditService

    {
        private readonly CampusTradeDbContext _context;
        private readonly ILogger<CreditService> _logger;

        public CreditService(CampusTradeDbContext context, ILogger<CreditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ApplyCreditChangeAsync(CreditEvent creditEvent)
        {
            var user = await _context.Users.FindAsync(creditEvent.UserId);
            if (user == null)
            {
                _logger.LogWarning("用户 {UserId} 不存在，无法调整信用分", creditEvent.UserId);
                return;
            }

            // 获取分值增减幅度
            var delta = GetDelta(creditEvent.EventType);

            // 限制在 0~130 范围内
            var newScore = Math.Clamp(user.CreditScore + delta, 0, 130);

            user.CreditScore = newScore;

            // 写入变更记录，使用统一映射
            _context.CreditHistory.Add(new CreditHistory
            {
                UserId = user.UserId,
                ChangeType = CreditEventTypeMapper.ToDbValue(creditEvent.EventType),
                NewScore = newScore,
                CreatedAt = creditEvent.OccurredAt
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation("用户 {UserId} 信用变更：{EventType} -> {Delta}，当前分数为 {NewScore}",
                user.UserId, creditEvent.EventType, delta, newScore);
        }

        private decimal GetDelta(CreditEventType type) => type switch
        {
            CreditEventType.TransactionCompleted    => +5m,
            CreditEventType.PositiveReviewReward    => +3m,
            CreditEventType.ReportPenalty           => -10m,
            _ => 0m
        };
    }
}
