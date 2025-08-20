using CampusTrade.API.Models.DTOs.Order;
using CampusTrade.API.Models.DTOs.Payment;
using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Services.Interfaces
{
    /// <summary>
    /// 订单服务接口
    /// </summary>
    public interface IOrderService
    {
        #region 订单创建
        /// <summary>
        /// 创建订单
        /// </summary>
        /// <param name="userId">当前用户ID</param>
        /// <param name="request">创建订单请求</param>
        /// <returns>创建的订单详情</returns>
        Task<OrderDetailResponse> CreateOrderAsync(int userId, CreateOrderRequest request);
        #endregion

        #region 订单查询
        /// <summary>
        /// 获取订单详情
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <param name="userId">当前用户ID（用于权限验证）</param>
        /// <returns>订单详情</returns>
        Task<OrderDetailResponse?> GetOrderDetailAsync(int orderId, int userId);

        /// <summary>
        /// 获取用户的订单列表（分页）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="role">用户角色（buyer/seller）</param>
        /// <param name="status">订单状态筛选</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <returns>订单列表和总数</returns>
        Task<(List<OrderListResponse> Orders, int TotalCount)> GetUserOrdersAsync(
            int userId, string? role = null, string? status = null, int pageIndex = 1, int pageSize = 10);

        /// <summary>
        /// 获取商品的订单列表
        /// </summary>
        /// <param name="productId">商品ID</param>
        /// <param name="userId">当前用户ID（用于权限验证）</param>
        /// <returns>订单列表</returns>
        Task<List<OrderListResponse>> GetProductOrdersAsync(int productId, int userId);

        /// <summary>
        /// 获取用户订单统计
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>订单统计信息</returns>
        Task<OrderStatisticsResponse> GetUserOrderStatisticsAsync(int userId);
        #endregion

        #region 订单状态管理
        /// <summary>
        /// 更新订单状态
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <param name="userId">当前用户ID</param>
        /// <param name="request">状态更新请求</param>
        /// <returns>是否更新成功</returns>
        Task<bool> UpdateOrderStatusAsync(int orderId, int userId, UpdateOrderStatusRequest request);

        /// <summary>
        /// 买家确认付款
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <param name="userId">买家用户ID</param>
        /// <returns>是否操作成功</returns>
        Task<bool> ConfirmPaymentAsync(int orderId, int userId);

        /// <summary>
        /// 卖家发货
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <param name="userId">卖家用户ID</param>
        /// <param name="trackingInfo">物流信息</param>
        /// <returns>是否操作成功</returns>
        Task<bool> ShipOrderAsync(int orderId, int userId, string? trackingInfo = null);

        /// <summary>
        /// 买家确认收货
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <param name="userId">买家用户ID</param>
        /// <returns>是否操作成功</returns>
        Task<bool> ConfirmDeliveryAsync(int orderId, int userId);

        /// <summary>
        /// 完成订单
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <param name="userId">用户ID</param>
        /// <returns>是否操作成功</returns>
        Task<bool> CompleteOrderAsync(int orderId, int userId);

        /// <summary>
        /// 取消订单
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <param name="userId">用户ID</param>
        /// <param name="reason">取消原因</param>
        /// <returns>是否操作成功</returns>
        Task<bool> CancelOrderAsync(int orderId, int userId, string? reason = null);

        /// <summary>
        /// 使用虚拟账户支付订单
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <param name="userId">买家用户ID</param>
        /// <returns>支付结果</returns>
        Task<PaymentResult> PayOrderWithVirtualAccountAsync(int orderId, int userId);
        #endregion

        #region 订单超时管理
        /// <summary>
        /// 检查并处理过期订单
        /// </summary>
        /// <returns>处理的过期订单数量</returns>
        Task<int> ProcessExpiredOrdersAsync();

        /// <summary>
        /// 获取即将过期的订单（用于提醒）
        /// </summary>
        /// <param name="beforeMinutes">多少分钟内过期</param>
        /// <returns>即将过期的订单列表</returns>
        Task<List<OrderDetailResponse>> GetExpiringOrdersAsync(int beforeMinutes = 30);
        #endregion

        #region 订单验证
        /// <summary>
        /// 验证订单状态转换是否合法
        /// </summary>
        /// <param name="currentStatus">当前状态</param>
        /// <param name="newStatus">新状态</param>
        /// <param name="userRole">用户角色（buyer/seller）</param>
        /// <returns>是否合法</returns>
        bool IsValidStatusTransition(string currentStatus, string newStatus, string userRole);

        /// <summary>
        /// 检查用户是否有权限操作订单
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <param name="userId">用户ID</param>
        /// <returns>是否有权限</returns>
        Task<bool> HasOrderPermissionAsync(int orderId, int userId);
        #endregion
    }
}
