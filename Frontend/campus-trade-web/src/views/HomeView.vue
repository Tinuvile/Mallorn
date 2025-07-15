<script setup lang="ts">
  import { ref, onMounted } from 'vue'
  import { ShoppingBag, ChatLineRound, Lock } from '@element-plus/icons-vue'

  // 统计数据
  const totalProducts = ref(1234)
  const totalUsers = ref(5678)
  const totalTransactions = ref(987)

  // 最新商品数据
  const recentProducts = ref([
    {
      id: 1,
      name: '二手笔记本电脑',
      price: 2999,
      seller: '张同学',
      image: 'https://via.placeholder.com/200x150?text=笔记本',
    },
    {
      id: 2,
      name: '数学教材',
      price: 45,
      seller: '李同学',
      image: 'https://via.placeholder.com/200x150?text=教材',
    },
    {
      id: 3,
      name: '自行车',
      price: 300,
      seller: '王同学',
      image: 'https://via.placeholder.com/200x150?text=自行车',
    },
    {
      id: 4,
      name: '台灯',
      price: 80,
      seller: '赵同学',
      image: 'https://via.placeholder.com/200x150?text=台灯',
    },
  ])

  onMounted(() => {
    // 这里可以加载真实数据
    console.log('Home page mounted')
  })
</script>

<template>
  <div class="home">
    <div class="welcome-section">
      <el-card class="welcome-card">
        <div class="welcome-content">
          <h1>欢迎来到校园交易平台</h1>
          <p>在这里，你可以轻松买卖二手物品，与同学们交流交易</p>
          <div class="stats">
            <div class="stat-item">
              <el-statistic title="在售商品" :value="totalProducts" />
            </div>
            <div class="stat-item">
              <el-statistic title="注册用户" :value="totalUsers" />
            </div>
            <div class="stat-item">
              <el-statistic title="成功交易" :value="totalTransactions" />
            </div>
          </div>
        </div>
      </el-card>
    </div>

    <div class="features-section">
      <h2>平台特色</h2>
      <el-row :gutter="20">
        <el-col :span="8" :xs="24" :sm="12" :md="8">
          <el-card class="feature-card">
            <div class="feature-icon">
              <el-icon size="48" color="#409EFF">
                <ShoppingBag />
              </el-icon>
            </div>
            <h3>商品发布</h3>
            <p>轻松发布你的闲置物品，详细的分类和搜索功能让买家快速找到心仪商品</p>
          </el-card>
        </el-col>

        <el-col :span="8" :xs="24" :sm="12" :md="8">
          <el-card class="feature-card">
            <div class="feature-icon">
              <el-icon size="48" color="#67C23A">
                <ChatLineRound />
              </el-icon>
            </div>
            <h3>即时沟通</h3>
            <p>内置消息系统，买卖双方可以实时沟通，确保交易顺利进行</p>
          </el-card>
        </el-col>

        <el-col :span="8" :xs="24" :sm="12" :md="8">
          <el-card class="feature-card">
            <div class="feature-icon">
              <el-icon size="48" color="#E6A23C">
                <Lock />
              </el-icon>
            </div>
            <h3>安全保障</h3>
            <p>校园内部交易，身份认证，为你的每一次交易提供安全保障</p>
          </el-card>
        </el-col>
      </el-row>
    </div>

    <div class="recent-section">
      <h2>最新商品</h2>
      <el-row :gutter="20">
        <el-col
          :span="6"
          :xs="24"
          :sm="12"
          :md="6"
          v-for="product in recentProducts"
          :key="product.id"
        >
          <el-card class="product-card" shadow="hover">
            <img :src="product.image" class="product-image" />
            <div class="product-info">
              <h4>{{ product.name }}</h4>
              <p class="product-price">¥{{ product.price }}</p>
              <p class="product-seller">卖家：{{ product.seller }}</p>
            </div>
          </el-card>
        </el-col>
      </el-row>
    </div>
  </div>
</template>

<style scoped>
  .home {
    width: 100%;
    padding: 20px;
    box-sizing: border-box;
  }

  .welcome-section {
    margin-bottom: 40px;
  }

  .welcome-card {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    border: none;
  }

  .welcome-card :deep(.el-card__body) {
    padding: 40px;
  }

  .welcome-content h1 {
    font-size: 32px;
    margin-bottom: 16px;
    text-align: center;
  }

  .welcome-content p {
    font-size: 18px;
    text-align: center;
    margin-bottom: 30px;
    opacity: 0.9;
  }

  .stats {
    display: flex;
    justify-content: space-around;
    gap: 20px;
  }

  .stat-item {
    text-align: center;
  }

  .stat-item :deep(.el-statistic__content) {
    color: white;
  }

  .stat-item :deep(.el-statistic__head) {
    color: rgba(255, 255, 255, 0.8);
  }

  .features-section,
  .recent-section {
    margin-bottom: 40px;
  }

  .features-section h2,
  .recent-section h2 {
    text-align: center;
    margin-bottom: 30px;
    color: #333;
    font-size: 28px;
  }

  .feature-card {
    text-align: center;
    height: 200px;
    margin-bottom: 20px;
  }

  .feature-icon {
    margin-bottom: 16px;
  }

  .feature-card h3 {
    color: #333;
    margin-bottom: 12px;
  }

  .feature-card p {
    color: #666;
    line-height: 1.6;
  }

  .product-card {
    margin-bottom: 20px;
    cursor: pointer;
    transition: transform 0.2s;
  }

  .product-card:hover {
    transform: translateY(-4px);
  }

  .product-image {
    width: 100%;
    height: 150px;
    object-fit: cover;
    border-radius: 4px;
  }

  .product-info {
    padding: 12px 0;
  }

  .product-info h4 {
    margin: 0 0 8px 0;
    color: #333;
    font-size: 16px;
  }

  .product-price {
    color: #e6a23c;
    font-size: 18px;
    font-weight: bold;
    margin: 4px 0;
  }

  .product-seller {
    color: #999;
    font-size: 14px;
    margin: 4px 0 0 0;
  }

  @media (max-width: 768px) {
    .stats {
      flex-direction: column;
      gap: 10px;
    }

    .welcome-content h1 {
      font-size: 24px;
    }

    .welcome-content p {
      font-size: 16px;
    }
  }
</style>
