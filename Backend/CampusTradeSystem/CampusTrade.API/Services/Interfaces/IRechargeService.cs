using CampusTrade.API.Models.DTOs.Payment;

namespace CampusTrade.API.Services.Interfaces
{
    /// <summary>
    /// 充值服务接口
    /// </summary>
    public interface IRechargeService
    {
        /// <summary>
        /// 创建充值订单
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="request">充值请求</param>
        /// <returns>充值响应</returns>
        Task<RechargeResponse> CreateRechargeAsync(int userId, CreateRechargeRequest request);

        /// <summary>
        /// 处理充值回调（第三方支付完成后调用）
        /// </summary>
        /// <param name="request">回调请求</param>
        /// <returns>处理结果</returns>
        Task<bool> HandleRechargeCallbackAsync(RechargeCallbackRequest request);

        /// <summary>
        /// 完成模拟充值
        /// </summary>
        /// <param name="rechargeId">充值记录ID</param>
        /// <param name="userId">用户ID</param>
        /// <returns>是否成功</returns>
        Task<bool> CompleteSimulationRechargeAsync(int rechargeId, int userId);

        /// <summary>
        /// 获取用户充值记录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <returns>充值记录列表</returns>
        Task<(List<RechargeResponse> Records, int TotalCount)> GetUserRechargeRecordsAsync(
            int userId, int pageIndex = 1, int pageSize = 10);

        /// <summary>
        /// 处理过期的充值订单
        /// </summary>
        /// <returns>处理的过期订单数量</returns>
        Task<int> ProcessExpiredRechargesAsync();
    }
}
