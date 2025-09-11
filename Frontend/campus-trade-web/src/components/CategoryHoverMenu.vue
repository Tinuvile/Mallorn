<template>
  <div class="category-hover-menu" :style="{ width: menuWidth + 'px' }">
    <!-- 左侧一级分类列表 -->
    <div class="category-main">
      <div class="category-title">
        <v-icon size="18" class="mr-2">mdi-format-list-bulleted</v-icon>
        <span>商品分类</span>
      </div>

      <!-- 分类加载中 -->
      <div v-if="categoriesLoading" class="loading-container">
        <v-progress-circular indeterminate color="primary" size="20"></v-progress-circular>
        <span class="loading-text">加载中...</span>
      </div>

      <!-- 一级分类列表 -->
      <div v-else class="category-list" @mouseleave="onCategoryListLeave">
        <div
          v-for="(category, index) in rootCategories"
          :key="category.category_id"
          class="category-item"
          :class="{
            'category-item--active': hoveredCategory?.category_id === category.category_id,
            'category-item--selected': selectedCategory?.category_id === category.category_id,
          }"
          @mouseenter="onCategoryHover(category)"
          @click="onCategoryClick(category)"
        >
          <div class="category-item-content">
            <span class="category-name">{{ category.name }}</span>
            <v-icon
              v-if="category.children && category.children.length > 0"
              size="16"
              class="arrow-icon"
              >mdi-chevron-right</v-icon
            >
          </div>
        </div>
      </div>
    </div>

    <!-- 右侧悬停展示区域 -->
    <transition name="slide-fade">
      <div
        v-if="hoveredCategory && hoveredCategory.children && hoveredCategory.children.length > 0"
        class="category-dropdown"
        @mouseenter="onDropdownEnter"
        @mouseleave="onDropdownLeave"
      >
        <div class="dropdown-header">
          <span class="dropdown-title">{{ hoveredCategory.name }}</span>
          <span class="dropdown-count">({{ getTotalProductCount(hoveredCategory) }}件商品)</span>
        </div>

        <!-- 二级分类展示 -->
        <div class="subcategory-grid">
          <div
            v-for="subCategory in hoveredCategory.children"
            :key="subCategory.category_id"
            class="subcategory-group"
          >
            <div class="subcategory-title" @click="onCategoryClick(subCategory)">
              {{ subCategory.name }}
              <span class="product-count">({{ subCategory.active_product_count }})</span>
            </div>

            <!-- 三级标签 -->
            <div v-if="subCategory.children && subCategory.children.length > 0" class="tag-list">
              <v-chip
                v-for="tag in subCategory.children"
                :key="tag.category_id"
                size="small"
                variant="outlined"
                class="tag-chip"
                @click="onCategoryClick(tag)"
              >
                {{ tag.name }}
              </v-chip>
            </div>
          </div>
        </div>

        <!-- 查看全部链接 -->
        <div class="dropdown-footer">
          <v-btn
            variant="text"
            color="primary"
            size="small"
            @click="onCategoryClick(hoveredCategory)"
          >
            查看 {{ hoveredCategory.name }} 全部商品
            <v-icon size="16" class="ml-1">mdi-arrow-right</v-icon>
          </v-btn>
        </div>
      </div>
    </transition>
  </div>
</template>

<script setup lang="ts">
  import { ref, computed, onMounted, onUnmounted } from 'vue'
  import { useRouter } from 'vue-router'
  import { categoryApi, type CategoryItem } from '@/services/api'

  // Props
  interface Props {
    menuWidth?: number
  }

  const props = withDefaults(defineProps<Props>(), {
    menuWidth: 200,
  })

  // Emits
  const emit = defineEmits<{
    categorySelect: [category: CategoryItem]
  }>()

  // Router
  const router = useRouter()

  // Data
  const categoriesLoading = ref(false)
  const rootCategories = ref<CategoryItem[]>([])
  const hoveredCategory = ref<CategoryItem | null>(null)
  const selectedCategory = ref<CategoryItem | null>(null)
  const dropdownHovered = ref(false)
  const hoverTimer = ref<ReturnType<typeof setTimeout> | null>(null)

  // Computed
  const getTotalProductCount = (category: CategoryItem): number => {
    let total = category.active_product_count || 0
    if (category.children) {
      total += category.children.reduce((sum, child) => sum + getTotalProductCount(child), 0)
    }
    return total
  }

  // Methods
  const loadCategories = async () => {
    categoriesLoading.value = true
    try {
      const response = await categoryApi.getCategoryTree()
      if (response.success && response.data) {
        // 过滤掉重复的分类，只保留有意义的一级分类
        const allCategories = response.data.root_categories || []

        // 优先选择有子分类的分类，过滤掉重复项
        const meaningfulCategories = allCategories.filter(
          cat => cat.children && cat.children.length > 0
        )

        // 如果没有有子分类的，就使用前6个分类
        if (meaningfulCategories.length === 0) {
          rootCategories.value = allCategories.slice(0, 6)
        } else {
          rootCategories.value = meaningfulCategories
        }
      } else {
        // 使用默认分类
        rootCategories.value = getDefaultCategories()
      }
    } catch (error) {
      console.error('加载分类失败:', error)
      rootCategories.value = getDefaultCategories()
    } finally {
      categoriesLoading.value = false
    }
  }

  const getDefaultCategories = (): CategoryItem[] => [
    {
      category_id: 1,
      name: '教材',
      level: 1,
      full_path: '教材',
      product_count: 0,
      active_product_count: 0,
      children: [],
    },
    {
      category_id: 2,
      name: '数码',
      level: 1,
      full_path: '数码',
      product_count: 0,
      active_product_count: 0,
      children: [],
    },
    {
      category_id: 3,
      name: '日用',
      level: 1,
      full_path: '日用',
      product_count: 0,
      active_product_count: 0,
      children: [],
    },
  ]

  const onCategoryHover = (category: CategoryItem) => {
    // 立即清除任何延迟隐藏的定时器
    if (hoverTimer.value) {
      clearTimeout(hoverTimer.value)
      hoverTimer.value = null
    }

    // 如果是同一个分类，直接显示，无需延迟
    if (hoveredCategory.value?.category_id === category.category_id) {
      return
    }

    // 延迟显示悬停内容，避免鼠标快速移动时频繁切换
    hoverTimer.value = setTimeout(() => {
      hoveredCategory.value = category
    }, 80) // 稍微缩短延迟时间
  }

  const onCategoryListLeave = () => {
    // 清除悬停定时器
    if (hoverTimer.value) {
      clearTimeout(hoverTimer.value)
      hoverTimer.value = null
    }

    // 延迟隐藏，给用户时间移动到下拉区域
    hoverTimer.value = setTimeout(() => {
      if (!dropdownHovered.value) {
        hoveredCategory.value = null
      }
    }, 150)
  }

  const onDropdownEnter = () => {
    // 清除任何延迟隐藏的定时器
    if (hoverTimer.value) {
      clearTimeout(hoverTimer.value)
      hoverTimer.value = null
    }
    dropdownHovered.value = true
  }

  const onDropdownLeave = () => {
    dropdownHovered.value = false

    // 延迟隐藏下拉菜单，给用户时间移动回分类列表
    hoverTimer.value = setTimeout(() => {
      if (!dropdownHovered.value) {
        hoveredCategory.value = null
      }
    }, 100)
  }

  const onCategoryClick = (category: CategoryItem) => {
    selectedCategory.value = category
    emit('categorySelect', category)

    // 跳转到商品列表页面
    router.push({
      name: 'products',
      query: { categoryId: category.category_id.toString() },
    })
  }

  // Lifecycle
  onMounted(() => {
    loadCategories()
  })

  onUnmounted(() => {
    if (hoverTimer.value) {
      clearTimeout(hoverTimer.value)
    }
  })
</script>

<style scoped>
  .category-hover-menu {
    position: relative;
    background: white;
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    overflow: visible;
  }

  .category-main {
    position: relative;
    z-index: 2;
  }

  .category-title {
    display: flex;
    align-items: center;
    padding: 12px 16px;
    background: #f8f9fa;
    font-weight: 600;
    color: #333;
    border-bottom: 1px solid #eee;
    border-radius: 8px 8px 0 0;
  }

  .loading-container {
    display: flex;
    align-items: center;
    padding: 20px 16px;
    justify-content: center;
    gap: 8px;
  }

  .loading-text {
    font-size: 14px;
    color: #666;
  }

  .category-list {
    padding: 8px 0;
  }

  .category-item {
    position: relative;
    cursor: pointer;
    transition: all 0.2s ease;
  }

  .category-item-content {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 10px 16px;
    font-size: 14px;
    color: #333;
  }

  .category-item:hover {
    background: #f0f7ff;
  }

  .category-item--active {
    background: #e3f2fd;
    border-right: 3px solid #2196f3;
  }

  .category-name {
    flex: 1;
  }

  .arrow-icon {
    color: #999;
  }

  /* 右侧下拉菜单 */
  .category-dropdown {
    position: absolute;
    left: 100%;
    top: 0;
    min-width: 400px;
    max-width: 600px;
    background: white;
    border-radius: 0 8px 8px 0;
    box-shadow: 2px 0 8px rgba(0, 0, 0, 0.15);
    z-index: 3;
    border-left: 1px solid #eee;
  }

  .dropdown-header {
    padding: 16px 20px;
    border-bottom: 1px solid #eee;
    background: #fafafa;
  }

  .dropdown-title {
    font-weight: 600;
    font-size: 16px;
    color: #333;
  }

  .dropdown-count {
    font-size: 12px;
    color: #666;
    margin-left: 8px;
  }

  .subcategory-grid {
    padding: 16px 20px;
    max-height: 400px;
    overflow-y: auto;
  }

  .subcategory-group {
    margin-bottom: 16px;
  }

  .subcategory-title {
    font-weight: 500;
    color: #333;
    margin-bottom: 8px;
    cursor: pointer;
    padding: 4px 0;
    transition: color 0.2s ease;
  }

  .subcategory-title:hover {
    color: #2196f3;
  }

  .product-count {
    font-size: 12px;
    color: #999;
    font-weight: normal;
  }

  .tag-list {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
  }

  .tag-chip {
    font-size: 12px !important;
    height: 24px !important;
    cursor: pointer;
  }

  .tag-chip:hover {
    border-color: #2196f3;
    color: #2196f3;
  }

  .dropdown-footer {
    padding: 12px 20px;
    border-top: 1px solid #eee;
    background: #fafafa;
  }

  /* 动画效果 */
  .slide-fade-enter-active,
  .slide-fade-leave-active {
    transition: all 0.3s ease;
  }

  .slide-fade-enter-from {
    opacity: 0;
    transform: translateX(-10px);
  }

  .slide-fade-leave-to {
    opacity: 0;
    transform: translateX(-10px);
  }
</style>
