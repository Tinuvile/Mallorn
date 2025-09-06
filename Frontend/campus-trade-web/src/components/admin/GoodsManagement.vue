<template>
  <div class="goods-management">
    <!-- 操作按钮区 -->
    <div class="actions-bar">
      <div class="left-actions">
        <button class="action-btn secondary" @click="refreshData">
          <span>🔄</span>
          刷新数据
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
          @keyup.enter="performSearch"
        >
        <button class="search-btn" @click="performSearch">搜索</button>
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
            <tr v-for="item in filteredGoods" :key="item.productId" :class="{ loading: loading }">
              <td>
                <input 
                  type="checkbox" 
                  :value="item.productId"
                  v-model="selectedGoods"
                >
              </td>
              <td>
                <div class="product-image">
                  <img :src="item.image" :alt="item.title" />
                </div>
              </td>
              <td class="product-name">{{ item.title }}</td>
              <td>{{ item.categoryName }}</td>
              <td class="price">¥{{ item.basePrice }}</td>
              <td>{{ item.condition }}</td>
              <td>
                <span class="status-tag" :class="getStatusClass(item.status)">
                  {{ getStatusText(item.status) }}
                </span>
              </td>
              <td>{{ new Date(item.createdAt || item.publish_time || '').toLocaleDateString() }}</td>
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
              <img :src="selectedGoodsDetail.image" :alt="selectedGoodsDetail.title">
            </div>
            <div class="detail-info">
              <div class="detail-grid">
                <div class="detail-item">
                  <label>商品名称</label>
                  <div>{{ selectedGoodsDetail.title }}</div>
                </div>
                <div class="detail-item">
                  <label>价格</label>
                  <div class="price">¥{{ selectedGoodsDetail.basePrice }}</div>
                </div>
                <div class="detail-item">
                  <label>分类</label>
                  <div>{{ selectedGoodsDetail.categoryName }}</div>
                </div>
                <div class="detail-item">
                  <label>成色</label>
                  <div>{{ selectedGoodsDetail.condition }}</div>
                </div>
                <div class="detail-item">
                  <label>状态</label>
                  <div>
                    <span class="status-tag" :class="getStatusClass(selectedGoodsDetail.status)">
                      {{ getStatusText(selectedGoodsDetail.status) }}
                    </span>
                  </div>
                </div>
                <div class="detail-item">
                  <label>发布时间</label>
                  <div>{{ new Date(selectedGoodsDetail.createdAt || selectedGoodsDetail.publish_time || '').toLocaleDateString() }}</div>
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

    <!-- 编辑商品模态框 -->
    <div v-if="showEditDialog" class="modal-overlay" @click="closeEditDialog">
      <div class="modal-content large" @click.stop>
        <div class="modal-header">
          <h3>编辑商品</h3>
          <button class="modal-close" @click="closeEditDialog">✕</button>
        </div>
        <div class="modal-body">
          <form @submit.prevent="saveGoods" class="goods-form">
            <div class="form-grid">
              <div class="form-item">
                <label>商品名称 *</label>
                <input 
                  v-model="goodsForm.title" 
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
                    v-model="goodsForm.basePrice" 
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
                  v-model="goodsForm.categoryId" 
                  class="form-select"
                  :class="{ error: categoryError }"
                  @blur="validateCategory"
                >
                  <option value="">请选择分类</option>
                  <option v-for="category in categories" :key="category.categoryId" :value="category.categoryId.toString()">
                    {{ category.name }}
                  </option>
                </select>
                <span v-if="categoryError" class="error-message">{{ categoryError }}</span>
              </div>
              <div class="form-item">
                <label>商品状态</label>
                <select v-model="goodsForm.status" class="form-select">
                  <option value="在售">在售</option>
                  <option value="已下架">已下架</option>
                  <option value="交易中">交易中</option>
                </select>
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
              <label>管理员备注</label>
              <textarea 
                v-model="goodsForm.adminNote" 
                class="form-textarea"
                rows="3"
                placeholder="管理员操作备注（可选）"
              ></textarea>
            </div>
            <div class="form-actions">
              <button type="button" class="action-btn secondary" @click="closeEditDialog">
                取消
              </button>
              <button type="submit" class="action-btn primary" :disabled="!isFormValid || saving">
                {{ saving ? '保存中...' : '保存修改' }}
              </button>
            </div>
          </form>
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
import { ref, reactive, computed, onMounted } from 'vue'
import { adminApi, type AdminProduct, type AdminProductQuery, type AdminUpdateProductRequest, type BatchProductOperationRequest } from '@/services/api'

// 定义接口
interface Goods extends AdminProduct {
  // 添加一些界面需要的字段
  condition?: string
  image?: string
  // 为了向后兼容，添加旧字段名的别名
  productId?: number
  basePrice?: number
  categoryId?: number
  categoryName?: string
  sellerId?: number
  sellerName?: string
  createdAt?: string
  updatedAt?: string
  imageUrls?: string[]
}

// 响应式数据
const loading = ref(false)
const saving = ref(false)
const deleting = ref(false)
const search = ref('')
const selectedGoods = ref<number[]>([])
const showAddDialog = ref(false)
const showEditDialog = ref(false)
const showDetailDialog = ref(false)
const showDeleteDialog = ref(false)
const editingGoods = ref(false)
const currentEditingGoods = ref<Goods | null>(null)
const selectedGoodsDetail = ref<Goods | null>(null)
const goodsToDelete = ref<Goods[]>([])
const deleteMessage = ref('')

// 分页信息
const pageIndex = ref(0)
const pageSize = ref(20)
const totalCount = ref(0)
const totalPages = ref(0)

// 表单验证错误
const nameError = ref('')
const priceError = ref('')
const categoryError = ref('')
const conditionError = ref('')
const descriptionError = ref('')

// 表单数据
const goodsForm = reactive({
  title: '',
  basePrice: '',
  categoryId: '',
  condition: '',
  description: '',
  status: '',
  adminNote: '',
  images: [] as File[]
})

// 选项数据
const categories = ref<Array<{ categoryId: number; name: string }>>([])
const conditions = [
  '全新', '99新', '95新', '9成新', '8成新', '7成新'
]

// 商品数据
const goodsList = ref<Goods[]>([])

// 查询参数
const queryParams = reactive<AdminProductQuery>({
  pageIndex: 0,
  pageSize: 20,
  status: undefined,
  categoryId: undefined,
  searchKeyword: ''
})

// 计算属性
const filteredGoods = computed(() => {
  let filtered = goodsList.value
  
  if (search.value) {
    filtered = filtered.filter(goods => 
      goods.title.toLowerCase().includes(search.value.toLowerCase()) ||
      (goods.categoryName && goods.categoryName.includes(search.value))
    )
  }
  
  return filtered
})

const isFormValid = computed(() => {
  // 编辑模式下，只要有任何一个字段有值且没有错误就认为表单有效
  if (editingGoods.value) {
    return (goodsForm.title || goodsForm.basePrice || goodsForm.categoryId || goodsForm.description) &&
           !nameError.value &&
           !priceError.value &&
           !categoryError.value &&
           !descriptionError.value
  }
  // 新增模式下需要所有必要字段
  return goodsForm.title && 
         goodsForm.basePrice && 
         goodsForm.categoryId && 
         goodsForm.description &&
         !nameError.value &&
         !priceError.value &&
         !categoryError.value &&
         !descriptionError.value
})

// API 调用方法
const fetchGoods = async () => {
  try {
    loading.value = true
    
    const query: AdminProductQuery = {
      ...queryParams,
      searchKeyword: search.value || undefined
    }
    
    const response = await adminApi.getManagedProducts(query)
    
    if (response.success && response.data) {
      goodsList.value = response.data.products.map(product => ({
        // 新的API结构字段
        product_id: product.product_id,
        title: product.title,
        description: product.description || '',
        base_price: product.base_price,
        status: product.status,
        publish_time: product.publish_time,
        view_count: product.view_count,
        main_image_url: product.main_image_url,
        thumbnail_url: product.thumbnail_url,
        user: product.user,
        category: product.category,
        days_until_auto_remove: product.days_until_auto_remove,
        is_popular: product.is_popular,
        tags: product.tags || [],
        // 向后兼容的字段映射
        productId: product.product_id,
        basePrice: product.base_price,
        categoryId: product.category.category_id,
        categoryName: product.category.name,
        sellerId: product.user.user_id,
        sellerName: product.user.username || product.user.name || '未知用户',
        createdAt: product.publish_time,
        updatedAt: product.publish_time,
        imageUrls: product.main_image_url ? [product.main_image_url] : [],
        // 界面特定字段
        condition: '9成新',
        image: product.main_image_url || product.thumbnail_url || 'https://via.placeholder.com/60'
      }))
      totalCount.value = response.data.totalCount
      totalPages.value = Math.ceil(response.data.totalCount / pageSize.value)
    }
  } catch (error) {
    console.error('获取商品列表失败:', error)
  } finally {
    loading.value = false
  }
}

const fetchCategories = async () => {
  try {
    const response = await adminApi.getManagedCategories()
    
    if (response.success && response.data) {
      // 这里需要获取分类的详细信息，现在先使用模拟数据
      categories.value = [
        { categoryId: 1, name: '手机数码' },
        { categoryId: 2, name: '电脑配件' },
        { categoryId: 3, name: '图书教材' },
        { categoryId: 4, name: '生活用品' },
        { categoryId: 5, name: '服装配饰' },
        { categoryId: 6, name: '运动器材' }
      ]
    }
  } catch (error) {
    console.error('获取分类列表失败:', error)
  }
}

// 验证方法
const validateName = () => {
  if (!goodsForm.title) {
    nameError.value = '商品名称不能为空'
  } else if (goodsForm.title.length > 50) {
    nameError.value = '商品名称不能超过50个字符'
  } else {
    nameError.value = ''
  }
}

const validatePrice = () => {
  if (!goodsForm.basePrice) {
    priceError.value = '价格不能为空'
  } else if (parseFloat(goodsForm.basePrice) <= 0) {
    priceError.value = '价格必须大于0'
  } else {
    priceError.value = ''
  }
}

const validateCategory = () => {
  categoryError.value = goodsForm.categoryId ? '' : '请选择商品分类'
}

const validateCondition = () => {
  // 管理员编辑时condition不是必需的
  conditionError.value = ''
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
    'active': 'status-active',
    'sold': 'status-sold',
    'inactive': 'status-offline',
    'pending': 'status-pending',
    '在售': 'status-active',
    '已售出': 'status-sold',
    '已下架': 'status-offline',
    '交易中': 'status-pending'
  }
  return classes[status] || 'status-default'
}

const getStatusText = (status: string) => {
  const statusMap: Record<string, string> = {
    'active': '在售',
    'sold': '已售出',
    'inactive': '已下架',
    'pending': '审核中',
    '在售': '在售',
    '已售出': '已售出',
    '已下架': '已下架',
    '交易中': '交易中'
  }
  return statusMap[status] || status
}

const toggleSelectAll = (event: Event) => {
  const target = event.target as HTMLInputElement
  if (target.checked) {
    selectedGoods.value = filteredGoods.value.map(item => item.productId || item.product_id).filter(id => id !== undefined) as number[]
  } else {
    selectedGoods.value = []
  }
}

const viewDetails = async (item: Goods) => {
  try {
    // 使用正确的产品ID字段
    const productId = item.productId || item.product_id
    const response = await adminApi.getProductDetailForAdmin(productId)
    
    if (response.success && response.data) {
      // 将后端数据映射为前端格式
      const product = response.data
      selectedGoodsDetail.value = {
        // 新的API结构字段
        product_id: product.product_id,
        title: product.title,
        description: product.description || '',
        base_price: product.base_price,
        status: product.status,
        publish_time: product.publish_time,
        view_count: product.view_count,
        main_image_url: product.main_image_url,
        thumbnail_url: product.thumbnail_url,
        user: product.user,
        category: product.category,
        days_until_auto_remove: product.days_until_auto_remove,
        is_popular: product.is_popular,
        tags: product.tags || [],
        // 向后兼容的字段映射
        productId: product.product_id,
        basePrice: product.base_price,
        categoryId: product.category.category_id,
        categoryName: product.category.name,
        sellerId: product.user.user_id,
        sellerName: product.user.username || product.user.name || '未知用户',
        createdAt: product.publish_time,
        updatedAt: product.publish_time,
        imageUrls: product.main_image_url ? [product.main_image_url] : [],
        condition: item.condition,
        image: item.image
      }
      showDetailDialog.value = true
    }
  } catch (error) {
    console.error('获取商品详情失败:', error)
  }
}

const editGoods = (item: Goods) => {
  editingGoods.value = true
  currentEditingGoods.value = item
  Object.assign(goodsForm, {
    title: item.title,
    basePrice: (item.basePrice || item.base_price)?.toString() || '0',
    categoryId: (item.categoryId || item.category?.category_id)?.toString() || '0',
    condition: item.condition || '9成新',
    description: item.description,
    status: item.status,
    adminNote: '',
    images: []
  })
  showEditDialog.value = true
}

const deleteGoods = (item: Goods) => {
  goodsToDelete.value = [item]
  deleteMessage.value = `确定要删除商品"${item.title}"吗？`
  showDeleteDialog.value = true
}

const deleteSelectedGoods = () => {
  if (selectedGoods.value.length === 0) return
  
  const items = goodsList.value.filter(item => {
    const id = item.productId || item.product_id
    return id && selectedGoods.value.includes(id)
  })
  goodsToDelete.value = items
  deleteMessage.value = `确定要删除选中的${selectedGoods.value.length}个商品吗？`
  showDeleteDialog.value = true
}

const confirmDelete = async () => {
  deleting.value = true
  try {
    const idsToDelete = goodsToDelete.value.map(item => item.productId || item.product_id).filter(id => id !== undefined) as number[]
    
    console.log('准备删除的商品ID:', idsToDelete)
    console.log('删除的商品对象:', goodsToDelete.value)
    
    if (idsToDelete.length === 1) {
      // 单个删除
      console.log('执行单个删除，ID:', idsToDelete[0])
      await adminApi.deleteProductAsAdmin(idsToDelete[0], '管理员删除')
      alert('商品删除成功！')
    } else {
      // 批量删除
      const batchRequest: BatchProductOperationRequest = {
        productIds: idsToDelete,
        operationType: 'delete',
        reason: '管理员批量删除'
      }
      console.log('执行批量删除，请求数据:', batchRequest)
      await adminApi.batchOperateProducts(batchRequest)
      alert(`成功删除 ${idsToDelete.length} 个商品！`)
    }
    
    // 删除成功后刷新列表
    await fetchGoods()
    selectedGoods.value = []
    showDeleteDialog.value = false
  } catch (error) {
    console.error('删除失败:', error)
    let errorMessage = '删除失败，请检查网络连接或联系管理员'
    if (error instanceof Error) {
      errorMessage = `删除失败: ${error.message}`
    } else if (error && typeof error === 'object' && 'response' in error) {
      const axiosError = error as any
      if (axiosError.response?.data?.message) {
        errorMessage = `删除失败: ${axiosError.response.data.message}`
      } else if (axiosError.response?.status) {
        errorMessage = `删除失败: HTTP ${axiosError.response.status}`
      }
    }
    alert(errorMessage)
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
  validateDescription()
  
  if (!isFormValid.value) return
  
  saving.value = true
  try {
    if (currentEditingGoods.value) {
      const updateRequest: AdminUpdateProductRequest = {}
      
      // 只包含有值的字段
      if (goodsForm.title && goodsForm.title.trim()) updateRequest.title = goodsForm.title.trim()
      if (goodsForm.description && goodsForm.description.trim()) updateRequest.description = goodsForm.description.trim()
      if (goodsForm.basePrice && goodsForm.basePrice.trim() && !isNaN(parseFloat(goodsForm.basePrice)) && parseFloat(goodsForm.basePrice) > 0) {
        updateRequest.basePrice = parseFloat(goodsForm.basePrice)
      }
      if (goodsForm.categoryId && goodsForm.categoryId.trim() && !isNaN(parseInt(goodsForm.categoryId)) && parseInt(goodsForm.categoryId) > 0) {
        updateRequest.categoryId = parseInt(goodsForm.categoryId)
      }
      if (goodsForm.status && goodsForm.status.trim()) updateRequest.status = goodsForm.status
      if (goodsForm.adminNote && goodsForm.adminNote.trim()) updateRequest.adminNote = goodsForm.adminNote
      
      // 如果没有任何字段要更新，则不执行请求
      if (Object.keys(updateRequest).length === 0) {
        alert('请至少修改一个字段')
        return
      }
      
      const productId = currentEditingGoods.value.productId || currentEditingGoods.value.product_id!
      console.log('更新请求数据:', updateRequest)
      console.log('商品ID:', productId)
      
      await adminApi.updateProductAsAdmin(productId, updateRequest)
      alert('商品信息更新成功！')
    } else {
      alert('找不到要更新的商品信息')
      return
    }
    
    // 保存成功后刷新列表
    await fetchGoods()
    closeEditDialog()
  } catch (error) {
    console.error('保存失败:', error)
    let errorMessage = '保存失败，请检查输入信息或联系管理员'
    if (error instanceof Error) {
      errorMessage = `保存失败: ${error.message}`
    } else if (error && typeof error === 'object' && 'response' in error) {
      const axiosError = error as any
      if (axiosError.response?.data?.message) {
        errorMessage = `保存失败: ${axiosError.response.data.message}`
      } else if (axiosError.response?.status) {
        errorMessage = `保存失败: HTTP ${axiosError.response.status}`
      }
    }
    alert(errorMessage)
  } finally {
    saving.value = false
  }
}

const closeEditDialog = () => {
  showEditDialog.value = false
  editingGoods.value = false
  currentEditingGoods.value = null
  Object.assign(goodsForm, {
    title: '',
    basePrice: '',
    categoryId: '',
    condition: '',
    description: '',
    status: '',
    adminNote: '',
    images: []
  })
  // 清空错误信息
  nameError.value = ''
  priceError.value = ''
  categoryError.value = ''
  conditionError.value = ''
  descriptionError.value = ''
}

// 刷新数据
const refreshData = () => {
  selectedGoods.value = []
  fetchGoods()
  fetchCategories()
}

// 分页方法
const changePage = (newPageIndex: number) => {
  queryParams.pageIndex = newPageIndex
  pageIndex.value = newPageIndex
  fetchGoods()
}

// 搜索方法
const performSearch = () => {
  queryParams.pageIndex = 0
  pageIndex.value = 0
  fetchGoods()
}

// 组件挂载时获取数据
onMounted(() => {
  fetchGoods()
  fetchCategories()
})
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
  gap: 8px;
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

.search-btn {
  padding: 10px 16px;
  background-color: #FF85A2;
  color: white;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 14px;
  transition: background-color 0.2s ease;
}

.search-btn:hover {
  background-color: #ff6b90;
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
  
  .pagination-container {
    flex-direction: column;
    gap: 12px;
  }
  
  .pagination-buttons {
    justify-content: center;
  }
}
</style>
