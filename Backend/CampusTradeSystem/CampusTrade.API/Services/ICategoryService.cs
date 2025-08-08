using CampusTrade.API.DTOs.Category;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CampusTrade.API.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetCategoryTreeAsync();
        Task<string> GetCategoryFullNameAsync(int categoryId);
        Task<bool> IsThirdLevelAsync(int categoryId);
        Task CreateCategoryAsync(CreateCategoryDto dto);
        Task UpdateCategoryAsync(int categoryId, UpdateCategoryDto dto);
        Task DeleteCategoryAsync(int categoryId);
    }
}