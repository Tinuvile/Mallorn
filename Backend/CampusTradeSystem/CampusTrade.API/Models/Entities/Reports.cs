using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 举报实体类 - 处理用户对订单的举报
    /// </summary>
    [Table("REPORTS")]
    public class Reports
    {
        #region 基本信息

        /// <summary>
        /// 举报ID - 主键，自增
        /// </summary>
        [Key]
        [Column("REPORT_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReportId { get; set; }

        /// <summary>
        /// 订单ID - 外键，关联抽象订单表
        /// </summary>
        [Required]
        [Column("ORDER_ID")]
        public int OrderId { get; set; }

        /// <summary>
        /// 举报人ID - 外键，关联用户表
        /// </summary>
        [Required]
        [Column("REPORTER_ID")]
        public int ReporterId { get; set; }

        /// <summary>
        /// 举报类型 - 商品问题/服务问题/欺诈/虚假描述/其他
        /// </summary>
        [Required]
        [Column("TYPE")]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 优先级 - 1-10，数字越大优先级越高
        /// </summary>
        [Column("PRIORITY")]
        [Range(1, 10)]
        public int? Priority { get; set; }

        /// <summary>
        /// 举报描述
        /// </summary>
        [Column("DESCRIPTION")]
        public string? Description { get; set; }

        /// <summary>
        /// 处理状态 - 待处理/处理中/已处理/已关闭
        /// </summary>
        [Required]
        [Column("STATUS")]
        [StringLength(20)]
        public string Status { get; set; } = "待处理";

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("CREATE_TIME")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        #endregion

        #region 导航属性

        /// <summary>
        /// 关联的抽象订单
        /// </summary>
        public virtual AbstractOrder AbstractOrder { get; set; } = null!;

        /// <summary>
        /// 举报人信息
        /// </summary>
        public virtual User Reporter { get; set; } = null!;

        /// <summary>
        /// 举报证据列表
        /// </summary>
        public virtual ICollection<ReportEvidence> Evidences { get; set; } = new List<ReportEvidence>();

        #endregion

        #region 业务方法

        /// <summary>
        /// 验证举报类型是否有效
        /// </summary>
        public bool IsValidType()
        {
            var validTypes = new[] { "商品问题", "服务问题", "欺诈", "虚假描述", "其他" };
            return validTypes.Contains(Type);
        }

        /// <summary>
        /// 验证举报状态是否有效
        /// </summary>
        public bool IsValidStatus()
        {
            var validStatuses = new[] { "待处理", "处理中", "已处理", "已关闭" };
            return validStatuses.Contains(Status);
        }

        /// <summary>
        /// 检查是否为高优先级举报
        /// </summary>
        public bool IsHighPriority()
        {
            return Priority.HasValue && Priority.Value >= 7;
        }

        /// <summary>
        /// 检查是否为紧急举报
        /// </summary>
        public bool IsUrgent()
        {
            return Priority.HasValue && Priority.Value >= 9;
        }

        /// <summary>
        /// 检查举报是否已处理
        /// </summary>
        public bool IsProcessed()
        {
            return Status == "已处理" || Status == "已关闭";
        }

        /// <summary>
        /// 检查举报是否正在处理中
        /// </summary>
        public bool IsInProgress()
        {
            return Status == "处理中";
        }

        /// <summary>
        /// 检查举报是否待处理
        /// </summary>
        public bool IsPending()
        {
            return Status == "待处理";
        }

        /// <summary>
        /// 开始处理举报
        /// </summary>
        public void StartProcessing()
        {
            if (IsPending())
            {
                Status = "处理中";
            }
        }

        /// <summary>
        /// 完成处理举报
        /// </summary>
        public void CompleteProcessing()
        {
            if (IsInProgress())
            {
                Status = "已处理";
            }
        }

        /// <summary>
        /// 关闭举报
        /// </summary>
        public void CloseReport()
        {
            if (!IsProcessed())
            {
                Status = "已关闭";
            }
        }

        /// <summary>
        /// 设置优先级
        /// </summary>
        /// <param name="priority">优先级值（1-10）</param>
        public void SetPriority(int priority)
        {
            if (priority >= 1 && priority <= 10)
            {
                Priority = priority;
            }
        }

        /// <summary>
        /// 添加证据
        /// </summary>
        /// <param name="evidence">证据对象</param>
        public void AddEvidence(ReportEvidence evidence)
        {
            if (evidence != null && !IsProcessed())
            {
                evidence.ReportId = ReportId;
                Evidences.Add(evidence);
            }
        }

        /// <summary>
        /// 获取证据数量
        /// </summary>
        public int GetEvidenceCount()
        {
            return Evidences?.Count ?? 0;
        }

        /// <summary>
        /// 检查是否有足够证据
        /// </summary>
        public bool HasSufficientEvidence()
        {
            return GetEvidenceCount() > 0;
        }

        /// <summary>
        /// 计算举报存在时长（小时）
        /// </summary>
        public double GetExistingHours()
        {
            return (DateTime.Now - CreateTime).TotalHours;
        }

        /// <summary>
        /// 检查是否为超时举报（超过24小时未处理）
        /// </summary>
        public bool IsOverdue()
        {
            return IsPending() && GetExistingHours() > 24;
        }

        /// <summary>
        /// 获取举报优先级描述
        /// </summary>
        public string GetPriorityDescription()
        {
            if (!Priority.HasValue) return "未设置";

            return Priority.Value switch
            {
                >= 9 => "紧急",
                >= 7 => "高",
                >= 4 => "中",
                _ => "低"
            };
        }

        /// <summary>
        /// 验证举报数据完整性
        /// </summary>
        public bool IsValid()
        {
            return OrderId > 0
                && ReporterId > 0
                && IsValidType()
                && IsValidStatus()
                && (!Priority.HasValue || (Priority.Value >= 1 && Priority.Value <= 10));
        }

        #endregion
    }
}
