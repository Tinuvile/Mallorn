import { defineStore } from 'pinia'
import { ref } from 'vue'
import { virtualAccountApi, type VirtualAccountBalance, type VirtualAccountDetails, type BalanceCheckResult } from '@/services/api'

export interface VirtualAccount {
  accountId?: number
  userId: number
  balance: number
  lastUpdateTime?: string
  createTime?: string
}

export const useVirtualAccountStore = defineStore('virtualAccount', () => {
  const account = ref<VirtualAccount | null>(null)
  const isLoading = ref<boolean>(false)
  const error = ref<string | null>(null)

  // 初始化虚拟账户状态
  const initializeAccount = () => {
    const savedAccount = localStorage.getItem('virtualAccount')
    if (savedAccount) {
      account.value = JSON.parse(savedAccount)
    }
  }

  // 获取余额
  const fetchBalance = async (): Promise<{ success: boolean; message: string; data?: VirtualAccountBalance }> => {
    try {
      isLoading.value = true
      error.value = null

      const response = await virtualAccountApi.getBalance()

      if (response.success && response.data) {
        const balanceData = response.data
        
        // 更新账户信息
        account.value = {
          userId: balanceData.userId,
          balance: balanceData.balance,
          lastUpdateTime: balanceData.lastUpdateTime
        }

        // 保存到本地存储
        localStorage.setItem('virtualAccount', JSON.stringify(account.value))

        return { 
          success: true, 
          message: response.message || '获取余额成功', 
          data: balanceData 
        }
      }

      return { 
        success: false, 
        message: response.message || '获取余额失败' 
      }
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : '获取余额失败，请重试'
      error.value = message
      return { success: false, message }
    } finally {
      isLoading.value = false
    }
  }

  // 获取账户详情
  const fetchAccountDetails = async (): Promise<{ success: boolean; message: string; data?: VirtualAccountDetails }> => {
    try {
      isLoading.value = true
      error.value = null

      const response = await virtualAccountApi.getAccountDetails()

      if (response.success && response.data) {
        const detailsData = response.data
        
        // 更新账户信息
        account.value = {
          accountId: detailsData.accountId,
          userId: detailsData.userId,
          balance: detailsData.balance,
          createTime: detailsData.createTime
        }

        // 保存到本地存储
        localStorage.setItem('virtualAccount', JSON.stringify(account.value))

        return { 
          success: true, 
          message: response.message || '获取账户详情成功', 
          data: detailsData 
        }
      }

      return { 
        success: false, 
        message: response.message || '获取账户详情失败' 
      }
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : '获取账户详情失败，请重试'
      error.value = message
      return { success: false, message }
    } finally {
      isLoading.value = false
    }
  }

  // 检查余额是否充足
  const checkBalance = async (amount: number): Promise<{ success: boolean; message: string; data?: BalanceCheckResult }> => {
    try {
      if (amount <= 0) {
        return { 
          success: false, 
          message: '检查金额必须大于0' 
        }
      }

      isLoading.value = true
      error.value = null

      const response = await virtualAccountApi.checkBalance(amount)

      if (response.success && response.data) {
        return { 
          success: true, 
          message: response.message || '余额检查成功', 
          data: response.data 
        }
      }

      return { 
        success: false, 
        message: response.message || '余额检查失败' 
      }
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : '余额检查失败，请重试'
      error.value = message
      return { success: false, message }
    } finally {
      isLoading.value = false
    }
  }

  // 清除账户信息
  const clearAccount = () => {
    account.value = null
    localStorage.removeItem('virtualAccount')
  }

  return {
    account,
    isLoading,
    error,
    initializeAccount,
    fetchBalance,
    fetchAccountDetails,
    checkBalance,
    clearAccount
  }
})