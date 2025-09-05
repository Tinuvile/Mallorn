/**
 * 订单状态映射工具
 * 用于前后端状态值的转换
 */

// 后端中文状态 -> 前端英文状态
export const backendToFrontendStatus = (backendStatus: string): string => {
  const statusMap: Record<string, string> = {
    待付款: 'pending',
    已付款: 'processing',
    已发货: 'shipped',
    已送达: 'delivered',
    已完成: 'completed',
    已取消: 'cancelled',
  }

  return statusMap[backendStatus] || backendStatus
}

// 前端英文状态 -> 后端中文状态
export const frontendToBackendStatus = (frontendStatus: string): string => {
  const statusMap: Record<string, string> = {
    pending: '待付款',
    processing: '已付款',
    shipped: '已发货',
    delivered: '已送达',
    completed: '已完成',
    cancelled: '已取消',
  }

  return statusMap[frontendStatus] || frontendStatus
}

// 状态显示文本映射
export const getStatusDisplayText = (status: string, userRole?: 'buyer' | 'seller'): string => {
  // 统一转换为前端状态
  const frontendStatus = backendToFrontendStatus(status)

  if (!userRole) {
    const statusMap: Record<string, string> = {
      pending: '待付款',
      processing: '待发货',
      shipped: '已发货',
      delivered: '待收货',
      completed: '已完成',
      cancelled: '已取消',
    }
    return statusMap[frontendStatus] || status
  }

  // 根据用户角色显示不同的状态文本
  if (userRole === 'seller') {
    const sellerStatusMap: Record<string, string> = {
      pending: '待付款',
      processing: '待发货',
      shipped: '待收货', // 卖家视角：已发货后是待收货
      delivered: '待收货',
      completed: '已完成',
      cancelled: '已取消',
    }
    return sellerStatusMap[frontendStatus] || status
  } else {
    const buyerStatusMap: Record<string, string> = {
      pending: '待付款',
      processing: '待发货',
      shipped: '已发货', // 买家视角：已发货
      delivered: '待收货',
      completed: '已完成',
      cancelled: '已取消',
    }
    return buyerStatusMap[frontendStatus] || status
  }
}

// 状态颜色映射
export const getStatusColor = (status: string): string => {
  // 统一转换为前端状态
  const frontendStatus = backendToFrontendStatus(status)

  const colorMap: Record<string, string> = {
    pending: 'warning',
    processing: 'info',
    shipped: 'orange',
    delivered: 'primary',
    completed: 'success',
    cancelled: 'error',
  }
  return colorMap[frontendStatus] || 'grey'
}

// 判断状态是否可以执行某个操作
export const canExecuteAction = (
  status: string,
  action: 'pay' | 'ship' | 'confirm_delivery' | 'complete' | 'cancel',
  userRole: 'buyer' | 'seller'
): boolean => {
  // 统一转换为前端状态
  const frontendStatus = backendToFrontendStatus(status)

  const actionRules: Record<string, Record<string, string[]>> = {
    pending: {
      buyer: ['pay', 'cancel'],
      seller: ['cancel'],
    },
    processing: {
      buyer: ['cancel'],
      seller: ['ship', 'cancel'],
    },
    shipped: {
      buyer: ['confirm_delivery'],
      seller: [],
    },
    delivered: {
      buyer: ['complete'],
      seller: ['complete'],
    },
    completed: {
      buyer: [],
      seller: [],
    },
    cancelled: {
      buyer: [],
      seller: [],
    },
  }

  return actionRules[frontendStatus]?.[userRole]?.includes(action) || false
}
