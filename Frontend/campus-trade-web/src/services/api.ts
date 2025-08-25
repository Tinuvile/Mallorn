import axios, { type InternalAxiosRequestConfig, type AxiosResponse, type AxiosError } from 'axios'

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
    userId,
    email
  })
},

// 验证邮箱验证码
verifyCode: (userId: number, code: string): Promise<ApiResponse> => {
  return api.post('/api/auth/verify-code', {
    userId,
    code
  })
},

// 验证邮箱链接（GET请求）
verifyEmail: (token: string): Promise<ApiResponse> => {
  return api.get(`/api/auth/verify-email?token=${encodeURIComponent(token)}`)
},

// 退出所有设备
logoutAll: (): Promise<ApiResponse<{ revokedTokens: number }>> => {
  return api.post('/api/auth/logout-all')
}

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

// 订单状态枚举
export enum OrderStatus {
  PENDING = 'pending',
  PROCESSING = 'processing',
  SHIPPED = 'shipped',
  DELIVERED = 'delivered',
  COMPLETED = 'completed',
  CANCELLED = 'cancelled'
}

// 订单相关接口
export const orderApi = {
  // 创建订单
  createOrder: (data: CreateOrderRequest): Promise<ApiResponse<OrderDetailResponse>> => {
    return api.post('/api/order', data)
  },

  // 获取订单详情
  getOrderDetail: (orderId: number): Promise<ApiResponse<OrderDetailResponse>> => {
    return api.get(`/api/order/${orderId}`)
  },

  // 获取用户订单列表
  getUserOrders: (filters?: {
    role?: 'buyer' | 'seller'
    status?: string
    pageIndex?: number
    pageSize?: number
  }): Promise<ApiResponse<UserOrdersResponse>> => {
    const params = new URLSearchParams()
    if (filters?.role) params.append('role', filters.role)
    if (filters?.status) params.append('status', filters.status)
    if (filters?.pageIndex) params.append('pageIndex', filters.pageIndex.toString())
    if (filters?.pageSize) params.append('pageSize', filters.pageSize.toString())
    
    return api.get(`/api/order?${params.toString()}`)
  },

  // 获取商品订单列表
  getProductOrders: (productId: number): Promise<ApiResponse<OrderListResponse[]>> => {
    return api.get(`/api/order/product/${productId}`)
  },

  // 获取用户订单统计
  getUserOrderStatistics: (): Promise<ApiResponse<OrderStatisticsResponse>> => {
    return api.get('/api/order/statistics')
  },

  // 更新订单状态
  updateOrderStatus: (orderId: number, data: UpdateOrderStatusRequest): Promise<ApiResponse<void>> => {
    return api.put(`/api/order/${orderId}/status`, data)
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
  id: number
  title: string
  price: number
  originalPrice?: number
  categoryId: number
  categoryName: string
  sellerId: number
  sellerName: string
  status: string
  condition: string
  primaryImage: string
  viewCount: number
  likeCount: number
  createdAt: string
  isLiked: boolean
  location?: string
}

export interface CategoryItem {
  id: number
  name: string
  parentId?: number
  icon?: string
  productCount?: number
  children?: CategoryItem[]
}

export interface CategoryBreadcrumb {
  id: number
  name: string
  level: number
}

export interface CreateProductRequest {
  title: string
  description: string
  price: number
  originalPrice?: number
  categoryId: number
  condition: string
  stock: number
  tags?: string[]
  specifications?: Omit<ProductSpecification, 'id'>[]
  location?: string
  contactMethod: string
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

export interface CategoryTreeResponse extends CategoryItem {
  children?: CategoryTreeResponse[]
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
  updateProduct: (productId: number, data: UpdateProductRequest): Promise<ApiResponse<ProductDetail>> => {
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
    
    if (queryParams.pageIndex !== undefined) params.append('pageIndex', queryParams.pageIndex.toString())
    if (queryParams.pageSize !== undefined) params.append('pageSize', queryParams.pageSize.toString())
    if (queryParams.categoryId !== undefined) params.append('categoryId', queryParams.categoryId.toString())
    if (queryParams.condition) params.append('condition', queryParams.condition)
    if (queryParams.minPrice !== undefined) params.append('minPrice', queryParams.minPrice.toString())
    if (queryParams.maxPrice !== undefined) params.append('maxPrice', queryParams.maxPrice.toString())
    if (queryParams.sortBy) params.append('sortBy', queryParams.sortBy)
    if (queryParams.sortOrder) params.append('sortOrder', queryParams.sortOrder)
    if (queryParams.status) params.append('status', queryParams.status)
    if (queryParams.tags && queryParams.tags.length > 0) {
      queryParams.tags.forEach(tag => params.append('tags', tag))
    }
    
    return api.get(`/api/product?${params.toString()}`)
  },

  // 搜索商品
  searchProducts: (searchParams: ProductSearchParams): Promise<ApiResponse<ProductListResponse>> => {
    const params = new URLSearchParams()
    params.append('keyword', searchParams.keyword)
    
    if (searchParams.pageIndex !== undefined) params.append('pageIndex', searchParams.pageIndex.toString())
    if (searchParams.pageSize !== undefined) params.append('pageSize', searchParams.pageSize.toString())
    if (searchParams.categoryId !== undefined) params.append('categoryId', searchParams.categoryId.toString())
    
    return api.get(`/api/product/search?${params.toString()}`)
  },

  // 获取热门商品
  getPopularProducts: (count: number = 10, categoryId?: number): Promise<ApiResponse<ProductListItem[]>> => {
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
    
    const endpoint = userId !== undefined 
      ? `/api/product/user/${userId}?${params.toString()}`
      : `/api/product/user?${params.toString()}`
    
    return api.get(endpoint)
  },

  // 获取分类树
  getCategoryTree: (includeProductCount: boolean = true): Promise<ApiResponse<CategoryTreeResponse[]>> => {
    return api.get(`/api/product/categories/tree?includeProductCount=${includeProductCount}`)
  },

  // 获取子分类
  getSubCategories: (parentId?: number): Promise<ApiResponse<CategoryItem[]>> => {
    const endpoint = parentId !== undefined 
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
  getProductsToAutoRemove: (days: number = 7): Promise<ApiResponse<ProductsToAutoRemoveResponse>> => {
    return api.get(`/api/product/auto-remove?days=${days}`)
  },

  // 延期商品下架时间
  extendProductAutoRemoveTime: (productId: number, extendDays: number): Promise<ApiResponse> => {
    return api.patch(`/api/product/${productId}/extend`, { extendDays })
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

export interface DashboardStatsDto {
  monthlyTransactions: MonthlyTransactionDto[];
  popularProducts: PopularProductDto[];
  userActivities: UserActivityDto[];
}

export interface MonthlyTransactionDto {
  month: string;
  orderCount: number;
  totalAmount: number;
}

export interface PopularProductDto {
  productId: number;
  productTitle: string;
  orderCount: number;
}

export interface UserActivityDto {
  date: string;
  activeUserCount: number;
  newUserCount: number;
}

export interface DashboardSummaryDto {
  totalUsers: number;
  activeUsers: number;
  totalOrders: number;
  monthlySales: number;
}

// Dashboard API方法
export const dashboardApi = {
  // 获取统计数据
  getStatistics: (year: number, activityDays: number = 30): Promise<ApiResponse<DashboardStatsDto>> => {
    return api.get(`/api/dashboard/statistics?year=${year}&activityDays=${activityDays}`)
  },

  // 获取汇总数据
  getSummary: (): Promise<ApiResponse<DashboardSummaryDto>> => {
    return api.get('/api/dashboard/summary')
  },

  // 导出Excel
  exportExcel: (year: number): Promise<Blob> => {
    return api.get(`/api/dashboard/export/excel?year=${year}`, {
      responseType: 'blob'
    })
  },

  // 导出PDF
  exportPdf: (year: number): Promise<Blob> => {
    return api.get(`/api/dashboard/export/pdf?year=${year}`, {
      responseType: 'blob'
    })
  }
}

export default api
