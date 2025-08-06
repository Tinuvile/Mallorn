using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Models.DTOs.Product;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.File;
using CampusTrade.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.Product;

/// <summary>
/// 商品服务实现类
/// </summary>
public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductCacheService _productCache;
    private readonly ICategoryCacheService _categoryCache;
    private readonly IUserCacheService _userCache;
    private readonly IFileService _fileService;
    private readonly ILogger<ProductService> _logger;

    // 自动下架配置
    private const int DEFAULT_AUTO_REMOVE_DAYS = 20; // 默认20天自动下架
    private const int HIGH_VIEW_EXTEND_DAYS = 10;    // 高浏览量延期10天
    private const int HIGH_VIEW_THRESHOLD = 100;     // 高浏览量门槛

    public ProductService(
        IUnitOfWork unitOfWork,
        IProductCacheService productCache,
        ICategoryCacheService categoryCache,
        IUserCacheService userCache,
        IFileService fileService,
        ILogger<ProductService> logger)
    {
        _unitOfWork = unitOfWork;
        _productCache = productCache;
        _categoryCache = categoryCache;
        _userCache = userCache;
        _fileService = fileService;
        _logger = logger;
    }

    #region 商品发布与管理

    /// <summary>
    /// 发布商品
    /// </summary>
    public async Task<ApiResponse<ProductDetailDto>> CreateProductAsync(CreateProductDto createDto, int userId)
    {
        try
        {
            // 验证用户是否存在
            var user = await _userCache.GetUserAsync(userId);
            if (user == null || user.GetType().Name == "NullUser")
            {
                return ApiResponse<ProductDetailDto>.CreateError("用户不存在");
            }

            // 验证分类是否存在
            var categories = await _categoryCache.GetCategoryTreeAsync();
            if (categories == null || !categories.Any())
            {
                return ApiResponse<ProductDetailDto>.CreateError("分类系统暂时不可用，请稍后重试");
            }

            var category = FindCategoryById(categories, createDto.CategoryId);
            if (category == null)
            {
                return ApiResponse<ProductDetailDto>.CreateError("商品分类不存在");
            }

            // 检查用户是否有重复标题的商品
            var duplicateExists = await _unitOfWork.Products.IsProductExistsAsync(createDto.Title, userId);
            if (duplicateExists)
            {
                return ApiResponse<ProductDetailDto>.CreateError("您已发布过同名商品，请修改标题");
            }

            // 创建商品实体
            var product = new Models.Entities.Product
            {
                UserId = userId,
                CategoryId = createDto.CategoryId,
                Title = createDto.Title,
                Description = createDto.Description,
                BasePrice = createDto.BasePrice,
                PublishTime = DateTime.Now,
                Status = Models.Entities.Product.ProductStatus.OnSale,
                AutoRemoveTime = createDto.AutoRemoveTime ?? DateTime.Now.AddDays(DEFAULT_AUTO_REMOVE_DAYS),
                ViewCount = 0
            };

            // 保存商品
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            // 添加商品图片
            if (createDto.ImageUrls.Any())
            {
                await AddProductImagesInternal(product.ProductId, createDto.ImageUrls);
            }

            // 清除相关缓存
            await _productCache.InvalidateProductsByCategoryAsync(createDto.CategoryId);

            // 返回商品详情
            var productDetail = await GetProductDetailInternalAsync(product.ProductId, userId);
            return ApiResponse<ProductDetailDto>.CreateSuccess(productDetail, "商品发布成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布商品失败，UserId: {UserId}, Title: {Title}", userId, createDto.Title);
            return ApiResponse<ProductDetailDto>.CreateError("发布商品失败，请稍后重试");
        }
    }

    /// <summary>
    /// 更新商品信息
    /// </summary>
    public async Task<ApiResponse<ProductDetailDto>> UpdateProductAsync(int productId, UpdateProductDto updateDto, int userId)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByPrimaryKeyAsync(productId);
            if (product == null)
            {
                return ApiResponse<ProductDetailDto>.CreateError("商品不存在");
            }

            if (product.UserId != userId)
            {
                return ApiResponse<ProductDetailDto>.CreateError("无权限修改此商品");
            }

            // 更新商品信息
            var hasChanges = false;

            if (!string.IsNullOrEmpty(updateDto.Title) && updateDto.Title != product.Title)
            {
                // 检查重复标题
                var duplicateExists = await _unitOfWork.Products.IsProductExistsAsync(updateDto.Title, userId);
                if (duplicateExists)
                {
                    return ApiResponse<ProductDetailDto>.CreateError("您已发布过同名商品，请修改标题");
                }
                product.Title = updateDto.Title;
                hasChanges = true;
            }

            if (updateDto.Description != null && updateDto.Description != product.Description)
            {
                product.Description = updateDto.Description;
                hasChanges = true;
            }

            if (updateDto.BasePrice.HasValue && updateDto.BasePrice.Value != product.BasePrice)
            {
                product.BasePrice = updateDto.BasePrice.Value;
                hasChanges = true;
            }

            if (updateDto.CategoryId.HasValue && updateDto.CategoryId.Value != product.CategoryId)
            {
                // 验证新分类是否存在
                var categories = await _categoryCache.GetCategoryTreeAsync();
                if (categories == null || !categories.Any())
                {
                    return ApiResponse<ProductDetailDto>.CreateError("分类系统暂时不可用，请稍后重试");
                }

                var category = FindCategoryById(categories, updateDto.CategoryId.Value);
                if (category == null)
                {
                    return ApiResponse<ProductDetailDto>.CreateError("商品分类不存在");
                }

                var oldCategoryId = product.CategoryId;
                product.CategoryId = updateDto.CategoryId.Value;

                // 清除旧分类缓存
                await _productCache.InvalidateProductsByCategoryAsync(oldCategoryId);
                await _productCache.InvalidateProductsByCategoryAsync(updateDto.CategoryId.Value);
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(updateDto.Status) && updateDto.Status != product.Status)
            {
                product.Status = updateDto.Status;
                hasChanges = true;
            }

            if (updateDto.AutoRemoveTime.HasValue && updateDto.AutoRemoveTime.Value != product.AutoRemoveTime)
            {
                product.AutoRemoveTime = updateDto.AutoRemoveTime.Value;
                hasChanges = true;
            }

            if (hasChanges)
            {
                _unitOfWork.Products.Update(product);
                await _unitOfWork.SaveChangesAsync();

                // 清除商品缓存
                await _productCache.InvalidateProductCacheAsync(productId);
            }

            // 更新图片
            if (updateDto.ImageUrls != null)
            {
                await UpdateProductImagesInternal(productId, updateDto.ImageUrls);
            }

            // 返回更新后的商品详情
            var productDetail = await GetProductDetailInternalAsync(productId, userId);
            return ApiResponse<ProductDetailDto>.CreateSuccess(productDetail, "商品信息更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新商品失败，ProductId: {ProductId}, UserId: {UserId}", productId, userId);
            return ApiResponse<ProductDetailDto>.CreateError("更新商品失败，请稍后重试");
        }
    }

    /// <summary>
    /// 删除商品（下架）
    /// </summary>
    public async Task<ApiResponse> DeleteProductAsync(int productId, int userId)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByPrimaryKeyAsync(productId);
            if (product == null)
            {
                return ApiResponse.CreateError("商品不存在");
            }

            if (product.UserId != userId)
            {
                return ApiResponse.CreateError("无权限删除此商品");
            }

            // 设置为已下架状态
            product.Status = Models.Entities.Product.ProductStatus.OffShelf;
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            // 清除缓存
            await _productCache.InvalidateProductCacheAsync(productId);
            await _productCache.InvalidateProductsByCategoryAsync(product.CategoryId);

            _logger.LogInformation("商品下架成功，ProductId: {ProductId}, UserId: {UserId}", productId, userId);
            return ApiResponse.CreateSuccess("商品下架成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除商品失败，ProductId: {ProductId}, UserId: {UserId}", productId, userId);
            return ApiResponse.CreateError("删除商品失败，请稍后重试");
        }
    }

    /// <summary>
    /// 获取商品详情
    /// </summary>
    public async Task<ApiResponse<ProductDetailDto>> GetProductDetailAsync(int productId, int? currentUserId = null, bool increaseViewCount = true)
    {
        try
        {
            // 增加浏览次数
            if (increaseViewCount)
            {
                await _unitOfWork.Products.IncreaseViewCountAsync(productId);
                await _productCache.InvalidateProductCacheAsync(productId);
            }

            var productDetail = await GetProductDetailInternalAsync(productId, currentUserId);
            if (productDetail == null)
            {
                return ApiResponse<ProductDetailDto>.CreateError("商品不存在");
            }

            return ApiResponse<ProductDetailDto>.CreateSuccess(productDetail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取商品详情失败，ProductId: {ProductId}", productId);
            return ApiResponse<ProductDetailDto>.CreateError("获取商品详情失败，请稍后重试");
        }
    }

    /// <summary>
    /// 修改商品状态
    /// </summary>
    public async Task<ApiResponse> UpdateProductStatusAsync(int productId, string status, int userId)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByPrimaryKeyAsync(productId);
            if (product == null)
            {
                return ApiResponse.CreateError("商品不存在");
            }

            if (product.UserId != userId)
            {
                return ApiResponse.CreateError("无权限修改此商品状态");
            }

            product.Status = status;
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            // 清除缓存
            await _productCache.InvalidateProductCacheAsync(productId);
            await _productCache.InvalidateProductsByCategoryAsync(product.CategoryId);

            return ApiResponse.CreateSuccess("商品状态更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新商品状态失败，ProductId: {ProductId}, Status: {Status}, UserId: {UserId}", productId, status, userId);
            return ApiResponse.CreateError("更新商品状态失败，请稍后重试");
        }
    }

    #endregion

    #region 商品查询与搜索

    /// <summary>
    /// 分页查询商品列表
    /// </summary>
    public async Task<ApiResponse<ProductPagedListDto>> GetProductsAsync(ProductQueryDto queryDto, int? currentUserId = null)
    {
        try
        {
            var (products, totalCount) = await _unitOfWork.Products.GetPagedProductsAsync(
                queryDto.PageIndex,
                queryDto.PageSize,
                queryDto.CategoryId,
                queryDto.Status,
                queryDto.Keyword,
                queryDto.MinPrice,
                queryDto.MaxPrice,
                queryDto.UserId
            );

            var productDtos = await ConvertToProductListDtosAsync(products, currentUserId);

            // 应用额外的筛选条件

            if (queryDto.HasImageOnly == true)
            {
                productDtos = productDtos.Where(p => !string.IsNullOrEmpty(p.MainImageUrl)).ToList();
            }

            if (queryDto.MinViewCount.HasValue)
            {
                productDtos = productDtos.Where(p => p.ViewCount >= queryDto.MinViewCount.Value).ToList();
            }

            // 应用排序
            productDtos = ApplySorting(productDtos, queryDto.SortBy, queryDto.SortDirection).ToList();

            var totalPages = (int)Math.Ceiling((double)totalCount / queryDto.PageSize);

            var result = new ProductPagedListDto
            {
                Products = productDtos,
                TotalCount = totalCount,
                PageIndex = queryDto.PageIndex,
                PageSize = queryDto.PageSize,
                TotalPages = totalPages,
                HasNextPage = queryDto.PageIndex < totalPages - 1,
                HasPreviousPage = queryDto.PageIndex > 0
            };

            return ApiResponse<ProductPagedListDto>.CreateSuccess(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询商品列表失败，Query: {@QueryDto}", queryDto);
            return ApiResponse<ProductPagedListDto>.CreateError("查询商品列表失败，请稍后重试");
        }
    }

    /// <summary>
    /// 获取用户发布的商品列表
    /// </summary>
    public async Task<ApiResponse<ProductPagedListDto>> GetUserProductsAsync(int userId, int pageIndex = 0, int pageSize = 20, string? status = null)
    {
        try
        {
            var queryDto = new ProductQueryDto
            {
                UserId = userId,
                Status = status,
                PageIndex = pageIndex,
                PageSize = pageSize,
                SortBy = ProductSortBy.PublishTime,
                SortDirection = SortDirection.Descending
            };

            return await GetProductsAsync(queryDto, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户商品列表失败，UserId: {UserId}", userId);
            return ApiResponse<ProductPagedListDto>.CreateError("获取用户商品列表失败，请稍后重试");
        }
    }

    /// <summary>
    /// 获取热门商品列表
    /// </summary>
    public async Task<ApiResponse<List<ProductListDto>>> GetPopularProductsAsync(int count = 10, int? categoryId = null)
    {
        try
        {
            var products = await _unitOfWork.Products.GetTopViewProductsAsync(count);

            // 指定分类的筛选
            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }

            // 只获取在售商品
            products = products.Where(p => p.Status == Models.Entities.Product.ProductStatus.OnSale);

            var productDtos = await ConvertToProductListDtosAsync(products);

            // 标记为热门商品
            foreach (var dto in productDtos)
            {
                dto.IsPopular = true;
            }

            return ApiResponse<List<ProductListDto>>.CreateSuccess(productDtos.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取热门商品失败，Count: {Count}, CategoryId: {CategoryId}", count, categoryId);
            return ApiResponse<List<ProductListDto>>.CreateError("获取热门商品失败，请稍后重试");
        }
    }

    /// <summary>
    /// 搜索商品
    /// </summary>
    public async Task<ApiResponse<ProductPagedListDto>> SearchProductsAsync(string keyword, int pageIndex = 0, int pageSize = 20, int? categoryId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return ApiResponse<ProductPagedListDto>.CreateError("搜索关键词不能为空");
            }

            var queryDto = new ProductQueryDto
            {
                Keyword = keyword.Trim(),
                CategoryId = categoryId,
                PageIndex = pageIndex,
                PageSize = pageSize,
                Status = Models.Entities.Product.ProductStatus.OnSale, // 只搜索在售商品
                SortBy = ProductSortBy.ViewCount, // 按浏览量排序，让热门商品排前面
                SortDirection = SortDirection.Descending
            };

            return await GetProductsAsync(queryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索商品失败，Keyword: {Keyword}, CategoryId: {CategoryId}", keyword, categoryId);
            return ApiResponse<ProductPagedListDto>.CreateError("搜索商品失败，请稍后重试");
        }
    }

    #endregion

    #region 分类管理

    /// <summary>
    /// 获取分类树
    /// </summary>
    public async Task<ApiResponse<CategoryTreeDto>> GetCategoryTreeAsync(bool includeProductCount = true)
    {
        try
        {
            var categories = await _categoryCache.GetCategoryTreeAsync();
            if (categories == null || !categories.Any())
            {
                return ApiResponse<CategoryTreeDto>.CreateError("分类系统暂时不可用，请稍后重试");
            }

            var categoryDtos = await ConvertToCategoryDtosAsync(categories, includeProductCount);

            var result = new CategoryTreeDto
            {
                RootCategories = categoryDtos,
                TotalCount = CountAllCategories(categories),
                LastUpdateTime = DateTime.Now
            };

            return ApiResponse<CategoryTreeDto>.CreateSuccess(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分类树失败");
            return ApiResponse<CategoryTreeDto>.CreateError("获取分类树失败，请稍后重试");
        }
    }

    /// <summary>
    /// 获取分类面包屑导航
    /// </summary>
    public async Task<ApiResponse<CategoryBreadcrumbDto>> GetCategoryBreadcrumbAsync(int categoryId)
    {
        try
        {
            var categories = await _categoryCache.GetCategoryTreeAsync();
            var breadcrumbItems = new List<CategoryBreadcrumbItemDto>();

            if (GetCategoryBreadcrumbRecursive(categories, categoryId, breadcrumbItems, 1))
            {
                var result = new CategoryBreadcrumbDto
                {
                    Breadcrumb = breadcrumbItems
                };
                return ApiResponse<CategoryBreadcrumbDto>.CreateSuccess(result);
            }

            return ApiResponse<CategoryBreadcrumbDto>.CreateError("分类不存在");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分类面包屑导航失败，CategoryId: {CategoryId}", categoryId);
            return ApiResponse<CategoryBreadcrumbDto>.CreateError("获取分类面包屑导航失败，请稍后重试");
        }
    }

    /// <summary>
    /// 根据父分类获取子分类列表
    /// </summary>
    public async Task<ApiResponse<List<CategoryDto>>> GetSubCategoriesAsync(int? parentId = null)
    {
        try
        {
            var categories = await _categoryCache.GetCategoryTreeAsync();

            if (parentId == null)
            {
                // 获取一级分类
                var rootCategoryDtos = await ConvertToCategoryDtosAsync(categories, true);
                return ApiResponse<List<CategoryDto>>.CreateSuccess(rootCategoryDtos);
            }
            else
            {
                // 获取指定父分类的子分类
                var parentCategory = FindCategoryById(categories, parentId.Value);
                if (parentCategory == null)
                {
                    return ApiResponse<List<CategoryDto>>.CreateError("父分类不存在");
                }

                var subCategoryDtos = await ConvertToCategoryDtosAsync(parentCategory.Children.ToList(), true);
                return ApiResponse<List<CategoryDto>>.CreateSuccess(subCategoryDtos);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取子分类列表失败，ParentId: {ParentId}", parentId);
            return ApiResponse<List<CategoryDto>>.CreateError("获取子分类列表失败，请稍后重试");
        }
    }

    /// <summary>
    /// 获取分类下的商品列表
    /// </summary>
    public async Task<ApiResponse<ProductPagedListDto>> GetProductsByCategoryAsync(int categoryId, int pageIndex = 0, int pageSize = 20, bool includeSubCategories = true)
    {
        try
        {
            var categories = await _categoryCache.GetCategoryTreeAsync();
            var targetCategory = FindCategoryById(categories, categoryId);
            if (targetCategory == null)
            {
                return ApiResponse<ProductPagedListDto>.CreateError("分类不存在");
            }

            var categoryIds = new List<int> { categoryId };

            // 如果包含子分类，获取所有子分类ID
            if (includeSubCategories)
            {
                GetAllSubCategoryIds(targetCategory, categoryIds);
            }

            // 查询商品
            var queryDto = new ProductQueryDto
            {
                CategoryId = categoryId, // 这里仍使用单个分类ID，由Repository层处理子分类逻辑
                PageIndex = pageIndex,
                PageSize = pageSize,
                Status = Models.Entities.Product.ProductStatus.OnSale, // 只显示在售商品
                SortBy = ProductSortBy.PublishTime,
                SortDirection = SortDirection.Descending
            };

            return await GetProductsAsync(queryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分类商品列表失败，CategoryId: {CategoryId}", categoryId);
            return ApiResponse<ProductPagedListDto>.CreateError("获取分类商品列表失败，请稍后重试");
        }
    }

    #endregion

    #region 其他

    // 图片管理
    public async Task<ApiResponse> AddProductImagesAsync(int productId, List<string> imageUrls, int userId)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByPrimaryKeyAsync(productId);
            if (product == null)
            {
                return ApiResponse.CreateError("商品不存在");
            }

            if (product.UserId != userId)
            {
                return ApiResponse.CreateError("无权限操作此商品");
            }

            await AddProductImagesInternal(productId, imageUrls);
            await _productCache.InvalidateProductCacheAsync(productId);

            return ApiResponse.CreateSuccess("图片添加成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加商品图片失败，ProductId: {ProductId}", productId);
            return ApiResponse.CreateError("添加商品图片失败，请稍后重试");
        }
    }

    public async Task<ApiResponse> RemoveProductImageAsync(int productId, int imageId, int userId)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByPrimaryKeyAsync(productId);
            if (product == null)
            {
                return ApiResponse.CreateError("商品不存在");
            }

            if (product.UserId != userId)
            {
                return ApiResponse.CreateError("无权限操作此商品");
            }

            var image = await _unitOfWork.ProductImages.GetByPrimaryKeyAsync(imageId);
            if (image == null || image.ProductId != productId)
            {
                return ApiResponse.CreateError("图片不存在");
            }

            _unitOfWork.ProductImages.Delete(image);
            await _unitOfWork.SaveChangesAsync();
            await _productCache.InvalidateProductCacheAsync(productId);

            return ApiResponse.CreateSuccess("图片删除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除商品图片失败，ProductId: {ProductId}, ImageId: {ImageId}", productId, imageId);
            return ApiResponse.CreateError("删除商品图片失败，请稍后重试");
        }
    }

    public async Task<ApiResponse> UpdateProductImageOrderAsync(int productId, Dictionary<int, int> imageOrders, int userId)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByPrimaryKeyAsync(productId);
            if (product == null)
            {
                return ApiResponse.CreateError("商品不存在");
            }

            if (product.UserId != userId)
            {
                return ApiResponse.CreateError("无权限操作此商品");
            }

            var images = await _unitOfWork.ProductImages.FindAsync(img => img.ProductId == productId);
            foreach (var image in images)
            {
                if (imageOrders.TryGetValue(image.ImageId, out var newOrder))
                {
                    image.DisplayOrder = newOrder;
                    _unitOfWork.ProductImages.Update(image);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _productCache.InvalidateProductCacheAsync(productId);

            return ApiResponse.CreateSuccess("图片顺序更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新图片顺序失败，ProductId: {ProductId}", productId);
            return ApiResponse.CreateError("更新图片顺序失败，请稍后重试");
        }
    }

    // 智能下架管理
    /// <summary>
    /// 获取即将自动下架的商品列表
    /// </summary>
    public async Task<ApiResponse<List<ProductListDto>>> GetProductsToAutoRemoveAsync(int days = 7, int? userId = null)
    {
        try
        {
            var targetDate = DateTime.Now.AddDays(days);
            var products = await _unitOfWork.Products.GetAutoRemoveProductsAsync(targetDate);

            // 指定了用户ID的筛选
            if (userId.HasValue)
            {
                products = products.Where(p => p.UserId == userId.Value);
            }

            // 只获取在售商品
            products = products.Where(p => p.Status == Models.Entities.Product.ProductStatus.OnSale);

            var productDtos = await ConvertToProductListDtosAsync(products);
            return ApiResponse<List<ProductListDto>>.CreateSuccess(productDtos.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取即将下架商品失败，Days: {Days}, UserId: {UserId}", days, userId);
            return ApiResponse<List<ProductListDto>>.CreateError("获取即将下架商品失败，请稍后重试");
        }
    }

    /// <summary>
    /// 延期商品自动下架时间
    /// </summary>
    public async Task<ApiResponse> ExtendProductAutoRemoveTimeAsync(int productId, int extendDays, int userId)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByPrimaryKeyAsync(productId);
            if (product == null)
            {
                return ApiResponse.CreateError("商品不存在");
            }

            if (product.UserId != userId)
            {
                return ApiResponse.CreateError("无权限操作此商品");
            }

            if (product.Status != Models.Entities.Product.ProductStatus.OnSale)
            {
                return ApiResponse.CreateError("只能延期在售商品的下架时间");
            }

            // 延期下架时间
            if (product.AutoRemoveTime.HasValue)
            {
                product.AutoRemoveTime = product.AutoRemoveTime.Value.AddDays(extendDays);
            }
            else
            {
                product.AutoRemoveTime = DateTime.Now.AddDays(DEFAULT_AUTO_REMOVE_DAYS + extendDays);
            }

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            // 清除缓存
            await _productCache.InvalidateProductCacheAsync(productId);

            _logger.LogInformation("商品下架时间延期成功，ProductId: {ProductId}, ExtendDays: {ExtendDays}", productId, extendDays);
            return ApiResponse.CreateSuccess($"商品下架时间已延期{extendDays}天");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "延期商品下架时间失败，ProductId: {ProductId}, ExtendDays: {ExtendDays}", productId, extendDays);
            return ApiResponse.CreateError("延期商品下架时间失败，请稍后重试");
        }
    }

    /// <summary>
    /// 手动设置商品自动下架时间
    /// </summary>
    public async Task<ApiResponse> SetProductAutoRemoveTimeAsync(int productId, DateTime? autoRemoveTime, int userId)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByPrimaryKeyAsync(productId);
            if (product == null)
            {
                return ApiResponse.CreateError("商品不存在");
            }

            if (product.UserId != userId)
            {
                return ApiResponse.CreateError("无权限操作此商品");
            }

            // 验证设置的下架时间不能早于当前时间
            if (autoRemoveTime.HasValue && autoRemoveTime.Value <= DateTime.Now)
            {
                return ApiResponse.CreateError("下架时间不能早于当前时间");
            }

            product.AutoRemoveTime = autoRemoveTime;
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            // 清除缓存
            await _productCache.InvalidateProductCacheAsync(productId);

            var message = autoRemoveTime.HasValue
                ? $"商品自动下架时间已设置为{autoRemoveTime.Value:yyyy-MM-dd HH:mm}"
                : "商品自动下架时间已取消";

            return ApiResponse.CreateSuccess(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置商品下架时间失败，ProductId: {ProductId}", productId);
            return ApiResponse.CreateError("设置商品下架时间失败，请稍后重试");
        }
    }

    /// <summary>
    /// 处理自动下架商品（由定时任务调用）
    /// 智能下架逻辑：默认20天自动下架，高浏览量商品延期10天
    /// </summary>
    public async Task<(int ProcessedCount, int SuccessCount, int ExtendedCount)> ProcessAutoRemoveProductsAsync()
    {
        var processedCount = 0;
        var successCount = 0;
        var extendedCount = 0;

        try
        {
            // 获取应该下架的商品（当前时间之前）
            var productsToRemove = await _unitOfWork.Products.GetAutoRemoveProductsAsync(DateTime.Now);

            // 只处理在售商品
            var activeProducts = productsToRemove.Where(p => p.Status == Models.Entities.Product.ProductStatus.OnSale).ToList();

            foreach (var product in activeProducts)
            {
                processedCount++;

                try
                {
                    // 检查是否为高浏览量商品，如果是且还没有延期过，则延期
                    if (product.ViewCount >= HIGH_VIEW_THRESHOLD)
                    {
                        // 检查是否已经延期过（通过发布时间和下架时间的差值判断）
                        var daysBetween = (product.AutoRemoveTime ?? DateTime.Now).Subtract(product.PublishTime).Days;

                        // 如果时间差接近默认天数，说明还没有延期过
                        if (daysBetween <= DEFAULT_AUTO_REMOVE_DAYS + 2) // 允许2天的误差
                        {
                            // 延期10天
                            product.AutoRemoveTime = product.AutoRemoveTime?.AddDays(HIGH_VIEW_EXTEND_DAYS)
                                                   ?? DateTime.Now.AddDays(HIGH_VIEW_EXTEND_DAYS);
                            _unitOfWork.Products.Update(product);
                            extendedCount++;

                            _logger.LogInformation("高浏览量商品自动延期下架，ProductId: {ProductId}, ViewCount: {ViewCount}, NewRemoveTime: {NewRemoveTime}",
                                product.ProductId, product.ViewCount, product.AutoRemoveTime);
                            continue;
                        }
                    }

                    // 执行下架
                    product.Status = Models.Entities.Product.ProductStatus.OffShelf;
                    _unitOfWork.Products.Update(product);
                    successCount++;

                    _logger.LogInformation("商品自动下架成功，ProductId: {ProductId}, ViewCount: {ViewCount}, OriginalRemoveTime: {OriginalRemoveTime}",
                        product.ProductId, product.ViewCount, product.AutoRemoveTime);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理单个商品自动下架失败，ProductId: {ProductId}", product.ProductId);
                }
            }

            // 批量保存更改
            if (processedCount > 0)
            {
                await _unitOfWork.SaveChangesAsync();

                // 清除相关缓存
                foreach (var product in activeProducts)
                {
                    await _productCache.InvalidateProductCacheAsync(product.ProductId);
                    await _productCache.InvalidateProductsByCategoryAsync(product.CategoryId);
                }
            }

            _logger.LogInformation("自动下架处理完成，ProcessedCount: {ProcessedCount}, SuccessCount: {SuccessCount}, ExtendedCount: {ExtendedCount}",
                processedCount, successCount, extendedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理自动下架商品失败");
        }

        return (processedCount, successCount, extendedCount);
    }

    // TODO: 以下功能模块暂未实现，如有需要可后续添加：
    // - 统计与分析：商品统计信息、用户商品统计、分类商品统计
    // - 批量操作：批量更新状态、批量删除
    // - 商品推荐：相似商品推荐、用户推荐商品

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 转换为商品列表DTO
    /// </summary>
    private async Task<List<ProductListDto>> ConvertToProductListDtosAsync(IEnumerable<Models.Entities.Product> products, int? currentUserId = null)
    {
        var productList = products.ToList();
        if (!productList.Any()) return new List<ProductListDto>();

        var result = new List<ProductListDto>();

        foreach (var product in productList)
        {
            // 获取用户信息
            var userInfo = await GetValidUserInfoAsync(product.UserId);

            // 获取分类信息
            var categories = await _categoryCache.GetCategoryTreeAsync();
            var category = categories?.Count > 0 ? FindCategoryById(categories, product.CategoryId) : null;
            var categoryPath = categories?.Count > 0 ? GetCategoryPath(categories, product.CategoryId) : "未知分类";

            // 获取商品图片
            var images = await _unitOfWork.ProductImages.FindAsync(img => img.ProductId == product.ProductId);
            var mainImage = images.OrderBy(img => img.DisplayOrder).FirstOrDefault();

            var dto = new ProductListDto
            {
                ProductId = product.ProductId,
                Title = product.Title,
                BasePrice = product.BasePrice,
                PublishTime = product.PublishTime,
                ViewCount = product.ViewCount,
                Status = product.Status,
                MainImageUrl = mainImage?.ImageUrl,
                ThumbnailUrl = mainImage != null ? _fileService.GetThumbnailFileName(mainImage.ImageUrl) : null,

                DaysUntilAutoRemove = product.AutoRemoveTime?.Subtract(DateTime.Now).Days,
                IsPopular = product.ViewCount >= HIGH_VIEW_THRESHOLD,
                Tags = GetProductTags(product), // 获取商品标签
                User = new ProductUserDto
                {
                    UserId = product.UserId,
                    Username = userInfo?.Username,
                    Name = userInfo?.FullName,
                    AvatarUrl = "/images/default-avatar.png", // 默认头像
                    CreditScore = userInfo?.CreditScore ?? 0,
                    IsOnline = false // TODO: 实现在线状态检测
                },
                Category = new ProductCategoryDto
                {
                    CategoryId = product.CategoryId,
                    Name = category?.Name ?? "",
                    ParentId = category?.ParentId,
                    FullPath = categoryPath
                }
            };

            result.Add(dto);
        }

        return result;
    }

    /// <summary>
    /// 应用排序
    /// </summary>
    private IEnumerable<ProductListDto> ApplySorting(IEnumerable<ProductListDto> products, ProductSortBy? sortBy, SortDirection? sortDirection)
    {
        if (!products.Any()) return products;

        var isDescending = sortDirection == SortDirection.Descending;

        return sortBy switch
        {
            ProductSortBy.PublishTime => isDescending
                ? products.OrderByDescending(p => p.PublishTime)
                : products.OrderBy(p => p.PublishTime),
            ProductSortBy.Price => isDescending
                ? products.OrderByDescending(p => p.BasePrice)
                : products.OrderBy(p => p.BasePrice),
            ProductSortBy.ViewCount => isDescending
                ? products.OrderByDescending(p => p.ViewCount)
                : products.OrderBy(p => p.ViewCount),
            ProductSortBy.Title => isDescending
                ? products.OrderByDescending(p => p.Title)
                : products.OrderBy(p => p.Title),
            ProductSortBy.Status => isDescending
                ? products.OrderByDescending(p => p.Status)
                : products.OrderBy(p => p.Status),
            _ => products.OrderByDescending(p => p.PublishTime) // 默认按发布时间降序
        };
    }

    /// <summary>
    /// 获取商品标签
    /// </summary>
    private List<string> GetProductTags(Models.Entities.Product product)
    {
        var tags = new List<string>();



        // 根据发布时间添加新品标签
        if (product.PublishTime > DateTime.Now.AddDays(-3))
        {
            tags.Add("新品");
        }

        // 根据浏览量添加热门标签
        if (product.ViewCount >= HIGH_VIEW_THRESHOLD)
        {
            tags.Add("热门");
        }

        return tags;
    }

    /// <summary>
    /// 转换分类实体为DTO
    /// </summary>
    private async Task<List<CategoryDto>> ConvertToCategoryDtosAsync(List<Category> categories, bool includeProductCount = true)
    {
        var result = new List<CategoryDto>();

        foreach (var category in categories)
        {
            var productCount = 0;
            var activeProductCount = 0;

            if (includeProductCount)
            {
                productCount = await _unitOfWork.Categories.GetProductCountByCategoryAsync(category.CategoryId);
                activeProductCount = await _unitOfWork.Categories.GetActiveProductCountByCategoryAsync(category.CategoryId);
            }

            var level = GetCategoryLevel(category);
            var fullPath = await GetCategoryFullPath(category.CategoryId);

            var dto = new CategoryDto
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                ParentId = category.ParentId,
                Level = level,
                FullPath = fullPath,
                ProductCount = productCount,
                ActiveProductCount = activeProductCount,
                Children = await ConvertToCategoryDtosAsync(category.Children.ToList(), includeProductCount)
            };

            result.Add(dto);
        }

        return result;
    }

    /// <summary>
    /// 统计所有分类数量
    /// </summary>
    private int CountAllCategories(List<Category> categories)
    {
        var count = categories.Count;
        foreach (var category in categories)
        {
            count += CountAllCategories(category.Children.ToList());
        }
        return count;
    }

    /// <summary>
    /// 获取分类层级
    /// </summary>
    private int GetCategoryLevel(Category category)
    {
        var level = 1;
        var current = category;
        while (current.ParentId.HasValue)
        {
            level++;
            // 这里简化处理，实际应该查找父分类
            break; // 暂时只支持检测是否为根分类
        }
        return level;
    }

    /// <summary>
    /// 获取分类完整路径
    /// </summary>
    private async Task<string> GetCategoryFullPath(int categoryId)
    {
        var categories = await _categoryCache.GetCategoryTreeAsync();
        return GetCategoryPath(categories, categoryId);
    }

    /// <summary>
    /// 递归获取分类面包屑导航
    /// </summary>
    private bool GetCategoryBreadcrumbRecursive(List<Category> categories, int categoryId, List<CategoryBreadcrumbItemDto> breadcrumbItems, int level)
    {
        foreach (var category in categories)
        {
            var tempBreadcrumb = new List<CategoryBreadcrumbItemDto>(breadcrumbItems)
            {
                new CategoryBreadcrumbItemDto
                {
                    CategoryId = category.CategoryId,
                    Name = category.Name,
                    Level = level
                }
            };

            if (category.CategoryId == categoryId)
            {
                breadcrumbItems.Clear();
                breadcrumbItems.AddRange(tempBreadcrumb);
                return true;
            }

            if (GetCategoryBreadcrumbRecursive(category.Children.ToList(), categoryId, tempBreadcrumb, level + 1))
            {
                breadcrumbItems.Clear();
                breadcrumbItems.AddRange(tempBreadcrumb);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 获取所有子分类ID
    /// </summary>
    private void GetAllSubCategoryIds(Category category, List<int> categoryIds)
    {
        foreach (var child in category.Children)
        {
            categoryIds.Add(child.CategoryId);
            GetAllSubCategoryIds(child, categoryIds);
        }
    }

    /// <summary>
    /// 内部获取商品详情方法
    /// </summary>
    private async Task<ProductDetailDto?> GetProductDetailInternalAsync(int productId, int? currentUserId)
    {
        var product = await _unitOfWork.Products.GetWithIncludeAsync(
            p => p.ProductId == productId,
            null,
            p => p.User,
            p => p.Category,
            p => p.ProductImages
        );

        var productEntity = product.FirstOrDefault();
        if (productEntity == null) return null;

        // 获取用户信息
        var userInfo = await GetValidUserInfoAsync(productEntity.UserId);

        // 获取分类路径
        var categories = await _categoryCache.GetCategoryTreeAsync();
        var categoryPath = categories?.Count > 0 ? GetCategoryPath(categories, productEntity.CategoryId) : "未知分类";

        // 构建DTO
        var dto = new ProductDetailDto
        {
            ProductId = productEntity.ProductId,
            Title = productEntity.Title,
            Description = productEntity.Description,
            BasePrice = productEntity.BasePrice,
            PublishTime = productEntity.PublishTime,
            ViewCount = productEntity.ViewCount,
            AutoRemoveTime = productEntity.AutoRemoveTime,
            Status = productEntity.Status,
            UserId = productEntity.UserId,
            CategoryId = productEntity.CategoryId,

            DaysUntilAutoRemove = productEntity.AutoRemoveTime?.Subtract(DateTime.Now).Days,
            IsOwnProduct = currentUserId == productEntity.UserId,
            User = new ProductUserDto
            {
                UserId = productEntity.UserId,
                Username = userInfo?.Username,
                Name = userInfo?.FullName,
                AvatarUrl = "/images/default-avatar.png", // 默认头像
                CreditScore = userInfo?.CreditScore ?? 0,
                IsOnline = false // TODO: 实现在线状态检测
            },
            Category = new ProductCategoryDto
            {
                CategoryId = productEntity.CategoryId,
                Name = productEntity.Category?.Name ?? "",
                ParentId = productEntity.Category?.ParentId,
                FullPath = categoryPath
            },
            Images = productEntity.ProductImages.Select(img => new ProductImageDto
            {
                ImageId = img.ImageId,
                ImageUrl = img.ImageUrl,
                ThumbnailUrl = _fileService.GetThumbnailFileName(img.ImageUrl),
                DisplayOrder = img.DisplayOrder
            }).OrderBy(img => img.DisplayOrder).ToList()
        };

        return dto;
    }

    /// <summary>
    /// 添加商品图片（内部方法）
    /// </summary>
    private async Task AddProductImagesInternal(int productId, List<string> imageUrls)
    {
        var images = imageUrls.Select((url, index) => new ProductImage
        {
            ProductId = productId,
            ImageUrl = url,
            DisplayOrder = index
        }).ToList();

        await _unitOfWork.ProductImages.AddRangeAsync(images);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// 更新商品图片（内部方法）
    /// </summary>
    private async Task UpdateProductImagesInternal(int productId, List<string> imageUrls)
    {
        // 删除现有图片
        var existingImages = await _unitOfWork.ProductImages.FindAsync(img => img.ProductId == productId);
        if (existingImages.Any())
        {
            _unitOfWork.ProductImages.DeleteRange(existingImages);
        }

        // 添加新图片
        if (imageUrls.Any())
        {
            await AddProductImagesInternal(productId, imageUrls);
        }
        else
        {
            await _unitOfWork.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 根据ID查找分类
    /// </summary>
    private Category? FindCategoryById(List<Category> categories, int categoryId)
    {
        foreach (var category in categories)
        {
            if (category.CategoryId == categoryId)
                return category;

            var found = FindCategoryById(category.Children.ToList(), categoryId);
            if (found != null)
                return found;
        }
        return null;
    }

    /// <summary>
    /// 获取分类路径
    /// </summary>
    private string GetCategoryPath(List<Category> categories, int categoryId)
    {
        var path = new List<string>();
        GetCategoryPathRecursive(categories, categoryId, path);
        return string.Join(" > ", path);
    }

    /// <summary>
    /// 递归获取分类路径
    /// </summary>
    private bool GetCategoryPathRecursive(List<Category> categories, int categoryId, List<string> path)
    {
        foreach (var category in categories)
        {
            var tempPath = new List<string>(path) { category.Name };

            if (category.CategoryId == categoryId)
            {
                path.Clear();
                path.AddRange(tempPath);
                return true;
            }

            if (GetCategoryPathRecursive(category.Children.ToList(), categoryId, tempPath))
            {
                path.Clear();
                path.AddRange(tempPath);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 获取有效的用户信息（处理NullUser情况）
    /// </summary>
    private async Task<User?> GetValidUserInfoAsync(int userId)
    {
        try
        {
            var user = await _userCache.GetUserAsync(userId);

            // 检查是否为NullUser或null
            if (user == null || user.GetType().Name == "NullUser")
            {
                return null;
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取用户信息失败，UserId: {UserId}", userId);
            return null;
        }
    }

    #endregion
}
