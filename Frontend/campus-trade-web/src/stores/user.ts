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
  emailVerified?: boolean
  isActive?: boolean
  createdAt?: string
  lastLoginAt?: string
  lastLoginIp?: string
  loginCount?: number
}

export interface UserProfile {
  userId: number
  username: string
  email: string
  fullName?: string
  phone?: string
  studentId?: string
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
    department?: string
  }
  virtualAccount?: {
    accountId: number
    balance: number
    createdAt: string
  }
}

export interface UpdateProfileData {
  username?: string
  fullName?: string
  phone?: string
}

export interface ChangePasswordData {
  currentPassword: string
  newPassword: string
  confirmPassword: string
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
          emailVerified: tokenData.email_verified || false
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
          emailVerified: userData.emailVerified || false
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

  // 发送验证码
  const sendVerificationCode = async (email: string): Promise<{ success: boolean; message: string }> => {
    try {
      if (!user.value?.userId) {
        return { success: false, message: '用户未登录' }
      }

      const response = await authApi.sendVerificationCode(user.value.userId, email)

      if (response.success) {
        return { success: true, message: response.message || '验证码发送成功' }
      }

      return { success: false, message: response.message || '发送验证码失败' }
    } catch (error: unknown) {
      console.error('发送验证码失败:', error)
      let message = '发送验证码失败，请重试'

      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }

      return { success: false, message }
    }
  }

  // 验证邮箱验证码
  const verifyCode = async (code: string): Promise<{ success: boolean; message: string }> => {
    try {
      if (!user.value?.userId) {
        return { success: false, message: '用户未登录' }
      }

      const response = await authApi.verifyCode(user.value.userId, code)

      if (response.success) {
        // 更新本地用户信息的邮箱验证状态
        if (user.value) {
          user.value.emailVerified = true
          localStorage.setItem('user', JSON.stringify(user.value))
        }
        return { success: true, message: response.message || '验证成功' }
      }

      return { success: false, message: response.message || '验证失败' }
    } catch (error: unknown) {
      console.error('验证码验证失败:', error)
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

  // 验证邮箱链接
  const verifyEmailLink = async (token: string): Promise<{ success: boolean; message: string }> => {
    try {
      const response = await authApi.verifyEmail(token)

      if (response.success) {
        // 更新本地用户信息的邮箱验证状态
        if (user.value) {
          user.value.emailVerified = true
          localStorage.setItem('user', JSON.stringify(user.value))
        }
        return { success: true, message: response.message || '邮箱验证成功' }
      }

      return { success: false, message: response.message || '邮箱验证失败' }
    } catch (error: unknown) {
      console.error('邮箱验证失败:', error)
      let message = '邮箱验证失败，请重试'

      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }

      return { success: false, message }
    }
  }

  // 退出所有设备
  const logoutAll = async (): Promise<{ success: boolean; message: string; revokedCount?: number }> => {
    try {
      const response = await authApi.logoutAll()

      if (response.success && response.data) {
        return { 
          success: true, 
          message: response.message || '已退出所有设备',
          revokedCount: response.data.revokedTokens
        }
      }

      return { success: false, message: response.message || '退出所有设备失败' }
    } catch (error: unknown) {
      console.error('退出所有设备失败:', error)
      let message = '退出所有设备失败，请重试'

      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }

      return { success: false, message }
    }
  }

  // 获取用户详细信息（包含信用分、虚拟账户等）
  const fetchUserProfile = async (): Promise<{ success: boolean; message: string; data?: UserProfile }> => {
    try {
      const response = await authApi.getUserProfile()

      if (response.success && response.data) {
        return { 
          success: true, 
          message: response.message || '获取用户信息成功',
          data: response.data
        }
      }

      return { success: false, message: response.message || '获取用户信息失败' }
    } catch (error: unknown) {
      console.error('获取用户详细信息失败:', error)
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

  // 更新用户基本信息
  const updateUserProfile = async (profileData: UpdateProfileData): Promise<{ success: boolean; message: string }> => {
    try {
      const response = await authApi.updateUserProfile(profileData)

      if (response.success) {
        // 更新本地用户信息
        if (user.value) {
          if (profileData.username) user.value.username = profileData.username
          if (profileData.fullName) user.value.fullName = profileData.fullName
          if (profileData.phone) user.value.phone = profileData.phone
          localStorage.setItem('user', JSON.stringify(user.value))
        }
        return { success: true, message: response.message || '用户信息更新成功' }
      }

      return { success: false, message: response.message || '用户信息更新失败' }
    } catch (error: unknown) {
      console.error('更新用户信息失败:', error)
      let message = '更新用户信息失败'

      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }

      return { success: false, message }
    }
  }

  // 修改密码
  const changePassword = async (passwordData: ChangePasswordData): Promise<{ success: boolean; message: string }> => {
    try {
      const response = await authApi.changePassword(passwordData)

      if (response.success) {
        return { success: true, message: response.message || '密码修改成功' }
      }

      return { success: false, message: response.message || '密码修改失败' }
    } catch (error: unknown) {
      console.error('修改密码失败:', error)
      let message = '修改密码失败'

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
    sendVerificationCode,    
    verifyCode,              
    verifyEmailLink,         
    logoutAll,
    fetchUserProfile,
    updateUserProfile,
    changePassword
  }
})