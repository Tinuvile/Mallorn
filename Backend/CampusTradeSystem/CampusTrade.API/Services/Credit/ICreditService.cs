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
        Task ApplyCreditChangeAsync(CreditEvent creditEvent);
    }
}
