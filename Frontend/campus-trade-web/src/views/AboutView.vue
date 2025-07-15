<template>
  <div class="about">
    <div class="hero-section">
      <el-card class="hero-card">
        <div class="hero-content">
          <h1>关于校园交易平台</h1>
          <p>致力于为大学生提供安全、便捷的校园内物品交易服务</p>
        </div>
      </el-card>
    </div>

    <div class="content-section">
      <el-row :gutter="40">
        <el-col :span="12" :xs="24">
          <div class="about-text">
            <h2>我们的使命</h2>
            <p>
              校园交易平台是专门为大学生打造的二手物品交易平台。我们相信，在校园这个特殊的环境中，
              同学们之间的交易应该更加安全、便捷和值得信赖。
            </p>
            <p>
              通过我们的平台，你可以轻松出售不再需要的物品，也可以以优惠的价格购买到心仪的商品。
              我们致力于减少浪费，促进资源的有效利用，同时为大学生提供经济实惠的购物选择。
            </p>
          </div>
        </el-col>

        <el-col :span="12" :xs="24">
          <div class="features-list">
            <h2>平台优势</h2>
            <el-space direction="vertical" size="large" style="width: 100%">
              <div class="feature-item">
                <el-icon size="24" color="#409EFF" class="feature-icon">
                  <UserFilled />
                </el-icon>
                <div class="feature-text">
                  <h3>校园身份认证</h3>
                  <p>所有用户都需要通过学生身份验证，确保交易安全</p>
                </div>
              </div>

              <div class="feature-item">
                <el-icon size="24" color="#67C23A" class="feature-icon">
                  <LocationFilled />
                </el-icon>
                <div class="feature-text">
                  <h3>校园内交易</h3>
                  <p>支持面对面交易，减少物流成本和风险</p>
                </div>
              </div>

              <div class="feature-item">
                <el-icon size="24" color="#E6A23C" class="feature-icon">
                  <MessageBox />
                </el-icon>
                <div class="feature-text">
                  <h3>便捷沟通</h3>
                  <p>内置聊天系统，买卖双方可以实时沟通</p>
                </div>
              </div>

              <div class="feature-item">
                <el-icon size="24" color="#F56C6C" class="feature-icon">
                  <Lock />
                </el-icon>
                <div class="feature-text">
                  <h3>交易保障</h3>
                  <p>完善的评价系统和投诉机制，保障交易双方权益</p>
                </div>
              </div>
            </el-space>
          </div>
        </el-col>
      </el-row>
    </div>

    <div class="stats-section">
      <h2>平台数据</h2>
      <el-row :gutter="20">
        <el-col :span="6" :xs="12" :sm="6">
          <el-card class="stat-card">
            <el-statistic title="注册用户" :value="userCount" />
          </el-card>
        </el-col>
        <el-col :span="6" :xs="12" :sm="6">
          <el-card class="stat-card">
            <el-statistic title="在售商品" :value="productCount" />
          </el-card>
        </el-col>
        <el-col :span="6" :xs="12" :sm="6">
          <el-card class="stat-card">
            <el-statistic title="成功交易" :value="transactionCount" />
          </el-card>
        </el-col>
        <el-col :span="6" :xs="12" :sm="6">
          <el-card class="stat-card">
            <el-statistic title="用户满意度" :value="satisfaction" suffix="%" />
          </el-card>
        </el-col>
      </el-row>
    </div>

    <div class="team-section">
      <h2>开发团队</h2>
      <el-row :gutter="20" justify="center">
        <el-col :span="8" :xs="24" :sm="12" :md="8" v-for="member in teamMembers" :key="member.id">
          <el-card class="team-card">
            <div class="team-avatar">
              <el-avatar :size="80" :src="member.avatar">
                {{ member.name.charAt(0) }}
              </el-avatar>
            </div>
            <h3>{{ member.name }}</h3>
            <p class="role">{{ member.role }}</p>
            <p class="description">{{ member.description }}</p>
          </el-card>
        </el-col>
      </el-row>
    </div>

    <div class="contact-section">
      <el-card class="contact-card">
        <h2>联系我们</h2>
        <el-row :gutter="40">
          <el-col :span="12" :xs="24">
            <div class="contact-info">
              <div class="contact-item">
                <el-icon><Message /></el-icon>
                <span>邮箱：support@campus-trade.edu</span>
              </div>
              <div class="contact-item">
                <el-icon><Phone /></el-icon>
                <span>电话：400-123-4567</span>
              </div>
              <div class="contact-item">
                <el-icon><Location /></el-icon>
                <span>地址：某某大学创新创业中心</span>
              </div>
            </div>
          </el-col>
          <el-col :span="12" :xs="24">
            <div class="feedback-form">
              <h3>意见反馈</h3>
              <el-form :model="feedbackForm" label-width="80px">
                <el-form-item label="姓名">
                  <el-input v-model="feedbackForm.name" />
                </el-form-item>
                <el-form-item label="邮箱">
                  <el-input v-model="feedbackForm.email" />
                </el-form-item>
                <el-form-item label="反馈内容">
                  <el-input
                    v-model="feedbackForm.message"
                    type="textarea"
                    :rows="4"
                    placeholder="请输入您的意见或建议"
                  />
                </el-form-item>
                <el-form-item>
                  <el-button type="primary" @click="submitFeedback">提交反馈</el-button>
                </el-form-item>
              </el-form>
            </div>
          </el-col>
        </el-row>
      </el-card>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { ref, reactive } from 'vue'
  import { ElMessage } from 'element-plus'
  import {
    UserFilled,
    LocationFilled,
    MessageBox,
    Lock,
    Message,
    Phone,
    Location,
  } from '@element-plus/icons-vue'

  // 统计数据
  const userCount = ref(5678)
  const productCount = ref(1234)
  const transactionCount = ref(987)
  const satisfaction = ref(96)

  // 团队成员
  const teamMembers = ref([
    {
      id: 1,
      name: '张三',
      role: '产品经理',
      description: '负责产品规划和用户体验设计',
      avatar: '',
    },
    {
      id: 2,
      name: '李四',
      role: '前端开发',
      description: '负责前端界面开发和交互实现',
      avatar: '',
    },
    {
      id: 3,
      name: '王五',
      role: '后端开发',
      description: '负责服务器端开发和数据库设计',
      avatar: '',
    },
  ])

  // 反馈表单
  const feedbackForm = reactive({
    name: '',
    email: '',
    message: '',
  })

  // 提交反馈
  const submitFeedback = () => {
    if (!feedbackForm.name || !feedbackForm.email || !feedbackForm.message) {
      ElMessage.warning('请填写完整信息')
      return
    }

    // 这里可以调用 API 提交反馈
    ElMessage.success('感谢您的反馈，我们会认真处理！')

    // 重置表单
    feedbackForm.name = ''
    feedbackForm.email = ''
    feedbackForm.message = ''
  }
</script>

<style scoped>
  .about {
    width: 100%;
    padding: 20px;
    box-sizing: border-box;
  }

  .hero-section {
    margin-bottom: 40px;
  }

  .hero-card {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    border: none;
  }

  .hero-card :deep(.el-card__body) {
    padding: 60px 40px;
  }

  .hero-content {
    text-align: center;
  }

  .hero-content h1 {
    font-size: 36px;
    margin-bottom: 20px;
  }

  .hero-content p {
    font-size: 18px;
    opacity: 0.9;
  }

  .content-section {
    margin-bottom: 60px;
  }

  .about-text h2,
  .features-list h2 {
    color: #333;
    margin-bottom: 20px;
    font-size: 24px;
  }

  .about-text p {
    color: #666;
    line-height: 1.8;
    margin-bottom: 16px;
  }

  .feature-item {
    display: flex;
    align-items: flex-start;
    gap: 16px;
  }

  .feature-icon {
    margin-top: 4px;
  }

  .feature-text h3 {
    margin: 0 0 8px 0;
    color: #333;
    font-size: 16px;
  }

  .feature-text p {
    margin: 0;
    color: #666;
    line-height: 1.6;
  }

  .stats-section {
    margin-bottom: 60px;
    text-align: center;
  }

  .stats-section h2 {
    color: #333;
    margin-bottom: 30px;
    font-size: 28px;
  }

  .stat-card {
    text-align: center;
    margin-bottom: 20px;
  }

  .team-section {
    margin-bottom: 60px;
    text-align: center;
  }

  .team-section h2 {
    color: #333;
    margin-bottom: 30px;
    font-size: 28px;
  }

  .team-card {
    text-align: center;
    margin-bottom: 20px;
    padding: 20px;
  }

  .team-avatar {
    margin-bottom: 16px;
  }

  .team-card h3 {
    margin: 0 0 8px 0;
    color: #333;
  }

  .role {
    color: #409eff;
    font-weight: bold;
    margin: 0 0 12px 0;
  }

  .description {
    color: #666;
    line-height: 1.6;
    margin: 0;
  }

  .contact-section {
    margin-bottom: 40px;
  }

  .contact-card h2 {
    text-align: center;
    color: #333;
    margin-bottom: 30px;
    font-size: 24px;
  }

  .contact-info {
    padding: 20px 0;
  }

  .contact-item {
    display: flex;
    align-items: center;
    gap: 12px;
    margin-bottom: 20px;
    font-size: 16px;
    color: #666;
  }

  .feedback-form h3 {
    color: #333;
    margin-bottom: 20px;
  }

  @media (max-width: 768px) {
    .hero-content h1 {
      font-size: 28px;
    }

    .hero-content p {
      font-size: 16px;
    }

    .content-section {
      margin-bottom: 40px;
    }

    .about-text,
    .features-list {
      margin-bottom: 30px;
    }
  }
</style>
