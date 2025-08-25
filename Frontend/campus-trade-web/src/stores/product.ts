import { defineStore } from 'pinia'
import { ref } from 'vue'
import { 
  productApi, 
  type ApiResponse,
  type ProductDetail,
  type ProductListItem,
  type CategoryTreeResponse,
  type CategoryItem,
  type ProductsToAutoRemoveResponse,
  type CreateProductRequest,
  type UpdateProductRequest,
  type ProductQueryParams,
  type ProductSearchParams,
  type ProductListResponse,
  type CategoryBreadcrumbResponse
} from '@/services/api'

// 从 api.ts 导出接口类型供其他组件使用
export type {
  ProductImage,
  ProductSpecification,
  ProductDetail,
  ProductListItem,
  CategoryItem,
  CategoryBreadcrumb,
  CreateProductRequest,
  UpdateProductRequest,
  ProductQueryParams,
  ProductSearchParams,
  ProductListResponse,
  CategoryTreeResponse,
  CategoryBreadcrumbResponse,
  ProductsToAutoRemoveResponse
} from '@/services/api'

export const useProductStore = defineStore('product', () => {
  const currentProduct = ref<ProductDetail | null>(null)
  const userProducts = ref<ProductListItem[]>([])
  const popularProducts = ref<ProductListItem[]>([])
  const categoryTree = ref<CategoryTreeResponse[]>([])
  const subCategories = ref<CategoryItem[]>([])

  // 获取商品详情
  const getProductDetail = async (productId: number): Promise<ApiResponse<ProductDetail>> => {
    try {
      const response = await productApi.getProductDetail(productId)
      if (response.success && response.data) {
        currentProduct.value = response.data
      }
      return response
    } catch (error: unknown) {
      console.error('获取商品详情失败:', error)
      let message = '获取商品详情失败'
      
      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }
      
      return {
        success: false,
        message,
        timestamp: new Date().toISOString()
      }
    }
  }

  // 发布商品
  const createProduct = async (productData: CreateProductRequest): Promise<ApiResponse<ProductDetail>> => {
    try {
      return await productApi.createProduct(productData)
    } catch (error: unknown) {
      console.error('发布商品失败:', error)
      let message = '发布商品失败'
      
      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }
      
      return {
        success: false,
        message,
        timestamp: new Date().toISOString()
      }
    }
  }

  // 更新商品
  const updateProduct = async (productId: number, productData: UpdateProductRequest): Promise<ApiResponse<ProductDetail>> => {
    try {
      const response = await productApi.updateProduct(productId, productData)
      if (response.success && response.data) {
        currentProduct.value = response.data
      }
      return response
    } catch (error: unknown) {
      console.error('更新商品失败:', error)
      let message = '更新商品失败'
      
      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }
      
      return {
        success: false,
        message,
        timestamp: new Date().toISOString()
      }
    }
  }

  // 删除商品
  const deleteProduct = async (productId: number): Promise<ApiResponse> => {
    try {
      return await productApi.deleteProduct(productId)
    } catch (error: unknown) {
      console.error('删除商品失败:', error)
      let message = '删除商品失败'
      
      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }
      
      return {
        success: false,
        message,
        timestamp: new Date().toISOString()
      }
    }
  }

  // 更新商品状态
  const updateProductStatus = async (productId: number, status: string): Promise<ApiResponse> => {
    try {
      return await productApi.updateProductStatus(productId, status)
    } catch (error: unknown) {
      console.error('更新商品状态失败:', error)
      let message = '更新商品状态失败'
      
      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }
      
      return {
        success: false,
        message,
        timestamp: new Date().toISOString()
      }
    }
  }

  // 查询商品列表
  const getProducts = async (queryParams: ProductQueryParams): Promise<ApiResponse<ProductListResponse>> => {
    try {
      return await productApi.getProducts(queryParams)
    } catch (error: unknown) {
      console.error('查询商品列表失败:', error)
      let message = '查询商品列表失败'
      
      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }
      
      return {
        success: false,
        message,
        data: {
          products: [],
          totalCount: 0,
          pageIndex: 0,
          pageSize: 0,
          totalPages: 0
        },
        timestamp: new Date().toISOString()
      }
    }
  }

  // 搜索商品
  const searchProducts = async (searchParams: ProductSearchParams): Promise<ApiResponse<ProductListResponse>> => {
    try {
      return await productApi.searchProducts(searchParams)
    } catch (error: unknown) {
      console.error('搜索商品失败:', error)
      let message = '搜索商品失败'
      
      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }
      
      return {
        success: false,
        message,
        data: {
          products: [],
          totalCount: 0,
          pageIndex: 0,
          pageSize: 0,
          totalPages: 0
        },
        timestamp: new Date().toISOString()
      }
    }
  }

  // 获取热门商品
  const getPopularProducts = async (count: number = 10, categoryId?: number): Promise<ApiResponse<ProductListItem[]>> => {
    try {
      const response = await productApi.getPopularProducts(count, categoryId)
      if (response.success && response.data) {
        popularProducts.value = response.data
      }
      return response
    } catch (error: unknown) {
      console.error('获取热门商品失败:', error)
      let message = '获取热门商品失败'
      
      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }
      
      return {
        success: false,
        message,
        data: [],
        timestamp: new Date().toISOString()
      }
    }
  }

  // 获取用户发布的商品
  const getUserProducts = async (
    userId?: number, 
    pageIndex: number = 0, 
    pageSize: number = 20, 
    status?: string
  ): Promise<ApiResponse<ProductListResponse>> => {
    try {
      const response = await productApi.getUserProducts(userId, pageIndex, pageSize, status)
      if (response.success && response.data) {
        userProducts.value = response.data.products
      }
      return response
    } catch (error: unknown) {
      console.error('获取用户商品失败:', error)
      let message = '获取用户商品失败'
      
      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }
      
      return {
        success: false,
        message,
        data: {
          products: [],
          totalCount: 0,
          pageIndex: 0,
          pageSize: 0,
          totalPages: 0
        },
        timestamp: new Date().toISOString()
      }
    }
  }

  // 获取分类树
  const getCategoryTree = async (includeProductCount: boolean = true): Promise<ApiResponse<CategoryTreeResponse[]>> => {
    try {
      const response = await productApi.getCategoryTree(includeProductCount)
      if (response.success && response.data) {
        categoryTree.value = response.data
      }
      return response
    } catch (error: unknown) {
      console.error('获取分类树失败:', error)
      let message = '获取分类树失败'
      
      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }
      
      return {
        success: false,
        message,
        data: [],
        timestamp: new Date().toISOString()
      }
    }
  }

  // 获取子分类
  const getSubCategories = async (parentId?: number): Promise<ApiResponse<CategoryItem[]>> => {
    try {
      const response = await productApi.getSubCategories(parentId)
      if (response.success && response.data) {
        subCategories.value = response.data
      }
      return response
    } catch (error: unknown) {
      console.error('获取子分类失败:', error)
      let message = '获取子分类失败'
      
      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }
      
      return {
        success: false,
        message,
        data: [],
        timestamp: new Date().toISOString()
      }
    }
  }

  // 获取分类面包屑
  const getCategoryBreadcrumb = async (categoryId: number): Promise<ApiResponse<CategoryBreadcrumbResponse>> => {
    try {
      return await productApi.getCategoryBreadcrumb(categoryId)
    } catch (error: unknown) {
      console.error('获取分类面包屑失败:', error)
      let message = '获取分类面包屑失败'
      
      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }
      
      return {
        success: false,
        message,
        timestamp: new Date().toISOString()
      }
    }
  }

  // 获取分类下的商品
  const getProductsByCategory = async (
    categoryId: number, 
    pageIndex: number = 0, 
    pageSize: number = 20, 
    includeSubCategories: boolean = true
  ): Promise<ApiResponse<ProductListResponse>> => {
    try {
      return await productApi.getProductsByCategory(categoryId, pageIndex, pageSize, includeSubCategories)
    } catch (error: unknown) {
      console.error('获取分类商品失败:', error)
      let message = '获取分类商品失败'
      
      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }
      
      return {
        success: false,
        message,
        data: {
          products: [],
          totalCount: 0,
          pageIndex: 0,
          pageSize: 0,
          totalPages: 0
        },
        timestamp: new Date().toISOString()
      }
    }
  }

  // 获取即将自动下架的商品
  const getProductsToAutoRemove = async (days: number = 7): Promise<ApiResponse<ProductsToAutoRemoveResponse>> => {
    try {
      return await productApi.getProductsToAutoRemove(days)
    } catch (error: unknown) {
      console.error('获取即将下架商品失败:', error)
      let message = '获取即将下架商品失败'
      
      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }
      
      return {
        success: false,
        message,
        data: {
          products: [],
          totalCount: 0
        },
        timestamp: new Date().toISOString()
      }
    }
  }

  // 延期商品下架时间
  const extendProductAutoRemoveTime = async (productId: number, extendDays: number): Promise<ApiResponse> => {
    try {
      return await productApi.extendProductAutoRemoveTime(productId, extendDays)
    } catch (error: unknown) {
      console.error('延期商品下架时间失败:', error)
      let message = '延期商品下架时间失败'
      
      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }
      
      return {
        success: false,
        message,
        timestamp: new Date().toISOString()
      }
    }
  }

  return {
    currentProduct,
    userProducts,
    popularProducts,
    categoryTree,
    subCategories,
    
    getProductDetail,
    createProduct,
    updateProduct,
    deleteProduct,
    updateProductStatus,
    getProducts,
    searchProducts,
    getPopularProducts,
    getUserProducts,
    getCategoryTree,
    getSubCategories,
    getCategoryBreadcrumb,
    getProductsByCategory,
    getProductsToAutoRemove,
    extendProductAutoRemoveTime,
  }
})