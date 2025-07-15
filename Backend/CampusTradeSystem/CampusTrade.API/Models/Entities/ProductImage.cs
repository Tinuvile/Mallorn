using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 商品图片实体类 - 对应 Oracle 数据库中的 PRODUCT_IMAGES 表
    /// 用于存储商品的图片信息，一个商品可以有多张图片
    /// </summary>
    [Table("PRODUCT_IMAGES")]
    public class ProductImage
    {
        /// <summary>
        /// 图片ID - 主键，对应Oracle中的image_id字段，自增
        /// </summary>
        [Key]
        [Column("IMAGE_ID", TypeName = "NUMBER")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ImageId { get; set; }

        /// <summary>
        /// 商品ID - 外键，对应Oracle中的product_id字段
        /// 关联到products表，标识图片所属商品
        /// </summary>
        [Required]
        [Column("PRODUCT_ID", TypeName = "NUMBER")]
        public int ProductId { get; set; }

        /// <summary>
        /// 图片URL - 对应Oracle中的image_url字段
        /// 存储图片的访问地址，最大长度200字符
        /// </summary>
        [Required]
        [Column("IMAGE_URL", TypeName = "VARCHAR2(200)")]
        [StringLength(200, ErrorMessage = "图片URL不能超过200个字符")]
        [Url(ErrorMessage = "图片URL格式不正确")]
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// 图片显示顺序 - 用于前端显示排序
        /// 数据库中没有此字段，但应用层需要
        /// </summary>
        [NotMapped]
        public int DisplayOrder { get; set; } = 0;

        // 导航属性：关联的商品
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        /// <summary>
        /// 图片类型枚举
        /// </summary>
        public enum ImageType
        {
            /// <summary>
            /// 主图 - 商品的主要展示图片
            /// </summary>
            Main,

            /// <summary>
            /// 详情图 - 商品的详细图片
            /// </summary>
            Detail,

            /// <summary>
            /// 缩略图 - 用于列表展示的小图
            /// </summary>
            Thumbnail
        }

        /// <summary>
        /// 支持的图片格式
        /// </summary>
        public static class SupportedFormats
        {
            public const string JPEG = ".jpg";
            public const string PNG = ".png";
            public const string GIF = ".gif";
            public const string WEBP = ".webp";

            /// <summary>
            /// 获取所有支持的格式
            /// </summary>
            public static string[] GetAll() => new[] { JPEG, PNG, GIF, WEBP };
        }

        /// <summary>
        /// 验证图片URL是否为支持的格式
        /// </summary>
        /// <returns>是否为支持的格式</returns>
        public bool IsSupportedFormat()
        {
            if (string.IsNullOrEmpty(ImageUrl))
                return false;

            var extension = Path.GetExtension(ImageUrl).ToLower();
            return SupportedFormats.GetAll().Contains(extension);
        }

        /// <summary>
        /// 获取图片文件扩展名
        /// </summary>
        /// <returns>文件扩展名</returns>
        public string GetFileExtension()
        {
            return Path.GetExtension(ImageUrl).ToLower();
        }

        /// <summary>
        /// 验证图片URL是否有效
        /// </summary>
        /// <returns>是否有效</returns>
        public bool IsValidImageUrl()
        {
            if (string.IsNullOrEmpty(ImageUrl))
                return false;

            // 检查URL格式
            if (!Uri.TryCreate(ImageUrl, UriKind.Absolute, out Uri? uri))
                return false;

            // 检查是否为HTTP或HTTPS协议
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            // 检查文件格式
            return IsSupportedFormat();
        }
    }
}
