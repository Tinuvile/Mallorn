<template>
  <v-app id="inspire">
    <!-- 导航栏 -->
    <v-app-bar color="#BBDEFB" height="72" dark>
      <span class="title" style="font-size: 24px; margin-left: 30px;margin-right: 30px;">Campus Secondhand</span>
      <v-btn icon color="indigo" @click="goToUserDetail" class="mx-2">
        <v-icon size="40">mdi-account-circle</v-icon>
      </v-btn>
      <v-btn icon @click="goToCart" class="mx-2">
        <v-icon size="40">mdi-cart</v-icon>
      </v-btn>

      <v-btn class="me-2" color="black" height="40" variant="text" width="100" style="margin-left: 100px;" @click="goToLogin"
        size="large" v-if="!isLoggedIn">登录/注册</v-btn>

      <!-- 搜索框 -->
      <v-text-field :loading="loading" append-inner-icon="mdi-magnify" density="compact" label="Search" variant="solo"
        hide-details single-line @click:append-inner="onClick" style="margin-left: 100px;"></v-text-field>
 <v-btn
    color="primary"
    class="mx-2"
    to="/order"
    prepend-icon="mdi-file-document-outline"
  >
    我的订单
  </v-btn>

      <v-spacer></v-spacer>
    </v-app-bar>
    <!-- 错误提示的snackbar -->
    <v-snackbar
      v-model="showAuthWarning"
      :timeout="3000"
      color="error"
      location="top"
    >
      {{ authWarningMessage }}
      <template v-slot:actions>
        <v-btn
          variant="text"
          @click="showAuthWarning = false"
        >
          关闭
        </v-btn>
      </template>
    </v-snackbar>

    <v-divider></v-divider>
    <!-- 页面主体内容 -->
    <v-main style="margin-top: 30px;" >

      <div class="d-flex flex-row">
      <!-- 分类列表 -->
        <v-card class="pa-0" width = "15%" style="background-color: #f5f5f5; border-radius: 5px; margin: 40px 20px 20px 20px; ">
        <v-list class="pa-4" style="margin-top: 20px;margin-left: -10px; background-color: transparent; ">
          <v-list-item
            v-for="(category, index) in categories"
            :key="index"
            link
            @click="selectCategory(category.id)"
          >
            <v-list-item-title>{{ category.name }}</v-list-item-title>
          </v-list-item>
        </v-list>
      </v-card>
      <!-- 滚动图片展示 -->
      <v-carousel 
      style="height: 500px;width: 80%; margin: 40px auto 20px; overflow: hidden;" 
       interval="3000"
        show-arrows="hover" hide-delimiters rounded class="rounded-lg" contain cycle>
        <v-carousel-item v-for="(image, index) in carouselImages" :key="index">
          <img :src="image.url" :alt="image.title" style="
            width: 100%;
            height: 100%;
            object-fit: cover;
            border-radius: inherit;
          " />
          >
          <div class="pa-6 text-center" style="
            position: absolute; 
            top: 50%; 
            left: 50%; 
            transform: translate(-50%, -50%);
            z-index: 1; /* 设置 z-index 确保文字显示在图片上方 */
            padding: 20px;
            border-radius: 10px;
          ">
            <h1 class="text-white">{{ image.title }}</h1>
            <p class="text-white">{{ image.subtitle }}</p>
          </div>
        </v-carousel-item>
      </v-carousel>

      <!-- 个人信息区块 -->
        <v-card class="pa-4" width="20%" style="margin: 40px 20px 20px 20px; border-radius: 10px;background:linear-gradient(to bottom,#fceeee,#F8BBD0)">
          <v-avatar size="100" class="mx-auto">
            <img :src="userInfo.avatar" :alt="userInfo.name" />
          </v-avatar>
          <v-card-title class="text-center mt-4">{{ userInfo.name }}</v-card-title>
          <v-card-subtitle class="text-center">{{ userInfo.email }}</v-card-subtitle>
          <v-divider class="my-4"></v-divider>
          <v-list style="background-color: transparent;">
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
      <v-sheet class="mx-auto pa-2 pt-6" style="border: 3px solid #fceeee; border-radius: 10px;box-shadow: 0 40px 8px rgba(0, 0, 0, 0.1);">
        <v-icon color="deep-purple">mdi-sale</v-icon>
        <p style="font-size: 40px; font-weight: bold; color: #E1BEE7;">促销</p>
        <v-slide-group show-arrows>
          <v-slide-group-item v-for="(product, index) in saleProducts" :key="index">
            <v-card class="ma-3" height="200" width="250" rounded @click="goToProductDetail(product.id)">
              <v-img :src="product.imageUrl" :alt="product.name" height="150" contain placeholder="加载中..." />
              <v-card-title class="text-center pt-2">{{ product.name }}</v-card-title>
            </v-card>
          </v-slide-group-item>
        </v-slide-group>
      </v-sheet>

      <!-- 热销商品 -->
      <v-sheet class="mx-auto pa-2 pt-6">
        <v-icon color="red">mdi-fire</v-icon>
        <p style="font-size: 40px;font-weight: bold; color: #C2185B;">热销中</p>

        <v-slide-group show-arrows>
          <v-slide-group-item v-for="product in hotProducts" :key="product.id">

            <v-card class="ma-3" elevation="0" @click="goToProductDetail(product.id)">
              <v-img :width="product.id === 1 ? 300 : 150"
                :src="'https://picsum.photos/'+(product.id === 1 ? '300/200' : '150/200')+'?random='+product.id" 
                :alt="product.name" 
                height="200" 
                rounded 
                contain 
                loading="lazy" 
              />
              <v-card-item>
                <v-card-title class="text-center">{{ product.name }}</v-card-title> 
              </v-card-item>
            </v-card>
          </v-slide-group-item>
        </v-slide-group>

        <v-container fluid>
          <v-row>
        <v-col v-for="product in recommendedProducts" :key="product.id" cols="2">
          <v-card class="ma-3" elevation="0" @click="goToProductDetail(product.id)">
            <v-img 
              :src="product.imageUrl" 
              :alt="product.name" 
              height="200" 
              rounded 
              contain 
              loading="lazy" 
            />
            <v-card-item>
              <v-card-title class="text-center">{{ product.name }}</v-card-title> 
            </v-card-item>
          </v-card>
        </v-col>
      </v-row>
        </v-container>
      </v-sheet>
    </v-main>
  </v-app>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'

const router = useRouter()
const userStore = useUserStore()
const showAuthWarning = ref(false)
const authWarningMessage = ref('')
const isLoading = ref(false)
const isLoggedIn = computed(() => userStore.isLoggedIn) // 添加登录状态


// 分类数据
const categories = ref([
  { id: 1, name: '电脑/配件/办公' },
  { id: 2, name: '工业品/商业/农业' },
  { id: 3, name: '家电/手机/通信' },
  { id: 4, name: '家具/家装/家居' },
  { id: 5, name: '食品/生鲜/酒类' },
  { id: 6, name: '女装/男装/内衣' },
  { id: 7, name: '美妆/个护/娱乐' },
  { id: 8, name: '运动/户外/交通' },
  { id: 9, name: '母婴/玩具/乐器' },
])

// 分类选择方法
const selectCategory = (categoryId) => {
  console.log('选中分类 ID:', categoryId)
  // 这里可以添加跳转或筛选逻辑
}

// 个人信息数据
const userInfo = ref({
  avatar: 'https://picsum.photos/100/100?random=1',
  name: '张三',
  email: 'zhangsan@example.com',
  phone: '13800138000',
  role: '普通用户'
})


// 模拟购物车商品数量
const cartItemsCount = ref(3)

// 点击购物车图标时的跳转方法(还需要根据实际路由调整)
const goToCart = () => {
  //router.push({ name: 'Cart' }) // 跳转到名为'Cart'的路由
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
    // 调用 Store 的 fetchUserInfo 方法获取最新用户数据
    const result = await userStore.fetchUserInfo(userStore.user?.username || '')
    
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
const goToProductDetail = (id) => {
  router.push({ 
    path: `/goods/${id}`,  // 路由路径格式：/goods/商品ID
    params: { id }         // 传递商品ID参数
  })
}

// 模拟促销商品数据 (ID范围: 1-9)
const saleProducts = ref([
  { id: 1, name: '商品1', imageUrl: '/images/shoe1.jpg' },
  { id: 2, name: '商品2', imageUrl: '/images/shoe2.jpg' },
  { id: 3, name: '商品3', imageUrl: '/images/shoe3.jpg' },
  { id: 4, name: '商品4', imageUrl: '/images/shoe4.jpg' },
  { id: 5, name: '商品5', imageUrl: '/images/shoe5.jpg' },
  { id: 6, name: '商品6', imageUrl: 'https://picsum.photos/250/150?random=6' },
  { id: 7, name: '商品7', imageUrl: 'https://picsum.photos/250/150?random=7' },
  { id: 8, name: '商品8', imageUrl: 'https://picsum.photos/250/150?random=8' },
  { id: 9, name: '商品9', imageUrl: 'https://picsum.photos/250/150?random=9' },
])

// 模拟热销商品数据 (ID范围: 100-108)
const hotProducts = ref([
  { id: 100, name: '热销商品1', imageUrl: 'https://picsum.photos/300/200?random=1' },
  { id: 101, name: '热销商品2', imageUrl: 'https://picsum.photos/150/200?random=2' },
  { id: 102, name: '热销商品3', imageUrl: 'https://picsum.photos/150/200?random=3' },
  { id: 103, name: '热销商品4', imageUrl: 'https://picsum.photos/150/200?random=4' },
  { id: 104, name: '热销商品5', imageUrl: 'https://picsum.photos/150/200?random=5' },
  { id: 105, name: '热销商品6', imageUrl: 'https://picsum.photos/150/200?random=6' },
  { id: 106, name: '热销商品7', imageUrl: 'https://picsum.photos/150/200?random=7' },
  { id: 107, name: '热销商品8', imageUrl: 'https://picsum.photos/150/200?random=8' },
  { id: 108, name: '热销商品9', imageUrl: 'https://picsum.photos/150/200?random=9' },
  // 可按需添加更多商品
])

// 添加推荐商品数据 (ID范围: 200-211)
const recommendedProducts = ref([
  { id: 200, name: '推荐商品1', imageUrl: 'https://picsum.photos/200/200?random=200' },
  { id: 201, name: '推荐商品2', imageUrl: 'https://picsum.photos/200/200?random=201' },
  { id: 202, name: '推荐商品3', imageUrl: 'https://picsum.photos/200/200?random=202' },
  { id: 203, name: '推荐商品4', imageUrl: 'https://picsum.photos/200/200?random=203' },
  { id: 204, name: '推荐商品5', imageUrl: 'https://picsum.photos/200/200?random=204' },
  { id: 205, name: '推荐商品6', imageUrl: 'https://picsum.photos/200/200?random=205' },
  { id: 206, name: '推荐商品7', imageUrl: 'https://picsum.photos/200/200?random=206' },
  { id: 207, name: '推荐商品8', imageUrl: 'https://picsum.photos/200/200?random=207' },
  { id: 208, name: '推荐商品9', imageUrl: 'https://picsum.photos/200/200?random=208' },
  { id: 209, name: '推荐商品10', imageUrl: 'https://picsum.photos/200/200?random=209' },
  { id: 210, name: '推荐商品11', imageUrl: 'https://picsum.photos/200/200?random=210' },
  { id: 211, name: '推荐商品12', imageUrl: 'https://picsum.photos/200/200?random=211' },
])


// 轮播图数据
const carouselImages = ref([
  {
    url: '/images/campus.png',
    title: '校园交易平台',
    subtitle: '开启便捷品质生活，品类齐全 现货热销'
  },
  {
    url: '/images/show.jpg',
    title: '实战必备穿搭优选',
    subtitle: '衣橱焕新精致指南，精选球鞋 火热开抢'
  },
  {
    url: '/images/show2.jpg',
    title: '崭新出售',
    subtitle: '极限竞速，极限挑战'
  }
])

const loading = ref(false)
</script>