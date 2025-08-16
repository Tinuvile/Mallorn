using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 学生信息实体 - 对应 Oracle 数据库中的 STUDENTS 表
    /// </summary>
    [Table("STUDENTS")]
    public class Student
    {
        /// <summary>
        /// 学生ID - 主键，对应Oracle中的student_id字段
        /// </summary>
        [Key]
        [Column("STUDENT_ID", TypeName = "VARCHAR2(20)")]
        [StringLength(20)]
        [Required]
        public string StudentId { get; set; } = string.Empty;

        /// <summary>
        /// 学生姓名 - 必填字段
        /// </summary>
        [Required]
        [Column("NAME", TypeName = "VARCHAR2(50)")]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 所属院系 - 可为空
        /// </summary>
        [Column("DEPARTMENT", TypeName = "VARCHAR2(50)")]
        [StringLength(50)]
        public string? Department { get; set; }

        // 导航属性：一个学生对应一个用户账户
        public virtual User? User { get; set; }
    }
}
