<template>
  <v-app>
    <div class="profile-container">
      <!-- Navigation Bar -->
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
      </header>

      <!-- 退出按钮 -->
      <v-btn 
        fab
        to="/"
        class="logout-btn"
        color="white"
        size="56"
      >
        <v-icon size="32" color="red-darken-1">mdi-logout-variant</v-icon>
      </v-btn>

      <!-- 加载状态 -->
      <v-overlay
        :model-value="isLoading"
        class="align-center justify-center"
      >
        <v-progress-circular
          color="primary"
          indeterminate
          size="64"
        ></v-progress-circular>
      </v-overlay>

      <!-- 主内容区 -->
      <template v-if="!isLoading">
        <!-- 背景装饰图 -->
        <div class="decorative-bg">
          <img src="/images/UserBack1.png" class="top-image" alt="Decorative image">
          <img src="/images/UserBack2.png" class="bottom-image" alt="Decorative image">
        </div>
        
        <!-- 用户信息卡片 -->
        <v-container class="content-wrapper py-2">
          <v-row class="row-wrapper">
            <v-col cols="12" md="8" lg="6" class="col-wrapper">
              <v-card class="user-profile-card" elevation="12">
                <v-card-title class="text-center pt-6">
                  <v-avatar size="120" color="indigo-lighten-4" class="profile-avatar">
                    <v-icon size="64" color="indigo-darken-3">mdi-account-circle</v-icon>
                  </v-avatar>
                </v-card-title>
                
                <v-card-text class="text-center px-6 pb-0">
                  <div class="user-info-column">
                    <p class="text-h4 font-weight-bold mb-6">个人信息</p>
                    <v-divider class="my-4"></v-divider>
                    
                    <!-- 未登录提示 -->
                    <div v-if="!userStore.user" class="text-center py-8">
                      <v-icon size="64" color="grey-lighten-1" class="mb-4">mdi-account-off</v-icon>
                      <h3 class="text-h5 mb-4">未登录</h3>
                      <p class="text-body-1 mb-6">请登录后查看个人信息</p>
                      <v-btn 
                        color="primary" 
                        to="/login"
                        size="large"
                      >
                        <v-icon start>mdi-login</v-icon>
                        前往登录
                      </v-btn>
                    </div>

                    <!-- 已登录显示用户信息 -->
                    <template v-else>
                      <v-list lines="two" density="comfortable" class="text-left">
                        <v-list-item prepend-icon="mdi-account" title="用户名">
                          <template v-slot:append>
                            <span class="text-body-1">{{ userStore.user.username }}</span>
                          </template>
                        </v-list-item>
                        
                        <v-list-item prepend-icon="mdi-identifier" title="用户ID">
                          <template v-slot:append>
                            <span class="text-body-1">{{ userStore.user.userId }}</span>
                          </template>
                        </v-list-item>
                        
                        <v-list-item prepend-icon="mdi-card-account-details" title="学号">
                          <template v-slot:append>
                            <span class="text-body-1">{{ userStore.user.studentId || '未填写' }}</span>
                          </template>
                        </v-list-item>
                        
                        <v-list-item prepend-icon="mdi-email" title="邮箱">
                          <template v-slot:append>
                            <span class="text-body-1">{{ userStore.user.email }}</span>
                            <v-chip 
                              :color="userStore.user.emailVerified ? 'success' : 'warning'" 
                              size="small" 
                              class="ml-2"
                            >
                              {{ userStore.user.emailVerified ? '已认证' : '未认证' }}
                            </v-chip>
                          </template>
                        </v-list-item>
                        
                        <v-list-item prepend-icon="mdi-phone" title="电话">
                          <template v-slot:append>
                            <span class="text-body-1">{{ userStore.user.phone || '未填写' }}</span>
                          </template>
                        </v-list-item>
                      </v-list>
                      
                      <!-- 信用评分 -->
                      <div class="credit-score-container mt-6" v-if="userStore.user.creditScore !== undefined">
                        <div class="d-flex justify-center align-center">
                          <v-progress-circular
                            :model-value="userStore.user.creditScore"
                            :color="creditScoreColor"
                            :size="120"
                            :width="12"
                            class="mr-4"
                          >
                            <span class="text-h5 font-weight-bold">{{ userStore.user.creditScore }}</span>
                          </v-progress-circular>
                          
                          <div class="text-left">
                            <h3 class="text-h6 mb-2">信用评分</h3>
                            <p class="text-caption text-medium-emphasis">
                              {{ creditScoreRemark }}
                            </p>
                          </div>
                        </div>
                      </div>
                    </template>
                  </div>
                </v-card-text>
                
                <!-- 操作按钮 - 仅登录时显示 -->
                <v-card-actions class="justify-center pb-6" v-if="userStore.user">
                  <v-btn 
                    color="indigo" 
                    variant="tonal" 
                    class="mr-4" 
                    @click="openEditDialog"
                  >
                    <v-icon start>mdi-pencil</v-icon>
                    编辑资料
                  </v-btn>
                  <v-btn 
                    color="blue" 
                    variant="tonal" 
                    @click="handleEmailVerification"
                    :disabled="!userStore.user.email"
                  >
                    <v-icon start>mdi-email-check</v-icon>
                    {{ userStore.user.emailVerified ? '重新验证' : '邮箱认证' }}
                  </v-btn>
                  <!-- 新增退出登录按钮 -->
                  <v-btn 
                    color="red" 
                    variant="tonal" 
                    class="ml-4"
                    @click="handleLogout"
                    :loading="loggingOut"
                  >
                    <v-icon start>mdi-logout</v-icon>
                    退出登录
                  </v-btn>
                  <!-- 新增注销账户按钮 -->
                  <v-btn 
                    color="grey-darken-2" 
                    variant="tonal" 
                    class="ml-4"
                    @click="showDeleteDialog = true"
                    :loading="deletingAccount"
                  >
                    <v-icon start>mdi-account-remove</v-icon>
                    注销账户
                  </v-btn>
                </v-card-actions>
              </v-card>
            </v-col>
          </v-row>
        </v-container>

        <!-- 编辑资料对话框 -->
        <v-dialog v-model="editDialog" max-width="600" persistent>
          <v-card>
            <v-card-title class="text-h5">编辑个人资料</v-card-title>
            <v-card-text>
              <v-form ref="editForm">
                <v-text-field
                  v-model="editableUser.username"
                  label="用户名"
                  prepend-icon="mdi-account"
                  :rules="[requiredRule]"
                  required
                ></v-text-field>
                
                <v-text-field
                  v-model="editableUser.email"
                  label="邮箱"
                  prepend-icon="mdi-email"
                  :rules="[requiredRule, emailRule]"
                  required
                ></v-text-field>
                
                <v-text-field
                  v-model="editableUser.phone"
                  label="电话"
                  prepend-icon="mdi-phone"
                  :rules="[phoneRule]"
                ></v-text-field>
              </v-form>
            </v-card-text>
            <v-card-actions>
              <v-spacer></v-spacer>
              <v-btn color="secondary" @click="editDialog = false">取消</v-btn>
              <v-btn color="primary" @click="saveChanges" :loading="savingChanges">保存</v-btn>
            </v-card-actions>
          </v-card>
        </v-dialog>

        <!-- 邮箱验证对话框 -->
        <v-dialog v-model="showVerificationDialog" max-width="500">
          <v-card>
            <v-card-title class="text-h5">邮箱认证</v-card-title>
            <v-card-text>
              <p>我们将发送验证邮件到：</p>
              <p class="text-h6 text-primary">{{ userStore.user?.email }}</p>
              <p class="mt-2">请检查您的邮箱并点击验证链接完成认证</p>
            </v-card-text>
            <v-card-actions>
              <v-spacer></v-spacer>
              <v-btn color="secondary" @click="showVerificationDialog = false">取消</v-btn>
              <v-btn 
                color="primary" 
                @click="sendVerificationEmail"
                :loading="sendingEmail"
              >
                发送验证邮件
              </v-btn>
            </v-card-actions>
          </v-card>
        </v-dialog>

        <!-- 注销账户确认对话框 -->
        <v-dialog v-model="showDeleteDialog" max-width="500">
          <v-card>
            <v-card-title class="text-h5">确认注销账户</v-card-title>
            <v-card-text>
              <p class="text-body-1">您确定要注销账户吗？</p>
              <p class="text-body-2 text-warning mt-2">此操作将永久删除您的账户和所有数据，且不可恢复！</p>
            </v-card-text>
            <v-card-actions>
              <v-spacer></v-spacer>
              <v-btn color="secondary" @click="showDeleteDialog = false">取消</v-btn>
              <v-btn 
                color="error" 
                @click="handleDeleteAccount"
                :loading="deletingAccount"
              >
                确认注销
              </v-btn>
            </v-card-actions>
          </v-card>
        </v-dialog>

        <!-- 全局消息提示 -->
        <v-snackbar v-model="showSnackbar" :timeout="3000" :color="snackbarColor">
          {{ snackbarMessage }}
          <template v-slot:actions>
            <v-btn variant="text" @click="showSnackbar = false">关闭</v-btn>
          </template>
        </v-snackbar>
      </template>
    </div>
  </v-app>
</template>

<script setup lang="ts">
import { computed, ref, onMounted } from 'vue'
import type { VForm } from 'vuetify/components'
import { useUserStore } from '@/stores/user'

const userStore = useUserStore()

// 状态管理
const isLoading = ref(true)
const editDialog = ref(false)
const showVerificationDialog = ref(false)
const showDeleteDialog = ref(false)
const showSnackbar = ref(false)
const snackbarMessage = ref('')
const snackbarColor = ref('success')
const sendingEmail = ref(false)
const savingChanges = ref(false)
const loggingOut = ref(false)
const deletingAccount = ref(false)

// 表单引用
const editForm = ref<VForm | null>(null)

// 可编辑用户数据
const editableUser = ref({
  username: '',
  email: '',
  phone: ''
})

// 验证规则
const requiredRule = (v: string) => !!v || '必填项'
const emailRule = (v: string) => /.+@.+\..+/.test(v) || '请输入有效的邮箱地址'
const phoneRule = (v: string) => !v || /^1[3-9]\d{9}$/.test(v) || '请输入有效的手机号码'

// 计算属性
const creditScoreColor = computed(() => {
  if (!userStore.user?.creditScore) return 'grey'
  const score = userStore.user.creditScore
  if (score >= 90) return 'success'
  if (score >= 70) return 'info'
  if (score >= 50) return 'warning'
  return 'error'
})

const creditScoreRemark = computed(() => {
  if (!userStore.user?.creditScore) return '登录后查看信用评分'
  const score = userStore.user.creditScore
  if (score >= 90) return '优秀 - 信用状况非常好'
  if (score >= 70) return '良好 - 继续保持'
  if (score >= 50) return '一般 - 有提升空间'
  return '待提高 - 请注意信用行为'
})

// 生命周期钩子
onMounted(async () => {
  try {
    await userStore.fetchUserInfo('current')
    // 初始化可编辑数据
    if (userStore.user) {
      editableUser.value = {
        username: userStore.user.username,
        email: userStore.user.email,
        phone: userStore.user.phone || ''
      }
    }
  } catch (error) {
    console.error('加载用户信息失败:', error)
  } finally {
    isLoading.value = false
  }
})

// 方法定义
const openEditDialog = () => {
  if (userStore.user) {
    editableUser.value = {
      username: userStore.user.username,
      email: userStore.user.email,
      phone: userStore.user.phone || ''
    }
  }
  editDialog.value = true
}

const saveChanges = async () => {
  if (!editForm.value) return
  
  const { valid } = await editForm.value.validate()
  if (!valid) return

  savingChanges.value = true
  try {
    // 这里应该调用API更新用户信息
    // 模拟API调用
    await new Promise(resolve => setTimeout(resolve, 1000))
    
    // 更新store中的数据
    if (userStore.user) {
      userStore.user = {
        ...userStore.user,
        username: editableUser.value.username,
        email: editableUser.value.email,
        phone: editableUser.value.phone,
        emailVerified: editableUser.value.email !== userStore.user.email 
          ? false 
          : userStore.user.emailVerified
      }
    }
    
    editDialog.value = false
    showMessage('资料更新成功', 'success')
  } catch (error) {
    console.error('更新失败:', error)
    showMessage('更新用户信息失败', 'error')
  } finally {
    savingChanges.value = false
  }
}

const handleEmailVerification = () => {
  if (!userStore.user?.email) {
    showMessage('请先设置有效的邮箱地址', 'warning')
    return
  }
  showVerificationDialog.value = true
}

const sendVerificationEmail = async () => {
  sendingEmail.value = true
  try {
    // 这里应该调用API发送验证邮件
    // 模拟API调用
    await new Promise(resolve => setTimeout(resolve, 1000))
    
    showVerificationDialog.value = false
    showMessage('验证邮件已发送，请检查您的邮箱', 'success')
  } catch (error) {
    console.error('发送失败:', error)
    showMessage('发送验证邮件失败', 'error')
  } finally {
    sendingEmail.value = false
  }
}

// 在 UserDetailView.vue 的 script setup 部分修改 handleLogout 方法
const handleLogout = async () => {
  loggingOut.value = true
  try {
    // 调用 userStore 的 logout 方法
    await userStore.logout()
    
    // 显示成功消息
    showMessage('已成功退出登录', 'success')
    
    // 延迟跳转，让用户看到成功消息
    setTimeout(() => {
      window.location.href = '/login'
    }, 1500)
  } catch (error) {
    console.error('退出登录失败:', error)
    showMessage('退出登录失败', 'error')
  } finally {
    loggingOut.value = false
  }
}

// 新增方法 - 处理注销账户（暂时只做UI展示）
const handleDeleteAccount = async () => {
  deletingAccount.value = true
  try {
    // 这里应该是调用API注销账户
    // 暂时只模拟API调用
    await new Promise(resolve => setTimeout(resolve, 1000))
    
    showMessage('账户注销功能暂未实现', 'info')
    showDeleteDialog.value = false
  } catch (error) {
    console.error('注销账户失败:', error)
    showMessage('注销账户失败', 'error')
  } finally {
    deletingAccount.value = false
  }
}

const showMessage = (message: string, color: string) => {
  snackbarMessage.value = message
  snackbarColor.value = color
  showSnackbar.value = true
}
</script>

<style scoped>
.navbar {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  height: 60px;
  background-color: rgba(255, 255, 255, 0.95);
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
  display: flex;
  align-items: center;
  padding: 0 24px;
  z-index: 1000;
}

.navbar .icon {
  margin-right: 12px;
  display: flex;
  align-items: center;
}

.navbar .title {
  font-size: 1.25rem;
  font-weight: 600;
  color: #333;
}

.logout-btn {
  position: fixed;
  top: 75px;
  left: 24px;
  z-index: 1000;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  transition: all 0.3s ease;
}

.logout-btn:hover {
  transform: scale(1.05);
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2);
}

.profile-container {
  position: relative;
  min-height: 100vh;
  background: linear-gradient(135deg, #FFD6E0 0%, #C1E0FF 100%);
  padding-top: 60px;
}

.content-wrapper {
  position: relative;
  z-index: 2;
  padding-top: 20px;
}

.user-profile-card {
  border-radius: 16px;
  overflow: hidden;
  background-color: rgba(255, 255, 255, 0.9);
  backdrop-filter: blur(5px);
}

.profile-avatar {
  border: 4px solid white;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  margin-top: -60px;
}

.credit-score-container {
  background-color: rgba(255, 255, 255, 0.7);
  border-radius: 12px;
  padding: 16px;
  margin-top: 24px;
}

.decorative-bg {
  position: absolute;
  top: 50%;
  right: 5%;
  width: 40%;
  transform: translateY(-50%);
  z-index: 1;
  opacity: 1;
}

.decorative-bg img {
  width: 100%;
  height: auto;
  object-fit: contain;
}

@media (max-width: 960px) {
  .content-wrapper {
    padding-top: 10px;
  }
  
  .decorative-bg {
    display: none;
  }
  
  .user-profile-card {
    margin-top: 20px;
  }
  
  .logout-btn {
    top: 70px;
    left: 15px;
    size: 48px;
  }
}
</style>