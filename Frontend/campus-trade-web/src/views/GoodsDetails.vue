<template>
  <v-app>
    <header class="navbar">
      <span class="icon">
        <svg width="24px" height="24px" stroke-width="1.5" viewBox="0 0 24 24" fill="none"
          xmlns="http://www.w3.org/2000/svg" color="#000000">
          <circle cx="12" cy="12" r="10" stroke="#000000" stroke-width="1.5"></circle>
          <path
            d="M7.63262 3.06689C8.98567 3.35733 9.99999 4.56025 9.99999 6.00007C9.99999 7.65693 8.65685 9.00007 6.99999 9.00007C5.4512 9.00007 4.17653 7.82641 4.01685 6.31997"
            stroke="#000000" stroke-width="1.5"></path>
          <path
            d="M22 13.0505C21.3647 12.4022 20.4793 12 19.5 12C17.567 12 16 13.567 16 15.5C16 17.2632 17.3039 18.7219 19 18.9646"
            stroke="#000000" stroke-width="1.5"></path>
          <path d="M14.5 8.51L14.51 8.49889" stroke="#000000" stroke-width="1.5" stroke-linecap="round"
            stroke-linejoin="round"></path>
          <path
            d="M10 17C11.1046 17 12 16.1046 12 15C12 13.8954 11.1046 13 10 13C8.89543 13 8 13.8954 8 15C8 16.1046 8.89543 17 10 17Z"
            stroke="#000000" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
        </svg>
      </span>
      <span class="title">Campus Secondhand</span>
    </header>
    <v-container fluid>
      <!-- 返回按钮和搜索栏 -->
      <v-row class="mt-3">
        <v-col cols="4">
          <v-btn icon color="indigo" @click="goToHome" style="margin-left: 50px;">
            <v-icon>mdi-home</v-icon>
          </v-btn>
        </v-col>
        <v-col cols="6">
          <v-text-field :loading="loading" append-inner-icon="mdi-magnify" density="compact" label="搜索" variant="solo"
            hide-details single-line @click:append-inner="onSearch"></v-text-field>
        </v-col>
      </v-row>
      <!-- 商品详情 -->
      <v-row style="margin-top: 20px; margin-left: 50px;">
        <!-- 左侧滚动图片区域 -->
        <v-col cols="12" sm="6">
          <v-carousel height="600" hide-delimiters v-model="currentImageIndex">
            <v-carousel-item v-for="(img, index) in imgList" :key="index" :src="img"></v-carousel-item>
          </v-carousel>

          <!-- 添加缩略图导航 -->
          <div class="d-flex mt-3 justify-center">
            <div v-for="(img, index) in imgList" :key="index" class="thumbnail-item mr-2 cursor-pointer"
              :class="{ 'selected-thumbnail': currentImageIndex === index }" @mouseover="currentImageIndex = index">
              <img :src="img" :alt="'缩略图 ' + (index + 1)" class="thumbnail-img">
            </div>
          </div>
        </v-col>
        <!-- 右侧商品信息区域 -->
        <v-col cols="12" sm="6">
          <!-- 标题及店铺信息 -->
          <div class="d-flex justify-space-between align-center mb-2">
            <h2 class="text-h5 font-weight-bold">
              {{ productName }}
            </h2>
          </div>
          <!-- 销量 -->
          <div class="mb-3">
            <v-chip class="mr-2" color="orange" text-color="white" small>
              超2000人浏览
            </v-chip>
            <v-chip class="mr-2" color="grey" text-color="white" small>
              已售100+
            </v-chip>
          </div>
          <!-- 价格显示区域 -->
          <div class="mb-3">
            <span class="text-h4 font-weight-bold" style="color: red;">￥{{ productPrice }}</span>
          </div>
          <!-- 活动信息 -->
          <div class="mb-3">
            <span class="mr-2" style="color: red;">活动</span>
            <v-chip class="mr-1" color="gold" text-color="white" small>
              领券立减
            </v-chip>
            <span>送你500淘金币，下单直抵5元</span>
          </div>
          <!-- 配送信息 -->
          <div class="mb-3">
            <span class="mr-2" style="color: red;">配送</span>
            <span>48小时内发货 快递:免运费</span>
            <span class="grey--text ml-2">北京 至 上海嘉定</span>
          </div>
          <!-- 保障信息 -->
          <div class="mb-3" style="color: red;">
            <span class="mr-2">保障</span>
            <v-chip class="mr-1" color="grey" text-color="white" small>
              退货包
            </v-chip>
            <v-chip class="mr-1" color="grey" text-color="white" small>
              7天无理由退货
            </v-chip>
            <v-chip class="mr-1" color="grey" text-color="white" small>
              极速退款
            </v-chip>
          </div>
          <!-- 型号 -->
          <div class="mb-3">
            <span class="mr-2" style="color: red;">型号</span>
            <v-radio-group v-model="selectedColor">
              <v-radio v-for="(color, index) in colorList" :key="index" :label="color" :value="color"></v-radio>
            </v-radio-group>
          </div>
          <!-- 议价及购买按钮 -->
          <div class="d-flex">
            <v-btn color="orange" text-color="white" class="mr-3"  @click="showNegotiateModal = true"> 
              议价
            </v-btn>
            <v-btn color="red" text-color="white"  @click="navigateToConfirmOrder">
              购买
            </v-btn>
          </div>
        </v-col>
      </v-row>

   <!-- 议价浮窗 -->
   <div class="negotiate-modal-mask" v-if="showNegotiateModal">
     <div class="negotiate-modal">
      <!-- 议价标题 -->
      <h2 class="negotiate-title">商品议价</h2>
      
      <!-- 商品信息区域 -->
      <div class="product-info-section">
        <p class="product-name" style="color: #666;">商品名称: {{ productName }}</p>
        <p class="product-size">所选鞋码: {{ selectedColor || '未选择型号' }}</p>
      </div>
      
      <!-- 分隔线 -->
      <div class="divider"></div>
      
      <!-- 价格区域 -->
      <div class="price-section">
        <p class="original-price">原价: ¥{{ productPrice }}</p>
        <p class="seller-price">卖家出价:</p>
        <input type="number" v-model="userOffer" class="offer-input" placeholder="请输入您的出价">
      </div>
      
      <!-- 出价按钮 -->
      <button class="confirm-offer-btn" @click="submitOffer">确定出价</button>
      
      <!-- 关闭按钮 -->
      <button class="close-btn" @click="showNegotiateModal = false">×</button>
    </div>
  </div>

      <!-- 商品参数等扩展内容，可按需完善 -->
      <!-- 导航栏 -->
      <v-row class="mt-5">
        <v-col cols="9">
          <v-tabs v-model="activeTab" color="primary" dark centered grow>
            <v-tab style="font-size: 18px; padding: 16px;" @click="scrollToSection('productParams')">
              商品参数
            </v-tab>
            <v-tab style="font-size: 18px; padding: 16px;" @click="scrollToSection('productReviews')">
              商品评价
            </v-tab>
          </v-tabs>
        </v-col>
      </v-row>
      <!-- 分界线 -->
      <v-divider class="mb-4"></v-divider>
      <!-- 展示内容 -->
      <v-row>
        <v-col cols="9">
          <v-tabs-items v-model="activeTab">
            <!-- 商品参数 -->
            <v-tab-item>
              <h3 id="productParams" class="text-h6 font-weight-bold mb-4">商品参数</h3>
              <v-row>
                <v-col cols="6" class="mb-2" v-for="(param, index) in productParams" :key="index">
                  <div class="d-flex">
                    <div class="text-grey" style="width: 120px;">{{ param.name }}</div>
                    <div>{{ param.value }}</div>
                  </div>
                </v-col>
              </v-row>
            </v-tab-item>

            <!-- 商品评价 -->
            <v-tab-item>
              <h3 id="productReviews" class="text-h6 font-weight-bold mb-2">商品评价</h3>
              <div v-if="reviews.length === 0">
                <p>暂时还没有评价哦，快来成为第一个评价的人吧！</p>
              </div>
              <div v-else>
                <div v-for="(review, index) in reviews" :key="index">
                  <div class="d-flex justify-between align-start mb-4">
                    <div class="d-flex align-start flex-grow-1">
                      <v-avatar size="40">
                        <img :src="review.avatar" alt="Avatar">
                      </v-avatar>
                      <div class="ml-3">
                        <div class="font-weight-bold">{{ review.nickname }}</div>
                        <div class="text-sm grey--text">{{ review.time }}</div>
                        <div class="mt-2">{{ review.content }}</div>
                      </div>
                    </div>
                    <div class="d-flex flex-column align-center ml-5">
                      <!-- 点赞按钮样式 -->
                      <v-btn icon bg-transparent @click="toggleLike(index)" elevation="0" class="like-btn">
                        <v-icon :color="review.isLiked ? 'red' : 'gray'">mdi-thumb-up</v-icon>
                      </v-btn>
                      <div class="text-sm grey--text">{{ review.likes }}</div>
                    </div>
                  </div>
                  <v-divider v-if="index < reviews.length - 1" class="mb-4" />
                </div>
              </div>
            </v-tab-item>
          </v-tabs-items>
        </v-col>
        <!-- 右侧留白区域 - 添加固定购买卡片 -->
        <v-col cols="3">
          <!-- 固定购买卡片 -->
          <div v-if="showFixedBuyCard" class="fixed-buy-card" :class="{ 'fade-in': showFixedBuyCard }">
            <!-- 商品图片 -->
            <img :src="imgList[0]" alt="商品缩略图" class="fixed-card-img">

            <!-- 型号选择 -->
            <div class="mt-2">
              <span class="text-sm text-grey">选择型号</span>
              <v-radio-group v-model="selectedColor" class="mt-1">
                <v-radio v-for="(color, index) in colorList" :key="index" :label="color" :value="color"
                  class="text-xs"></v-radio>
              </v-radio-group>
            </div>

            <!-- 价格 -->
            <div class="mt-2 text-h6 font-weight-bold text-red">¥{{ productPrice }}</div>

            <!-- 按钮组 -->
            <div class="d-flex mt-3">
              <v-btn color="orange" text-color="white" class="flex-grow-1 mr-2" small>
                加入购物车
              </v-btn>
              <v-btn color="red" text-color="white" class="flex-grow-1" small  @click="navigateToConfirmOrder">
                购买
              </v-btn>
            </div>
          </div>
        </v-col>
      </v-row>
    </v-container>
  </v-app>
</template>


<script setup>
import { ref, onMounted, onUnmounted, computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';

const router = useRouter();
const route = useRoute();

// 获取商品ID（从路由参数）
const productId = ref(route.params.id || 0);

// 导航到确认订单页面
const navigateToConfirmOrder = () => {
  if (!selectedColor.value) {
    alert('请先选择商品型号');
    return;
  }
  
  // 传递商品信息到确认订单页面
  router.push({
    name: 'confirmorderview',
    query: {
      productId: productId.value,
      productName: productName.value,
      productPrice: productPrice.value,
      selectedColor: selectedColor.value,
      productImage: imgList.value[0],
      quantity: 1
    }
  });
};

// 使用 ref 创建响应式数据
const imgList = ref([
  '/images/n1.jpg',
  '/images/n2.jpg',
  '/images/n3.jpg'
]);

const colorList = ref([
  '【现货】44码',
  '【现货】45码',
  '【现货】46码',
]);

const selectedColor = ref('');
const activeTab = ref(0); // 默认显示商品参数
const currentImageIndex = ref(0);


// 添加首页跳转方法
const goToHome = () => {
  router.push('/'); // 跳转到首页路由，确保与你的路由配置一致
};

// 商品名称和价格数据
const productName = ref('New Balance NB 530');
const productPrice = ref(152.15);

// 商品参数数据
const productParams = ref([
  { name: '主货号', value: 'MR530SG' },
  { name: '发售价格', value: '¥699' },
  { name: '主色', value: '白色' },
  { name: '发售日期', value: '2020春季' },
  { name: '鞋面材质', value: '网布,织物,人造革' },
  { name: '鞋头款式', value: '圆头' },
  { name: '鞋跟类型', value: '平跟' },
  { name: '闭合方式', value: '系带' },
  { name: '鞋帮高度', value: '低帮' },
  { name: '功能性', value: '减震,耐磨,透气' },
  { name: '适用季节', value: '春,夏,秋,冬' },
  { name: '辅色', value: '银色' },
  { name: '跑鞋等级', value: '入门跑' },
  { name: '适用场地', value: '公路,健身房,跑步机' },
  { name: '鞋身重量（约）', value: '290g' },
  { name: '适用场景', value: '休闲,城市通勤' }
]);



// 修改议价相关状态
const showNegotiateModal = ref(false);
const sellerPrice = ref(0);
const userOffer = ref('');

// 提交议价
const submitOffer = () => {
  if (!selectedColor.value) {
    alert('请先选择商品型号');
    return;
  }
  if (!userOffer.value || userOffer.value <= 0) {
    alert('请输入有效的出价金额');
    return;
  }
  // 这里可以添加实际的提交逻辑
  alert(`您的出价 ¥${userOffer.value} 已提交，请等待卖家回复`);
  showNegotiateModal.value = false;
};

// 模拟评价数据
const reviews = ref([
  {
    avatar: 'https://picsum.photos/40/40?random=1',
    nickname: '张三',
    time: '2025-07-20 10:00',
    content: '这个模型质量很不错，细节很到位，很喜欢！',
    likes: 10,
    isLiked: false
  },
  {
    avatar: 'https://picsum.photos/40/40?random=2',
    nickname: '李四',
    time: '2025-07-22 14:30',
    content: '包装很用心，物流也很快，推荐购买！',
    likes: 5,
    isLiked: false
  },
    // 新增评价1
  {
    avatar: 'https://picsum.photos/40/40?random=3',
    nickname: '王五',
    time: '2025-07-25 09:15',
    content: '鞋子很舒服，尺码标准，透气性好，推荐购买！',
    likes: 15,
    isLiked: false
  },
  // 新增评价2
  {
    avatar: 'https://picsum.photos/40/40?random=4',
    nickname: '赵六',
    time: '2025-07-26 14:20',
    content: '颜色和图片一致，做工精细，性价比高，物流也快。',
    likes: 8,
    isLiked: false
  },
  // 新增评价3
  {
    avatar: 'https://picsum.photos/40/40?random=5',
    nickname: '孙七',
    time: '2025-07-27 11:30',
    content: '穿上很轻便，适合日常穿搭，已经回购第二双了。',
    likes: 20,
    isLiked: false
  },
  // 新增评价4
  {
    avatar: 'https://picsum.photos/40/40?random=6',
    nickname: '周八',
    time: '2025-07-28 16:45',
    content: '减震效果不错，跑步很舒服，就是鞋码有点偏大，建议拍小一码。',
    likes: 12,
    isLiked: false
  },
  // 新增评价5
  {
    avatar: 'https://picsum.photos/40/40?random=7',
    nickname: '吴九',
    time: '2025-07-29 10:00',
    content: '外观复古好看，搭配牛仔裤很合适，质量也很好，值得购买。',
    likes: 17,
    isLiked: false
  }
]);

// 点赞状态方法
const toggleLike = (index) => {
  const review = reviews.value[index];
  if (review.isLiked) {
    review.likes--;
  } else {
    review.likes++;
  }
  review.isLiked = !review.isLiked;
};

const scrollToSection = (sectionId) => {
  const element = document.getElementById(sectionId);
  if (element) {
    element.scrollIntoView({
      behavior: 'smooth',
      block: 'start'
    });
  }
};

// 固定购买卡片相关状态
const showFixedBuyCard = ref(false);
const scrollPosition = ref(0);

// 监听滚动事件
const handleScroll = () => {
  scrollPosition.value = window.scrollY;

  // 获取商品参数区域的位置
  const paramsSection = document.getElementById('productParams');
  if (paramsSection) {
    const paramsTop = paramsSection.offsetTop - 200; // 提前200px显示

    // 当滚动到商品参数区域时显示卡片
    showFixedBuyCard.value = scrollPosition.value >= paramsTop;
  }
};

// 组件挂载时添加滚动监听
onMounted(() => {
  window.addEventListener('scroll', handleScroll);
  // 初始检查一次位置
  handleScroll();
});

// 组件卸载时移除滚动监听
onUnmounted(() => {
  window.removeEventListener('scroll', handleScroll);
});

</script>

<style scoped>
/* 缩略图样式 */
.thumbnail-item {
  width: 80px;
  height: 80px;
  /* 未选中时灰白色边框 */
  border: 2px solid #e0e0e0;
  border-radius: 4px;
  overflow: hidden;
  /* 添加过渡效果使边框变化更平滑 */
  transition: border-color 0.3s ease;
}

/* 鼠标悬浮时黑色边框 */
.thumbnail-item:hover {
  border-color: #000000;
}

/* 选中状态样式保持不变 */
.selected-thumbnail {
  border-color: #1976d2;
  box-shadow: 0 0 0 2px rgba(25, 118, 210, 0.3);
}

.thumbnail-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

/* 固定购买卡片样式 */
.fixed-buy-card {
  position: fixed;
  top: 200px;
  width: 280px;
  padding: 16px;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  background: white;
  z-index: 10;
  transition: all 0.3s ease;
}

/* 淡入动画 */
.fade-in {
  animation: fadeIn 0.5s ease forwards;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(20px);
  }

  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* 卡片图片样式 */
.fixed-card-img {
  width: 100%;
  height: 180px;
  object-fit: cover;
  border-radius: 4px;
}

/* 隐藏滚动条但保持滚动功能 */
.v-radio-group {
  max-height: 120px;
  overflow-y: auto;
  padding-right: 8px;
}

.v-radio-group::-webkit-scrollbar {
  width: 4px;
}

.v-radio-group::-webkit-scrollbar-thumb {
  background-color: #ddd;
  border-radius: 2px;
}


/* 议价浮窗样式 */
.negotiate-modal-mask {
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

.negotiate-modal {
  width: 500px;
  background-color: white;
  border-radius: 8px;
  padding: 25px;
  position: relative;
}

.negotiate-title {
  text-align: center;
  font-size: 30px;
  font-weight: bold;
  margin-bottom: 20px;
  color: #333;
}

.product-info-section {
  margin-bottom: 15px;
  padding-bottom: 15px;
}

.product-name {
  font-size: 16px;
  margin-bottom: 8px;
}

.product-size {
  font-size: 16px;
  color: #666;
}

.divider {
  height: 1px;
  background-color: #eee;
  margin: 15px 0;
}

.price-section {
  margin-bottom: 25px;
}

.original-price {
  font-size: 14px;
  color: #999;
  text-decoration: line-through;
  margin-bottom: 8px;
}

.seller-price {
  font-size: 16px;
  margin-bottom: 15px;
}

.offer-input {
  width: 100%;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 16px;
  margin-bottom: 20px;
}

.confirm-offer-btn {
  width: 100%;
  padding: 12px;
  background-color: #007bff;
  color: white;
  border: none;
  border-radius: 4px;
  font-size: 16px;
  cursor: pointer;
}

.confirm-offer-btn:hover {
  background-color: #0056b3;
}

.close-btn {
  position: absolute;
  top: 15px;
  right: 15px;
  font-size: 20px;
  background: none;
  border: none;
  cursor: pointer;
  color: #999;
}

/* 顶部导航栏 */
.navbar {
  height: 50px;
  border-bottom: 2px solid #adadad;
  display: flex;
  position: 0 0 0 0;
  align-items: center;
  padding-left: 32px;
  background: #fff;
  position: relative;
  z-index: 2;
}

.icon {
  font-size: 25px;
  color: #222;
  margin-right: 10px;
}

.title {
  font-weight: 600;
  font-size: 30px;
  letter-spacing: 0.5px;
}
</style>