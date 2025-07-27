using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CampusTrade.API.Repositories.Exchange
{
    public interface IExchangeRepository
    {
        Task AddAsync(ExchangeRequest exchangeRequest);
        Task<ExchangeRequest> GetByIdAsync(int exchangeId);
        Task UpdateAsync(ExchangeRequest exchangeRequest);
    }

    public class ExchangeRepository : IExchangeRepository
    {
        private readonly CampusTradeDbContext _dbContext;

        public ExchangeRepository(CampusTradeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 添加新的换物请求记录
        public async Task AddAsync(ExchangeRequest exchangeRequest)
        {
            await _dbContext.ExchangeRequests.AddAsync(exchangeRequest);
            await _dbContext.SaveChangesAsync();
        }

        // 根据ID查询换物请求记录
        public async Task<ExchangeRequest> GetByIdAsync(int exchangeId)
        {
            return await _dbContext.ExchangeRequests
                .FirstOrDefaultAsync(e => e.ExchangeId == exchangeId);
        }

        // 更新换物请求记录
        public async Task UpdateAsync(ExchangeRequest exchangeRequest)
        {
            _dbContext.ExchangeRequests.Update(exchangeRequest);
            await _dbContext.SaveChangesAsync();
        }
    }
}
