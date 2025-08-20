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
        /// <summary>
        /// 举报ID - 主键，由序列和触发器自增
        /// </summary>
        [Key]
        [Column("REPORT_ID")]
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
        [Column("TYPE", TypeName = "VARCHAR2(50)")]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 优先级 - 1-10，数字越大优先级越高
        /// </summary>
        [Column("PRIORITY", TypeName = "NUMBER(2,0)")]
        [Range(1, 10)]
        public int? Priority { get; set; }

        /// <summary>
        /// 举报描述
        /// </summary>
        [Column("DESCRIPTION", TypeName = "CLOB")]
        public string? Description { get; set; }

        /// <summary>
        /// 处理状态 - 待处理/处理中/已处理/已关闭（默认值由Oracle处理）
        /// </summary>
        [Required]
        [Column("STATUS", TypeName = "VARCHAR2(20)")]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间（由Oracle DEFAULT处理）
        /// </summary>
        [Column("CREATE_TIME")]
        public DateTime CreateTime { get; set; }

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
    }
}
