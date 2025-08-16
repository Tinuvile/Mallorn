<template>
  <div class="app-container">
    <!-- White top navigation bar -->
    <header class="top-navbar">
      <div class="navbar-content">
        <div class="left-section">
          <span class="icon">
            <svg width="24px" height="24px" stroke-width="1.5" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" color="#000000">
              <circle cx="12" cy="12" r="10" stroke="#000000" stroke-width="1.5"></circle>
              <path d="M7.63262 3.06689C8.98567 3.35733 9.99999 4.56025 9.99999 6.00007C9.99999 7.65693 8.65685 9.00007 6.99999 9.00007C5.4512 9.00007 4.17653 7.82641 4.01685 6.31997" stroke="#000000" stroke-width="1.5"></path>
              <path d="M22 13.0505C21.3647 12.4022 20.4793 12 19.5 12C17.567 12 16 13.567 16 15.5C16 17.2632 17.3039 18.7219 19 18.9646" stroke="#000000" stroke-width="1.5"></path>
              <path d="M14.5 8.51L14.51 8.49889" stroke="#000000" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
              <path d="M10 17C11.1046 17 12 16.1046 12 15C12 13.8954 11.1046 13 10 13C8.89543 13 8 13.8954 8 15C8 16.1046 8.89543 17 10 17Z" stroke="#000000" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
            </svg>
          </span>
          <span class="title">Campus Secondhand</span>
        </div>
        
        <div class="right-section">
          <v-btn 
            fab
            to="/"
            class="logout-btn"
            color="white"
            size="46"
          >
            <v-icon size="32" color="red-darken-1">mdi-logout-variant</v-icon>
          </v-btn>
        </div>
      </div>
    </header>

    <div class="container">
      <div class="header">
        <div class="logo">
          <div class="page-title-container">
            <div class="page-title">
              <span class="title-text">数据看板</span>
              <span class="title-line"></span>
            </div>
          </div>
        </div>
      </div>

      <div class="stats-container">
        <div class="stat-card">
          <div class="stat-title">总用户数</div>
          <div class="stat-value">8,742</div>
          <div class="stat-desc">较上月增长 12.5%</div>
        </div>
        <div class="stat-card">
          <div class="stat-title">活跃用户</div>
          <div class="stat-value">5,283</div>
          <div class="stat-desc">较上月增长 8.3%</div>
        </div>
        <div class="stat-card">
          <div class="stat-title">总订单数</div>
          <div class="stat-value">12,647</div>
          <div class="stat-desc">较上月增长 15.2%</div>
        </div>
        <div class="stat-card">
          <div class="stat-title">本月交易额</div>
          <div class="stat-value">¥385,642</div>
          <div class="stat-desc">较上月增长 10.8%</div>
        </div>
      </div>

      <div class="charts-container">
        <div class="chart-wrapper">
          <div class="chart-title">用户活跃度趋势</div>
          <div class="chart-container">
            <canvas id="activityChart"></canvas>
          </div>
        </div>
        <div class="chart-wrapper">
          <div class="chart-title">月度交易量</div>
          <div class="chart-container">
            <canvas id="transactionChart"></canvas>
          </div>
        </div>
      </div>

      <div class="table-container">
        <div class="table-title">热销商品排行</div>
        <table class="hot-products">
          <thead>
            <tr>
              <th>排名</th>
              <th>商品名称</th>
              <th>分类</th>
              <th>销量</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td><span class="rank top1">1</span></td>
              <td class="product-name">考研英语历年真题</td>
              <td class="product-category">学习资料</td>
              <td class="sales-count">156</td>
            </tr>
            <tr>
              <td><span class="rank top2">2</span></td>
              <td class="product-name">机械工程原理（二手）</td>
              <td class="product-category">教材</td>
              <td class="sales-count">128</td>
            </tr>
            <tr>
              <td><span class="rank top3">3</span></td>
              <td class="product-name">惠普暗影精灵游戏本</td>
              <td class="product-category">电子产品</td>
              <td class="sales-count">95</td>
            </tr>
            <tr>
              <td><span class="rank">4</span></td>
              <td class="product-name">Nike Air运动鞋</td>
              <td class="product-category">运动装备</td>
              <td class="sales-count">87</td>
            </tr>
            <tr>
              <td><span class="rank">5</span></td>
              <td class="product-name">华为MateBook 14</td>
              <td class="product-category">电子产品</td>
              <td class="sales-count">76</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="footer">
        校园交易平台 © 2023 数据更新时间: 2023-06-15
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { Chart, registerables } from 'chart.js'

// 注册Chart.js的所有功能
Chart.register(...registerables)

// 模拟数据
const stats = ref({
  totalUsers: 8742,
  activeUsers: 5283,
  totalOrders: 12647,
  monthlySales: 385642
})

const popularProducts = ref([
  { id: 1, name: "考研英语历年真题", category: "学习资料", salesCount: 156 },
  { id: 2, name: "机械工程原理（二手）", category: "教材", salesCount: 128 },
  { id: 3, name: "惠普暗影精灵游戏本", category: "电子产品", salesCount: 95 },
  { id: 4, name: "Nike Air运动鞋", category: "运动装备", salesCount: 87 },
  { id: 5, name: "华为MateBook 14", category: "电子产品", salesCount: 76 }
])

onMounted(() => {
  // 初始化图表
  initCharts()
})

function initCharts() {
  // 用户活跃度趋势图
  const activityCtx = document.getElementById('activityChart')?.getContext('2d')
  if (activityCtx) {
    new Chart(activityCtx, {
      type: 'line',
      data: {
        labels: ['1月', '2月', '3月', '4月', '5月', '6月'],
        datasets: [{
          label: '日活跃用户',
          data: [1200, 1900, 3000, 5000, 4500, 5283],
          borderColor: '#FF85A2',
          backgroundColor: 'rgba(255, 209, 220, 0.3)',
          tension: 0.4,
          fill: true,
          pointBackgroundColor: '#FF85A2',
          pointRadius: 4,
          pointHoverRadius: 6
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        animation: false,
        plugins: {
          legend: {
            position: 'top',
            labels: {
              fontSize: 12,
              color: '#555'
            }
          },
          tooltip: {
            backgroundColor: 'rgba(255, 255, 255, 0.9)',
            titleColor: '#333',
            bodyColor: '#666',
            borderColor: '#ddd',
            borderWidth: 1,
            padding: 10,
            boxPadding: 5,
            usePointStyle: true,
            callbacks: {
              label: function(context) {
                return `活跃用户: ${context.raw}`
              }
            }
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            grid: {
              color: 'rgba(0, 0, 0, 0.05)'
            },
            ticks: {
              color: '#666',
              fontSize: 11
            }
          },
          x: {
            grid: {
              display: false
            },
            ticks: {
              color: '#666',
              fontSize: 11
            }
          }
        }
      }
    })
  }

  // 月度交易量图
  const transactionCtx = document.getElementById('transactionChart')?.getContext('2d')
  if (transactionCtx) {
    new Chart(transactionCtx, {
      type: 'bar',
      data: {
        labels: ['1月', '2月', '3月', '4月', '5月', '6月'],
        datasets: [{
          label: '月度交易量(万元)',
          data: [45, 59, 80, 81, 56, 38.5],
          backgroundColor: [
            'rgba(179, 224, 255, 0.6)',
            'rgba(179, 224, 255, 0.7)',
            'rgba(179, 224, 255, 0.8)',
            'rgba(179, 224, 255, 0.9)',
            'rgba(179, 224, 255, 0.7)',
            'rgba(179, 224, 255, 0.8)'
          ],
          borderColor: [
            'rgba(77, 166, 255, 1)',
            'rgba(77, 166, 255, 1)',
            'rgba(77, 166, 255, 1)',
            'rgba(77, 166, 255, 1)',
            'rgba(77, 166, 255, 1)',
            'rgba(77, 166, 255, 1)'
          ],
          borderWidth: 1,
          borderRadius: 4
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        animation: false,
        plugins: {
          legend: {
            position: 'top',
            labels: {
              fontSize: 12,
              color: '#555'
            }
          },
          tooltip: {
            backgroundColor: 'rgba(255, 255, 255, 0.9)',
            titleColor: '#333',
            bodyColor: '#666',
            borderColor: '#ddd',
            borderWidth: 1,
            padding: 10,
            callbacks: {
              label: function(context) {
                return `交易量: ${context.raw}万元`
              }
            }
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            grid: {
              color: 'rgba(0, 0, 0, 0.05)'
            },
            ticks: {
              color: '#666',
              fontSize: 11
            }
          },
          x: {
            grid: {
              display: false
            },
            ticks: {
              color: '#666',
              fontSize: 11
            }
          }
        }
      }
    })
  }
}
</script>

<style scoped>
.app-container {
  margin: 0;
  padding: 0;
  background-color: #f5f7fa;
  font-family: "微软雅黑", "Arial", sans-serif;
  min-height: 100vh;
}

/* White top navigation bar */
.top-navbar {
  background-color: white;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
  padding: 15px 0;
  position: sticky;
  top: 0;
  z-index: 100;
}

.navbar-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 30px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.left-section {
  display: flex;
  align-items: center;
  gap: 10px;
}

.right-section {
  display: flex;
  align-items: center;
}

.top-navbar .icon {
  display: flex;
  align-items: center;
}

.top-navbar .title {
  font-size: 20px;
  font-weight: bold;
  color: #333;
}

.container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 30px;
  box-sizing: border-box;
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  position: relative;
  z-index: 1;
}

.container::before {
  content: '';
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: 
    linear-gradient(rgba(255, 255, 255, 0.3), rgba(255, 255, 255, 0.3)), 
    url('https://s.coze.cn/image/wK-W6ayfWKw/') no-repeat center center;
  background-size: cover;
  z-index: -1;
}

.header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px 0;
  margin-bottom: 30px;
}

.logo {
  display: flex;
  align-items: center;
  gap: 15px;
}

/* 新增页面标题样式 */
.page-title-container {
  width: 100%;
}

.page-title {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
}

.title-text {
  font-size: 32px;
  font-weight: bold;
  color: #333;
  position: relative;
  padding-bottom: 8px;
  margin-bottom: 15px;
  font-family: '1-中等-思源黑体', sans-serif;
}

.title-line {
  display: block;
  width: 60px;
  height: 4px;
  background: linear-gradient(90deg, #FF85A2, #FFD1DC);
  border-radius: 2px;
}

.stats-container {
  display: flex;
  flex-wrap: wrap;
  gap: 20px;
  margin-bottom: 30px;
}

.stat-card {
  flex: 1;
  min-width: 200px;
  background-color: white;
  border-radius: 12px;
  padding: 20px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
  border-left: 5px solid #FFD1DC;
}

.stat-title {
  font-size: 16px;
  color: #666;
  margin-bottom: 10px;
}

.stat-value {
  font-size: 32px;
  font-weight: bold;
  color: #FF85A2;
  margin-bottom: 5px;
}

.stat-desc {
  font-size: 14px;
  color: #888;
}

.charts-container {
  display: flex;
  flex-wrap: wrap;
  gap: 20px;
  margin-bottom: 30px;
}

.chart-wrapper {
  flex: 1;
  min-width: 400px;
  background-color: white;
  border-radius: 12px;
  padding: 20px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
}

.chart-title {
  font-size: 18px;
  color: #333;
  margin-bottom: 15px;
  font-weight: bold;
}

.chart-container {
  height: 350px;
  width: 100%;
}

.table-container {
  background-color: white;
  border-radius: 12px;
  padding: 20px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
}

.table-title {
  font-size: 18px;
  color: #333;
  margin-bottom: 15px;
  font-weight: bold;
}

.hot-products {
  width: 100%;
  border-collapse: collapse;
}

.hot-products th, .hot-products td {
  padding: 12px 20px;
  text-align: left;
  border-bottom: 1px solid #eee;
}

.hot-products th {
  font-weight: bold;
  color: #555;
  background-color: #f9f9f9;
}

.hot-products tr:nth-child(even) {
  background-color: #fafafa;
}

.rank {
  color: #FF85A2;
  font-weight: bold;
  display: inline-block;
  width: 24px;
  height: 24px;
  line-height: 24px;
  text-align: center;
  background-color: #FFE8F0;
  border-radius: 50%;
}

.rank.top1 {
  background-color: #FFD1DC;
  color: #FF6B90;
}

.rank.top2 {
  background-color: #B3E0FF;
  color: #4DA6FF;
}

.rank.top3 {
  background-color: #FFE5CC;
  color: #FF9500;
}

.product-name {
  font-weight: 500;
}

.product-category {
  color: #757575;
  font-size: 14px;
}

.sales-count {
  font-weight: bold;
  color: #FF85A2;
}

.footer {
  margin-top: 30px;
  text-align: center;
  color: #757575;
  font-size: 14px;
  padding-top: 20px;
  border-top: 1px solid #eee;
}

@font-face {
  font-family: '1-中等-思源黑体';
  src: url('https://lf-coze-web-cdn.coze.cn/obj/eden-cn/lm-lgvj/ljhwZthlaukjlkulzlp/fonts/image-canvas-fonts/1-中等-思源黑体.woff2') format('woff2');
  font-display: swap;
}
</style>      