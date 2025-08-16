namespace CampusTrade.API.Models.DTOs.Report
{
    /// <summary>
    /// 举报关联商品分类信息DTO
    /// </summary>
    public class ReportProductCategoryDto
    {
        /// <summary>
        /// 分类ID
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// 分类名称
        /// </summary>
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// 举报ID
        /// </summary>
        public int ReportId { get; set; }

        /// <summary>
        /// 商品ID
        /// </summary>
        public int? ProductId { get; set; }

        /// <summary>
        /// 商品标题
        /// </summary>
        public string? ProductTitle { get; set; }
    }
}
