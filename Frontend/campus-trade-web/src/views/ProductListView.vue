<template>
  <v-app>
    <!-- 导航栏 -->
    <v-app-bar color="#BBDEFB" height="72" dark>
      <span class="title" style="font-size: 24px; margin-left: 30px; margin-right: 30px">
        Campus Secondhand
      </span>

      <!-- 搜索框 -->
      <v-text-field
        v-model="searchKeyword"
        :loading="searchLoading"
        append-inner-icon="mdi-magnify"
        density="compact"
        label="搜索商品..."
        variant="solo"
        hide-details
        single-line
        class="mx-4"
        style="max-width: 400px"
        @keydown.enter="handleSearch"
        @click:append-inner="handleSearch"
      ></v-text-field>

      <v-spacer></v-spacer>

      <!-- 右侧用户区域 -->
      <div class="d-flex align-center">
        <v-btn icon @click="goToHome" class="mx-2">
          <v-icon>mdi-home</v-icon>
        </v-btn>

        <v-btn icon @click="goToUserDetail" class="mx-2" v-if="userStore.isLoggedIn">
          <v-icon>mdi-account</v-icon>
        </v-btn>

        <v-btn variant="outlined" color="white" @click="goToLogin" v-else> 登录 </v-btn>
      </div>
    </v-app-bar>

    <v-main class="bg-grey-lighten-3">
      <v-container fluid class="py-4">
        <v-row>
          <!-- 左侧筛选区域 -->
          <v-col cols="12" md="3">
            <v-card class="mb-4">
              <v-card-title class="py-3">
                <v-icon class="mr-2">mdi-filter</v-icon>
                筛选条件
              </v-card-title>
              <v-divider></v-divider>

              <!-- 分类筛选 -->
              <v-card-text>
                <div class="mb-4">
                  <h4 class="mb-2">商品分类</h4>
                  <v-chip
                    :color="!selectedCategoryId ? 'primary' : 'default'"
                    :variant="!selectedCategoryId ? 'flat' : 'outlined'"
                    class="mr-2 mb-2"
                    @click="selectCategory(null)"
                  >
                    全部分类
                  </v-chip>
                  <div v-for="category in rootCategories" :key="category.category_id">
                    <v-chip
                      :color="selectedCategoryId === category.category_id ? 'primary' : 'default'"
                      :variant="selectedCategoryId === category.category_id ? 'flat' : 'outlined'"
                      class="mr-2 mb-2"
                      @click="selectCategory(category.category_id)"
                    >
                      {{ category.name }} ({{ category.product_count }})
                    </v-chip>
                  </div>
                </div>

                <!-- 价格筛选 -->
                <div class="mb-4">
                  <h4 class="mb-2">价格范围</h4>
                  <v-row>
                    <v-col cols="6">
                      <v-text-field
                        v-model="filters.minPrice"
                        type="number"
                        density="compact"
                        variant="outlined"
                        label="最低价"
                        prefix="¥"
                        @change="applyFilters"
                      ></v-text-field>
                    </v-col>
                    <v-col cols="6">
                      <v-text-field
                        v-model="filters.maxPrice"
                        type="number"
                        density="compact"
                        variant="outlined"
                        label="最高价"
                        prefix="¥"
                        @change="applyFilters"
                      ></v-text-field>
                    </v-col>
                  </v-row>
                </div>

                <!-- 商品状态筛选 -->
                <div class="mb-4">
                  <h4 class="mb-2">商品状态</h4>
                  <v-select
                    v-model="filters.status"
                    :items="statusOptions"
                    density="compact"
                    variant="outlined"
                    label="选择状态"
                    clearable
                    @update:model-value="applyFilters"
                  ></v-select>
                </div>

                <!-- 清空筛选 -->
                <v-btn variant="outlined" color="grey" block @click="clearFilters">
                  清空筛选
                </v-btn>
              </v-card-text>
            </v-card>
          </v-col>

          <!-- 右侧商品列表区域 -->
          <v-col cols="12" md="9">
            <!-- 面包屑导航 -->
            <v-breadcrumbs v-if="breadcrumbs.length > 0" :items="breadcrumbs" class="pa-0 mb-3">
              <template v-slot:item="{ item }">
                <v-breadcrumbs-item
                  :disabled="item.disabled"
                  @click="!item.disabled && selectCategory(item.categoryId)"
                >
                  {{ item.title }}
                </v-breadcrumbs-item>
              </template>
            </v-breadcrumbs>

            <!-- 结果统计和排序 -->
            <v-card class="mb-4">
              <v-card-text class="py-3">
                <v-row align="center">
                  <v-col>
                    <div class="text-body-1">
                      共找到
                      <span class="font-weight-bold text-primary">{{ totalCount }}</span> 件商品
                      <span v-if="searchKeyword" class="text-grey">
                        关键词："{{ searchKeyword }}"
                      </span>
                    </div>
                  </v-col>
                  <v-col cols="auto">
                    <v-select
                      v-model="sortBy"
                      :items="sortOptions"
                      density="compact"
                      variant="outlined"
                      label="排序方式"
                      style="width: 160px"
                      @update:model-value="handleSortChange"
                    ></v-select>
                  </v-col>
                </v-row>
              </v-card-text>
            </v-card>

            <!-- 商品网格 -->
            <div v-if="loading && products.length === 0" class="text-center py-8">
              <v-progress-circular indeterminate color="primary" size="40"></v-progress-circular>
              <div class="mt-3">加载中...</div>
            </div>

            <div v-else-if="products.length === 0" class="text-center py-8">
              <v-icon size="64" color="grey">mdi-package-variant</v-icon>
              <div class="text-h6 mt-3 text-grey">暂无商品</div>
              <div class="text-body-2 text-grey">试试调整筛选条件或搜索其他关键词</div>
            </div>

            <v-row v-else>
              <v-col v-for="product in products" :key="product.id" cols="12" sm="6" lg="4" xl="3">
                <v-card class="product-card" elevation="2" @click="goToProductDetail(product.id)">
                  <div class="position-relative">
                    <v-img
                      :src="product.primaryImage || '/images/placeholder.jpg'"
                      :alt="product.title"
                      height="200"
                      cover
                    >
                      <template v-slot:placeholder>
                        <div class="d-flex align-center justify-center fill-height">
                          <v-progress-circular indeterminate color="grey"></v-progress-circular>
                        </div>
                      </template>
                    </v-img>

                    <!-- 商品状态标签 -->
                    <v-chip
                      v-if="product.status !== '在售'"
                      class="position-absolute"
                      style="top: 8px; right: 8px"
                      size="small"
                      :color="getStatusColor(product.status)"
                    >
                      {{ product.status }}
                    </v-chip>
                  </div>

                  <v-card-text class="pb-2">
                    <div class="text-body-1 font-weight-medium mb-1 text-truncate">
                      {{ product.title }}
                    </div>

                    <div class="d-flex align-center justify-space-between mb-2">
                      <div class="text-h6 text-error font-weight-bold">
                        ¥{{ product.price.toFixed(2) }}
                      </div>
                      <div
                        v-if="product.originalPrice && product.originalPrice > product.price"
                        class="text-body-2 text-decoration-line-through text-grey"
                      >
                        ¥{{ product.originalPrice.toFixed(2) }}
                      </div>
                    </div>

                    <div class="text-body-2 text-grey mb-1">分类: {{ product.categoryName }}</div>

                    <div class="d-flex align-center justify-space-between">
                      <div class="text-body-2 text-grey">卖家: {{ product.sellerName }}</div>
                      <div class="text-body-2 text-grey">
                        <v-icon size="small" class="mr-1">mdi-eye</v-icon>
                        {{ product.viewCount }}
                      </div>
                    </div>
                  </v-card-text>
                </v-card>
              </v-col>
            </v-row>

            <!-- 分页 -->
            <div v-if="totalPages > 1" class="d-flex justify-center mt-6">
              <v-pagination
                v-model="currentPage"
                :length="totalPages"
                :total-visible="7"
                @update:model-value="handlePageChange"
              ></v-pagination>
            </div>
          </v-col>
        </v-row>
      </v-container>
    </v-main>
  </v-app>
</template>

<script setup>
  import { ref, reactive, computed, onMounted, watch } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import { useUserStore } from '@/stores/user'
  import { productApi, categoryApi } from '@/services/api'

  const route = useRoute()
  const router = useRouter()
  const userStore = useUserStore()

  // 响应式数据
  const loading = ref(false)
  const searchLoading = ref(false)
  const searchKeyword = ref('')
  const products = ref([])
  const totalCount = ref(0)
  const currentPage = ref(1)
  const pageSize = ref(20)
  const selectedCategoryId = ref(null)
  const rootCategories = ref([])
  const breadcrumbs = ref([])
  const sortBy = ref('publishTime_desc')

  // 筛选条件
  const filters = reactive({
    minPrice: null,
    maxPrice: null,
    status: '在售',
  })

  // 计算属性
  const totalPages = computed(() => {
    return Math.ceil(totalCount.value / pageSize.value)
  })

  // 排序选项
  const sortOptions = [
    { title: '最新发布', value: 'publishTime_desc' },
    { title: '价格从低到高', value: 'price_asc' },
    { title: '价格从高到低', value: 'price_desc' },
    { title: '浏览量最高', value: 'viewCount_desc' },
  ]

  // 状态选项
  const statusOptions = [
    { title: '在售', value: '在售' },
    { title: '已下架', value: '已下架' },
    { title: '交易中', value: '交易中' },
  ]

  // 方法
  const loadProducts = async () => {
    loading.value = true
    try {
      const [sortField, sortOrder] = sortBy.value.split('_')

      const queryParams = {
        pageIndex: currentPage.value,
        pageSize: pageSize.value,
        categoryId: selectedCategoryId.value,
        minPrice: filters.minPrice ? parseFloat(filters.minPrice) : undefined,
        maxPrice: filters.maxPrice ? parseFloat(filters.maxPrice) : undefined,
        status: filters.status,
        sortBy: sortField,
        sortOrder: sortOrder,
      }

      console.log('商品查询参数:', queryParams)
      console.log('选中的分类ID:', selectedCategoryId.value)

      let response
      if (searchKeyword.value.trim()) {
        // 使用搜索接口
        response = await productApi.searchProducts({
          keyword: searchKeyword.value.trim(),
          pageIndex: currentPage.value,
          pageSize: pageSize.value,
          categoryId: selectedCategoryId.value,
        })
      } else if (selectedCategoryId.value) {
        // 检查是否为一级分类（有子分类的分类）
        const selectedCategory = findCategoryById(rootCategories.value, selectedCategoryId.value)
        const hasChildren =
          selectedCategory && selectedCategory.children && selectedCategory.children.length > 0

        if (hasChildren) {
          // 一级分类：使用包含子分类的查询接口
          console.log('使用分类查询接口，包含子分类')
          response = await productApi.getProductsByCategory(
            selectedCategoryId.value,
            currentPage.value - 1, // API使用0基索引
            pageSize.value,
            true // 包含子分类
          )
        } else {
          // 叶子分类：使用普通查询接口
          response = await productApi.getProducts(queryParams)
        }
      } else {
        // 使用普通查询接口
        response = await productApi.getProducts(queryParams)
      }

      console.log('API响应:', response)

      if (response.success && response.data) {
        // 处理不同API接口的数据格式差异
        let productsData = []
        let totalCountData = 0

        if (response.data.products) {
          // 通用的商品数据格式
          productsData = response.data.products
          totalCountData = response.data.total_count || response.data.totalCount || 0
        }

        console.log('处理的商品数据:', productsData)
        console.log('商品总数:', totalCountData)

        // 映射后端数据格式到前端显示格式
        products.value = (productsData || []).map(product => ({
          id: product.product_id,
          title: product.title,
          price: product.base_price,
          primaryImage: product.main_image_url,
          status: product.status,
          categoryName: product.category?.name || '未分类',
          sellerName: product.user?.username || '未知用户',
          viewCount: product.view_count || 0,
          createdAt: product.publish_time,
        }))
        totalCount.value = totalCountData
      } else {
        console.error('获取商品列表失败:', response.message)
        products.value = []
        totalCount.value = 0
      }
    } catch (error) {
      console.error('获取商品列表失败:', error)
      products.value = []
      totalCount.value = 0
    } finally {
      loading.value = false
    }
  }

  const loadCategories = async () => {
    try {
      const response = await categoryApi.getCategoryTree()
      console.log('分类数据响应:', response)
      if (response.success && response.data) {
        rootCategories.value = response.data.root_categories || []
        console.log('加载的分类数据:', rootCategories.value)
      }
    } catch (error) {
      console.error('获取分类失败:', error)
    }
  }

  const selectCategory = categoryId => {
    selectedCategoryId.value = categoryId
    currentPage.value = 1
    updateBreadcrumbs()
    updateUrlParams()
    loadProducts()
  }

  const updateBreadcrumbs = () => {
    breadcrumbs.value = [{ title: '全部商品', categoryId: null, disabled: false }]

    if (selectedCategoryId.value) {
      const category = findCategoryById(rootCategories.value, selectedCategoryId.value)
      if (category) {
        breadcrumbs.value.push({
          title: category.name,
          categoryId: category.category_id,
          disabled: true,
        })
      }
    }
  }

  const findCategoryById = (categories, categoryId) => {
    for (const category of categories) {
      if (category.category_id === categoryId) {
        return category
      }
      if (category.children && category.children.length > 0) {
        const found = findCategoryById(category.children, categoryId)
        if (found) return found
      }
    }
    return null
  }

  const handleSearch = () => {
    currentPage.value = 1
    updateUrlParams()
    loadProducts()
  }

  const handleSortChange = () => {
    currentPage.value = 1
    loadProducts()
  }

  const handlePageChange = page => {
    currentPage.value = page
    updateUrlParams()
    loadProducts()
    // 滚动到顶部
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  const applyFilters = () => {
    currentPage.value = 1
    updateUrlParams()
    loadProducts()
  }

  const clearFilters = () => {
    filters.minPrice = null
    filters.maxPrice = null
    filters.status = null
    selectedCategoryId.value = null
    searchKeyword.value = ''
    currentPage.value = 1
    updateBreadcrumbs()
    updateUrlParams()
    loadProducts()
  }

  const updateUrlParams = () => {
    const query = {}
    if (selectedCategoryId.value) query.categoryId = selectedCategoryId.value
    if (searchKeyword.value.trim()) query.keyword = searchKeyword.value.trim()
    if (currentPage.value > 1) query.page = currentPage.value
    if (filters.minPrice) query.minPrice = filters.minPrice
    if (filters.maxPrice) query.maxPrice = filters.maxPrice
    if (filters.status) query.status = filters.status

    router.replace({ query })
  }

  const initFromUrlParams = () => {
    if (route.query.categoryId) {
      selectedCategoryId.value = parseInt(route.query.categoryId)
    }
    if (route.query.keyword) {
      searchKeyword.value = route.query.keyword
    }
    if (route.query.page) {
      currentPage.value = parseInt(route.query.page)
    }
    if (route.query.minPrice) {
      filters.minPrice = parseFloat(route.query.minPrice)
    }
    if (route.query.maxPrice) {
      filters.maxPrice = parseFloat(route.query.maxPrice)
    }
    if (route.query.status) {
      filters.status = route.query.status
    }
  }

  const getStatusColor = status => {
    switch (status) {
      case '在售':
        return 'success'
      case '已下架':
        return 'grey'
      case '交易中':
        return 'warning'
      default:
        return 'default'
    }
  }

  const goToProductDetail = productId => {
    router.push(`/goods/${productId}`)
  }

  const goToHome = () => {
    router.push('/')
  }

  const goToUserDetail = () => {
    router.push('/userdetailview')
  }

  const goToLogin = () => {
    router.push('/login')
  }

  // 监听路由变化
  watch(
    () => route.query,
    () => {
      initFromUrlParams()
      updateBreadcrumbs()
      loadProducts()
    }
  )

  // 组件挂载
  onMounted(async () => {
    initFromUrlParams()
    await loadCategories()
    updateBreadcrumbs()
    await loadProducts()
  })
</script>

<style scoped>
  .product-card {
    cursor: pointer;
    transition: all 0.3s ease;
  }

  .product-card:hover {
    transform: translateY(-4px);
    box-shadow: 0 8px 16px rgba(0, 0, 0, 0.15) !important;
  }

  .position-absolute {
    position: absolute;
  }

  .text-truncate {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .v-card {
    border-radius: 12px;
  }

  .v-chip {
    cursor: pointer;
  }
</style>
