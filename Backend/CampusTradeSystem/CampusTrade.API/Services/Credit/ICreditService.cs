using System.Threading.Tasks;
using CampusTrade.API.Models.DTOs;

namespace CampusTrade.API.Services
{
    /// <summary>
    /// 信用分服务接口
    /// 定义统一的信用分处理行为
    /// </summary>
    public interface ICreditService
    {
        /// <summary>
        /// 应用一次信用分变更
        /// </summary>
        /// <param name="creditEvent">信用变更事件</param>
        /// <param name="autoSave">是否自动保存，默认true。设置为false时由调用方控制事务</param>
        Task ApplyCreditChangeAsync(CreditEvent creditEvent, bool autoSave = true);

        /// <summary>
        /// 获取用户当前信用分
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户信用分，如果用户不存在返回null</returns>
        Task<decimal?> GetUserCreditScoreAsync(int userId);

        /// <summary>
        /// 检查用户是否满足最低信用分要求
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="minRequiredScore">最低要求分数</param>
        /// <returns>是否满足要求</returns>
        Task<bool> CheckMinCreditRequirementAsync(int userId, decimal minRequiredScore);
    }
}
