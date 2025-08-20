using System.Text.Json.Serialization;

namespace CampusTrade.API.Models.DTOs.Product;

/// <summary>
/// 商品列表项响应DTO
/// </summary>
public class ProductListDto
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
    /// 商品状态
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 主图URL
    /// </summary>
    [JsonPropertyName("main_image_url")]
    public string? MainImageUrl { get; set; }

    /// <summary>
    /// 缩略图URL
    /// </summary>
    [JsonPropertyName("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// 发布用户信息
    /// </summary>
    [JsonPropertyName("user")]
    public ProductUserDto User { get; set; } = new();

    /// <summary>
    /// 分类信息
    /// </summary>
    [JsonPropertyName("category")]
    public ProductCategoryDto Category { get; set; } = new();



    /// <summary>
    /// 距离自动下架剩余天数
    /// </summary>
    [JsonPropertyName("days_until_auto_remove")]
    public int? DaysUntilAutoRemove { get; set; }

    /// <summary>
    /// 是否为热门商品（高浏览量）
    /// </summary>
    [JsonPropertyName("is_popular")]
    public bool IsPopular { get; set; }

    /// <summary>
    /// 商品标签
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// 分页商品列表响应DTO
/// </summary>
public class ProductPagedListDto
{
    /// <summary>
    /// 商品列表
    /// </summary>
    [JsonPropertyName("products")]
    public List<ProductListDto> Products { get; set; } = new();

    /// <summary>
    /// 总记录数
    /// </summary>
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    /// <summary>
    /// 页索引（从0开始）
    /// </summary>
    [JsonPropertyName("page_index")]
    public int PageIndex { get; set; }

    /// <summary>
    /// 页大小
    /// </summary>
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    /// <summary>
    /// 是否有下一页
    /// </summary>
    [JsonPropertyName("has_next_page")]
    public bool HasNextPage { get; set; }

    /// <summary>
    /// 是否有上一页
    /// </summary>
    [JsonPropertyName("has_previous_page")]
    public bool HasPreviousPage { get; set; }
}
