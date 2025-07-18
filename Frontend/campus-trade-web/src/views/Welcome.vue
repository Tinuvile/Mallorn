<template>
  <v-app id="inspire">
    <v-system-bar>
      <v-spacer></v-spacer>

      <v-icon>mdi-circle</v-icon>
      <v-icon>mdi-account</v-icon>
    </v-system-bar>

    <!-- 导航栏 -->
    <v-app-bar color="#E3F2FD" height="72" dark>
      <template v-slot:img="{ props }">
        <v-img v-bind="props" gradient="to top right, rgba(19,84,122,.5), rgba(128,208,199,.8)"></v-img>
      </template>
      <v-btn icon color="indigo"  class="mx-2">
        <v-icon size="40">mdi-account-circle</v-icon>
      </v-btn>
      <v-btn icon @click="goToCart" class="mx-2">
        <v-icon size="40">mdi-cart</v-icon>
      </v-btn>

      <v-btn class="me-2" color="black" height="40" variant="text" width="100" style="margin-left: 100px;"
        size="large">登录</v-btn>
      <v-btn class="me-3" color="black" height="40" variant="text" width="100" style="margin-left: 50px;"
        size="large">注册</v-btn>

      <!-- 搜索框 -->
      <v-text-field :loading="loading" append-inner-icon="mdi-magnify" density="compact" label="Search" variant="solo"
        hide-details single-line @click:append-inner="onClick" style="margin-left: 100px;"></v-text-field>


      <v-spacer></v-spacer>
    </v-app-bar>

    <!-- 页面底部 -->
    <v-footer color="grey" height="30" app></v-footer>

    <!-- 页面左侧 -->
    <v-navigation-drawer floating>
      <h1 style="text-align: start; margin: 20px 0px -30px 15px;">在这里探索不同</h1>
      <v-icon size="40">mdi-shop-find</v-icon>
      
      <v-card :disabled="loading" :loading="loading" class="mx-auto my-12" max-width="200" @click="goToBook">
        <template v-slot:loader="{ isActive }">
          <v-progress-linear :active="isActive" color="deep-purple" height="4" indeterminate></v-progress-linear>
        </template>

        <v-img height="120" src="/images/books.jpg" cover></v-img>

        <v-card-item>
          <v-card-title>Books</v-card-title>

          <v-card-subtitle>
            <span class="me-1">这里有你想要的所有书籍</span>
          </v-card-subtitle>
        </v-card-item>
      </v-card>

      <v-card :disabled="loading" :loading="loading" class="mx-auto my-12" max-width="200" @click="goToBook">
        <template v-slot:loader="{ isActive }">
          <v-progress-linear :active="isActive" color="deep-purple" height="4" indeterminate></v-progress-linear>
        </template>

        <v-img height="120" src="/images/daily.jpg" cover></v-img>

        <v-card-item>
          <v-card-title>Daily using</v-card-title>

          <v-card-subtitle>
            <span class="me-1">这里有你想要的日常用品</span>
          </v-card-subtitle>
        </v-card-item>
      </v-card>

      <v-card :disabled="loading" :loading="loading" class="mx-auto my-12" max-width="200" @click="goToBook">
        <template v-slot:loader="{ isActive }">
          <v-progress-linear :active="isActive" color="deep-purple" height="4" indeterminate></v-progress-linear>
        </template>

        <v-img height="120" src="/images/dianzi.jpg" cover></v-img>

        <v-card-item>
          <v-card-title>Electronic Products</v-card-title>

          <v-card-subtitle>
            <span class="me-1">这里有你想要的电子产品</span>
          </v-card-subtitle>
        </v-card-item>
      </v-card>

      <v-card :disabled="loading" :loading="loading" class="mx-auto my-12" max-width="200" @click="goToBook">
        <template v-slot:loader="{ isActive }">
          <v-progress-linear :active="isActive" color="deep-purple" height="4" indeterminate></v-progress-linear>
        </template>

        <v-img height="120" src="/images/food.jpg" cover></v-img>

        <v-card-item>
          <v-card-title>Food</v-card-title>

          <v-card-subtitle>
            <span class="me-1">这里有你想要的所有食品</span>
          </v-card-subtitle>
        </v-card-item>
      </v-card>
    </v-navigation-drawer>

    <v-divider></v-divider>
    <!-- 页面主体内容 -->
    <v-main style="margin-top: -20px;">
      <!-- 滚动图片展示 -->
      <v-carousel style=" height: 350px;width: 80%; margin: 40px auto 20px; overflow: hidden;" interval="3000"
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

      <!-- 促销 -->
      <v-sheet class="mx-auto pa-2 pt-6">
        <v-icon>mdi-sale</v-icon>
        <p style="font-size: 40px;">促销</p>
        <v-slide-group show-arrows>
          <v-slide-group-item v-for="(product, index) in products" :key="index">
            <v-card class="ma-3" height="200" width="250" rounded>
              <v-img :src="product.imageUrl" :alt="product.name" height="150" contain placeholder="加载中..." />
              <v-card-title class="text-center pt-2">{{ product.name }}</v-card-title>
            </v-card>
          </v-slide-group-item>
        </v-slide-group>
      </v-sheet>

      <!-- 热销商品 -->
      <v-sheet class="mx-auto pa-2 pt-6">
        <v-icon>mdi-fire</v-icon>
        <p style="font-size: 40px;">热销中</p>

        <v-slide-group show-arrows>
          <v-slide-group-item v-for="n in 12" :key="n">
            <v-img :width="n === 1 ? 300 : 150"
              :src="'https://picsum.photos/'+(n === 1 ? '300/200' : '150/200')+'?random='+n" :alt="'图片'+n" class="ma-3"
              height="200" rounded contain loading="lazy" />
          </v-slide-group-item>
        </v-slide-group>

        <v-container fluid>
          <v-row>
            <v-col v-for="n in 12" :key="n" cols="2">
              <v-img :width="200" :src="'https://picsum.photos/200/200?random=' + n" :alt="'图片' + n" rounded contain
                loading="lazy" class="ma-2 rounded-lg" />
            </v-col>
          </v-row>
        </v-container>
      </v-sheet>
    </v-main>
  </v-app>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()

// 模拟购物车商品数量
const cartItemsCount = ref(3)

// 点击购物车图标时的跳转方法(还需要根据实际路由调整)
const goToCart = () => {
  //router.push({ name: 'Cart' }) // 跳转到名为'Cart'的路由
  // 也可以使用路径跳转: router.push('/cart')
}
const products = ref([
  { id: 1, name: '商品1', imageUrl: 'https://picsum.photos/250/150?random=1' },
  { id: 2, name: '商品2', imageUrl: 'https://picsum.photos/250/150?random=2' },
  { id: 3, name: '商品3', imageUrl: 'https://picsum.photos/250/150?random=3' },
  { id: 4, name: '商品4', imageUrl: 'https://picsum.photos/250/150?random=4' },
  { id: 5, name: '商品5', imageUrl: 'https://picsum.photos/250/150?random=5' },
])

// 轮播图数据
const carouselImages = ref([
  {
    url: 'https://picsum.photos/1200/400?random=1',
    title: '智能家电新体验',
    subtitle: '开启便捷品质生活，品类齐全 现货热销'
  },
  {
    url: 'https://picsum.photos/1200/400?random=2',
    title: '轻盈夏日穿搭优选',
    subtitle: '衣橱焕新精致指南，精选女装 火热开抢'
  },
  {
    url: 'https://picsum.photos/1200/400?random=3',
    title: '美食盛宴',
    subtitle: '尽享美味，舌尖上的诱惑'
  }
])

  const loading = ref(false)
  function goToBook() {
    loading.value = true
    setTimeout(() => (loading.value = false), 2000)
  }
</script>