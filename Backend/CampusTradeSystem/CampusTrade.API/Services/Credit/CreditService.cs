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

        public async Task ApplyCreditChangeAsync(CreditEvent creditEvent, bool autoSave = true)
        {
            const int maxRetries = 3;
            var retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    var user = await _context.Users.FindAsync(creditEvent.UserId);
                    if (user == null)
                    {
                        _logger.LogWarning("用户 {UserId} 不存在，无法调整信用分", creditEvent.UserId);
                        return;
                    }

                    // 获取分值增减幅度
                    var delta = GetDelta(creditEvent.EventType);

                    // 获取原分数（用于日志）
                    var oldScore = user.CreditScore;

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

                    // 只有在autoSave为true时才自动保存，否则由调用方控制事务
                    if (autoSave)
                    {
                        await _context.SaveChangesAsync();
                    }

                    _logger.LogInformation("用户 {UserId} 信用变更：{EventType} -> {Delta}，{OldScore} -> {NewScore}",
                        user.UserId, creditEvent.EventType, delta, oldScore, newScore);

                    // 成功执行，退出重试循环
                    break;
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
                {
                    retryCount++;
                    _logger.LogWarning(ex, "信用分更新并发冲突，用户 {UserId}，重试次数 {RetryCount}/{MaxRetries}",
                        creditEvent.UserId, retryCount, maxRetries);

                    if (retryCount >= maxRetries)
                    {
                        _logger.LogError("信用分更新重试失败，用户 {UserId}，已达到最大重试次数", creditEvent.UserId);
                        throw new InvalidOperationException($"信用分更新失败，用户 {creditEvent.UserId} 并发冲突");
                    }

                    // 重新加载实体以获取最新状态
                    foreach (var entry in _context.ChangeTracker.Entries())
                    {
                        await entry.ReloadAsync();
                    }

                    // 短暂延迟后重试
                    await Task.Delay(TimeSpan.FromMilliseconds(100 * retryCount));
                }
            }
        }

        public async Task<decimal?> GetUserCreditScoreAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                return user?.CreditScore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户信用分失败，用户ID: {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> CheckMinCreditRequirementAsync(int userId, decimal minRequiredScore)
        {
            try
            {
                var currentScore = await GetUserCreditScoreAsync(userId);
                return currentScore.HasValue && currentScore.Value >= minRequiredScore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查用户信用分要求失败，用户ID: {UserId}, 要求分数: {MinScore}", userId, minRequiredScore);
                return false;
            }
        }

        private decimal GetDelta(CreditEventType type) => type switch
        {
            CreditEventType.TransactionCompleted => +5m,
            CreditEventType.PositiveReviewReward => +3m,
            CreditEventType.ReportPenalty => -10m,
            CreditEventType.NegativeReviewPenalty => -5m,
            _ => 0m
        };
    }
}
