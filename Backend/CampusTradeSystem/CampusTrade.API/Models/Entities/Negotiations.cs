using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 议价实体类
    /// </summary>
    public class Negotiation
    {
        /// <summary>
        /// 议价ID - 主键，自增
        /// </summary>
        [Key]
        [Column("NEGOTIATION_ID", TypeName = "NUMBER")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NegotiationId { get; set; }

        /// <summary>
        /// 订单ID - 外键
        /// </summary>
        [Required]
        [Column("ORDER_ID", TypeName = "NUMBER")]
        public int OrderId { get; set; }

        /// <summary>
        /// 提议价格
        /// </summary>
        [Required]
        [Column("PROPOSED_PRICE", TypeName = "decimal(10,2)")]
        [Range(0.01, 99999999.99, ErrorMessage = "提议价格必须在0.01到99999999.99之间")]
        public decimal ProposedPrice { get; set; }

        /// <summary>
        /// 议价状态
        /// </summary>
        [Required]
        [Column("STATUS", TypeName = "VARCHAR2(20)")]
        [MaxLength(20)]
        public string Status { get; set; } = "等待回应";

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column("CREATED_AT", TypeName = "TIMESTAMP")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        #region 导航属性

        /// <summary>
        /// 关联的订单
        /// 外键关系：negotiations.order_id -> orders.order_id
        /// </summary>
        public virtual Order Order { get; set; } = null!;

        #endregion

        #region 业务方法

        /// <summary>
        /// 检查议价是否处于等待回应状态
        /// </summary>
        /// <returns>如果状态为"等待回应"返回true</returns>
        public bool IsPending()
        {
            return Status == "等待回应";
        }

        /// <summary>
        /// 检查议价是否已被接受
        /// </summary>
        /// <returns>如果状态为"接受"返回true</returns>
        public bool IsAccepted()
        {
            return Status == "接受";
        }

        /// <summary>
        /// 检查议价是否已被拒绝
        /// </summary>
        /// <returns>如果状态为"拒绝"返回true</returns>
        public bool IsRejected()
        {
            return Status == "拒绝";
        }

        /// <summary>
        /// 检查是否为反报价
        /// </summary>
        /// <returns>如果状态为"反报价"返回true</returns>
        public bool IsCounterOffer()
        {
            return Status == "反报价";
        }

        /// <summary>
        /// 检查议价是否已完成（接受或拒绝）
        /// </summary>
        /// <returns>如果状态不是"等待回应"和"反报价"返回true</returns>
        public bool IsCompleted()
        {
            return Status == "接受" || Status == "拒绝";
        }

        /// <summary>
        /// 检查议价是否仍在进行中
        /// </summary>
        /// <returns>如果状态为"等待回应"或"反报价"返回true</returns>
        public bool IsActive()
        {
            return Status == "等待回应" || Status == "反报价";
        }

        /// <summary>
        /// 接受议价
        /// </summary>
        /// <exception cref="InvalidOperationException">当状态不允许接受时抛出</exception>
        public void Accept()
        {
            if (!CanBeAccepted())
            {
                throw new InvalidOperationException($"无法接受议价。当前状态：{Status}，只有'等待回应'或'反报价'状态的议价可以被接受。");
            }

            Status = "接受";
        }

        /// <summary>
        /// 拒绝议价
        /// </summary>
        /// <exception cref="InvalidOperationException">当状态不允许拒绝时抛出</exception>
        public void Reject()
        {
            if (!CanBeRejected())
            {
                throw new InvalidOperationException($"无法拒绝议价。当前状态：{Status}，只有'等待回应'或'反报价'状态的议价可以被拒绝。");
            }

            Status = "拒绝";
        }

        /// <summary>
        /// 设置为反报价状态
        /// </summary>
        /// <exception cref="InvalidOperationException">当状态不允许反报价时抛出</exception>
        public void SetCounterOffer()
        {
            if (!CanBeCountered())
            {
                throw new InvalidOperationException($"无法设置反报价。当前状态：{Status}，只有'等待回应'状态的议价可以设置反报价。");
            }

            Status = "反报价";
        }

        /// <summary>
        /// 检查是否可以被接受
        /// </summary>
        /// <returns>如果可以被接受返回true</returns>
        public bool CanBeAccepted()
        {
            return Status == "等待回应" || Status == "反报价";
        }

        /// <summary>
        /// 检查是否可以被拒绝
        /// </summary>
        /// <returns>如果可以被拒绝返回true</returns>
        public bool CanBeRejected()
        {
            return Status == "等待回应" || Status == "反报价";
        }

        /// <summary>
        /// 检查是否可以设置反报价
        /// </summary>
        /// <returns>如果可以设置反报价返回true</returns>
        public bool CanBeCountered()
        {
            return Status == "等待回应";
        }

        /// <summary>
        /// 获取格式化的提议价格字符串
        /// </summary>
        /// <returns>格式化的价格字符串</returns>
        public string GetFormattedPrice()
        {
            return $"¥{ProposedPrice:F2}";
        }

        /// <summary>
        /// 获取状态显示文本
        /// </summary>
        /// <returns>用于显示的状态文本</returns>
        public string GetStatusDisplayText()
        {
            return Status switch
            {
                "等待回应" => "⏳ 等待回应",
                "接受" => "✅ 接受",
                "拒绝" => "❌ 拒绝",
                "反报价" => "🔄 反报价",
                _ => Status
            };
        }

        /// <summary>
        /// 计算与原价的差额
        /// </summary>
        /// <param name="originalPrice">原价</param>
        /// <returns>差额（正数表示涨价，负数表示降价）</returns>
        public decimal CalculatePriceDifference(decimal originalPrice)
        {
            return ProposedPrice - originalPrice;
        }

        /// <summary>
        /// 计算与原价的折扣率
        /// </summary>
        /// <param name="originalPrice">原价</param>
        /// <returns>折扣率（0-1之间，0.8表示8折）</returns>
        public decimal CalculateDiscountRate(decimal originalPrice)
        {
            if (originalPrice <= 0)
            {
                throw new ArgumentException("原价必须大于0", nameof(originalPrice));
            }

            return ProposedPrice / originalPrice;
        }

        /// <summary>
        /// 检查议价是否已超时（超过指定时间仍未回应）
        /// </summary>
        /// <param name="timeoutHours">超时时间（小时），默认24小时</param>
        /// <returns>如果超时返回true</returns>
        public bool IsTimeout(int timeoutHours = 24)
        {
            if (IsCompleted()) return false;

            var timeoutTime = CreatedAt.AddHours(timeoutHours);
            return DateTime.Now > timeoutTime;
        }

        /// <summary>
        /// 获取议价的详细描述
        /// </summary>
        /// <returns>包含价格、状态、时间的详细描述</returns>
        public string GetDescription()
        {
            var statusText = GetStatusDisplayText();
            return $"议价{GetFormattedPrice()}，状态：{statusText}，创建时间：{CreatedAt:yyyy-MM-dd HH:mm:ss}";
        }

        #endregion

        #region 静态方法

        /// <summary>
        /// 创建新的议价请求
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <param name="proposedPrice">提议价格</param>
        /// <returns>新的议价请求实例</returns>
        /// <exception cref="ArgumentException">当参数无效时抛出</exception>
        public static Negotiation Create(int orderId, decimal proposedPrice)
        {
            if (orderId <= 0)
            {
                throw new ArgumentException("订单ID必须大于0", nameof(orderId));
            }

            if (proposedPrice <= 0)
            {
                throw new ArgumentException("提议价格必须大于0", nameof(proposedPrice));
            }

            return new Negotiation
            {
                OrderId = orderId,
                ProposedPrice = proposedPrice,
                Status = "等待回应",
                CreatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// 验证议价状态是否有效
        /// </summary>
        /// <param name="status">要验证的状态</param>
        /// <returns>如果状态有效返回true</returns>
        public static bool IsValidStatus(string status)
        {
            return ValidStatuses.Contains(status);
        }

        /// <summary>
        /// 获取所有有效的议价状态
        /// </summary>
        /// <returns>有效状态数组</returns>
        public static string[] GetValidStatuses()
        {
            return ValidStatuses.ToArray();
        }

        /// <summary>
        /// 验证提议价格是否合理（相对于原价）
        /// </summary>
        /// <param name="proposedPrice">提议价格</param>
        /// <param name="originalPrice">原价</param>
        /// <param name="maxDiscountRate">最大折扣率，默认0.5（5折）</param>
        /// <param name="maxMarkupRate">最大涨价率，默认1.5（1.5倍）</param>
        /// <returns>如果价格合理返回true</returns>
        public static bool IsReasonablePrice(decimal proposedPrice, decimal originalPrice,
            decimal maxDiscountRate = 0.5m, decimal maxMarkupRate = 1.5m)
        {
            if (originalPrice <= 0 || proposedPrice <= 0) return false;

            var rate = proposedPrice / originalPrice;
            return rate >= maxDiscountRate && rate <= maxMarkupRate;
        }

        #endregion

        #region 常量定义

        /// <summary>
        /// 有效的议价状态列表
        /// </summary>
        public static readonly HashSet<string> ValidStatuses = new()
        {
            "等待回应", "接受", "拒绝", "反报价"
        };

        /// <summary>
        /// 默认超时时间（小时）
        /// </summary>
        public const int DefaultTimeoutHours = 24;

        /// <summary>
        /// 最大议价时间（小时）
        /// </summary>
        public const int MaxNegotiationHours = 168; // 7天

        /// <summary>
        /// 最小折扣率（5折）
        /// </summary>
        public const decimal MinDiscountRate = 0.5m;

        /// <summary>
        /// 最大涨价率（1.5倍）
        /// </summary>
        public const decimal MaxMarkupRate = 1.5m;

        #endregion
    }
}
