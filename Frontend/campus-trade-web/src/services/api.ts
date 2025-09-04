import axios, { type InternalAxiosRequestConfig, type AxiosResponse, type AxiosError } from 'axios'
import { backendToFrontendStatus, frontendToBackendStatus } from '@/utils/orderStatusMapping'

// 创建 axios 实例
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5085',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
})

// 请求拦截器
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error: AxiosError) => {
    return Promise.reject(error)
  }
)

// 响应拦截器
api.interceptors.response.use(
  (response: AxiosResponse) => {
    return response.data
  },
  (error: AxiosError) => {
    console.error('API请求错误:', error)
    console.error('错误响应数据:', error.response?.data)
    console.error('错误状态码:', error.response?.status)
    console.error('错误头信息:', error.response?.headers)

    if (error.response?.status === 401) {
      // 清除 token 并跳转到登录页
      localStorage.removeItem('token')
      localStorage.removeItem('user')
      window.location.href = '/login'
    }

    // 保留原始错误信息，让上层处理
    return Promise.reject(error)
  }
)

// 生成设备ID
const generateDeviceId = (): string => {
  const saved = localStorage.getItem('deviceId')
  if (saved) return saved

  const deviceId = 'web_' + Math.random().toString(36).substr(2, 9) + '_' + Date.now().toString(36)
  localStorage.setItem('deviceId', deviceId)
  return deviceId
}

// 获取设备名称
const getDeviceName = (): string => {
  const userAgent = navigator.userAgent
  let deviceName = 'Unknown Device'

  if (userAgent.includes('Chrome')) deviceName = 'Chrome Browser'
  else if (userAgent.includes('Firefox')) deviceName = 'Firefox Browser'
  else if (userAgent.includes('Safari')) deviceName = 'Safari Browser'
  else if (userAgent.includes('Edge')) deviceName = 'Edge Browser'

  return deviceName
}

// 后端API响应格式
export interface ApiResponse<T = unknown> {
  success: boolean
  message: string
  data?: T
  error_code?: string
  timestamp: string
}

// 登录接口数据格式（匹配后端LoginWithDeviceRequest）
export interface LoginData {
  username: string
  password: string
  device_id?: string
  device_name?: string
  remember_me?: boolean
}

// 注册接口数据格式（匹配后端RegisterDto）
export interface RegisterData {
  student_id: string
  name: string
  email: string
  password: string
  confirm_password: string
  username?: string
  phone?: string
}

// Token响应格式（匹配后端TokenResponse）
export interface TokenResponse {
  access_token: string
  refresh_token: string
  token_type: string
  expires_in: number
  expires_at: string
  user_id: number
  username: string
  email: string
  student_id: string
  credit_score: number
  device_id?: string
  user_status: string
  email_verified: boolean
  two_factor_enabled: boolean
  refresh_expires_at: string
}

// 用户信息格式
export interface UserInfo {
  userId: number
  username: string
  email: string
  fullName: string
  phone?: string
  studentId: string
  creditScore: number
  emailVerified: boolean
  createdAt: string
  student?: {
    studentId: string
    name: string
    department: string
  }
}

export interface SendCodeRequest {
  userId: number
  email: string
}

export interface VerifyCodeRequest {
  userId: number
  code: string
}

export interface LogoutAllResponse {
  revokedTokens: number
}

// 用户详细信息响应
export interface UserProfileResponse {
  userId: number
  username: string
  email: string
  fullName?: string
  phone?: string
  studentId: string
  creditScore: number
  emailVerified: boolean
  isActive: boolean
  createdAt: string
  lastLoginAt?: string
  lastLoginIp?: string
  loginCount: number
  student?: {
    studentId: string
    name: string
    department: string
  }
  virtualAccount?: {
    accountId: number
    balance: number
    createdAt: string
  }
}

// 更新用户信息请求
export interface UpdateUserProfileData {
  username?: string
  fullName?: string
  phone?: string
}

// 信用历史响应
export interface CreditHistoryResponse {
  items: CreditHistoryItem[]
  totalCount: number
  totalPages: number
  currentPage: number
}

export interface CreditHistoryItem {
  logId: number
  changeType: string
  newScore: number
  createdAt: string
}

// 修改密码请求
export interface ChangePasswordData {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

// 用户统计信息响应
export interface UserStatisticsResponse {
  publishedProducts: number
  buyOrders: number
  sellOrders: number
  receivedReviews: number
  registrationDays: number
}

// 安全信息响应
export interface SecurityInfoResponse {
  emailVerified: boolean
  twoFactorEnabled: boolean
  passwordChangedAt?: string
  lastLoginAt?: string
  lastLoginIp?: string
  activeDevicesCount: number
  recentLogins: RecentLoginItem[]
}

export interface RecentLoginItem {
  logTime: string
  ipAddress?: string
  deviceType: string
  riskLevel: number
}

// API 方法
export const authApi = {
  // 登录
  login: (
    loginData: Omit<LoginData, 'device_id' | 'device_name'>
  ): Promise<ApiResponse<TokenResponse>> => {
    const requestData: LoginData = {
      ...loginData,
      device_id: generateDeviceId(),
      device_name: getDeviceName(),
      remember_me: false,
    }
    return api.post('/api/auth/login', requestData)
  },

  // 注册
  register: (data: RegisterData): Promise<ApiResponse<UserInfo>> => {
    return api.post('/api/auth/register', data)
  },

  // 获取用户信息
  getUser: (username: string): Promise<ApiResponse<UserInfo>> => {
    return api.get(`/api/auth/user/${username}`)
  },

  // 验证学生身份
  validateStudent: (
    studentId: string,
    name: string
  ): Promise<ApiResponse<{ isValid: boolean; studentId: string }>> => {
    return api.post('/api/auth/validate-student', {
      student_id: studentId,
      name,
    })
  },

  // 登出
  logout: (refreshToken: string): Promise<ApiResponse> => {
    return api.post('/api/auth/logout', {
      refresh_token: refreshToken,
    })
  },

  sendVerificationCode: (userId: number, email: string): Promise<ApiResponse> => {
    return api.post('/api/auth/send-verification-code', {
      UserId: userId,
      Email: email,
    })
  },

  // 验证邮箱验证码
  verifyCode: (userId: number, code: string): Promise<ApiResponse> => {
    return api.post('/api/auth/verify-code', {
      UserId: userId,
      Code: code,
    })
  },

  // 验证邮箱链接（GET请求）
  verifyEmail: (token: string): Promise<ApiResponse> => {
    return api.get(`/api/auth/verify-email?token=${encodeURIComponent(token)}`)
  },

  // 退出所有设备
  logoutAll: (): Promise<ApiResponse<{ revokedTokens: number }>> => {
    return api.post('/api/auth/logout-all')
  },

  // 获取当前用户详细信息
  getUserProfile: (): Promise<ApiResponse<UserProfileResponse>> => {
    return api.get('/api/auth/profile')
  },

  // 更新用户信息
  updateUserProfile: (data: UpdateUserProfileData): Promise<ApiResponse> => {
    return api.put('/api/auth/profile', data)
  },

  // 获取信用历史
  getCreditHistory: (
    page: number = 1,
    pageSize: number = 20
  ): Promise<ApiResponse<CreditHistoryResponse>> => {
    return api.get(`/api/auth/credit-history?page=${page}&pageSize=${pageSize}`)
  },

  // 修改密码
  changePassword: (data: ChangePasswordData): Promise<ApiResponse> => {
    return api.post('/api/auth/change-password', data)
  },

  // 获取用户统计信息
  getUserStatistics: (): Promise<ApiResponse<UserStatisticsResponse>> => {
    return api.get('/api/auth/statistics')
  },

  // 获取用户安全信息
  getSecurityInfo: (): Promise<ApiResponse<SecurityInfoResponse>> => {
    return api.get('/api/auth/security')
  },
}

export interface OrderItemResponse {
  id: number
  productId: number
  productName: string
  productImage: string
  specification: string
  price: number
  quantity: number
  totalAmount: number
}

export interface OrderAddressResponse {
  recipient: string
  phone: string
  location: string
}

export interface OrderDetailResponse {
  id: number
  orderNumber: string
  orderDate: string
  status: OrderStatus
  userId: number
  sellerId: number
  totalAmount: number
  discountAmount: number
  shippingFee: number
  finalPayment: number
  items: OrderItemResponse[]
  address: OrderAddressResponse
  trackingInfo?: string
  review?: string
  reviewDate?: string
  cancelledReason?: string
  cancelledDate?: string
}

export interface OrderListResponse {
  id: number
  orderNumber: string
  orderDate: string
  status: OrderStatus
  productName: string
  productImage: string
  totalAmount: number
  quantity: number
  userRole: 'buyer' | 'seller'
}

export interface OrderStatisticsResponse {
  totalOrders: number
  pendingCount: number
  processingCount: number
  shippedCount: number
  deliveredCount: number
  completedCount: number
  cancelledCount: number
  totalAmount: number
}

export interface CreateOrderRequest {
  productId: number
  productName: string
  productImage: string
  specification: string
  price: number
  quantity: number
  address: OrderAddressResponse
  totalAmount: number
  discountAmount: number
  shippingFee: number
  finalPayment: number
}

export interface UpdateOrderStatusRequest {
  status: OrderStatus
  trackingInfo?: string
  reason?: string
}

export interface ShipOrderRequest {
  trackingInfo?: string
}

export interface CancelOrderRequest {
  reason?: string
}

export interface PaymentResult {
  success: boolean
  message: string
  amount?: number
}

export interface UserOrdersResponse {
  orders: OrderListResponse[]
  totalCount: number
  pageIndex: number
  pageSize: number
  totalPages: number
}

// 用户简要信息接口
export interface UserBriefInfo {
  userId: number
  username: string
  nickname?: string
  avatarUrl?: string
  creditScore?: number
}

// 商品简要信息接口
export interface ProductBriefInfo {
  productId: number
  title: string
  price: number
  mainImageUrl?: string
  status: string
}

// 订单状态枚举
export enum OrderStatus {
  PENDING = 'pending',
  PROCESSING = 'processing',
  SHIPPED = 'shipped',
  DELIVERED = 'delivered',
  COMPLETED = 'completed',
  CANCELLED = 'cancelled',
}

// 后端订单详情响应接口（匹配后端DTO）
export interface BackendOrderDetailResponse {
  orderId: number
  buyerId: number
  buyer?: UserBriefInfo
  sellerId: number
  seller?: UserBriefInfo
  productId: number
  product?: ProductBriefInfo
  totalAmount?: number
  finalPrice?: number
  status: string
  createTime: string
  expireTime?: string
  isExpired?: boolean
  remainingMinutes?: number
}

// 后端订单列表响应接口（匹配后端DTO）
export interface BackendOrderListResponse {
  orderId: number
  product?: ProductBriefInfo
  otherUser?: UserBriefInfo
  totalAmount?: number
  finalPrice?: number
  status: string
  createTime: string
  userRole: 'buyer' | 'seller' // 后端返回的用户角色
  isExpired?: boolean
}

// 后端用户订单响应接口
export interface BackendUserOrdersResponse {
  orders: BackendOrderListResponse[]
  totalCount: number
  pageIndex: number
  pageSize: number
  totalPages: number
}

// 适配器函数：后端订单详情 -> 前端订单详情
export const adaptOrderDetail = (backendData: BackendOrderDetailResponse): OrderDetailResponse => {
  return {
    id: backendData.orderId,
    orderNumber: `ORD-${backendData.orderId.toString().padStart(6, '0')}`,
    orderDate: backendData.createTime,
    status: backendToFrontendStatus(backendData.status) as OrderStatus,
    userId: backendData.buyerId,
    sellerId: backendData.sellerId,
    totalAmount: backendData.totalAmount || 0,
    discountAmount: 0, // 后端暂无此字段
    shippingFee: 0, // 后端暂无此字段
    finalPayment: backendData.finalPrice || backendData.totalAmount || 0,
    items: backendData.product
      ? [
          {
            id: backendData.productId,
            productId: backendData.productId,
            productName: backendData.product.title || '',
            productImage: backendData.product.mainImageUrl || '',
            specification: '',
            price: backendData.finalPrice || backendData.totalAmount || 0,
            quantity: 1,
            totalAmount: backendData.finalPrice || backendData.totalAmount || 0,
          },
        ]
      : [],
    address: {
      recipient: '',
      phone: '',
      location: '',
    },
    trackingInfo: '',
    review: '',
    reviewDate: '',
    cancelledReason: '',
    cancelledDate: '',
  }
}

// 适配器函数：后端订单列表 -> 前端订单列表
export const adaptOrderList = (backendData: BackendOrderListResponse[]): OrderListResponse[] => {
  return backendData.map(order => ({
    id: order.orderId,
    orderNumber: `ORD-${order.orderId.toString().padStart(6, '0')}`,
    orderDate: order.createTime,
    status: backendToFrontendStatus(order.status) as OrderStatus,
    productName: order.product?.title || '未知商品',
    productImage: order.product?.mainImageUrl || '/images/default-product.png',
    totalAmount: order.totalAmount || 0,
    quantity: 1, // 后端暂无此字段
    userRole: order.userRole, // 保留后端返回的用户角色
  }))
}

// 适配器函数：前端状态更新请求 -> 后端状态更新请求
export const adaptUpdateStatusRequest = (
  frontendData: UpdateOrderStatusRequest
): { status: string; remarks?: string } => {
  return {
    status: frontendToBackendStatus(frontendData.status),
    remarks: frontendData.reason,
  }
}

// 订单相关接口
export const orderApi = {
  // 创建订单
  createOrder: async (data: CreateOrderRequest): Promise<ApiResponse<OrderDetailResponse>> => {
    const response = await api.post('/api/order', data)
    const responseData = response.data

    // 检查是否是 ApiResponse 格式
    if (responseData.success !== undefined) {
      // 标准 ApiResponse 格式
      if (responseData.success && responseData.data) {
        const adaptedData = adaptOrderDetail(responseData.data)
        return {
          ...responseData,
          data: adaptedData,
        }
      }
      return responseData as ApiResponse<OrderDetailResponse>
    } else if (responseData.orderId) {
      // 直接返回的订单对象
      const adaptedData = adaptOrderDetail(responseData)
      return {
        success: true,
        message: '订单创建成功',
        data: adaptedData,
        error_code: undefined,
        timestamp: new Date().toISOString(),
      }
    }

    return {
      success: false,
      message: '响应格式错误',
      data: undefined,
      error_code: 'INVALID_RESPONSE',
      timestamp: new Date().toISOString(),
    }
  },

  // 获取订单详情
  getOrderDetail: async (orderId: number): Promise<ApiResponse<OrderDetailResponse>> => {
    const response = await api.get<ApiResponse<BackendOrderDetailResponse>>(`/api/order/${orderId}`)
    if (response.data.success && response.data.data) {
      return {
        ...response.data,
        data: adaptOrderDetail(response.data.data),
      }
    }
    return response.data as unknown as ApiResponse<OrderDetailResponse>
  },

  // 获取用户订单列表
  getUserOrders: async (filters?: {
    role?: 'buyer' | 'seller'
    status?: string
    pageIndex?: number
    pageSize?: number
  }): Promise<ApiResponse<UserOrdersResponse>> => {
    const params = new URLSearchParams()
    if (filters?.role) params.append('role', filters.role)
    if (filters?.status) params.append('status', frontendToBackendStatus(filters.status))
    if (filters?.pageIndex) params.append('pageIndex', filters.pageIndex.toString())
    if (filters?.pageSize) params.append('pageSize', filters.pageSize.toString())

    const response = await api.get(`/api/order?${params.toString()}`)

    // 检查是否是 ApiResponse 格式
    if (response.data.success !== undefined) {
      // 标准 ApiResponse 格式
      if (response.data.success && response.data.data) {
        const adaptedOrders = adaptOrderList(response.data.data.orders)
        return {
          ...response.data,
          data: {
            ...response.data.data,
            orders: adaptedOrders,
          },
        }
      }
      return response.data as ApiResponse<UserOrdersResponse>
    } else if (response.data.orders) {
      // 直接返回的订单列表格式
      const adaptedOrders = adaptOrderList(response.data.orders)
      return {
        success: true,
        message: '获取订单成功',
        data: {
          orders: adaptedOrders,
          totalCount: response.data.totalCount || 0,
          pageIndex: response.data.pageIndex || 1,
          pageSize: response.data.pageSize || 10,
          totalPages: response.data.totalPages || 1,
        },
        error_code: undefined,
        timestamp: new Date().toISOString(),
      }
    }

    return {
      success: false,
      message: '获取订单失败',
      data: undefined,
      error_code: 'NO_DATA',
      timestamp: new Date().toISOString(),
    }
  },

  // 获取商品订单列表
  getProductOrders: async (productId: number): Promise<ApiResponse<OrderListResponse[]>> => {
    const response = await api.get<ApiResponse<BackendOrderListResponse[]>>(
      `/api/order/product/${productId}`
    )
    if (response.data.success && response.data.data) {
      return {
        ...response.data,
        data: adaptOrderList(response.data.data),
      }
    }
    return response.data as unknown as ApiResponse<OrderListResponse[]>
  },

  // 获取用户订单统计
  getUserOrderStatistics: (): Promise<ApiResponse<OrderStatisticsResponse>> => {
    return api.get('/api/order/statistics')
  },

  // 更新订单状态
  updateOrderStatus: (
    orderId: number,
    data: UpdateOrderStatusRequest
  ): Promise<ApiResponse<void>> => {
    const adaptedData = adaptUpdateStatusRequest(data)
    return api.put(`/api/order/${orderId}/status`, adaptedData)
  },

  // 确认付款
  confirmPayment: (orderId: number): Promise<ApiResponse<void>> => {
    return api.post(`/api/order/${orderId}/confirm-payment`)
  },

  // 发货
  shipOrder: (orderId: number, data?: ShipOrderRequest): Promise<ApiResponse<void>> => {
    return api.post(`/api/order/${orderId}/ship`, data)
  },

  // 确认收货
  confirmDelivery: (orderId: number): Promise<ApiResponse<void>> => {
    return api.post(`/api/order/${orderId}/confirm-delivery`)
  },

  // 完成订单
  completeOrder: (orderId: number, review?: string): Promise<ApiResponse<void>> => {
    const params = review ? new URLSearchParams({ review }) : undefined
    return api.post(`/api/order/${orderId}/complete${params ? `?${params.toString()}` : ''}`)
  },

  // 支付订单
  payOrder: (orderId: number): Promise<ApiResponse<PaymentResult>> => {
    return api.post(`/api/order/${orderId}/pay`)
  },

  // 取消订单
  cancelOrder: (orderId: number, data?: CancelOrderRequest): Promise<ApiResponse<void>> => {
    return api.post(`/api/order/${orderId}/cancel`, data)
  },
}

// 商品相关接口定义
export interface ProductImage {
  id: number
  url: string
  isPrimary: boolean
  order: number
}

export interface ProductSpecification {
  id: number
  name: string
  value: string
}

export interface ProductDetail {
  id: number
  title: string
  description: string
  price: number
  originalPrice?: number
  categoryId: number
  categoryName: string
  sellerId: number
  sellerName: string
  sellerAvatar?: string
  status: string
  condition: string
  stock: number
  viewCount: number
  likeCount: number
  createdAt: string
  updatedAt: string
  images: ProductImage[]
  specifications: ProductSpecification[]
  tags: string[]
  isLiked: boolean
  isCollected: boolean
  autoRemoveAt?: string
  location?: string
  contactMethod: string
}

export interface ProductListItem {
  product_id: number
  title: string
  base_price: number
  publish_time: string
  view_count: number
  status: string
  main_image_url?: string
  thumbnail_url?: string
  user: {
    user_id: number
    username: string
  }
  category: {
    category_id: number
    name: string
    parent_id?: number
  }
  days_until_auto_remove?: number
  is_popular: boolean
  tags: string[]

  // 兼容现有代码，添加别名属性
  id?: number
  price?: number
  primaryImage?: string
  sellerId?: number
  sellerName?: string
  categoryId?: number
  categoryName?: string
  createdAt?: string
}

export interface CategoryItem {
  category_id: number
  name: string
  parent_id?: number
  level: number
  full_path: string
  product_count: number
  active_product_count: number
  children?: CategoryItem[]
}

export interface CategoryBreadcrumb {
  id: number
  name: string
  level: number
}

export interface CreateProductRequest {
  title: string
  description?: string
  base_price: number
  category_id: number
  image_urls?: string[]
  auto_remove_time?: string
}

export interface UpdateProductRequest {
  title?: string
  description?: string
  price?: number
  originalPrice?: number
  categoryId?: number
  condition?: string
  stock?: number
  tags?: string[]
  specifications?: Omit<ProductSpecification, 'id'>[]
  location?: string
  contactMethod?: string
}

export interface ProductQueryParams {
  pageIndex?: number
  pageSize?: number
  categoryId?: number
  condition?: string
  minPrice?: number
  maxPrice?: number
  sortBy?: string
  sortOrder?: 'asc' | 'desc'
  status?: string
  tags?: string[]
}

export interface ProductSearchParams {
  keyword: string
  pageIndex?: number
  pageSize?: number
  categoryId?: number
}

export interface ProductListResponse {
  products: ProductListItem[]
  totalCount: number
  pageIndex: number
  pageSize: number
  totalPages: number
}

export interface CategoryTreeResponse {
  root_categories: CategoryItem[]
  total_count: number
  last_update_time: string
}

export interface CategoryBreadcrumbResponse {
  items: CategoryBreadcrumb[]
}

export interface ProductsToAutoRemoveResponse {
  products: ProductListItem[]
  totalCount: number
}

// 商品相关接口
export const productApi = {
  // 获取商品详情
  getProductDetail: (productId: number): Promise<ApiResponse<ProductDetail>> => {
    return api.get(`/api/product/${productId}`)
  },

  // 发布商品
  createProduct: (data: CreateProductRequest): Promise<ApiResponse<ProductDetail>> => {
    return api.post('/api/product', data)
  },

  // 更新商品
  updateProduct: (
    productId: number,
    data: UpdateProductRequest
  ): Promise<ApiResponse<ProductDetail>> => {
    return api.put(`/api/product/${productId}`, data)
  },

  // 删除商品
  deleteProduct: (productId: number): Promise<ApiResponse> => {
    return api.delete(`/api/product/${productId}`)
  },

  // 更新商品状态
  updateProductStatus: (productId: number, status: string): Promise<ApiResponse> => {
    return api.patch(`/api/product/${productId}/status`, { status })
  },

  // 查询商品列表
  getProducts: (queryParams: ProductQueryParams): Promise<ApiResponse<ProductListResponse>> => {
    const params = new URLSearchParams()

    if (queryParams.pageIndex !== undefined)
      params.append('pageIndex', queryParams.pageIndex.toString())
    if (queryParams.pageSize !== undefined)
      params.append('pageSize', queryParams.pageSize.toString())
    if (queryParams.categoryId !== undefined)
      params.append('categoryId', queryParams.categoryId.toString())
    if (queryParams.condition) params.append('condition', queryParams.condition)
    if (queryParams.minPrice !== undefined)
      params.append('minPrice', queryParams.minPrice.toString())
    if (queryParams.maxPrice !== undefined)
      params.append('maxPrice', queryParams.maxPrice.toString())
    if (queryParams.sortBy) params.append('sortBy', queryParams.sortBy)
    if (queryParams.sortOrder) params.append('sortOrder', queryParams.sortOrder)
    if (queryParams.status) params.append('status', queryParams.status)
    if (queryParams.tags && queryParams.tags.length > 0) {
      queryParams.tags.forEach(tag => params.append('tags', tag))
    }

    return api.get(`/api/product?${params.toString()}`)
  },

  // 搜索商品
  searchProducts: (
    searchParams: ProductSearchParams
  ): Promise<ApiResponse<ProductListResponse>> => {
    const params = new URLSearchParams()
    params.append('keyword', searchParams.keyword)

    if (searchParams.pageIndex !== undefined)
      params.append('pageIndex', searchParams.pageIndex.toString())
    if (searchParams.pageSize !== undefined)
      params.append('pageSize', searchParams.pageSize.toString())
    if (searchParams.categoryId !== undefined)
      params.append('categoryId', searchParams.categoryId.toString())

    return api.get(`/api/product/search?${params.toString()}`)
  },

  // 获取热门商品
  getPopularProducts: (
    count: number = 10,
    categoryId?: number
  ): Promise<ApiResponse<ProductListItem[]>> => {
    const params = new URLSearchParams()
    params.append('count', count.toString())

    if (categoryId !== undefined) params.append('categoryId', categoryId.toString())

    return api.get(`/api/product/popular?${params.toString()}`)
  },

  // 获取用户发布的商品
  getUserProducts: (
    userId?: number,
    pageIndex: number = 0,
    pageSize: number = 20,
    status?: string
  ): Promise<ApiResponse<ProductListResponse>> => {
    const params = new URLSearchParams()
    params.append('pageIndex', pageIndex.toString())
    params.append('pageSize', pageSize.toString())

    if (status) params.append('status', status)

    const endpoint =
      userId !== undefined
        ? `/api/product/user/${userId}?${params.toString()}`
        : `/api/product/user?${params.toString()}`

    return api.get(endpoint)
  },

  // 获取分类树
  getCategoryTree: (
    includeProductCount: boolean = true
  ): Promise<ApiResponse<CategoryTreeResponse[]>> => {
    return api.get(`/api/product/categories/tree?includeProductCount=${includeProductCount}`)
  },

  // 获取子分类
  getSubCategories: (parentId?: number): Promise<ApiResponse<CategoryItem[]>> => {
    const endpoint =
      parentId !== undefined
        ? `/api/product/categories?parentId=${parentId}`
        : '/api/product/categories'

    return api.get(endpoint)
  },

  // 获取分类面包屑
  getCategoryBreadcrumb: (categoryId: number): Promise<ApiResponse<CategoryBreadcrumbResponse>> => {
    return api.get(`/api/product/categories/${categoryId}/breadcrumb`)
  },

  // 获取分类下的商品
  getProductsByCategory: (
    categoryId: number,
    pageIndex: number = 0,
    pageSize: number = 20,
    includeSubCategories: boolean = true
  ): Promise<ApiResponse<ProductListResponse>> => {
    const params = new URLSearchParams()
    params.append('pageIndex', pageIndex.toString())
    params.append('pageSize', pageSize.toString())
    params.append('includeSubCategories', includeSubCategories.toString())

    return api.get(`/api/product/categories/${categoryId}/products?${params.toString()}`)
  },

  // 获取即将自动下架的商品
  getProductsToAutoRemove: (
    days: number = 7
  ): Promise<ApiResponse<ProductsToAutoRemoveResponse>> => {
    return api.get(`/api/product/auto-remove?days=${days}`)
  },

  // 延期商品下架时间
  extendProductAutoRemoveTime: (productId: number, extendDays: number): Promise<ApiResponse> => {
    return api.patch(`/api/product/${productId}/extend`, { extendDays })
  },
}

// 文件上传相关接口
export const fileApi = {
  // 上传商品图片
  uploadProductImage: (
    file: File
  ): Promise<
    ApiResponse<{ fileName: string; fileUrl: string; thumbnailUrl?: string; fileSize: number }>
  > => {
    const formData = new FormData()
    formData.append('file', file)
    return api.post('/api/File/upload/product-image', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  // 批量上传文件
  batchUploadFiles: (
    files: File[],
    fileType: string = 'ProductImage'
  ): Promise<ApiResponse<Array<{ fileName: string; fileUrl: string; fileSize: number }>>> => {
    const formData = new FormData()
    files.forEach(file => formData.append('files', file))
    formData.append('fileType', fileType)
    return api.post('/api/File/upload/batch', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  // 删除文件
  deleteFile: (fileUrl: string): Promise<ApiResponse> => {
    return api.post('/api/File/delete', { fileUrl })
  },
}

// 分类相关接口
export const categoryApi = {
  // 获取商品分类树
  getCategoryTree: (): Promise<ApiResponse<CategoryTreeResponse>> => {
    return api.get('/api/Product/categories/tree')
  },

  // 获取分类面包屑导航
  getCategoryBreadcrumb: (categoryId: number): Promise<ApiResponse<CategoryBreadcrumb[]>> => {
    return api.get(`/api/Product/categories/${categoryId}/breadcrumb`)
  },
}

export interface VirtualAccountBalance {
  userId: number
  balance: number
  lastUpdateTime: string
}

export interface VirtualAccountDetails {
  accountId: number
  userId: number
  balance: number
  createTime: string
}

export interface BalanceCheckResult {
  userId: number
  requestAmount: number
  currentBalance: number
  hasSufficientBalance: boolean
}

// 虚拟账户相关接口
export const virtualAccountApi = {
  // 获取余额
  getBalance: (): Promise<ApiResponse<VirtualAccountBalance>> => {
    return api.get('/api/virtualaccounts/balance')
  },

  // 获取账户详情
  getAccountDetails: (): Promise<ApiResponse<VirtualAccountDetails>> => {
    return api.get('/api/virtualaccounts/details')
  },

  // 检查余额是否充足
  checkBalance: (amount: number): Promise<ApiResponse<BalanceCheckResult>> => {
    return api.get(`/api/virtualaccounts/check-balance?amount=${amount}`)
  },
}

// 充值相关类型定义
export enum RechargeMethod {
  Simulation = 1,
}

export interface CreateRechargeRequest {
  amount: number
  method: RechargeMethod
  remarks?: string
}

export interface RechargeResponse {
  rechargeId: number
  amount: number
  method: RechargeMethod
  status: string
  createTime: string
  expireTime?: string
}

export interface RechargeRecord {
  rechargeId: number
  amount: number
  method: RechargeMethod
  status: string
  createTime: string
  completeTime?: string
  remarks?: string
}

export interface UserRechargeRecordsResponse {
  records: RechargeRecord[]
  totalCount: number
  pageIndex: number
  pageSize: number
  totalPages: number
}

// 充值相关接口
export const rechargeApi = {
  // 创建充值订单
  createRecharge: (data: CreateRechargeRequest): Promise<ApiResponse<RechargeResponse>> => {
    return api.post('/api/recharge', data)
  },

  // 完成模拟充值
  completeSimulationRecharge: (
    rechargeId: number
  ): Promise<ApiResponse<{ success: boolean; message: string }>> => {
    return api.post(`/api/recharge/${rechargeId}/simulate-complete`)
  },

  // 获取用户充值记录
  getUserRechargeRecords: (
    pageIndex = 1,
    pageSize = 10
  ): Promise<ApiResponse<UserRechargeRecordsResponse>> => {
    return api.get(`/api/recharge/records?pageIndex=${pageIndex}&pageSize=${pageSize}`)
  },
}

export interface DashboardStatsDto {
  monthlyTransactions: MonthlyTransactionDto[]
  popularProducts: PopularProductDto[]
  userActivities: UserActivityDto[]
}

export interface MonthlyTransactionDto {
  month: string
  orderCount: number
  totalAmount: number
}

export interface PopularProductDto {
  productId: number
  productTitle: string
  orderCount: number
}

export interface UserActivityDto {
  date: string
  activeUserCount: number
  newUserCount: number
}

export interface DashboardSummaryDto {
  totalUsers: number
  activeUsers: number
  totalOrders: number
  monthlySales: number
}

// Dashboard API方法
export const dashboardApi = {
  // 获取统计数据
  getStatistics: (
    year: number,
    activityDays: number = 30
  ): Promise<ApiResponse<DashboardStatsDto>> => {
    return api.get(`/api/dashboard/statistics?year=${year}&activityDays=${activityDays}`)
  },

  // 获取汇总数据
  getSummary: (): Promise<ApiResponse<DashboardSummaryDto>> => {
    return api.get('/api/dashboard/summary')
  },

  // 导出Excel
  exportExcel: (year: number): Promise<Blob> => {
    return api.get(`/api/dashboard/export/excel?year=${year}`, {
      responseType: 'blob',
    })
  },

  // 导出PDF
  exportPdf: (year: number): Promise<Blob> => {
    return api.get(`/api/dashboard/export/pdf?year=${year}`, {
      responseType: 'blob',
    })
  },
}

export default api
