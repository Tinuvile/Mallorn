using System.ComponentModel.DataAnnotations;

namespace CampusTrade.API.Models.DTOs.Admin
{
    /// <summary>
    /// 更新管理员请求DTO
    /// </summary>
    public class UpdateAdminDto
    {
        /// <summary>
        /// 管理员角色
        /// </summary>
        [RegularExpression("^(super|category_admin)$", ErrorMessage = "管理员角色只能是super或category_admin")]
        public string? Role { get; set; }

        /// <summary>
        /// 分配的分类ID（仅模块管理员需要）
        /// </summary>
        public int? AssignedCategory { get; set; }
    }
}
