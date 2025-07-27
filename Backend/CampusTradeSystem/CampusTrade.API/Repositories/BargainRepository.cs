using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CampusTrade.API.Repositories.Bargain
{
    public interface IBargainRepository
    {
        Task AddAsync(Negotiation negotiation);
        Task<Negotiation> GetByIdAsync(int negotiationId);
        Task UpdateAsync(Negotiation negotiation);
    }

    public class BargainRepository : IBargainRepository
    {
        private readonly CampusTradeDbContext _dbContext;

        public BargainRepository(CampusTradeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 添加新的议价记录
        public async Task AddAsync(Negotiation negotiation)
        {
            await _dbContext.Negotiations.AddAsync(negotiation);
            await _dbContext.SaveChangesAsync();
        }

        // 根据ID查询议价记录
        public async Task<Negotiation> GetByIdAsync(int negotiationId)
        {
            return await _dbContext.Negotiations
                .FirstOrDefaultAsync(n => n.NegotiationId == negotiationId);
        }

        // 更新议价记录
        public async Task UpdateAsync(Negotiation negotiation)
        {
            _dbContext.Negotiations.Update(negotiation);
            await _dbContext.SaveChangesAsync();
        }
    }
}
