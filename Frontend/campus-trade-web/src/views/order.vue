<template>
  <v-app>
    <header class="navbar">
      <span class="icon">
        <svg width="24px" height="24px" stroke-width="1.5" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" color="#000000">
          <circle cx="12" cy="12" r="10" stroke="#000000" stroke-width="1.5"></circle>
          <path d="M7.63262 3.06689C8.98567 3.35733 9.99999 4.56025 9.99999 6.00007C9.99999 7.65693 8.65685 9.00007 6.99999 9.00007C5.4512 9.00007 4.17653 7.82641 4.01685 6.31997" stroke="#000000" stroke-width="1.5"></path>
          <path d="M22 13.0505C21.3647 12.4022 20.4793 12 19.5 12C17.567 12 16 13.567 16 15.5C16 17.2632 17.3039 18.7219 19 18.9646" stroke="#000000" stroke-width="1.5"></path>
          <path d="M14.5 8.51L14.51 8.49889" stroke="#000000" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
          <path d="M10 17C11.1046 17 12 16.1046 12 15C12 13.8954 11.1046 13 10 13C8.89543 13 8 13.8954 8 15C8 16.1046 8.89543 17 10 17Z" stroke="#000000" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
        </svg>
      </span>
      <span class="title">Campus Secondhand</span>
      <v-btn
            color="primary"
            variant="outlined"
            prepend-icon="mdi-home"
            style="position: absolute; right: 50px;"
            @click="goToHome"
          >
            返回主页
          </v-btn>
    </header>

    <!-- 左侧导航栏 -->
    <v-navigation-drawer permanent class="mt-14">
      <v-list nav>
        <v-list-item
          v-for="(item, i) in statusItems"
          :key="i"
          :value="item.value"
          :active="activeTab === item.value"
          @click="activeTab = item.value"
          :color="item.color"
          rounded="lg"
          class="mb-2"
        >
          <template v-slot:prepend>
            <v-icon :icon="item.icon"></v-icon>
          </template>
          <v-list-item-title>{{ item.text }}</v-list-item-title>
        </v-list-item>
      </v-list>
    </v-navigation-drawer>

    <!-- 主内容区域 -->
    <v-main class="mt-14">
      <v-container>
        <div class="d-flex align-center mb-6">
          <h1 class="text-h4 mr-4">{{ pageTitle }}</h1>
          
        </div>

        <!-- 订单列表 -->
        <v-list rounded="xl">
          <v-list-item
            v-for="order in filteredOrders"
            :key="order.id"
            @click="showOrderDetails(order)"
            class="mb-4"
          >
            <v-card width="100%" rounded="xl">
              <v-card-item >
                <v-row>
                  <v-col cols="8">
                    <div class="text-subtitle-1">订单号：{{ order.orderNumber }}</div>
                    <div class="text-body-2">下单时间：{{ order.orderDate }}</div>
                  </v-col>
                  <v-col cols="4" class="text-right">
                    <v-chip :color="getStatusColor(order.status)">
                      {{ getStatusText(order.status) }}
                    </v-chip>
                  </v-col>
                </v-row>
              </v-card-item>

              <v-card-text>
                <v-row align="center">
                  <v-col cols="2">
                    <v-img :src="order.productImage" height="80" cover></v-img>
                  </v-col>
                  <v-col cols="6">
                    <div class="text-h6">{{ order.productName }}</div>
                    <div class="text-body-2">{{ order.productDescription }}</div>
                  </v-col>
                  <v-col cols="4" class="text-right">
                    <div class="text-h6">￥{{ order.totalAmount }}</div>
                    <div class="text-body-2">数量：{{ order.quantity }}</div>
                  </v-col>
                </v-row>
              </v-card-text>
            </v-card>
          </v-list-item>
        </v-list>

        <!-- 订单详情对话框 -->
        <v-dialog v-model="showDialog" max-width="700" >
          <v-card v-if="selectedOrder" rounded="xl">
            <v-card-title class="text-h5 pl-10 pt-10" >
              订单详情
              <v-spacer></v-spacer>
              <v-btn
        icon="mdi-close"
        @click="showDialog = false"
        class="position-absolute"
        style="top: 8px; right: 8px;"
        size="small"
      ></v-btn>
            </v-card-title>
            
            <v-card-text>
              <v-list>
                <v-list-item>
                  <div class="text-body-1">订单号：{{ selectedOrder.orderNumber }}</div>
                </v-list-item>
                <v-list-item>
                  <div class="text-body-1">下单时间：{{ selectedOrder.orderDate }}</div>
                </v-list-item>
                <v-list-item>
                  <div class="text-body-1">订单状态：{{ getStatusText(selectedOrder.status) }}</div>
                  <v-btn
        v-if="showActionButton"
        :color="getActionButtonColor"
        class="ml-4"
        size="small"
        @click="handleOrderAction"
      >
        {{ getActionButtonText }}
      </v-btn>
                </v-list-item>
                <v-divider class="my-2"></v-divider>
                <v-list-item>
                  <v-row>
                    <v-col cols="12">
                      <div class="text-h6 mb-2">商品信息</div>
                      <v-row align="center">
                        <v-col cols="2">
                          <v-img :src="selectedOrder.productImage" height="80" cover></v-img>
                        </v-col>
                        <v-col cols="10">
                          <div class="text-h6">{{ selectedOrder.productName }}</div>
                          <div class="text-body-2">{{ selectedOrder.productDescription }}</div>
                          <div class="text-body-1">单价：￥{{ selectedOrder.price }}</div>
                          <div class="text-body-1">数量：{{ selectedOrder.quantity }}</div>
                        </v-col>
                      </v-row>
                    </v-col>
                  </v-row>
                </v-list-item>
                <v-divider class="my-2"></v-divider>
                <v-list-item>
                  <div class="text-h6">收货信息</div>
                </v-list-item>
                <v-list-item>
                  <div class="text-body-1">收货人：{{ selectedOrder.receiverName }}</div>
                </v-list-item>
                <v-list-item>
                  <div class="text-body-1">联系电话：{{ selectedOrder.receiverPhone }}</div>
                </v-list-item>
               
              </v-list>
            </v-card-text>

           
          </v-card>
        </v-dialog>
      </v-container>
    </v-main>
  </v-app>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const activeTab = ref('all')
const showDialog = ref(false)
const selectedOrder = ref(null)

// 状态项配置
const statusItems = [
  {
    text: '全部订单',
    value: 'all',
    icon: 'mdi-format-list-bulleted',
    color: 'grey'
  },
  {
    text: '待付款',
    value: 'pending',
    icon: 'mdi-cash-clock',
    color: 'warning'
  },
  {
    text: '待发货',
    value: 'processing',
    icon: 'mdi-package-variant-closed',
    color: 'info'
  },
  {
    text: '已发货',
    value: 'shipped',
    icon: 'mdi-truck-delivery',
    color: 'primary'
  },
  {
    text: '已完成',
    value: 'completed',
    icon: 'mdi-check-circle',
    color: 'success'
  }
]




// 添加标题计算属性
const pageTitle = computed(() => {
  const titleMap = {
    all: '全部订单',
    pending: '待付款',
    processing: '待发货',
    shipped: '已发货',
    completed: '已完成'
  }
  return titleMap[activeTab.value] || '我的订单'
})



// 模拟订单数据
const orders = ref([
  {
    id: 1,
    orderNumber: 'ORD20240101001',
    orderDate: '2024-01-01 10:00:00',
    status: 'pending',
    productName: 'iPhone 14 Pro',
    productDescription: '全新未拆封，256GB深空黑色',
    productImage: '/path/to/iphone.jpg',
    price: 8999.00,
    quantity: 1,
    totalAmount: 8999.00,
    receiverName: '张三',
    receiverPhone: '13800138000',
    receiverAddress: '北京市朝阳区望京街道'
  },
  {
    id: 2,
    orderNumber: 'ORD20240102002',
    orderDate: '2024-01-02 14:30:00',
    status: 'processing',
    productName: 'MacBook Pro 13英寸',
    productDescription: 'M2芯片，512GB存储，9成新',
    productImage: '/path/to/macbook.jpg',
    price: 12999.00,
    quantity: 1,
    totalAmount: 12999.00,
    receiverName: '李四',
    receiverPhone: '13900139000',
    receiverAddress: '上海市浦东新区陆家嘴金融区'
  },
  {
    id: 3,
    orderNumber: 'ORD20240103003',
    orderDate: '2024-01-03 09:15:00',
    status: 'shipped',
    productName: 'AirPods Pro 2代',
    productDescription: '原装正品，使用3个月',
    productImage: '/path/to/airpods.jpg',
    price: 1299.00,
    quantity: 1,
    totalAmount: 1299.00,
    receiverName: '王五',
    receiverPhone: '13700137000',
    receiverAddress: '广州市天河区珠江新城'
  },
  {
    id: 4,
    orderNumber: 'ORD20240104004',
    orderDate: '2024-01-04 16:45:00',
    status: 'completed',
    productName: '小米13 Ultra',
    productDescription: '徕卡影像，黑色，128GB',
    productImage: '/path/to/xiaomi.jpg',
    price: 4599.00,
    quantity: 1,
    totalAmount: 4599.00,
    receiverName: '赵六',
    receiverPhone: '13600136000',
    receiverAddress: '深圳市南山区科技园'
  }
])

// 根据状态筛选订单
const filteredOrders = computed(() => {
  if (activeTab.value === 'all') return orders.value
  return orders.value.filter(order => order.status === activeTab.value)
})

// 获取状态显示文本
const getStatusText = (status) => {
  const statusMap = {
    pending: '待付款',
    processing: '待发货',
    shipped: '已发货',
    completed: '已完成'
  }
  return statusMap[status] || status
}

// 获取状态颜色
const getStatusColor = (status) => {
  const colorMap = {
    pending: 'warning',
    processing: 'info',
    shipped: 'primary',
    completed: 'success'
  }
  return colorMap[status] || 'grey'
}

// 显示订单详情
const showOrderDetails = (order) => {
  selectedOrder.value = order
  showDialog.value = true
}

// 添加用户角色（模拟，实际应从用户登录信息获取）
const currentUserRole = ref('seller') // 'seller' 或 'buyer'

// 判断是否显示操作按钮
const showActionButton = computed(() => {
  if (!selectedOrder.value) return false
  
  const status = selectedOrder.value.status
  if (currentUserRole.value === 'seller') {
    return status === 'processing' // 卖家且订单待发货时显示
  } else {
    return status === 'pending' || status === 'shipped' // 买家且订单待付款或已发货时显示
  }
})

// 获取按钮文本
const getActionButtonText = computed(() => {
  if (!selectedOrder.value) return ''

  const status = selectedOrder.value.status
  if (currentUserRole.value === 'seller') {
    return status === 'processing' ? '发货' : ''
  } else {
    switch (status) {
      case 'pending': return '付款'
      case 'shipped': return '确认收货'
      default: return ''
    }
  }
})

// 获取按钮颜色
const getActionButtonColor = computed(() => {
  if (!selectedOrder.value) return 'primary'

  const status = selectedOrder.value.status
  if (currentUserRole.value === 'seller') {
    return status === 'processing' ? 'success' : 'primary'
  } else {
    switch (status) {
      case 'pending': return 'warning'
      case 'shipped': return 'success'
      default: return 'primary'
    }
  }
})

// 处理订单操作
const handleOrderAction = async () => {
  if (!selectedOrder.value) return

  const status = selectedOrder.value.status
  try {
    if (currentUserRole.value === 'seller') {
      if (status === 'processing') {
        // 卖家发货操作
        await shipOrder(selectedOrder.value.id)
        selectedOrder.value.status = 'shipped'
      }
    } else {
      switch (status) {
        case 'pending':
          // 买家付款操作
          await payOrder(selectedOrder.value.id)
          selectedOrder.value.status = 'processing'
          break
        case 'shipped':
          // 买家确认收货操作
          await confirmReceived(selectedOrder.value.id)
          selectedOrder.value.status = 'completed'
          break
      }
    }
  } catch (error) {
    console.error('操作失败:', error)
    // 这里可以添加错误提示
  }
}

// 模拟 API 调用
const shipOrder = async (orderId) => {
  // 实际项目中这里需要调用后端 API
  console.log('发货操作:', orderId)
}

const payOrder = async (orderId) => {
  console.log('付款操作:', orderId)
}

const confirmReceived = async (orderId) => {
  console.log('确认收货操作:', orderId)
}

// 返回主页
const goToHome = () => {
  router.push('/')
}
</script>

<style scoped>
.navbar {
  height: 56px;
  border-bottom: 1px solid #e0e0e0;
  display: flex;
  align-items: center;
  padding-left: 32px;
  background: #fff;
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 100;
}

.icon {
  font-size: 25px;
  color: #222;
  margin-right: 10px;
}

.title {
  font-weight: 600;
  font-size: 20px;
  letter-spacing: 0.5px;
}

.v-container {
  max-width: 1200px;  
  padding: 24px;      
  margin-left: 50px;         
}

.v-navigation-drawer {
  top: 0;
  height: calc(100vh - 56px) !important;
  z-index: 99;
}

.v-main {
  margin-left: 2px;
  min-height: calc(100vh - 56px);
  background-color: #f5f5f5;
}

.v-list-item {
  margin: 4px 8px;
  border-radius: 8px;
}

.v-list-item--active {
  background-color: rgba(var(--v-theme-primary), 0.1);
}

.v-list-item:hover {
  background-color: rgba(var(--v-theme-primary), 0.05);
}

/* 添加定位样式 */
.position-relative {
  position: relative;
}

.position-absolute {
  position: absolute;
}
</style>




