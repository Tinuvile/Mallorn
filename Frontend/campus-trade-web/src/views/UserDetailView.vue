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

      <!-- Decorative background images on the right -->
      <div class="decorative-bg">
        <img src="https://grace-l-hub.oss-cn-shanghai.aliyuncs.com/OIP-C%281%29%281%29.png" class="top-image" alt="Decorative image">
        <img src="https://grace-l-hub.oss-cn-shanghai.aliyuncs.com/OIP-C%282%29%281%29.png" class="bottom-image" alt="Decorative image">
      </div>
      
      <!-- Main content container - 调整了位置 -->
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
                  <p class="text-h4 font-weight-bold mb-6" style="font-family: 'Comic Sans MS', cursive;">User Information</p>
                  
                  <v-divider class="my-4"></v-divider>
                  
                  <v-list lines="two" density="comfortable" class="text-left">
                    <v-list-item prepend-icon="mdi-account" title="用户名">
                      <template v-slot:append>
                        <span class="text-body-1">{{ user.name }}</span>
                      </template>
                    </v-list-item>
                    
                    <v-list-item prepend-icon="mdi-identifier" title="用户ID">
                      <template v-slot:append>
                        <span class="text-body-1">{{ user.userID }}</span>
                      </template>
                    </v-list-item>
                    
                    <v-list-item prepend-icon="mdi-card-account-details" title="学号">
                      <template v-slot:append>
                        <span class="text-body-1">{{ user.studentId }}</span>
                      </template>
                    </v-list-item>
                    
                    <v-list-item prepend-icon="mdi-email" title="邮箱">
                      <template v-slot:append>
                        <span class="text-body-1">{{ user.email }}</span>
                        <v-chip :color="user.emailVerified ? 'success' : 'warning'" size="small" class="ml-2">
                          {{ user.emailVerified ? '已认证' : '未认证' }}
                        </v-chip>
                      </template>
                    </v-list-item>
                    
                    <v-list-item prepend-icon="mdi-phone" title="电话">
                      <template v-slot:append>
                        <span class="text-body-1">{{ user.phone }}</span>
                      </template>
                    </v-list-item>
                  </v-list>
                </div>
                
                <div class="credit-score-container mt-6">
                  <div class="d-flex justify-center align-center">
                    <v-progress-circular
                      :model-value="user.creditScore"
                      :color="creditScoreColor"
                      :size="120"
                      :width="12"
                      class="mr-4"
                    >
                      <span class="text-h5 font-weight-bold">{{ user.creditScore }}</span>
                    </v-progress-circular>
                    
                    <div class="text-left">
                      <h3 class="text-h6 mb-2">信用评分</h3>
                      <p class="text-caption text-medium-emphasis">
                        {{ creditScoreRemark }}
                      </p>
                    </div>
                  </div>
                </div>
              </v-card-text>
              
              <v-card-actions class="justify-center pb-6">
                <v-btn color="indigo" variant="tonal" class="mr-4" @click="openEditDialog">
                  <v-icon start>mdi-pencil</v-icon>
                  编辑资料
                </v-btn>
                <v-btn color="blue" variant="tonal" @click="handleEmailVerification">
                  <v-icon start>mdi-email-check</v-icon>
                  邮箱认证
                </v-btn>
              </v-card-actions>
            </v-card>
          </v-col>
        </v-row>
      </v-container>

      <!-- Edit Profile Dialog -->
      <v-dialog v-model="editDialog" max-width="600">
        <v-card>
          <v-card-title class="text-h5">编辑个人资料</v-card-title>
          <v-card-text>
            <v-form ref="editForm" @submit.prevent="saveChanges">
              <v-text-field
                v-model="editableUser.name"
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
                :rules="[requiredRule, phoneRule]"
                required
              ></v-text-field>
            </v-form>
          </v-card-text>
          <v-card-actions>
            <v-spacer></v-spacer>
            <v-btn color="secondary" @click="editDialog = false">取消</v-btn>
            <v-btn color="primary" @click="saveChanges">保存</v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>

      <!-- Email Verification Dialog -->
      <v-dialog v-model="showVerificationDialog" max-width="500">
        <v-card>
          <v-card-title class="text-h5">邮箱认证</v-card-title>
          <v-card-text>
            <p>我们将发送一封验证邮件到 {{ user.email }}</p>
            <p>请检查您的邮箱并点击邮件中的验证链接完成认证。</p>
          </v-card-text>
          <v-card-actions>
            <v-spacer></v-spacer>
            <v-btn color="primary" @click="sendVerificationEmail">发送验证邮件</v-btn>
            <v-btn color="secondary" @click="showVerificationDialog = false">取消</v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>

      <!-- Snackbar for messages -->
      <v-snackbar v-model="showSnackbar" :timeout="2000" :color="snackbarColor">
        {{ snackbarMessage }}
      </v-snackbar>
    </div>
  </v-app>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import type { VForm } from 'vuetify/components'

interface User {
  name: string
  userID: string
  studentId: string
  email: string
  phone: string
  creditScore: number
  emailVerified: boolean
}

const user = ref<User>({
  name: '张晓明',
  userID: 'USER123456',
  studentId: '2023101234',
  email: 'zhangxm@university.edu',
  phone: '13800138000',
  creditScore: 85,
  emailVerified: false
})

const editDialog = ref(false)
const editableUser = ref<User>({...user.value})
const editForm = ref<VForm | null>(null)
const showVerificationDialog = ref(false)
const showSnackbar = ref(false)
const snackbarMessage = ref('')
const snackbarColor = ref('success')

// Validation rules
const requiredRule = (v: string) => !!v || '此项为必填项'
const emailRule = (v: string) => /.+@.+\..+/.test(v) || '请输入有效的邮箱地址'
const phoneRule = (v: string) => /^1[3-9]\d{9}$/.test(v) || '请输入有效的手机号码'

const creditScoreColor = computed(() => {
  if (user.value.creditScore >= 90) return 'success'
  if (user.value.creditScore >= 70) return 'info'
  if (user.value.creditScore >= 50) return 'warning'
  return 'error'
})

const creditScoreRemark = computed(() => {
  if (user.value.creditScore >= 90) return '优秀 - 您的信用状况非常好'
  if (user.value.creditScore >= 70) return '良好 - 继续保持'
  if (user.value.creditScore >= 50) return '一般 - 有提升空间'
  return '待提高 - 请注意信用行为'
})

const openEditDialog = () => {
  editableUser.value = {...user.value}
  editDialog.value = true
}

const saveChanges = async () => {
  if (!editForm.value) return
  
  const { valid } = await editForm.value.validate()
  
  if (!valid) return

  if (editableUser.value.email !== user.value.email) {
    editableUser.value.emailVerified = false
  }

  user.value = {...editableUser.value}
  editDialog.value = false
  showSnackbarMessage('资料更新成功', 'success')
}

const handleEmailVerification = () => {
  if (user.value.emailVerified) {
    showSnackbarMessage('邮箱已认证', 'success')
  } else {
    showVerificationDialog.value = true
  }
}

const sendVerificationEmail = () => {
  console.log(`Sending verification email to ${user.value.email}`)
  
  setTimeout(() => {
    showVerificationDialog.value = false
    showSnackbarMessage('验证邮件已发送，请检查您的邮箱', 'info')
    
    setTimeout(() => {
      user.value.emailVerified = true
      showSnackbarMessage('邮箱认证成功', 'success')
    }, 3000)
  }, 1000)
}

const handleLogout = () => {
  console.log('退出按钮被点击')
  // router.push('/login')
}

const showSnackbarMessage = (message: string, color: string) => {
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

/* 优化的退出按钮样式 */
.logout-btn {
  position: fixed;
  top: 75px;
  left: 24px;
  z-index: 1000;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  transition: all 0.3s ease;
}

.logout-btn:hover {
  transform: scale(1.1);
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2);
}

.logout-btn::before {
  content: '';
  position: absolute;
  top: -4px;
  left: -4px;
  right: -4px;
  bottom: -4px;
  border-radius: 50%;
  border: 2px solid rgba(239, 83, 80, 0.3);
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0% {
    transform: scale(0.95);
    opacity: 0.7;
  }
  70% {
    transform: scale(1.1);
    opacity: 0.3;
  }
  100% {
    transform: scale(0.95);
    opacity: 0.7;
  }
}

.profile-container {
  position: relative;
  min-height: 100vh;
  background: linear-gradient(135deg, 
    #ffd6e0 0%,    /* 淡粉色 */
    #c8f0ff 100%   /* 淡蓝色 */
  );
  background-attachment: fixed;
  overflow: hidden;
  padding-top: 60px; /* Space for navbar */
}

/* 调整内容容器位置 */
.content-wrapper {
  position: relative;
  z-index: 2;
  height: auto;
  min-height: calc(100vh - 60px);
  display: flex;
  align-items: flex-start; /* 使内容向上对齐 */
  padding-top: 10px; /* 减少顶部间距 */
}

.row-wrapper {
  width: 100%;
  margin-top: 0;
}

.col-wrapper {
  display: flex;
  justify-content: center;
  padding-top: 0; /* 移除顶部内边距 */
}

/* 调整卡片位置 */
.user-profile-card {
  margin-top: 10px;
  border-radius: 16px;
  overflow: hidden;
  transition: transform 0.3s ease;
  background-color: rgba(255, 255, 255, 0.9);
  max-width: 1000px;
  width: 100%;
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
}

.user-info-column {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.v-list {
  width: 100%;
  max-width: 500px;
}

.decorative-bg {
  position: absolute;
  top: 50%;
  right: 5%;
  width: 40%;
  height: 80%;
  transform: translateY(-50%);
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  gap: 0px;
  z-index: 1;
}

.decorative-bg img {
  max-width: 80%;
  height: auto;
  object-fit: contain;
  opacity: 1;
  transition: opacity 0.3s ease;
}

.top-image {
  opacity: 1; 
  width: 500px;
  height: auto;
  margin-bottom: 3px !important; 
}

.bottom-image {
  opacity: 1; 
  width: 500px;
  height: auto;
  margin-top: 3px !important;   
}

/* 响应式调整 */
@media (max-width: 960px) {
  .content-wrapper {
    padding-top: 5px;
  }
  
  .col-wrapper {
    padding-top: 0;
  }
  
  .user-profile-card {
    margin-top: 5px;
  }
  
  .logout-btn {
    top: 70px;
    left: 15px;
    size: 48px;
  }
  
  .decorative-bg {
    width: 100%;
    opacity: 0.08;
  }
  
  .decorative-bg img {
    max-width: 60%;
    opacity: 1;
  }
  
  .top-image,
  .bottom-image {
    width: 100%;
    max-width: 300px;
  }
}
</style>