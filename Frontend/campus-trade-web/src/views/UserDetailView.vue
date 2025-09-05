<template>
  <v-app>
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
            d="M22 13.0505C21.364 12.4022 20.4793 12 19.5 12C17.567 12 16 13.567 16 15.5C16 17.2632 17.3039 18.7219 19 18.9646"
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
      <!-- 新增退出按钮 -->
      <v-btn class="logout-btn ml-auto" icon to="/">
        <v-icon color="black">mdi-exit-to-app</v-icon>
      </v-btn>
    </header>
    <v-main class="grey lighten-4">
      <div class="pc-user-detail">
        <v-container class="px-4" style="max-width: 1600px">
          <div class="user-info-header">
            <h1 class="text-h4 font-weight-bold text-center primary--text mb-2">个人信息中心</h1>
            <v-divider thickness="4" color="#f5d8d8" class="mb-6"></v-divider>
          </div>
          <!-- 加载状态 -->
          <v-row
            v-if="isLoading"
            justify="center"
            align="center"
            class="fill-height"
            style="height: 70vh"
          >
            <v-col cols="12" class="text-center">
              <v-progress-circular indeterminate color="primary" size="64"></v-progress-circular>
              <p class="mt-4 grey--text">加载用户信息中...</p>
            </v-col>
          </v-row>

          <!-- 错误状态 -->
          <v-row
            v-else-if="virtualAccountStore.error"
            justify="center"
            align="center"
            class="fill-height"
            style="height: 70vh"
          >
            <v-col cols="12" md="6" class="text-center">
              <v-icon color="error" size="64" class="mb-4">mdi-alert-circle</v-icon>
              <p class="error--text mb-4 text-h6">{{ virtualAccountStore.error }}</p>
              <v-btn @click="retryLoading" color="primary" large> 重试 </v-btn>
            </v-col>
          </v-row>

          <!-- 主内容区 -->
          <v-row v-else class="mt-6" style="align-items: stretch">
            <!-- 左侧用户信息区 -->
            <v-col cols="12" md="4" class="pr-md-4 d-flex">
              <v-card class="custom-card pa-6 d-flex flex-column" style="flex: 1">
                <div class="d-flex align-center mb-6">
                  <div class="user-avatar-container mr-4">
                    <img src="/images/userphoto.png" alt="用户头像" class="user-avatar-image" />
                  </div>
                  <div>
                    <h2 class="text-h5 font-weight-bold">
                      {{ userStore.user?.fullName || userStore.user?.username || '未命名用户' }}
                    </h2>
                    <p class="text--secondary">计算机科学与技术学院</p>
                  </div>
                </div>

                <v-row class="mt-4 flex-grow-1" style="align-content: flex-start">
                  <!-- 用户ID -->
                  <v-col cols="12">
                    <v-card class="pa-3 info-card-1 rounded-lg">
                      <div class="d-flex justify-space-between">
                        <span class="font-weight-medium">用户ID:</span>
                        <span>{{ userStore.user?.userId || 'N/A' }}</span>
                      </div>
                    </v-card>
                  </v-col>

                  <!-- 用户名 -->
                  <v-col cols="12">
                    <v-card class="pa-3 info-card-1 rounded-lg">
                      <div class="d-flex justify-space-between">
                        <span class="font-weight-medium">用户名:</span>
                        <span>{{ userStore.user?.username || 'N/A' }}</span>
                      </div>
                    </v-card>
                  </v-col>

                  <!-- 学号 -->
                  <v-col cols="12">
                    <v-card class="pa-3 info-card-1 rounded-lg">
                      <div class="d-flex justify-space-between">
                        <span class="font-weight-medium">学号:</span>
                        <span>{{ userStore.user?.studentId || 'N/A' }}</span>
                      </div>
                    </v-card>
                  </v-col>

                  <!-- 手机号 -->
                  <v-col cols="12">
                    <v-card class="pa-3 info-card-1 rounded-lg">
                      <div class="d-flex justify-space-between">
                        <span class="font-weight-medium">手机:</span>
                        <span>{{
                          userStore.user?.phone ? maskPhone(userStore.user.phone) : 'N/A'
                        }}</span>
                      </div>
                    </v-card>
                  </v-col>

                  <!-- 邮箱 -->
                  <v-col cols="12">
                    <v-card class="pa-3 info-card-1 rounded-lg">
                      <div class="d-flex justify-space-between">
                        <span class="font-weight-medium">邮箱:</span>
                        <span>{{ userStore.user?.email || 'N/A' }}</span>
                        <v-icon
                          v-if="userStore.user?.emailVerified"
                          color="success"
                          small
                          title="邮箱已认证"
                        >
                          mdi-check-circle
                        </v-icon>
                      </div>
                    </v-card>
                  </v-col>
                </v-row>

                <!-- 修改后的按钮区域 - 垂直排列并水平居中 -->
                <div class="vertical-button-container mt-4">
                  <v-btn
                    class="action-btn account-action-btn"
                    @click="openChangePasswordDialog"
                    block
                  >
                    修改密码
                  </v-btn>
                  <v-btn class="action-btn edit-btn mb-2" @click="editInfo" block> 编辑信息 </v-btn>
                  <v-btn
                    class="action-btn verify-btn"
                    @click="handleVerifyEmail"
                    block
                    :loading="isVerifying"
                  >
                    <v-icon v-if="userStore.user?.emailVerified" left>mdi-check-circle</v-icon>
                    {{ userStore.user?.emailVerified ? '已认证' : '邮箱认证' }}
                  </v-btn>
                </div>
              </v-card>
            </v-col>
            <v-divider
              vertical
              thickness="4"
              color="#c9d6df"
              class="mx-md-2 my-6 d-none d-md-flex"
            ></v-divider>

            <!-- 中间交易记录区 -->
            <v-col cols="12" md="4" class="px-md-4 d-flex">
              <v-card class="custom-card pa-6 d-flex flex-column" style="flex: 1">
                <div class="d-flex align-center mb-4">
                  <v-icon color="primary" class="mr-2">mdi-history</v-icon>
                  <h3 class="text-h5 font-weight-bold primary--text">交易记录</h3>
                </div>

                <v-row class="flex-grow-1" style="align-content: flex-start">
                  <!-- 交易记录列表 -->
                  <v-col cols="12" v-for="(transaction, index) in transactions" :key="index">
                    <v-card class="pa-3 info-card rounded-lg">
                      <div class="d-flex justify-space-between align-center mb-1">
                        <span class="font-weight-medium">{{ transaction.title }}</span>
                        <span :class="transaction.amount > 0 ? 'green--text' : 'red--text'">
                          {{ transaction.amount > 0 ? '+' : '' }}¥{{
                            Math.abs(transaction.amount).toFixed(2)
                          }}
                        </span>
                      </div>
                      <div class="text-caption text--secondary">{{ transaction.date }}</div>
                    </v-card>
                  </v-col>
                </v-row>

                <!-- 信用评分区域 -->
                <v-card class="pa-4 mt-4 credit-score-card rounded-lg">
                  <div class="d-flex justify-space-between align-center mb-3">
                    <span class="font-weight-medium">信用评分:</span>
                    <div class="d-flex align-center">
                      <span class="text-h4 font-weight-bold mr-2">{{
                        userStore.user?.creditScore || 'N/A'
                      }}</span>
                      <v-icon color="amber">mdi-star</v-icon>
                    </div>
                  </div>
                  <v-progress-linear
                    :value="calculateCreditScorePercentage(userStore.user?.creditScore)"
                    color="amber"
                    height="10"
                    rounded
                    class="mb-2"
                  ></v-progress-linear>
                  <div class="d-flex justify-space-between text-caption text--secondary">
                    <span>0</span>
                    <span>5</span>
                  </div>
                </v-card>
              </v-card>
            </v-col>
            <v-divider
              vertical
              thickness="4"
              color="#c9d6df"
              class="mx-md-2 my-6 d-none d-md-flex"
            ></v-divider>

            <!-- 右侧区域 (账户信息和快捷功能) -->
            <v-col cols="12" md="3" class="d-flex flex-column">
              <!-- 账户信息卡片 -->
              <v-card class="custom-card pa-6 mb-6 flex-grow-1">
                <div class="d-flex align-center mb-4">
                  <v-icon color="primary" class="mr-2">mdi-wallet</v-icon>
                  <h3 class="text-h5 font-weight-bold primary--text">账户信息</h3>
                </div>

                <v-row>
                  <v-col cols="12">
                    <v-card class="pa-4 balance-card rounded-lg">
                      <div class="d-flex justify-space-between align-center">
                        <span class="font-weight-medium">账户余额:</span>
                        <span class="text-h4 font-weight-bold primary--text"
                          >¥{{ formatBalance(virtualAccountStore.account?.balance) }}</span
                        >
                      </div>
                    </v-card>
                  </v-col>

                  <v-col cols="6">
                    <v-card class="pa-3 info-card rounded-lg">
                      <div class="d-flex justify-space-between align-center">
                        <span class="font-weight-medium">账户ID:</span>
                        <span>{{ virtualAccountStore.account?.accountId || 'N/A' }}</span>
                      </div>
                    </v-card>
                  </v-col>

                  <v-col cols="6">
                    <v-card class="pa-3 info-card rounded-lg">
                      <div class="d-flex justify-space-between align-center">
                        <span class="font-weight-medium">用户ID:</span>
                        <span>{{ virtualAccountStore.account?.userId || 'N/A' }}</span>
                      </div>
                    </v-card>
                  </v-col>

                  <v-col cols="6">
                    <v-card class="pa-3 info-card rounded-lg">
                      <div class="d-flex justify-space-between align-center">
                        <span class="font-weight-medium">创建时间:</span>
                        <span>{{ formatDate(virtualAccountStore.account?.createTime) }}</span>
                      </div>
                    </v-card>
                  </v-col>

                  <v-col cols="6">
                    <v-card class="pa-3 info-card rounded-lg">
                      <div class="d-flex justify-space-between align-center">
                        <span class="font-weight-medium">最后更新:</span>
                        <span>{{ formatDate(virtualAccountStore.account?.lastUpdateTime) }}</span>
                      </div>
                    </v-card>
                  </v-col>
                </v-row>
              </v-card>

              <!-- 快捷功能卡片 - 修改为账号注销和退出登录 -->
              <v-card class="custom-card pa-6 flex-grow-1">
                <div class="d-flex align-center mb-4">
                  <v-icon color="primary" class="mr-2">mdi-account-cog</v-icon>
                  <h3 class="text-h5 font-weight-bold primary--text">账号管理</h3>
                </div>

                <v-row class="justify-center">
                  <v-col cols="12" class="text-center mb-4">
                    <v-btn
                      color="error"
                      class="py-4 d-flex flex-column account-action-btn"
                      height="auto"
                      width="60%"
                      @click="deleteAccount"
                    >
                      <v-icon size="28" class="mb-2">mdi-account-remove</v-icon>
                      <span>退出所有设备</span>
                    </v-btn>
                  </v-col>
                  <v-col cols="12" class="text-center">
                    <v-btn
                      color="primary"
                      class="py-4 d-flex flex-column account-action-btn"
                      height="auto"
                      width="60%"
                      @click="logout"
                      :loading="isLoggingOut"
                    >
                      <v-icon size="28" class="mb-2">mdi-logout</v-icon>
                      <span>退出登录</span>
                    </v-btn>
                  </v-col>
                </v-row>
              </v-card>
            </v-col>
          </v-row>
        </v-container>
      </div>

      <!-- 邮箱认证对话框 -->
      <v-dialog v-model="emailVerifyDialog" max-width="500px" persistent>
        <v-card>
          <v-card-title class="text-h5 primary--text">
            <v-icon color="primary" class="mr-2">mdi-email-check</v-icon>
            邮箱认证
          </v-card-title>

          <v-card-text class="pt-4">
            <!-- 步骤1：发送验证码 -->
            <div v-if="verifyStep === 1">
              <p>
                我们将向您的邮箱 <strong>{{ userStore.user?.email }}</strong> 发送验证码
              </p>
              <v-text-field
                v-model="email"
                label="邮箱地址"
                type="email"
                outlined
                dense
                class="mt-4"
                :disabled="true"
              ></v-text-field>
            </div>

            <!-- 步骤2：输入验证码 -->
            <div v-if="verifyStep === 2">
              <p>
                验证码已发送至 <strong>{{ email }}</strong
                >，请输入收到的6位验证码
              </p>
              <v-otp-input
                v-model="verificationCode"
                length="6"
                type="number"
                outlined
                class="mt-4 justify-center"
              ></v-otp-input>
              <div class="text-caption text--secondary mt-2">
                <span v-if="countdown > 0">{{ countdown }}秒后可重新发送</span>
                <v-btn
                  v-else
                  text
                  small
                  color="primary"
                  @click="sendVerificationCode"
                  :loading="isSendingCode"
                >
                  重新发送验证码
                </v-btn>
              </div>
            </div>

            <!-- 步骤3：验证成功 -->
            <div v-if="verifyStep === 3" class="text-center">
              <v-icon color="success" size="64" class="mb-4">mdi-check-circle</v-icon>
              <p class="text-h6 success--text">邮箱认证成功！</p>
              <p class="text--secondary">您的邮箱已成功验证</p>
            </div>
          </v-card-text>

          <v-card-actions>
            <v-spacer></v-spacer>
            <v-btn
              v-if="verifyStep !== 3"
              text
              @click="closeEmailVerifyDialog"
              :disabled="isVerifying"
            >
              取消
            </v-btn>
            <v-btn
              v-if="verifyStep === 1"
              color="primary"
              @click="sendVerificationCode"
              :loading="isSendingCode"
            >
              发送验证码
            </v-btn>
            <v-btn
              v-if="verifyStep === 2"
              color="primary"
              @click="verifyCode"
              :disabled="verificationCode.length !== 6"
              :loading="isVerifying"
            >
              验证
            </v-btn>
            <v-btn v-if="verifyStep === 3" color="primary" @click="closeEmailVerifyDialog">
              完成
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>

      <!-- 编辑用户信息对话框 -->
      <v-dialog v-model="editDialog" max-width="600px" persistent>
        <v-card>
          <v-card-title class="text-h5 primary--text">
            <v-icon color="primary" class="mr-2">mdi-account-edit</v-icon>
            编辑个人信息
          </v-card-title>

          <v-card-text class="pt-4">
            <v-form>
              <v-text-field
                v-model="editForm.username"
                label="用户名"
                outlined
                dense
                prepend-inner-icon="mdi-account"
                class="mb-4"
                :rules="[
                  v => !!v || '用户名不能为空',
                  v => v.length <= 50 || '用户名长度不能超过50字符',
                ]"
              ></v-text-field>

              <v-text-field
                v-model="editForm.fullName"
                label="真实姓名"
                outlined
                dense
                prepend-inner-icon="mdi-card-account-details"
                class="mb-4"
                :rules="[v => !v || v.length <= 100 || '姓名长度不能超过100字符']"
              ></v-text-field>

              <v-text-field
                v-model="editForm.phone"
                label="手机号"
                outlined
                dense
                prepend-inner-icon="mdi-phone"
                class="mb-4"
                :rules="[v => !v || /^1[3-9]\d{9}$/.test(v) || '请输入正确的手机号格式']"
              ></v-text-field>
            </v-form>
          </v-card-text>

          <v-card-actions>
            <v-spacer></v-spacer>
            <v-btn text @click="editDialog = false" :disabled="editLoading"> 取消 </v-btn>
            <v-btn color="primary" @click="saveUserInfo" :loading="editLoading"> 保存 </v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>

      <!-- 修改密码对话框 -->
      <v-dialog v-model="changePasswordDialog" max-width="600px" persistent>
        <v-card>
          <v-card-title class="text-h5 primary--text">
            <v-icon color="primary" class="mr-2">mdi-lock-reset</v-icon>
            修改密码
          </v-card-title>

          <v-card-text class="pt-4">
            <v-form ref="passwordForm" v-model="passwordFormValid">
              <v-row>
                <v-col cols="12">
                  <v-text-field
                    v-model="passwordFormData.currentPassword"
                    label="当前密码"
                    type="password"
                    outlined
                    dense
                    :rules="[v => !!v || '请输入当前密码']"
                  ></v-text-field>
                </v-col>

                <v-col cols="12">
                  <v-text-field
                    v-model="passwordFormData.newPassword"
                    label="新密码"
                    type="password"
                    outlined
                    dense
                    :rules="[v => !!v || '请输入新密码', v => v.length >= 6 || '密码长度至少6位']"
                  ></v-text-field>
                </v-col>

                <v-col cols="12">
                  <v-text-field
                    v-model="passwordFormData.confirmPassword"
                    label="确认新密码"
                    type="password"
                    outlined
                    dense
                    :rules="[
                      v => !!v || '请确认新密码',
                      v => v === passwordFormData.newPassword || '两次输入的密码不一致',
                    ]"
                  ></v-text-field>
                </v-col>
              </v-row>
            </v-form>
          </v-card-text>

          <v-card-actions>
            <v-spacer></v-spacer>
            <v-btn text @click="closeChangePasswordDialog" :disabled="isChangingPassword">
              取消
            </v-btn>
            <v-btn
              color="primary"
              @click="changePassword"
              :loading="isChangingPassword"
              :disabled="!passwordFormValid"
            >
              确认修改
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>

      <!-- 提示信息弹窗 -->
      <v-snackbar v-model="snackbar.show" :color="snackbar.color" timeout="3000">
        {{ snackbar.message }}
      </v-snackbar>
    </v-main>
  </v-app>
</template>

<script setup lang="ts">
  import { ref, onMounted } from 'vue'
  import { useRouter } from 'vue-router'
  import { useUserStore } from '@/stores/user'
  import { useVirtualAccountStore, type VirtualAccount } from '@/stores/virtualaccount'

  // 路由
  const router = useRouter()

  // 状态管理
  const userStore = useUserStore()
  const virtualAccountStore = useVirtualAccountStore()
  const isLoading = ref(true)
  const isLoggingOut = ref(false)

  // 邮箱认证相关状态
  const emailVerifyDialog = ref(false)
  const verifyStep = ref(1) // 1: 发送验证码, 2: 输入验证码, 3: 验证成功
  const email = ref('')
  const verificationCode = ref('')
  const isSendingCode = ref(false)
  const isVerifying = ref(false)
  const countdown = ref(0)

  // 提示信息
  const snackbar = ref({
    show: false,
    message: '',
    color: 'success',
  })

  // 修改密码相关状态
  const changePasswordDialog = ref(false)
  const passwordFormValid = ref(false)
  const isChangingPassword = ref(false)
  const passwordFormData = ref({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  })

  // 表单引用
  const passwordForm = ref(null)

  // 交易记录数据
  const transactions = ref([
    { title: '二手教材', amount: 50.0, date: '2024-03-15 14:30' },
    { title: '校园卡充值', amount: -100.0, date: '2024-03-10 09:15' },
    { title: '代取快递', amount: 15.0, date: '2024-03-05 17:45' },
    { title: '校园周边', amount: -35.0, date: '2024-02-28 13:20' },
  ])

  // 计算信用评分百分比
  const calculateCreditScorePercentage = (score: number | undefined) => {
    if (!score) return 0
    const maxScore = 5 // 假设满分是5分
    return (score / maxScore) * 100
  }

  // 手机号脱敏处理
  const maskPhone = (phone: string) => {
    if (!phone || phone.length < 7) return phone
    return phone.replace(/(\d{3})\d{4}(\d{4})/, '$1****$2')
  }

  // 格式化余额显示
  const formatBalance = (balance: number | undefined) => {
    if (balance === undefined || balance === null) return '0.00'
    return balance.toFixed(2)
  }

  // 格式化日期显示
  const formatDate = (dateString: string | undefined) => {
    if (!dateString) return 'N/A'
    try {
      const date = new Date(dateString)
      return date.toLocaleDateString('zh-CN')
    } catch {
      return 'N/A'
    }
  }

  // 获取账户详情
  const fetchAccountDetails = async () => {
    try {
      await virtualAccountStore.fetchAccountDetails()
    } catch (error) {
      console.error('获取账户详情失败:', error)
    }
  }

  // 重试加载
  const retryLoading = async () => {
    isLoading.value = true
    virtualAccountStore.error = null
    await loadData()
  }

  // 加载数据
  const loadData = async () => {
    // 防止重复调用
    if (userStore.isLoadingProfile) {
      console.log('User profile loading in progress, skipping...')
      return
    }

    try {
      // 使用新的getUserProfile方法获取完整用户信息
      const result = await userStore.getUserProfile()
      if (result.success && result.data?.virtualAccount) {
        // 如果用户信息中包含虚拟账户信息，直接使用
        virtualAccountStore.account = {
          accountId: result.data.virtualAccount.accountId,
          userId: result.data.userId,
          balance: result.data.virtualAccount.balance,
          lastUpdateTime: result.data.virtualAccount.createdAt,
          createTime: result.data.virtualAccount.createdAt,
        } as VirtualAccount
      } else {
        // 否则单独获取虚拟账户信息
        await virtualAccountStore.fetchBalance()
      }
    } catch (error) {
      console.error('加载用户信息失败:', error)
    } finally {
      isLoading.value = false
    }
  }

  // 处理邮箱认证按钮点击
  const handleVerifyEmail = async () => {
    if (userStore.user?.emailVerified) {
      // 已认证，显示提示
      showSnackbar('您的邮箱已通过认证', 'info')
      return
    }

    // 未认证，打开认证对话框
    email.value = userStore.user?.email || ''
    verifyStep.value = 1
    emailVerifyDialog.value = true
  }

  // 发送验证码
  const sendVerificationCode = async () => {
    if (!userStore.user?.email) {
      showSnackbar('邮箱地址不存在', 'error')
      return
    }

    isSendingCode.value = true
    try {
      const result = await userStore.sendVerificationCode(userStore.user.email)

      if (result.success) {
        showSnackbar('验证码已发送，请查收邮箱', 'success')
        verifyStep.value = 2
        startCountdown()
      } else {
        showSnackbar(result.message, 'error')
      }
    } catch (error) {
      console.error('发送验证码失败:', error)
      showSnackbar('发送验证码失败，请重试', 'error')
    } finally {
      isSendingCode.value = false
    }
  }

  // 验证验证码
  const verifyCode = async () => {
    if (verificationCode.value.length !== 6) {
      showSnackbar('请输入6位验证码', 'error')
      return
    }

    isVerifying.value = true
    try {
      const result = await userStore.verifyCode(verificationCode.value)

      if (result.success) {
        showSnackbar('邮箱认证成功', 'success')
        verifyStep.value = 3
        // 更新本地用户信息
        await userStore.getUserProfile()
      } else {
        showSnackbar(result.message, 'error')
      }
    } catch (error) {
      console.error('验证码验证失败:', error)
      showSnackbar('验证失败，请重试', 'error')
    } finally {
      isVerifying.value = false
    }
  }

  // 倒计时计时器
  const startCountdown = () => {
    countdown.value = 60
    const timer = setInterval(() => {
      countdown.value--
      if (countdown.value <= 0) {
        clearInterval(timer)
      }
    }, 1000)
  }

  // 显示提示信息
  const showSnackbar = (message: string, color: 'success' | 'error' | 'info' = 'success') => {
    snackbar.value = {
      show: true,
      message,
      color,
    }
  }

  // 关闭对话框时的清理
  const closeEmailVerifyDialog = () => {
    emailVerifyDialog.value = false
    verifyStep.value = 1
    verificationCode.value = ''
    countdown.value = 0
  }

  // 编辑用户信息对话框状态
  const editDialog = ref(false)
  const editForm = ref({
    username: '',
    fullName: '',
    phone: '',
  })
  const editLoading = ref(false)

  // 新增按钮处理函数
  const editInfo = () => {
    // 填充当前用户信息到编辑表单
    editForm.value = {
      username: userStore.user?.username || '',
      fullName: userStore.user?.fullName || '',
      phone: userStore.user?.phone || '',
    }
    editDialog.value = true
  }

  // 保存用户信息
  const saveUserInfo = async () => {
    editLoading.value = true
    try {
      const result = await userStore.updateUserProfile(editForm.value)

      if (result.success) {
        showSnackbar('用户信息更新成功', 'success')
        editDialog.value = false
        // 刷新用户信息
        await userStore.getUserProfile()
      } else {
        showSnackbar(result.message, 'error')
      }
    } catch (error) {
      console.error('更新用户信息失败:', error)
      showSnackbar('更新失败，请重试', 'error')
    } finally {
      editLoading.value = false
    }
  }

  // 打开修改密码对话框
  const openChangePasswordDialog = () => {
    changePasswordDialog.value = true
  }

  // 关闭修改密码对话框
  const closeChangePasswordDialog = () => {
    changePasswordDialog.value = false
    if ((passwordForm.value as any)?.reset) {
      ;(passwordForm.value as any).reset()
    }
    passwordFormData.value = {
      currentPassword: '',
      newPassword: '',
      confirmPassword: '',
    }
  }

  // 修改密码
  const changePassword = async () => {
    if (!passwordFormValid.value) return

    isChangingPassword.value = true
    try {
      const result = await userStore.changePassword(passwordFormData.value)

      if (result.success) {
        showSnackbar('密码修改成功', 'success')
        changePasswordDialog.value = false
      } else {
        showSnackbar(result.message, 'error')
      }
    } catch (error) {
      console.error('修改密码失败:', error)
      showSnackbar('修改失败，请重试', 'error')
    } finally {
      isChangingPassword.value = false
    }
  }

  // 账号注销
  const deleteAccount = () => {
    console.log('账号注销按钮被点击')
    // 这里可以添加账号注销的逻辑
  }

  // 退出登录 - 使用user.ts中的logout方法
  const logout = async () => {
    isLoggingOut.value = true
    try {
      await userStore.logout()
      showSnackbar('退出登录成功', 'success')
      // 跳转到登录页面
      router.push('/')
    } catch (error) {
      console.error('退出登录失败:', error)
      showSnackbar('退出登录失败，请重试', 'error')
    } finally {
      isLoggingOut.value = false
    }
  }

  onMounted(async () => {
    await loadData()
  })
</script>

<style scoped>
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

  /* 图标和标题放大 */
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

  /* 主卡片样式 */
  .custom-card {
    background-color: #def0ff !important;
    border-radius: 12px;
    transition: all 0.3s ease;
    box-shadow: none !important;
    border: 1px solid rgba(255, 255, 255, 0.3);
  }

  .custom-card:hover {
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12) !important;
    transform: translateY(-2px);
  }

  .info-card {
    background-color: rgba(255, 255, 255, 0.7) !important;
    border: 1px solid rgba(255, 255, 255, 0.5);
    transition: all 0.2s ease;
    box-shadow: none !important; /* 默认无阴影 */
  }

  .info-card:hover {
    background-color: rgba(255, 255, 255, 0.9) !important;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1) !important; /* 悬停时添加阴影 */
  }

  .info-card-1 {
    background-color: rgba(255, 255, 255, 0.7) !important;
    border: 1px solid rgba(255, 255, 255, 0.5);
    transition: all 0.2s ease;
    box-shadow: none !important; /* 默认无阴影 */
    overflow: hidden !important;
  }

  .info-card-1:hover {
    background-color: rgba(255, 255, 255, 0.9) !important;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1) !important; /* 悬停时添加阴影 */
  }

  /* 余额卡片样式 - 修改为默认无阴影 */
  .balance-card {
    background-color: rgba(255, 255, 255, 0.8) !important;
    border: 1px solid rgba(255, 255, 255, 0.6);
    box-shadow: none !important; /* 默认极阴影 */
  }

  .balance-card:hover {
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1) !important; /* 悬停时添加阴影 */
  }

  .v-application {
    background-color: #f6fcff !important; /* 使用您喜欢的颜色 */
  }

  /* 信用评分卡片样式 - 修改为默认无阴影 */
  .credit-score-card {
    background: linear-gradient(
      135deg,
      rgba(255, 255, 255, 0.9),
      rgba(255, 255, 255, 0.7)
    ) !important;
    border: 1px solid rgba(255, 255, 255, 0.6);
    box-shadow: none !important; /* 默认无阴影 */
  }

  .credit-score-card:hover {
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1) !important; /* 悬停时添加阴影 */
  }

  .user-avatar-container {
    width: 60px; /* 调小尺寸 */
    height: 60px; /* 调小尺寸 */
    display: flex;
    align-items: center;
    justify-content: center;
  }

  /* 用户头像图片样式 */
  .user-avatar-image {
    width: 100%;
    height: 100%;
    object-fit: cover; /* 保持图片比例并填充容器 */
    border-radius: 4px; /* 可选：添加轻微圆角 */
  }

  /* 账号管理按钮样式 */
  .account-action-btn {
    border: 1px solid rgba(255, 255, 255, 0.3);
    transition: all 0.3s ease;
  }

  .account-action-btn:hover {
    box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2) !important;
    transform: translateY(-2px);
  }

  /* 补充PC端特有样式 */
  .pc-user-detail {
    min-height: 100vh;
    display: flex;
    flex-direction: column;
  }

  .ml-auto {
    margin-left: auto;
  }

  .logout-btn {
    margin-left: auto;
  }

  /* 修改后的按钮样式 - 垂直排列并水平居中 */
  .vertical-button-container {
    display: flex;
    flex-direction: column;
    align-items: center; /* 水平居中 */
    gap: 8px;
    width: 100%;
  }

  .action-btn {
    background-color: #ffdfdf !important;
    color: white !important;
    width: 140px !important; /* 固定宽度 */
    max-width: 140px;
    min-width: 140px;
    margin: 0 auto;
    display: flex;
    align-items: center;
    justify-content: center;
    box-shadow: none !important;
  }

  .action-btn:hover {
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15) !important;
    transform: translateY(-2px);
    background-color: #ffc9c9 !important;
  }

  /* 邮箱认证按钮样式调整 */
  .verify-btn {
    background-color: #4caf50 !important;
  }

  .verify-btn:hover {
    background-color: #45a049 !important;
  }

  /* 编辑信息按钮样式 */
  .edit-btn {
    background-color: #2196f3 !important;
  }

  .edit-btn:hover {
    background-color: #1976d2 !important;
  }

  /* OTP输入框居中 */
  .v-otp-input {
    justify-content: center;
  }

  .v-otp-input :deep(.v-text-field) {
    margin: 0 4px;
  }

  /* 响应式调整 */
  @media (max-width: 960px) {
    .action-btn {
      max-width: 120px;
      font-size: 14px;
    }
  }
</style>
