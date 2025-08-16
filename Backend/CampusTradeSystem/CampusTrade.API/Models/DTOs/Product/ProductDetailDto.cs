using System.Text.Json.Serialization;

namespace CampusTrade.API.Models.DTOs.Product;

/// <summary>
/// 商品详情响应DTO
/// </summary>
public class ProductDetailDto
{
    /// <summary>
    /// 商品ID
    /// </summary>
    [JsonPropertyName("product_id")]
    public int ProductId { get; set; }

    /// <summary>
    /// 商品标题
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 商品描述
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 基础价格
    /// </summary>
    [JsonPropertyName("base_price")]
    public decimal BasePrice { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    [JsonPropertyName("publish_time")]
    public DateTime PublishTime { get; set; }

    /// <summary>
    /// 浏览次数
    /// </summary>
    [JsonPropertyName("view_count")]
    public int ViewCount { get; set; }

    /// <summary>
    /// 自动下架时间
    /// </summary>
    [JsonPropertyName("auto_remove_time")]
    public DateTime? AutoRemoveTime { get; set; }

    /// <summary>
    /// 商品状态
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 发布用户ID
    /// </summary>
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    /// <summary>
    /// 发布用户信息
    /// </summary>
    [JsonPropertyName("user")]
    public ProductUserDto User { get; set; } = new();

    /// <summary>
    /// 分类ID
    /// </summary>
    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }

    /// <summary>
    /// 分类信息
    /// </summary>
    [JsonPropertyName("category")]
    public ProductCategoryDto Category { get; set; } = new();

    /// <summary>
    /// 商品图片列表
    /// </summary>
    [JsonPropertyName("images")]
    public List<ProductImageDto> Images { get; set; } = new();



    /// <summary>
    /// 距离自动下架剩余天数
    /// </summary>
    [JsonPropertyName("days_until_auto_remove")]
    public int? DaysUntilAutoRemove { get; set; }

    /// <summary>
    /// 是否为当前用户发布的商品
    /// </summary>
    [JsonPropertyName("is_own_product")]
    public bool IsOwnProduct { get; set; }
}

/// <summary>
/// 商品用户信息DTO
/// </summary>
public class ProductUserDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// 姓名
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 头像URL
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 信用分
    /// </summary>
    [JsonPropertyName("credit_score")]
    public decimal CreditScore { get; set; }

    /// <summary>
    /// 是否在线
    /// </summary>
    [JsonPropertyName("is_online")]
    public bool IsOnline { get; set; }
}

/// <summary>
/// 商品分类信息DTO
/// </summary>
public class ProductCategoryDto
{
    /// <summary>
    /// 分类ID
    /// </summary>
    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 父分类ID
    /// </summary>
    [JsonPropertyName("parent_id")]
    public int? ParentId { get; set; }

    /// <summary>
    /// 完整分类路径
    /// </summary>
    [JsonPropertyName("full_path")]
    public string FullPath { get; set; } = string.Empty;
}

/// <summary>
/// 商品图片信息DTO
/// </summary>
public class ProductImageDto
{
    /// <summary>
    /// 图片ID
    /// </summary>
    [JsonPropertyName("image_id")]
    public int ImageId { get; set; }

    /// <summary>
    /// 图片URL
    /// </summary>
    [JsonPropertyName("image_url")]
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// 缩略图URL
    /// </summary>
    [JsonPropertyName("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// 显示顺序
    /// </summary>
    [JsonPropertyName("display_order")]
    public int DisplayOrder { get; set; }
}
