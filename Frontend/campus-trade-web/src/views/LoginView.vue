<template>
  <div class="container">
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
    <div class="main">
      <!-- 左侧面板 -->
      <div class="left-panel" :class="{ 'panel-slide-left': isLogin, 'panel-slide-right': !isLogin }">
        <transition name="content-fade" mode="out-in">
          <div v-if="!isLogin" key="register-content" class="panel-content">
            <!-- 动画贴纸区域 -->
            <div class="lottie-left-center">
              <Vue3Lottie
                :animationData="lottieData"
                style="width: 500px; height: 500px"
                :loop="true"
                :autoplay="true"
              />
            </div>
            <div class="sentence-left">
              已有账户？点击此处
              <span class="login-link" @click="switchToLogin">登录</span>
            </div>
          </div>
          
          <!-- 登录表单 -->
          <div v-else key="login-content" class="panel-content">
            <h1 class="register-title" style="font-weight:bold;">Log in</h1>
            <form class="login-form" autocomplete="off">
              <div class="form-item">
                <span class="star">*</span>
                <div class="form-main">
                  <v-text-field
                    v-model="loginValue"
                    :rules="rules"
                    hide-details="auto"
                    label="用户名/邮箱"
                    placeholder="请输入用户名/邮箱"
                    variant="underlined"
                  ></v-text-field>
                </div>
              </div>
              <div class="form-item">
                <span class="star">*</span>
                <div class="form-main">
                  <v-text-field
                    v-model="password"
                    :rules="rules"
                    hide-details="auto"
                    label="密码"
                    placeholder="请输入密码"
                    type="password"
                    variant="underlined"
                  ></v-text-field>
                </div>
              </div>
              <div class="v-btn-row">
                <v-btn
                  class="custom-btn"
                  height="48"
                  style="background: #acd6ee; color: #fff; border-radius: 24px; margin-bottom: 10px; margin-top: 10px;"
                >
                  登录
                </v-btn>
                <v-btn
                  class="custom-btn"
                  height="48"
                  style="background: #e7e7e7; color: #757575; border-radius: 24px;margin-bottom: 10px; margin-top: 10px;"
                >
                  返回
                </v-btn>
              </div>
            </form>
          </div>
        </transition>
      </div>

      <!-- 右侧面板 -->
      <div class="right-panel" :class="{ 'panel-slide-right': isLogin, 'panel-slide-left': !isLogin }">
        <transition name="content-fade" mode="out-in">
          <!-- 注册表单 -->
          <div v-if="!isLogin" key="register-content" class="panel-content">
            <h1 class="register-title" style="font-weight:bold;text-align:center;">Register</h1>
            <form class="register-form" autocomplete="off">
              <div class="form-item">
                <span class="star">*</span>
                <div class="form-main">
                  <v-text-field
                    v-model="registerForm.studentId"
                    :rules="rules"
                    hide-details="auto"
                    label="学号"
                    placeholder="请输入学号"
                    variant="underlined"
                  ></v-text-field>
                </div>
              </div>
              <div class="form-item">
                <span class="star">*</span>
                <div class="form-main">
                  <v-text-field
                    v-model="registerForm.name"
                    :rules="rules"
                    hide-details="auto"
                    label="姓名"
                    placeholder="请输入真实姓名"
                    variant="underlined"
                  ></v-text-field>
                </div>
              </div>
              <div class="form-item">
                <span class="star">*</span>
                <div class="form-main">
                  <v-text-field
                    v-model="registerForm.email"
                    :rules="rules"
                    hide-details="auto"
                    label="邮箱"
                    placeholder="请输入邮箱"
                    variant="underlined"
                  ></v-text-field>
                </div>
              </div>
              <div class="form-item">
                <span class="kong"> </span>
                <div class="form-main">
                  <v-text-field
                    v-model="registerForm.username"
                    hide-details="auto"
                    label="用户名"
                    placeholder="请输入用户名"
                    variant="underlined"
                  ></v-text-field>
                </div>
              </div>
              <div class="form-item">
                <span class="kong"> </span>
                <div class="form-main">
                  <v-text-field
                    v-model="registerForm.phone"
                    hide-details="auto"
                    label="手机号"
                    placeholder="请输入手机号"
                    variant="underlined"
                  ></v-text-field>
                </div>
              </div>
              <div class="form-item">
                <span class="star">*</span>
                <div class="form-main">
                  <v-text-field
                    v-model="registerForm.password"
                    :rules="rules"
                    hide-details="auto"
                    label="密码"
                    placeholder="请输入密码"
                    type="password"
                    variant="underlined"
                  ></v-text-field>
                </div>
              </div>
              <div class="form-item">
                <span class="star">*</span>
                <div class="form-main">
                  <v-text-field
                    v-model="registerForm.confirmPassword"
                    :rules="rules"
                    hide-details="auto"
                    label="确认密码"
                    placeholder="请确认密码"
                    type="password"
                    variant="underlined"
                  ></v-text-field>
                </div>
              </div>
              <div class="text-center" style="margin-bottom: 10px;">
                <div>
                  <v-btn
                    class="ma-2"
                    color="#ff4e6d"
                    justify-content="center"
                  >
                    验证学生身份
                    <v-icon
                      icon="mdi-checkbox-marked-circle"
                      end
                    ></v-icon>
                  </v-btn>
                </div>
              </div>
              <div class="v-btn-row">
                <v-btn
                  class="custom-btn"
                  height="48"
                  style="background: #ff4e6d; color: #fff; border-radius: 24px;"
                >
                  注册
                </v-btn>
                <v-btn
                  class="custom-btn"
                  height="48"
                  style="background: #e7e7e7; color: #757575; border-radius: 24px;"
                >
                  返回
                </v-btn>
              </div>
            </form>
          </div>
          
          <!-- 登录提示内容 -->
          <div v-else key="login-content" class="panel-content">
            <!-- 动画贴纸区域 -->
            <div class="lottie-right-center">
              <Vue3Lottie
                :animationData="lottieData"
                style="width: 500px; height: 500px"
                :loop="true"
                :autoplay="true"
              />
            </div>
            <div class="sentence-right">
              没有账户？点击此处
              <span class="login-link" @click="switchToRegister">注册</span>
            </div>
          </div>
        </transition>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import lottieData from '../assets/animation1.json'
console.log('lottieData:', lottieData)

import lottieData2 from '../assets/talk.json'
console.log('lottieData2:', lottieData2)

const isLogin = ref(false)
const loginValue = ref('')
const password = ref('')
const registerForm = ref({
  studentId: '',
  name: '',
  email: '',
  username: '',
  phone: '',
  password: '',
  confirmPassword: ''
})
const rules = [
  v => !!v || '必填',
  v => (v && v.length >= 2) || '至少2个字符'
]

// 切换到登录页面
const switchToLogin = () => {
  isLogin.value = true
}

// 切换到注册页面
const switchToRegister = () => {
  isLogin.value = false
}
</script>

<style>
html {
  height: 100%;
  width: 100%;
  margin: 0;
  padding: 0;
  box-sizing: border-box;
  overflow: hidden;
}
#app, .container {
  height: 100vh;
  min-height: 100vh;
  width: 100vw;
  min-width: 100vw;
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}
</style>

<style scoped>
/* 面板滑动动画 */
.left-panel, .right-panel {
  transition: all 0.8s cubic-bezier(0.4, 0, 0.2, 1);
  overflow: hidden;
}

/* 登录状态：左侧白色，右侧蓝色 */
.left-panel.panel-slide-left {
  background: #fff !important;
  transform: translateX(-10px);
}

.right-panel.panel-slide-right {
  background: #c0f1ff !important;
  transform: translateX(10px);
}

/* 注册状态：左侧粉色，右侧白色 */
.left-panel.panel-slide-right {
  background: #fceeee !important;
  transform: translateX(0px);
}

.right-panel.panel-slide-left {
  background: #fff !important;
  transform: translateX(-10px);
}

/* 内容淡入淡出动画 */
.content-fade-enter-active,
.content-fade-leave-active {
  transition: all 0.5s ease;
}

.content-fade-enter-from {
  opacity: 0;
  transform: translateY(20px) scale(0.95);
}

.content-fade-leave-to {
  opacity: 0;
  transform: translateY(-20px) scale(0.95);
}

.content-fade-enter-to,
.content-fade-leave-from {
  opacity: 1;
  transform: translateY(0) scale(1);
}

/* 面板内容容器 */
.panel-content {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  position: relative;
}

/* 登录链接悬停效果 */
.login-link {
  transition: all 0.3s ease;
  position: relative;
}

.login-link:hover {
  color: #ff4e6d;
  transform: translateY(-2px);
}

.login-link::after {
  content: '';
  position: absolute;
  bottom: -2px;
  left: 0;
  width: 0;
  height: 2px;
  background: #ff4e6d;
  transition: width 0.3s ease;
}

.login-link:hover::after {
  width: 100%;
}

/* 按钮悬停效果 */
.custom-btn {
  transition: all 0.3s ease;
}

.custom-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

/* 表单项动画 */
.form-item {
  transition: all 0.3s ease;
}

.form-item:hover {
  transform: translateX(5px);
}

/*控制注册和返回按钮*/
.v-btn-row {
  display: flex;
  gap: 50px;
  justify-content: center;
  margin-top: 15px;
}

.custom-btn {
  font-size: 24px;
  font-family: inherit;
  font-weight: bold;
  letter-spacing: 2px;
}

.container {
  width: 100vw;
  height: 100vh;
  margin: 0;
  background: #fff;
  font-family: 'Alibaba PuHuiTi', 'Arial', 'Microsoft YaHei', sans-serif;
  box-sizing: border-box;
}

.main {
  display: flex;
  height: 100%;
  width: 100%;
  min-height: 600px;
}

.left-panel, .right-panel {
  width: 50%;
  min-width: 200px;
  position: relative;
}

/* 顶部导航栏 */
.navbar {
  height: 50px;
  border-bottom: 2px solid #adadad;
  display: flex;
  position: 0 0 0 0;
  align-items: center;
  padding-left: 32px;
  background: #fff;
  position: relative;
  z-index: 2;
}

.icon {
  font-size: 25px;
  color: #222;
  margin-right: 10px;
}

.title {
  font-weight: 600;
  font-size: 30px;
  letter-spacing: 0.5px;
}

/* 主体布局 */
.left-panel {
  width: 50%;
  background: #fceeee;
  position: relative;
  min-height: 718px;
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
  align-items: flex-start;
  padding-left: 28px;
  padding-bottom: 32px;
}

.login-tip {
  font-size: 20px;
  color: #999;
  margin: 50px 50px 50px 50px;
  margin-color: #000000;
}

.login-link {
  color: #222;
  font-weight: bold;
  font-size: 20px;
  text-decoration: underline;
  margin-left: 2px;
  cursor: pointer;
}

/* 保证lottie动画居中 */
.left-panel {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  position: relative;
  height: 100%;
  background: #fceeee;
}

.lottie-left-center {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 300px;
}

.lottie-right-center {
  flex: 1;
  margin-top: 250px;
  align-items: center;
  justify-content: center;
  min-height: 300px;
}

/* 右侧表单区 */
.right-panel {
  width: 50%;
  background: #fff;
  position: relative;
  padding-top: 0px;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.register-title {
  font-size: 46px;
  font-family: 'Comic Sans MS', 'Arial Black', cursive, sans-serif;
  font-weight: bold;
  margin-top: 0;
  margin-bottom: 30px;
  color: #222;
  letter-spacing: 1px;
  transition: all 0.3s ease;
}

.register-title:hover {
  transform: scale(1.05);
}

/* 注册表单部分 */
.register-form {
  width: 600px;
  margin-top: 2px;
  display: block;
  flex-direction: column;
  gap: 0;
  text-align: left;
  background: rgba(234, 248, 255, 0.12);
  border: 0.5px solid #eef1ff;
  border-radius: 32px;
  padding: 30px 32px 20px;
  box-shadow: 0 8px 32px 0 rgba(80, 44, 44, 0.12);
  transition: all 0.3s ease;
}

.register-form:hover {
  box-shadow: 0 12px 40px 0 rgba(80, 44, 44, 0.18);
}

/* 登录表单部分 */
.login-form {
  width: 600px;
  margin-top: 2px;
  display: block;
  flex-direction: column;
  gap: 0;
  text-align: left;
  background: rgba(255, 234, 234, 0.12);
  border: 0.5px solid #eef1ff;
  border-radius: 32px;
  padding: 40px 32px 20px;
  box-shadow: 0 8px 32px 0 rgba(80, 44, 44, 0.12);
  transition: all 0.3s ease;
}

.login-form:hover {
  box-shadow: 0 12px 40px 0 rgba(80, 44, 44, 0.18);
}

.form-item {
  display: flex;
  align-items: flex-start;
  margin-bottom: 15px;
  min-height: 50px;
}

.star {
  color: #ff4e6d;
  font-size: 22px;
  line-height: 40px;
  width: 32px;
  text-align: left;
  font-family: inherit;
}

.kong {
  color: #ff4e6d;
  font-size: 22px;
  line-height: 40px;
  width: 32px;
  text-align: left;
  font-family: inherit;
}

.form-main {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.label {
  margin-left: 0;
  font-size: 19px;
  color: #4d4d4d;
  margin-bottom: 4px;
}

.input {
  border: none;
  border-bottom: 1px solid #bdbdbd;
  outline: none;
  font-size: 18px;
  padding: 6px 0 4px 0;
  background: transparent;
  color: #333;
  transition: border 0.2s;
}

.input:focus {
  border-bottom: 2px solid #ffb3bb;
}

/* 验证按钮 */
.verify-btn {
  width: 160px;
  margin: 18px 0 0 44px;
  padding: 7px 0;
  text-align: center;
  border-radius: 20px;
  background: #ffe3e8;
  color: #ff4e6d;
  font-size: 18px;
  letter-spacing: 1px;
  cursor: not-allowed;
  opacity: 0.7;
  font-weight: 400;
}

.sentence-left {
  position: absolute;
  left: 3px;
  bottom: 30px;
  margin: 0;
}

.sentence-right {
  position: absolute;
  right: 32px;
  bottom: 70px;
  margin: 0;
}

@media (max-width: 768px) {
  .main {
    flex-direction: column;
  }
  .left-panel, .right-panel {
    width: 100%;
  }
}
</style>
