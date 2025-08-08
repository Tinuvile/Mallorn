using AutoMapper;
using CampusTrade.API.DTOs.Category;
using CampusTrade.API.Exceptions;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoriesRepository _categoryRepository;
        private readonly ICategoryCacheService _cacheService;
        private readonly ILogger<CategoryService> _logger;
        private readonly IMapper _mapper;

        public CategoryService(
            ICategoriesRepository categoryRepository,
            ICategoryCacheService cacheService,
            ILogger<CategoryService> logger,
            IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _cacheService = cacheService;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<List<CategoryDto>> GetCategoryTreeAsync()
        {
            var tree = await _cacheService.GetCategoryTreeAsync();
            return _mapper.Map<List<CategoryDto>>(tree);
        }

        public async Task<string> GetCategoryFullNameAsync(int categoryId)
        {
            return await _categoryRepository.GetCategoryFullNameAsync(categoryId);
        }

        public async Task<bool> IsThirdLevelAsync(int categoryId)
        {
            return await _categoryRepository.IsThirdLevelAsync(categoryId);
        }

        public async Task CreateCategoryAsync(CreateCategoryDto dto)
        {
            var existing = await _categoryRepository.GetCategoryByNameAsync(dto.Name, dto.ParentId);
            if (existing != null)
                throw new BusinessException("同级分类下名称不能重复");

            var category = new Category
            {
                Name = dto.Name,
                ParentId = dto.ParentId
            };
            await _categoryRepository.AddAsync(category);
            await _cacheService.RefreshCategoryTreeAsync();
            _logger.LogInformation("创建分类成功：{Name} (ParentId: {ParentId})", dto.Name, dto.ParentId);
        }

        public async Task UpdateCategoryAsync(int categoryId, UpdateCategoryDto dto)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId);
            if (category == null)
                throw new NotFoundException("分类不存在");

            var duplicate = await _categoryRepository.GetCategoryByNameAsync(dto.Name, category.ParentId);
            if (duplicate != null && duplicate.CategoryId != categoryId)
                throw new BusinessException("同级分类下名称不能重复");

            category.Name = dto.Name;
            await _categoryRepository.UpdateAsync(category);
            await _cacheService.RefreshCategoryTreeAsync();
            _logger.LogInformation("更新分类成功：{Id} -> {NewName}", categoryId, dto.Name);
        }

        public async Task DeleteCategoryAsync(int categoryId)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId);
            if (category == null)
                throw new NotFoundException("分类不存在");

            if (!await _categoryRepository.CanDeleteCategoryAsync(categoryId))
                throw new BusinessException("该分类下存在子分类或商品，无法删除");

            await _categoryRepository.DeleteAsync(category);
            await _cacheService.RefreshCategoryTreeAsync();
            _logger.LogInformation("删除分类成功：{Id}", categoryId);
        }
    }
}
