using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CampusTrade.API.Models.DTOs.Product;

/// <summary>
/// 创建商品请求DTO
/// </summary>
public class CreateProductDto
{
    /// <summary>
    /// 商品标题
    /// </summary>
    [Required(ErrorMessage = "商品标题不能为空")]
    [StringLength(100, ErrorMessage = "商品标题不能超过100个字符")]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 商品描述
    /// </summary>
    [StringLength(5000, ErrorMessage = "商品描述不能超过5000个字符")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 基础价格
    /// </summary>
    [Required(ErrorMessage = "商品价格不能为空")]
    [Range(0.01, 999999.99, ErrorMessage = "商品价格必须在0.01到999999.99之间")]
    [JsonPropertyName("base_price")]
    public decimal BasePrice { get; set; }

    /// <summary>
    /// 分类ID
    /// </summary>
    [Required(ErrorMessage = "商品分类不能为空")]
    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }

    /// <summary>
    /// 商品图片URL列表
    /// </summary>
    [JsonPropertyName("image_urls")]
    public List<string> ImageUrls { get; set; } = new();



    /// <summary>
    /// 自动下架时间（可选，如果不设置则使用默认的20天）
    /// </summary>
    [JsonPropertyName("auto_remove_time")]
    public DateTime? AutoRemoveTime { get; set; }
}
