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
            @click="currentCategory = category.value" 
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
          <v-list style="background-color: white; border-radius: 0;" rounded="lg">
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
        <span class="text-h5">{{ currentSwapMessage.sender }} 的换物请求</span>
        <v-btn icon @click="showSwapDialog = false">
          <v-icon>mdi-close</v-icon>
        </v-btn>
      </v-card-title>
      
      <v-card-text>
        <!-- 对方商品信息 -->
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

       <!-- 换物说明（条件显示） -->
        <p class="text-body-2" v-if="currentSwapMessage.swapStatus === 'pending'">
          对方希望用以上商品与您的商品交换，是否接受？
        </p>

        <!-- 已处理状态提示（新增） -->
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
      </v-card-text>

      <!-- 按钮区域条件渲染 -->
      <v-card-actions class="justify-end" v-if="currentSwapMessage.swapStatus === 'pending'">
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
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'

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
  switch(currentCategory.value) {
    case 'system':
      // 系统消息：包含系统通知和校园管理员
      return messages.value.filter(msg => ['系统通知', '校园管理员'].includes(msg.sender))
    case 'bargain':
      // 议价消息：非系统发送且内容含"议价"/"便宜"关键词
      return messages.value.filter(msg => 
        !['系统通知', '校园管理员'].includes(msg.sender) && 
        (msg.content.includes('议价'))
      )
    case 'reply':
      // 回复消息:收到的其他消息
      return messages.value.filter(msg => 
        !['系统通知', '校园管理员'].includes(msg.sender) && 
        !msg.content.includes('议价') && 
        !msg.content.includes('换物')
      )
    case 'swap':  // 换物请求过滤条件
      return messages.value.filter(msg => 
        msg.content.includes('换物')  // 匹配换物关键词
      )
    default:
      return messages.value
  }
})

// 返回上一页
const goBack = () => {
  router.go(-1)
}

// 消息点击事件处理
const handleMessageClick = (message) => {
  // 标记消息为已读
  const targetIndex = messages.value.findIndex(item => item.id === message.id)
  if (targetIndex > -1) {
    messages.value[targetIndex].read = true
  }

  // 根据消息类型显示不同弹窗
  if (message.type === 'bargain') {
    currentBargainMessage.value = message
    showBargainDialog.value = true
  } else if (message.type === 'swap') {  // 新增换物类型处理
    currentSwapMessage.value = message
    showSwapDialog.value = true
  } else {
    currentMessage.value = message
    showMessageDetail.value = true
  }
}
// 确认接受的最终操作
const confirmAccept = () => {
  currentBargainMessage.value.bargainStatus = 'accepted'
  showBargainDialog.value = false  // 关闭议价弹窗
  showConfirmDialog.value = false  // 关闭确认弹窗
}

// 拒绝接受报价
const handleReject = () => {
  if (!showRejectInput.value) {
    showRejectInput.value = true  // 第一次点击拒绝显示输入框
    showError.value = false  // 重置错误提示
  } else {
    // 校验输入价格是否有效
    const inputPrice = Number(rejectReason.value)
    const minPrice = currentBargainMessage.value.newOffer
    
    if (isNaN(inputPrice) || inputPrice < minPrice) {
      showError.value = true  // 显示错误提示
      return  // 阻止后续操作
    }

    // 输入有效时更新状态
    currentBargainMessage.value.bargainStatus = 'rejected'
    currentBargainMessage.value.rejectReason = rejectReason.value
    showBargainDialog.value = false
    showRejectInput.value = false
    rejectReason.value = ''
    showError.value = false  // 重置错误提示
  }
}

// 确认接受换物
const confirmSwapAccept = () => {
  currentSwapMessage.value.swapStatus = 'accepted' 
  showSwapDialog.value = false        // 关闭换物主弹窗
  showSwapAcceptConfirm.value = false // 关闭确认弹窗
}

// 确认拒绝换物
const confirmSwapReject = () => {
  currentSwapMessage.value.swapStatus = 'rejected'
  showSwapDialog.value = false        // 关闭换物主弹窗
  showSwapRejectConfirm.value = false // 关闭确认弹窗
}

// 模拟消息数据
const messages = ref([
  {
    id: 1,
    sender: '系统通知',
    content: '您发布的商品"Nike运动鞋"已通过审核，现在可以在平台上显示',
    time: '今天 09:23',
    read: false
  },
  {
    id: 2,
    sender: '李同学',
    content: '东西很好，我很喜欢',
    time: '昨天 16:45',
    read: false
  },
  {
    id: 3,
    sender: '校园管理员',
    content: '【重要通知】本周五将进行系统维护，维护期间平台暂停服务，敬请谅解',
    time: '昨天 10:12',
    read: false
  },
  {
    id: 4,
    sender: '张同学',
    content: '交易已完成，感谢你的商品！',
    time: '06月18日',
    read: false
  },
  {
    id: 5,
    sender: '王同学',
    content: '议价：我给出了最新报价，请查看',
    time: '06月15日',
    read: false,
    type: 'bargain',
    productName: 'New Balance NB 530', 
    productImage: '/images/n1.jpg', 
    myOffer: 150,  
    newOffer: 100,
    bargainStatus: 'pending'      
  },
  {
    id: 6,
    sender: '陈同学',
    content: '换物：你好，我有一个好看的帽子1，想和你的鞋子交换，方便聊聊吗？',
    time: '今天 11:05',
    read: false,
    type: 'swap',
    swapProductName: '帽子1',  // 对方商品名称
    swapProductImage: '/images/hot1.webp',  // 对方商品图片
    swapProductPrice: 2800,  // 对方商品价格
    swapProductLink: '/goods/123',  // 对方商品链接
    swapStatus: 'pending'
  },
  {
    id: 7,
    sender: '周同学',
    content: '换物：看到你发布的鞋子，我有一个好看的帽子2想和你交换，需要的话联系我~',
    time: '昨天 14:30',
    read: false,
    type: 'swap',
    swapProductName: '帽子2',
    swapProductImage: '/images/hot3.webp',
    swapProductPrice: 4200,
    swapProductLink: '/goods/456',
    swapStatus: 'pending'
  }
])
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