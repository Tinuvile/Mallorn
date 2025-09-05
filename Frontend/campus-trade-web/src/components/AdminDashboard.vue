<template>
  <v-card class="ma-4" elevation="2">
    <v-card-title class="text-h5 primary--text">
      <v-icon left color="primary">mdi-shield-account</v-icon>
      管理员控制台
    </v-card-title>
    <v-card-text>
      <v-row>
        <!-- 模块管理员入口 -->
        <v-col cols="12" md="6">
          <v-card outlined hover @click="goToModeratorPanel">
            <v-card-title class="text-h6">
              <v-icon left color="blue">mdi-account-supervisor</v-icon>
              模块管理员
            </v-card-title>
            <v-card-text>
              <p>管理本模块的商品信息和举报审核</p>
              <v-list dense>
                <v-list-item>
                  <v-list-item-icon>
                    <v-icon color="green">mdi-package-variant</v-icon>
                  </v-list-item-icon>
                  <v-list-item-content>
                    <v-list-item-title>商品管理</v-list-item-title>
                    <v-list-item-subtitle>CRUD操作，状态管理</v-list-item-subtitle>
                  </v-list-item-content>
                </v-list-item>
                <v-list-item>
                  <v-list-item-icon>
                    <v-icon color="orange">mdi-flag</v-icon>
                  </v-list-item-icon>
                  <v-list-item-content>
                    <v-list-item-title>举报审核</v-list-item-title>
                    <v-list-item-subtitle>处理用户举报，审核决策</v-list-item-subtitle>
                  </v-list-item-content>
                </v-list-item>
              </v-list>
            </v-card-text>
            <v-card-actions>
              <v-spacer></v-spacer>
              <v-btn color="primary" text>
                进入管理
                <v-icon right>mdi-arrow-right</v-icon>
              </v-btn>
            </v-card-actions>
          </v-card>
        </v-col>

        <!-- 系统管理员入口 -->
        <v-col cols="12" md="6">
          <v-card outlined hover @click="goToSystemAudit">
            <v-card-title class="text-h6">
              <v-icon left color="red">mdi-shield-crown</v-icon>
              系统管理员
            </v-card-title>
            <v-card-text>
              <p>监控和审计模块管理员操作</p>
              <v-list dense>
                <v-list-item>
                  <v-list-item-icon>
                    <v-icon color="purple">mdi-history</v-icon>
                  </v-list-item-icon>
                  <v-list-item-content>
                    <v-list-item-title>操作审计</v-list-item-title>
                    <v-list-item-subtitle>查看管理员操作历史</v-list-item-subtitle>
                  </v-list-item-content>
                </v-list-item>
                <v-list-item>
                  <v-list-item-icon>
                    <v-icon color="teal">mdi-chart-line</v-icon>
                  </v-list-item-icon>
                  <v-list-item-content>
                    <v-list-item-title>统计分析</v-list-item-title>
                    <v-list-item-subtitle>操作频率，异常预警</v-list-item-subtitle>
                  </v-list-item-content>
                </v-list-item>
              </v-list>
            </v-card-text>
            <v-card-actions>
              <v-spacer></v-spacer>
              <v-btn color="red" text>
                进入审计
                <v-icon right>mdi-arrow-right</v-icon>
              </v-btn>
            </v-card-actions>
          </v-card>
        </v-col>
      </v-row>

      <!-- 快速统计 -->
      <v-row class="mt-4">
        <v-col cols="12">
          <v-card outlined>
            <v-card-title>
              <v-icon left>mdi-chart-box</v-icon>
              系统概览
            </v-card-title>
            <v-card-text>
              <v-row>
                <v-col cols="6" md="3">
                  <v-card color="blue lighten-4" outlined>
                    <v-card-text class="text-center">
                      <v-icon size="40" color="blue">mdi-package-variant</v-icon>
                      <div class="text-h4 mt-2">{{ stats.totalGoods }}</div>
                      <div class="text-subtitle-1">总商品数</div>
                    </v-card-text>
                  </v-card>
                </v-col>
                <v-col cols="6" md="3">
                  <v-card color="orange lighten-4" outlined>
                    <v-card-text class="text-center">
                      <v-icon size="40" color="orange">mdi-flag</v-icon>
                      <div class="text-h4 mt-2">{{ stats.pendingReports }}</div>
                      <div class="text-subtitle-1">待审核举报</div>
                    </v-card-text>
                  </v-card>
                </v-col>
                <v-col cols="6" md="3">
                  <v-card color="green lighten-4" outlined>
                    <v-card-text class="text-center">
                      <v-icon size="40" color="green">mdi-account-supervisor</v-icon>
                      <div class="text-h4 mt-2">{{ stats.activeModerators }}</div>
                      <div class="text-subtitle-1">总管理员数</div>
                    </v-card-text>
                  </v-card>
                </v-col>
                <v-col cols="6" md="3">
                  <v-card color="purple lighten-4" outlined>
                    <v-card-text class="text-center">
                      <v-icon size="40" color="purple">mdi-clock-outline</v-icon>
                      <div class="text-h4 mt-2">{{ stats.todayOperations }}</div>
                      <div class="text-subtitle-1">今日操作数</div>
                    </v-card-text>
                  </v-card>
                </v-col>
              </v-row>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { adminApi } from '@/services/api'

const router = useRouter()

// 动态统计数据
const stats = reactive({
  totalGoods: 0,
  pendingReports: 0,
  activeModerators: 0,
  todayOperations: 0
})

// 获取统计数据
const fetchStatistics = async () => {
  try {
    const response = await adminApi.getAdminStatistics()
    if (response.success && response.data) {
      stats.totalGoods = response.data.totalProducts
      stats.pendingReports = response.data.pendingReports
      stats.activeModerators = response.data.activeModerators
      stats.todayOperations = response.data.todayOperations
    }
  } catch (error) {
    console.error('获取统计数据失败:', error)
    // 设置默认值
    stats.totalGoods = 0
    stats.pendingReports = 0
    stats.activeModerators = 0
    stats.todayOperations = 0
  }
}

// 导航方法
const goToModeratorPanel = () => {
  router.push('/admin/moderator')
}

const goToSystemAudit = () => {
  router.push('/admin/system-audit')
}

// 组件挂载时获取数据
onMounted(() => {
  fetchStatistics()
})
</script>

<style scoped>
.v-card:hover {
  transform: translateY(-2px);
  transition: transform 0.2s ease-in-out;
}
</style>
