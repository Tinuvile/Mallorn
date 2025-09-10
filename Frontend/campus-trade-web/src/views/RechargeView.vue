<template>
  <v-app>
    <!-- 导航栏 -->
    <header class="navbar">
      <span class="icon">
        <svg
          width="48px"
          height="48px"
          stroke-width="1.5"
          viewBox="0 0 24 24"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
          color="#ffffff"
        >
          <circle cx="12" cy="12" r="10" stroke="#ffffff" stroke-width="1.5"></circle>
          <path
            d="M7.63262 3.06689C8.98567 3.35733 9.99999 4.56025 9.99999 6.00007C9.99999 7.65693 8.65685 9.00007 6.99999 9.00007C5.4512 9.00007 4.17653 7.82641 4.01685 6.31997"
            stroke="#ffffff"
            stroke-width="1.5"
          ></path>
          <path
            d="M22 13.0505C21.3647 12.4022 20.4793 12 19.5 12C17.567 12 16 13.567 16 15.5C16 17.2632 17.3039 18.7219 19 18.9646"
            stroke="#ffffff"
            stroke-width="1.5"
          ></path>
          <path
            d="M14.5 8.51L14.51 8.49889"
            stroke="#ffffff"
            stroke-width="1.5"
            stroke-linecap="round"
            stroke-linejoin="round"
          ></path>
          <path
            d="M10 17C11.1046 17 12 16.1046 12 15C12 13.8954 11.1046 13 10 13C8.89543 13 8 13.8954 8 15C8 16.1046 8.89543 17 10 17Z"
            stroke="#ffffff"
            stroke-width="1.5"
            stroke-linecap="round"
            stroke-linejoin="round"
          ></path>
        </svg>
      </span>
      <span class="title">Campus Secondhand</span>
      <!-- 返回按钮 -->
      <v-btn class="logout-btn ml-auto" icon to="/">
        <v-icon color="black">mdi-exit-to-app</v-icon>
      </v-btn>
    </header>

    <!-- 标题区域 -->
    <div class="simple-title-section">
      <h2>账户充值</h2>
      <v-divider color="#fb7c7c" thickness="4" class="mx-auto" style="width: 95%"></v-divider>
    </div>

    <!-- 主体内容 -->
    <v-main class="bg-gray-100 main-content">
      <v-container class="pa-0 h-full" fluid>
        <v-row class="h-full ma-0 justify-center">
          <!-- 充值主界面 -->
          <v-col cols="12" md="8" sm="12" class="h-full px-4">
            <v-card elevation="2" class="rounded-lg">
              <!-- 余额显示 -->
              <v-card-title class="bg-pink-50 text-pink-800 py-4 d-flex align-center">
                <span class="title-bar flex-grow-1">当前余额</span>
              </v-card-title>

              <v-card-text class="py-6 px-6">
                <div class="text-center mb-6">
                  <div v-if="accountStore.isLoading" class="py-4">
                    <v-progress-circular
                      indeterminate
                      color="primary"
                      size="40"
                    ></v-progress-circular>
                    <div class="mt-2">加载余额中...</div>
                  </div>
                  <div v-else>
                    <div class="balance-display">
                      <span class="balance-amount">¥{{ currentBalance.toFixed(2) }}</span>
                    </div>
                    <div class="balance-label">账户余额</div>
                    <div v-if="accountStore.account?.lastUpdateTime" class="balance-update-time">
                      更新时间: {{ new Date(accountStore.account.lastUpdateTime).toLocaleString('zh-CN') }}
                    </div>
                  </div>
                </div>

                <v-divider class="my-6"></v-divider>

                <!-- 快速充值选项 -->
                <div class="mb-6">
                  <h3 class="text-h6 mb-4 text-center">选择充值金额</h3>
                  <v-row class="justify-center">
                    <v-col
                      v-for="amount in quickAmounts"
                      :key="amount"
                      cols="4"
                      sm="3"
                      md="2"
                      class="pa-2"
                    >
                      <v-btn
                        :variant="rechargeAmount === amount ? 'elevated' : 'outlined'"
                        :color="rechargeAmount === amount ? 'primary' : 'default'"
                        class="quick-amount-btn"
                        @click="selectAmount(amount)"
                        height="60"
                        block
                      >
                        ¥{{ amount }}
                      </v-btn>
                    </v-col>
                  </v-row>
                </div>

                <!-- 自定义金额输入 -->
                <div class="mb-6">
                  <h3 class="text-h6 mb-4 text-center">或输入自定义金额</h3>
                  <v-text-field
                    v-model="customAmount"
                    label="充值金额"
                    type="number"
                    min="0.01"
                    max="99999"
                    step="0.01"
                    prefix="¥"
                    variant="outlined"
                    :error-messages="amountErrors"
                    class="custom-amount-input"
                    @input="onCustomAmountChange"
                  ></v-text-field>
                </div>

                <!-- 充值说明 -->
                <v-textarea
                  v-model="remarks"
                  label="备注（可选）"
                  variant="outlined"
                  rows="3"
                  placeholder="请输入充值说明"
                  class="mb-6"
                ></v-textarea>

                <!-- 充值按钮 -->
                <div class="text-center">
                  <v-btn
                    color="#ffdfdf"
                    class="recharge-btn"
                    height="50"
                    :loading="accountStore.isLoading"
                    :disabled="!isValidAmount"
                    @click="handleRecharge"
                  >
                    <span class="recharge-btn-text">立即充值 ¥{{ finalAmount.toFixed(2) }}</span>
                  </v-btn>
                </div>
              </v-card-text>
            </v-card>
          </v-col>

          <!-- 充值记录 -->
          <v-col cols="12" md="4" sm="12" class="h-full px-4">
            <v-card elevation="2" class="rounded-lg h-full d-flex flex-column">
              <v-card-title class="bg-pink-50 text-pink-800 py-4 d-flex align-center">
                <span class="title-bar flex-grow-1">充值记录</span>
                <v-btn icon size="small" @click="loadRechargeRecords" :loading="recordsLoading">
                  <v-icon>mdi-refresh</v-icon>
                </v-btn>
              </v-card-title>

              <v-card-text class="py-4 px-4 flex-grow-1 overflow-y-auto">
                <div v-if="recordsLoading && rechargeRecords.length === 0" class="text-center py-4">
                  <v-progress-circular
                    indeterminate
                    color="primary"
                    size="30"
                  ></v-progress-circular>
                  <div class="mt-2">加载记录中...</div>
                </div>

                <div v-else-if="rechargeRecords.length === 0" class="text-center py-8 text-grey">
                  <v-icon size="48" color="grey">mdi-history</v-icon>
                  <div class="mt-2">暂无充值记录</div>
                </div>

                <v-list v-else class="pa-0">
                  <v-list-item
                    v-for="record in rechargeRecords"
                    :key="record.rechargeId"
                    class="record-item"
                  >
                    <div class="record-content">
                      <div class="record-header">
                        <span class="record-amount">+¥{{ record.amount.toFixed(2) }}</span>
                        <v-chip :color="getStatusColor(record.status)" size="small" variant="flat">
                          {{ getStatusText(record.status) }}
                        </v-chip>
                      </div>
                      <div class="record-details">
                        <div class="record-time">{{ formatDate(record.createTime) }}</div>
                      </div>
                    </div>
                  </v-list-item>
                </v-list>

                <!-- 分页 -->
                <div v-if="totalPages > 1" class="text-center mt-4">
                  <v-pagination
                    v-model="currentPage"
                    :length="totalPages"
                    :total-visible="5"
                    @update:model-value="loadRechargeRecords"
                  ></v-pagination>
                </div>
              </v-card-text>
            </v-card>
          </v-col>
        </v-row>
      </v-container>
    </v-main>

    <!-- 充值确认对话框 -->
    <v-dialog v-model="showConfirmDialog" max-width="500px" persistent>
      <v-card>
        <v-card-title class="bg-pink-50 text-pink-800 py-3 d-flex align-center">
          <span class="title-bar flex-grow-1">确认充值</span>
        </v-card-title>
        <v-card-text class="py-6 px-4">
          <div class="text-center">
            <div class="confirm-amount mb-4">
              <span class="text-h4 text-primary font-weight-bold"
                >¥{{ finalAmount.toFixed(2) }}</span
              >
            </div>
            <div class="mb-4">
              <p class="text-body-1">确认要充值此金额吗？</p>
              <p v-if="remarks" class="text-caption text-grey">备注：{{ remarks }}</p>
            </div>
            <div class="text-caption text-grey">
              <v-icon size="16" class="mr-1">mdi-information</v-icon>
              这是模拟充值，仅用于演示
            </div>
          </div>
        </v-card-text>
        <v-card-actions class="justify-center py-4">
          <v-btn text @click="showConfirmDialog = false" class="mr-4"> 取消 </v-btn>
          <v-btn color="primary" :loading="processing" @click="processRecharge"> 确认充值 </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- 充值结果对话框 -->
    <v-dialog v-model="showResultDialog" max-width="500px">
      <v-card>
        <v-card-text class="text-center py-6">
          <v-icon :color="rechargeResult.success ? 'green' : 'red'" size="64" class="mb-4">
            {{ rechargeResult.success ? 'mdi-check-circle' : 'mdi-alert-circle' }}
          </v-icon>
          <h3 class="text-h6 mb-2">
            {{ rechargeResult.success ? '充值成功！' : '充值失败' }}
          </h3>
          <p class="text-body-2">{{ rechargeResult.message }}</p>
        </v-card-text>
        <v-card-actions class="justify-center">
          <v-btn color="primary" @click="closeResultDialog"> 确定 </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-app>
</template>

<script setup lang="ts">
  import { ref, computed, onMounted, watch } from 'vue'
  import { useVirtualAccountStore } from '@/stores/virtualaccount'
  import type { RechargeRecord } from '@/services/api'

  const accountStore = useVirtualAccountStore()

  // 响应式数据
  const rechargeAmount = ref(0)
  const customAmount = ref('')
  const remarks = ref('')
  const amountErrors = ref<string[]>([])
  const showConfirmDialog = ref(false)
  const showResultDialog = ref(false)
  const processing = ref(false)
  const recordsLoading = ref(false)
  const rechargeRecords = ref<RechargeRecord[]>([])
  const currentPage = ref(1)
  const totalPages = ref(1)
  const pageSize = 9  // 每页显示9条记录

  // 快速充值金额选项
  const quickAmounts = ref([10, 20, 50, 100, 200, 500])

  // 充值结果
  const rechargeResult = ref({
    success: false,
    message: '',
  })

  // 计算属性
  const currentBalance = computed(() => {
    // 确保返回数字类型，避免显示为0的问题
    const balance = accountStore.account?.balance
    return typeof balance === 'number' ? balance : 0
  })

  const finalAmount = computed(() => {
    if (customAmount.value) {
      return parseFloat(customAmount.value) || 0
    }
    return rechargeAmount.value
  })

  const isValidAmount = computed(() => {
    return finalAmount.value > 0 && finalAmount.value <= 99999
  })

  // 方法
  const selectAmount = (amount: number) => {
    rechargeAmount.value = amount
    customAmount.value = ''
    validateAmount()
  }

  const onCustomAmountChange = () => {
    rechargeAmount.value = 0
    validateAmount()
  }

  const validateAmount = () => {
    amountErrors.value = []

    if (customAmount.value) {
      const amount = parseFloat(customAmount.value)
      if (isNaN(amount) || amount <= 0) {
        amountErrors.value.push('请输入有效的金额')
      } else if (amount > 99999) {
        amountErrors.value.push('单次充值金额不能超过99999元')
      }
    }
  }

  const handleRecharge = () => {
    if (!isValidAmount.value) {
      return
    }
    // 重置之前的充值结果
    rechargeResult.value = { success: false, message: '' }
    showConfirmDialog.value = true
  }

  const processRecharge = async () => {
    processing.value = true

    try {
      // 1. 创建充值订单
      const createResult = await accountStore.createRecharge(finalAmount.value, remarks.value)

      if (!createResult.success || !createResult.data) {
        rechargeResult.value = {
          success: false,
          message: createResult.message,
        }
        return
      }

      // 2. 完成模拟充值
      const completeResult = await accountStore.completeSimulationRecharge(
        createResult.data.rechargeId
      )

      rechargeResult.value = {
        success: completeResult.success,
        message: completeResult.message,
      }

      if (completeResult.success) {
        // 充值成功，重置表单并刷新记录
        resetForm()
        await loadRechargeRecords()
      }
    } catch (error) {
      rechargeResult.value = {
        success: false,
        message: '充值过程中发生错误，请重试',
      }
    } finally {
      processing.value = false
      showConfirmDialog.value = false
      showResultDialog.value = true
    }
  }

  const resetForm = () => {
    rechargeAmount.value = 0
    customAmount.value = ''
    remarks.value = ''
    amountErrors.value = []
  }

  const closeResultDialog = () => {
    showResultDialog.value = false
    // 不需要立即重置 rechargeResult，避免闪现问题
    // rechargeResult 会在下次充值时被重新设置
  }

  // 加载充值记录
  const loadRechargeRecords = async () => {
    recordsLoading.value = true

    try {
      console.log('开始加载充值记录，页码:', currentPage.value, '页大小:', pageSize)
      const result = await accountStore.getRechargeRecords(currentPage.value, pageSize)
      console.log('充值记录API响应:', result)

      if (result.success && result.data) {
        rechargeRecords.value = result.data.records
        totalPages.value = result.data.totalPages
        console.log('充值记录加载成功，记录数:', result.data.records.length, '总页数:', result.data.totalPages)
      } else {
        console.error('充值记录加载失败:', result.message)
      }
    } catch (error) {
      console.error('加载充值记录异常:', error)
    } finally {
      recordsLoading.value = false
    }
  }

  // 格式化日期
  const formatDate = (dateString: string) => {
    const date = new Date(dateString)
    return date.toLocaleString('zh-CN', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
    })
  }

  // 获取状态颜色
  const getStatusColor = (status: string) => {
    switch (status) {
      case '成功':
      case 'Completed':
        return 'green'
      case '处理中':
      case 'Pending':
        return 'orange'
      case '失败':
      case 'Failed':
        return 'red'
      default:
        return 'grey'
    }
  }

  // 获取状态文本
  const getStatusText = (status: string) => {
    switch (status) {
      case '成功':
      case 'Completed':
        return '已完成'
      case '处理中':
      case 'Pending':
        return '待支付'
      case '失败':
      case 'Failed':
        return '已失败'
      default:
        return status
    }
  }

  // 监听自定义金额变化
  watch(customAmount, validateAmount)

  // 组件挂载时初始化
  onMounted(async () => {
    try {
      // 初始化账户存储
      accountStore.initializeAccount()
      
      // 强制从服务器获取最新的账户余额
      const balanceResult = await accountStore.fetchBalance()
      if (!balanceResult.success) {
        console.error('获取账户余额失败:', balanceResult.message)
      } else {
        console.log('余额获取成功:', balanceResult.data)
      }
      
      // 加载充值记录
      await loadRechargeRecords()
    } catch (error) {
      console.error('初始化账户信息失败:', error)
    }
  })
</script>

<style scoped>
  /* 重置全局样式 */
  body,
  .v-application {
    margin: 0;
    padding: 0;
  }

  /* 导航栏样式 */
  .navbar {
    position: relative;
    width: 100%;
    height: 60px;
    margin: 0;
    padding: 0 16px;
    background-color: #cadefc;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    z-index: 1000;
    display: flex;
    align-items: center;
    justify-content: flex-start;
  }

  .icon svg {
    width: 48px;
    height: 48px;
  }

  .title {
    font-size: 32px;
    font-weight: bold;
    margin-left: 16px;
    color: white !important;
  }

  .simple-title-section {
    text-align: center;
    padding: 20px 0 10px 0;
    background-color: #fcf6f6;
  }

  .simple-title-section h2 {
    font-size: 1.8rem;
    font-weight: 500;
    color: #333;
    margin-bottom: 16px;
  }

  .main-content {
    min-height: calc(100vh - 168px) !important;
    background-color: #fcf6f6;
  }

  .title-bar {
    background-color: #ffdfdf;
    color: #0a0608;
    font-weight: 600;
    font-size: 1.25rem;
    padding: 8px 12px;
    border-radius: 4px;
    display: inline-flex;
    align-items: center;
  }

  /* 余额显示样式 */
  .balance-display {
    margin-bottom: 8px;
  }

  .balance-amount {
    font-size: 3rem;
    font-weight: bold;
    color: #1976d2;
  }

  .balance-label {
    font-size: 1rem;
    color: #666;
    margin-top: 8px;
  }

  .balance-update-time {
    font-size: 0.8rem;
    color: #999;
    margin-top: 4px;
  }

  /* 快速金额按钮样式 */
  .quick-amount-btn {
    font-size: 1.1rem;
    font-weight: 600;
    text-transform: none;
  }

  /* 自定义金额输入样式 */
  .custom-amount-input {
    max-width: 300px;
    margin: 0 auto;
  }

  /* 充值按钮样式 */
  .recharge-btn {
    min-width: 200px;
    color: white !important;
    font-weight: 600 !important;
    border-radius: 25px !important;
    box-shadow: 0 4px 8px rgba(255, 223, 223, 0.3) !important;
  }

  .recharge-btn-text {
    font-size: 1.1rem;
    font-weight: 600;
  }

  /* 充值记录样式 */
  .record-item {
    border-bottom: 1px solid #f0f0f0;
    padding: 12px 0;
  }

  .record-item:last-child {
    border-bottom: none;
  }

  .record-content {
    width: 100%;
  }

  .record-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 8px;
  }

  .record-amount {
    font-size: 1.1rem;
    font-weight: 600;
    color: #4caf50;
  }

  .record-details {
    font-size: 0.9rem;
    color: #666;
  }

  .record-time {
    margin-bottom: 4px;
  }

  .record-remarks {
    font-style: italic;
  }

  /* 确认对话框样式 */
  .confirm-amount {
    padding: 16px;
    background-color: #f8f9fa;
    border-radius: 8px;
  }

  /* 响应式设计 */
  @media (max-width: 768px) {
    .balance-amount {
      font-size: 2.5rem;
    }

    .quick-amount-btn {
      font-size: 1rem;
      height: 50px;
    }

    .recharge-btn {
      width: 100%;
      min-width: auto;
    }
  }

  /* 滚动条样式 */
  ::-webkit-scrollbar {
    width: 6px;
  }

  ::-webkit-scrollbar-track {
    background: #f1f1f1;
    border-radius: 3px;
  }

  ::-webkit-scrollbar-thumb {
    background: #c1c1c1;
    border-radius: 3px;
  }

  ::-webkit-scrollbar-thumb:hover {
    background: #a8a8a8;
  }
</style>
