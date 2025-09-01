using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 商品分类实体类 - 对应 Oracle 数据库中的 CATEGORIES 表
    /// 支持树形结构，可以有父分类和子分类
    /// </summary>
    [Table("CATEGORIES")]
    public class Category
    {
        /// <summary>
        /// 分类ID - 主键，对应Oracle中的category_id字段，非自增，Oracle序列和触发器处理
        /// </summary>
        [Key]
        [Column("CATEGORY_ID")]
        public int CategoryId { get; set; }

        /// <summary>
        /// 父分类ID - 外键，对应Oracle中的parent_id字段
        /// 如果为空，则表示这是一级分类（根分类）
        /// </summary>
        [Column("PARENT_ID")]
        public int? ParentId { get; set; }

        /// <summary>
        /// 分类名称 - 对应Oracle中的name字段
        /// 分类的显示名称，最大长度50字符
        /// </summary>
        [Required]
        [Column("NAME", TypeName = "VARCHAR2(50)")]
        [StringLength(50, ErrorMessage = "分类名称不能超过50个字符")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 父分类 - 多对一关系
        /// 通过ParentId外键关联到父分类
        /// </summary>
        [ForeignKey("ParentId")]
        public virtual Category? Parent { get; set; }

        /// <summary>
        /// 子分类集合 - 一对多关系
        /// 当前分类下的所有子分类
        /// </summary>
        public virtual ICollection<Category> Children { get; set; } = new List<Category>();

        /// <summary>
        /// 该分类下的商品集合
        /// </summary>
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();

        /// <summary>
        /// 负责管理该分类的管理员集合
        /// </summary>
        public virtual ICollection<Admin> Admins { get; set; } = new List<Admin>();
    }
}
