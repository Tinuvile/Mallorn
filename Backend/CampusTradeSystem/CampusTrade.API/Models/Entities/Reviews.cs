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

        /// <summary>
        /// 评价ID（由Oracle序列和触发器生成）
        /// </summary>
        [Key]
        [Column("REVIEW_ID")]
        public int ReviewId { get; set; }

        /// <summary>
        /// 订单ID（外键，关联抽象订单）
        /// </summary>
        [Required]
        [Column("ORDER_ID")]
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
        /// 是否匿名评价（默认值由Oracle处理）
        /// </summary>
        [Required]
        [Column("IS_ANONYMOUS")]
        public int IsAnonymous { get; set; } = 0;

        /// <summary>
        /// 评价创建时间（由Oracle DEFAULT处理）
        /// </summary>
        [Required]
        [Column("CREATE_TIME")]
        public DateTime CreateTime { get; set; }

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

        /// <summary>
        /// 关联的抽象订单
        /// </summary>
        public virtual AbstractOrder AbstractOrder { get; set; } = null!;
    }
}
