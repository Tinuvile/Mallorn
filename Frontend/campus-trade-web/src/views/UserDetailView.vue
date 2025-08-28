<template>
  <v-app>
    <header class="navbar">
      <span class="icon">
        <svg width="48px" height="48px" stroke-width="1.5" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" color="#ffffff">
          <circle cx="12" cy="12" r="10" stroke="#ffffff" stroke-width="1.5"></circle>
          <path d="M7.63262 3.06689C8.98567 3.35733 9.99999 4.56025 9.99999 6.00007C9.99999 7.65693 8.65685 9.00007 6.99999 9.00007C5.4512 9.00007 4.17653 7.82641 4.01685 6.31997" stroke="#ffffff" stroke-width="1.5"></path>
          <path d="M22 13.0505C21.364 12.4022 20.4793 12 19.5 12C17.567 12 16 13.567 16 15.5C16 17.2632 17.3039 18.7219 19 18.9646" stroke="#ffffff" stroke-width="1.5" ></path>
          <path d="M14.5 8.51L14.51 8.49889" stroke="#ffffff" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
          <path d="M10 17C11.1046 17 12 16.1046 12 15C12 13.8954 11.1046 13 10 13C8.89543 13 8 13.8954 8 15C8 16.1046 8.89543 17 10 17Z" stroke="#ffffff" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
        </svg>
      </span>
      <span class="title">Campus Secondhand</span>
      <!-- 新增退出按钮 -->
      <v-btn 
        class="logout-btn ml-auto"
        icon
        to="/"
      >
        <v-icon color="black">mdi-exit-to-app</v-icon>
      </v-btn>
    </header>
    <v-main class="grey lighten-4" style="min-height: 100vh;">
      <div class="pc-user-detail">
        <v-container class="px-4" style="max-width: 1600px">
          <div class="user-info-header">
            <h1 class="text-h4 font-weight-bold text-center primary--text mb-2">个人信息中心</h1>
            <v-divider thickness="4" color="#ffa5a5" class="mb-6"></v-divider>
          </div>
          <!-- 加载状态 -->
          <v-row v-if="isLoading" justify="center" align="center" class="fill-height" style="height: 70vh">
            <v-col cols="12" class="text-center">
              <v-progress-circular indeterminate color="primary" size="64"></v-progress-circular>
              <p class="mt-4 grey--text">加载用户信息中...</p>
            </v-col>
          </v-row>

          <!-- 错误状态 -->
          <v-row v-else-if="virtualAccountStore.error" justify="center" align="center" class="fill-height" style="height: 70vh">
            <v-col cols="12" md="6" class="text-center">
              <v-icon color="error" size="64" class="mb-4">mdi-alert-circle</v-icon>
              <p class="error--text mb-4 text-h6">{{ virtualAccountStore.error }}</p>
              <v-btn @click="retryLoading" color="primary" large>
                重试
              </v-btn>
            </v-col>
          </v-row>

          <!-- 主内容区 -->
          <v-row v-else class="mt-6" style="align-items: stretch;">
            <!-- 左侧用户信息区 -->
            <v-col cols="12" md="4" class="pr-md-4 d-flex">
              <v-card class="custom-card pa-6 d-flex flex-column" style="flex: 1;">
                <div class="d-flex align-center mb-6">
                <div class="user-avatar-container mr-4">
                  <img 
                    src="/images/userphoto.png" 
                    alt="用户头像"
                    class="user-avatar-image"
                  >
                </div>
                  <div>
                    <h2 class="text-h5 font-weight-bold">{{ userStore.user?.fullName || userStore.user?.username || '未命名用户' }}</h2>
                    <p class="text--secondary">计算机科学与技术学院</p>
                  </div>
                </div>

                <v-row class="mt-4 flex-grow-1" style="align-content: flex-start;">
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
                        <span>{{ userStore.user?.phone ? maskPhone(userStore.user.phone) : 'N/A' }}</span>
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
                    class="action-btn edit-btn mb-2"
                    @click="editInfo"
                    block
                  >
                    编辑信息
                  </v-btn>
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
              color="#4d606e" 
              class="mx-md-2 my-6 d-none d-md-flex"
            ></v-divider>

            <!-- 中间交易记录区 - 修改部分 -->
            <v-col cols="12" md="4" class="px-md-4 d-flex">
              <v-card class="custom-card pa-6 d-flex flex-column" style="flex: 1;">
                <div class="d-flex align-center mb-4">
                  <v-icon color="primary" class="mr-2">mdi-history</v-icon>
                  <h3 class="text-h5 font-weight-bold primary--text">交易记录</h3>
                </div>
                
                <v-row class="flex-grow-1" style="align-content: flex-start;">
                  <!-- 交易记录列表 - 修改为显示已完成订单 -->
                  <v-col cols="12" v-for="(order, index) in completedOrders" :key="index">
                    <v-card class="pa-3 info-card rounded-lg" :class="{ 'dashed-placeholder': !order.id }">
                      <div v-if="order.id">
                        <div class="d-flex justify-space-between align-center mb-1">
                          <span class="font-weight-medium text-truncate">{{ order.productName }}</span>
                          <span class="green--text">
                            ¥{{ order.totalAmount.toFixed(2) }}
                          </span>
                        </div>
                        <div class="d-flex justify-space-between align-center text-caption text--secondary">
                          <span>{{ formatOrderDate(order.orderDate) }}</span>
                          <v-chip x-small color="success" text-color="white">
                            已完成
                          </v-chip>
                        </div>
                      </div>
                      <div v-else class="d-flex justify-center align-center fill-height">
                        <v-icon color="grey lighten-1">mdi-cart</v-icon>
                        <span class="ml-2 grey--text text--lighten-1">{{ order.productName }}</span>
                      </div>
                    </v-card>
                  </v-col>
                </v-row>
                
                <!-- 信用评分区域 -->
                <v-card class="pa-4 mt-4 credit-score-card rounded-lg">
                  <div class="d-flex justify-space-between align-center mb-3">
                    <span class="font-weight-medium">信用评分:</span>
                    <div class="d-flex align-center">
                      <span class="text-h4 font-weight-bold mr-2">{{ userStore.user?.creditScore || 'N/A' }}</span>
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
              color="#4d606e" 
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
                        <span class="text-h4 font-weight-bold primary--text">¥{{ formatBalance(virtualAccountStore.account?.balance) }}</span>
                      </div>
                    </v-card>
                  </v-col>
                  
                  <v-col cols="6">
                    <v-card class="pa-3 info-card-1 rounded-lg">
                      <div class="d-flex justify-space-between align-center">
                        <span class="font-weight-medium">账户ID:</span>
                        <span>{{ virtualAccountStore.account?.accountId || 'N/A' }}</span>
                      </div>
                    </v-card>
                  </v-col>
                  
                  <v-col cols="6">
                    <v-card class="pa-3 info-card-1 rounded-lg">
                      <div class="d-flex justify-space-between align-center">
                        <span class="font-weight-medium">用户ID:</span>
                        <span>{{ virtualAccountStore.account?.userId || 'N/A' }}</span>
                      </div>
                    </v-card>
                  </v-col>
                  
                  <v-col cols="6">
                    <v-card class="pa-3 info-card-1 rounded-lg">
                      <div class="d-flex justify-space-between align-center">
                        <span class="font-weight-medium">创建时间:</span>
                        <span>{{ formatDate(virtualAccountStore.account?.createTime) }}</span>
                      </div>
                    </v-card>
                  </v-col>
                  
                  <v-col cols="6">
                    <v-card class="pa-3 info-card-1 rounded-lg">
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
              <p>我们将向您的邮箱 <strong>{{ userStore.user?.email }}</strong> 发送验证码</p>
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
              <p>验证码已发送至 <strong>{{ email }}</strong>，请输入收到的6位验证码</p>
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
              <p class="text-h6 success--text">邮箱认证成功!</p>
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
            <v-btn 
              v-if="verifyStep === 3"
              color="primary" 
              @click="closeEmailVerifyDialog"
            >
              完成
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
import { ref, onMounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useUserStore } from '@/stores/user';
import { useVirtualAccountStore } from '@/stores/virtualaccount';
import { useOrderStore, OrderStatus, type OrderListResponse } from '@/stores/order';

// 路由
const router = useRouter();

// 状态管理
const userStore = useUserStore();
const virtualAccountStore = useVirtualAccountStore();
const orderStore = useOrderStore();
const isLoading = ref(true);
const isLoggingOut = ref(false);

// 邮箱认证相关状态
const emailVerifyDialog = ref(false);
const verifyStep = ref(1); // 1: 发送验证码, 2: 输入验证码, 3: 验证成功
const email = ref('');
const verificationCode = ref('');
const isSendingCode = ref(false);
const isVerifying = ref(false);
const countdown = ref(0);

// 提示信息
const snackbar = ref({
  show: false,
  message: '',
  color: 'success'
});

// 获取已完成订单
const completedOrders = computed(() => {
  // 从订单store获取已完成状态的订单
  const orders = orderStore.getOrdersByStatus(OrderStatus.COMPLETED);
  
  // 只取最近的4个订单
  const recentOrders = orders.slice(0, 4);
  
  // 创建一个空订单对象来填充不足的部分
  const emptyOrder: OrderListResponse = {
    id: 0,
    orderNumber: '',
    orderDate: '',
    status: OrderStatus.COMPLETED,
    productName: '暂无交易记录',
    productImage: '',
    totalAmount: 0,
    quantity: 0
  };
  
  // 如果不足4个，用空订单对象填充
  while (recentOrders.length < 4) {
    recentOrders.push({ ...emptyOrder });
  }
  
  return recentOrders;
});

// 计算信用评分百分比
const calculateCreditScorePercentage = (score: number | undefined) => {
  if (!score) return 0;
  const maxScore = 5; // 假设满分是5分
  return (score / maxScore) * 100;
};

// 手机号脱敏处理
const maskPhone = (phone: string) => {
  if (!phone || phone.length < 7) return phone;
  return phone.replace(/(\d{3})\d{4}(\d{4})/, '$1****$2');
};

// 格式化余额显示
const formatBalance = (balance: number | undefined) => {
  if (balance === undefined || balance === null) return '0.00';
  return balance.toFixed(2);
};

// 格式化日期显示
const formatDate = (dateString: string | undefined) => {
  if (!dateString) return 'N/A';
  try {
    const date = new Date(dateString);
    return date.toLocaleDateString('zh-CN');
  } catch {
    return 'N/A';
  }
};

// 格式化订单日期 - 修复类型错误
const formatOrderDate = (dateString: string | null | undefined) => {
  if (!dateString) return '日期未知';
  try {
    const date = new Date(dateString);
    return date.toLocaleDateString('zh-CN');
  } catch {
    return '日期未知';
  }
};

// 获取账户详情
const fetchAccountDetails = async () => {
  try {
    await virtualAccountStore.fetchAccountDetails();
  } catch (error) {
    console.error('获取账户详情失败:', error);
  }
};

// 重试加载
const retryLoading = async () => {
  isLoading.value = true;
  virtualAccountStore.error = null;
  await loadData();
};

// 加载数据
const loadData = async () => {
  try {
    await userStore.fetchUserInfo('current');
    if (userStore.user) {
      await virtualAccountStore.fetchBalance();
      // 加载用户订单
      await orderStore.getUserOrders({ 
        status: OrderStatus.COMPLETED,
        pageSize: 4 
      });
    }
  } catch (error) {
    console.error('加载用户信息失败:', error);
  } finally {
    isLoading.value = false;
  }
};

// 处理邮箱认证按钮点击
const handleVerifyEmail = async () => {
  if (userStore.user?.emailVerified) {
    // 已认证，显示提示
    showSnackbar('您的邮箱已通过认证', 'info');
    return;
  }

  // 未认证，打开认证对话框
  email.value = userStore.user?.email || '';
  verifyStep.value = 1;
  emailVerifyDialog.value = true;
};

// 发送验证码
const sendVerificationCode = async () => {
  if (!userStore.user?.email) {
    showSnackbar('邮箱地址不存在', 'error');
    return;
  }

  isSendingCode.value = true;
  try {
    const result = await userStore.sendVerificationCode(userStore.user.email);
    
    if (result.success) {
      showSnackbar('验证码已发送，请查收邮箱', 'success');
      verifyStep.value = 2;
      startCountdown();
    } else {
      showSnackbar(result.message, 'error');
    }
  } catch (error) {
    console.error('发送验证码失败:', error);
    showSnackbar('发送验证码失败，请重试', 'error');
  } finally {
    isSendingCode.value = false;
  }
};

// 验证验证码
const verifyCode = async () => {
  if (verificationCode.value.length !== 6) {
    showSnackbar('请输入6位验证码', 'error');
    return;
  }

  isVerifying.value = true;
  try {
    const result = await userStore.verifyCode(verificationCode.value);
    
    if (result.success) {
      showSnackbar('邮箱认证成功', 'success');
      verifyStep.value = 3;
      // 更新本地用户信息
      await userStore.fetchUserInfo(userStore.user?.username || '');
    } else {
      showSnackbar(result.message, 'error');
    }
  } catch (error) {
    console.error('验证码验证失败:', error);
    showSnackbar('验证失败，请重试', 'error');
  } finally {
    isVerifying.value = false;
  }
};

// 倒计时计时器
const startCountdown = () => {
  countdown.value = 60;
  const timer = setInterval(() => {
    countdown.value--;
    if (countdown.value <= 0) {
      clearInterval(timer);
    }
  }, 1000);
};

// 显示提示信息
const showSnackbar = (message: string, color: 'success' | 'error' | 'info' = 'success') => {
  snackbar.value = {
    show: true,
    message,
    color
  };
};

// 关闭对话框时的清理
const closeEmailVerifyDialog = () => {
  emailVerifyDialog.value = false;
  verifyStep.value = 1;
  verificationCode.value = '';
  countdown.value = 0;
};

// 新增按钮处理函数
const editInfo = () => {
  console.log('编辑信息按钮被点击');
  // 这里可以添加编辑信息的逻辑
};

// 账号注销
const deleteAccount = () => {
  console.log('账号注销按钮被点击');
  // 这里可以添加账号注销的逻辑
};

// 退出登录 - 使用user.ts中的logout方法
const logout = async () => {
  isLoggingOut.value = true;
  try {
    await userStore.logout();
    showSnackbar('退出登录成功', 'success');
    // 跳转到登录页面
    router.push('/');
  } catch (error) {
    console.error('退出登录失败:', error);
    showSnackbar('退出登录失败，请重试', 'error');
  } finally {
    isLoggingOut.value = false;
  }
};

onMounted(async () => {
  await loadData();
});
</script>

<style scoped>
.v-application {
  background-color: #f6fcff !important;
  min-height: 100vh;
  overflow: hidden !important;
}

.v-application--wrap {
  min-height: 100vh;
  background-color: #f6fcff;
  overflow: hidden !important;
}

/* 隐藏所有滚动条 - 通用解决方案 */
::-webkit-scrollbar {
  width: 0 !important;
  height: 0 !important;
  display: none !important;
  background: transparent !important;
}

/* 隐藏Edge和IE的滚动条 */
body, html, .v-application, .v-application--wrap, .v-main, .grey.lighten-4 {
  -ms-overflow-style: none !important; /* IE and Edge */
  scrollbar-width: none !important; /* Firefox */
  overflow: -moz-scrollbars-none !important; /* 旧版Firefox */
}

/* 隐藏特定容器的滚动条 */
.pc-user-detail, .v-container, .v-row, .v-col, .custom-card {
  -ms-overflow-style: none !important;
  scrollbar-width: none !important;
}

.pc-user-detail::-webkit-scrollbar,
.v-container::-webkit-scrollbar,
.v-row::-webkit-scrollbar,
.v-col::-webkit-scrollbar,
.custom-card::-webkit-scrollbar {
  display: none !important;
  width: 0 !important;
  height: 0 !important;
}

/* 导航栏样式 */
.navbar {
  position: relative; 
  width: 100%;
  height: 60px;
  margin:  0;
  padding: 0 16px;
  background-color:#cadefc;
  box-shadow: 0 2px 4px rgba(0, 0, 极 0, 0.1);
  z-index: 1000; 
  display: flex;
  align-items: center;
  justify-content:flex-start;
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
  overflow: hidden !important;
}

.custom-card:hover {
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12) !important;
  transform: translateY(-2px);
}

.info-card {
  background-color: rgba(255, 255, 255, 0.7) !important;
  border: 1px solid rgba(255, 255, 255, 0.5);
  transition: all 0.2s ease;
  box-shadow: none !important;
  min-height: 80px;
  display: flex;
  flex-direction: column;
  justify-content: center;
  overflow: hidden !important;
}

.info-card:hover {
  background-color: rgba(255, 255, 255, 0.9) !important;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1) !important;
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

/* 虚线占位符样式 */
.dashed-placeholder {
  border: 2px dashed #c4c4c4 !important;
  background-color: rgba(249, 249, 249, 0.7) !important;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden !important;
  min-height: 80px;
  transition: all 0.3s ease;
}

.info-card:not(.dashed-placeholder) {
  border: 1px solid rgba(255, 255, 255, 0.5) !important;
  background-color: rgba(255, 255, 255, 0.7) !important;
  transition: all 0.3s ease;
}

/* 虚线框悬停效果 */
.dashed-placeholder:hover {
  border-color: #a0a0a0 !important;
  background-color: rgba(249, 249, 249, 0.9) !important;
}

/* 实线框悬停效果 */
.info-card:not(.dashed-placeholder):hover {
  background-color: rgba(255, 255, 255, 0.9) !important;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1) !important;
}

/* 虚线框内容样式优化 */
.dashed-placeholder .d-flex {
  flex-direction: column;
  color: #a0a0a0;
}

.dashed-placeholder .v-icon {
  font-size: 24px;
  margin-bottom: 8px;
}


/* 余额卡片样式 */
.balance-card {
  background-color: rgba(255, 255, 255, 0.8) !important;
  border: 1px solid rgba(255, 255, 255, 0.6);
  box-shadow: none !important;
  overflow: hidden !important;
}

.balance-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1) !important;
}

/* 信用评分卡片样式 */
.credit-score-card {
  background: linear-gradient(135deg, rgba(255, 255, 255, 0.9), rgba(255, 255, 255, 0.7)) !important;
  border: 1px solid rgba(255, 255, 255, 0.6);
  box-shadow: none !important;
  overflow: hidden !important;
}

.credit-score-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1) !important;
}

.user-avatar-container {
  width: 60px;
  height: 60px;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden !important;
}

/* 用户头像图片样式 */
.user-avatar-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 4px;
}

/* 账号管理按钮样式 */
.account-action-btn {
  border: 1px solid rgba(255, 255, 255, 0.3);
  transition: all 0.3s ease;
}

.account-action-btn:hover {
  box-shadow: 0 6px 极16px rgba(0, 0, 0, 0.2) !important;
  transform: translateY(-2px);
}

/* 补充PC端特有样式 */
.pc-user-detail {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  overflow: hidden !important;
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
  align-items: center;
  gap: 8px;
  width: 100%;
}

.action-btn {
  background-color: #ffdfdf !important;
  color: white !important;
  width: 140px !important;
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
  background-color: #4CAF50 !important;
}

.verify-btn:hover {
  background-color: #45a049 !important;
}

/* OTP输入框居中 */
.v-otp-input {
  justify-content: center;
}

.v-otp-input :deep(.v-text-field) {
  margin: 0 4px;
}

/* 文本截断 */
.text-trunc极ate {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 120px;
}

/* 响应式调整 */
@media (max-width: 960px) {
  .action-btn {
    max-width: 120px;
    font-size: 14px;
  }
  
  .text-truncate {
    max-width: 100px;
  }
}

@media (max-width: 600px) {
  .text-truncate {
    max-width: 80px;
  }
}
</style>