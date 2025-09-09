
namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 信用事件类型枚举
    /// 对应数据库 CREDIT_HISTORY 表中的 CHANGE_TYPE 字段
    /// </summary>
    public enum CreditEventType
    {
        /// <summary>
        /// 交易完成 -> 数据库值: "交易完成"
        /// </summary>
        TransactionCompleted,

        /// <summary>
        /// 举报处罚 -> 数据库值: "举报处罚"
        /// </summary>
        ReportPenalty,

        /// <summary>
        /// 轻度举报处罚 -> 数据库值: "举报处罚" (-5分)
        /// </summary>
        LightReportPenalty,

        /// <summary>
        /// 中度举报处罚 -> 数据库值: "举报处罚" (-10分)
        /// </summary>
        ModerateReportPenalty,

        /// <summary>
        /// 重度举报处罚 -> 数据库值: "举报处罚" (-15分)
        /// </summary>
        SevereReportPenalty,

        /// <summary>
        /// 好评奖励 -> 数据库值: "好评奖励"
        /// </summary>
        PositiveReviewReward,

        /// <summary>
        /// 差评惩罚
        /// </summary>
        NegativeReviewPenalty
    }

    /// <summary>
    /// 枚举与数据库常量的映射工具
    /// </summary>
    public static class CreditEventTypeMapper
    {
        public static string ToDbValue(CreditEventType eventType) => eventType switch
        {
            CreditEventType.TransactionCompleted => CreditHistory.ChangeTypes.TransactionCompleted,
            CreditEventType.ReportPenalty => CreditHistory.ChangeTypes.ReportPenalty,
            CreditEventType.LightReportPenalty => CreditHistory.ChangeTypes.ReportPenalty,
            CreditEventType.ModerateReportPenalty => CreditHistory.ChangeTypes.ReportPenalty,
            CreditEventType.SevereReportPenalty => CreditHistory.ChangeTypes.ReportPenalty,
            CreditEventType.PositiveReviewReward => CreditHistory.ChangeTypes.PositiveReviewReward,
            CreditEventType.NegativeReviewPenalty => CreditHistory.ChangeTypes.NegativeReviewPenalty,
            _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null)
        };

        public static CreditEventType FromDbValue(string dbValue) => dbValue switch
        {
            CreditHistory.ChangeTypes.TransactionCompleted => CreditEventType.TransactionCompleted,
            CreditHistory.ChangeTypes.ReportPenalty => CreditEventType.ReportPenalty,
            CreditHistory.ChangeTypes.PositiveReviewReward => CreditEventType.PositiveReviewReward,
            CreditHistory.ChangeTypes.NegativeReviewPenalty => CreditEventType.NegativeReviewPenalty,
            _ => throw new ArgumentOutOfRangeException(nameof(dbValue), dbValue, null)
        };
    }
}
