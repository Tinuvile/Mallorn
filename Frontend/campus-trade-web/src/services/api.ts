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
  NEGOTIATING = 'negotiating',
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

  // 上传举报证据文件
  uploadReportEvidence: (
    file: File
  ): Promise<
    ApiResponse<{ fileName: string; fileUrl: string; thumbnailUrl?: string; fileSize: number }>
  > => {
    const formData = new FormData()
    formData.append('file', file)
    return api.post('/api/File/upload/report-evidence', formData, {
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

  // 批量上传举报证据文件
  batchUploadReportEvidence: (
    files: File[]
  ): Promise<ApiResponse<Array<{ fileName: string; fileUrl: string; fileSize: number }>>> => {
    const formData = new FormData()
    files.forEach(file => formData.append('files', file))
    formData.append('fileType', 'ReportEvidence')
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

// 评价相关接口定义
export interface CreateReviewRequest {
  orderId: number
  rating: number
  descAccuracy: number
  serviceAttitude: number
  isAnonymous: boolean
  content?: string
}

export interface ReviewResponse {
  reviewId: number
  orderId: number
  rating: number
  descAccuracy: number
  serviceAttitude: number
  isAnonymous: boolean
  content?: string
  createTime: string
  sellerResponse?: string
  sellerResponseTime?: string
}

export interface ReviewListResponse {
  reviews: ReviewResponse[]
  totalCount: number
  pageIndex: number
  pageSize: number
  totalPages: number
}

// 举报相关接口定义
export interface CreateReportRequest {
  orderId: number
  type: string
  description?: string
  evidenceFiles?: EvidenceFile[]
}

export interface EvidenceFile {
  fileType: string
  fileUrl: string
}

export interface ReportResponse {
  reportId: number
  orderId: number
  reporterId: number
  type: string
  description?: string
  status: string
  priority?: number
  createTime: string
  updateTime?: string
}

export interface ReportListResponse {
  reports: ReportResponse[]
  totalCount: number
  pageIndex: number
  pageSize: number
  totalPages: number
}

// 争议评价相关接口
export interface CreateDisputeRequest {
  orderId: number
  reason: string
  description: string
  evidenceFiles?: EvidenceFile[]
}

export interface DisputeResponse {
  disputeId: number
  orderId: number
  reason: string
  description: string
  status: string
  createTime: string
  resolveTime?: string
}

// 卖家回应评价相关接口
export interface CreateReviewResponseRequest {
  reviewId: number
  responseContent: string
}

// 评价相关API
export const reviewApi = {
  // 创建评价
  createReview: (data: CreateReviewRequest): Promise<ApiResponse<ReviewResponse>> => {
    return api.post('/api/reviews', data)
  },

  // 获取订单评价
  getOrderReview: (orderId: number): Promise<ApiResponse<ReviewResponse>> => {
    return api.get(`/api/reviews/order/${orderId}`)
  },

  // 获取商品评价列表
  getProductReviews: (
    productId: number,
    pageIndex = 1,
    pageSize = 10
  ): Promise<ApiResponse<ReviewListResponse>> => {
    return api.get(`/api/reviews/item/${productId}?pageIndex=${pageIndex}&pageSize=${pageSize}`)
  },

  // 卖家回应评价
  createReviewResponse: (data: CreateReviewResponseRequest): Promise<ApiResponse<void>> => {
    return api.post('/api/reviews/response', data)
  },
}

// 举报相关API
export const reportApi = {
  // 创建举报
  createReport: (data: CreateReportRequest): Promise<ApiResponse<ReportResponse>> => {
    // 转换属性名以匹配后端DTO
    const payload = {
      order_id: data.orderId,
      type: data.type,
      description: data.description,
      evidence_files: data.evidenceFiles?.map(file => ({
        file_type: file.fileType,
        file_url: file.fileUrl,
      })),
    }
    console.log('发送到后端的举报数据:', JSON.stringify(payload, null, 2))
    return api.post('/api/report', payload)
  },

  // 获取用户举报列表
  getUserReports: (pageIndex = 1, pageSize = 10): Promise<ApiResponse<ReportListResponse>> => {
    return api.get(`/api/report/user?pageIndex=${pageIndex}&pageSize=${pageSize}`)
  },

  // 创建争议评价
  createDispute: (data: CreateDisputeRequest): Promise<ApiResponse<DisputeResponse>> => {
    return api.post('/api/report/dispute', data)
  },

  // 获取举报详情
  getReportDetail: (reportId: number): Promise<ApiResponse<ReportDetail>> => {
    return api.get(`/api/report/${reportId}`)
  },

  // 获取举报类型列表
  getReportTypes: (): Promise<
    ApiResponse<
      Array<{
        value: string
        label: string
        priority: number
      }>
    >
  > => {
    return api.get('/api/report/types')
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

// 通知相关接口数据格式
export interface CreateNotificationRequest {
  recipientId: number
  templateId: number
  parameters: Record<string, any>
  orderId?: number
}

export interface NotificationResponse {
  success: boolean
  message: string
  notificationId?: number
}

export interface MessageItem {
  id: number
  type: 'notification' | 'bargain' | 'swap' | 'reply'
  sender: string
  content: string
  time: string
  read: boolean
  // 议价消息特有属性
  productName?: string
  productImage?: string
  myOffer?: number
  newOffer?: number
  bargainStatus?: 'pending' | 'accepted' | 'rejected'
  rejectReason?: string
  // 换物消息特有属性
  swapProductName?: string
  swapProductImage?: string
  swapProductPrice?: number
  swapProductLink?: string
  swapStatus?: 'pending' | 'accepted' | 'rejected'
}

export interface UserMessagesResponse {
  success: boolean
  data: MessageItem[]
  message?: string
}

// 通知API方法
export const notificationApi = {
  // 创建通知
  createNotification: (
    data: CreateNotificationRequest
  ): Promise<ApiResponse<NotificationResponse>> => {
    return api.post('/api/notification/create', data)
  },

  // 获取通知队列状态
  getQueueStats: (): Promise<
    ApiResponse<{
      pending: number
      success: number
      failed: number
      total: number
    }>
  > => {
    return api.get('/api/notification/queue-stats')
  },

  // 获取用户通知历史
  getUserNotifications: (
    userId: number,
    pageSize: number = 10,
    pageIndex: number = 0
  ): Promise<ApiResponse<any[]>> => {
    return api.get(
      `/api/notification/user/${userId}/history?pageSize=${pageSize}&pageIndex=${pageIndex}`
    )
  },

  // 获取用户消息列表（新增）
  getUserMessages: (
    userId: number,
    category?: 'system' | 'bargain' | 'reply' | 'swap',
    pageSize: number = 10,
    pageIndex: number = 0
  ): Promise<UserMessagesResponse> => {
    const params = new URLSearchParams()
    params.append('pageSize', pageSize.toString())
    params.append('pageIndex', pageIndex.toString())
    if (category) params.append('category', category)

    return api.get(`/api/notification/user/${userId}/messages?${params.toString()}`)
  },

  // 标记消息为已读（新增）
  markMessageAsRead: (userId: number, messageId: number): Promise<ApiResponse<void>> => {
    return api.post(`/api/notification/user/${userId}/messages/${messageId}/read`)
  },

  // 获取议价对话历史
  getBargainConversation: (orderId: number): Promise<ApiResponse<any[]>> => {
    return api.get(`/api/notification/bargain-conversation/${orderId}`)
  },

  // 获取用户未读消息数量
  getUnreadMessageCount: (
    userId: number,
    category?: 'system' | 'bargain' | 'reply' | 'swap' | 'all'
  ): Promise<ApiResponse<{ unreadCount: number; category: string }>> => {
    const params = new URLSearchParams()
    if (category) params.append('category', category)

    return api.get(`/api/notification/user/${userId}/unread-count?${params.toString()}`)
  },
}

// 议价相关接口数据格式
export interface BargainRequestDto {
  productId: number
  offeredPrice: number
  message?: string
}

export interface BargainResponseDto {
  negotiationId: number
  status: '接受' | '拒绝' | '反报价'
  proposedPrice?: number
  rejectReason?: string
}

export interface BargainNegotiation {
  negotiationId: number
  productId: number
  productName: string
  productImage: string
  requesterId: number
  requesterName: string
  sellerId: number
  sellerName: string
  originalPrice: number
  offeredPrice: number
  currentPrice?: number
  status: string
  createdAt: string
  updatedAt: string
}

// 议价API方法
export const bargainApi = {
  // 创建议价请求
  createBargainRequest: (
    data: BargainRequestDto
  ): Promise<ApiResponse<{ negotiationId: number }>> => {
    return api.post('/api/bargain/request', data)
  },

  // 处理议价回应
  handleBargainResponse: (data: BargainResponseDto): Promise<ApiResponse<void>> => {
    return api.post('/api/bargain/response', data)
  },

  // 获取我的议价记录
  getMyNegotiations: (
    pageIndex: number = 1,
    pageSize: number = 10
  ): Promise<
    ApiResponse<{
      negotiations: BargainNegotiation[]
      totalCount: number
      pageIndex: number
      pageSize: number
      totalPages: number
    }>
  > => {
    return api.get(`/api/bargain/my-negotiations?pageIndex=${pageIndex}&pageSize=${pageSize}`)
  },

  // 获取议价详情
  getNegotiationDetails: (negotiationId: number): Promise<ApiResponse<BargainNegotiation>> => {
    return api.get(`/api/bargain/${negotiationId}`)
  },
}

// 换物相关接口数据格式
export interface ExchangeRequestDto {
  offerProductId: number
  requestProductId: number
  terms?: string
}

export interface ExchangeResponseDto {
  exchangeRequestId: number
  status: string
  responseMessage?: string
}

export interface ExchangeRequestItem {
  exchangeRequestId: number
  offerProductId: number
  offerProductName: string
  offerProductImage: string
  requestProductId: number
  requestProductName: string
  requestProductImage: string
  requesterId: number
  requesterName: string
  ownerId: number
  ownerName: string
  terms?: string
  status: string
  createdAt: string
}

// 换物API方法
export const exchangeApi = {
  // 创建换物请求
  createExchangeRequest: (
    data: ExchangeRequestDto
  ): Promise<ApiResponse<{ exchangeRequestId: number }>> => {
    return api.post('/api/exchange/request', data)
  },

  // 处理换物回应
  handleExchangeResponse: (data: ExchangeResponseDto): Promise<ApiResponse<void>> => {
    return api.post('/api/exchange/response', data)
  },

  // 获取我的换物请求记录
  getMyExchangeRequests: (
    pageIndex: number = 1,
    pageSize: number = 10
  ): Promise<
    ApiResponse<{
      exchangeRequests: ExchangeRequestItem[]
      totalCount: number
      pageIndex: number
      pageSize: number
      totalPages: number
    }>
  > => {
    return api.get(`/api/exchange/my-requests?pageIndex=${pageIndex}&pageSize=${pageSize}`)
  },

  // 获取换物请求详情
  getExchangeRequestDetails: (
    exchangeRequestId: number
  ): Promise<ApiResponse<ExchangeRequestItem>> => {
    return api.get(`/api/exchange/${exchangeRequestId}`)
  },
}

// 管理员相关接口数据格式
export interface AdminInfo {
  adminId: number
  userId: number
  username: string
  email: string
  role: string
  roleDisplayName: string
  assignedCategory?: number
  assignedCategoryName?: string
  createdAt: string
  isActive: boolean
}

export interface AdminProduct {
  product_id: number
  title: string
  description?: string
  base_price: number
  status: string
  publish_time: string
  view_count: number
  main_image_url?: string
  thumbnail_url?: string
  user: {
    user_id: number
    username?: string
    name?: string
    avatar_url?: string
    credit_score?: number
    is_online?: boolean
  }
  category: {
    category_id: number
    name: string
    parent_id?: number
    full_path?: string
  }
  days_until_auto_remove?: number
  is_popular?: boolean
  tags?: string[]
}

export interface AdminProductQuery {
  pageIndex?: number
  pageSize?: number
  status?: string
  categoryId?: number
  searchKeyword?: string
  userId?: number
  startDate?: string
  endDate?: string
}

export interface AdminProductsResponse {
  products: AdminProduct[]
  totalCount: number
  pageIndex: number
  pageSize: number
}

export interface AdminUpdateProductRequest {
  title?: string
  description?: string
  basePrice?: number
  status?: string
  categoryId?: number
  adminNote?: string
}

export interface BatchProductOperationRequest {
  productIds: number[]
  operationType: string
  reason?: string
}

export interface ReportItem {
  report_id: number
  order_id: number
  type: string
  priority?: number
  status: string
  description?: string
  create_time: string
  evidence_count: number
}

export interface ReportDetail {
  report_id: number
  order_id: number
  type: string
  priority?: number
  status: string
  description?: string
  create_time: string
  reporter?: {
    user_id: number
    username: string
  }
  evidences: Array<{
    evidence_id: number
    file_type: string
    file_url: string
    uploaded_at: string
  }>
}

export interface HandleReportRequest {
  HandleResult: string
  HandleNote?: string
  ApplyPenalty?: boolean
  PenaltyType?: string
  PenaltyDuration?: number
}

export interface AdminReportsResponse {
  reports: ReportItem[]
  totalCount: number
  pageIndex: number
  pageSize: number
  totalPages: number
}

export interface AdminStatistics {
  totalUsers: number
  totalProducts: number
  totalReports: number
  pendingReports: number
  activeModerators: number
  todayOperations: number
  monthlyStats: Array<{
    month: string
    userCount: number
    productCount: number
    reportCount: number
  }>
}

// 管理员相关接口
export const adminApi = {
  // 获取当前管理员信息
  getCurrentAdminInfo: (): Promise<ApiResponse<AdminInfo>> => {
    return api.get('/api/admin/current')
  },

  // 获取管理员可管理的商品列表
  getManagedProducts: (query: AdminProductQuery): Promise<ApiResponse<AdminProductsResponse>> => {
    const params = new URLSearchParams()

    if (query.pageIndex !== undefined) params.append('pageIndex', query.pageIndex.toString())
    if (query.pageSize !== undefined) params.append('pageSize', query.pageSize.toString())
    if (query.status) params.append('status', query.status)
    if (query.categoryId !== undefined) params.append('categoryId', query.categoryId.toString())
    if (query.searchKeyword) params.append('searchKeyword', query.searchKeyword)
    if (query.userId !== undefined) params.append('userId', query.userId.toString())
    if (query.startDate) params.append('startDate', query.startDate)
    if (query.endDate) params.append('endDate', query.endDate)

    return api.get(`/api/admin/products?${params.toString()}`)
  },

  // 获取商品详情（管理员视角）
  getProductDetailForAdmin: (productId: number): Promise<ApiResponse<AdminProduct>> => {
    return api.get(`/api/admin/products/${productId}`)
  },

  // 更新商品信息（管理员操作）
  updateProductAsAdmin: (
    productId: number,
    data: AdminUpdateProductRequest
  ): Promise<ApiResponse> => {
    return api.put(`/api/admin/products/${productId}`, data)
  },

  // 删除商品（管理员操作）
  deleteProductAsAdmin: (productId: number, reason?: string): Promise<ApiResponse> => {
    return api.delete(`/api/admin/products/${productId}`, {
      data: reason || '管理员删除',
      headers: {
        'Content-Type': 'application/json',
      },
    })
  },

  // 批量操作商品
  batchOperateProducts: (
    data: BatchProductOperationRequest
  ): Promise<
    ApiResponse<{
      message: string
      failedProducts?: Array<{ productId: number; reason: string }>
    }>
  > => {
    return api.post('/api/admin/products/batch', data)
  },

  // 验证商品管理权限
  validateProductPermission: (
    productId: number
  ): Promise<
    ApiResponse<{
      hasPermission: boolean
      adminRole: string
    }>
  > => {
    return api.get(`/api/admin/products/${productId}/permission`)
  },

  // 获取管理员可管理的分类列表
  getManagedCategories: (): Promise<
    ApiResponse<{
      categoryIds: number[]
      adminRole: string
    }>
  > => {
    return api.get('/api/admin/categories')
  },

  // 获取管理员负责的举报列表
  getAdminReports: (
    pageIndex: number = 0,
    pageSize: number = 10,
    status?: string
  ): Promise<ApiResponse<AdminReportsResponse>> => {
    const params = new URLSearchParams()
    params.append('pageIndex', pageIndex.toString())
    params.append('pageSize', pageSize.toString())
    if (status) params.append('status', status)

    return api.get(`/api/admin/reports?${params.toString()}`)
  },

  // 处理举报
  handleReport: (reportId: number, data: HandleReportRequest): Promise<ApiResponse> => {
    return api.post(`/api/admin/reports/${reportId}/handle`, data)
  },

  // 获取举报详情（管理员专用）
  getReportDetail: (reportId: number): Promise<ApiResponse<ReportDetail>> => {
    return api.get(`/api/admin/reports/${reportId}`)
  },

  // 获取举报审核历史
  getReportAuditHistory: (
    reportId: number
  ): Promise<
    ApiResponse<
      Array<{
        timestamp: string
        action: string
        moderator: string
        comment: string
      }>
    >
  > => {
    return api.get(`/api/admin/reports/${reportId}/history`)
  },

  // 验证举报处理权限
  validateReportPermission: (
    reportId: number
  ): Promise<
    ApiResponse<{
      hasPermission: boolean
      adminRole: string
    }>
  > => {
    return api.get(`/api/admin/permissions/report/${reportId}`)
  },

  // 获取管理员统计信息
  getAdminStatistics: (): Promise<ApiResponse<AdminStatistics>> => {
    return api.get('/api/admin/statistics')
  },

  // 获取所有审计日志（仅系统管理员）
  getAllAuditLogs: (
    pageIndex: number = 0,
    pageSize: number = 10,
    targetAdminId?: number,
    actionType?: string,
    categoryId?: number,
    startDate?: Date,
    endDate?: Date
  ): Promise<
    ApiResponse<{
      logs: Array<{
        logId: number
        adminId: number
        adminUsername: string
        adminRole: string
        actionType: string
        targetId?: number
        logDetail?: string
        logTime: string
      }>
      pagination: {
        pageIndex: number
        pageSize: number
        totalCount: number
        totalPages: number
      }
    }>
  > => {
    const params = new URLSearchParams()
    params.append('pageIndex', pageIndex.toString())
    params.append('pageSize', pageSize.toString())
    if (targetAdminId !== undefined) params.append('targetAdminId', targetAdminId.toString())
    if (actionType) params.append('actionType', actionType)
    if (categoryId !== undefined) params.append('categoryId', categoryId.toString())
    if (startDate) params.append('startDate', startDate.toISOString())
    if (endDate) params.append('endDate', endDate.toISOString())

    return api.get(`/api/admin/audit-logs?${params.toString()}`)
  },

  // 权限管理相关API

  // 获取所有管理员列表（仅系统管理员）
  getAllAdmins: (
    pageIndex: number = 0,
    pageSize: number = 100,
    role?: string,
    searchKeyword?: string
  ): Promise<
    ApiResponse<{
      admins: Array<{
        adminId: number
        username: string
        role: string
        assignedCategory?: number
        email?: string
        isActive: boolean
      }>
      pagination: {
        pageIndex: number
        pageSize: number
        totalCount: number
        totalPages: number
      }
    }>
  > => {
    const params = new URLSearchParams()
    params.append('pageIndex', pageIndex.toString())
    params.append('pageSize', pageSize.toString())
    if (role) params.append('role', role)
    if (searchKeyword) params.append('searchKeyword', searchKeyword)

    return api.get(`/api/admin?${params.toString()}`)
  },

  // 分配分类管理员权限
  assignCategoryAdmin: (data: {
    username: string
    email: string
    categoryId: number
  }): Promise<
    ApiResponse<{
      adminId: number
      message: string
    }>
  > => {
    return api.post('/api/admin/assign', {
      username: data.username,
      email: data.email,
      role: 'category_admin',
      assignedCategory: data.categoryId,
    })
  },

  // 修改管理员分类权限
  updateAdminCategory: (
    adminId: number,
    newCategoryId: number
  ): Promise<
    ApiResponse<{
      message: string
    }>
  > => {
    return api.put(`/api/admin/${adminId}`, { assignedCategory: newCategoryId })
  },

  // 撤销管理员权限
  revokeAdminPermission: (
    adminId: number
  ): Promise<
    ApiResponse<{
      message: string
    }>
  > => {
    return api.delete(`/api/admin/${adminId}`)
  },

  // 获取管理员详细信息
  getAdminDetail: (
    adminId: number
  ): Promise<
    ApiResponse<{
      adminId: number
      username: string
      email: string
      role: string
      assignedCategory?: number
      createdAt: string
      lastLoginAt?: string
      isActive: boolean
    }>
  > => {
    return api.get(`/api/admin/${adminId}`)
  },
}

export default api
