using System;
using System.Collections.Generic; // Added for IEnumerable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq; // Added for Where, Select, ToList, Average

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 评价实体类
    /// </summary>
    [Table("REVIEWS")]
    public class Review
    {
        #region 常量定义
        // 评分范围
        public const decimal MinRating = 1.0m;
        public const decimal MaxRating = 5.0m;
        public const int MinDetailRating = 1;
        public const int MaxDetailRating = 5;

        // 评价等级
        public static class RatingLevels
        {
            public const string Excellent = "非常好";    // 5分
            public const string Good = "好";             // 4分
            public const string Average = "一般";        // 3分
            public const string Poor = "差";             // 2分
            public const string VeryPoor = "非常差";     // 1分
        }

        // 内容长度限制
        public const int MaxContentLength = 1000;
        public const int MaxReplyLength = 500;
        #endregion

        #region 基本信息
        /// <summary>
        /// 评价ID
        /// </summary>
        [Key]
        [Column("REVIEW_ID", TypeName = "NUMBER")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReviewId { get; set; }

        /// <summary>
        /// 订单ID（外键，关联抽象订单）
        /// </summary>
        [Required]
        [Column("ORDER_ID", TypeName = "NUMBER")]
        public int OrderId { get; set; }

        /// <summary>
        /// 总体评分（1-5分，支持一位小数）
        /// </summary>
        [Column("RATING", TypeName = "NUMBER(2,1)")]
        [Range(1.0, 5.0, ErrorMessage = "评分必须在1.0到5.0之间")]
        public decimal? Rating { get; set; }

        /// <summary>
        /// 描述准确性评分（1-5分）
        /// </summary>
        [Column("DESC_ACCURACY", TypeName = "NUMBER(2,0)")]
        [Range(1, 5, ErrorMessage = "描述准确性评分必须在1到5之间")]
        public int? DescAccuracy { get; set; }

        /// <summary>
        /// 服务态度评分（1-5分）
        /// </summary>
        [Column("SERVICE_ATTITUDE", TypeName = "NUMBER(2,0)")]
        [Range(1, 5, ErrorMessage = "服务态度评分必须在1到5之间")]
        public int? ServiceAttitude { get; set; }

        /// <summary>
        /// 是否匿名评价
        /// </summary>
        [Required]
        [Column("IS_ANONYMOUS", TypeName = "NUMBER")]
        public int IsAnonymous { get; set; } = 0;

        /// <summary>
        /// 评价创建时间
        /// </summary>
        [Required]
        [Column("CREATE_TIME", TypeName = "TIMESTAMP")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 卖家回复
        /// </summary>
        [Column("SELLER_REPLY", TypeName = "CLOB")]
        [MaxLength(MaxReplyLength)]
        public string? SellerReply { get; set; }

        /// <summary>
        /// 评价内容
        /// </summary>
        [Column("CONTENT", TypeName = "CLOB")]
        [MaxLength(MaxContentLength)]
        public string? Content { get; set; }
        #endregion

        #region 导航属性
        /// <summary>
        /// 关联的抽象订单
        /// </summary>
        public virtual AbstractOrder AbstractOrder { get; set; } = null!;
        #endregion

        #region 计算属性
        /// <summary>
        /// 是否为匿名评价（Boolean形式）
        /// </summary>
        [NotMapped]
        public bool IsAnonymousReview => IsAnonymous == 1;

        /// <summary>
        /// 是否有卖家回复
        /// </summary>
        [NotMapped]
        public bool HasSellerReply => !string.IsNullOrWhiteSpace(SellerReply);

        /// <summary>
        /// 是否为正面评价（评分>=4）
        /// </summary>
        [NotMapped]
        public bool IsPositiveReview => Rating >= 4.0m;

        /// <summary>
        /// 是否为负面评价（评分<=2）
        /// </summary>
        [NotMapped]
        public bool IsNegativeReview => Rating <= 2.0m;

        /// <summary>
        /// 是否为完整评价（包含所有评分）
        /// </summary>
        [NotMapped]
        public bool IsCompleteReview => Rating.HasValue && DescAccuracy.HasValue && ServiceAttitude.HasValue;

        /// <summary>
        /// 综合满意度分数（三项评分的平均值）
        /// </summary>
        [NotMapped]
        public decimal? OverallSatisfaction
        {
            get
            {
                if (!IsCompleteReview) return null;
                return (Rating!.Value + DescAccuracy!.Value + ServiceAttitude!.Value) / 3.0m;
            }
        }
        #endregion

        #region 业务方法
        /// <summary>
        /// 设置匿名状态
        /// </summary>
        public void SetAnonymous(bool isAnonymous)
        {
            IsAnonymous = isAnonymous ? 1 : 0;
        }

        /// <summary>
        /// 添加卖家回复
        /// </summary>
        public void AddSellerReply(string reply)
        {
            if (string.IsNullOrWhiteSpace(reply))
                throw new ArgumentException("回复内容不能为空", nameof(reply));

            if (reply.Length > MaxReplyLength)
                throw new ArgumentException($"回复内容不能超过{MaxReplyLength}个字符", nameof(reply));

            SellerReply = reply;
        }

        /// <summary>
        /// 获取评分等级描述
        /// </summary>
        public string GetRatingLevelDescription()
        {
            if (!Rating.HasValue) return "未评分";

            return Rating.Value switch
            {
                >= 4.5m => RatingLevels.Excellent,
                >= 3.5m => RatingLevels.Good,
                >= 2.5m => RatingLevels.Average,
                >= 1.5m => RatingLevels.Poor,
                _ => RatingLevels.VeryPoor
            };
        }

        /// <summary>
        /// 获取描述准确性等级
        /// </summary>
        public string GetDescAccuracyLevel()
        {
            if (!DescAccuracy.HasValue) return "未评分";

            return DescAccuracy.Value switch
            {
                5 => "非常准确",
                4 => "比较准确",
                3 => "一般",
                2 => "不太准确",
                1 => "很不准确",
                _ => "无效评分"
            };
        }

        /// <summary>
        /// 获取服务态度等级
        /// </summary>
        public string GetServiceAttitudeLevel()
        {
            if (!ServiceAttitude.HasValue) return "未评分";

            return ServiceAttitude.Value switch
            {
                5 => "非常好",
                4 => "比较好",
                3 => "一般",
                2 => "不太好",
                1 => "很差",
                _ => "无效评分"
            };
        }

        /// <summary>
        /// 验证评分是否有效
        /// </summary>
        public bool IsValidRating()
        {
            return Rating >= MinRating && Rating <= MaxRating;
        }

        /// <summary>
        /// 验证详细评分是否有效
        /// </summary>
        public bool AreDetailRatingsValid()
        {
            return (!DescAccuracy.HasValue || (DescAccuracy >= MinDetailRating && DescAccuracy <= MaxDetailRating)) &&
                   (!ServiceAttitude.HasValue || (ServiceAttitude >= MinDetailRating && ServiceAttitude <= MaxDetailRating));
        }

        /// <summary>
        /// 获取评价时长描述
        /// </summary>
        public string GetTimeAgoDescription()
        {
            var timeDiff = DateTime.Now - CreateTime;

            if (timeDiff.TotalMinutes < 1)
                return "刚刚";
            if (timeDiff.TotalMinutes < 60)
                return $"{(int)timeDiff.TotalMinutes}分钟前";
            if (timeDiff.TotalHours < 24)
                return $"{(int)timeDiff.TotalHours}小时前";
            if (timeDiff.TotalDays < 30)
                return $"{(int)timeDiff.TotalDays}天前";
            return CreateTime.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// 获取评价摘要
        /// </summary>
        public string GetReviewSummary()
        {
            var summary = GetRatingLevelDescription();
            if (!string.IsNullOrWhiteSpace(Content))
            {
                var maxLength = 30;
                var contentSummary = Content.Length <= maxLength ? Content : Content.Substring(0, maxLength) + "...";
                summary += $" - {contentSummary}";
            }
            return summary;
        }

        /// <summary>
        /// 是否为高质量评价（有详细内容且评分完整）
        /// </summary>
        public bool IsHighQualityReview()
        {
            return IsCompleteReview &&
                   !string.IsNullOrWhiteSpace(Content) &&
                   Content.Length >= 10; // 至少10个字符的评价内容
        }

        /// <summary>
        /// 获取评价完整度百分比
        /// </summary>
        public int GetCompletenessPercentage()
        {
            var score = 0;
            var maxScore = 5;

            if (Rating.HasValue) score++;
            if (DescAccuracy.HasValue) score++;
            if (ServiceAttitude.HasValue) score++;
            if (!string.IsNullOrWhiteSpace(Content)) score++;
            if (!string.IsNullOrWhiteSpace(SellerReply)) score++;

            return (int)((double)score / maxScore * 100);
        }
        #endregion

        #region 静态方法
        /// <summary>
        /// 验证评分范围是否有效
        /// </summary>
        public static bool IsValidRatingRange(decimal rating)
        {
            return rating >= MinRating && rating <= MaxRating;
        }

        /// <summary>
        /// 验证详细评分范围是否有效
        /// </summary>
        public static bool IsValidDetailRatingRange(int rating)
        {
            return rating >= MinDetailRating && rating <= MaxDetailRating;
        }

        /// <summary>
        /// 创建完整评价
        /// </summary>
        public static Review CreateCompleteReview(int orderId, decimal rating, int descAccuracy,
            int serviceAttitude, string content, bool isAnonymous = false)
        {
            return new Review
            {
                OrderId = orderId,
                Rating = rating,
                DescAccuracy = descAccuracy,
                ServiceAttitude = serviceAttitude,
                Content = content,
                IsAnonymous = isAnonymous ? 1 : 0,
                CreateTime = DateTime.Now
            };
        }

        /// <summary>
        /// 创建快速评价（仅总体评分）
        /// </summary>
        public static Review CreateQuickReview(int orderId, decimal rating, bool isAnonymous = false)
        {
            return new Review
            {
                OrderId = orderId,
                Rating = rating,
                IsAnonymous = isAnonymous ? 1 : 0,
                CreateTime = DateTime.Now
            };
        }

        /// <summary>
        /// 创建匿名评价
        /// </summary>
        public static Review CreateAnonymousReview(int orderId, decimal rating, string content)
        {
            return new Review
            {
                OrderId = orderId,
                Rating = rating,
                Content = content,
                IsAnonymous = 1,
                CreateTime = DateTime.Now
            };
        }

        /// <summary>
        /// 获取评分对应的星级显示
        /// </summary>
        public static string GetStarRating(decimal? rating)
        {
            if (!rating.HasValue) return "☆☆☆☆☆";

            var fullStars = (int)Math.Floor(rating.Value);
            var hasHalfStar = rating.Value - fullStars >= 0.5m;
            var emptyStars = 5 - fullStars - (hasHalfStar ? 1 : 0);

            return new string('★', fullStars) +
                   (hasHalfStar ? "☆" : "") +
                   new string('☆', emptyStars);
        }

        /// <summary>
        /// 计算多个评价的平均评分
        /// </summary>
        public static decimal CalculateAverageRating(IEnumerable<Review> reviews)
        {
            var validRatings = reviews.Where(r => r.Rating.HasValue).Select(r => r.Rating.Value).ToList();
            return validRatings.Count == 0 ? 0 : validRatings.Average();
        }
        #endregion
    }
}
