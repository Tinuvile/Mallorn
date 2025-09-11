using CampusTrade.API.Infrastructure.Utils;
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
                    CreateTime = TimeHelper.Now
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
                    ExpireTime = TimeHelper.AddMinutes(RECHARGE_TIMEOUT_MINUTES)
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
            _logger.LogInformation("开始完成模拟充值，充值记录 {RechargeId}，用户 {UserId}", rechargeId, userId);

            var recharge = await _rechargeRepository.GetByPrimaryKeyAsync(rechargeId);
            if (recharge == null)
            {
                _logger.LogWarning("模拟充值：找不到充值记录，充值记录 {RechargeId}", rechargeId);
                return false;
            }

            if (recharge.UserId != userId)
            {
                _logger.LogWarning("模拟充值：用户不匹配，充值记录 {RechargeId}，记录用户 {RecordUserId}，请求用户 {RequestUserId}",
                    rechargeId, recharge.UserId, userId);
                return false;
            }

            if (recharge.Status != "处理中")
            {
                _logger.LogWarning("模拟充值：充值记录 {RechargeId} 状态不是处理中，当前状态：{Status}",
                    rechargeId, recharge.Status);
                return false;
            }

            _logger.LogInformation("充值记录验证通过，开始执行模拟充值，充值记录 {RechargeId}，金额 {Amount}",
                rechargeId, recharge.Amount);

            try
            {
                _logger.LogInformation("开始事务，准备更新余额和充值状态");
                await _unitOfWork.BeginTransactionAsync();

                // 增加虚拟账户余额
                _logger.LogInformation("调用CreditAsync增加余额，用户 {UserId}，金额 {Amount}", userId, recharge.Amount);
                var creditResult = await _virtualAccountRepository.CreditAsync(userId, recharge.Amount,
                    $"模拟充值 - {rechargeId}");

                if (!creditResult)
                {
                    _logger.LogError("增加虚拟账户余额失败，用户 {UserId}，充值记录 {RechargeId}", userId, rechargeId);
                    await _unitOfWork.RollbackTransactionAsync();
                    return false;
                }

                _logger.LogInformation("余额增加成功，开始更新充值记录状态");
                // 更新充值记录状态
                await _rechargeRepository.UpdateRechargeStatusAsync(rechargeId, "成功", TimeHelper.Now);

                _logger.LogInformation("保存更改到数据库");
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("模拟充值完成，用户 {UserId}，充值记录 {RechargeId}，金额 {Amount}",
                    userId, rechargeId, recharge.Amount);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "模拟充值执行异常，充值记录ID: {RechargeId}，错误信息: {ErrorMessage}",
                    rechargeId, ex.Message);
                try
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogInformation("事务已回滚");
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "事务回滚失败");
                }
                return false;
            }
        }

        public async Task<(List<RechargeResponse> Records, int TotalCount)> GetUserRechargeRecordsAsync(
            int userId, int pageIndex = 1, int pageSize = 10)
        {
            _logger.LogInformation("获取用户充值记录，用户ID: {UserId}, 页码: {PageIndex}, 页大小: {PageSize}",
                userId, pageIndex, pageSize);

            var (records, totalCount) = await _rechargeRepository.GetByUserIdAsync(userId, pageIndex, pageSize);

            var responseList = records.Select(r => new RechargeResponse
            {
                RechargeId = r.RechargeId,
                Amount = r.Amount,
                Method = RechargeMethod.Simulation, // 目前只支持模拟充值
                Status = r.Status,
                CreateTime = r.CreateTime,
                ExpireTime = r.CompleteTime ?? r.CreateTime.AddMinutes(RECHARGE_TIMEOUT_MINUTES)
            }).ToList();

            _logger.LogInformation("用户 {UserId} 充值记录查询完成，记录数: {Count}, 总数: {TotalCount}",
                userId, responseList.Count, totalCount);

            return (responseList, totalCount);
        }

        public async Task<int> ProcessExpiredRechargesAsync()
        {
            var startTime = TimeHelper.Now;
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

                var endTime = TimeHelper.Now;
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
