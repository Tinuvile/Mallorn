using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CampusTrade.API.Models.DTOs.Product;

/// <summary>
/// 商品查询条件DTO
/// </summary>
public class ProductQueryDto
{
    /// <summary>
    /// 关键词搜索（标题、描述）
    /// </summary>
    [JsonPropertyName("keyword")]
    public string? Keyword { get; set; }

    /// <summary>
    /// 分类ID
    /// </summary>
    [JsonPropertyName("category_id")]
    public int? CategoryId { get; set; }

    /// <summary>
    /// 最小价格
    /// </summary>
    [Range(0, 999999.99, ErrorMessage = "最小价格不能为负数")]
    [JsonPropertyName("min_price")]
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// 最大价格
    /// </summary>
    [Range(0, 999999.99, ErrorMessage = "最大价格不能为负数")]
    [JsonPropertyName("max_price")]
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// 商品状态
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// 发布用户ID
    /// </summary>
    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }



    /// <summary>
    /// 发布开始时间
    /// </summary>
    [JsonPropertyName("publish_start_time")]
    public DateTime? PublishStartTime { get; set; }

    /// <summary>
    /// 发布结束时间
    /// </summary>
    [JsonPropertyName("publish_end_time")]
    public DateTime? PublishEndTime { get; set; }

    /// <summary>
    /// 排序字段
    /// </summary>
    [JsonPropertyName("sort_by")]
    public ProductSortBy? SortBy { get; set; } = ProductSortBy.PublishTime;

    /// <summary>
    /// 排序方向
    /// </summary>
    [JsonPropertyName("sort_direction")]
    public SortDirection? SortDirection { get; set; } = Models.DTOs.Product.SortDirection.Descending;

    /// <summary>
    /// 页索引（从0开始）
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "页索引不能为负数")]
    [JsonPropertyName("page_index")]
    public int PageIndex { get; set; } = 0;

    /// <summary>
    /// 页大小
    /// </summary>
    [Range(1, 100, ErrorMessage = "页大小必须在1到100之间")]
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 是否只显示有图片的商品
    /// </summary>
    [JsonPropertyName("has_image_only")]
    public bool? HasImageOnly { get; set; }

    /// <summary>
    /// 最小浏览次数
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "最小浏览次数不能为负数")]
    [JsonPropertyName("min_view_count")]
    public int? MinViewCount { get; set; }
}

/// <summary>
/// 商品排序字段枚举
/// </summary>
public enum ProductSortBy
{
    /// <summary>
    /// 按发布时间排序
    /// </summary>
    PublishTime,

    /// <summary>
    /// 按价格排序
    /// </summary>
    Price,

    /// <summary>
    /// 按浏览次数排序
    /// </summary>
    ViewCount,

    /// <summary>
    /// 按标题排序
    /// </summary>
    Title,

    /// <summary>
    /// 按商品状态排序
    /// </summary>
    Status
}

/// <summary>
/// 排序方向枚举
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// 升序
    /// </summary>
    Ascending,

    /// <summary>
    /// 降序
    /// </summary>
    Descending
}

/// <summary>
/// 商品统计查询DTO
/// </summary>
public class ProductStatsQueryDto
{
    /// <summary>
    /// 开始时间
    /// </summary>
    [JsonPropertyName("start_time")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [JsonPropertyName("end_time")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 分类ID
    /// </summary>
    [JsonPropertyName("category_id")]
    public int? CategoryId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }
}

/// <summary>
/// 商品统计响应DTO
/// </summary>
public class ProductStatsDto
{
    /// <summary>
    /// 总商品数
    /// </summary>
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    /// <summary>
    /// 在售商品数
    /// </summary>
    [JsonPropertyName("on_sale_count")]
    public int OnSaleCount { get; set; }

    /// <summary>
    /// 已下架商品数
    /// </summary>
    [JsonPropertyName("off_shelf_count")]
    public int OffShelfCount { get; set; }

    /// <summary>
    /// 交易中商品数
    /// </summary>
    [JsonPropertyName("in_transaction_count")]
    public int InTransactionCount { get; set; }

    /// <summary>
    /// 总浏览次数
    /// </summary>
    [JsonPropertyName("total_view_count")]
    public long TotalViewCount { get; set; }

    /// <summary>
    /// 平均价格
    /// </summary>
    [JsonPropertyName("average_price")]
    public decimal AveragePrice { get; set; }

    /// <summary>
    /// 最高价格
    /// </summary>
    [JsonPropertyName("max_price")]
    public decimal MaxPrice { get; set; }

    /// <summary>
    /// 最低价格
    /// </summary>
    [JsonPropertyName("min_price")]
    public decimal MinPrice { get; set; }

    /// <summary>
    /// 按分类统计
    /// </summary>
    [JsonPropertyName("category_stats")]
    public List<CategoryStatsDto> CategoryStats { get; set; } = new();
}

/// <summary>
/// 分类统计DTO
/// </summary>
public class CategoryStatsDto
{
    /// <summary>
    /// 分类ID
    /// </summary>
    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    [JsonPropertyName("category_name")]
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// 商品数量
    /// </summary>
    [JsonPropertyName("product_count")]
    public int ProductCount { get; set; }

    /// <summary>
    /// 平均价格
    /// </summary>
    [JsonPropertyName("average_price")]
    public decimal AveragePrice { get; set; }
}
