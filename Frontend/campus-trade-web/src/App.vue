<script setup lang="ts">
  import { computed, onMounted } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import { ElMessage, ElMessageBox } from 'element-plus'
  import { ArrowDown } from '@element-plus/icons-vue'
  import { useUserStore } from './stores/user'

  const route = useRoute()
  const router = useRouter()
  const userStore = useUserStore()

  // 判断是否是登录页面
  const isLoginPage = computed(() => route.path === '/login')

  // 初始化用户状态
  onMounted(() => {
    userStore.initializeAuth()
  })
</script>

<template>
  <div id="app">
    <!-- 登录页面 -->
    <RouterView />
  </div>
</template>

<style scoped>
  #app {
    font-family: 'Avenir', Helvetica, Arial, sans-serif;
    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
    color: #2c3e50;
    height: 100vh;
  }
</style>
