<template>
  <div class="full-page">
    <!-- 头部导航栏保持不变 -->
    <header class="navbar">
      <span class="icon">
        <svg
          width="48px"
          height="48px"
          stroke-width="1.5"
          viewBox="0 0 24 24"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
          color="#ffffff"
        >
          <circle cx="12" cy="12" r="10" stroke="#ffffff" stroke-width="1.5"></circle>
          <path
            d="M7.63262 3.06689C8.98567 3.35733 9.99999 4.56025 9.99999 6.00007C9.99999 7.65693 8.65685 9.00007 6.99999 9.00007C5.4512 9.00007 4.17653 7.82641 4.01685 6.31997"
            stroke="#ffffff"
            stroke-width="1.5"
          ></path>
          <path
            d="M22 13.0505C21.3647 12.4022 20.4793 12 19.5 12C17.567 12 16 13.567 16 15.5C16 17.2632 17.3039 18.7219 19 18.9646"
            stroke="#ffffff"
            stroke-width="1.5"
          ></path>
          <path
            d="M14.5 8.51L14.51 8.49889"
            stroke="#ffffff"
            stroke-width="1.5"
            stroke-linecap="round"
            stroke-linejoin="round"
          ></path>
          <path
            d="M10 17C11.1046 17 12 16.1046 12 15C12 13.8954 11.1046 13 10 13C8.89543 13 8 13.8954 8 15C8 16.1046 8.89543 17 10 17Z"
            stroke="#ffffff"
            stroke-width="1.5"
            stroke-linecap="round"
            stroke-linejoin="round"
          ></path>
        </svg>
      </span>
      <span class="title">Campus Secondhand</span>
      <v-btn class="logout-btn ml-auto" icon to="/">
        <v-icon color="black">mdi-exit-to-app</v-icon>
      </v-btn>
    </header>

    <v-card flat class="content-card">
      <div class="title-container">
        <v-card-title class="product-title">发布商品</v-card-title>
      </div>

      <v-divider class="my-3 custom-divider"></v-divider>

      <v-row class="ma-0 content-row">
        <!-- 左侧：图片上传区域 -->
        <v-col cols="12" md="4" class="pa-6">
          <h2 class="text-h6 mb-4">上传商品图片</h2>
          <v-card outlined class="pa-4 rounded-lg image-upload-card">
            <v-btn
              color="#cadefc"
              size="large"
              class="mb-4 mx-auto d-block select-image-btn"
              @click="triggerFileInput"
              :loading="uploading"
            >
              选择图片
            </v-btn>
            <!-- 修改文件输入部分 -->
            <input
              ref="fileInput"
              type="file"
              multiple
              accept="image/*"
              style="display: none"
              @change="handleImageUpload"
            />
            <div class="image-grid-container rounded-lg">
              <div class="image-grid">
                <div
                  v-for="(preview, index) in imagePreviews"
                  :key="index"
                  class="image-item dashed-border"
                >
                  <img :src="preview" class="preview-image" alt="商品图片" />
                  <v-btn icon small color="error" class="delete-btn" @click="removeImage(index)">
                    <v-icon>mdi-delete</v-icon>
                  </v-btn>
                </div>
                <div
                  v-for="n in 4 - imagePreviews.length"
                  :key="'empty-' + n"
                  class="image-item empty dashed-border"
                >
                  <v-icon size="48" color="grey">mdi-plus</v-icon>
                </div>
              </div>
            </div>

            <div class="text-caption text-grey mt-2">
              最多可上传4张图片，建议上传清晰、真实的商品图片
            </div>
          </v-card>
        </v-col>

        <!-- 右侧：商品信息表单 -->
        <v-col cols="12" md="8" class="pa-0">
          <div class="d-flex h-100">
            <v-divider vertical class="mr-4 custom-vertical-divider"></v-divider>
            <div class="pa-6 flex-grow-1 form-content">
              <h2 class="text-subtitle-2 mb-2">商品标题</h2>
              <v-text-field
                v-model="productData.title"
                label="请输入商品名称"
                variant="outlined"
                required
                density="compact"
                :error-messages="fieldErrors.title"
                class="right-input-border"
              ></v-text-field>

              <h2 class="text-subtitle-2 mb-2 mt-4">商品描述</h2>
              <v-textarea
                v-model="productData.description"
                label="商品描述"
                variant="outlined"
                rows="2"
                density="compact"
                placeholder="请详细描述商品信息，包括新旧程度、使用情况等"
                required
                :error-messages="fieldErrors.description"
                class="right-input-border"
              ></v-textarea>

              <h2 class="text-subtitle-2 mb-2 mt-4">基础价格</h2>
              <v-text-field
                v-model="productData.price"
                label="价格"
                variant="outlined"
                type="number"
                density="compact"
                prefix="¥"
                required
                :error-messages="fieldErrors.price"
                class="right-input-border"
              ></v-text-field>

              <h2 class="text-subtitle-2 mb-2 mt-4">商品分类</h2>
              <v-select
                v-model="productData.categoryId"
                :items="categoryOptions"
                item-title="name"
                item-value="id"
                label="商品分类"
                variant="outlined"
                density="compact"
                placeholder="请选择分类"
                required
                :error-messages="fieldErrors.categoryId"
                class="right-input-border"
              ></v-select>

              <h2 class="text-subtitle-2 mb-2 mt-4">商品成色</h2>
              <v-select
                v-model="productData.condition"
                :items="conditionOptions"
                label="商品成色"
                variant="outlined"
                density="compact"
                placeholder="请选择商品成色"
                required
                :error-messages="fieldErrors.condition"
                class="right-input-border"
              ></v-select>

              <h2 class="text-subtitle-2 mb-2 mt-4">库存数量</h2>
              <v-text-field
                v-model="productData.stock"
                label="库存数量"
                variant="outlined"
                type="number"
                density="compact"
                required
                :error-messages="fieldErrors.stock"
                class="right-input-border"
              ></v-text-field>

              <!-- 联系方式部分已移除，使用用户信息中的联系方式 -->

              <h2 class="text-subtitle-2 mb-2 mt-4">所在位置</h2>
              <v-text-field
                v-model="productData.location"
                label="所在位置"
                variant="outlined"
                density="compact"
                placeholder="请输入商品所在位置（如：XX校区XX宿舍）"
                :error-messages="fieldErrors.location"
                class="right-input-border"
              ></v-text-field>

              <v-card-actions class="px-0 mt-6">
                <v-spacer></v-spacer>
                <v-btn
                  color="primary"
                  size="large"
                  @click="submitProduct"
                  :loading="submitting"
                  class="submit-btn-class"
                >
                  发布商品
                </v-btn>
              </v-card-actions>
            </div>
          </div>
        </v-col>
      </v-row>
    </v-card>

    <!-- 成功提示对话框 -->
    <v-dialog v-model="successDialog" max-width="400">
      <v-card>
        <v-card-title class="text-h5">发布成功</v-card-title>
        <v-card-text>商品已成功发布！</v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="primary" @click="successDialog = false">确定</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
  import { ref, reactive, onMounted, computed, watch } from 'vue'
  import { useProductStore } from '@/stores/product'
  import { useUserStore } from '@/stores/user'
  import { useRouter } from 'vue-router'
  import {
    productApi,
    fileApi,
    categoryApi,
    type CreateProductRequest,
    type CategoryTreeResponse,
  } from '@/services/api'

  const productStore = useProductStore()
  const userStore = useUserStore()
  const router = useRouter()

  // 图片上传相关
  const images = ref<File[]>([])
  const imagePreviews = ref<string[]>([])
  const fileInput = ref<HTMLInputElement>()
  const uploading = ref(false)

  // 商品表单数据
  const productData = reactive({
    title: '',
    description: '',
    price: 0,
    categoryId: 0,
    condition: '',
    stock: 1,
    location: '',
    tags: [] as string[],
    specifications: [] as any[],
    contactMethod: '',
    imageUrls: [] as string[], // 存储已上传图片的URL
  })

  // 表单错误信息
  const fieldErrors = reactive({
    title: '',
    description: '',
    price: '',
    categoryId: '',
    condition: '',
    stock: '',
    location: '',
    contactMethod: '',
  })

  // 分类选项
  const categoryOptions = ref<{ id: number; name: string }[]>([])
  const conditionOptions = ref([
    { value: '全新', text: '全新' },
    { value: '几乎全新', text: '几乎全新' },
    { value: '轻微使用', text: '轻微使用' },
    { value: '明显使用', text: '明显使用' },
    { value: '需要维修', text: '需要维修' },
  ])

  // 提交状态
  const submitting = ref(false)
  const successDialog = ref(false)

  // 计算用户是否有联系方式
  const hasContactInfo = computed(() => {
    return !!(userStore.user?.phone || userStore.user?.email)
  })

  // 根据用户联系方式自动设置 contactMethod
  const setContactMethodFromUser = () => {
    if (!userStore.user) return

    const { phone, email } = userStore.user

    if (phone && email) {
      productData.contactMethod = 'both'
    } else if (phone) {
      productData.contactMethod = 'phone'
    } else if (email) {
      productData.contactMethod = 'email'
    } else {
      productData.contactMethod = ''
    }
  }

  // 加载分类数据
  onMounted(async () => {
    await loadCategories()
    // 确保用户信息已加载
    if (userStore.isLoggedIn && !userStore.user?.phone && !userStore.user?.email) {
      await userStore.fetchUserInfo(userStore.user?.username || '')
    }

    // 设置默认联系方式
    setContactMethodFromUser()
  })

  // 监听用户信息变化，自动更新联系方式
  watch(() => userStore.user, setContactMethodFromUser, { deep: true })

  // 加载分类列表
  const loadCategories = async () => {
    try {
      const response = await categoryApi.getCategoryTree()
      if (response.success && response.data) {
        // 将分类树扁平化为选项列表
        const flattenCategories = (categories: any[], prefix = ''): any[] => {
          let result: any[] = []
          categories.forEach(category => {
            const fullName = prefix ? `${prefix} > ${category.name}` : category.name
            result.push({
              id: category.category_id,
              name: fullName,
            })
            if (category.children && category.children.length > 0) {
              result = result.concat(flattenCategories(category.children, fullName))
            }
          })
          return result
        }

        categoryOptions.value = flattenCategories(response.data.root_categories || [])
      } else {
        // 使用默认分类
        categoryOptions.value = [
          { id: 1, name: '教材 > 计算机科学' },
          { id: 2, name: '教材 > 数学' },
          { id: 3, name: '教材 > 英语' },
          { id: 4, name: '数码 > 手机' },
          { id: 5, name: '数码 > 电脑' },
          { id: 6, name: '日用 > 文具' },
        ]
      }
    } catch (error) {
      console.error('加载分类失败:', error)
      // 使用默认分类
      categoryOptions.value = [
        { id: 1, name: '教材 > 计算机科学' },
        { id: 2, name: '教材 > 数学' },
        { id: 3, name: '教材 > 英语' },
        { id: 4, name: '数码 > 手机' },
        { id: 5, name: '数码 > 电脑' },
        { id: 6, name: '日用 > 文具' },
      ]
    }
  }

  // 触发文件选择
  const triggerFileInput = () => {
    fileInput.value?.click()
  }

  // 处理图片上传
  const handleImageUpload = (event: Event) => {
    const target = event.target as HTMLInputElement
    const files = target.files

    if (files && files.length > 0) {
      // 限制最多上传4张图片
      const filesToUpload = Array.from(files).slice(0, 4)

      filesToUpload.forEach(file => {
        const reader = new FileReader()
        reader.onload = e => {
          if (e.target?.result) {
            imagePreviews.value.push(e.target.result as string)
          }
        }
        reader.readAsDataURL(file)
      })
      images.value = filesToUpload

      // 重置input，允许再次选择相同文件
      target.value = ''
    }
  }

  // 删除图片
  const removeImage = (index: number) => {
    imagePreviews.value.splice(index, 1)
    images.value.splice(index, 1)
  }

  // 验证表单
  const validateForm = (): boolean => {
    let isValid = true

    // 重置错误信息
    Object.keys(fieldErrors).forEach(key => {
      fieldErrors[key as keyof typeof fieldErrors] = ''
    })

    if (!productData.title.trim()) {
      fieldErrors.title = '请输入商品标题'
      isValid = false
    }

    if (!productData.description.trim()) {
      fieldErrors.description = '请输入商品描述'
      isValid = false
    }

    if (!productData.price || productData.price <= 0) {
      fieldErrors.price = '请输入有效的价格'
      isValid = false
    }

    if (!productData.categoryId) {
      fieldErrors.categoryId = '请选择商品分类'
      isValid = false
    }

    if (!productData.condition) {
      fieldErrors.condition = '请选择商品成色'
      isValid = false
    }

    if (!productData.stock || productData.stock <= 0) {
      fieldErrors.stock = '请输入有效的库存数量'
      isValid = false
    }

    if (!productData.contactMethod) {
      fieldErrors.contactMethod = '请确保您已设置联系方式'
      isValid = false
    }

    if (imagePreviews.value.length === 0) {
      // 这里可以添加图片错误提示
      isValid = false
    }

    return isValid
  }

  // 上传图片到服务器
  const uploadImages = async (): Promise<string[]> => {
    if (images.value.length === 0) {
      return []
    }

    uploading.value = true
    try {
      const uploadedUrls: string[] = []

      // 逐个上传图片
      for (const image of images.value) {
        try {
          const response = await fileApi.uploadProductImage(image)
          if (response.success && response.data?.fileUrl) {
            uploadedUrls.push(response.data.fileUrl)
          } else {
            console.error('图片上传失败:', response.message)
          }
        } catch (error) {
          console.error('上传单个图片失败:', error)
        }
      }

      return uploadedUrls
    } catch (error) {
      console.error('批量上传图片失败:', error)
      return []
    } finally {
      uploading.value = false
    }
  }

  // 提交商品
  const submitProduct = async () => {
    if (!validateForm()) {
      return
    }

    submitting.value = true

    try {
      // 先上传图片
      const imageUrls = await uploadImages()

      if (images.value.length > 0 && imageUrls.length === 0) {
        console.error('图片上传失败')
        return
      }

      // 准备商品数据，匹配后端API格式
      const productRequest: CreateProductRequest = {
        title: productData.title,
        description: productData.description,
        base_price: productData.price,
        category_id: productData.categoryId,
        image_urls: imageUrls,
        auto_remove_time: new Date(Date.now() + 20 * 24 * 60 * 60 * 1000).toISOString(), // 20天后自动下架
      }

      // 调用API创建商品
      const response = await productApi.createProduct(productRequest)

      if (response.success) {
        successDialog.value = true
        // 跳转到商品列表或详情页
        setTimeout(() => {
          router.push('/')
        }, 2000)
      } else {
        // 处理错误信息
        console.error('发布失败:', response.message)
        alert(`发布失败: ${response.message}`)
      }
    } catch (error) {
      console.error('发布商品失败:', error)
      alert('发布商品失败，请重试')
    } finally {
      submitting.value = false
    }
  }
</script>

<style scoped>
  /* 重置全局样式 */
  body,
  .v-application {
    font-family: 'Microsoft YaHei', '微软雅黑', sans-serif !important;
    margin: 0;
    padding: 0;
    overflow: auto !important; /* 允许全局滚动 */
  }

  /* 导航栏样式 */
  .navbar {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    height: 60px;
    padding: 0 16px;
    background-color: #cadefc;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    z-index: 1000;
    display: flex;
    align-items: center;
    justify-content: flex-start;
  }

  /* 主要内容容器 */
  .full-page {
    min-height: 100vh;
    display: flex;
    flex-direction: column;
    background-color: #f5f5f5;
  }

  .content-wrapper {
    flex: 1;
    margin-top: 60px; /* 导航栏高度 */
    overflow: visible; /* 允许内容溢出 */
  }

  .content-card {
    min-height: calc(100vh - 60px);
    border-radius: 0 !important;
    margin-top: 60px;
  }

  /* 内容行 */
  .content-row {
    min-height: calc(100vh - 140px);
  }

  /* 列样式 */
  .v-col {
    overflow: visible; /* 允许内容溢出 */
  }

  .image-col {
    max-height: none;
  }

  .form-col {
    max-height: none;
    overflow: visible;
  }

  /* 表单内容区域 */
  .form-content {
    padding-bottom: 32px;
  }

  /* 图片上传区域 */
  .image-upload-card {
    height: auto;
    min-height: 400px;
    background-color: #f0f2f6 !important;
    border: 1px solid #cadefc !important;
  }

  /* 图片网格容器 */
  .image-grid-container {
    height: 400px;
    overflow: visible; /* 允许图片区域内容正常显示 */
  }

  /* 发布商品标题容器 */
  .title-container {
    display: flex;
    justify-content: center;
    align-items: center;
    height: 60px;
    padding-top: 16px;
  }

  /* 发布商品标题样式 */
  .product-title {
    font-size: 1.8rem !important;
    font-weight: 500;
    width: 100%;
    text-align: center;
    padding: 0 !important;
    margin: 0 !important;
  }

  /* 图标和标题 */
  .icon svg {
    width: 48px;
    height: 48px;
  }

  .title {
    font-size: 32px;
    font-weight: bold;
    margin-left: 16px;
    color: white !important;
  }

  .select-image-btn {
    border: 2px dashed #1e88e5 !important;
    background-color: #cadefc !important;
    color: #1e88e5 !important;
    font-weight: bold;
    text-transform: none;
  }

  .select-image-btn:hover {
    background-color: #bbdefb !important;
    border-style: solid !important;
  }

  .submit-btn-class {
    background-color: #cadefc !important;
    color: white !important;
    font-weight: bold !important;
    border-radius: 8px !important;
    box-shadow: 0 3px 5px rgba(76, 175, 80, 0.3) !important;
    transition: all 0.2s ease !important;
    margin-bottom: 20px;
  }

  /* 分割线样式 */
  .v-divider {
    margin-top: 0 !important;
    margin-bottom: 8px !important;
  }

  .custom-divider {
    border-color: #fb7c7c !important;
    border-width: 2px !important;
  }

  .custom-vertical-divider {
    border-color: #1f1b1b !important;
    border-width: 1px !important;
    margin-right: 16px !important;
    height: auto !important;
    align-self: stretch !important;
  }

  /* 右侧输入框统一边框样式 */
  .right-input-border :deep(.v-field) {
    border: 1px solid #cadefc !important;
    border-radius: 8px !important;
    transition: all 0.3s ease;
  }

  /* 聚焦状态 */
  .right-input-border :deep(.v-field--focused) {
    border-color: #1e88e5 !important;
    box-shadow: 0 0 0 3px rgba(30, 136, 229, 0.2) !important;
  }

  /* 悬停效果 */
  .right-input-border :deep(.v-field:hover) {
    border-color: #90caf9 !important;
  }

  /* 图片上传卡片边框样式 */
  .v-card--outlined {
    border-color: rgba(0, 0, 0, 0.12) !important;
  }

  /* 图片网格 */
  .image-grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    grid-template-rows: repeat(2, 1fr);
    gap: 12px;
    background-color: transparent;
  }

  .image-item {
    position: relative;
    aspect-ratio: 1/1;
    background-color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    overflow: hidden;
  }

  /* 预览图片样式 */
  .preview-image {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  /* 虚线边框样式 */
  .dashed-border {
    border: 2px dashed #c4c4c4;
    border-radius: 4px;
  }

  .empty {
    background-color: #f9f9f9;
  }

  .delete-btn {
    position: absolute;
    top: 4px;
    right: 4px;
    z-index: 2;
  }

  /* 用户联系方式样式 */
  .user-contact-info {
    background-color: #f5f5f5;
    padding: 12px;
    border-radius: 8px;
    border: 1px solid #e0e0e0;
  }

  .contact-details div {
    margin-bottom: 8px;
    display: flex;
    align-items: center;
  }

  .contact-details div:last-child {
    margin-bottom: 0;
  }

  /* 响应式设计 */
  @media (max-width: 960px) {
    .content-wrapper {
      margin-top: 60px;
    }

    .content-card {
      min-height: calc(100vh - 60px);
    }

    .form-content {
      padding-bottom: 24px;
    }

    .custom-vertical-divider {
      display: none;
    }

    .pa-6 {
      padding: 16px !important;
    }
  }

  /* 移动端优化 */
  @media (max-width: 600px) {
    .title {
      font-size: 24px;
    }

    .product-title {
      font-size: 1.5rem !important;
    }

    .image-grid-container {
      height: 300px;
    }

    .submit-btn-class {
      width: 100%;
      margin-bottom: 16px;
    }

    .content-row {
      min-height: auto;
    }
  }

  /* 确保Vuetify组件可以正常滚动 */
  :deep(.v-card__body) {
    overflow: visible !important;
  }

  :deep(.v-col) {
    overflow: visible !important;
  }

  :deep(.v-row) {
    overflow: visible !important;
  }

  /* 右侧标题样式调整 */
  .text-subtitle-2 {
    font-size: 0.875rem !important;
    font-weight: 500;
  }

  /* 右侧输入框间距调整 */
  :deep(.v-input) {
    margin-top: 0;
  }
</style>
