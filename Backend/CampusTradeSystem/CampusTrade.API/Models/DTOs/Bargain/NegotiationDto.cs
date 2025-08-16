namespace CampusTrade.API.Models.DTOs.Bargain
{
    /// <summary>
    /// 议价信息DTO
    /// </summary>
    public class NegotiationDto
    {
        /// <summary>
        /// 议价ID
        /// </summary>
        public int NegotiationId { get; set; }

        /// <summary>
        /// 订单ID
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// 提议价格
        /// </summary>
        public decimal ProposedPrice { get; set; }

        /// <summary>
        /// 议价状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
