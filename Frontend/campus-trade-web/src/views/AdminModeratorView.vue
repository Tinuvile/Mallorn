<template>
  <div class="app-container">
    <!-- 顶部导航栏 -->
    <header class="top-navbar">
      <div class="navbar-content">
        <div class="left-section">
          <span class="icon">
            <svg width="24px" height="24px" stroke-width="1.5" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" color="#000000">
              <path d="M12 2L2 7L12 12L22 7L12 2Z" stroke="#000000" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
              <path d="M2 17L12 22L22 17" stroke="#000000" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
              <path d="M2 12L12 17L22 12" stroke="#000000" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
            </svg>
          </span>
          <span class="title">Campus Secondhand</span>
          <span class="subtitle">模块管理员 - {{ currentModule }}</span>
        </div>
        
        <div class="right-section">
          <div class="admin-info">
            <span class="admin-name">{{ adminInfo.name }}</span>
          </div>
          <v-btn 
            fab
            to="/"
            class="logout-btn"
            color="white"
            size="46"
          >
            <v-icon size="32" color="red-darken-1">mdi-logout-variant</v-icon>
          </v-btn>
        </div>
      </div>
    </header>

    <!-- 主内容区 -->
    <div class="container">
      <div class="header">
        <div class="page-title-container">
          <div class="page-title">
            <span class="title-text">模块管理控制台</span>
            <span class="title-line"></span>
          </div>
        </div>
      </div>

      <!-- 功能切换标签 -->
      <div class="tabs-container">
        <div class="tab-buttons">
          <button 
            class="tab-button" 
            :class="{ active: activeTab === 'goods' }"
            @click="activeTab = 'goods'"
          >
            商品管理
          </button>
          <button 
            class="tab-button" 
            :class="{ active: activeTab === 'reports' }"
            @click="activeTab = 'reports'"
          >
            举报审核
          </button>
        </div>
      </div>

      <!-- 内容区域 -->
      <div class="content-area">
        <div v-if="activeTab === 'goods'" class="tab-content">
          <GoodsManagement />
        </div>
        <div v-if="activeTab === 'reports'" class="tab-content">
          <ReportModeration />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import GoodsManagement from '@/components/admin/GoodsManagement.vue'
import ReportModeration from '@/components/admin/ReportModeration.vue'

// 响应式数据
const activeTab = ref('goods')
const currentModule = ref('电子产品')

const adminInfo = reactive({
  name: '张三',
  id: 'admin001',
  module: '电子产品'
})
</script>

<style scoped>
.app-container {
  min-height: 100vh;
  background-color: #f5f5f5;
}

/* 顶部导航栏样式 */
.top-navbar {
  background-color: white;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  position: sticky;
  top: 0;
  z-index: 100;
}

.navbar-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 24px;
  height: 64px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.left-section {
  display: flex;
  align-items: center;
  gap: 15px;
}

.icon {
  display: flex;
  align-items: center;
}

.title {
  font-size: 20px;
  font-weight: bold;
  color: #333;
}

.subtitle {
  font-size: 14px;
  color: #666;
  background-color: #FFE8F0;
  padding: 4px 12px;
  border-radius: 16px;
}

.right-section {
  display: flex;
  align-items: center;
  gap: 16px;
}

.admin-info {
  display: flex;
  align-items: center;
  gap: 8px;
}

.admin-name {
  font-size: 14px;
  color: #333;
  font-weight: 500;
}

.logout-btn {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1) !important;
}

/* 主容器样式 */
.container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 24px;
}

.header {
  padding: 20px 0;
  margin-bottom: 30px;
}

.page-title-container {
  width: 100%;
}

.page-title {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
}

.title-text {
  font-size: 32px;
  font-weight: bold;
  color: #333;
  position: relative;
  padding-bottom: 8px;
  margin-bottom: 15px;
}

.title-line {
  display: block;
  width: 60px;
  height: 4px;
  background: linear-gradient(90deg, #FF85A2, #FFD1DC);
  border-radius: 2px;
}

/* 标签页样式 */
.tabs-container {
  margin-bottom: 30px;
}

.tab-buttons {
  display: flex;
  gap: 8px;
  background-color: white;
  padding: 8px;
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
  width: fit-content;
}

.tab-button {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 24px;
  border: none;
  background-color: transparent;
  border-radius: 8px;
  cursor: pointer;
  font-size: 16px;
  color: #666;
  transition: all 0.2s ease;
}

.tab-button:hover {
  background-color: #f9f9f9;
  color: #333;
}

.tab-button.active {
  background-color: #FF85A2;
  color: white;
  box-shadow: 0 2px 8px rgba(255, 133, 162, 0.3);
}

.tab-icon {
  font-size: 18px;
}

/* 内容区域 */
.content-area {
  min-height: 500px;
}

.tab-content {
  background-color: white;
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
  padding: 24px;
}
</style>
