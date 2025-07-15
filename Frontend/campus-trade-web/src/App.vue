<script setup lang="ts">
  import { computed, onMounted } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import { ElMessage, ElMessageBox } from 'element-plus'
  import { ArrowDown } from '@element-plus/icons-vue'
  import { useUserStore } from '@/stores/user'

  const route = useRoute()
  const router = useRouter()
  const userStore = useUserStore()

  // 判断是否是登录页面
  const isLoginPage = computed(() => route.path === '/login')

  // 初始化用户状态
  onMounted(() => {
    userStore.initializeAuth()
  })

  // 处理下拉菜单命令
  const handleCommand = async (command: string) => {
    switch (command) {
      case 'profile':
        // 跳转到个人信息页面
        ElMessage.info('个人信息功能开发中...')
        break
      case 'logout':
        try {
          await ElMessageBox.confirm('确定要退出登录吗？', '提示', {
            confirmButtonText: '确定',
            cancelButtonText: '取消',
            type: 'warning',
          })
          userStore.logout()
          ElMessage.success('已退出登录')
          router.push('/login')
        } catch {
          // 用户取消退出
        }
        break
    }
  }
</script>

<template>
  <div id="app">
    <!-- 导航栏 -->
    <el-container v-if="!isLoginPage">
      <el-header class="app-header">
        <div class="header-content">
          <div class="logo">
            <h2>校园交易平台</h2>
          </div>
          <el-menu mode="horizontal" :default-active="$route.path" router class="nav-menu">
            <el-menu-item index="/">首页</el-menu-item>
            <el-menu-item index="/about">关于</el-menu-item>
          </el-menu>
          <div class="user-info" v-if="userStore.isLoggedIn">
            <el-dropdown @command="handleCommand">
              <span class="user-name">
                {{ userStore.user?.fullName || userStore.user?.username }}
                <el-icon class="el-icon--right">
                  <arrow-down />
                </el-icon>
              </span>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="profile">个人信息</el-dropdown-item>
                  <el-dropdown-item command="logout" divided>退出登录</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </div>
      </el-header>
      <el-main class="app-main">
        <RouterView />
      </el-main>
    </el-container>

    <!-- 登录页面 -->
    <RouterView v-else />
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

  .app-header {
    background-color: #ffffff;
    border-bottom: 1px solid #e6e6e6;
    padding: 0;
    line-height: 60px;
    height: 60px !important;
  }

  .header-content {
    display: flex;
    align-items: center;
    height: 100%;
    width: 100%;
    padding: 0 20px;
  }

  .logo {
    margin-right: 30px;
  }

  .logo h2 {
    margin: 0;
    color: #409eff;
    font-size: 20px;
  }

  .nav-menu {
    flex: 1;
    border-bottom: none;
  }

  .user-info {
    margin-left: auto;
  }

  .user-name {
    cursor: pointer;
    color: #333;
    font-size: 14px;
    display: flex;
    align-items: center;
  }

  .app-main {
    padding: 20px;
    background-color: #f5f5f5;
    min-height: calc(100vh - 60px);
  }

  /* 覆盖 Element Plus 的一些样式 */
  :deep(.el-menu--horizontal > .el-menu-item) {
    border-bottom: 2px solid transparent;
  }

  :deep(.el-menu--horizontal > .el-menu-item.is-active) {
    border-bottom: 2px solid #409eff;
    color: #409eff;
  }
</style>
