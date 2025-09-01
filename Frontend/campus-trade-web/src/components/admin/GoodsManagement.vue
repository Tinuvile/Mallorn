<template>
  <div class="goods-management">
    <!-- 操作按钮区 -->
    <div class="actions-bar">
      <div class="left-actions">
        <button class="action-btn primary" @click="showAddDialog = true">
          <span>➕</span>
          新增商品
        </button>
        <button 
          class="action-btn danger" 
          @click="deleteSelectedGoods" 
          :disabled="selectedGoods.length === 0"
        >
          批量删除 ({{ selectedGoods.length }})
        </button>
      </div>
      <div class="search-box">
        <input 
          v-model="search" 
          placeholder="搜索商品..." 
          class="search-input"
        >
      </div>
    </div>

    <!-- 商品列表表格 -->
    <div class="table-card">
      <div class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th width="50">
                <input 
                  type="checkbox" 
                  @change="toggleSelectAll"
                  :checked="selectedGoods.length === filteredGoods.length && filteredGoods.length > 0"
                >
              </th>
              <th width="80">图片</th>
              <th>商品名称</th>
              <th>分类</th>
              <th>价格</th>
              <th>成色</th>
              <th>状态</th>
              <th>发布时间</th>
              <th width="150">操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in filteredGoods" :key="item.id" :class="{ loading: loading }">
              <td>
                <input 
                  type="checkbox" 
                  :value="item.id"
                  v-model="selectedGoods"
                >
              </td>
              <td>
                <div class="product-image">
                  <img :src="item.image" :alt="item.name" />
                </div>
              </td>
              <td class="product-name">{{ item.name }}</td>
              <td>{{ item.category }}</td>
              <td class="price">¥{{ item.price }}</td>
              <td>{{ item.condition }}</td>
              <td>
                <span class="status-tag" :class="getStatusClass(item.status)">
                  {{ item.status }}
                </span>
              </td>
              <td>{{ item.createdAt }}</td>
              <td>
                <div class="table-actions">
                  <button class="table-action-btn view" @click="viewDetails(item)" title="查看详情">
                    查看
                  </button>
                  <button class="table-action-btn edit" @click="editGoods(item)" title="编辑商品">
                    编辑
                  </button>
                  <button class="table-action-btn delete" @click="deleteGoods(item)" title="删除商品">
                    删除
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- 新增/编辑商品模态框 -->
    <div v-if="showAddDialog" class="modal-overlay" @click="closeAddDialog">
      <div class="modal-content large" @click.stop>
        <div class="modal-header">
          <h3>{{ editingGoods ? '编辑商品' : '新增商品' }}</h3>
          <button class="modal-close" @click="closeAddDialog">✕</button>
        </div>
        <div class="modal-body">
          <form @submit.prevent="saveGoods" class="goods-form">
            <div class="form-grid">
              <div class="form-item">
                <label>商品名称 *</label>
                <input 
                  v-model="goodsForm.name" 
                  type="text" 
                  class="form-input"
                  :class="{ error: nameError }"
                  @blur="validateName"
                >
                <span v-if="nameError" class="error-message">{{ nameError }}</span>
              </div>
              <div class="form-item">
                <label>价格 *</label>
                <div class="price-input">
                  <span class="currency">¥</span>
                  <input 
                    v-model="goodsForm.price" 
                    type="number" 
                    class="form-input"
                    :class="{ error: priceError }"
                    @blur="validatePrice"
                  >
                </div>
                <span v-if="priceError" class="error-message">{{ priceError }}</span>
              </div>
              <div class="form-item">
                <label>商品分类 *</label>
                <select 
                  v-model="goodsForm.category" 
                  class="form-select"
                  :class="{ error: categoryError }"
                  @blur="validateCategory"
                >
                  <option value="">请选择分类</option>
                  <option v-for="category in categories" :key="category" :value="category">
                    {{ category }}
                  </option>
                </select>
                <span v-if="categoryError" class="error-message">{{ categoryError }}</span>
              </div>
              <div class="form-item">
                <label>商品成色 *</label>
                <select 
                  v-model="goodsForm.condition" 
                  class="form-select"
                  :class="{ error: conditionError }"
                  @blur="validateCondition"
                >
                  <option value="">请选择成色</option>
                  <option v-for="condition in conditions" :key="condition" :value="condition">
                    {{ condition }}
                  </option>
                </select>
                <span v-if="conditionError" class="error-message">{{ conditionError }}</span>
              </div>
            </div>
            <div class="form-item full-width">
              <label>商品描述 *</label>
              <textarea 
                v-model="goodsForm.description" 
                class="form-textarea"
                :class="{ error: descriptionError }"
                rows="4"
                @blur="validateDescription"
              ></textarea>
              <span v-if="descriptionError" class="error-message">{{ descriptionError }}</span>
            </div>
            <div class="form-item full-width">
              <label>商品图片</label>
              <div class="file-upload">
                <input 
                  type="file" 
                  multiple 
                  accept="image/*"
                  @change="handleFileUpload"
                  class="file-input"
                >
                <div class="upload-hint">支持多张图片上传，建议尺寸 800x800px</div>
              </div>
            </div>
            <div class="form-actions">
              <button type="button" class="action-btn secondary" @click="closeAddDialog">
                取消
              </button>
              <button type="submit" class="action-btn primary" :disabled="!isFormValid || saving">
                {{ saving ? '保存中...' : '保存' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- 商品详情模态框 -->
    <div v-if="showDetailDialog" class="modal-overlay" @click="showDetailDialog = false">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>商品详情</h3>
          <button class="modal-close" @click="showDetailDialog = false">✕</button>
        </div>
        <div class="modal-body" v-if="selectedGoodsDetail">
          <div class="goods-detail">
            <div class="detail-image">
              <img :src="selectedGoodsDetail.image" :alt="selectedGoodsDetail.name">
            </div>
            <div class="detail-info">
              <div class="detail-grid">
                <div class="detail-item">
                  <label>商品名称</label>
                  <div>{{ selectedGoodsDetail.name }}</div>
                </div>
                <div class="detail-item">
                  <label>价格</label>
                  <div class="price">¥{{ selectedGoodsDetail.price }}</div>
                </div>
                <div class="detail-item">
                  <label>分类</label>
                  <div>{{ selectedGoodsDetail.category }}</div>
                </div>
                <div class="detail-item">
                  <label>成色</label>
                  <div>{{ selectedGoodsDetail.condition }}</div>
                </div>
                <div class="detail-item">
                  <label>状态</label>
                  <div>
                    <span class="status-tag" :class="getStatusClass(selectedGoodsDetail.status)">
                      {{ selectedGoodsDetail.status }}
                    </span>
                  </div>
                </div>
                <div class="detail-item">
                  <label>发布时间</label>
                  <div>{{ selectedGoodsDetail.createdAt }}</div>
                </div>
              </div>
              <div class="detail-description">
                <label>商品描述</label>
                <p>{{ selectedGoodsDetail.description }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 删除确认模态框 -->
    <div v-if="showDeleteDialog" class="modal-overlay" @click="showDeleteDialog = false">
      <div class="modal-content small" @click.stop>
        <div class="modal-header">
          <h3>确认删除</h3>
          <button class="modal-close" @click="showDeleteDialog = false">✕</button>
        </div>
        <div class="modal-body">
          <p class="delete-message">{{ deleteMessage }}</p>
          <div class="form-actions">
            <button class="action-btn secondary" @click="showDeleteDialog = false">
              取消
            </button>
            <button class="action-btn danger" @click="confirmDelete" :disabled="deleting">
              {{ deleting ? '删除中...' : '确认删除' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed } from 'vue'

// 定义接口
interface Goods {
  id: number
  name: string
  category: string
  price: number
  condition: string
  status: string
  image: string
  description: string
  createdAt: string
}

// 响应式数据
const loading = ref(false)
const saving = ref(false)
const deleting = ref(false)
const search = ref('')
const selectedGoods = ref<number[]>([])
const showAddDialog = ref(false)
const showDetailDialog = ref(false)
const showDeleteDialog = ref(false)
const editingGoods = ref(false)
const selectedGoodsDetail = ref<Goods | null>(null)
const goodsToDelete = ref<Goods[]>([])
const deleteMessage = ref('')

// 表单验证错误
const nameError = ref('')
const priceError = ref('')
const categoryError = ref('')
const conditionError = ref('')
const descriptionError = ref('')

// 表单数据
const goodsForm = reactive({
  name: '',
  price: '',
  category: '',
  condition: '',
  description: '',
  images: [] as File[]
})

// 选项数据
const categories = [
  '手机数码', '电脑配件', '图书教材', '生活用品', '服装配饰', '运动器材'
]

const conditions = [
  '全新', '99新', '95新', '9成新', '8成新', '7成新'
]

// 模拟商品数据
const goodsList = ref<Goods[]>([
  {
    id: 1,
    name: 'iPhone 13 Pro',
    category: '手机数码',
    price: 5999,
    condition: '99新',
    status: '在售',
    image: 'https://via.placeholder.com/60',
    description: '个人自用iPhone 13 Pro，成色很新，无磕碰无划痕',
    createdAt: '2025-01-20'
  },
  {
    id: 2,
    name: '高等数学教材',
    category: '图书教材',
    price: 35,
    condition: '9成新',
    status: '已售出',
    image: 'https://via.placeholder.com/60',
    description: '大一高数教材，内容完整，有少量笔记',
    createdAt: '2025-01-18'
  }
])

// 计算属性
const filteredGoods = computed(() => {
  if (!search.value) return goodsList.value
  return goodsList.value.filter(goods => 
    goods.name.toLowerCase().includes(search.value.toLowerCase()) ||
    goods.category.includes(search.value)
  )
})

const isFormValid = computed(() => {
  return goodsForm.name && 
         goodsForm.price && 
         goodsForm.category && 
         goodsForm.condition && 
         goodsForm.description &&
         !nameError.value &&
         !priceError.value &&
         !categoryError.value &&
         !conditionError.value &&
         !descriptionError.value
})

// 验证方法
const validateName = () => {
  if (!goodsForm.name) {
    nameError.value = '商品名称不能为空'
  } else if (goodsForm.name.length > 50) {
    nameError.value = '商品名称不能超过50个字符'
  } else {
    nameError.value = ''
  }
}

const validatePrice = () => {
  if (!goodsForm.price) {
    priceError.value = '价格不能为空'
  } else if (parseFloat(goodsForm.price) <= 0) {
    priceError.value = '价格必须大于0'
  } else {
    priceError.value = ''
  }
}

const validateCategory = () => {
  categoryError.value = goodsForm.category ? '' : '请选择商品分类'
}

const validateCondition = () => {
  conditionError.value = goodsForm.condition ? '' : '请选择商品成色'
}

const validateDescription = () => {
  if (!goodsForm.description) {
    descriptionError.value = '商品描述不能为空'
  } else if (goodsForm.description.length > 500) {
    descriptionError.value = '商品描述不能超过500个字符'
  } else {
    descriptionError.value = ''
  }
}

// 方法
const getStatusClass = (status: string) => {
  const classes: Record<string, string> = {
    '在售': 'status-active',
    '已售出': 'status-sold',
    '已下架': 'status-offline',
    '审核中': 'status-pending'
  }
  return classes[status] || 'status-default'
}

const toggleSelectAll = (event: Event) => {
  const target = event.target as HTMLInputElement
  if (target.checked) {
    selectedGoods.value = filteredGoods.value.map(item => item.id)
  } else {
    selectedGoods.value = []
  }
}

const viewDetails = (item: Goods) => {
  selectedGoodsDetail.value = item
  showDetailDialog.value = true
}

const editGoods = (item: Goods) => {
  editingGoods.value = true
  Object.assign(goodsForm, {
    name: item.name,
    price: item.price.toString(),
    category: item.category,
    condition: item.condition,
    description: item.description,
    images: []
  })
  showAddDialog.value = true
}

const deleteGoods = (item: Goods) => {
  goodsToDelete.value = [item]
  deleteMessage.value = `确定要删除商品"${item.name}"吗？`
  showDeleteDialog.value = true
}

const deleteSelectedGoods = () => {
  if (selectedGoods.value.length === 0) return
  
  const items = goodsList.value.filter(item => selectedGoods.value.includes(item.id))
  goodsToDelete.value = items
  deleteMessage.value = `确定要删除选中的${selectedGoods.value.length}个商品吗？`
  showDeleteDialog.value = true
}

const confirmDelete = async () => {
  deleting.value = true
  try {
    await new Promise(resolve => setTimeout(resolve, 1000))
    
    const idsToDelete = goodsToDelete.value.map(item => item.id)
    goodsList.value = goodsList.value.filter(item => !idsToDelete.includes(item.id))
    
    selectedGoods.value = []
    showDeleteDialog.value = false
  } catch (error) {
    console.error('删除失败:', error)
  } finally {
    deleting.value = false
  }
}

const handleFileUpload = (event: Event) => {
  const target = event.target as HTMLInputElement
  if (target.files) {
    goodsForm.images = Array.from(target.files)
  }
}

const saveGoods = async () => {
  // 验证所有字段
  validateName()
  validatePrice()
  validateCategory()
  validateCondition()
  validateDescription()
  
  if (!isFormValid.value) return
  
  saving.value = true
  try {
    await new Promise(resolve => setTimeout(resolve, 1000))
    
    if (editingGoods.value) {
      console.log('编辑商品:', goodsForm)
    } else {
      const newGoods: Goods = {
        id: Date.now(),
        name: goodsForm.name,
        category: goodsForm.category,
        price: parseFloat(goodsForm.price),
        condition: goodsForm.condition,
        description: goodsForm.description,
        status: '在售',
        image: 'https://via.placeholder.com/60',
        createdAt: new Date().toISOString().split('T')[0]
      }
      goodsList.value.unshift(newGoods)
    }
    
    closeAddDialog()
  } catch (error) {
    console.error('保存失败:', error)
  } finally {
    saving.value = false
  }
}

const closeAddDialog = () => {
  showAddDialog.value = false
  editingGoods.value = false
  Object.assign(goodsForm, {
    name: '',
    price: '',
    category: '',
    condition: '',
    description: '',
    images: []
  })
  // 清空错误信息
  nameError.value = ''
  priceError.value = ''
  categoryError.value = ''
  conditionError.value = ''
  descriptionError.value = ''
}
</script>

<style scoped>
.goods-management {
  padding: 0;
}

/* 操作栏样式 */
.actions-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
  padding: 16px 0;
}

.left-actions {
  display: flex;
  gap: 12px;
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

.action-btn.danger {
  background-color: #f44336;
  color: white;
}

.action-btn.danger:hover:not(:disabled) {
  background-color: #d32f2f;
}

.action-btn.secondary {
  background-color: #f5f5f5;
  color: #666;
}

.action-btn.secondary:hover {
  background-color: #e0e0e0;
}

.search-box {
  display: flex;
  align-items: center;
}

.search-input {
  padding: 10px 16px;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 14px;
  width: 250px;
}

.search-input:focus {
  outline: none;
  border-color: #FF85A2;
}

/* 表格样式 */
.table-card {
  background-color: white;
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
  overflow: hidden;
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

.product-image {
  width: 60px;
  height: 60px;
  border-radius: 8px;
  overflow: hidden;
}

.product-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.product-name {
  font-weight: 500;
  color: #333;
}

.price {
  font-weight: 600;
  color: #FF85A2;
}

.status-tag {
  padding: 4px 8px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 500;
}

.status-active { background-color: #e8f5e8; color: #4caf50; }
.status-sold { background-color: #f5f5f5; color: #666; }
.status-offline { background-color: #ffebee; color: #f44336; }
.status-pending { background-color: #fff3e0; color: #ff9800; }

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

.table-action-btn.view {
  background-color: #e3f2fd;
  color: #2196f3;
}

.table-action-btn.edit {
  background-color: #e8f5e8;
  color: #4caf50;
}

.table-action-btn.delete {
  background-color: #ffebee;
  color: #f44336;
}

.table-action-btn:hover {
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

.modal-content.small {
  max-width: 400px;
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

/* 表单样式 */
.goods-form {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 20px;
}

.form-item {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.form-item.full-width {
  grid-column: 1 / -1;
}

.form-item label {
  font-size: 14px;
  color: #555;
  font-weight: 500;
}

.form-input, .form-select, .form-textarea {
  padding: 10px 12px;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 14px;
  transition: border-color 0.2s ease;
}

.form-input:focus, .form-select:focus, .form-textarea:focus {
  outline: none;
  border-color: #FF85A2;
}

.form-input.error, .form-select.error, .form-textarea.error {
  border-color: #f44336;
}

.price-input {
  position: relative;
  display: flex;
  align-items: center;
}

.currency {
  position: absolute;
  left: 12px;
  color: #666;
  font-size: 14px;
  z-index: 1;
}

.price-input .form-input {
  padding-left: 32px;
}

.error-message {
  font-size: 12px;
  color: #f44336;
}

.file-upload {
  border: 2px dashed #ddd;
  border-radius: 8px;
  padding: 20px;
  text-align: center;
  transition: border-color 0.2s ease;
}

.file-upload:hover {
  border-color: #FF85A2;
}

.file-input {
  width: 100%;
  padding: 8px;
}

.upload-hint {
  margin-top: 8px;
  font-size: 12px;
  color: #666;
}

.form-actions {
  display: flex;
  gap: 12px;
  justify-content: flex-end;
  margin-top: 20px;
}

/* 商品详情样式 */
.goods-detail {
  display: grid;
  grid-template-columns: 200px 1fr;
  gap: 24px;
}

.detail-image {
  width: 200px;
  height: 200px;
  border-radius: 8px;
  overflow: hidden;
}

.detail-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.detail-info {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.detail-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: 16px;
}

.detail-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.detail-item label {
  font-size: 12px;
  color: #666;
  font-weight: 500;
}

.detail-item div {
  font-size: 14px;
  color: #333;
}

.detail-description {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.detail-description label {
  font-size: 14px;
  color: #666;
  font-weight: 500;
}

.detail-description p {
  margin: 0;
  font-size: 14px;
  color: #333;
  line-height: 1.5;
}

.delete-message {
  font-size: 16px;
  color: #333;
  margin: 0 0 20px 0;
  text-align: center;
}

/* 响应式设计 */
@media (max-width: 768px) {
  .actions-bar {
    flex-direction: column;
    gap: 16px;
    align-items: stretch;
  }
  
  .goods-detail {
    grid-template-columns: 1fr;
    gap: 16px;
  }
  
  .detail-image {
    width: 100%;
    max-width: 200px;
    margin: 0 auto;
  }
}
</style>
