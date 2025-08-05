using CampusTrade.API.Models.DTOs.Payment;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.Order
{
    /// <summary>
    /// 充值服务实现
    /// </summary>
    public class RechargeService : IRechargeService
    {
        private readonly IRechargeRecordsRepository _rechargeRepository;
        private readonly IVirtualAccountsRepository _virtualAccountRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RechargeService> _logger;

        // 充值配置常量
        private const decimal MIN_RECHARGE_AMOUNT = 1.00m;
        private const decimal MAX_RECHARGE_AMOUNT = 10000.00m;
        private const int RECHARGE_TIMEOUT_MINUTES = 30;

        public RechargeService(
            IRechargeRecordsRepository rechargeRepository,
            IVirtualAccountsRepository virtualAccountRepository,
            IUnitOfWork unitOfWork,
            ILogger<RechargeService> logger)
        {
            _rechargeRepository = rechargeRepository;
            _virtualAccountRepository = virtualAccountRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<RechargeResponse> CreateRechargeAsync(int userId, CreateRechargeRequest request)
        {
            _logger.LogInformation("开始创建充值订单，用户ID: {UserId}, 金额: {Amount}, 方式: {Method}",
                userId, request.Amount, request.Method);

            // 验证充值金额
            if (request.Amount < MIN_RECHARGE_AMOUNT || request.Amount > MAX_RECHARGE_AMOUNT)
            {
                _logger.LogWarning("充值金额超出范围，用户ID: {UserId}, 金额: {Amount}", userId, request.Amount);
                throw new ArgumentException($"充值金额必须在 {MIN_RECHARGE_AMOUNT} - {MAX_RECHARGE_AMOUNT} 之间");
            }

            // 确保用户有虚拟账户
            _logger.LogInformation("检查用户虚拟账户，用户ID: {UserId}", userId);
            var account = await _virtualAccountRepository.GetByUserIdAsync(userId);
            if (account == null)
            {
                _logger.LogError("用户虚拟账户不存在，用户ID: {UserId}", userId);
                throw new InvalidOperationException("用户虚拟账户不存在，请联系管理员");
            }

            try
            {
                _logger.LogInformation("开始事务，创建充值记录");
                await _unitOfWork.BeginTransactionAsync();

                // 创建充值记录
                var rechargeRecord = new RechargeRecord
                {
                    UserId = userId,
                    Amount = request.Amount,
                    Status = "处理中", // 使用数据库允许的状态值
                    CreateTime = DateTime.UtcNow
                };

                _logger.LogInformation("保存充值记录到数据库");
                var savedRecord = await _rechargeRepository.AddAsync(rechargeRecord);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("用户 {UserId} 创建充值订单 {RechargeId}，金额 {Amount}，方式 {Method}",
                    userId, savedRecord.RechargeId, request.Amount, request.Method);

                var response = new RechargeResponse
                {
                    RechargeId = savedRecord.RechargeId,
                    Amount = savedRecord.Amount,
                    Method = request.Method,
                    Status = savedRecord.Status,
                    CreateTime = savedRecord.CreateTime,
                    ExpireTime = DateTime.UtcNow.AddMinutes(RECHARGE_TIMEOUT_MINUTES)
                };

                // 模拟充值不需要支付URL
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建充值订单异常，用户ID: {UserId}, 错误: {Error}", userId, ex.Message);
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public Task<bool> HandleRechargeCallbackAsync(RechargeCallbackRequest request)
        {
            // 由于只支持模拟充值，不需要处理第三方回调
            _logger.LogWarning("不支持第三方回调处理，充值记录 {RechargeId}", request.RechargeId);
            return Task.FromResult(false);
        }

        public async Task<bool> CompleteSimulationRechargeAsync(int rechargeId, int userId)
        {
            var recharge = await _rechargeRepository.GetByPrimaryKeyAsync(rechargeId);
            if (recharge == null || recharge.UserId != userId)
            {
                _logger.LogWarning("模拟充值：找不到充值记录或用户不匹配，充值记录 {RechargeId}，用户 {UserId}",
                    rechargeId, userId);
                return false;
            }

            if (recharge.Status != "处理中")
            {
                _logger.LogWarning("模拟充值：充值记录 {RechargeId} 状态不是处理中，当前状态：{Status}",
                    rechargeId, recharge.Status);
                return false;
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // 增加虚拟账户余额
                await _virtualAccountRepository.CreditAsync(userId, recharge.Amount,
                    $"模拟充值 - {rechargeId}");

                // 更新充值记录状态
                await _rechargeRepository.UpdateRechargeStatusAsync(rechargeId, "成功", DateTime.UtcNow);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("模拟充值完成，用户 {UserId}，充值记录 {RechargeId}，金额 {Amount}",
                    userId, rechargeId, recharge.Amount);

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "模拟充值失败，充值记录ID: {RechargeId}", rechargeId);
                return false;
            }
        }

        public async Task<(List<RechargeResponse> Records, int TotalCount)> GetUserRechargeRecordsAsync(
            int userId, int pageIndex = 1, int pageSize = 10)
        {
            var (records, totalCount) = await _rechargeRepository.GetByUserIdAsync(userId, pageIndex, pageSize);

            var responseList = records.Select(r => new RechargeResponse
            {
                RechargeId = r.RechargeId,
                Amount = r.Amount,
                Method = RechargeMethod.Simulation, // 由于原实体没有PaymentMethod，暂时使用默认值
                Status = r.Status,
                CreateTime = r.CreateTime,
                ExpireTime = r.CreateTime.AddMinutes(RECHARGE_TIMEOUT_MINUTES) // 计算过期时间
            }).ToList();

            return (responseList, totalCount);
        }

        public async Task<int> ProcessExpiredRechargesAsync()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("开始处理过期充值订单，开始时间: {StartTime}", startTime);

            try
            {
                // 使用现有的GetExpiredRechargesAsync方法
                var expiredRecharges = await _rechargeRepository.GetExpiredRechargesAsync(TimeSpan.FromMinutes(RECHARGE_TIMEOUT_MINUTES));
                int processedCount = 0;

                foreach (var recharge in expiredRecharges.Where(r => r.Status == "处理中"))
                {
                    try
                    {
                        await _unitOfWork.BeginTransactionAsync();

                        await _rechargeRepository.UpdateRechargeStatusAsync(recharge.RechargeId, "失败", null);
                        await _unitOfWork.SaveChangesAsync();
                        await _unitOfWork.CommitTransactionAsync();

                        processedCount++;
                        _logger.LogInformation("充值订单 {RechargeId} 已标记为失败", recharge.RechargeId);
                    }
                    catch (Exception ex)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        _logger.LogError(ex, "处理过期充值订单失败，充值ID: {RechargeId}", recharge.RechargeId);
                    }
                }

                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;
                _logger.LogInformation("处理过期充值订单完成，处理数量: {Count}，耗时: {Duration}ms",
                    processedCount, duration.TotalMilliseconds);

                return processedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理过期充值订单时发生异常");
                return 0;
            }
        }
    }
}
