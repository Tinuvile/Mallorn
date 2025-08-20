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
                      {{ getStatusText(order.status, getUserRoleInOrder(order)) }}
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
                    <!-- 订单卡片操作按钮 -->
                    <div class="mt-2" v-if="shouldShowCardButton(order)">
                      <v-btn
                        :color="getCardButtonColor(order)"
                        size="small"
                        @click.stop="handleCardAction(order)"
                      >
                        {{ getCardButtonText(order) }}
                      </v-btn>
                    </div>
                  </v-col>
                </v-row>
              </v-card-text>
            </v-card>
          </v-list-item>
        </v-list>

        <!-- 订单详情对话框 -->
        <v-dialog v-model="showDialog" max-width="900" >
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
              <!-- 订单进度条 -->
              <div class="mb-6">
                <v-stepper 
                  :model-value="currentStep" 
                  :items="stepperItems"
                  alt-labels
                  class="elevation-0 custom-stepper"
                  hide-actions
                >
                
                  <template v-slot:item.1>
                    <v-card flat class="text-center">
                      <div class="text-subtitle-1 mb-2">{{ getStepTitle(1) }}</div>
                      <div v-if="shouldShowStepButton(1)" class="mt-2">
                        <v-btn 
                          :color="getStepButtonColor(1)"
                          @click="handleStepAction(1)"
                          size="small"
                        >
                          {{ getStepButtonText(1) }}
                        </v-btn>
                      </div>
                    </v-card>
                  </template>

                  <template v-slot:item.2>
                    <v-card flat class="text-center">
                      <div class="text-subtitle-1 mb-2">{{ getStepTitle(2) }}</div>
                      <div v-if="shouldShowStepButton(2)" class="mt-2">
                        <v-btn 
                          :color="getStepButtonColor(2)"
                          @click="handleStepAction(2)"
                          size="small"
                        >
                          {{ getStepButtonText(2) }}
                        </v-btn>
                      </div>
                    </v-card>
                  </template>

                  <template v-slot:item.3>
                    <v-card flat class="text-center">
                      <div class="text-subtitle-1 mb-2">{{ getStepTitle(3) }}</div>
                      <div v-if="shouldShowStepButton(3)" class="mt-2">
                        <v-btn 
                          :color="getStepButtonColor(3)"
                          @click="handleStepAction(3)"
                          size="small"
                        >
                          {{ getStepButtonText(3) }}
                        </v-btn>
                      </div>
                    </v-card>
                  </template>

                  <template v-slot:item.4>
                    <v-card flat class="text-center">
                      <div class="text-subtitle-1 mb-2">{{ getStepTitle(4) }}</div>
                      <div v-if="shouldShowStepButton(4)" class="mt-2">
                        <v-btn 
                          :color="getStepButtonColor(4)"
                          @click="handleStepAction(4)"
                          size="small"
                        >
                          {{ getStepButtonText(4) }}
                        </v-btn>
                      </div>
                      <!-- 评价显示区域 -->
                      <div v-if="currentUserRoleInOrder === 'seller' && currentStep >= 4" class="mt-2">
                        <div v-if="selectedOrder.review" class="text-body-2">
                          买家评价：{{ selectedOrder.review }}
                        </div>
                        <div v-else class="text-body-2 text-grey">
                          买家暂未评价
                        </div>
                      </div>
                    </v-card>
                  </template>
                </v-stepper>
              </div>

              <v-divider class="my-4"></v-divider>

              <v-list>
                <v-list-item>
                  <div class="text-body-1">订单号：{{ selectedOrder.orderNumber }}</div>
                </v-list-item>
                <v-list-item>
                  <div class="text-body-1">下单时间：{{ selectedOrder.orderDate }}</div>
                </v-list-item>
                <v-list-item>
                  <div class="text-body-1 d-flex justify-space-between align-center">
                    <span>订单状态：{{ getStatusText(selectedOrder.status, currentUserRoleInOrder) }}</span>
                    <!-- 订单状态行的操作按钮 -->
                    <v-btn
                      v-if="shouldShowDetailButton"
                      :color="getDetailButtonColor"
                      size="small"
                      @click="handleDetailAction"
                    >
                      {{ getDetailButtonText }}
                    </v-btn>
                  </div>
                </v-list-item>
                <v-list-item>
                  <div class="text-body-1">我的身份：{{ currentUserRoleInOrder === 'buyer' ? '买家' : '卖家' }}</div>
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

        <!-- 评价对话框 -->
        <v-dialog v-model="showReviewDialogState" max-width="600">
          <v-card rounded="xl">
            <v-card-title class="text-h5 pl-6 pt-6">
              {{ isViewingReview ? '查看评价' : '商品评价' }}
              <v-spacer></v-spacer>
              <v-btn
                icon="mdi-close"
                @click="closeReviewDialog"
                class="position-absolute"
                style="top: 8px; right: 8px;"
                size="small"
              ></v-btn>
            </v-card-title>
            
            <v-card-text>
              <!-- 订单信息 -->
              <div v-if="selectedOrder" class="mb-4">
                <v-row align="center">
                  <v-col cols="3">
                    <v-img :src="selectedOrder.productImage" height="60" cover></v-img>
                  </v-col>
                  <v-col cols="9">
                    <div class="text-h6">{{ selectedOrder.productName }}</div>
                    <div class="text-body-2">{{ selectedOrder.productDescription }}</div>
                  </v-col>
                </v-row>
              </div>

              <v-divider class="mb-4"></v-divider>

              <!-- 查看模式 -->
              <div v-if="isViewingReview">
                <div class="text-h6 mb-3">买家评价：</div>
                <v-card variant="outlined" class="pa-4">
                  <div class="text-body-1">{{ selectedOrder?.review || '暂无评价' }}</div>
                </v-card>
              </div>

              <!-- 编辑模式 -->
              <div v-else>
                <div class="text-h6 mb-3">请对此商品进行评价：</div>
                <v-textarea
                  v-model="reviewText"
                  label="请输入您的评价"
                  placeholder="分享您对商品的使用感受..."
                  rows="4"
                  variant="outlined"
                  counter="500"
                  maxlength="500"
                  :rules="reviewRules"
                ></v-textarea>
              </div>
            </v-card-text>

            <v-card-actions class="px-6 pb-6">
              <v-spacer></v-spacer>
              <v-btn
                color="grey"
                variant="outlined"
                @click="closeReviewDialog"
              >
                {{ isViewingReview ? '关闭' : '取消' }}
              </v-btn>
              <v-btn
                v-if="!isViewingReview"
                color="primary"
                @click="submitReview"
                :disabled="!reviewText.trim()"
                :loading="isSubmittingReview"
              >
                发布评价
              </v-btn>
            </v-card-actions>
          </v-card>
        </v-dialog>
      </v-container>
    </v-main>

    <!-- 交互式小宠物 -->
    <div class="pet-container" ref="petContainer">
      <div class="pet" ref="pet" @mouseenter="onPetHover" @mouseleave="onPetLeave">
        <!-- 猫咪身体 -->
        <div class="pet-body">
          <!-- 猫耳朵 -->
          <div class="ear ear-left"></div>
          <div class="ear ear-right"></div>
          
          <!-- 猫脸 -->
          <div class="face">
            <!-- 眼睛 -->
            <div class="eye eye-left">
              <div class="pupil" ref="leftPupil"></div>
            </div>
            <div class="eye eye-right">
              <div class="pupil" ref="rightPupil"></div>
            </div>
            
            <!-- 鼻子 -->
            <div class="nose"></div>
            
            <!-- 嘴巴 -->
            <div class="mouth"></div>
          </div>
          
          <!-- 猫爪 -->
          <div class="paw paw-left"></div>
          <div class="paw paw-right"></div>
        </div>
        
        <!-- 尾巴 -->
        <div class="tail"></div>
        
        <!-- 爱心气泡（hover时显示） -->
        <div class="love-bubble" ref="loveBubble">
          <span>💕</span>
        </div>
      </div>
    </div>
  </v-app>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const activeTab = ref('all')
const showDialog = ref(false)
const selectedOrder = ref(null)

// 小宠物相关的引用
const petContainer = ref(null)
const pet = ref(null)
const leftPupil = ref(null)
const rightPupil = ref(null)
const loveBubble = ref(null)

// 评价相关变量
const showReviewDialogState = ref(false)
const reviewText = ref('')
const isViewingReview = ref(false)
const isSubmittingReview = ref(false)

// 评价输入验证规则
const reviewRules = [
  v => !!v || '请输入评价内容',
  v => (v && v.length >= 5) || '评价内容至少需要5个字符',
  v => (v && v.length <= 500) || '评价内容不能超过500个字符'
]

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
    color: 'orange'
  },
  {
    text: '待收货',
    value: 'delivered',
    icon: 'mdi-package-variant',
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
    delivered: '待收货',
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
    review: null // 买家评价
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
    review: null
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
    review: null
  },
  {
    id: 4,
    orderNumber: 'ORD20240104004',
    orderDate: '2024-01-04 16:45:00',
    status: 'delivered',
    productName: '华为Watch GT 3',
    productDescription: '运动智能手表，黑色表带',
    productImage: '/path/to/huawei-watch.jpg',
    price: 899.00,
    quantity: 1,
    totalAmount: 899.00,
    receiverName: '赵六',
    receiverPhone: '13600136000',
    review: null
  },
  {
    id: 5,
    orderNumber: 'ORD20240105005',
    orderDate: '2024-01-05 11:20:00',
    status: 'completed',
    productName: '小米13 Ultra',
    productDescription: '徕卡影像，黑色，128GB',
    productImage: '/path/to/xiaomi.jpg',
    price: 4599.00,
    quantity: 1,
    totalAmount: 4599.00,
    receiverName: '钱七',
    receiverPhone: '13500135000',
    review: '商品质量很好，卖家发货很快！'
  }
])

// 根据状态筛选订单
const filteredOrders = computed(() => {
  if (activeTab.value === 'all') return orders.value
  return orders.value.filter(order => order.status === activeTab.value)
})

// 获取状态显示文本
const getStatusText = (status, userRole = null) => {
  // 如果没有指定用户角色，使用通用的状态文本
  if (!userRole) {
    const statusMap = {
      pending: '待付款',
      processing: '待发货',
      shipped: '已发货',
      delivered: '待收货',
      completed: '已完成'
    }
    return statusMap[status] || status
  }

  // 根据用户角色显示不同的状态文本
  if (userRole === 'seller') {
    const sellerStatusMap = {
      pending: '待付款',
      processing: '待发货',
      shipped: '待收货',  // 卖家视角：已发货后是待收货
      delivered: '待收货',
      completed: '已完成'
    }
    return sellerStatusMap[status] || status
  } else {
    const buyerStatusMap = {
      pending: '待付款',
      processing: '待发货',
      shipped: '已发货',  // 买家视角：已发货
      delivered: '待收货',
      completed: '已完成'
    }
    return buyerStatusMap[status] || status
  }
}

// 获取状态颜色
const getStatusColor = (status) => {
  const colorMap = {
    pending: 'warning',
    processing: 'info',
    shipped: 'orange',
    delivered: 'primary',
    completed: 'success'
  }
  return colorMap[status] || 'grey'
}

// 显示订单详情
const showOrderDetails = (order) => {
  selectedOrder.value = order
  showDialog.value = true
}

// 计算当前用户在选中订单中的身份
const currentUserRoleInOrder = computed(() => {
  if (!selectedOrder.value) return 'buyer'
  return getUserRoleInOrder(selectedOrder.value)
})

// 计算进度条步骤
const stepperItems = computed(() => {
  if (!selectedOrder.value) return []
  
  if (currentUserRoleInOrder.value === 'buyer') {
    return ['待付款', '待发货', '已发货', '已完成']
  } else {
    return ['待发货', '待收货', '已完成']
  }
})

// 计算当前步骤
const currentStep = computed(() => {
  if (!selectedOrder.value) return 1
  
  const status = selectedOrder.value.status
  const role = currentUserRoleInOrder.value
  
  if (role === 'buyer') {
    switch (status) {
      case 'pending': return 1
      case 'processing': return 2
      case 'shipped': return 3
      case 'delivered': return 3
      case 'completed': return 4
      default: return 1
    }
  } else {
    switch (status) {
      case 'pending': return 1
      case 'processing': return 1
      case 'shipped': return 2
      case 'delivered': return 2
      case 'completed': return 3
      default: return 1
    }
  }
})

// 获取步骤标题
const getStepTitle = (step) => {
  if (!selectedOrder.value) return ''
  
  const role = currentUserRoleInOrder.value
  if (role === 'buyer') {
    const titles = ['待付款', '待发货', '已发货', '已完成']
    return titles[step - 1] || ''
  } else {
    const titles = ['待发货', '待收货', '已完成']
    return titles[step - 1] || ''
  }
}

// 判断是否显示步骤按钮
const shouldShowStepButton = (step) => {
  if (!selectedOrder.value) return false
  
  const currentStepValue = currentStep.value
  const role = currentUserRoleInOrder.value
  const status = selectedOrder.value.status
  
  if (role === 'buyer') {
    // 买家流程：待付款-待发货-已发货-已完成
    if (step === 1 && currentStepValue === 1 && status === 'pending') return true
    if (step === 3 && currentStepValue === 3 && (status === 'shipped' || status === 'delivered')) return true
    if (step === 4 && currentStepValue === 4 && status === 'completed') return true
  } else {
    // 卖家流程：待发货-待收货-已完成
    if (step === 1 && currentStepValue === 1 && status === 'processing') return true
    if (step === 3 && currentStepValue === 3 && status === 'completed') return true
  }
  
  return false
}

// 获取步骤按钮文本
const getStepButtonText = (step) => {
  if (!selectedOrder.value) return ''
  
  const role = currentUserRoleInOrder.value
  const status = selectedOrder.value.status
  
  if (role === 'buyer') {
    if (step === 1 && status === 'pending') return '付款'
    if (step === 3 && (status === 'shipped' || status === 'delivered')) return '确认收货'
    if (step === 4 && status === 'completed') {
      return selectedOrder.value.review ? '查看评价' : '评价'
    }
  } else {
    if (step === 1 && status === 'processing') return '发货'
    if (step === 3 && status === 'completed') return '查看评价'
  }
  
  return ''
}

// 获取步骤按钮颜色
const getStepButtonColor = (step) => {
  const role = currentUserRoleInOrder.value
  const status = selectedOrder.value?.status
  
  if (role === 'buyer') {
    if (step === 1 && status === 'pending') return 'warning'
    if (step === 3 && (status === 'shipped' || status === 'delivered')) return 'success'
    if (step === 4 && status === 'completed') return 'primary'
  } else {
    if (step === 1 && status === 'processing') return 'success'
    if (step === 3 && status === 'completed') return 'primary'
  }
  
  return 'primary'
}

// 处理步骤操作
const handleStepAction = async (step) => {
  if (!selectedOrder.value) return
  
  const role = currentUserRoleInOrder.value
  const status = selectedOrder.value.status
  
  try {
    if (role === 'buyer') {
      if (step === 1 && status === 'pending') {
        // 买家付款
        await payOrder(selectedOrder.value.id)
        updateOrderStatus(selectedOrder.value.id, 'processing')
      } else if (step === 3 && (status === 'shipped' || status === 'delivered')) {
        // 买家确认收货
        await confirmReceived(selectedOrder.value.id)
        updateOrderStatus(selectedOrder.value.id, 'completed')
      } else if (step === 4 && status === 'completed') {
        // 买家评价或查看评价
        if (selectedOrder.value.review) {
          await showReviewDetails()
        } else {
          await showReviewDialog()
        }
      }
    } else {
      if (step === 1 && status === 'processing') {
        // 卖家发货
        await shipOrder(selectedOrder.value.id)
        updateOrderStatus(selectedOrder.value.id, 'shipped')
      } else if (step === 3 && status === 'completed') {
        // 卖家查看评价
        await showReviewDetails()
      }
    }
  } catch (error) {
    console.error('操作失败:', error)
  }
}

// 更新订单状态
const updateOrderStatus = (orderId, newStatus) => {
  // 更新订单列表中的状态
  const orderIndex = orders.value.findIndex(order => order.id === orderId)
  if (orderIndex !== -1) {
    orders.value[orderIndex].status = newStatus
  }
  // 更新选中订单的状态
  if (selectedOrder.value && selectedOrder.value.id === orderId) {
    selectedOrder.value.status = newStatus
  }
}

// 显示评价对话框
const showReviewDialog = () => {
  reviewText.value = ''
  isViewingReview.value = false
  showReviewDialogState.value = true
}

// 显示评价详情
const showReviewDetails = () => {
  isViewingReview.value = true
  showReviewDialogState.value = true
}

// 关闭评价对话框
const closeReviewDialog = () => {
  showReviewDialogState.value = false
  reviewText.value = ''
  isViewingReview.value = false
  isSubmittingReview.value = false
}

// 提交评价
const submitReview = async () => {
  if (!reviewText.value.trim() || !selectedOrder.value) return
  
  isSubmittingReview.value = true
  try {
    // 模拟API调用
    await new Promise(resolve => setTimeout(resolve, 1000))
    
    // 更新订单的评价信息
    const orderIndex = orders.value.findIndex(order => order.id === selectedOrder.value.id)
    if (orderIndex !== -1) {
      orders.value[orderIndex].review = reviewText.value
    }
    
    // 更新选中订单的评价
    if (selectedOrder.value) {
      selectedOrder.value.review = reviewText.value
    }
    
    // 关闭对话框
    closeReviewDialog()
    
    console.log('评价提交成功:', reviewText.value)
  } catch (error) {
    console.error('提交评价失败:', error)
  } finally {
    isSubmittingReview.value = false
  }
}

// 订单详情页面按钮相关逻辑
const shouldShowDetailButton = computed(() => {
  if (!selectedOrder.value) return false
  
  const status = selectedOrder.value.status
  const role = currentUserRoleInOrder.value
  
  if (role === 'seller') {
    return status === 'processing' || status === 'completed'
  } else {
    return status === 'pending' || status === 'shipped' || status === 'delivered' || status === 'completed'
  }
})

const getDetailButtonText = computed(() => {
  if (!selectedOrder.value) return ''
  
  const status = selectedOrder.value.status
  const role = currentUserRoleInOrder.value
  
  if (role === 'seller') {
    switch (status) {
      case 'processing': return '发货'
      case 'completed': return '查看评价'
      default: return ''
    }
  } else {
    switch (status) {
      case 'pending': return '付款'
      case 'shipped': return '确认收货'
      case 'delivered': return '确认收货'
      case 'completed': return selectedOrder.value.review ? '查看评价' : '评价'
      default: return ''
    }
  }
})

const getDetailButtonColor = computed(() => {
  if (!selectedOrder.value) return 'primary'
  
  const status = selectedOrder.value.status
  const role = currentUserRoleInOrder.value
  
  if (role === 'seller') {
    switch (status) {
      case 'processing': return 'success'
      case 'completed': return 'primary'
      default: return 'primary'
    }
  } else {
    switch (status) {
      case 'pending': return 'warning'
      case 'shipped': return 'success'
      case 'delivered': return 'success'
      case 'completed': return 'primary'
      default: return 'primary'
    }
  }
})

const handleDetailAction = async () => {
  if (!selectedOrder.value) return
  
  const status = selectedOrder.value.status
  const role = currentUserRoleInOrder.value
  
  try {
    if (role === 'seller') {
      switch (status) {
        case 'processing':
          // 卖家发货操作
          await shipOrder(selectedOrder.value.id)
          updateOrderStatus(selectedOrder.value.id, 'shipped')
          break
        case 'completed':
          // 卖家查看评价
          await showReviewDetails()
          break
      }
    } else {
      switch (status) {
        case 'pending':
          // 买家付款操作
          await payOrder(selectedOrder.value.id)
          updateOrderStatus(selectedOrder.value.id, 'processing')
          break
        case 'shipped':
        case 'delivered':
          // 买家确认收货操作
          await confirmReceived(selectedOrder.value.id)
          updateOrderStatus(selectedOrder.value.id, 'completed')
          break
        case 'completed':
          // 买家评价或查看评价
          if (selectedOrder.value.review) {
            await showReviewDetails()
          } else {
            await showReviewDialog()
          }
          break
      }
    }
  } catch (error) {
    console.error('操作失败:', error)
  }
}

// 添加用户角色（模拟，实际应从用户登录信息获取）
const currentUserRole = ref('buyer') // 'buyer' 或 'seller'

// 根据订单和用户角色获取用户在该订单中的身份
const getUserRoleInOrder = (order) => {
  // 这里应该根据订单的买家ID和卖家ID以及当前用户ID来判断
  // 现在先模拟：奇数订单ID为买家，偶数订单ID为卖家
  return order.id % 2 === 1 ? 'buyer' : 'seller'
}

// 判断是否显示操作按钮（已移除，现在使用stepper中的按钮）

// 模拟 API 调用
const shipOrder = async (orderId) => {
  // 实际项目中这里需要调用后端 API
  console.log('发货操作:', orderId)
  // 模拟异步延迟
  await new Promise(resolve => setTimeout(resolve, 1000))
}

const payOrder = async (orderId) => {
  console.log('付款操作:', orderId)
  // 这里可以集成支付宝沙盒
  await new Promise(resolve => setTimeout(resolve, 1000))
}

const confirmReceived = async (orderId) => {
  console.log('确认收货操作:', orderId)
  await new Promise(resolve => setTimeout(resolve, 1000))
}

// 返回主页
const goToHome = () => {
  router.push('/')
}

// 判断是否在卡片上显示操作按钮
const shouldShowCardButton = (order) => {
  const status = order.status
  const role = getUserRoleInOrder(order)
  
  if (role === 'seller') {
    return status === 'processing' // 卖家：待发货时显示发货按钮
  } else {
    return status === 'pending' || status === 'shipped' || status === 'delivered' // 买家：待付款、已发货或待收货时显示按钮
  }
}

// 获取卡片按钮文本
const getCardButtonText = (order) => {
  const status = order.status
  const role = getUserRoleInOrder(order)
  
  if (role === 'seller') {
    return status === 'processing' ? '发货' : ''
  } else {
    switch (status) {
      case 'pending': return '付款'
      case 'shipped': return '确认收货'
      case 'delivered': return '确认收货'
      default: return ''
    }
  }
}

// 获取卡片按钮颜色
const getCardButtonColor = (order) => {
  const status = order.status
  const role = getUserRoleInOrder(order)
  
  if (role === 'seller') {
    return status === 'processing' ? 'success' : 'primary'
  } else {
    switch (status) {
      case 'pending': return 'warning'
      case 'shipped': return 'success'
      case 'delivered': return 'success'
      default: return 'primary'
    }
  }
}

// 处理卡片按钮点击事件
const handleCardAction = async (order) => {
  const status = order.status
  const role = getUserRoleInOrder(order)
  
  try {
    if (role === 'seller') {
      if (status === 'processing') {
        // 卖家发货操作
        await shipOrder(order.id)
        updateOrderStatus(order.id, 'shipped')
      }
    } else {
      switch (status) {
        case 'pending':
          // 买家付款操作
          await payOrder(order.id)
          updateOrderStatus(order.id, 'processing')
          break
        case 'shipped':
        case 'delivered':
          // 买家确认收货操作
          await confirmReceived(order.id)
          updateOrderStatus(order.id, 'completed')
          break
      }
    }
  } catch (error) {
    console.error('操作失败:', error)
  }
}

// 小宠物交互逻辑
const trackMouse = (event) => {
  if (!leftPupil.value || !rightPupil.value || !pet.value) return
  
  const petRect = pet.value.getBoundingClientRect()
  const petCenterX = petRect.left + petRect.width / 2
  const petCenterY = petRect.top + petRect.height / 2
  
  // 计算鼠标相对于小宠物的角度
  const deltaX = event.clientX - petCenterX
  const deltaY = event.clientY - petCenterY
  const angle = Math.atan2(deltaY, deltaX)
  
  // 限制瞳孔移动范围
  const maxDistance = 3
  const distance = Math.min(maxDistance, Math.sqrt(deltaX * deltaX + deltaY * deltaY) / 20)
  
  const pupilX = Math.cos(angle) * distance
  const pupilY = Math.sin(angle) * distance
  
  // 移动瞳孔
  leftPupil.value.style.transform = `translate(${pupilX}px, ${pupilY}px)`
  rightPupil.value.style.transform = `translate(${pupilX}px, ${pupilY}px)`
}

const onPetHover = () => {
  if (loveBubble.value) {
    loveBubble.value.style.opacity = '1'
    loveBubble.value.style.transform = 'translateY(-10px) scale(1)'
  }
}

const onPetLeave = () => {
  if (loveBubble.value) {
    loveBubble.value.style.opacity = '0'
    loveBubble.value.style.transform = 'translateY(0) scale(0.8)'
  }
}

// 组件挂载和卸载时的事件监听
onMounted(() => {
  document.addEventListener('mousemove', trackMouse)
})

onUnmounted(() => {
  document.removeEventListener('mousemove', trackMouse)
})
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

/* 隐藏 stepper 的导航按钮 */
.custom-stepper :deep(.v-stepper__actions) {
  display: none !important;
}

/* 隐藏 NEXT/PREVIOUS 按钮 */
.custom-stepper :deep(.v-btn[data-v-stepper-prev]),
.custom-stepper :deep(.v-btn[data-v-stepper-next]) {
  display: none !important;
}

/* 隐藏步骤底部的状态文字 */
.custom-stepper :deep(.v-stepper-window__container) {
  display: none !important;
}

/* 隐藏步骤内容区域 */
.custom-stepper :deep(.v-stepper-window) {
  display: none !important;
}

/* 隐藏可能存在的步骤标签文字 */
.custom-stepper :deep(.v-stepper-item__subtitle),
.custom-stepper :deep(.v-stepper__step__step) + .v-stepper__label {
  display: none !important;
}

/* 添加定位样式 */
.position-relative {
  position: relative;
}

.position-absolute {
  position: absolute;
}

/* 小宠物样式 */
.pet-container {
  position: fixed;
  bottom: 20px;
  left: 80px;
  z-index: 9999;
  pointer-events: none;
}

.pet {
  position: relative;
  width: 80px;
  height: 80px;
  cursor: pointer;
  pointer-events: all;
  transition: transform 0.3s ease;

}

.pet:hover {
  transform: scale(1.1);
}

.pet-body {
  position: relative;
  width: 60px;
  height: 60px;
  background: linear-gradient(135deg, #f8b4b4 0%, rgb(255, 158, 158) 100%);
  border-radius: 50%;
  margin: 10px;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);
}

/* 猫耳朵 */
.ear {
  position: absolute;
  width: 20px;
  height: 20px;
  background: linear-gradient(135deg, #ffc5c5 0%, rgb(255, 188, 188) 100%);
  border-radius: 50% 50% 50% 0;
  top: -8px;
}

.ear-left {
  left: 8px;
  transform: rotate(-45deg);
}

.ear-right {
  right: 8px;
  transform: rotate(45deg);
}

.ear::after {
  content: '';
  position: absolute;
  width: 12px;
  height: 12px;
  background: #bb878b;
  border-radius: 50% 50% 50% 0;
  top: 4px;
  left: 4px;
}

/* 脸部 */
.face {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 40px;
  height: 40px;
}

/* 眼睛 */
.eye {
  position: absolute;
  width: 12px;
  height: 12px;
  background: white;
  border-radius: 50%;
  top: 8px;
  border: 2px solid #2c2c2c;
}

.eye-left {
  left: 6px;
}

.eye-right {
  right: 6px;
}

.pupil {
  position: absolute;
  width: 6px;
  height: 6px;
  background: #2c2c2c;
  border-radius: 50%;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  transition: transform 0.1s ease;
}

/* 鼻子 */
.nose {
  position: absolute;
  width: 6px;
  height: 4px;
  background: #ff4757;
  border-radius: 50%;
  top: 18px;
  left: 50%;
  transform: translateX(-50%);
}

/* 嘴巴 */
.mouth {
  position: absolute;
  top: 24px;
  left: 50%;
  transform: translateX(-50%);
}

.mouth::before,
.mouth::after {
  content: '';
  position: absolute;
  width: 8px;
  height: 4px;
  border: 2px solid #2c2c2c;
  border-top: none;
  border-radius: 0 0 8px 8px;
}

.mouth::before {
  left: -6px;
  transform: rotate(-20deg);
}

.mouth::after {
  right: -6px;
  transform: rotate(20deg);
}

/* 猫爪 */
.paw {
  position: absolute;
  width: 12px;
  height: 8px;
  background: #ff9a56;
  border-radius: 50%;
  bottom: -4px;
}

.paw-left {
  left: 12px;
}

.paw-right {
  right: 12px;
}

/* 尾巴 */
.tail {
  position: absolute;
  width: 30px;
  height: 8px;
  background: linear-gradient(90deg, #ffc2db 0%, #ffb0b5 100%);
  border-radius: 4px;
  right: -25px;
  top: 30px;
  transform: rotate(20deg);
  animation: tailWag 2s ease-in-out infinite;
}

@keyframes tailWag {
  0%, 100% { transform: rotate(20deg); }
  50% { transform: rotate(40deg); }
}

/* 爱心气泡 */
.love-bubble {
  position: absolute;
  top: -30px;
  left: 50%;
  transform: translateX(-50%) translateY(0) scale(0.8);
  opacity: 0;
  transition: all 0.3s ease;
  font-size: 20px;
  animation: float 2s ease-in-out infinite;
}

@keyframes float {
  0%, 100% { transform: translateX(-50%) translateY(0) scale(0.8); }
  50% { transform: translateX(-50%) translateY(-5px) scale(1); }
}

/* 添加眨眼动画 */
.eye {
  animation: blink 3s infinite;
}

@keyframes blink {
  0%, 90%, 100% { height: 12px; }
  95% { height: 2px; }
}

/* 响应式调整 */
@media (max-width: 768px) {
  .pet-container {
    bottom: 10px;
    left: 10px;
  }
  
  .pet {
    width: 60px;
    height: 60px;
  }
  
  .pet-body {
    width: 45px;
    height: 45px;
    margin: 7.5px;
  }
}
</style>