import { defineStore } from 'pinia'
import { ref } from 'vue'
import {
  dashboardApi,
  type DashboardStatsDto,
  type DashboardSummaryDto,
  type ApiResponse
} from '@/services/api'

export const useDashboardStore = defineStore('dashboard', () => {
  // 状态
  const stats = ref<DashboardStatsDto | null>(null)
  const summary = ref<DashboardSummaryDto | null>(null)
  const isLoading = ref<boolean>(false)
  const error = ref<string | null>(null)

  // 获取统计数据
  const fetchStatistics = async (year: number, activityDays: number = 30) => {
    isLoading.value = true
    error.value = null
    
    try {
      const response: ApiResponse<DashboardStatsDto> = await dashboardApi.getStatistics(year, activityDays)
      
      if (response.success && response.data) {
        stats.value = response.data
        return { success: true, message: response.message }
      } else {
        error.value = response.message || '获取统计数据失败'
        return { success: false, message: response.message || '获取统计数据失败' }
      }
    } catch (err: unknown) {
      console.error('获取统计数据失败:', err)
      let message = '获取统计数据失败，请重试'
      
      if (err && typeof err === 'object' && 'response' in err) {
        const errorObj = err as { response?: { data?: { message?: string } } }
        if (errorObj.response?.data?.message) {
          message = errorObj.response.data.message
        }
      }
      
      error.value = message
      return { success: false, message }
    } finally {
      isLoading.value = false
    }
  }

  // 获取汇总数据
  const fetchSummary = async () => {
    isLoading.value = true
    error.value = null
    
    try {
      const response: ApiResponse<DashboardSummaryDto> = await dashboardApi.getSummary()
      
      if (response.success && response.data) {
        summary.value = response.data
        return { success: true, message: response.message }
      } else {
        error.value = response.message || '获取汇总数据失败'
        return { success: false, message: response.message || '获取汇总数据失败' }
      }
    } catch (err: unknown) {
      console.error('获取汇总数据失败:', err)
      let message = '获取汇总数据失败，请重试'
      
      if (err && typeof err === 'object' && 'response' in err) {
        const errorObj = err as { response?: { data?: { message?: string } } }
        if (errorObj.response?.data?.message) {
          message = errorObj.response.data.message
        }
      }
      
      error.value = message
      return { success: false, message }
    } finally {
      isLoading.value = false
    }
  }

  // 导出Excel
  const exportToExcel = async (year: number): Promise<{ success: boolean; message: string; blob?: Blob }> => {
    try {
      const blob = await dashboardApi.exportExcel(year)
      return { 
        success: true, 
        message: '导出成功',
        blob
      }
    } catch (err: unknown) {
      console.error('导出Excel失败:', err)
      let message = '导出Excel失败，请重试'
      
      if (err && typeof err === 'object' && 'response' in err) {
        const errorObj = err as { response?: { data?: { message?: string } } }
        if (errorObj.response?.data?.message) {
          message = errorObj.response.data.message
        }
      }
      
      return { success: false, message }
    }
  }

  // 导出PDF
  const exportToPdf = async (year: number): Promise<{ success: boolean; message: string; blob?: Blob }> => {
    try {
      const blob = await dashboardApi.exportPdf(year)
      return { 
        success: true, 
        message: '导出成功',
        blob
      }
    } catch (err: unknown) {
      console.error('导出PDF失败:', err)
      let message = '导出PDF失败，请重试'
      
      if (err && typeof err === 'object' && 'response' in err) {
        const errorObj = err as { response?: { data?: { message?: string } } }
        if (errorObj.response?.data?.message) {
          message = errorObj.response.data.message
        }
      }
      
      return { success: false, message }
    }
  }

  // 下载文件
  const downloadFile = (blob: Blob, filename: string) => {
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = filename
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    window.URL.revokeObjectURL(url)
  }

  // 重置状态
  const reset = () => {
    stats.value = null
    summary.value = null
    isLoading.value = false
    error.value = null
  }

  return {
    // 状态
    stats,
    summary,
    isLoading,
    error,
    
    // 方法
    fetchStatistics,
    fetchSummary,
    exportToExcel,
    exportToPdf,
    downloadFile,
    reset
  }
})