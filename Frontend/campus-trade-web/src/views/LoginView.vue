<template>
  <div class="login-container">
    <div class="login-card">
      <div class="login-header">
        <h1>校园交易平台</h1>
        <p>欢迎登录</p>
      </div>

      <el-form
        ref="loginFormRef"
        :model="loginForm"
        :rules="loginRules"
        class="login-form"
        @submit.prevent="handleLogin"
      >
        <el-form-item prop="username">
          <el-input
            v-model="loginForm.username"
            placeholder="请输入用户名或邮箱"
            size="large"
            :prefix-icon="User"
          />
        </el-form-item>

        <el-form-item prop="password">
          <el-input
            v-model="loginForm.password"
            type="password"
            placeholder="请输入密码"
            size="large"
            :prefix-icon="Lock"
            show-password
            @keyup.enter="handleLogin"
          />
        </el-form-item>

        <el-form-item>
          <el-checkbox v-model="loginForm.remember_me">记住我</el-checkbox>
        </el-form-item>

        <el-form-item>
          <el-button
            type="primary"
            size="large"
            :loading="loading"
            style="width: 100%"
            @click="handleLogin"
          >
            {{ loading ? '登录中...' : '登录' }}
          </el-button>
        </el-form-item>
      </el-form>

      <div class="login-footer">
        <p>
          还没有账号？
          <el-button type="text" @click="showRegister = true">立即注册</el-button>
        </p>
      </div>
    </div>

    <!-- 注册对话框 -->
    <el-dialog
      v-model="showRegister"
      title="用户注册"
      width="500px"
      :before-close="handleCloseRegister"
    >
      <el-form
        ref="registerFormRef"
        :model="registerForm"
        :rules="registerRules"
        label-width="100px"
      >
        <el-form-item label="学号" prop="student_id">
          <el-input v-model="registerForm.student_id" placeholder="请输入学号" />
        </el-form-item>

        <el-form-item label="姓名" prop="name">
          <el-input v-model="registerForm.name" placeholder="请输入真实姓名" />
        </el-form-item>

        <el-form-item label="邮箱" prop="email">
          <el-input v-model="registerForm.email" placeholder="请输入邮箱" />
        </el-form-item>

        <el-form-item label="用户名" prop="username">
          <el-input v-model="registerForm.username" placeholder="请输入用户名" />
        </el-form-item>

        <el-form-item label="手机号" prop="phone">
          <el-input v-model="registerForm.phone" placeholder="请输入手机号" />
        </el-form-item>

        <el-form-item label="密码" prop="password">
          <el-input
            v-model="registerForm.password"
            type="password"
            placeholder="请输入密码"
            show-password
          />
        </el-form-item>

        <el-form-item label="确认密码" prop="confirm_password">
          <el-input
            v-model="registerForm.confirm_password"
            type="password"
            placeholder="请确认密码"
            show-password
          />
        </el-form-item>

        <el-form-item>
          <el-button
            type="info"
            size="small"
            :loading="validatingStudent"
            @click="validateStudentIdentity"
          >
            {{ validatingStudent ? '验证中...' : '验证学生身份' }}
          </el-button>
          <span v-if="studentValidated" style="color: green; margin-left: 10px">
            ✓ 学生身份验证成功
          </span>
          <span v-else-if="studentValidationFailed" style="color: red; margin-left: 10px">
            ✗ 学生身份验证失败
          </span>
        </el-form-item>
      </el-form>

      <template #footer>
        <span class="dialog-footer">
          <el-button @click="handleCloseRegister">取消</el-button>
          <el-button type="primary" :loading="registerLoading" @click="handleRegister">
            {{ registerLoading ? '注册中...' : '注册' }}
          </el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
  import { ref, reactive } from 'vue'
  import { useRouter } from 'vue-router'
  import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
  import { User, Lock } from '@element-plus/icons-vue'
  import { useUserStore } from '@/stores/user'
  import type { RegisterData } from '@/services/api'

  const router = useRouter()
  const userStore = useUserStore()

  const loading = ref(false)
  const registerLoading = ref(false)
  const showRegister = ref(false)
  const validatingStudent = ref(false)
  const studentValidated = ref(false)
  const studentValidationFailed = ref(false)

  const loginFormRef = ref<FormInstance>()
  const registerFormRef = ref<FormInstance>()

  // 登录表单
  const loginForm = reactive({
    username: '',
    password: '',
    remember_me: false,
  })

  // 注册表单
  const registerForm = reactive<RegisterData>({
    student_id: '',
    name: '',
    email: '',
    password: '',
    confirm_password: '',
    username: '',
    phone: '',
  })

  // 登录表单验证规则
  const loginRules: FormRules = {
    username: [
      { required: true, message: '请输入用户名或邮箱', trigger: 'blur' },
      { min: 3, max: 100, message: '用户名或邮箱长度在 3 到 100 个字符', trigger: 'blur' },
    ],
    password: [
      { required: true, message: '请输入密码', trigger: 'blur' },
      { min: 6, message: '密码长度不能少于 6 个字符', trigger: 'blur' },
    ],
  }

  // 注册表单验证规则
  const registerRules: FormRules = {
    student_id: [
      { required: true, message: '请输入学号', trigger: 'blur' },
      { max: 20, message: '学号长度不能超过20字符', trigger: 'blur' },
    ],
    name: [
      { required: true, message: '请输入姓名', trigger: 'blur' },
      { max: 100, message: '姓名长度不能超过100字符', trigger: 'blur' },
    ],
    email: [
      { required: true, message: '请输入邮箱', trigger: 'blur' },
      { type: 'email', message: '请输入正确的邮箱格式', trigger: 'blur' },
      { max: 100, message: '邮箱长度不能超过100字符', trigger: 'blur' },
    ],
    username: [{ max: 50, message: '用户名长度不能超过50字符', trigger: 'blur' }],
    phone: [
      {
        pattern: /^1[3-9]\d{9}$/,
        message: '请输入正确的手机号格式',
        trigger: 'blur',
      },
      { max: 20, message: '手机号长度不能超过20字符', trigger: 'blur' },
    ],
    password: [
      { required: true, message: '请输入密码', trigger: 'blur' },
      { min: 6, max: 100, message: '密码长度必须在6-100字符之间', trigger: 'blur' },
    ],
    confirm_password: [
      { required: true, message: '请确认密码', trigger: 'blur' },
      {
        validator: (rule, value, callback) => {
          if (value !== registerForm.password) {
            callback(new Error('两次输入的密码不一致'))
          } else {
            callback()
          }
        },
        trigger: 'blur',
      },
    ],
  }

  // 验证学生身份
  const validateStudentIdentity = async () => {
    if (!registerForm.student_id || !registerForm.name) {
      ElMessage.warning('请先填写学号和姓名')
      return
    }

    validatingStudent.value = true
    studentValidated.value = false
    studentValidationFailed.value = false

    try {
      const result = await userStore.validateStudent(registerForm.student_id, registerForm.name)

      if (result.success && result.isValid) {
        studentValidated.value = true
        ElMessage.success('学生身份验证成功')
      } else {
        studentValidationFailed.value = true
        ElMessage.error(result.message || '学生身份验证失败')
      }
    } catch {
      studentValidationFailed.value = true
      ElMessage.error('验证过程中发生错误')
    } finally {
      validatingStudent.value = false
    }
  }

  // 处理登录
  const handleLogin = async () => {
    if (!loginFormRef.value) return

    await loginFormRef.value.validate(async valid => {
      if (valid) {
        loading.value = true
        try {
          const result = await userStore.login({
            username: loginForm.username,
            password: loginForm.password,
            remember_me: loginForm.remember_me,
          })

          if (result.success) {
            ElMessage.success(result.message)
            router.push('/')
          } else {
            ElMessage.error(result.message)
          }
        } catch {
          ElMessage.error('登录失败，请重试')
        } finally {
          loading.value = false
        }
      }
    })
  }

  // 处理注册
  const handleRegister = async () => {
    if (!registerFormRef.value) return

    await registerFormRef.value.validate(async valid => {
      if (valid) {
        registerLoading.value = true
        try {
          const result = await userStore.register(registerForm)
          if (result.success) {
            ElMessage.success(result.message)
            showRegister.value = false
            resetRegisterForm()
          } else {
            ElMessage.error(result.message)
          }
        } catch {
          ElMessage.error('注册失败，请重试')
        } finally {
          registerLoading.value = false
        }
      }
    })
  }

  // 关闭注册对话框
  const handleCloseRegister = () => {
    showRegister.value = false
    resetRegisterForm()
  }

  // 重置注册表单
  const resetRegisterForm = () => {
    registerFormRef.value?.resetFields()
    Object.assign(registerForm, {
      student_id: '',
      name: '',
      email: '',
      password: '',
      confirm_password: '',
      username: '',
      phone: '',
    })
    studentValidated.value = false
    studentValidationFailed.value = false
  }
</script>

<style scoped>
  .login-container {
    width: 100vw;
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    margin: 0;
    padding: 0;
    box-sizing: border-box;
  }

  .login-card {
    width: 400px;
    padding: 40px;
    background: white;
    border-radius: 10px;
    box-shadow: 0 10px 25px rgba(0, 0, 0, 0.1);
  }

  .login-header {
    text-align: center;
    margin-bottom: 30px;
  }

  .login-header h1 {
    color: #333;
    font-size: 28px;
    margin-bottom: 10px;
  }

  .login-header p {
    color: #666;
    font-size: 16px;
    margin: 0;
  }

  .login-form {
    margin-bottom: 20px;
  }

  .login-footer {
    text-align: center;
  }

  .login-footer p {
    color: #666;
    margin: 0;
  }

  .dialog-footer {
    display: flex;
    justify-content: flex-end;
    gap: 10px;
  }
</style>
