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
      <v-btn icon @click="goToMessage" class="mx-2" style="position: relative;">
        <v-icon size="40">mdi-view-list</v-icon>
        <!-- 未读消息数量显示 -->
        <v-badge
          v-if="unreadMessageCount > 0"
          :content="unreadMessageCount > 99 ? '99+' : unreadMessageCount.toString()"
          color="red"
          location="top end"
          offset-x="4"
          offset-y="4"
        >
        </v-badge>
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
      
      <!-- 管理员导航按钮 -->
      <v-btn
        v-if="isAdmin"
        class="mx-2"
        to="/admin"
        height="40"
        variant="text"
        color="orange"
        prepend-icon="mdi-shield-account"
      >
        管理员中心
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
    <v-main style="margin-top: 10px">
      <div class="d-flex flex-row">
        <!-- 分类菜单 -->
        <div style="margin: 40px 40px 20px 20px">
          <CategoryHoverMenu :menu-width="240" @category-select="onCategorySelect" />
        </div>
        <!-- 滚动图片展示 -->
        <v-carousel
          style="height: 500px; width: 60%; margin: 40px auto 20px; overflow: hidden"
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
            margin: 40px 20px 20px 40px;
            border-radius: 10px;
            background: linear-gradient(to bottom, #fceeee, #f8bbd0);
          "
          v-if="isLoggedIn"
        >
          <div class="text-center mb-4">
            <v-avatar size="50" class="mb-2">
              <v-icon size="50">mdi-account-circle</v-icon>
            </v-avatar>
          </div>
          <v-card-title class="text-center mt-2">{{ displayName }}</v-card-title>
          <v-divider class="my-4"></v-divider>

          <!-- 加载状态 -->
          <div v-if="userProfileLoading" class="text-center pa-4">
            <v-progress-circular indeterminate color="primary" size="30"></v-progress-circular>
            <div class="mt-2 text-body-2">加载用户信息...</div>
          </div>

          <!-- 用户信息列表 -->
          <v-list v-else style="background-color: transparent">
            <v-list-item v-if="userStore.user?.phone">
              <v-list-item-icon>
                <v-icon>mdi-phone</v-icon>
              </v-list-item-icon>
              <v-list-item-content>
                <v-list-item-title>{{ userStore.user.phone }}</v-list-item-title>
              </v-list-item-content>
            </v-list-item>

            <v-list-item>
              <v-list-item-icon>
                <v-icon>mdi-email</v-icon>
              </v-list-item-icon>
              <v-list-item-content>
                <v-list-item-title>{{ userStore.user?.email || 'N/A' }}</v-list-item-title>
              </v-list-item-content>
            </v-list-item>

            <v-list-item>
              <v-list-item-icon>
                <v-icon>mdi-school</v-icon>
              </v-list-item-icon>
              <v-list-item-content>
                <v-list-item-title>{{ userStore.user?.studentId || 'N/A' }}</v-list-item-title>
              </v-list-item-content>
            </v-list-item>

            <v-list-item>
              <v-list-item-icon>
                <v-icon>mdi-star</v-icon>
              </v-list-item-icon>
              <v-list-item-content>
                <v-list-item-title
                  >信用分: {{ userStore.user?.creditScore || 0 }}</v-list-item-title
                >
              </v-list-item-content>
            </v-list-item>

            <v-list-item v-if="userStore.user?.virtualAccount?.balance !== undefined">
              <v-list-item-icon>
                <v-icon>mdi-wallet</v-icon>
              </v-list-item-icon>
              <v-list-item-content>
                <v-list-item-title
                  >余额: ¥{{ userStore.user.virtualAccount.balance }}</v-list-item-title
                >
              </v-list-item-content>
            </v-list-item>
          </v-list>

          <!-- 充值按钮 -->
          <div class="text-center mt-4">
            <v-btn
              color="primary"
              variant="outlined"
              @click="goToRecharge"
              prepend-icon="mdi-plus-circle"
              class="recharge-btn"
            >
              账户充值
            </v-btn>
          </div>
        </v-card>

        <!-- 未登录提示卡片 -->
        <v-card
          class="pa-4"
          width="20%"
          style="
            margin: 40px 20px 20px 20px;
            border-radius: 10px;
            background: linear-gradient(to bottom, #e3f2fd, #bbdefb);
          "
          v-else
        >
          <div class="text-center">
            <v-icon size="80" color="grey">mdi-account-off</v-icon>
            <v-card-title class="text-center mt-4">未登录</v-card-title>
            <v-divider class="my-4"></v-divider>
            <p class="text-body-2 text-center">登录后查看个人信息</p>
            <v-btn color="primary" variant="outlined" @click="goToLogin" class="mt-2">
              立即登录
            </v-btn>
          </div>
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
  import { ref, computed, onMounted, watch } from 'vue'
  import { useRouter } from 'vue-router'
  import { useUserStore } from '@/stores/user'
  import CategoryHoverMenu from '@/components/CategoryHoverMenu.vue'
  import { productApi, notificationApi } from '@/services/api'

  const router = useRouter()
  const userStore = useUserStore()
  const showAuthWarning = ref(false)
  const authWarningMessage = ref('')
  const isLoading = ref(false)
  const userProfileLoading = ref(false)
  const isLoggedIn = computed(() => userStore.isLoggedIn) // 添加登录状态
  const isAdmin = computed(() => userStore.isAdmin) // 添加管理员状态
  const unreadMessageCount = ref(0) // 未读消息数量

  // 计算显示名称
  const displayName = computed(() => {
    const user = userStore.user
    if (!user) return '未知用户'

    // 优先级：fullName > student.name > username > '未知用户'
    return user.fullName || user.student?.name || user.username || '未知用户'
  })

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

  // 跳转到充值页面
  const goToRecharge = () => {
    if (!userStore.isLoggedIn) {
      showAuthWarning.value = true
      authWarningMessage.value = '请先登录后再进行充值'
      return
    }
    router.push('/recharge')
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

  // 加载用户详细信息
  const loadUserProfile = async () => {
    if (!userStore.isLoggedIn || userStore.isLoadingProfile) {
      return
    }

    userProfileLoading.value = true
    try {
      const result = await userStore.getUserProfile()
      if (!result.success) {
        console.warn('获取用户详细信息失败:', result.message)
        showAuthWarning.value = true
        authWarningMessage.value = result.message || '获取用户信息失败'
      } else {
        // 用户信息加载成功后，检查管理员身份
        await userStore.checkAdminStatus()
        // 加载未读消息数量
        await loadUnreadMessageCount()
      }
    } catch (error) {
      console.error('加载用户信息异常:', error)
      showAuthWarning.value = true
      authWarningMessage.value = '加载用户信息失败，请稍后重试'
    } finally {
      userProfileLoading.value = false
    }
  }

  // 加载未读消息数量
  const loadUnreadMessageCount = async () => {
    if (!userStore.isLoggedIn || !userStore.user?.userId) {
      unreadMessageCount.value = 0
      return
    }

    try {
      const response = await notificationApi.getUnreadMessageCount(userStore.user.userId)
      if (response.success && response.data) {
        unreadMessageCount.value = response.data.unreadCount
      } else {
        unreadMessageCount.value = 0
      }
    } catch (error) {
      console.error('获取未读消息数量失败:', error)
      unreadMessageCount.value = 0
    }
  }

  // 组件挂载时加载数据
  onMounted(async () => {
    // 并行加载用户信息和商品数据
    await Promise.all([loadUserProfile(), loadAllProducts()])
  })

  // 监听登录状态变化，自动刷新用户信息
  watch(isLoggedIn, (newValue, oldValue) => {
    if (newValue && !oldValue) {
      // 仅在从未登录变为已登录时才加载用户信息
      console.log('登录状态变化，加载用户信息')
      loadUserProfile()
    } else if (!newValue && oldValue) {
      // 从已登录变为未登录时，清零未读消息数量
      unreadMessageCount.value = 0
    }
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

  /* 充值按钮样式 */
  .recharge-btn {
    font-weight: 500 !important;
    border-radius: 20px !important;
    transition: all 0.3s ease !important;
    min-width: 120px;
  }

  .recharge-btn:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(25, 118, 210, 0.3) !important;
  }
</style>
