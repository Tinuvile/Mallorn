using CampusTrade.API.Models.DTOs.Product;

namespace CampusTrade.API.Models.DTOs.Admin
{
    /// <summary>
    /// 管理员商品管理请求DTO
    /// </summary>
    public class AdminProductQueryDto
    {
        /// <summary>
        /// 页索引
        /// </summary>
        public int PageIndex { get; set; } = 0;

        /// <summary>
        /// 页大小
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// 商品状态
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// 分类ID（分类管理员自动根据权限过滤）
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// 搜索关键字
        /// </summary>
        public string? SearchKeyword { get; set; }

        /// <summary>
        /// 用户ID（用于查询特定用户的商品）
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndDate { get; set; }
    }

    /// <summary>
    /// 管理员商品更新请求DTO
    /// </summary>
    public class AdminUpdateProductDto
    {
        /// <summary>
        /// 商品标题
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// 商品描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 基础价格
        /// </summary>
        public decimal? BasePrice { get; set; }

        /// <summary>
        /// 商品状态
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// 分类ID
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// 管理员操作备注
        /// </summary>
        public string? AdminNote { get; set; }
    }

    /// <summary>
    /// 管理员商品详情响应DTO
    /// </summary>
    public class AdminProductDetailDto : ProductDetailDto
    {
        /// <summary>
        /// 管理员操作日志
        /// </summary>
        public List<AdminProductOperationLogDto> OperationLogs { get; set; } = new List<AdminProductOperationLogDto>();

        /// <summary>
        /// 商品状态历史
        /// </summary>
        public List<ProductStatusHistoryDto> StatusHistory { get; set; } = new List<ProductStatusHistoryDto>();
    }

    /// <summary>
    /// 管理员商品操作日志DTO
    /// </summary>
    public class AdminProductOperationLogDto
    {
        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTime OperationTime { get; set; }

        /// <summary>
        /// 操作管理员姓名
        /// </summary>
        public string AdminName { get; set; } = string.Empty;

        /// <summary>
        /// 操作类型
        /// </summary>
        public string OperationType { get; set; } = string.Empty;

        /// <summary>
        /// 操作详情
        /// </summary>
        public string OperationDetail { get; set; } = string.Empty;
    }

    /// <summary>
    /// 商品状态历史DTO
    /// </summary>
    public class ProductStatusHistoryDto
    {
        /// <summary>
        /// 变更时间
        /// </summary>
        public DateTime ChangeTime { get; set; }

        /// <summary>
        /// 变更前状态
        /// </summary>
        public string? FromStatus { get; set; }

        /// <summary>
        /// 变更后状态
        /// </summary>
        public string ToStatus { get; set; } = string.Empty;

        /// <summary>
        /// 变更原因
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// 操作人
        /// </summary>
        public string? OperatorName { get; set; }
    }

    /// <summary>
    /// 批量操作商品请求DTO
    /// </summary>
    public class BatchProductOperationDto
    {
        /// <summary>
        /// 商品ID列表
        /// </summary>
        public List<int> ProductIds { get; set; } = new List<int>();

        /// <summary>
        /// 操作类型（下架、删除、审核通过等）
        /// </summary>
        public string OperationType { get; set; } = string.Empty;

        /// <summary>
        /// 操作原因
        /// </summary>
        public string? Reason { get; set; }
    }
}
