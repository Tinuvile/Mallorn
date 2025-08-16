namespace CampusTrade.API.Models.DTOs.Admin
{
    /// <summary>
    /// 管理员信息响应DTO
    /// </summary>
    public class AdminResponseDto
    {
        /// <summary>
        /// 管理员ID
        /// </summary>
        public int AdminId { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 管理员角色
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// 角色显示名称
        /// </summary>
        public string RoleDisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 分配的分类ID
        /// </summary>
        public int? AssignedCategory { get; set; }

        /// <summary>
        /// 分配的分类名称
        /// </summary>
        public string? AssignedCategoryName { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 是否活跃
        /// </summary>
        public bool IsActive { get; set; }
    }
}
