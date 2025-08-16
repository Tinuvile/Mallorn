using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities;

/// <summary>
/// 刷新令牌实体
/// </summary>
[Table("REFRESH_TOKENS")]
public class RefreshToken
{
    /// <summary>
    /// 主键
    /// </summary>
    [Key]
    [Column("ID", TypeName = "VARCHAR2(36)")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Refresh Token值
    /// </summary>
    [Required]
    [Column("TOKEN", TypeName = "VARCHAR2(500)")]
    [StringLength(500)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 关联用户ID
    /// </summary>
    [Required]
    [Column("USER_ID")]
    public int UserId { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    [Required]
    [Column("EXPIRY_DATE")]
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// 是否已撤销（默认值由Oracle处理）
    /// </summary>
    [Column("IS_REVOKED")]
    public int IsRevoked { get; set; } = 0;

    /// <summary>
    /// 创建时间（由Oracle DEFAULT处理）
    /// </summary>
    [Required]
    [Column("CREATED_AT")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 撤销时间
    /// </summary>
    [Column("REVOKED_AT")]
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// 创建时的IP地址
    /// </summary>
    [Column("IP_ADDRESS", TypeName = "VARCHAR2(45)")]
    [StringLength(45)] // IPv6最大长度
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理信息
    /// </summary>
    [Column("USER_AGENT", TypeName = "VARCHAR2(500)")]
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// 设备标识
    /// </summary>
    [Column("DEVICE_ID", TypeName = "VARCHAR2(100)")]
    [StringLength(100)]
    public string? DeviceId { get; set; }

    /// <summary>
    /// 被哪个Token替换（Token轮换时使用）
    /// </summary>
    [Column("REPLACED_BY_TOKEN", TypeName = "VARCHAR2(500)")]
    [StringLength(500)]
    public string? ReplacedByToken { get; set; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    [Column("CREATED_BY")]
    public int? CreatedBy { get; set; }

    /// <summary>
    /// 最后使用时间
    /// </summary>
    [Column("LAST_USED_AT")]
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// 撤销者ID
    /// </summary>
    [Column("REVOKED_BY")]
    public int? RevokedBy { get; set; }

    /// <summary>
    /// 撤销原因
    /// </summary>
    [Column("REVOKE_REASON", TypeName = "VARCHAR2(200)")]
    [StringLength(200)]
    public string? RevokeReason { get; set; }

    /// <summary>
    /// 关联的用户实体
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
