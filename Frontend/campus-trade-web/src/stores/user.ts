import { defineStore } from 'pinia'
import { ref } from 'vue'
import {
  authApi,
  type RegisterData,
  type TokenResponse,
  type UserInfo,
  type ApiResponse,
} from '@/services/api'

export interface User {
  userId?: number
  username: string
  email: string
  fullName?: string
  phone?: string
  studentId?: string
  creditScore?: number
}

export const useUserStore = defineStore('user', () => {
  const user = ref<User | null>(null)
  const token = ref<string>('')
  const refreshToken = ref<string>('')
  const isLoggedIn = ref<boolean>(false)

  // 初始化用户状态
  const initializeAuth = () => {
    const savedToken = localStorage.getItem('token')
    const savedRefreshToken = localStorage.getItem('refreshToken')
    const savedUser = localStorage.getItem('user')

    if (savedToken && savedUser) {
      token.value = savedToken
      refreshToken.value = savedRefreshToken || ''
      user.value = JSON.parse(savedUser)
      isLoggedIn.value = true
    }
  }

  // 登录
  const login = async (loginData: {
    username: string
    password: string
    remember_me?: boolean
  }) => {
    try {
      const response: ApiResponse<TokenResponse> = await authApi.login(loginData)

      if (response.success && response.data) {
        const tokenData = response.data

        // 保存token
        token.value = tokenData.access_token
        refreshToken.value = tokenData.refresh_token

        // 保存用户信息
        user.value = {
          userId: tokenData.user_id,
          username: tokenData.username,
          email: tokenData.email,
          studentId: tokenData.student_id,
          creditScore: tokenData.credit_score,
        }

        isLoggedIn.value = true

        // 保存到本地存储
        localStorage.setItem('token', token.value)
        localStorage.setItem('refreshToken', refreshToken.value)
        localStorage.setItem('user', JSON.stringify(user.value))

        return { success: true, message: response.message || '登录成功' }
      }

      return { success: false, message: response.message || '登录失败' }
    } catch (error: unknown) {
      let message = '登录失败，请重试'

      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string; success?: boolean } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        } else if (err.response?.data?.success === false) {
          message = err.response.data.message || '登录失败'
        }
      }

      return { success: false, message }
    }
  }

  // 注册
  const register = async (registerData: RegisterData) => {
    try {
      const response: ApiResponse<UserInfo> = await authApi.register(registerData)

      if (response.success) {
        return { success: true, message: response.message || '注册成功' }
      }

      return { success: false, message: response.message || '注册失败' }
    } catch (error: unknown) {
      console.error('注册错误详情:', error)
      let message = '注册失败，请重试'

      // 尝试从不同的错误响应结构中提取错误消息
      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: unknown } }
        const errorData = err.response?.data
        if (typeof errorData === 'string') {
          message = errorData
        } else if (errorData && typeof errorData === 'object') {
          const data = errorData as Record<string, unknown>
          if (typeof data.message === 'string') {
            message = data.message
          } else if (typeof data.error === 'string') {
            message = data.error
          } else if (typeof data.details === 'string') {
            message = data.details
          }
        }
      } else if (
        error &&
        typeof error === 'object' &&
        'message' in error &&
        typeof (error as { message: unknown }).message === 'string'
      ) {
        message = (error as { message: string }).message
      }

      return { success: false, message }
    }
  }

  // 验证学生身份
  const validateStudent = async (studentId: string, name: string) => {
    try {
      const response = await authApi.validateStudent(studentId, name)

      if (response.success && response.data) {
        return {
          success: true,
          message: response.message || '验证成功',
          isValid: response.data.isValid,
        }
      }

      return { success: false, message: response.message || '验证失败' }
    } catch (error: unknown) {
      let message = '验证失败，请重试'

      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }

      return { success: false, message }
    }
  }

  // 登出
  const logout = async () => {
    try {
      if (refreshToken.value) {
        await authApi.logout(refreshToken.value)
      }
    } catch (error) {
      console.error('登出请求失败:', error)
      // 即使请求失败也要清除本地状态
    } finally {
      // 清除状态
      user.value = null
      token.value = ''
      refreshToken.value = ''
      isLoggedIn.value = false

      // 清除本地存储
      localStorage.removeItem('token')
      localStorage.removeItem('refreshToken')
      localStorage.removeItem('user')
    }
  }

  // 获取用户信息
  const fetchUserInfo = async (username: string) => {
    try {
      const response: ApiResponse<UserInfo> = await authApi.getUser(username)

      if (response.success && response.data) {
        const userData = response.data
        user.value = {
          userId: userData.userId,
          username: userData.username,
          email: userData.email,
          fullName: userData.fullName,
          phone: userData.phone,
          studentId: userData.studentId,
          creditScore: userData.creditScore,
        }
        localStorage.setItem('user', JSON.stringify(user.value))

        return { success: true, message: response.message || '获取用户信息成功' }
      }

      return { success: false, message: response.message || '获取用户信息失败' }
    } catch (error: unknown) {
      console.error('获取用户信息失败:', error)
      let message = '获取用户信息失败'

      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }

      return { success: false, message }
    }
  }

  return {
    user,
    token,
    refreshToken,
    isLoggedIn,
    initializeAuth,
    login,
    register,
    validateStudent,
    logout,
    fetchUserInfo,
  }
})
