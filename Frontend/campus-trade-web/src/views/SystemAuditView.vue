<template>
  <div class="app-container">
    <!-- 顶部导航栏 -->
    <header class="top-navbar">
      <div class="navbar-content">
        <div class="left-section">
          <span class="icon">
            <svg width="24px" height="24px" stroke-width="1.5" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" color="#000000">
              <path d="M3 7V17C3 18.1046 3.89543 19 5 19H19C20.1046 19 21 18.1046 21 17V7" stroke="#000000" stroke-width="1.5"></path>
              <path d="M21 7L16 2H8L3 7" stroke="#000000" stroke-width="1.5"></path>
              <path d="M9 12H15" stroke="#000000" stroke-width="1.5" stroke-linecap="round"></path>
              <path d="M9 15H12" stroke="#000000" stroke-width="1.5" stroke-linecap="round"></path>
            </svg>
          </span>
          <span class="title">Campus Secondhand</span>
          <span class="subtitle">系统管理员 - 操作审计</span>
        </div>
        
        <div class="right-section">
          <div class="admin-info">
            <span class="admin-name">{{ systemAdmin.name }}</span>
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
            <span class="title-text">操作审计中心</span>
            <span class="title-line"></span>
          </div>
        </div>
      </div>

      <!-- 筛选控制面板 -->
      <div class="filter-card">
        <div class="card-title">
          操作日志筛选
        </div>
        <div class="filter-grid">
          <div class="filter-item">
            <label>模块</label>
            <select v-model="filters.module" class="filter-select">
              <option value="">全部模块</option>
              <option v-for="module in moduleOptions" :key="module" :value="module">{{ module }}</option>
            </select>
          </div>
          <div class="filter-item">
            <label>模块管理员</label>
            <select v-model="filters.moderator" class="filter-select">
              <option value="">全部管理员</option>
              <option v-for="moderator in moderatorOptions" :key="moderator" :value="moderator">{{ moderator }}</option>
            </select>
          </div>
          <div class="filter-item">
            <label>操作类型</label>
            <select v-model="filters.operationType" class="filter-select">
              <option value="">全部类型</option>
              <option v-for="type in operationTypes" :key="type" :value="type">{{ type }}</option>
            </select>
          </div>
          <div class="filter-item">
            <label>时间范围</label>
            <input v-model="filters.dateRange" type="date" class="filter-input">
          </div>
        </div>
        <div class="filter-actions">
          <button class="action-btn primary" @click="searchLogs">
            搜索
          </button>
          <button class="action-btn secondary" @click="resetFilters">
            重置
          </button>
          <button class="action-btn success" @click="exportLogs">
            导出
          </button>
        </div>
      </div>

      <!-- 统计图表区域 -->
      <div class="charts-container">
        <div class="chart-card">
          <div class="card-title">
            操作频率统计
          </div>
          <div class="chart-container">
            <div id="operationChart" class="chart-placeholder">
              <p>图表加载中...</p>
            </div>
          </div>
        </div>
        <div class="chart-card">
          <div class="card-title">
            异常操作预警
          </div>
          <div class="warnings-list">
            <div 
              v-for="warning in warnings" 
              :key="warning.id"
              class="warning-item"
              :class="`warning-${warning.level}`"
            >
              <div class="warning-icon">
                {{ getWarningIcon(warning.level) }}
              </div>
              <div class="warning-content">
                <div class="warning-message">{{ warning.message }}</div>
                <div class="warning-time">{{ warning.time }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 操作日志表格 -->
      <div class="table-card">
        <div class="card-header">
          <div class="card-title">
            操作日志记录
          </div>
          <div class="search-box">
            <input 
              v-model="search" 
              placeholder="搜索日志..." 
              class="search-input"
            >
          </div>
        </div>
        
        <div class="table-container">
          <table class="data-table">
            <thead>
              <tr>
                <th v-for="header in headers" :key="header.value">
                  {{ header.text }}
                </th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in filteredLogs" :key="item.id" :class="{ loading: loading }">
                <td>{{ item.timestamp }}</td>
                <td>{{ item.moderator }}</td>
                <td>{{ item.module }}</td>
                <td>
                  <span class="operation-tag" :class="getOperationClass(item.operationType)">
                    {{ item.operationType }}
                  </span>
                </td>
                <td>{{ item.targetObject }}</td>
                <td>
                  <span class="status-tag" :class="getStatusClass(item.status)">
                    {{ item.status }}
                  </span>
                </td>
                <td>
                  <button class="table-action-btn" @click="viewDetails(item)">
                    查看
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- 操作详情模态框 -->
    <div v-if="detailDialog" class="modal-overlay" @click="detailDialog = false">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>操作详情</h3>
          <button class="modal-close" @click="detailDialog = false">✕</button>
        </div>
        <div class="modal-body" v-if="selectedLog">
          <div class="detail-grid">
            <div class="detail-item">
              <label>操作时间</label>
              <div class="detail-value">{{ selectedLog.timestamp }}</div>
            </div>
            <div class="detail-item">
              <label>操作类型</label>
              <div class="detail-value">{{ selectedLog.operationType }}</div>
            </div>
            <div class="detail-item">
              <label>模块管理员</label>
              <div class="detail-value">{{ selectedLog.moderator }}</div>
            </div>
            <div class="detail-item">
              <label>所属模块</label>
              <div class="detail-value">{{ selectedLog.module }}</div>
            </div>
            <div class="detail-item full-width">
              <label>操作详情</label>
              <div class="detail-value">{{ selectedLog.details }}</div>
            </div>
            <div class="detail-item">
              <label>涉及对象</label>
              <div class="detail-value">{{ selectedLog.targetObject }}</div>
            </div>
            <div class="detail-item">
              <label>操作结果</label>
              <div class="detail-value">{{ selectedLog.status }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'

// 定义类型
interface OperationLog {
  id: number
  timestamp: string
  moderator: string
  module: string
  operationType: string
  targetObject: string
  status: string
  details: string
}

interface Warning {
  id: number
  level: string
  message: string
  time: string
}

// 响应式数据
const loading = ref(false)
const search = ref('')
const detailDialog = ref(false)
const selectedLog = ref<OperationLog | null>(null)

const systemAdmin = reactive({
  name: '李四',
  id: 'sysadmin001'
})

const filters = reactive({
  module: null as string | null,
  moderator: null as string | null,
  operationType: null as string | null,
  dateRange: null as string | null
})

// 选项数据
const moduleOptions = [
  '电子产品', '图书文具', '生活用品', '服装配饰', '运动器材'
]

const moderatorOptions = [
  '张三', '李四', '王五', '赵六'
]

const operationTypes = [
  '新增商品', '编辑商品', '删除商品', '审核举报', '通过举报', '驳回举报'
]

// 表格头部
const headers = [
  { text: '时间', value: 'timestamp' },
  { text: '模块管理员', value: 'moderator' },
  { text: '模块', value: 'module' },
  { text: '操作类型', value: 'operationType' },
  { text: '涉及对象', value: 'targetObject' },
  { text: '状态', value: 'status' },
  { text: '操作', value: 'actions' }
]

// 模拟操作日志数据
const operationLogs = ref<OperationLog[]>([
  {
    id: 1,
    timestamp: '2025-01-20 14:30:25',
    moderator: '张三',
    module: '电子产品',
    operationType: '新增商品',
    targetObject: 'iPhone 13 Pro',
    status: '成功',
    details: '新增了一个iPhone 13 Pro商品，价格5999元'
  },
  {
    id: 2,
    timestamp: '2025-01-20 15:45:12',
    moderator: '李四',
    module: '图书文具',
    operationType: '审核举报',
    targetObject: '高等数学教材',
    status: '成功',
    details: '审核了关于高等数学教材的价格争议举报，判定举报成立'
  }
])

// 警告信息
const warnings = ref<Warning[]>([
  {
    id: 1,
    level: 'high',
    message: '张三在30分钟内删除了5个商品',
    time: '2025-01-20 16:00:00'
  },
  {
    id: 2,
    level: 'medium',
    message: '李四今日审核举报数量异常偏高',
    time: '2025-01-20 15:30:00'
  }
])

// 计算属性
const filteredLogs = computed(() => {
  return operationLogs.value.filter(log => {
    if (search.value && !log.moderator.includes(search.value) && !log.targetObject.includes(search.value)) {
      return false
    }
    if (filters.module && log.module !== filters.module) return false
    if (filters.moderator && log.moderator !== filters.moderator) return false
    if (filters.operationType && log.operationType !== filters.operationType) return false
    return true
  })
})

// 方法
const searchLogs = () => {
  loading.value = true
  setTimeout(() => {
    loading.value = false
  }, 1000)
}

const resetFilters = () => {
  Object.keys(filters).forEach((key) => {
    (filters as any)[key] = null
  })
}

const exportLogs = () => {
  console.log('导出日志')
}

const viewDetails = (item: OperationLog) => {
  selectedLog.value = item
  detailDialog.value = true
}

const getOperationClass = (type: string) => {
  const classes: Record<string, string> = {
    '新增商品': 'operation-add',
    '编辑商品': 'operation-edit',
    '删除商品': 'operation-delete',
    '审核举报': 'operation-review',
    '通过举报': 'operation-approve',
    '驳回举报': 'operation-reject'
  }
  return classes[type] || 'operation-default'
}

const getStatusClass = (status: string) => {
  return status === '成功' ? 'status-success' : 'status-error'
}

const getWarningIcon = (level: string) => {
  const icons: Record<string, string> = {
    'high': '●',
    'medium': '●',
    'low': '●'
  }
  return icons[level] || '●'
}

onMounted(() => {
  // 组件挂载后的初始化逻辑
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

/* 卡片通用样式 */
.filter-card, .chart-card, .table-card {
  background-color: white;
  border-radius: 12px;
  padding: 24px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
  margin-bottom: 24px;
}

.card-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 18px;
  font-weight: bold;
  color: #333;
  margin-bottom: 20px;
}

.title-icon {
  font-size: 20px;
}

/* 筛选面板样式 */
.filter-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px;
  margin-bottom: 20px;
}

.filter-item {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.filter-item label {
  font-size: 14px;
  color: #666;
  font-weight: 500;
}

.filter-select, .filter-input {
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 14px;
  background-color: white;
  transition: border-color 0.2s ease;
}

.filter-select:focus, .filter-input:focus {
  outline: none;
  border-color: #FF85A2;
}

.filter-actions {
  display: flex;
  gap: 12px;
  justify-content: flex-end;
}

.action-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 20px;
  border: none;
  border-radius: 8px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.action-btn.primary {
  background-color: #FF85A2;
  color: white;
}

.action-btn.secondary {
  background-color: #f5f5f5;
  color: #666;
}

.action-btn.success {
  background-color: #4CAF50;
  color: white;
}

.action-btn:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

/* 图表区域样式 */
.charts-container {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
  gap: 24px;
  margin-bottom: 24px;
}

.chart-container {
  height: 300px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.chart-placeholder {
  width: 100%;
  height: 100%;
  background-color: #f9f9f9;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #666;
}

/* 警告列表样式 */
.warnings-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.warning-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  border-radius: 8px;
  border-left: 4px solid;
}

.warning-item.warning-high {
  background-color: #ffebee;
  border-left-color: #f44336;
}

.warning-item.warning-medium {
  background-color: #fff3e0;
  border-left-color: #ff9800;
}

.warning-item.warning-low {
  background-color: #e8f5e8;
  border-left-color: #4caf50;
}

.warning-icon {
  font-size: 20px;
}

.warning-content {
  flex: 1;
}

.warning-message {
  font-size: 14px;
  color: #333;
  margin-bottom: 4px;
}

.warning-time {
  font-size: 12px;
  color: #666;
}

/* 表格样式 */
.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.search-box {
  display: flex;
  align-items: center;
}

.search-input {
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 14px;
  width: 250px;
}

.search-input:focus {
  outline: none;
  border-color: #FF85A2;
}

.table-container {
  overflow-x: auto;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
  background-color: white;
}

.data-table th,
.data-table td {
  padding: 12px 16px;
  text-align: left;
  border-bottom: 1px solid #eee;
}

.data-table th {
  background-color: #f9f9f9;
  font-weight: 600;
  color: #555;
  font-size: 14px;
}

.data-table tr:hover {
  background-color: #fafafa;
}

.operation-tag, .status-tag {
  padding: 4px 8px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 500;
}

.operation-add { background-color: #e8f5e8; color: #4caf50; }
.operation-edit { background-color: #e3f2fd; color: #2196f3; }
.operation-delete { background-color: #ffebee; color: #f44336; }
.operation-review { background-color: #fff3e0; color: #ff9800; }
.operation-approve { background-color: #e8f5e8; color: #4caf50; }
.operation-reject { background-color: #ffebee; color: #f44336; }
.operation-default { background-color: #f5f5f5; color: #666; }

.status-success { background-color: #e8f5e8; color: #4caf50; }
.status-error { background-color: #ffebee; color: #f44336; }

.table-action-btn {
  padding: 6px 12px;
  border: none;
  border-radius: 6px;
  background-color: #FF85A2;
  color: white;
  font-size: 12px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.table-action-btn:hover {
  background-color: #ff6b90;
}

/* 模态框样式 */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background-color: white;
  border-radius: 12px;
  width: 90%;
  max-width: 600px;
  max-height: 80vh;
  overflow-y: auto;
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 24px;
  border-bottom: 1px solid #eee;
}

.modal-header h3 {
  margin: 0;
  font-size: 20px;
  color: #333;
}

.modal-close {
  background: none;
  border: none;
  font-size: 24px;
  cursor: pointer;
  color: #666;
  padding: 0;
  width: 30px;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.modal-body {
  padding: 24px;
}

.detail-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 16px;
}

.detail-item {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.detail-item.full-width {
  grid-column: 1 / -1;
}

.detail-item label {
  font-size: 14px;
  color: #666;
  font-weight: 500;
}

.detail-value {
  font-size: 14px;
  color: #333;
  padding: 8px 12px;
  background-color: #f9f9f9;
  border-radius: 6px;
}
</style>
