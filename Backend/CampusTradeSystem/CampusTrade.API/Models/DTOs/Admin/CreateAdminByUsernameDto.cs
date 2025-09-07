using System.ComponentModel.DataAnnotations;

namespace CampusTrade.API.Models.DTOs.Admin
{
    /// <summary>
    /// 通过用户名创建管理员请求DTO
    /// </summary>
    public class CreateAdminByUsernameDto
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(50, ErrorMessage = "用户名长度不能超过50个字符")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 邮箱地址
        /// </summary>
        [Required(ErrorMessage = "邮箱地址不能为空")]
        [EmailAddress(ErrorMessage = "邮箱地址格式不正确")]
        public string Email { get; set; } = string.Empty;

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
