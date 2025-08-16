using System.ComponentModel.DataAnnotations;

namespace CampusTrade.API.Models.DTOs.Admin
{
    /// <summary>
    /// 创建管理员请求DTO
    /// </summary>
    public class CreateAdminDto
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        public int UserId { get; set; }

        /// <summary>
        /// 管理员角色
        /// super: 系统管理员
        /// category_admin: 模块管理员
        /// </summary>
        [Required(ErrorMessage = "管理员角色不能为空")]
        [RegularExpression("^(super|category_admin)$", ErrorMessage = "管理员角色只能是super或category_admin")]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// 分配的分类ID（仅模块管理员需要）
        /// </summary>
        public int? AssignedCategory { get; set; }
    }
}
