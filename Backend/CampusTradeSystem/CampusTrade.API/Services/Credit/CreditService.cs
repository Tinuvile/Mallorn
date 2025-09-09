using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CampusTrade.API.Data;
using CampusTrade.API.Models.DTOs;      // 引入 CreditEvent
using CampusTrade.API.Models.Entities;
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
        private readonly CampusTrade.API.Services.Notification.NotifiService _notificationService;

        public CreditService(CampusTradeDbContext context, ILogger<CreditService> logger, CampusTrade.API.Services.Notification.NotifiService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
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

                    // 发送信用分变更通知
                    try
                    {
                        var changeTypeDisplayName = GetCreditEventTypeDisplayName(creditEvent.EventType);
                        var reason = GetCreditChangeReason(creditEvent.EventType, creditEvent.Description);
                        var changeValueText = delta > 0 ? $"+{delta}" : delta.ToString();

                        var notificationParams = new Dictionary<string, object>
                        {
                            ["changeType"] = changeTypeDisplayName,
                            ["changeValue"] = changeValueText,
                            ["newScore"] = newScore,
                            ["reason"] = reason
                        };

                        await _notificationService.CreateNotificationAsync(
                            user.UserId,
                            29, // 信用分变更模板ID
                            notificationParams
                        );

                        _logger.LogInformation("信用分变更通知已发送，用户ID: {UserId}，变更类型: {EventType}，变更值: {Delta}",
                            user.UserId, creditEvent.EventType, delta);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "发送信用分变更通知失败，用户ID: {UserId}，变更类型: {EventType}",
                            user.UserId, creditEvent.EventType);
                        // 注意：通知发送失败不应该影响信用分变更操作，所以这里只记录日志
                    }

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
            CreditEventType.LightReportPenalty => -5m,      // 轻度处罚 -5分
            CreditEventType.ModerateReportPenalty => -10m,  // 中度处罚 -10分  
            CreditEventType.SevereReportPenalty => -15m,    // 重度处罚 -15分
            CreditEventType.NegativeReviewPenalty => -5m,
            _ => 0m
        };

        /// <summary>
        /// 获取信用事件类型的用户友好显示名称
        /// </summary>
        private static string GetCreditEventTypeDisplayName(CreditEventType eventType) => eventType switch
        {
            CreditEventType.TransactionCompleted => "交易完成奖励",
            CreditEventType.PositiveReviewReward => "好评奖励",
            CreditEventType.LightReportPenalty => "轻度违规处罚",
            CreditEventType.ModerateReportPenalty => "中度违规处罚",
            CreditEventType.SevereReportPenalty => "严重违规处罚",
            CreditEventType.NegativeReviewPenalty => "差评扣分",
            CreditEventType.ReportPenalty => "举报处罚",
            _ => "信用调整"
        };

        /// <summary>
        /// 获取信用分变更的原因说明
        /// </summary>
        private static string GetCreditChangeReason(CreditEventType eventType, string? description = null)
        {
            var baseReason = eventType switch
            {
                CreditEventType.TransactionCompleted => "感谢您完成交易，诚信经营！",
                CreditEventType.PositiveReviewReward => "恭喜您收到好评，继续保持优质服务！",
                CreditEventType.LightReportPenalty => "您的行为存在轻微违规，请注意遵守平台规则。",
                CreditEventType.ModerateReportPenalty => "您的行为违反平台规则，请严格遵守相关规定。",
                CreditEventType.SevereReportPenalty => "您的行为严重违反平台规则，如有异议请联系客服申诉。",
                CreditEventType.NegativeReviewPenalty => "您收到了差评，请改进服务质量。",
                CreditEventType.ReportPenalty => "您的行为被举报并经确认违规，请注意规范操作。",
                _ => "信用分已调整。"
            };

            return string.IsNullOrEmpty(description) ? baseReason : $"{baseReason} {description}";
        }
    }
}
