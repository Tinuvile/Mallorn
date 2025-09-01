using CampusTrade.API.Models.DTOs.Bargain;
using CampusTrade.API.Models.DTOs.Common;

namespace CampusTrade.API.Services.Interfaces
{
    /// <summary>
    /// 议价服务接口
    /// </summary>
    public interface IBargainService
    {
        /// <summary>
        /// 创建议价请求
        /// </summary>
        /// <param name="bargainRequest">议价请求DTO</param>
        /// <param name="userId">当前用户ID</param>
        /// <returns>操作结果</returns>
        Task<(bool Success, string Message, int? NegotiationId)> CreateBargainRequestAsync(BargainRequestDto bargainRequest, int userId);

        /// <summary>
        /// 处理议价回应
        /// </summary>
        /// <param name="bargainResponse">议价回应DTO</param>
        /// <param name="userId">当前用户ID</param>
        /// <returns>操作结果</returns>
        Task<(bool Success, string Message)> HandleBargainResponseAsync(BargainResponseDto bargainResponse, int userId);

        /// <summary>
        /// 获取用户的议价记录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>议价记录列表</returns>
        Task<(IEnumerable<NegotiationDto> Negotiations, int TotalCount)> GetUserNegotiationsAsync(int userId, int pageIndex = 1, int pageSize = 10);

        /// <summary>
        /// 获取议价详情
        /// </summary>
        /// <param name="negotiationId">议价ID</param>
        /// <param name="userId">当前用户ID</param>
        /// <returns>议价详情</returns>
        Task<NegotiationDto?> GetNegotiationDetailsAsync(int negotiationId, int userId);
    }
}
