import { defineStore } from 'pinia'
import { ref } from 'vue'
import {
  authApi,
  type RegisterData,
  type TokenResponse,
  type UserInfo,
  type ApiResponse,
  type UserProfileResponse,
  type UpdateUserProfileData,
  type ChangePasswordData,
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

export const useUserStore = defineStore('user', () => {
  const user = ref<User | null>(null)
  const token = ref<string>('')
  const refreshToken = ref<string>('')
  const isLoggedIn = ref<boolean>(false)
  const isAdmin = ref<boolean>(false)
  const adminInfo = ref<any>(null)
  
  // 添加防重复请求的标志
  const isLoadingProfile = ref<boolean>(false)

  // 初始化用户状态
  const initializeAuth = async () => {
    const savedToken = localStorage.getItem('token')
    const savedRefreshToken = localStorage.getItem('refreshToken')
    const savedUser = localStorage.getItem('user')
    const savedIsAdmin = localStorage.getItem('isAdmin')
    const savedAdminInfo = localStorage.getItem('adminInfo')

    if (savedToken && savedUser) {
      token.value = savedToken
      refreshToken.value = savedRefreshToken || ''
      user.value = JSON.parse(savedUser)
      isLoggedIn.value = true
      isAdmin.value = savedIsAdmin === 'true'
      
      if (savedAdminInfo) {
        try {
          adminInfo.value = JSON.parse(savedAdminInfo)
        } catch (e) {
          console.warn('解析管理员信息失败:', e)
          adminInfo.value = null
        }
      }

      // 如果用户已登录，重新验证管理员身份
      if (isLoggedIn.value) {
        checkAdminStatus().catch(console.warn)
      }
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

        // 保存用户信息（新增 emailVerified 字段）
        user.value = {
          userId: tokenData.user_id,
          username: tokenData.username,
          email: tokenData.email,
          studentId: tokenData.student_id,
          creditScore: tokenData.credit_score,
          emailVerified: tokenData.email_verified || false, // 新增字段，默认为 false
        }

        isLoggedIn.value = true

        // 保存到本地存储
        localStorage.setItem('token', token.value)
        localStorage.setItem('refreshToken', refreshToken.value)
        localStorage.setItem('user', JSON.stringify(user.value))

        // 检查管理员身份
        await checkAdminStatus()

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
    } finally {
      // 清除状态
      user.value = null
      token.value = ''
      refreshToken.value = ''
      isLoggedIn.value = false
      isAdmin.value = false
      adminInfo.value = null

      // 清除本地存储
      localStorage.removeItem('token')
      localStorage.removeItem('refreshToken')
      localStorage.removeItem('user')
      localStorage.removeItem('isAdmin')
      localStorage.removeItem('adminInfo')
    }
  }

  // 获取用户信息（新增 emailVerified 字段）
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
          emailVerified: userData.emailVerified || false, // 新增字段
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

  const sendVerificationCode = async (
    email: string
  ): Promise<{ success: boolean; message: string }> => {
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
  const logoutAll = async (): Promise<{
    success: boolean
    message: string
    revokedCount?: number
  }> => {
    try {
      const response = await authApi.logoutAll()

      if (response.success && response.data) {
        // 清除本地状态（可选，根据需求决定是否立即退出当前会话）
        return {
          success: true,
          message: response.message || '已退出所有设备',
          revokedCount: response.data.revokedTokens,
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

  // 获取用户详细信息（包含虚拟账户等）
  const getUserProfile = async () => {
    // 防止重复请求
    if (isLoadingProfile.value) {
      console.log('getUserProfile already in progress, skipping...')
      return { success: false, message: '正在获取用户信息，请稍候...' }
    }

    isLoadingProfile.value = true
    try {
      const response = await authApi.getUserProfile()

      if (response.success && response.data) {
        // 更新用户信息
        user.value = {
          userId: response.data.userId,
          username: response.data.username,
          email: response.data.email,
          fullName: response.data.fullName,
          phone: response.data.phone,
          studentId: response.data.studentId,
          creditScore: response.data.creditScore,
          emailVerified: response.data.emailVerified,
          isActive: response.data.isActive,
          createdAt: response.data.createdAt,
          lastLoginAt: response.data.lastLoginAt,
          lastLoginIp: response.data.lastLoginIp,
          loginCount: response.data.loginCount,
          student: response.data.student,
          virtualAccount: response.data.virtualAccount,
        }

        // 保存到本地存储
        localStorage.setItem('user', JSON.stringify(user.value))

        return {
          success: true,
          message: response.message || '获取用户信息成功',
          data: response.data,
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
    } finally {
      isLoadingProfile.value = false
    }
  }

  // 更新用户信息
  const updateUserProfile = async (updateData: {
    username?: string
    fullName?: string
    phone?: string
  }) => {
    try {
      const response = await authApi.updateUserProfile(updateData)

      if (response.success) {
        // 更新本地用户信息
        if (user.value) {
          if (updateData.username) user.value.username = updateData.username
          if (updateData.fullName) user.value.fullName = updateData.fullName
          if (updateData.phone) user.value.phone = updateData.phone
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
  const changePassword = async (passwordData: {
    currentPassword: string
    newPassword: string
    confirmPassword: string
  }) => {
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

  // 检查管理员身份
  const checkAdminStatus = async () => {
    try {
      // 导入adminApi
      const { adminApi } = await import('@/services/api')
      const response = await adminApi.getCurrentAdminInfo()

      if (response.success && response.data) {
        isAdmin.value = true
        adminInfo.value = response.data
        localStorage.setItem('isAdmin', 'true')
        localStorage.setItem('adminInfo', JSON.stringify(response.data))
        return { success: true, isAdmin: true, adminInfo: response.data }
      } else {
        isAdmin.value = false
        adminInfo.value = null
        localStorage.setItem('isAdmin', 'false')
        localStorage.removeItem('adminInfo')
        return { success: true, isAdmin: false }
      }
    } catch (error: unknown) {
      console.log('用户不是管理员或检查失败:', error)
      isAdmin.value = false
      adminInfo.value = null
      localStorage.setItem('isAdmin', 'false')
      localStorage.removeItem('adminInfo')
      return { success: true, isAdmin: false }
    }
  }

  return {
    user,
    token,
    refreshToken,
    isLoggedIn,
    isAdmin,
    adminInfo,
    isLoadingProfile,
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
    getUserProfile,
    updateUserProfile,
    changePassword,
    checkAdminStatus,
  }
})
