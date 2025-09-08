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

      <!-- 左右分栏布局 -->
      <div class="main-layout">
        <!-- 左侧：操作日志区域 -->
        <div class="left-panel">
          <!-- 筛选控制面板 -->
          <div class="filter-card">
            <div class="card-title">
              操作日志筛选
            </div>
            <div class="filter-grid">
              <div class="filter-item">
                <label>模块</label>
                <select v-model="filters.module" class="filter-select" @change="onModuleChange">
                  <option value="">全部模块</option>
                  <option v-for="module in moduleOptions" :key="module.categoryId" :value="module.categoryId">{{ module.name }}</option>
                </select>
              </div>
              <div class="filter-item">
                <label>模块管理员</label>
                <select v-model="filters.moderator" class="filter-select">
                  <option value="">全部管理员</option>
                  <option v-for="moderator in availableModerators" :key="moderator.adminId" :value="moderator.adminId">
                    {{ moderator.username }} ({{ moderator.role === 'super' ? '系统管理员' : '分类管理员' }})
                  </option>
                </select>
              </div>
              <div class="filter-item">
                <label>操作类型</label>
                <select v-model="filters.operationType" class="filter-select">
                  <option value="">全部类型</option>
                  <option v-for="type in operationTypes" :key="type.value" :value="type.value">{{ type.label }}</option>
                </select>
              </div>
              <div class="filter-item">
                <label>开始时间</label>
                <input v-model="filters.startDate" type="date" class="filter-input">
              </div>
              <div class="filter-item">
                <label>结束时间</label>
                <input v-model="filters.endDate" type="date" class="filter-input">
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

        <!-- 右侧：权限管理区域 -->
        <div class="right-panel">
          <!-- 权限管理面板 -->
          <div class="permission-card">
            <div class="card-title">
              权限管理中心
            </div>
            
            <!-- 权限管理操作选项 -->
            <div class="permission-tabs">
              <button 
                v-for="tab in permissionTabs" 
                :key="tab.value"
                class="tab-btn"
                :class="{ active: activeTab === tab.value }"
                @click="activeTab = tab.value"
              >
                {{ tab.label }}
              </button>
            </div>

            <!-- 权限分配面板 -->
            <div v-if="activeTab === 'assign'" class="permission-panel">
              <div class="panel-header">
                <h3>权限分配</h3>
                <button class="action-btn primary" @click="showAssignDialog = true">
                  新增管理员
                </button>
              </div>
              
              <div class="admins-grid">
                <div 
                  v-for="admin in categoryAdmins" 
                  :key="admin.adminId"
                  class="admin-card"
                >
                  <div class="admin-info">
                    <div class="admin-name">{{ admin.username }}</div>
                    <div class="admin-role">{{ getRoleName(admin.role) }}</div>
                    <div class="admin-category" v-if="admin.assignedCategory">
                      {{ getCategoryName(admin.assignedCategory) }}
                    </div>
                  </div>
                  <div class="admin-actions">
                    <button class="table-action-btn" @click="editAdmin(admin)">编辑</button>
                    <button class="table-action-btn danger" @click="revokeAdmin(admin)">撤销</button>
                  </div>
                </div>
              </div>
            </div>

            <!-- 权限修改面板 -->
            <div v-if="activeTab === 'modify'" class="permission-panel">
              <div class="panel-header">
                <h3>权限修改</h3>
              </div>
              
              <div class="modification-form">
                <div class="form-group">
                  <label>选择管理员</label>
                  <select v-model.number="modifyForm.adminId" class="filter-select">
                    <option :value="null">请选择管理员</option>
                    <option v-for="admin in categoryAdmins" :key="admin.adminId" :value="admin.adminId">
                      {{ admin.username }} - {{ getRoleName(admin.role) }}
                    </option>
                  </select>
                </div>
                
                <div class="form-group" v-if="modifyForm.adminId">
                  <label>新的分类权限</label>
                  <select v-model.number="modifyForm.newCategoryId" class="filter-select">
                    <option :value="null">请选择分类</option>
                    <option v-for="category in moduleOptions" :key="category.categoryId" :value="category.categoryId">
                      {{ category.name }}
                    </option>
                  </select>
                </div>
                
                <div class="form-actions" v-if="modifyForm.adminId && modifyForm.newCategoryId">
                  <button class="action-btn primary" @click="modifyPermission">
                    确认修改
                  </button>
                  <button class="action-btn secondary" @click="resetModifyForm">
                    重置
                  </button>
                </div>
              </div>
            </div>

            <!-- 权限撤销面板 -->
            <div v-if="activeTab === 'revoke'" class="permission-panel">
              <div class="panel-header">
                <h3>权限撤销</h3>
              </div>
              
              <div class="revoke-list">
                <div 
                  v-for="admin in categoryAdmins.filter(a => a.role !== 'super')" 
                  :key="admin.adminId"
                  class="revoke-item"
                >
                  <div class="admin-summary">
                    <div class="admin-name">{{ admin.username }}</div>
                    <div class="admin-details">
                      {{ getRoleName(admin.role) }} - {{ getCategoryName(admin.assignedCategory) }}
                    </div>
                  </div>
                  <button class="action-btn danger" @click="confirmRevoke(admin)">
                    撤销权限
                  </button>
                </div>
              </div>
            </div>
          </div>
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
              <label>操作管理员</label>
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

    <!-- 新增管理员对话框 -->
    <div v-if="showAssignDialog" class="modal-overlay" @click="showAssignDialog = false">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>分配管理员权限</h3>
          <button class="modal-close" @click="showAssignDialog = false">✕</button>
        </div>
        <div class="modal-body">
          <div class="assign-form">
            <div class="form-group">
              <label>用户名</label>
              <input 
                v-model="assignForm.username" 
                type="text" 
                class="form-input" 
                placeholder="请输入用户名"
                required
              >
            </div>
            <div class="form-group">
              <label>邮箱</label>
              <input 
                v-model="assignForm.email" 
                type="email" 
                class="form-input" 
                placeholder="请输入邮箱地址"
                required
              >
            </div>
            <div class="form-group">
              <label>分配分类</label>
              <select v-model="assignForm.categoryId" class="form-select" required>
                <option value="">请选择要管理的分类</option>
                <option v-for="category in moduleOptions" :key="category.categoryId" :value="category.categoryId">
                  {{ category.name }}
                </option>
              </select>
            </div>
          </div>
          <div class="modal-actions">
            <button class="action-btn secondary" @click="showAssignDialog = false">
              取消
            </button>
            <button 
              class="action-btn primary" 
              @click="assignPermission"
              :disabled="!assignForm.username || !assignForm.email || !assignForm.categoryId"
            >
              确认分配
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { adminApi, categoryApi } from '@/services/api'

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

interface AdminInfo {
  adminId: number
  userId?: number
  username: string
  role: string
  assignedCategory?: number
  assignedCategoryName?: string
  isActive?: boolean
  email?: string
  roleDisplayName?: string
  createdAt?: string
}

interface ModuleOption {
  name: string
  categoryId: number
}

interface OperationType {
  value: string
  label: string
}

// 响应式数据
const loading = ref(false)
const search = ref('')
const detailDialog = ref(false)
const selectedLog = ref<OperationLog | null>(null)

// 权限管理相关数据
const activeTab = ref('assign')
const showAssignDialog = ref(false)
const showEditDialog = ref(false)

const permissionTabs = [
  { value: 'assign', label: '权限分配' },
  { value: 'modify', label: '权限修改' },
  { value: 'revoke', label: '权限撤销' }
]

const modifyForm = reactive({
  adminId: null as number | null,
  newCategoryId: null as number | null
})

const assignForm = reactive({
  username: '',
  email: '',
  categoryId: null as number | null
})

const categoryAdmins = ref<AdminInfo[]>([])

const systemAdmin = reactive({
  name: '系统管理员',
  id: 'sysadmin001'
})

const filters = reactive({
  module: null as string | null,
  moderator: null as string | null,
  operationType: null as string | null,
  startDate: null as string | null,
  endDate: null as string | null
})

// 分页信息
const pageIndex = ref(0)
const pageSize = ref(20)
const totalCount = ref(0)

// 选项数据
const moduleOptions = ref<ModuleOption[]>([])
const allModerators = ref<AdminInfo[]>([])

const operationTypes: OperationType[] = [
  { value: '封禁用户', label: '封禁用户' },
  { value: '修改权限', label: '修改权限' },
  { value: '处理举报', label: '处理举报' },
  { value: '更新商品', label: '更新商品' },
  { value: '删除商品', label: '删除商品' },
  { value: '批量操作', label: '批量操作' }
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

// 操作日志数据
const operationLogs = ref<OperationLog[]>([])

// 计算属性
const availableModerators = computed(() => {
  if (!filters.module) {
    // 如果没有选择模块，显示所有管理员
    return allModerators.value
  }
  
  // 如果选择了模块，显示系统管理员和该分类的管理员
  return allModerators.value.filter(admin => 
    admin.role === 'super' || 
    (admin.role === 'category' && admin.assignedCategory === parseInt(filters.module || '0'))
  )
})

const filteredLogs = computed(() => {
  let logs = operationLogs.value
  
  // 前端只进行基本的文本搜索筛选
  if (search.value) {
    const searchLower = search.value.toLowerCase()
    logs = logs.filter(log => 
      log.moderator.toLowerCase().includes(searchLower) ||
      log.targetObject.toLowerCase().includes(searchLower) ||
      log.operationType.toLowerCase().includes(searchLower) ||
      log.details.toLowerCase().includes(searchLower)
    )
  }
  
  return logs
})

// API 调用方法
const fetchAuditLogs = async () => {
  try {
    loading.value = true
    
    const startDate = filters.startDate ? new Date(filters.startDate) : undefined
    const endDate = filters.endDate ? new Date(filters.endDate) : undefined
    
    // 根据选中的模块ID来筛选
    const categoryId = filters.module ? parseInt(filters.module) : undefined
    
    const response = await adminApi.getAllAuditLogs(
      pageIndex.value,
      pageSize.value,
      filters.moderator ? parseInt(filters.moderator) : undefined, // targetAdminId
      filters.operationType || undefined,
      categoryId, // 传递分类ID用于筛选管理员
      startDate,
      endDate
    )
    
    if (response.success && response.data) {
      // 转换API响应为组件需要的格式
      operationLogs.value = response.data.logs.map((log: any) => ({
        id: log.logId,
        timestamp: new Date(log.logTime).toLocaleString(),
        moderator: log.adminUsername || '未知管理员',
        module: getModuleFromCategory(log.adminRole, log.adminId) || '系统管理',
        operationType: log.actionType || '未知操作',
        targetObject: getTargetDescription(log.actionType, log.targetId) || `目标ID: ${log.targetId || '未知'}`,
        status: '成功', // 根据实际需要可以从其他字段获取
        details: log.logDetail || ''
      }))
      
      totalCount.value = response.data.pagination?.totalCount || response.data.logs.length
      
      // 更新管理员列表（从API响应中提取）
      const uniqueAdmins = new Map<number, AdminInfo>()
      response.data.logs.forEach((log: any) => {
        if (log.adminId && !uniqueAdmins.has(log.adminId)) {
          uniqueAdmins.set(log.adminId, {
            adminId: log.adminId,
            username: log.adminUsername || '未知管理员',
            role: log.adminRole || 'unknown',
            assignedCategory: log.adminRole === 'category' ? categoryId : undefined
          })
        }
      })
      allModerators.value = Array.from(uniqueAdmins.values())
    }
  } catch (error) {
    console.error('获取审计日志失败:', error)
    // 使用模拟数据作为备用
    loadMockData()
  } finally {
    loading.value = false
  }
}

// 加载所有管理员信息
const loadModerators = async () => {
  try {
    // 目前使用模拟数据，实际应该调用相应的API
    allModerators.value = [
      { adminId: 1, username: '系统管理员', role: 'super' },
      { adminId: 2, username: '张三', role: 'category', assignedCategory: 1 },
      { adminId: 3, username: '李四', role: 'category', assignedCategory: 2 },
      { adminId: 4, username: '王五', role: 'category', assignedCategory: 3 }
    ]
  } catch (error) {
    console.error('获取管理员列表失败:', error)
  }
}

// 加载分类数据
const loadCategoryData = async () => {
  try {
    const response = await categoryApi.getCategoryTree()
    if (response.success && response.data?.root_categories) {
      // 将分类树转换为平铺的模块选项列表
      const categories: Array<{name: string, categoryId: number}> = []
      
      // 递归提取所有分类
      const extractCategories = (items: any[]) => {
        items.forEach(item => {
          categories.push({
            name: item.name,
            categoryId: item.category_id
          })
          if (item.children && item.children.length > 0) {
            extractCategories(item.children)
          }
        })
      }
      
      extractCategories(response.data.root_categories)
      moduleOptions.value = categories
    }
  } catch (error) {
    console.error('加载分类数据失败:', error)
    // 如果加载失败，使用默认数据
    moduleOptions.value = [
      { name: '电子产品', categoryId: 1 },
      { name: '图书文具', categoryId: 2 },
      { name: '生活用品', categoryId: 3 },
      { name: '服装配饰', categoryId: 4 },
      { name: '运动器材', categoryId: 5 }
    ]
  }
}

const loadMockData = () => {
  operationLogs.value = [
    {
      id: 1,
      timestamp: '2024-01-15 14:30:00',
      moderator: '张三',
      module: '电子产品',
      operationType: '新增商品',
      targetObject: 'iPhone 13 Pro',
      status: '成功',
      details: '新增了一个iPhone 13 Pro商品，价格5999元'
    },
    {
      id: 2,
      timestamp: '2024-01-15 13:20:00',
      moderator: '李四',
      module: '图书文具',
      operationType: '审核举报',
      targetObject: '高等数学教材',
      status: '成功',
      details: '审核了关于高等数学教材的价格争议举报，判定举报成立'
    }
  ]
  
  // 设置模拟管理员数据
  allModerators.value = [
    { adminId: 1, username: '系统管理员', role: 'super' },
    { adminId: 2, username: '张三', role: 'category', assignedCategory: 1 },
    { adminId: 3, username: '李四', role: 'category', assignedCategory: 2 },
    { adminId: 4, username: '王五', role: 'category', assignedCategory: 3 }
  ]
  
  totalCount.value = operationLogs.value.length
}

// 方法
const getModuleFromCategory = (adminRole: string, adminId: number): string => {
  if (adminRole === 'super') {
    return '系统管理'
  }
  
  // 根据管理员ID找到对应的分类
  const admin = allModerators.value.find(a => a.adminId === adminId)
  if (admin?.assignedCategory) {
    const module = moduleOptions.value.find(m => m.categoryId === admin.assignedCategory)
    return module?.name || '未知模块'
  }
  
  return '系统管理'
}

const getModuleFromActionType = (actionType: string): string => {
  const moduleMap: Record<string, string> = {
    '封禁用户': '用户管理',
    '修改权限': '权限管理',
    '处理举报': '举报管理',
    '更新商品': '商品管理',
    '删除商品': '商品管理',
    '批量操作': '系统管理'
  }
  return moduleMap[actionType] || '系统管理'
}

const getTargetDescription = (actionType: string, targetId?: number): string => {
  if (!targetId) return '未知对象'
  
  const descriptions: Record<string, string> = {
    '封禁用户': `用户ID: ${targetId}`,
    '修改权限': `管理员ID: ${targetId}`,
    '处理举报': `举报ID: ${targetId}`,
    '更新商品': `商品ID: ${targetId}`,
    '删除商品': `商品ID: ${targetId}`,
    '批量操作': `操作批次: ${targetId}`
  }
  return descriptions[actionType] || `目标ID: ${targetId}`
}

const onModuleChange = () => {
  // 当模块改变时，重置管理员筛选
  filters.moderator = null
  // 可选：立即搜索
  searchLogs()
}

const searchLogs = () => {
  pageIndex.value = 0
  fetchAuditLogs()
}

const resetFilters = () => {
  filters.module = null
  filters.moderator = null
  filters.operationType = null
  filters.startDate = null
  filters.endDate = null
  search.value = ''
  pageIndex.value = 0
  fetchAuditLogs()
}

const exportLogs = () => {
  // 导出功能实现
  const csvContent = [
    ['时间', '管理员', '模块', '操作类型', '涉及对象', '状态', '详情'].join(','),
    ...filteredLogs.value.map(log => [
      log.timestamp,
      log.moderator,
      log.module,
      log.operationType,
      log.targetObject,
      log.status,
      log.details
    ].join(','))
  ].join('\n')
  
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
  const link = document.createElement('a')
  const url = URL.createObjectURL(blob)
  link.setAttribute('href', url)
  link.setAttribute('download', `audit_logs_${new Date().toISOString().split('T')[0]}.csv`)
  link.style.visibility = 'hidden'
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
}

const viewDetails = (item: OperationLog) => {
  selectedLog.value = item
  detailDialog.value = true
}

const getOperationClass = (type: string) => {
  const classes: Record<string, string> = {
    '封禁用户': 'operation-ban',
    '修改权限': 'operation-permission',
    '处理举报': 'operation-report',
    '更新商品': 'operation-edit',
    '删除商品': 'operation-delete',
    '批量操作': 'operation-batch'
  }
  return classes[type] || 'operation-default'
}

const getStatusClass = (status: string) => {
  return status === '成功' ? 'status-success' : 'status-error'
}

// 权限管理方法
const getRoleName = (role: string): string => {
  const roleNames: Record<string, string> = {
    'super': '系统管理员',
    'category': '分类管理员',
    'category_admin': '分类管理员',
    'unknown': '未知角色'
  }
  return roleNames[role] || '未知角色'
}

const getCategoryName = (categoryId?: number): string => {
  if (!categoryId) return '无分配'
  const category = moduleOptions.value.find(m => m.categoryId === categoryId)
  return category?.name || '未知分类'
}

const loadCategoryAdmins = async () => {
  try {
    console.log('正在加载分类管理员列表...')
    const response = await adminApi.getAllAdmins(0, 100, 'category_admin')
    console.log('管理员列表响应:', response)
    
    if (response.success && response.data) {
      categoryAdmins.value = response.data.admins.filter((admin: any) => admin.role !== 'super')
      console.log('过滤后的分类管理员:', categoryAdmins.value)
    }
  } catch (error) {
    console.error('获取分类管理员列表失败:', error)
    // 使用模拟数据
    categoryAdmins.value = [
      { adminId: 2, username: '张三', role: 'category_admin', assignedCategory: 1, isActive: true },
      { adminId: 3, username: '李四', role: 'category_admin', assignedCategory: 2, isActive: true },
      { adminId: 4, username: '王五', role: 'category_admin', assignedCategory: 3, isActive: true }
    ]
    console.log('使用模拟数据:', categoryAdmins.value)
  }
}

const assignPermission = async () => {
  try {
    const response = await adminApi.assignCategoryAdmin({
      username: assignForm.username,
      email: assignForm.email,
      categoryId: assignForm.categoryId!
    })
    
    if (response.success) {
      alert('权限分配成功！')
      resetAssignForm()
      loadCategoryAdmins()
      showAssignDialog.value = false
    } else {
      alert('权限分配失败: ' + response.message)
    }
  } catch (error: any) {
    console.error('权限分配失败:', error)
    const errorMessage = error.response?.data?.message || error.message || '权限分配失败，请重试'
    alert(errorMessage)
  }
}

const modifyPermission = async () => {
  try {
    console.log('权限修改请求数据:', {
      adminId: modifyForm.adminId,
      newCategoryId: modifyForm.newCategoryId
    })
    
    if (!modifyForm.adminId || !modifyForm.newCategoryId) {
      alert('请选择管理员和新的分类权限')
      return
    }
    
    const response = await adminApi.updateAdminCategory(modifyForm.adminId!, modifyForm.newCategoryId!)
    console.log('权限修改响应:', response)
    
    if (response.success) {
      alert('权限修改成功！')
      resetModifyForm()
      loadCategoryAdmins()
    } else {
      alert('权限修改失败: ' + response.message)
    }
  } catch (error: any) {
    console.error('权限修改失败:', error)
    const errorMessage = error.response?.data?.message || error.message || '权限修改失败，请重试'
    alert(errorMessage)
  }
}

const revokePermission = async (adminId: number) => {
  try {
    const response = await adminApi.revokeAdminPermission(adminId)
    
    if (response.success) {
      alert('权限撤销成功！')
      loadCategoryAdmins()
    } else {
      alert('权限撤销失败: ' + response.message)
    }
  } catch (error: any) {
    console.error('权限撤销失败:', error)
    const errorMessage = error.response?.data?.message || error.message || '权限撤销失败，请重试'
    alert(errorMessage)
  }
}

const editAdmin = (admin: AdminInfo) => {
  modifyForm.adminId = admin.adminId
  modifyForm.newCategoryId = admin.assignedCategory || null
  activeTab.value = 'modify'
}

const revokeAdmin = (admin: AdminInfo) => {
  if (confirm(`确定要撤销 ${admin.username} 的管理员权限吗？`)) {
    revokePermission(admin.adminId)
  }
}

const confirmRevoke = (admin: AdminInfo) => {
  if (confirm(`确定要撤销 ${admin.username} 的管理员权限吗？此操作不可撤销。`)) {
    revokePermission(admin.adminId)
  }
}

const resetModifyForm = () => {
  modifyForm.adminId = null
  modifyForm.newCategoryId = null
}

const resetAssignForm = () => {
  assignForm.username = ''
  assignForm.email = ''
  assignForm.categoryId = null
}

// 获取当前管理员信息
const fetchCurrentAdmin = async () => {
  try {
    const response = await adminApi.getCurrentAdminInfo()
    if (response.success && response.data) {
      systemAdmin.name = response.data.username || '系统管理员'
      systemAdmin.id = response.data.adminId.toString()
    }
  } catch (error) {
    console.error('获取管理员信息失败:', error)
  }
}

onMounted(() => {
  loadCategoryData()
  loadModerators()
  loadCategoryAdmins()
  fetchCurrentAdmin()
  fetchAuditLogs()
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
  max-width: 1400px;
  margin: 0 auto;
  padding: 0 24px;
}

.header {
  padding: 20px 0;
  margin-bottom: 30px;
}

/* 左右分栏布局 */
.main-layout {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 30px;
  min-height: calc(100vh - 200px);
}

.left-panel {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.right-panel {
  display: flex;
  flex-direction: column;
}

/* 响应式布局 */
@media (max-width: 1200px) {
  .main-layout {
    grid-template-columns: 1fr;
    gap: 24px;
  }
  
  .container {
    max-width: 100%;
    padding: 0 16px;
  }
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
.filter-card, .permission-card, .table-card {
  background-color: white;
  border-radius: 12px;
  padding: 24px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
  margin-bottom: 0;
}

/* 左侧面板卡片样式调整 */
.left-panel .filter-card {
  flex-shrink: 0;
}

.left-panel .table-card {
  flex: 1;
  min-height: 400px;
}

/* 右侧面板样式调整 */
.right-panel .permission-card {
  height: 100%;
  min-height: 600px;
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
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  margin-bottom: 20px;
  align-items: end;
}

@media (max-width: 1200px) {
  .filter-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
  }
}

@media (max-width: 768px) {
  .filter-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 480px) {
  .filter-grid {
    grid-template-columns: 1fr;
  }
}

.filter-item {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 140px;
  flex: 1;
}

.filter-item label {
  font-size: 12px;
  color: #666;
  font-weight: 500;
  white-space: nowrap;
}

.filter-select, .filter-input {
  padding: 6px 10px;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 13px;
  background-color: white;
  transition: border-color 0.2s ease;
  min-width: 120px;
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

/* 权限管理样式 */
.permission-tabs {
  display: flex;
  gap: 8px;
  margin-bottom: 24px;
  border-bottom: 1px solid #eee;
}

.tab-btn {
  padding: 12px 24px;
  border: none;
  background: none;
  color: #666;
  font-size: 14px;
  cursor: pointer;
  border-bottom: 2px solid transparent;
  transition: all 0.2s ease;
}

.tab-btn.active {
  color: #FF85A2;
  border-bottom-color: #FF85A2;
}

.tab-btn:hover {
  color: #FF85A2;
}

.permission-panel {
  flex: 1;
  overflow-y: auto;
  min-height: 400px;
}

.panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid #eee;
}

.panel-header h3 {
  margin: 0;
  font-size: 18px;
  color: #333;
}

.admins-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 12px;
  max-height: 400px;
  overflow-y: auto;
}

.admin-card {
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 16px;
  background-color: #fafafa;
  transition: box-shadow 0.2s ease;
}

.admin-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.admin-info {
  margin-bottom: 12px;
}

.admin-name {
  font-size: 16px;
  font-weight: 600;
  color: #333;
  margin-bottom: 4px;
}

.admin-role {
  font-size: 14px;
  color: #666;
  margin-bottom: 4px;
}

.admin-category {
  font-size: 12px;
  color: #FF85A2;
  background-color: #FFE8F0;
  padding: 2px 8px;
  border-radius: 12px;
  display: inline-block;
}

.admin-actions {
  display: flex;
  gap: 8px;
}

.modification-form {
  max-width: 400px;
}

.form-group {
  margin-bottom: 16px;
}

.form-group label {
  display: block;
  font-size: 14px;
  color: #666;
  margin-bottom: 6px;
  font-weight: 500;
}

.form-input,
.form-select {
  width: 100%;
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 14px;
  background-color: white;
  transition: border-color 0.2s ease;
}

.form-input:focus,
.form-select:focus {
  outline: none;
  border-color: #FF85A2;
}

.form-actions {
  display: flex;
  gap: 12px;
  margin-top: 24px;
}

.revoke-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.revoke-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px;
  border: 1px solid #ddd;
  border-radius: 8px;
  background-color: #fafafa;
}

.admin-summary {
  flex: 1;
}

.admin-details {
  font-size: 12px;
  color: #666;
  margin-top: 4px;
}

.assign-form {
  margin-bottom: 24px;
}

.modal-actions {
  display: flex;
  gap: 12px;
  justify-content: flex-end;
  padding-top: 16px;
  border-top: 1px solid #eee;
}

.table-action-btn.danger {
  background-color: #f44336;
}

.table-action-btn.danger:hover {
  background-color: #d32f2f;
}

.action-btn.danger {
  background-color: #f44336;
  color: white;
}

.action-btn.danger:hover {
  background-color: #d32f2f;
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
  max-height: 500px;
  overflow-y: auto;
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

.operation-ban { background-color: #ffebee; color: #f44336; }
.operation-permission { background-color: #e3f2fd; color: #2196f3; }
.operation-report { background-color: #fff3e0; color: #ff9800; }
.operation-edit { background-color: #e8f5e8; color: #4caf50; }
.operation-delete { background-color: #ffebee; color: #f44336; }
.operation-batch { background-color: #f3e5f5; color: #9c27b0; }
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
