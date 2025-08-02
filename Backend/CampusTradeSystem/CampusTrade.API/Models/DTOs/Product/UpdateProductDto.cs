using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CampusTrade.API.Models.DTOs.Product;

/// <summary>
/// 更新商品请求DTO
/// </summary>
public class UpdateProductDto
{
    /// <summary>
    /// 商品标题
    /// </summary>
    [StringLength(100, ErrorMessage = "商品标题不能超过100个字符")]
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// 商品描述
    /// </summary>
    [StringLength(5000, ErrorMessage = "商品描述不能超过5000个字符")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 基础价格
    /// </summary>
    [Range(0.01, 999999.99, ErrorMessage = "商品价格必须在0.01到999999.99之间")]
    [JsonPropertyName("base_price")]
    public decimal? BasePrice { get; set; }

    /// <summary>
    /// 分类ID
    /// </summary>
    [JsonPropertyName("category_id")]
    public int? CategoryId { get; set; }

    /// <summary>
    /// 商品图片URL列表
    /// </summary>
    [JsonPropertyName("image_urls")]
    public List<string>? ImageUrls { get; set; }



    /// <summary>
    /// 商品状态
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// 自动下架时间
    /// </summary>
    [JsonPropertyName("auto_remove_time")]
    public DateTime? AutoRemoveTime { get; set; }
}
