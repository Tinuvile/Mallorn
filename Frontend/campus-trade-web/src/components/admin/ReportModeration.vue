<template>
  <div class="report-moderation">
    <!-- 筛选操作区 -->
    <div class="filter-bar">
      <div class="filter-grid">
        <div class="filter-item">
          <label>举报状态</label>
          <select v-model="filters.status" class="filter-select">
            <option value="">全部状态</option>
            <option v-for="status in statusOptions" :key="status" :value="status">{{ status }}</option>
          </select>
        </div>
        <div class="filter-item">
          <label>举报类型</label>
          <select v-model="filters.type" class="filter-select">
            <option value="">全部类型</option>
            <option v-for="type in typeOptions" :key="type" :value="type">{{ type }}</option>
          </select>
        </div>
        <div class="filter-item">
          <label>举报日期</label>
          <input v-model="filters.dateRange" type="date" class="filter-input">
        </div>
        <div class="filter-item">
          <label>搜索</label>
          <input v-model="search" placeholder="搜索举报..." class="filter-input" @keyup.enter="applyFilters">
        </div>
        <div class="filter-actions">
          <button class="filter-btn" @click="applyFilters">筛选</button>
          <button class="filter-btn secondary" @click="resetFilters">重置</button>
        </div>
      </div>
    </div>

    <!-- 举报列表表格 -->
    <div class="table-card">
      <div class="card-header">
        <div class="card-title">
          举报审核列表
        </div>
      </div>
      
      <div class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>举报ID</th>
              <th>举报时间</th>
              <th>举报人</th>
              <th>被举报商品</th>
              <th>举报类型</th>
              <th>优先级</th>
              <th>状态</th>
              <th width="180">操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in filteredReports" :key="item.reportId" :class="{ loading: loading }">
              <td class="report-id">{{ item.reportId }}</td>
              <td>{{ new Date(item.createTime).toLocaleString() }}</td>
              <td>举报用户</td>
              <td class="target-goods">订单 #{{ item.orderId }}</td>
              <td>
                <span class="type-tag">{{ item.type }}</span>
              </td>
              <td>
                <span class="priority-tag" :class="getPriorityClass(item.priority)">
                  {{ getPriorityText(item.priority) }}
                </span>
              </td>
              <td>
                <span class="status-tag" :class="getStatusClass(item.status)">
                  {{ item.status }}
                </span>
              </td>
              <td>
                <div class="table-actions">
                  <button class="table-action-btn view" @click="viewReportDetails(item)" title="查看详情">
                    查看
                  </button>
                  <button 
                    class="table-action-btn moderate" 
                    @click="moderateReport(item)"
                    :disabled="item.status !== '待审核'"
                    title="审核举报"
                  >
                    审核
                  </button>
                  <button class="table-action-btn history" @click="viewHistory(item)" title="查看历史">
                    历史
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      
      <!-- 分页组件 -->
      <div class="pagination-container" v-if="totalPages > 1">
        <div class="pagination-info">
          共 {{ totalCount }} 条记录，第 {{ pageIndex + 1 }} / {{ totalPages }} 页
        </div>
        <div class="pagination-buttons">
          <button 
            class="pagination-btn" 
            :disabled="pageIndex === 0"
            @click="changePage(0)"
          >
            首页
          </button>
          <button 
            class="pagination-btn" 
            :disabled="pageIndex === 0"
            @click="changePage(pageIndex - 1)"
          >
            上一页
          </button>
          <button 
            class="pagination-btn" 
            :disabled="pageIndex >= totalPages - 1"
            @click="changePage(pageIndex + 1)"
          >
            下一页
          </button>
          <button 
            class="pagination-btn" 
            :disabled="pageIndex >= totalPages - 1"
            @click="changePage(totalPages - 1)"
          >
            末页
          </button>
        </div>
      </div>
    </div>

    <!-- 举报详情模态框 -->
    <div v-if="showDetailDialog" class="modal-overlay" @click="showDetailDialog = false">
      <div class="modal-content large" @click.stop>
        <div class="modal-header">
          <h3>举报详情</h3>
          <button class="modal-close" @click="showDetailDialog = false">✕</button>
        </div>
        <div class="modal-body" v-if="selectedReport">
          <div class="detail-grid">
            <div class="detail-item">
              <label>举报ID</label>
              <div class="detail-value">{{ selectedReport.reportId }}</div>
            </div>
            <div class="detail-item">
              <label>举报时间</label>
              <div class="detail-value">{{ new Date(selectedReport.createTime).toLocaleString() }}</div>
            </div>
            <div class="detail-item">
              <label>举报人</label>
              <div class="detail-value">{{ selectedReport.reporter?.username || '未知用户' }}</div>
            </div>
            <div class="detail-item">
              <label>被举报订单</label>
              <div class="detail-value">订单 #{{ selectedReport.orderId }}</div>
            </div>
            <div class="detail-item">
              <label>举报类型</label>
              <div class="detail-value">{{ selectedReport.type }}</div>
            </div>
            <div class="detail-item">
              <label>优先级</label>
              <div class="detail-value">
                <span class="priority-tag" :class="getPriorityClass(selectedReport.priority)">
                  {{ getPriorityText(selectedReport.priority) }}
                </span>
              </div>
            </div>
            <div class="detail-item full-width">
              <label>举报内容</label>
              <div class="detail-value content">{{ selectedReport.description }}</div>
            </div>
            <div v-if="selectedReport.evidences && selectedReport.evidences.length > 0" class="detail-item full-width">
              <label>举报证据</label>
              <div class="evidence-grid">
                <div v-for="(evidence, index) in selectedReport.evidences" :key="index" class="evidence-item">
                  <img :src="evidence.fileUrl" :alt="`证据 ${index + 1}`">
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 审核模态框 -->
    <div v-if="showModerationDialog" class="modal-overlay" @click="closeModerationDialog">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>举报审核</h3>
          <button class="modal-close" @click="closeModerationDialog">✕</button>
        </div>
        <div class="modal-body" v-if="reportToModerate">
          <div class="report-summary">
            <h4>举报信息</h4>
            <div class="summary-grid">
              <div><strong>举报人：</strong>{{ reportToModerate.reporter?.username || '未知用户' }}</div>
              <div><strong>被举报订单：</strong>订单 #{{ reportToModerate.orderId }}</div>
              <div><strong>举报类型：</strong>{{ reportToModerate.type }}</div>
              <div class="full-width"><strong>举报内容：</strong>{{ reportToModerate.description }}</div>
            </div>
          </div>
          
          <form @submit.prevent="submitModeration" class="moderation-form">
            <div class="form-item">
              <label>审核决定 *</label>
              <div class="radio-group">
                <label class="radio-item" v-for="option in handleResultOptions" :key="option">
                  <input type="radio" v-model="moderationForm.handleResult" :value="option">
                  <span class="radio-text">{{ option }}</span>
                </label>
              </div>
            </div>
            
            <div class="form-item">
              <label>审核意见 *</label>
              <textarea 
                v-model="moderationForm.handleNote" 
                class="form-textarea"
                rows="4"
                placeholder="请输入审核意见..."
                maxlength="500"
              ></textarea>
              <div class="char-count">{{ moderationForm.handleNote.length }}/500</div>
            </div>
            
            <div class="form-item">
              <label class="checkbox-label">
                <input type="checkbox" v-model="moderationForm.applyPenalty">
                <span>对被举报方进行处罚</span>
              </label>
            </div>
            
            <div v-if="moderationForm.applyPenalty" class="penalty-section">
              <div class="form-item">
                <label>处罚类型 *</label>
                <select v-model="moderationForm.penaltyType" class="form-select">
                  <option value="">请选择处罚类型</option>
                  <option v-for="type in penaltyTypeOptions" :key="type" :value="type">
                    {{ type }}
                  </option>
                </select>
              </div>
              
              <div v-if="moderationForm.penaltyType === '禁言' || moderationForm.penaltyType === '封号'" class="form-item">
                <label>处罚时长（天数） *</label>
                <input 
                  v-model.number="moderationForm.penaltyDuration" 
                  type="number" 
                  class="form-input"
                  min="1"
                  max="365"
                  placeholder="1-365天"
                >
              </div>
            </div>
            
            <div class="form-actions">
              <button type="button" class="action-btn secondary" @click="closeModerationDialog">
                取消
              </button>
              <button type="submit" class="action-btn primary" :disabled="!isModerationFormValid || moderating">
                {{ moderating ? '提交中...' : '提交审核' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- 审核历史模态框 -->
    <div v-if="showHistoryDialog" class="modal-overlay" @click="showHistoryDialog = false">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>审核历史</h3>
          <button class="modal-close" @click="showHistoryDialog = false">✕</button>
        </div>
        <div class="modal-body">
          <div class="timeline">
            <div v-for="(history, index) in moderationHistory" :key="index" class="timeline-item">
              <div class="timeline-marker" :class="getHistoryClass(history.action)"></div>
              <div class="timeline-content">
                <div class="timeline-header">
                  <h4>{{ history.action }}</h4>
                  <span class="timeline-time">{{ history.timestamp }}</span>
                </div>
                <div class="timeline-body">
                  <p><strong>操作人：</strong>{{ history.moderator }}</p>
                  <p><strong>操作说明：</strong>{{ history.comment }}</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { adminApi, reportApi, type ReportItem, type ReportDetail, type HandleReportRequest, type AdminReportsResponse } from '@/services/api'

// 响应式数据
const loading = ref(false)
const moderating = ref(false)
const search = ref('')
const showDetailDialog = ref(false)
const showModerationDialog = ref(false)
const showHistoryDialog = ref(false)
const selectedReport = ref<ReportDetail | null>(null)
const reportToModerate = ref<ReportDetail | null>(null)
const moderationHistory = ref<Array<{
  timestamp: string
  action: string
  moderator: string
  comment: string
}>>([])

// 分页信息
const pageIndex = ref(0)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = ref(0)

const filters = reactive({
  status: '' as string,
  type: '' as string,
  dateRange: '' as string
})

const moderationForm = reactive({
  handleResult: '',
  handleNote: '',
  applyPenalty: false,
  penaltyType: '',
  penaltyDuration: undefined as number | undefined
})

// 选项数据
const statusOptions = [
  '待审核', '已通过', '已驳回', '需要更多信息'
]

const typeOptions = [
  '价格欺诈', '商品描述不符', '虚假商品', '违规内容', '恶意交易', '其他'
]

const handleResultOptions = [
  '通过', '驳回', '需要更多信息'
]

const penaltyTypeOptions = [
  '警告', '禁言', '封号'
]

// 举报数据
const reportsList = ref<ReportItem[]>([])

// 计算属性
const filteredReports = computed(() => {
  let filtered = reportsList.value
  
  if (search.value) {
    filtered = filtered.filter(report => 
      report.description?.toLowerCase().includes(search.value.toLowerCase()) ||
      report.type.includes(search.value)
    )
  }
  
  if (filters.status) {
    filtered = filtered.filter(report => report.status === filters.status)
  }
  
  if (filters.type) {
    filtered = filtered.filter(report => report.type === filters.type)
  }
  
  return filtered
})

const isModerationFormValid = computed(() => {
  const hasResult = !!moderationForm.handleResult
  const hasNote = !!moderationForm.handleNote && moderationForm.handleNote.length <= 500
  const hasPenaltyInfo = !moderationForm.applyPenalty || 
    (!!moderationForm.penaltyType && 
     (!moderationForm.penaltyDuration || (moderationForm.penaltyDuration >= 1 && moderationForm.penaltyDuration <= 365)))
  
  return hasResult && hasNote && hasPenaltyInfo
})

// API 调用方法
const fetchReports = async () => {
  try {
    loading.value = true
    
    const response = await adminApi.getAdminReports(pageIndex.value, pageSize.value, filters.status || undefined)
    
    if (response.success && response.data) {
      reportsList.value = response.data.reports
      totalCount.value = response.data.totalCount
      totalPages.value = response.data.totalPages
    }
  } catch (error) {
    console.error('获取举报列表失败:', error)
  } finally {
    loading.value = false
  }
}

const fetchReportDetail = async (reportId: number): Promise<ReportDetail | null> => {
  try {
    const response = await reportApi.getReportDetail(reportId)
    
    if (response.success && response.data) {
      return response.data
    }
  } catch (error) {
    console.error('获取举报详情失败:', error)
  }
  
  return null
}

// 方法
const getPriorityClass = (priority?: number) => {
  const classes: Record<number, string> = {
    3: 'priority-high',
    2: 'priority-medium', 
    1: 'priority-low'
  }
  return classes[priority || 1] || 'priority-default'
}

const getPriorityText = (priority?: number) => {
  const priorityMap: Record<number, string> = {
    3: '高',
    2: '中',
    1: '低'
  }
  return priorityMap[priority || 1] || '低'
}

const getStatusClass = (status: string) => {
  const classes: Record<string, string> = {
    '待审核': 'status-pending',
    '已通过': 'status-approved',
    '已驳回': 'status-rejected',
    '需要更多信息': 'status-suspended',
    '已关闭': 'status-closed'
  }
  return classes[status] || 'status-default'
}

const getHistoryClass = (action: string) => {
  const classes: Record<string, string> = {
    '提交举报': 'history-submit',
    '开始审核': 'history-start',
    '审核通过': 'history-approved',
    '审核驳回': 'history-rejected',
    '挂起处理': 'history-suspended'
  }
  return classes[action] || 'history-default'
}

const viewReportDetails = async (item: ReportItem) => {
  const detail = await fetchReportDetail(item.reportId)
  if (detail) {
    selectedReport.value = detail
    showDetailDialog.value = true
  }
}

const moderateReport = async (item: ReportItem) => {
  const detail = await fetchReportDetail(item.reportId)
  if (detail) {
    reportToModerate.value = detail
    Object.assign(moderationForm, {
      handleResult: '',
      handleNote: '',
      applyPenalty: false,
      penaltyType: '',
      penaltyDuration: undefined
    })
    showModerationDialog.value = true
  }
}

const viewHistory = (item: ReportItem) => {
  // 模拟审核历史数据
  moderationHistory.value = [
    {
      timestamp: new Date(item.createTime).toLocaleString(),
      action: '提交举报',
      moderator: '举报用户',
      comment: '举报提交'
    },
    {
      timestamp: new Date().toLocaleString(),
      action: '开始审核',
      moderator: '管理员',
      comment: '开始处理该举报，正在调查中'
    }
  ]
  showHistoryDialog.value = true
}

const submitModeration = async () => {
  if (!reportToModerate.value || !isModerationFormValid.value) return
  
  moderating.value = true
  try {
    const request: HandleReportRequest = {
      handleResult: moderationForm.handleResult,
      handleNote: moderationForm.handleNote,
      applyPenalty: moderationForm.applyPenalty,
      penaltyType: moderationForm.applyPenalty ? moderationForm.penaltyType : undefined,
      penaltyDuration: moderationForm.applyPenalty ? moderationForm.penaltyDuration : undefined
    }
    
    const response = await adminApi.handleReport(reportToModerate.value.reportId, request)
    
    if (response.success) {
      // 处理成功后刷新列表
      await fetchReports()
      closeModerationDialog()
    }
  } catch (error) {
    console.error('审核提交失败:', error)
  } finally {
    moderating.value = false
  }
}

const closeModerationDialog = () => {
  showModerationDialog.value = false
  reportToModerate.value = null
  Object.assign(moderationForm, {
    handleResult: '',
    handleNote: '',
    applyPenalty: false,
    penaltyType: '',
    penaltyDuration: undefined
  })
}

// 分页方法
const changePage = (newPageIndex: number) => {
  pageIndex.value = newPageIndex
  fetchReports()
}

// 筛选方法
const applyFilters = () => {
  pageIndex.value = 0
  fetchReports()
}

// 重置筛选
const resetFilters = () => {
  Object.assign(filters, {
    status: '',
    type: '',
    dateRange: ''
  })
  search.value = ''
  pageIndex.value = 0
  fetchReports()
}

// 组件挂载时获取数据
onMounted(() => {
  fetchReports()
})
</script>

<style scoped>
.report-moderation {
  padding: 0;
}

/* 筛选栏样式 */
.filter-bar {
  background-color: white;
  border-radius: 12px;
  padding: 20px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
  margin-bottom: 24px;
}

.filter-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px;
  align-items: end;
}

.filter-actions {
  display: flex;
  gap: 8px;
}

.filter-btn {
  padding: 8px 16px;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 14px;
  transition: background-color 0.2s ease;
}

.filter-btn:not(.secondary) {
  background-color: #FF85A2;
  color: white;
}

.filter-btn:not(.secondary):hover {
  background-color: #ff6b90;
}

.filter-btn.secondary {
  background-color: #f5f5f5;
  color: #666;
}

.filter-btn.secondary:hover {
  background-color: #e0e0e0;
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

/* 表格卡片样式 */
.table-card {
  background-color: white;
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
  overflow: hidden;
}

.card-header {
  padding: 20px 24px;
  border-bottom: 1px solid #eee;
}

.card-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 18px;
  font-weight: bold;
  color: #333;
}

.title-icon {
  font-size: 20px;
}

.table-container {
  overflow-x: auto;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
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

.report-id {
  font-family: monospace;
  font-weight: 600;
  color: #FF85A2;
}

.target-goods {
  font-weight: 500;
}

/* 标签样式 */
.type-tag, .priority-tag, .status-tag {
  padding: 4px 8px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 500;
}

.type-tag {
  background-color: #e3f2fd;
  color: #2196f3;
}

.priority-high { background-color: #ffebee; color: #f44336; }
.priority-medium { background-color: #fff3e0; color: #ff9800; }
.priority-low { background-color: #e8f5e8; color: #4caf50; }

.status-pending { background-color: #fff3e0; color: #ff9800; }
.status-approved { background-color: #e8f5e8; color: #4caf50; }
.status-rejected { background-color: #ffebee; color: #f44336; }
.status-suspended { background-color: #e3f2fd; color: #2196f3; }
.status-closed { background-color: #f5f5f5; color: #666; }

/* 表格操作按钮 */
.table-actions {
  display: flex;
  gap: 8px;
}

.table-action-btn {
  width: 32px;
  height: 32px;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
}

.table-action-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.table-action-btn.view {
  background-color: #e3f2fd;
  color: #2196f3;
}

.table-action-btn.moderate {
  background-color: #e8f5e8;
  color: #4caf50;
}

.table-action-btn.history {
  background-color: #f3e5f5;
  color: #9c27b0;
}

.table-action-btn:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
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
  max-height: 90vh;
  overflow-y: auto;
}

.modal-content.large {
  max-width: 800px;
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

/* 详情网格样式 */
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

.detail-value.content {
  line-height: 1.5;
}

/* 证据网格 */
.evidence-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
  gap: 12px;
  margin-top: 8px;
}

.evidence-item {
  aspect-ratio: 1;
  border-radius: 8px;
  overflow: hidden;
}

.evidence-item img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

/* 举报摘要样式 */
.report-summary {
  margin-bottom: 24px;
  padding: 16px;
  background-color: #f9f9f9;
  border-radius: 8px;
}

.report-summary h4 {
  margin: 0 0 12px 0;
  font-size: 16px;
  color: #333;
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 8px;
  font-size: 14px;
}

.summary-grid .full-width {
  grid-column: 1 / -1;
  margin-top: 8px;
}

/* 审核表单样式 */
.moderation-form {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.form-item {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.form-item label {
  font-size: 14px;
  color: #555;
  font-weight: 500;
}

.radio-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.radio-item {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  padding: 8px;
  border-radius: 6px;
  transition: background-color 0.2s ease;
}

.radio-item:hover {
  background-color: #f9f9f9;
}

.radio-item input[type="radio"] {
  margin: 0;
}

.radio-text {
  font-size: 14px;
  color: #333;
}

.form-textarea, .form-select {
  padding: 10px 12px;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 14px;
  transition: border-color 0.2s ease;
}

.form-textarea:focus, .form-select:focus {
  outline: none;
  border-color: #FF85A2;
}

.char-count {
  font-size: 12px;
  color: #666;
  text-align: right;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  font-size: 14px;
}

.checkbox-label input[type="checkbox"] {
  margin: 0;
}

.penalty-section {
  margin-top: 16px;
  padding: 16px;
  background-color: #f9f9f9;
  border-radius: 8px;
  border-left: 4px solid #FF85A2;
}

/* 分页样式 */
.pagination-container {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 24px;
  border-top: 1px solid #eee;
  background-color: #fafafa;
}

.pagination-info {
  font-size: 14px;
  color: #666;
}

.pagination-buttons {
  display: flex;
  gap: 8px;
}

.pagination-btn {
  padding: 8px 12px;
  border: 1px solid #ddd;
  background-color: white;
  color: #666;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  transition: all 0.2s ease;
}

.pagination-btn:hover:not(:disabled) {
  background-color: #FF85A2;
  color: white;
  border-color: #FF85A2;
}

.pagination-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.form-actions {
  display: flex;
  gap: 12px;
  justify-content: flex-end;
  margin-top: 20px;
}

.action-btn {
  padding: 10px 20px;
  border: none;
  border-radius: 8px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.action-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.action-btn.primary {
  background-color: #FF85A2;
  color: white;
}

.action-btn.primary:hover:not(:disabled) {
  background-color: #ff6b90;
}

.action-btn.secondary {
  background-color: #f5f5f5;
  color: #666;
}

.action-btn.secondary:hover {
  background-color: #e0e0e0;
}

/* 时间线样式 */
.timeline {
  position: relative;
  padding-left: 30px;
}

.timeline::before {
  content: '';
  position: absolute;
  left: 12px;
  top: 0;
  bottom: 0;
  width: 2px;
  background-color: #e0e0e0;
}

.timeline-item {
  position: relative;
  margin-bottom: 24px;
}

.timeline-marker {
  position: absolute;
  left: -18px;
  top: 8px;
  width: 12px;
  height: 12px;
  border-radius: 50%;
  background-color: #FF85A2;
  border: 2px solid white;
  box-shadow: 0 0 0 2px #e0e0e0;
}

.timeline-marker.history-submit { background-color: #2196f3; }
.timeline-marker.history-start { background-color: #ff9800; }
.timeline-marker.history-approved { background-color: #4caf50; }
.timeline-marker.history-rejected { background-color: #f44336; }
.timeline-marker.history-suspended { background-color: #9c27b0; }

.timeline-content {
  background-color: white;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  padding: 16px;
}

.timeline-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}

.timeline-header h4 {
  margin: 0;
  font-size: 16px;
  color: #333;
}

.timeline-time {
  font-size: 12px;
  color: #666;
}

.timeline-body p {
  margin: 4px 0;
  font-size: 14px;
  color: #555;
}

/* 响应式设计 */
@media (max-width: 768px) {
  .detail-grid {
    grid-template-columns: 1fr;
  }
  
  .summary-grid {
    grid-template-columns: 1fr;
  }
  
  .evidence-grid {
    grid-template-columns: repeat(auto-fill, minmax(100px, 1fr));
  }
  
  .filter-grid {
    grid-template-columns: 1fr;
  }
  
  .pagination-container {
    flex-direction: column;
    gap: 12px;
  }
  
  .pagination-buttons {
    justify-content: center;
  }
}
</style>
