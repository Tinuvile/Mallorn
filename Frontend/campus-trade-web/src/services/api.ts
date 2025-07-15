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
  createdAt: string
  student?: {
    studentId: string
    name: string
    department: string
  }
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
}

export default api
