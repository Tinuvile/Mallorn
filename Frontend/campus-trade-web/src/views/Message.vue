<template>
  <v-app id="message-app">
    <!-- 顶部导航栏 -->
    <v-app-bar color="#BBDEFB" height="72" dark>
      <v-btn icon @click="goBack" class="mx-2">
        <v-icon size="36">mdi-arrow-left</v-icon>
      </v-btn>
      <span class="title" style="font-size: 24px;">消息中心</span>
      <v-spacer></v-spacer>
    </v-app-bar>

       <!-- 消息列表区域 -->
    <v-main style="background-color: #f5f5f5; padding: 0px;">
      <!-- 左侧导航栏 -->
      <v-navigation-drawer permanent>
        <v-list nav>  <!-- 导航列表 -->
          <v-list-item
            v-for="(category, i) in categories"
            :key="i"
            :value="category.value"
            :active="currentCategory === category.value" 
            @click="handleCategoryChange(category.value)" 
            :color="category.color"  
            rounded="lg"  
            class="mb-2" 
          >
            <template v-slot:prepend> 
              <v-icon :icon="category.icon"></v-icon>
            </template>
            <v-list-item-title>{{ category.label }}</v-list-item-title>
          </v-list-item>
        </v-list>
      </v-navigation-drawer>

      <!-- 右侧消息列表 -->
      <v-main>
        <v-container>
          <!-- 加载状态 -->
          <div v-if="loading" class="text-center py-4">
            <v-progress-circular indeterminate color="primary"></v-progress-circular>
            <p class="mt-2">正在加载消息...</p>
          </div>
          
          <!-- 消息列表 -->
          <v-list v-else style="background-color: white; border-radius: 0;" rounded="lg">
            <!-- 空状态提示 -->
            <div v-if="filteredMessages.length === 0" class="text-center py-8">
              <v-icon size="48" color="grey">mdi-message-outline</v-icon>
              <p class="text-grey mt-2">暂无消息</p>
            </div>
            
            <!-- 使用过滤后的消息列表 -->
            <v-list-item 
              v-for="(message, index) in filteredMessages" 
              :key="index"
              class="message-item"
              :class="{ 'unread': !message.read }"
              @click="handleMessageClick(message)"
            >
              <!-- 消息内容 -->
              <v-list-item-content class="pl-4">
                <div class="d-flex justify-space-between align-center">
                  <v-list-item-title class="font-weight-bold">{{ message.sender }}</v-list-item-title>
                  <span class="text-caption text-secondary">{{ message.time }}</span>
                </div>
                <v-list-item-subtitle class="text-truncate">{{ message.content }}</v-list-item-subtitle>
              </v-list-item-content>
              
              <!-- 未读标识 -->
              <v-list-item-action v-if="!message.read">
                <v-badge color="primary" dot></v-badge>
              </v-list-item-action>
            </v-list-item>
          </v-list>
        </v-container>
      </v-main>
    </v-main>

    <!--消息详情浮窗 -->
    <v-dialog 
      v-model="showMessageDetail" 
      max-width="600px"
      persistent
    >
      <v-card>
        <v-card-title class="d-flex justify-space-between align-center">
          <span class="text-h5">{{ currentMessage.sender }} 的消息</span>
          <v-btn icon @click="showMessageDetail = false">
            <v-icon>mdi-close</v-icon>
          </v-btn>
        </v-card-title>
        <v-card-text>
          <div class="text-caption text-secondary mb-3">{{ currentMessage.time }}</div>
          <p class="text-body-1">{{ currentMessage.content }}</p>
        </v-card-text>
      </v-card>
    </v-dialog>

     <!-- 议价消息特殊弹窗 -->
  <v-dialog 
    v-model="showBargainDialog" 
    max-width="700px"
    persistent
  >
    <v-card>
      <v-card-title class="d-flex justify-space-between align-center">
        <span class="text-h5">{{ currentBargainMessage.sender }} 的议价请求</span>
        <v-btn icon @click="showBargainDialog = false">
          <v-icon>mdi-close</v-icon>
        </v-btn>
      </v-card-title>
      
      <v-card-text>
        <!-- 商品信息 -->
        <v-row align="center" class="mb-4">
          <v-col cols="3">
            <v-img :src="currentBargainMessage.productImage" height="80" cover></v-img>
          </v-col>
          <v-col cols="9">
            <div class="text-h6">{{ currentBargainMessage.productName }}</div>
          </v-col>
        </v-row>

        <!-- 报价信息 -->
        <div class="mb-4">
          <p class="text-body-2">您的原始报价：￥{{ currentBargainMessage.myOffer }}</p>
          <p class="text-body-2">对方新报价：￥{{ currentBargainMessage.newOffer }}</p>
        </div>

        <!-- 已处理状态提示-->
        <div v-if="currentBargainMessage.bargainStatus !== 'pending'" class="text-center">
          <v-icon 
            :icon="currentBargainMessage.bargainStatus === 'accepted' ? 'mdi-check-circle' : 'mdi-alert-circle'"
            :color="currentBargainMessage.bargainStatus === 'accepted' ? 'green' : 'red'"
            size="48"
          ></v-icon>
          <p class="mt-2" v-if="currentBargainMessage.bargainStatus === 'accepted'">
            已成功接受报价！
          </p>
          <p class="mt-2" v-else>
            已拒绝报价，您的最低预期价格为：￥{{ currentBargainMessage.rejectReason }}
          </p>
        </div>

        <!-- 拒绝报价输入框（条件显示） -->
        <v-text-field
          v-model="rejectReason"
          label="请输入您的最低预期价格"
          type="number"
          v-show="showRejectInput"
          class="mt-3"
        ></v-text-field>
          <!-- 错误提示 -->
        <p v-show="showError" class="text-red-500 text-center mt-2" style="color: red;">
          最低预期价格不能低于对方报价（￥{{ currentBargainMessage.newOffer }}）
        </p>
      </v-card-text>

         <!-- 按钮区域条件渲染 -->
      <v-card-actions class="justify-end" v-if="currentBargainMessage.bargainStatus === 'pending'">
        <v-btn text @click="handleReject">拒绝</v-btn>
        <v-btn color="primary" text @click="showConfirmDialog = true">接受</v-btn>
      </v-card-actions>
      <v-card-actions class="justify-end" v-else>
        <v-btn color="primary" text @click="showBargainDialog = false">确定</v-btn>
      </v-card-actions>
  </v-card>
  </v-dialog>
    <!-- 接受报价确认弹窗 -->
  <v-dialog 
    v-model="showConfirmDialog" 
    max-width="400px"
    persistent
  >
    <v-card>
      <v-card-title class="text-h6">确认接受报价？</v-card-title>
      <v-card-text>您确定要接受对方的报价吗？确认后将无法修改。</v-card-text>
      <v-card-actions class="justify-end">
        <v-btn text @click="showConfirmDialog = false">再想想</v-btn>
        <v-btn color="primary" text @click="confirmAccept">确认</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

    <!-- 换物消息特殊弹窗 -->
  <v-dialog 
    v-model="showSwapDialog" 
    max-width="700px"
    persistent
  >
    <v-card>
      <v-card-title class="d-flex justify-space-between align-center">
        <span class="text-h5">
          {{ currentSwapMessage.userRole === 'sender' ? '我的换物请求' : currentSwapMessage.sender + ' 的换物请求' }}
        </span>
        <v-btn icon @click="showSwapDialog = false">
          <v-icon>mdi-close</v-icon>
        </v-btn>
      </v-card-title>
      
      <v-card-text>
        <!-- 商品信息显示 -->
        <v-row align="center" class="mb-4">
          <v-col cols="3">
            <v-img 
              :src="currentSwapMessage.swapProductImage" 
              height="80" 
              cover 
              class="rounded-8"
            ></v-img>
          </v-col>
          <v-col cols="9">
            <div class="text-h6">{{ currentSwapMessage.swapProductName }}</div>
            <div class="text-body-2">参考价格：￥{{ currentSwapMessage.swapProductPrice }}</div>
            <v-btn 
              text 
              color="primary" 
              :href="currentSwapMessage.swapProductLink"
              target="_blank"
              class="mt-2"
            >
              查看商品详情
            </v-btn>
          </v-col>
        </v-row>

        <!-- 换物说明（根据角色显示不同内容） -->
        <div v-if="currentSwapMessage.userRole === 'sender'">
          <!-- 发起者视角 -->
          <p class="text-body-2 mb-3">
            您向对方发起了换物请求，希望用您的《{{ currentSwapMessage.myProductName }}》换取对方的《{{ currentSwapMessage.otherProductName }}》
          </p>
          
          <!-- 请求状态显示 -->
          <div class="text-center">
            <v-icon 
              :icon="getStatusIcon(currentSwapMessage.swapStatus)"
              :color="getStatusColor(currentSwapMessage.swapStatus)"
              size="48"
            ></v-icon>
            <p class="mt-2">
              {{ getStatusText(currentSwapMessage.swapStatus) }}
            </p>
          </div>
        </div>
        
        <div v-else>
          <!-- 接收者视角 -->
          <p class="text-body-2" v-if="currentSwapMessage.swapStatus === 'pending'">
            对方希望用以上商品与您的《{{ currentSwapMessage.myProductName }}》交换，是否接受？
          </p>

          <!-- 已处理状态提示 -->
          <div v-if="currentSwapMessage.swapStatus !== 'pending'" class="text-center">
            <v-icon 
              :icon="currentSwapMessage.swapStatus === 'accepted' ? 'mdi-check-circle' : 'mdi-alert-circle'"
              :color="currentSwapMessage.swapStatus === 'accepted' ? 'green' : 'red'"
              size="48"
            ></v-icon>
            <p class="mt-2">
              {{ currentSwapMessage.swapStatus === 'accepted' ? '已成功接受换物请求！' : '已拒绝换物请求' }}
            </p>
          </div>
        </div>
      </v-card-text>

      <!-- 按钮区域条件渲染 -->
      <v-card-actions class="justify-end" v-if="currentSwapMessage.userRole === 'receiver' && currentSwapMessage.swapStatus === 'pending'">
        <v-btn text @click="showSwapRejectConfirm = true">拒绝换物</v-btn>
        <v-btn color="primary" text @click="showSwapAcceptConfirm = true">接受换物</v-btn>
      </v-card-actions>
      <v-card-actions class="justify-end" v-else>
        <v-btn color="primary" text @click="showSwapDialog = false">确定</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
  <!-- 接受换物确认弹窗 -->
  <v-dialog 
    v-model="showSwapAcceptConfirm" 
    max-width="400px"
    persistent
  >
    <v-card>
      <v-card-title class="text-h6">确认接受换物？</v-card-title>
      <v-card-text>您确定要接受对方的换物请求吗？确认后将无法修改。</v-card-text>
      <v-card-actions class="justify-end">
        <v-btn text @click="showSwapAcceptConfirm = false">再想想</v-btn>
        <v-btn color="primary" text @click="confirmSwapAccept">确认</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <!-- 拒绝换物确认弹窗 -->
  <v-dialog 
    v-model="showSwapRejectConfirm" 
    max-width="400px"
    persistent
  >
    <v-card>
      <v-card-title class="text-h6">确认拒绝换物？</v-card-title>
      <v-card-text>您确定要拒绝对方的换物请求吗？确认后将无法修改。</v-card-text>
      <v-card-actions class="justify-end">
        <v-btn text @click="showSwapRejectConfirm = false">再想想</v-btn>
        <v-btn color="primary" text @click="confirmSwapReject">确认</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
  
  </v-app>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { notificationApi, bargainApi, exchangeApi } from '@/services/api'

const router = useRouter()
const showMessageDetail = ref(false)
const currentMessage = ref({})

// 议价相关状态
const showBargainDialog = ref(false)
const currentBargainMessage = ref({})
const showRejectInput = ref(false)
const rejectReason = ref('')
const showConfirmDialog = ref(false)
const showError = ref(false)

// 换物相关状态
const showSwapDialog = ref(false)
const currentSwapMessage = ref({})
const showSwapAcceptConfirm = ref(false)  // 接受换物确认弹窗
const showSwapRejectConfirm = ref(false)  // 拒绝换物确认弹窗

// 数据加载状态
const loading = ref(false)
const messages = ref([])

// 分类配置和当前选中分类
const categories = ref([
  { 
    label: '系统消息', 
    value: 'system', 
    icon: 'mdi-information' ,  // 系统消息图标
    color: 'info' 
  },
  { 
    label: '议价消息', 
    value: 'bargain', 
    icon: 'mdi-handshake' ,  // 议价相关图标
    color: 'warning' 
  },
  { 
    label: '收到回复', 
    value: 'reply', 
    icon: 'mdi-message-reply' ,  // 回复相关图标
    color: 'primary'
  },
  { 
    label: '换物请求', 
    value: 'swap', 
    icon: 'mdi-swap-horizontal',  // 新增：交换图标
    color: 'success'  // 新增：成功色
  }
])
const currentCategory = ref('system')  // 默认显示系统消息

// 消息过滤逻辑
const filteredMessages = computed(() => {
  return messages.value.filter(msg => {
    switch(currentCategory.value) {
      case 'system':
        return msg.type === 'notification' && ['系统通知', '校园管理员'].includes(msg.sender)
      case 'bargain':
        return msg.type === 'bargain'
      case 'reply':
        return msg.type === 'reply' || (msg.type === 'notification' && !['系统通知', '校园管理员'].includes(msg.sender))
      case 'swap':
        return msg.type === 'swap'
      default:
        return true
    }
  })
})

// 获取当前用户ID
const getCurrentUserId = () => {
  const user = JSON.parse(localStorage.getItem('user') || '{}')
  return user.userId || 1 // 如果没有用户信息，使用默认值
}

// 加载用户消息
const loadMessages = async (category) => {
  loading.value = true
  try {
    const userId = getCurrentUserId()
    const response = await notificationApi.getUserMessages(userId, category, 50, 0)
    
    if (response.success && response.data) {
      messages.value = response.data
    } else {
      console.error('获取消息失败:', response.message)
      messages.value = []
    }
  } catch (error) {
    console.error('加载消息时发生错误:', error)
    messages.value = []
  } finally {
    loading.value = false
  }
}

// 返回上一页
const goBack = () => {
  router.go(-1)
}

// 消息点击事件处理
const handleMessageClick = async (message) => {
  // 标记消息为已读
  try {
    const userId = getCurrentUserId()
    await notificationApi.markMessageAsRead(userId, message.id)
    
    // 更新本地状态
    const targetIndex = messages.value.findIndex(item => item.id === message.id)
    if (targetIndex > -1) {
      messages.value[targetIndex].read = true
    }
  } catch (error) {
    console.error('标记消息已读失败:', error)
  }

  // 根据消息类型显示不同弹窗
  if (message.type === 'bargain') {
    currentBargainMessage.value = message
    showBargainDialog.value = true
  } else if (message.type === 'swap') {
    currentSwapMessage.value = message
    showSwapDialog.value = true
  } else {
    currentMessage.value = message
    showMessageDetail.value = true
  }
}

// 确认接受的最终操作
const confirmAccept = async () => {
  try {
    const message = currentBargainMessage.value
    await bargainApi.handleBargainResponse({
      negotiationId: message.id,
      action: 'accept'
    })
    
    message.bargainStatus = 'accepted'
    showBargainDialog.value = false
    showConfirmDialog.value = false
  } catch (error) {
    console.error('接受议价失败:', error)
  }
}

// 拒绝接受报价
const handleReject = async () => {
  if (!showRejectInput.value) {
    showRejectInput.value = true
    showError.value = false
  } else {
    const message = currentBargainMessage.value
    const inputPrice = Number(rejectReason.value)
    const minPrice = message.newOffer || 0
    
    if (isNaN(inputPrice) || inputPrice < minPrice) {
      showError.value = true
      return
    }

    try {
      await bargainApi.handleBargainResponse({
        negotiationId: message.id,
        action: 'reject',
        rejectReason: rejectReason.value
      })

      message.bargainStatus = 'rejected'
      message.rejectReason = rejectReason.value
      showBargainDialog.value = false
      showRejectInput.value = false
      rejectReason.value = ''
      showError.value = false
    } catch (error) {
      console.error('拒绝议价失败:', error)
    }
  }
}

// 确认接受换物
const confirmSwapAccept = async () => {
  try {
    const message = currentSwapMessage.value
    await exchangeApi.handleExchangeResponse({
      exchangeRequestId: message.id,
      status: '接受',
      responseMessage: '同意换物请求'
    })
    
    message.swapStatus = 'accepted'
    showSwapDialog.value = false
    showSwapAcceptConfirm.value = false
    
    // 显示成功提示
    console.log('换物请求已接受')
  } catch (error) {
    console.error('接受换物失败:', error)
    // 可以添加错误提示
    alert('接受换物失败，请稍后重试')
  }
}

// 确认拒绝换物
const confirmSwapReject = async () => {
  try {
    const message = currentSwapMessage.value
    await exchangeApi.handleExchangeResponse({
      exchangeRequestId: message.id,
      status: '拒绝',
      responseMessage: '拒绝换物请求'
    })
    
    message.swapStatus = 'rejected'
    showSwapDialog.value = false
    showSwapRejectConfirm.value = false
    
    // 显示成功提示
    console.log('换物请求已拒绝')
  } catch (error) {
    console.error('拒绝换物失败:', error)
    // 可以添加错误提示
    alert('拒绝换物失败，请稍后重试')
  }
}

// 监听分类变化，重新加载消息
const handleCategoryChange = (category) => {
  currentCategory.value = category
  loadMessages(category)
}

// 获取状态图标
const getStatusIcon = (status) => {
  switch(status) {
    case 'accepted':
      return 'mdi-check-circle'
    case 'rejected':
      return 'mdi-close-circle'
    case 'pending':
      return 'mdi-clock-outline'
    default:
      return 'mdi-help-circle'
  }
}

// 获取状态颜色
const getStatusColor = (status) => {
  switch(status) {
    case 'accepted':
      return 'green'
    case 'rejected':
      return 'red'
    case 'pending':
      return 'orange'
    default:
      return 'grey'
  }
}

// 获取状态文本
const getStatusText = (status) => {
  switch(status) {
    case 'accepted':
      return '对方已接受您的换物请求！'
    case 'rejected':
      return '对方已拒绝您的换物请求'
    case 'pending':
      return '等待对方回应...'
    default:
      return '状态未知'
  }
}

// 组件挂载时加载消息
onMounted(() => {
  loadMessages(currentCategory.value)
})
</script>

<style scoped>
/* 消息项样式 */
.message-item {
  border-bottom: 1px solid #eee;
  transition: background-color 0.2s ease;
  padding: 12px 16px;
}

.message-item:hover {
  background-color: #f9f9f9;
}

/* 未读消息样式 */
.unread .v-list-item-title {
  color: #1976d2;
}

.unread {
  background-color: #f5f9ff;
}

/* 调整内容区域边距 */
.v-list-item-content {
  margin: 8px 0;
}
.v-navigation-drawer {
  padding-top: 20px;
}

/* 议价弹窗商品图片样式 */
.v-img {
  border-radius: 8px;
  object-fit: cover;
}

/* 报价信息文字样式 */
.text-body-2 {
  color: #666;
  margin: 8px 0;
}
</style>