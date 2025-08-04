using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 充值记录实体类
    /// </summary>
    public class RechargeRecord
    {
        /// <summary>
        /// 充值记录ID - 主键，自增
        /// </summary>
        [Key]
        [Column("RECHARGE_ID", TypeName = "NUMBER")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RechargeId { get; set; }

        /// <summary>
        /// 用户ID - 外键
        /// </summary>
        [Required]
        [Column("USER_ID")]
        public int UserId { get; set; }

        /// <summary>
        /// 充值金额
        /// </summary>
        [Required]
        [Column("AMOUNT", TypeName = "decimal(10,2)")]
        [Range(0.01, 99999999.99, ErrorMessage = "充值金额必须在0.01到99999999.99之间")]
        public decimal Amount { get; set; }

        /// <summary>
        /// 充值状态
        /// </summary>
        [Required]
        [Column("STATUS", TypeName = "VARCHAR2(20)")]
        [MaxLength(20)]
        public string Status { get; set; } = "处理中";

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column("CREATE_TIME", TypeName = "TIMESTAMP")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 完成时间
        /// </summary>
        [Column("COMPLETE_TIME", TypeName = "TIMESTAMP")]
        public DateTime? CompleteTime { get; set; }

        #region 导航属性

        /// <summary>
        /// 关联的用户
        /// 外键关系：recharge_records.user_id -> users.user_id
        /// </summary>
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// 关联的虚拟账户
        /// 通过用户ID间接关联
        /// </summary>
        public virtual VirtualAccount? VirtualAccount { get; set; }

        #endregion

        #region 业务方法

        /// <summary>
        /// 检查充值记录是否处于处理中状态
        /// </summary>
        /// <returns>如果状态为"处理中"返回true</returns>
        public bool IsPending()
        {
            return Status == "处理中";
        }

        /// <summary>
        /// 检查充值记录是否已成功完成
        /// </summary>
        /// <returns>如果状态为"成功"返回true</returns>
        public bool IsSuccessful()
        {
            return Status == "成功";
        }

        /// <summary>
        /// 检查充值记录是否失败
        /// </summary>
        /// <returns>如果状态为"失败"返回true</returns>
        public bool IsFailed()
        {
            return Status == "失败";
        }

        /// <summary>
        /// 检查充值记录是否已完成（成功或失败）
        /// </summary>
        /// <returns>如果状态不是"处理中"返回true</returns>
        public bool IsCompleted()
        {
            return Status != "处理中";
        }

        /// <summary>
        /// 将充值状态标记为成功
        /// </summary>
        /// <exception cref="InvalidOperationException">当状态不是"处理中"时抛出</exception>
        public void MarkAsSuccessful()
        {
            if (!IsPending())
            {
                throw new InvalidOperationException($"无法将状态从'{Status}'更改为'成功'。只有'处理中'状态的充值可以标记为成功。");
            }

            Status = "成功";
            CompleteTime = DateTime.Now;
        }

        /// <summary>
        /// 将充值状态标记为失败
        /// </summary>
        /// <exception cref="InvalidOperationException">当状态不是"处理中"时抛出</exception>
        public void MarkAsFailed()
        {
            if (!IsPending())
            {
                throw new InvalidOperationException($"无法将状态从'{Status}'更改为'失败'。只有'处理中'状态的充值可以标记为失败。");
            }

            Status = "失败";
            CompleteTime = DateTime.Now;
        }

        /// <summary>
        /// 获取充值处理耗时
        /// </summary>
        /// <returns>如果已完成返回处理时长，否则返回null</returns>
        public TimeSpan? GetProcessingDuration()
        {
            if (CompleteTime.HasValue)
            {
                return CompleteTime.Value - CreateTime;
            }
            return null;
        }

        /// <summary>
        /// 获取格式化的充值金额字符串
        /// </summary>
        /// <returns>格式化的金额字符串</returns>
        public string GetFormattedAmount()
        {
            return $"¥{Amount:F2}";
        }

        /// <summary>
        /// 获取状态显示文本
        /// </summary>
        /// <returns>用于显示的状态文本</returns>
        public string GetStatusDisplayText()
        {
            return Status switch
            {
                "处理中" => "⏳ 处理中",
                "成功" => "✅ 成功",
                "失败" => "❌ 失败",
                _ => Status
            };
        }

        /// <summary>
        /// 检查充值记录是否已超时（超过指定时间仍未完成）
        /// </summary>
        /// <param name="timeoutMinutes">超时时间（分钟），默认30分钟</param>
        /// <returns>如果超时返回true</returns>
        public bool IsTimeout(int timeoutMinutes = 30)
        {
            if (IsCompleted()) return false;

            var timeoutTime = CreateTime.AddMinutes(timeoutMinutes);
            return DateTime.Now > timeoutTime;
        }

        /// <summary>
        /// 验证充值金额是否有效
        /// </summary>
        /// <returns>如果金额有效返回true</returns>
        public bool IsAmountValid()
        {
            return Amount >= VirtualAccount.MinRechargeAmount &&
                   Amount <= VirtualAccount.MaxRechargeAmount;
        }

        /// <summary>
        /// 获取充值记录的详细描述
        /// </summary>
        /// <returns>包含时间、金额、状态的详细描述</returns>
        public string GetDescription()
        {
            var duration = GetProcessingDuration();
            var durationText = duration.HasValue ? $"，耗时{duration.Value.TotalMinutes:F1}分钟" : "";

            return $"充值{GetFormattedAmount()}，状态：{Status}，创建时间：{CreateTime:yyyy-MM-dd HH:mm:ss}{durationText}";
        }

        #endregion

        #region 静态方法

        /// <summary>
        /// 创建新的充值记录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="amount">充值金额</param>
        /// <returns>新的充值记录实例</returns>
        /// <exception cref="ArgumentException">当参数无效时抛出</exception>
        public static RechargeRecord Create(int userId, decimal amount)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("用户ID必须大于0", nameof(userId));
            }

            if (amount < VirtualAccount.MinRechargeAmount || amount > VirtualAccount.MaxRechargeAmount)
            {
                throw new ArgumentException(
                    $"充值金额必须在{VirtualAccount.MinRechargeAmount:F2}到{VirtualAccount.MaxRechargeAmount:F2}之间",
                    nameof(amount));
            }

            return new RechargeRecord
            {
                UserId = userId,
                Amount = amount,
                Status = "处理中",
                CreateTime = DateTime.Now
            };
        }

        #endregion

        #region 静态验证方法

        /// <summary>
        /// 验证充值状态是否有效
        /// </summary>
        /// <param name="status">要验证的状态</param>
        /// <returns>如果状态有效返回true</returns>
        public static bool IsValidStatus(string status)
        {
            return ValidStatuses.Contains(status);
        }

        /// <summary>
        /// 获取所有有效的充值状态
        /// </summary>
        /// <returns>有效状态数组</returns>
        public static string[] GetValidStatuses()
        {
            return ValidStatuses.ToArray();
        }

        #endregion

        #region 常量定义

        /// <summary>
        /// 有效的充值状态列表
        /// </summary>
        public static readonly HashSet<string> ValidStatuses = new()
        {
            "处理中", "成功", "失败"
        };

        /// <summary>
        /// 默认超时时间（分钟）
        /// </summary>
        public const int DefaultTimeoutMinutes = 30;

        /// <summary>
        /// 最大处理时间（分钟）
        /// </summary>
        public const int MaxProcessingMinutes = 1440; // 24小时

        #endregion
    }
}
