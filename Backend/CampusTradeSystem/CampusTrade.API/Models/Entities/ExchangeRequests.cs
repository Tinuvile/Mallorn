using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 换物请求实体类
    /// </summary>
    public class ExchangeRequest
    {
        /// <summary>
        /// 换物请求ID - 主键，外键
        /// </summary>
        [Key]
        [Column("EXCHANGE_ID", TypeName = "NUMBER")]
        public int ExchangeId { get; set; }

        /// <summary>
        /// 提供商品ID - 外键
        /// </summary>
        [Required]
        [Column("OFFER_PRODUCT_ID")]
        public int OfferProductId { get; set; }

        /// <summary>
        /// 请求商品ID - 外键
        /// </summary>
        [Required]
        [Column("REQUEST_PRODUCT_ID")]
        public int RequestProductId { get; set; }

        /// <summary>
        /// 交换条件
        /// </summary>
        [Column("TERMS", TypeName = "CLOB")]
        public string? Terms { get; set; }

        /// <summary>
        /// 交换状态
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
        /// 关联的抽象订单
        /// 外键关系：exchange_requests.exchange_id -> abstract_orders.abstract_order_id
        /// </summary>
        public virtual AbstractOrder AbstractOrder { get; set; } = null!;

        /// <summary>
        /// 提供的商品
        /// 外键关系：exchange_requests.offer_product_id -> products.product_id
        /// </summary>
        public virtual Product OfferProduct { get; set; } = null!;

        /// <summary>
        /// 请求的商品
        /// 外键关系：exchange_requests.request_product_id -> products.product_id
        /// </summary>
        public virtual Product RequestProduct { get; set; } = null!;

        #endregion

        #region 业务方法

        /// <summary>
        /// 检查换物请求是否处于等待回应状态
        /// </summary>
        /// <returns>如果状态为"等待回应"返回true</returns>
        public bool IsPending()
        {
            return Status == "等待回应";
        }

        /// <summary>
        /// 检查换物请求是否已被接受
        /// </summary>
        /// <returns>如果状态为"接受"返回true</returns>
        public bool IsAccepted()
        {
            return Status == "接受";
        }

        /// <summary>
        /// 检查换物请求是否已被拒绝
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
        /// 检查换物请求是否已完成（接受或拒绝）
        /// </summary>
        /// <returns>如果状态不是"等待回应"和"反报价"返回true</returns>
        public bool IsCompleted()
        {
            return Status == "接受" || Status == "拒绝";
        }

        /// <summary>
        /// 检查换物请求是否仍在进行中
        /// </summary>
        /// <returns>如果状态为"等待回应"或"反报价"返回true</returns>
        public bool IsActive()
        {
            return Status == "等待回应" || Status == "反报价";
        }

        /// <summary>
        /// 接受换物请求
        /// </summary>
        /// <exception cref="InvalidOperationException">当状态不允许接受时抛出</exception>
        public void Accept()
        {
            if (!CanBeAccepted())
            {
                throw new InvalidOperationException($"无法接受换物请求。当前状态：{Status}，只有'等待回应'或'反报价'状态的请求可以被接受。");
            }

            Status = "接受";
        }

        /// <summary>
        /// 拒绝换物请求
        /// </summary>
        /// <exception cref="InvalidOperationException">当状态不允许拒绝时抛出</exception>
        public void Reject()
        {
            if (!CanBeRejected())
            {
                throw new InvalidOperationException($"无法拒绝换物请求。当前状态：{Status}，只有'等待回应'或'反报价'状态的请求可以被拒绝。");
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
                throw new InvalidOperationException($"无法设置反报价。当前状态：{Status}，只有'等待回应'状态的请求可以设置反报价。");
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
        /// 检查是否为自己与自己的交换（无效交换）
        /// </summary>
        /// <returns>如果是自我交换返回true</returns>
        public bool IsSelfExchange()
        {
            return OfferProductId == RequestProductId;
        }

        /// <summary>
        /// 检查两个商品是否属于同一用户（无效交换）
        /// </summary>
        /// <returns>如果属于同一用户返回true</returns>
        public bool IsSameUserExchange()
        {
            return OfferProduct?.UserId == RequestProduct?.UserId;
        }

        /// <summary>
        /// 验证交换请求是否有效
        /// </summary>
        /// <returns>如果交换请求有效返回true</returns>
        public bool IsValidExchange()
        {
            // 检查是否为自我交换
            if (IsSelfExchange()) return false;

            // 检查是否为同一用户的商品交换
            if (IsSameUserExchange()) return false;

            // 检查商品是否都在售状态
            if (OfferProduct?.Status != "在售" || RequestProduct?.Status != "在售") return false;

            return true;
        }

        /// <summary>
        /// 检查换物请求是否已超时（超过指定时间仍未回应）
        /// </summary>
        /// <param name="timeoutHours">超时时间（小时），默认48小时</param>
        /// <returns>如果超时返回true</returns>
        public bool IsTimeout(int timeoutHours = 48)
        {
            if (IsCompleted()) return false;

            var timeoutTime = CreatedAt.AddHours(timeoutHours);
            return DateTime.Now > timeoutTime;
        }

        /// <summary>
        /// 获取交换条件的摘要（限制长度）
        /// </summary>
        /// <param name="maxLength">最大长度，默认100字符</param>
        /// <returns>条件摘要</returns>
        public string GetTermsSummary(int maxLength = 100)
        {
            if (string.IsNullOrWhiteSpace(Terms))
            {
                return "无附加条件";
            }

            if (Terms.Length <= maxLength)
            {
                return Terms;
            }

            return Terms.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// 获取换物请求的详细描述
        /// </summary>
        /// <returns>包含商品信息、状态、时间的详细描述</returns>
        public string GetDescription()
        {
            var statusText = GetStatusDisplayText();
            var offerTitle = OfferProduct?.Title ?? "未知商品";
            var requestTitle = RequestProduct?.Title ?? "未知商品";

            return $"用'{offerTitle}'换取'{requestTitle}'，状态：{statusText}，创建时间：{CreatedAt:yyyy-MM-dd HH:mm:ss}";
        }

        /// <summary>
        /// 计算商品价值差额（基于商品基础价格）
        /// </summary>
        /// <returns>价值差额（正数表示提供商品价值更高）</returns>
        public decimal CalculateValueDifference()
        {
            var offerPrice = OfferProduct?.BasePrice ?? 0m;
            var requestPrice = RequestProduct?.BasePrice ?? 0m;
            return offerPrice - requestPrice;
        }

        /// <summary>
        /// 检查是否为公平交换（价值差异在合理范围内）
        /// </summary>
        /// <param name="maxDifferenceRate">最大价值差异率，默认0.3（30%）</param>
        /// <returns>如果是公平交换返回true</returns>
        public bool IsFairExchange(decimal maxDifferenceRate = 0.3m)
        {
            var offerPrice = OfferProduct?.BasePrice ?? 0m;
            var requestPrice = RequestProduct?.BasePrice ?? 0m;

            if (offerPrice == 0 || requestPrice == 0) return false;

            var minPrice = Math.Min(offerPrice, requestPrice);
            var maxPrice = Math.Max(offerPrice, requestPrice);
            var differenceRate = (maxPrice - minPrice) / minPrice;

            return differenceRate <= maxDifferenceRate;
        }

        #endregion

        #region 静态方法

        /// <summary>
        /// 创建新的换物请求
        /// </summary>
        /// <param name="exchangeId">换物请求ID（来自序列）</param>
        /// <param name="offerProductId">提供商品ID</param>
        /// <param name="requestProductId">请求商品ID</param>
        /// <param name="terms">交换条件</param>
        /// <returns>新的换物请求实例</returns>
        /// <exception cref="ArgumentException">当参数无效时抛出</exception>
        public static ExchangeRequest Create(int exchangeId, int offerProductId, int requestProductId, string? terms = null)
        {
            if (exchangeId <= 0)
            {
                throw new ArgumentException("换物请求ID必须大于0", nameof(exchangeId));
            }

            if (offerProductId <= 0)
            {
                throw new ArgumentException("提供商品ID必须大于0", nameof(offerProductId));
            }

            if (requestProductId <= 0)
            {
                throw new ArgumentException("请求商品ID必须大于0", nameof(requestProductId));
            }

            if (offerProductId == requestProductId)
            {
                throw new ArgumentException("提供商品和请求商品不能是同一个商品");
            }

            return new ExchangeRequest
            {
                ExchangeId = exchangeId,
                OfferProductId = offerProductId,
                RequestProductId = requestProductId,
                Terms = terms,
                Status = "等待回应",
                CreatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// 验证换物状态是否有效
        /// </summary>
        /// <param name="status">要验证的状态</param>
        /// <returns>如果状态有效返回true</returns>
        public static bool IsValidStatus(string status)
        {
            return ValidStatuses.Contains(status);
        }

        /// <summary>
        /// 获取所有有效的换物状态
        /// </summary>
        /// <returns>有效状态数组</returns>
        public static string[] GetValidStatuses()
        {
            return ValidStatuses.ToArray();
        }

        /// <summary>
        /// 验证商品是否可以用于交换
        /// </summary>
        /// <param name="product">要验证的商品</param>
        /// <returns>如果商品可以交换返回true</returns>
        public static bool IsProductExchangeable(Product? product)
        {
            if (product == null) return false;
            return product.Status == "在售";
        }

        /// <summary>
        /// 验证两个商品是否可以进行交换
        /// </summary>
        /// <param name="offerProduct">提供的商品</param>
        /// <param name="requestProduct">请求的商品</param>
        /// <returns>如果可以交换返回true</returns>
        public static bool CanExchange(Product? offerProduct, Product? requestProduct)
        {
            if (!IsProductExchangeable(offerProduct) || !IsProductExchangeable(requestProduct))
            {
                return false;
            }

            // 不能与自己的商品交换
            if (offerProduct!.UserId == requestProduct!.UserId)
            {
                return false;
            }

            // 不能是同一个商品
            if (offerProduct.ProductId == requestProduct.ProductId)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region 常量定义

        /// <summary>
        /// 有效的换物状态列表
        /// </summary>
        public static readonly HashSet<string> ValidStatuses = new()
        {
            "等待回应", "接受", "拒绝", "反报价"
        };

        /// <summary>
        /// 默认超时时间（小时）
        /// </summary>
        public const int DefaultTimeoutHours = 48;

        /// <summary>
        /// 最大换物时间（小时）
        /// </summary>
        public const int MaxExchangeHours = 168; // 7天

        /// <summary>
        /// 最大价值差异率（公平交换标准）
        /// </summary>
        public const decimal MaxFairDifferenceRate = 0.3m; // 30%

        /// <summary>
        /// 交换条件最大长度
        /// </summary>
        public const int MaxTermsLength = 4000;

        #endregion
    }
}
