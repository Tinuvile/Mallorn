<template>
  <v-app id="inspire">
    <!-- 导航栏 -->
    <v-app-bar color="#BBDEFB" height="72" dark>
      <span class="title" style="font-size: 24px; margin-left: 30px; margin-right: 30px"
        >Campus Secondhand</span
      >
      <v-btn icon color="indigo" @click="goToUserDetail" class="mx-2">
        <v-icon size="40">mdi-account-circle</v-icon>
      </v-btn>
      <v-btn icon @click="goToMessage" class="mx-2">
        <v-icon size="40">mdi-view-list</v-icon>
      </v-btn>

      <v-btn
        class="me-2"
        color="black"
        height="40"
        variant="text"
        width="100"
        style="margin-left: 100px"
        @click="goToLogin"
        size="large"
        v-if="!isLoggedIn"
        >登录/注册</v-btn
      >

      <!-- 搜索框 -->
      <v-text-field
        :loading="loading"
        append-inner-icon="mdi-magnify"
        density="compact"
        label="Search"
        variant="solo"
        hide-details
        single-line
        @click:append-inner="onClick"
        style="margin-left: 100px"
      ></v-text-field>
      <v-btn color="primary" class="mx-2" to="/order" prepend-icon="mdi-file-document-outline">
        我的订单
      </v-btn>

      <v-btn
        class="submit-btn-class mx-2"
        size="large"
        to="/goodsreleaseview"
        variant="text"
        height="40"
      >
        <img src="/images/ReleaseProduct.png" alt="发布商品" class="btn-icon" />
        发布商品
      </v-btn>
      <v-btn
        class="mx-2"
        to="/datawatchingview"
        height="40"
        variant="text"
        style="min-width: 40px; padding: 0"
      >
        <v-img src="/images/DataAnalyze.png" alt="数据看板" width="30" height="30" contain />
      </v-btn>
      <v-spacer></v-spacer>
    </v-app-bar>
    <!-- 错误提示的snackbar -->
    <v-snackbar v-model="showAuthWarning" :timeout="3000" color="error" location="top">
      {{ authWarningMessage }}
      <template v-slot:actions>
        <v-btn variant="text" @click="showAuthWarning = false"> 关闭 </v-btn>
      </template>
    </v-snackbar>

    <v-divider></v-divider>
    <!-- 页面主体内容 -->
    <v-main style="margin-top: 30px">
      <div class="d-flex flex-row">
        <!-- 分类菜单 -->
        <div style="margin: 40px 20px 20px 20px">
          <CategoryHoverMenu :menu-width="240" @category-select="onCategorySelect" />
        </div>
        <!-- 滚动图片展示 -->
        <v-carousel
          style="height: 500px; width: 80%; margin: 40px auto 20px; overflow: hidden"
          interval="3000"
          show-arrows="hover"
          hide-delimiters
          rounded
          class="rounded-lg"
          contain
          cycle
        >
          <v-carousel-item v-for="(image, index) in carouselImages" :key="index">
            <img
              :src="image.url"
              :alt="image.title"
              style="width: 100%; height: 100%; object-fit: cover; border-radius: inherit"
            />
            >
            <div
              class="pa-6 text-center"
              style="
                position: absolute;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
                z-index: 1; /* 设置 z-index 确保文字显示在图片上方 */
                padding: 20px;
                border-radius: 10px;
              "
            >
              <h1 class="text-white">{{ image.title }}</h1>
              <p class="text-white">{{ image.subtitle }}</p>
            </div>
          </v-carousel-item>
        </v-carousel>

        <!-- 个人信息区块 -->
        <v-card
          class="pa-4"
          width="20%"
          style="
            margin: 40px 20px 20px 20px;
            border-radius: 10px;
            background: linear-gradient(to bottom, #fceeee, #f8bbd0);
          "
        >
          <v-avatar size="100" class="mx-auto">
            <img :src="userInfo.avatar" :alt="userInfo.name" />
          </v-avatar>
          <v-card-title class="text-center mt-4">{{ userInfo.name }}</v-card-title>
          <v-card-subtitle class="text-center">{{ userInfo.email }}</v-card-subtitle>
          <v-divider class="my-4"></v-divider>
          <v-list style="background-color: transparent">
            <v-list-item>
              <v-list-item-icon>
                <v-icon>mdi-phone</v-icon>
              </v-list-item-icon>
              <v-list-item-content>
                <v-list-item-title>{{ userInfo.phone }}</v-list-item-title>
              </v-list-item-content>
            </v-list-item>
            <v-list-item>
              <v-list-item-icon>
                <v-icon>mdi-account-group</v-icon>
              </v-list-item-icon>
              <v-list-item-content>
                <v-list-item-title>{{ userInfo.role }}</v-list-item-title>
              </v-list-item-content>
            </v-list-item>
          </v-list>
        </v-card>
      </div>

      <!-- 促销 -->
      <v-sheet
        class="mx-auto pa-2 pt-6"
        style="
          border: 3px solid #fceeee;
          border-radius: 10px;
          box-shadow: 0 40px 8px rgba(0, 0, 0, 0.1);
        "
      >
        <v-icon color="deep-purple">mdi-sale</v-icon>
        <p style="font-size: 40px; font-weight: bold; color: #e1bee7">促销</p>
        <div v-if="productsLoading" class="text-center pa-4">
          <v-progress-circular indeterminate color="primary" size="30"></v-progress-circular>
          <div class="mt-2 text-body-2">加载中...</div>
        </div>
        <v-slide-group v-else show-arrows>
          <v-slide-group-item v-for="(product, index) in saleProducts" :key="index">
            <v-card
              class="ma-3"
              height="200"
              width="250"
              rounded
              @click="goToProductDetail(product.id)"
              elevation="0"
            >
              <v-img
                class="product-img"
                :src="product.imageUrl"
                :alt="product.name"
                height="150"
                contain
                placeholder="加载中..."
              />
              <v-card-title class="text-center">
                <div class="product-name">{{ product.name }}</div>
                <div class="product-price" style="margin-top: -5px">¥{{ product.price }}</div>
              </v-card-title>
            </v-card>
          </v-slide-group-item>
        </v-slide-group>
      </v-sheet>

      <!-- 热销商品 -->
      <v-sheet class="mx-auto pa-2 pt-6">
        <v-icon color="red">mdi-fire</v-icon>
        <p style="font-size: 40px; font-weight: bold; color: #c2185b">热销中</p>

        <div v-if="productsLoading" class="text-center pa-4">
          <v-progress-circular indeterminate color="primary" size="30"></v-progress-circular>
          <div class="mt-2 text-body-2">加载中...</div>
        </div>
        <v-slide-group v-else show-arrows>
          <v-slide-group-item v-for="product in hotProducts" :key="product.id">
            <v-card class="ma-3" elevation="0" @click="goToProductDetail(product.id)">
              <v-img
                class="product-img"
                :width="product.id === 100 ? 300 : 200"
                :src="product.imageUrl"
                :alt="product.name"
                height="200"
                rounded
                contain
                loading="lazy"
              />
              <v-card-item>
                <v-card-title class="text-center pt-2">
                  <div class="product-name">{{ product.name }}</div>
                  <div class="product-price">¥{{ product.price }}</div>
                </v-card-title>
              </v-card-item>
            </v-card>
          </v-slide-group-item>
        </v-slide-group>

        <v-container fluid>
          <v-icon color="red">mdi-store</v-icon>
          <p style="font-size: 40px; font-weight: bold; color: rgb(167, 54, 54)">更多推荐</p>

          <div v-if="productsLoading" class="text-center pa-4">
            <v-progress-circular indeterminate color="primary" size="30"></v-progress-circular>
            <div class="mt-2 text-body-2">加载中...</div>
          </div>
          <v-row v-else>
            <v-col v-for="product in recommendedProducts" :key="product.id" cols="3">
              <v-card class="ma-1" elevation="0" @click="goToProductDetail(product.id)">
                <!-- 推荐商品区域 -->
                <v-card class="pa-2" elevation="0" @click="goToProductDetail(product.id)">
                  <v-img
                    class="product-img"
                    :src="product.imageUrl"
                    :alt="product.name"
                    height="200"
                    rounded
                    contain
                    loading="lazy"
                  />
                  <v-card-item>
                    <v-card-title class="text-center">
                      <div class="product-name">{{ product.name }}</div>
                      <div class="product-price">¥{{ product.price }}</div>
                    </v-card-title>
                  </v-card-item>
                </v-card>
              </v-card>
            </v-col>
          </v-row>
        </v-container>
      </v-sheet>
    </v-main>
  </v-app>
</template>

<script setup>
  import { ref, computed, onMounted } from 'vue'
  import { useRouter } from 'vue-router'
  import { useUserStore } from '@/stores/user'
  import CategoryHoverMenu from '@/components/CategoryHoverMenu.vue'
  import { productApi } from '@/services/api'

  const router = useRouter()
  const userStore = useUserStore()
  const showAuthWarning = ref(false)
  const authWarningMessage = ref('')
  const isLoading = ref(false)
  const isLoggedIn = computed(() => userStore.isLoggedIn) // 添加登录状态

  // 分类选择处理
  const onCategorySelect = category => {
    console.log('选中分类:', category)
    // 跳转到商品列表页面
    router.push({
      name: 'products',
      query: {
        categoryId: category.category_id.toString(),
        categoryName: category.name,
      },
    })
  }

  // 个人信息数据
  const userInfo = ref({
    avatar: 'https://picsum.photos/100/100?random=1',
    name: '张三',
    email: 'zhangsan@example.com',
    phone: '13800138000',
    role: '普通用户',
  })

  // 模拟购物车商品数量
  const cartItemsCount = ref(3)

  // 点击消息图标时的跳转方法
  const goToMessage = () => {
    router.push({ name: 'message' })
  }

  //点击用户图标的跳转
  const goToUserDetail = async () => {
    if (!userStore.isLoggedIn) {
      showAuthWarning.value = true
      authWarningMessage.value = '请先登录后再查看个人信息'
      return
    }

    isLoading.value = true
    try {
      // 使用 getUserProfile 方法获取当前用户的完整信息
      const result = await userStore.getUserProfile()

      if (result.success) {
        router.push('/userdetailview')
      } else {
        showAuthWarning.value = true
        authWarningMessage.value = result.message || '获取用户信息失败'
      }
    } catch (error) {
      console.error('获取用户信息失败:', error)
      showAuthWarning.value = true
      authWarningMessage.value = '请求失败，请重试'
    } finally {
      isLoading.value = false
    }
  }

  // 跳转到登录页面
  const goToLogin = () => {
    router.push('/login')
  }

  // 跳转到商品详情页
  const goToProductDetail = id => {
    router.push({
      path: `/goods/${id}`, // 路由路径格式：/goods/商品ID
      params: { id }, // 传递商品ID参数
    })
  }

  // 商品数据 - 从后端获取
  const saleProducts = ref([])
  const hotProducts = ref([])
  const recommendedProducts = ref([])
  const productsLoading = ref(false)

  // 轮播图数据
  const carouselImages = ref([
    {
      url: '/images/campus.png',
      title: '校园交易平台',
      subtitle: '开启便捷品质生活，品类齐全 现货热销',
    },
    {
      url: '/images/show.jpg',
      title: '实战必备穿搭优选',
      subtitle: '衣橱焕新精致指南，精选球鞋 火热开抢',
    },
    {
      url: '/images/show2.jpg',
      title: '崭新出售',
      subtitle: '极限竞速，极限挑战',
    },
  ])

  const loading = ref(false)

  // 组件挂载时加载数据
  onMounted(() => {
    loadAllProducts()
  })

  // 获取热门商品
  const loadHotProducts = async () => {
    try {
      const response = await productApi.getPopularProducts()
      if (response.success && response.data) {
        hotProducts.value = response.data.map(product => ({
          id: product.product_id,
          name: product.title,
          imageUrl: product.main_image_url || '/images/placeholder.jpg',
          price: product.base_price,
        }))
      }
    } catch (error) {
      console.error('获取热门商品失败:', error)
    }
  }

  // 获取促销商品 (按价格低到高排序的前10个)
  const loadSaleProducts = async () => {
    try {
      const response = await productApi.getProducts({
        pageIndex: 1,
        pageSize: 10,
        sortBy: 'price',
        sortOrder: 'asc',
        status: '在售',
      })
      if (response.success && response.data && response.data.products) {
        saleProducts.value = response.data.products.map(product => ({
          id: product.product_id,
          name: product.title,
          imageUrl: product.main_image_url || '/images/placeholder.jpg',
          price: product.base_price,
        }))
      }
    } catch (error) {
      console.error('获取促销商品失败:', error)
    }
  }

  // 获取推荐商品 (最新发布的商品)
  const loadRecommendedProducts = async () => {
    try {
      const response = await productApi.getProducts({
        pageIndex: 1,
        pageSize: 12,
        sortBy: 'publishTime',
        sortOrder: 'desc',
        status: '在售',
      })
      if (response.success && response.data && response.data.products) {
        recommendedProducts.value = response.data.products.map(product => ({
          id: product.product_id,
          name: product.title,
          imageUrl: product.main_image_url || '/images/placeholder.jpg',
          price: product.base_price,
        }))
      }
    } catch (error) {
      console.error('获取推荐商品失败:', error)
    }
  }

  // 加载所有商品数据
  const loadAllProducts = async () => {
    productsLoading.value = true
    try {
      await Promise.all([loadHotProducts(), loadSaleProducts(), loadRecommendedProducts()])
    } finally {
      productsLoading.value = false
    }
  }
</script>

<style scoped>
  .v-card:hover {
    box-shadow: var(--v-shadow-key-1) !important; /* 强制使用默认阴影值 */
  }

  /* 商品图片悬浮放大效果 */
  .product-img {
    transition: transform 0.3s ease;
  }

  .product-img:hover {
    transform: scale(1.05); /* 放大5% */
  }

  .product-name {
    color: black;
    font-size: 14px;
    font-weight: 500;
    margin-bottom: 4px;
  }

  /* 商品价格样式 */
  .product-price {
    color: rgb(167, 54, 54);
    font-size: 18px;
    font-weight: bold;
  }

  /* 新增按钮图标样式 */
  .btn-icon {
    width: 24px;
    height: 24px;
    margin-right: 8px;
  }

  /* 调整按钮样式以适配图标 */
  .submit-btn-class {
    display: flex;
    align-items: center;
    justify-content: center;
    color: #1976d2 !important; /* 使用与导航栏其他按钮相似的颜色 */
    font-weight: 500 !important;
    border-radius: 4px !important;
    transition: all 0.2s ease !important;
    padding: 0 16px !important;
  }

  /* 按钮悬停效果 */
  .submit-btn-class:hover {
    background-color: rgba(25, 118, 210, 0.08) !important;
  }
</style>
