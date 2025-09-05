// store/modules/order.ts
import { defineStore } from 'pinia'
import { ref } from 'vue'
import {
  orderApi,
  type ApiResponse,
  type OrderListResponse,
  type UserOrdersResponse,
  type OrderDetailResponse,
  type OrderStatisticsResponse,
  type CreateOrderRequest,
  type UpdateOrderStatusRequest,
  type ShipOrderRequest,
  type CancelOrderRequest,
  type PaymentResult,
  OrderStatus,
} from '@/services/api'
import { getStatusDisplayText, canExecuteAction } from '@/utils/orderStatusMapping'

// 直接使用从 api.ts 导入的接口，避免重复定义

export const useOrderStore = defineStore('order', () => {
  const orders = ref<OrderListResponse[]>([])
  const currentOrder = ref<OrderDetailResponse | null>(null)
  const isLoading = ref<boolean>(false)
  const statistics = ref<OrderStatisticsResponse | null>(null)

  // 从本地存储初始化订单数据
  const initializeOrders = () => {
    const savedOrders = localStorage.getItem('userOrders')
    if (savedOrders) {
      orders.value = JSON.parse(savedOrders)
    }
  }

  // 创建订单
  const createOrder = async (request: CreateOrderRequest) => {
    isLoading.value = true
    try {
      const response: ApiResponse<OrderDetailResponse> = await orderApi.createOrder(request)

      if (response.success && response.data) {
        // 将详细订单转换为列表格式并添加到订单列表开头
        const orderList: OrderListResponse = {
          id: response.data.id,
          orderNumber: response.data.orderNumber,
          orderDate: response.data.orderDate,
          status: response.data.status,
          productName: response.data.items[0]?.productName || '',
          productImage: response.data.items[0]?.productImage || '',
          totalAmount: response.data.totalAmount,
          quantity: response.data.items.reduce((sum, item) => sum + item.quantity, 0),
          userRole: 'buyer', // 创建订单的用户是买家
        }

        orders.value.unshift(orderList)
        currentOrder.value = response.data

        // 更新本地存储
        localStorage.setItem('userOrders', JSON.stringify(orders.value))

        return {
          success: true,
          data: response.data,
          message: response.message || '创建订单成功',
        }
      }

      return {
        success: false,
        message: response.message || '创建订单失败',
      }
    } catch (error: unknown) {
      console.error('创建订单错误:', error)
      let message = '创建订单失败，请重试'

      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }

      return { success: false, message }
    } finally {
      isLoading.value = false
    }
  }

  // 获取订单详情
  const getOrderDetail = async (orderId: number) => {
    isLoading.value = true
    try {
      const response: ApiResponse<OrderDetailResponse> = await orderApi.getOrderDetail(orderId)

      if (response.success && response.data) {
        currentOrder.value = response.data
        return {
          success: true,
          data: response.data,
          message: response.message || '获取订单详情成功',
        }
      }

      return {
        success: false,
        message: response.message || '获取订单详情失败',
      }
    } catch (error: unknown) {
      console.error('获取订单详情错误:', error)
      let message = '获取订单详情失败，请重试'

      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }

      return { success: false, message }
    } finally {
      isLoading.value = false
    }
  }

  // 获取用户订单列表
  const getUserOrders = async (filters?: {
    role?: 'buyer' | 'seller'
    status?: OrderStatus
    pageIndex?: number
    pageSize?: number
  }) => {
    isLoading.value = true
    try {
      const response: ApiResponse<UserOrdersResponse> = await orderApi.getUserOrders(filters)

      if (response.success && response.data) {
        orders.value = response.data.orders
        // 保存到本地存储
        localStorage.setItem('userOrders', JSON.stringify(orders.value))
        return {
          success: true,
          data: response.data,
          message: response.message || '获取订单成功',
        }
      }

      return {
        success: false,
        message: response.message || '获取订单失败',
      }
    } catch (error: unknown) {
      console.error('获取订单列表错误:', error)
      let message = '获取订单失败，请重试'

      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }

      return { success: false, message }
    } finally {
      isLoading.value = false
    }
  }

  // 获取商品订单列表
  const getProductOrders = async (productId: number) => {
    isLoading.value = true
    try {
      const response: ApiResponse<OrderListResponse[]> = await orderApi.getProductOrders(productId)

      if (response.success && response.data) {
        return {
          success: true,
          data: response.data,
          message: response.message || '获取商品订单成功',
        }
      }

      return {
        success: false,
        message: response.message || '获取商品订单失败',
      }
    } catch (error: unknown) {
      console.error('获取商品订单错误:', error)
      let message = '获取商品订单失败，请重试'

      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }

      return { success: false, message }
    } finally {
      isLoading.value = false
    }
  }

  // 获取用户订单统计
  const getUserOrderStatistics = async () => {
    try {
      const response: ApiResponse<OrderStatisticsResponse> = await orderApi.getUserOrderStatistics()

      if (response.success && response.data) {
        statistics.value = response.data
        return {
          success: true,
          data: response.data,
          message: response.message || '获取统计成功',
        }
      }

      return {
        success: false,
        message: response.message || '获取统计失败',
      }
    } catch (error: unknown) {
      console.error('获取订单统计错误:', error)
      let message = '获取订单统计失败，请重试'

      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }

      return { success: false, message }
    }
  }

  // 更新订单状态
  const updateOrderStatus = async (orderId: number, request: UpdateOrderStatusRequest) => {
    try {
      const response: ApiResponse<void> = await orderApi.updateOrderStatus(orderId, request)

      if (response.success) {
        // 更新本地订单列表中的状态
        const orderIndex = orders.value.findIndex(order => order.id === orderId)
        if (orderIndex !== -1) {
          orders.value[orderIndex].status = request.status
          localStorage.setItem('userOrders', JSON.stringify(orders.value))
        }

        // 如果当前查看的就是这个订单，也更新
        if (currentOrder.value && currentOrder.value.id === orderId) {
          currentOrder.value.status = request.status
          if (request.trackingInfo) {
            currentOrder.value.trackingInfo = request.trackingInfo
          }
        }

        return {
          success: true,
          message: response.message || '更新订单状态成功',
        }
      }

      return {
        success: false,
        message: response.message || '更新订单状态失败',
      }
    } catch (error: unknown) {
      console.error('更新订单状态错误:', error)
      let message = '更新订单状态失败，请重试'

      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }

      return { success: false, message }
    }
  }

  // 确认付款
  const confirmPayment = async (orderId: number) => {
    try {
      const response: ApiResponse<void> = await orderApi.confirmPayment(orderId)

      if (response.success) {
        // 更新订单状态为待发货
        await updateOrderStatus(orderId, { status: OrderStatus.PROCESSING })
        return {
          success: true,
          message: response.message || '付款确认成功',
        }
      }

      return {
        success: false,
        message: response.message || '付款确认失败',
      }
    } catch (error: unknown) {
      console.error('确认付款错误:', error)
      let message = '确认付款失败，请重试'

      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }

      return { success: false, message }
    }
  }

  // 发货
  const shipOrder = async (orderId: number, request?: ShipOrderRequest) => {
    try {
      const response: ApiResponse<void> = await orderApi.shipOrder(orderId, request)

      if (response.success) {
        // 更新订单状态为已发货
        await updateOrderStatus(orderId, {
          status: OrderStatus.SHIPPED,
          trackingInfo: request?.trackingInfo,
        })
        return {
          success: true,
          message: response.message || '发货成功',
        }
      }

      return {
        success: false,
        message: response.message || '发货失败',
      }
    } catch (error: unknown) {
      console.error('发货错误:', error)
      let message = '发货失败，请重试'

      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }

      return { success: false, message }
    }
  }

  // 确认收货
  const confirmDelivery = async (orderId: number) => {
    try {
      // 步骤1：确认收货
      const deliveryResponse: ApiResponse<void> = await orderApi.confirmDelivery(orderId)

      if (!deliveryResponse.success) {
        return {
          success: false,
          message: deliveryResponse.message || '确认收货失败',
        }
      }

      // 步骤2：自动完成订单（转账给卖家）
      const completeResponse: ApiResponse<void> = await orderApi.completeOrder(orderId)

      if (completeResponse.success) {
        // 刷新订单列表获取最新状态
        await getUserOrders()
        return {
          success: true,
          message: '确认收货并完成订单成功',
        }
      }

      return {
        success: false,
        message: completeResponse.message || '确认收货成功，但完成订单失败',
      }
    } catch (error: unknown) {
      console.error('确认收货错误:', error)
      let message = '确认收货失败，请重试'

      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }

      return { success: false, message }
    }
  }

  // 完成订单
  const completeOrder = async (orderId: number, review?: string) => {
    try {
      const response: ApiResponse<void> = await orderApi.completeOrder(orderId, review)

      if (response.success) {
        // 更新订单状态为已完成
        await updateOrderStatus(orderId, { status: OrderStatus.COMPLETED })

        // 如果提供了评价，更新当前订单的评价信息
        if (currentOrder.value && currentOrder.value.id === orderId && review) {
          currentOrder.value.review = review
          currentOrder.value.reviewDate = new Date().toISOString()
        }

        return {
          success: true,
          message: response.message || '订单完成成功',
        }
      }

      return {
        success: false,
        message: response.message || '订单完成失败',
      }
    } catch (error: unknown) {
      console.error('完成订单错误:', error)
      let message = '完成订单失败，请重试'

      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }

      return { success: false, message }
    }
  }

  // 支付订单
  const payOrder = async (orderId: number) => {
    try {
      const response: ApiResponse<PaymentResult> = await orderApi.payOrder(orderId)

      if (response.success && response.data) {
        if (response.data.success) {
          // 支付成功，刷新订单列表
          await getUserOrders()
          return {
            success: true,
            message: response.data.message || '支付成功',
            amount: response.data.amount,
          }
        } else {
          return {
            success: false,
            message: response.data.message || '支付失败',
          }
        }
      }

      return {
        success: false,
        message: response.message || '支付失败',
      }
    } catch (error: unknown) {
      console.error('支付订单错误:', error)
      let message = '支付失败，请重试'

      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }

      return { success: false, message }
    }
  }

  // 取消订单
  const cancelOrder = async (orderId: number, request?: CancelOrderRequest) => {
    try {
      const response: ApiResponse<void> = await orderApi.cancelOrder(orderId, request)

      if (response.success) {
        // 更新订单状态为已取消
        await updateOrderStatus(orderId, {
          status: OrderStatus.CANCELLED,
          reason: request?.reason,
        })
        return {
          success: true,
          message: response.message || '取消订单成功',
        }
      }

      return {
        success: false,
        message: response.message || '取消订单失败',
      }
    } catch (error: unknown) {
      console.error('取消订单错误:', error)
      let message = '取消订单失败，请重试'

      if (error && typeof error === 'object' && 'response' in error) {
        const err = error as { response?: { data?: { message?: string } } }
        if (err.response?.data?.message) {
          message = err.response.data.message
        }
      }

      return { success: false, message }
    }
  }

  // 根据状态筛选订单
  const getOrdersByStatus = (status: OrderStatus | 'all') => {
    if (status === 'all') return orders.value
    return orders.value.filter(order => order.status === status)
  }

  // 根据ID获取订单
  const getOrderById = (orderId: number) => {
    return orders.value.find(order => order.id === orderId)
  }

  // 清除订单数据
  const clearOrders = () => {
    orders.value = []
    currentOrder.value = null
    statistics.value = null
    localStorage.removeItem('userOrders')
  }

  return {
    // 状态
    orders,
    currentOrder,
    isLoading,
    statistics,

    // 方法
    initializeOrders,
    createOrder,
    getOrderDetail,
    getUserOrders,
    getProductOrders,
    getUserOrderStatistics,
    updateOrderStatus,
    confirmPayment,
    shipOrder,
    confirmDelivery,
    completeOrder,
    payOrder,
    cancelOrder,
    getOrdersByStatus,
    getOrderById,
    clearOrders,
  }
})
