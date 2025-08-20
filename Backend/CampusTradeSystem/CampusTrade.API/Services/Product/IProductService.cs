using CampusTrade.API.Models.DTOs.Common;
using CampusTrade.API.Models.DTOs.Product;
using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Services.Product;

/// <summary>
/// 商品服务接口
/// </summary>
public interface IProductService
{
    #region 商品发布与管理

    /// <summary>
    /// 发布商品
    /// </summary>
    /// <param name="createDto">创建商品请求DTO</param>
    /// <param name="userId">发布用户ID</param>
    /// <returns>商品详情</returns>
    Task<ApiResponse<ProductDetailDto>> CreateProductAsync(CreateProductDto createDto, int userId);

    /// <summary>
    /// 更新商品信息
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="updateDto">更新商品请求DTO</param>
    /// <param name="userId">操作用户ID</param>
    /// <returns>更新后的商品详情</returns>
    Task<ApiResponse<ProductDetailDto>> UpdateProductAsync(int productId, UpdateProductDto updateDto, int userId);

    /// <summary>
    /// 删除商品（下架）
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="userId">操作用户ID</param>
    /// <returns>操作结果</returns>
    Task<ApiResponse> DeleteProductAsync(int productId, int userId);

    /// <summary>
    /// 获取商品详情
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="currentUserId">当前用户ID（可选，用于判断是否为自己的商品）</param>
    /// <param name="increaseViewCount">是否增加浏览次数</param>
    /// <returns>商品详情</returns>
    Task<ApiResponse<ProductDetailDto>> GetProductDetailAsync(int productId, int? currentUserId = null, bool increaseViewCount = true);

    /// <summary>
    /// 修改商品状态
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="status">新状态</param>
    /// <param name="userId">操作用户ID</param>
    /// <returns>操作结果</returns>
    Task<ApiResponse> UpdateProductStatusAsync(int productId, string status, int userId);

    #endregion

    #region 商品查询与搜索

    /// <summary>
    /// 分页查询商品列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <param name="currentUserId">当前用户ID（可选）</param>
    /// <returns>分页商品列表</returns>
    Task<ApiResponse<ProductPagedListDto>> GetProductsAsync(ProductQueryDto queryDto, int? currentUserId = null);

    /// <summary>
    /// 获取用户发布的商品列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="pageIndex">页索引</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="status">商品状态筛选（可选）</param>
    /// <returns>分页商品列表</returns>
    Task<ApiResponse<ProductPagedListDto>> GetUserProductsAsync(int userId, int pageIndex = 0, int pageSize = 20, string? status = null);

    /// <summary>
    /// 获取热门商品列表
    /// </summary>
    /// <param name="count">获取数量</param>
    /// <param name="categoryId">分类ID（可选）</param>
    /// <returns>热门商品列表</returns>
    Task<ApiResponse<List<ProductListDto>>> GetPopularProductsAsync(int count = 10, int? categoryId = null);

    /// <summary>
    /// 搜索商品
    /// </summary>
    /// <param name="keyword">搜索关键词</param>
    /// <param name="pageIndex">页索引</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="categoryId">分类ID（可选）</param>
    /// <returns>搜索结果</returns>
    Task<ApiResponse<ProductPagedListDto>> SearchProductsAsync(string keyword, int pageIndex = 0, int pageSize = 20, int? categoryId = null);

    #endregion

    #region 分类管理

    /// <summary>
    /// 获取分类树
    /// </summary>
    /// <param name="includeProductCount">是否包含商品数量</param>
    /// <returns>分类树</returns>
    Task<ApiResponse<CategoryTreeDto>> GetCategoryTreeAsync(bool includeProductCount = true);

    /// <summary>
    /// 获取分类面包屑导航
    /// </summary>
    /// <param name="categoryId">分类ID</param>
    /// <returns>面包屑导航</returns>
    Task<ApiResponse<CategoryBreadcrumbDto>> GetCategoryBreadcrumbAsync(int categoryId);

    /// <summary>
    /// 根据父分类获取子分类列表
    /// </summary>
    /// <param name="parentId">父分类ID（null表示获取一级分类）</param>
    /// <returns>子分类列表</returns>
    Task<ApiResponse<List<CategoryDto>>> GetSubCategoriesAsync(int? parentId = null);

    /// <summary>
    /// 获取分类下的商品列表
    /// </summary>
    /// <param name="categoryId">分类ID</param>
    /// <param name="pageIndex">页索引</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="includeSubCategories">是否包含子分类的商品</param>
    /// <returns>分页商品列表</returns>
    Task<ApiResponse<ProductPagedListDto>> GetProductsByCategoryAsync(int categoryId, int pageIndex = 0, int pageSize = 20, bool includeSubCategories = true);

    #endregion

    #region 图片管理

    /// <summary>
    /// 为商品添加图片
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="imageUrls">图片URL列表</param>
    /// <param name="userId">操作用户ID</param>
    /// <returns>操作结果</returns>
    Task<ApiResponse> AddProductImagesAsync(int productId, List<string> imageUrls, int userId);

    /// <summary>
    /// 删除商品图片
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="imageId">图片ID</param>
    /// <param name="userId">操作用户ID</param>
    /// <returns>操作结果</returns>
    Task<ApiResponse> RemoveProductImageAsync(int productId, int imageId, int userId);

    /// <summary>
    /// 更新商品图片顺序
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="imageOrders">图片ID和显示顺序的映射</param>
    /// <param name="userId">操作用户ID</param>
    /// <returns>操作结果</returns>
    Task<ApiResponse> UpdateProductImageOrderAsync(int productId, Dictionary<int, int> imageOrders, int userId);

    #endregion

    #region 智能下架管理

    /// <summary>
    /// 获取即将自动下架的商品列表
    /// </summary>
    /// <param name="days">距离下架天数</param>
    /// <param name="userId">用户ID（可选，仅获取指定用户的商品）</param>
    /// <returns>即将下架的商品列表</returns>
    Task<ApiResponse<List<ProductListDto>>> GetProductsToAutoRemoveAsync(int days = 7, int? userId = null);

    /// <summary>
    /// 延期商品自动下架时间
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="extendDays">延期天数</param>
    /// <param name="userId">操作用户ID</param>
    /// <returns>操作结果</returns>
    Task<ApiResponse> ExtendProductAutoRemoveTimeAsync(int productId, int extendDays, int userId);

    /// <summary>
    /// 手动设置商品自动下架时间
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="autoRemoveTime">自动下架时间</param>
    /// <param name="userId">操作用户ID</param>
    /// <returns>操作结果</returns>
    Task<ApiResponse> SetProductAutoRemoveTimeAsync(int productId, DateTime? autoRemoveTime, int userId);

    /// <summary>
    /// 处理自动下架商品（定时任务调用）
    /// </summary>
    /// <returns>处理结果统计</returns>
    Task<(int ProcessedCount, int SuccessCount, int ExtendedCount)> ProcessAutoRemoveProductsAsync();

    #endregion

    // TODO: 以下功能模块暂未实现，后续添加：
    // #region 统计与分析 - 商品统计信息、用户商品统计、分类商品统计
    // #region 批量操作 - 批量更新状态、批量删除
    // #region 商品推荐 - 相似商品推荐、用户推荐商品
}
