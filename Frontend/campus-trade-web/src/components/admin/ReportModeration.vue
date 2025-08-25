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
          <input v-model="search" placeholder="搜索举报..." class="filter-input">
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
            <tr v-for="item in filteredReports" :key="item.id" :class="{ loading: loading }">
              <td class="report-id">{{ item.id }}</td>
              <td>{{ item.reportDate }}</td>
              <td>{{ item.reporter }}</td>
              <td class="target-goods">{{ item.targetGoods }}</td>
              <td>
                <span class="type-tag">{{ item.type }}</span>
              </td>
              <td>
                <span class="priority-tag" :class="getPriorityClass(item.priority)">
                  {{ item.priority }}
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
              <div class="detail-value">{{ selectedReport.id }}</div>
            </div>
            <div class="detail-item">
              <label>举报时间</label>
              <div class="detail-value">{{ selectedReport.reportDate }}</div>
            </div>
            <div class="detail-item">
              <label>举报人</label>
              <div class="detail-value">{{ selectedReport.reporter }}</div>
            </div>
            <div class="detail-item">
              <label>被举报商品</label>
              <div class="detail-value">{{ selectedReport.targetGoods }}</div>
            </div>
            <div class="detail-item">
              <label>举报类型</label>
              <div class="detail-value">{{ selectedReport.type }}</div>
            </div>
            <div class="detail-item">
              <label>优先级</label>
              <div class="detail-value">
                <span class="priority-tag" :class="getPriorityClass(selectedReport.priority)">
                  {{ selectedReport.priority }}
                </span>
              </div>
            </div>
            <div class="detail-item full-width">
              <label>举报内容</label>
              <div class="detail-value content">{{ selectedReport.content }}</div>
            </div>
            <div v-if="selectedReport.evidence && selectedReport.evidence.length > 0" class="detail-item full-width">
              <label>举报证据</label>
              <div class="evidence-grid">
                <div v-for="(evidence, index) in selectedReport.evidence" :key="index" class="evidence-item">
                  <img :src="evidence" :alt="`证据 ${index + 1}`">
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
              <div><strong>举报人：</strong>{{ reportToModerate.reporter }}</div>
              <div><strong>被举报商品：</strong>{{ reportToModerate.targetGoods }}</div>
              <div><strong>举报类型：</strong>{{ reportToModerate.type }}</div>
              <div class="full-width"><strong>举报内容：</strong>{{ reportToModerate.content }}</div>
            </div>
          </div>
          
          <form @submit.prevent="submitModeration" class="moderation-form">
            <div class="form-item">
              <label>审核决定 *</label>
              <div class="radio-group">
                <label class="radio-item">
                  <input type="radio" v-model="moderationForm.decision" value="approved">
                  <span class="radio-text">举报成立，处理被举报商品</span>
                </label>
                <label class="radio-item">
                  <input type="radio" v-model="moderationForm.decision" value="rejected">
                  <span class="radio-text">举报不成立，驳回举报</span>
                </label>
                <label class="radio-item">
                  <input type="radio" v-model="moderationForm.decision" value="pending">
                  <span class="radio-text">需要进一步调查，挂起处理</span>
                </label>
              </div>
            </div>
            
            <div class="form-item">
              <label>审核意见 *</label>
              <textarea 
                v-model="moderationForm.comment" 
                class="form-textarea"
                rows="4"
                placeholder="请输入审核意见..."
                maxlength="500"
              ></textarea>
              <div class="char-count">{{ moderationForm.comment.length }}/500</div>
            </div>
            
            <div v-if="moderationForm.decision === 'approved'" class="form-item">
              <label>对被举报商品的处理方式 *</label>
              <select v-model="moderationForm.action" class="form-select">
                <option value="">请选择处理方式</option>
                <option v-for="action in actionOptions" :key="action" :value="action">
                  {{ action }}
                </option>
              </select>
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
import { ref, reactive, computed } from 'vue'

// 定义接口
interface Report {
  id: string
  reportDate: string
  reporter: string
  targetGoods: string
  type: string
  priority: string
  status: string
  content: string
  evidence?: string[]
}

interface ModerationHistory {
  timestamp: string
  action: string
  moderator: string
  comment: string
}

// 响应式数据
const loading = ref(false)
const moderating = ref(false)
const search = ref('')
const showDetailDialog = ref(false)
const showModerationDialog = ref(false)
const showHistoryDialog = ref(false)
const selectedReport = ref<Report | null>(null)
const reportToModerate = ref<Report | null>(null)
const moderationHistory = ref<ModerationHistory[]>([])

const filters = reactive({
  status: null as string | null,
  type: null as string | null,
  dateRange: null as string | null
})

const moderationForm = reactive({
  decision: '',
  comment: '',
  action: ''
})

// 选项数据
const statusOptions = [
  '待审核', '已通过', '已驳回', '挂起中', '已关闭'
]

const typeOptions = [
  '价格欺诈', '商品描述不符', '虚假商品', '违规内容', '恶意交易', '其他'
]

const actionOptions = [
  '下架商品', '警告卖家', '限制交易', '封禁账户'
]

// 模拟举报数据
const reportsList = ref<Report[]>([
  {
    id: 'R001',
    reportDate: '2025-01-20 14:30:00',
    reporter: '用户A',
    targetGoods: 'iPhone 13 Pro',
    type: '价格欺诈',
    priority: '高',
    status: '待审核',
    content: '该商品标价明显低于市场价，怀疑是诈骗商品',
    evidence: ['https://via.placeholder.com/200', 'https://via.placeholder.com/200']
  },
  {
    id: 'R002',
    reportDate: '2025-01-19 16:45:00',
    reporter: '用户B',
    targetGoods: '高等数学教材',
    type: '商品描述不符',
    priority: '中',
    status: '已通过',
    content: '收到的教材版本与描述不符，且有明显破损',
    evidence: ['https://via.placeholder.com/200']
  }
])

// 计算属性
const filteredReports = computed(() => {
  return reportsList.value.filter(report => {
    if (search.value && !report.reporter.includes(search.value) && !report.targetGoods.includes(search.value)) {
      return false
    }
    if (filters.status && report.status !== filters.status) return false
    if (filters.type && report.type !== filters.type) return false
    return true
  })
})

const isModerationFormValid = computed(() => {
  const hasDecision = !!moderationForm.decision
  const hasComment = !!moderationForm.comment && moderationForm.comment.length <= 500
  const hasAction = moderationForm.decision !== 'approved' || !!moderationForm.action
  
  return hasDecision && hasComment && hasAction
})

// 方法
const getPriorityClass = (priority: string) => {
  const classes: Record<string, string> = {
    '高': 'priority-high',
    '中': 'priority-medium',
    '低': 'priority-low'
  }
  return classes[priority] || 'priority-default'
}

const getStatusClass = (status: string) => {
  const classes: Record<string, string> = {
    '待审核': 'status-pending',
    '已通过': 'status-approved',
    '已驳回': 'status-rejected',
    '挂起中': 'status-suspended',
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

const viewReportDetails = (item: Report) => {
  selectedReport.value = item
  showDetailDialog.value = true
}

const moderateReport = (item: Report) => {
  reportToModerate.value = item
  Object.assign(moderationForm, {
    decision: '',
    comment: '',
    action: ''
  })
  showModerationDialog.value = true
}

const viewHistory = (item: Report) => {
  moderationHistory.value = [
    {
      timestamp: '2025-01-20 14:30:00',
      action: '提交举报',
      moderator: '用户A',
      comment: '举报商品价格异常'
    },
    {
      timestamp: '2025-01-20 15:00:00',
      action: '开始审核',
      moderator: '管理员张三',
      comment: '开始处理该举报，正在调查中'
    }
  ]
  showHistoryDialog.value = true
}

const submitModeration = async () => {
  moderating.value = true
  try {
    await new Promise(resolve => setTimeout(resolve, 1500))
    
    if (reportToModerate.value) {
      const report = reportsList.value.find(r => r.id === reportToModerate.value!.id)
      if (report) {
        switch (moderationForm.decision) {
          case 'approved':
            report.status = '已通过'
            break
          case 'rejected':
            report.status = '已驳回'
            break
          case 'pending':
            report.status = '挂起中'
            break
        }
      }
    }
    
    closeModerationDialog()
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
    decision: '',
    comment: '',
    action: ''
  })
}
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
}
</style>
