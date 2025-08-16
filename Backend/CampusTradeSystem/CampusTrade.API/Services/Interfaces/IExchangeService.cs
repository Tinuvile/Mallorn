using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Models.DTOs.Exchange;

namespace CampusTrade.API.Services.Interfaces
{
    /// <summary>
    /// 换物服务接口
    /// </summary>
    public interface IExchangeService
    {
        /// <summary>
        /// 创建换物请求
        /// </summary>
        /// <param name="exchangeRequest">换物请求DTO</param>
        /// <param name="userId">当前用户ID</param>
        /// <returns>操作结果</returns>
        Task<(bool Success, string Message, int? ExchangeRequestId)> CreateExchangeRequestAsync(ExchangeRequestDto exchangeRequest, int userId);

        /// <summary>
        /// 处理换物回应
        /// </summary>
        /// <param name="exchangeResponse">换物回应DTO</param>
        /// <param name="userId">当前用户ID</param>
        /// <returns>操作结果</returns>
        Task<(bool Success, string Message)> HandleExchangeResponseAsync(ExchangeResponseDto exchangeResponse, int userId);

        /// <summary>
        /// 获取用户的换物请求记录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>换物请求列表</returns>
        Task<(IEnumerable<ExchangeRequestInfoDto> ExchangeRequests, int TotalCount)> GetUserExchangeRequestsAsync(int userId, int pageIndex = 1, int pageSize = 10);

        /// <summary>
        /// 获取换物请求详情
        /// </summary>
        /// <param name="exchangeRequestId">换物请求ID</param>
        /// <param name="userId">当前用户ID</param>
        /// <returns>换物请求详情</returns>
        Task<ExchangeRequestInfoDto?> GetExchangeRequestDetailsAsync(int exchangeRequestId, int userId);
    }
}
