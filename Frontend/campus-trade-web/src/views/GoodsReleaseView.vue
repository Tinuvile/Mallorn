<template>
  <div class="full-page">
    <header class="navbar">
      <span class="icon">
        <svg width="48px" height="48px" stroke-width="1.5" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" color="#ffffff">
          <circle cx="12" cy="12" r="10" stroke="#ffffff" stroke-width="1.5"></circle>
          <path d="M7.63262 3.06689C8.98567 3.35733 9.99999 4.56025 9.99999 6.00007C9.99999 7.65693 8.65685 9.00007 6.99999 9.00007C5.4512 9.00007 4.17653 7.82641 4.01685 6.31997" stroke="#ffffff" stroke-width="1.5"></path>
          <path d="M22 13.0505C21.3647 12.4022 20.4793 12 19.5 12C17.567 12 16 13.567 16 15.5C16 17.2632 17.3039 18.7219 19 18.9646" stroke="#ffffff" stroke-width="1.5" ></path>
          <path d="M14.5 8.51L14.51 8.49889" stroke="#ffffff" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
          <path d="M10 17C11.1046 17 12 16.1046 12 15C12 13.8954 11.1046 13 10 13C8.89543 13 8 13.8954 8 15C8 16.1046 8.89543 17 10 17Z" stroke="#ffffff" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
        </svg>
      </span>
      <span class="title">Campus Secondhand</span>
      <v-btn 
        class="logout-btn ml-auto"
        icon
        to="/order"
      >
        <v-icon color="black">mdi-exit-to-app</v-icon>
      </v-btn>
    </header>
    <v-card flat class="full-height">
      <div class="title-container">
        <v-card-title class="product-title">发布商品</v-card-title>
      </div>
      
      <v-divider class="my-3 custom-divider"></v-divider>

      <v-row class="ma-0 full-height">
        <!-- 左侧：图片上传区域 -->
        <v-col cols="12" md="4" class="pa-6">
          <h2 class="text-h6 mb-4">上传商品图片</h2>
          <v-card outlined class="pa-4 rounded-lg image-upload-card">
            <v-btn
              color="#cadefc"
              size="large"
              class="mb-4 mx-auto d-block select-image-btn"
              @click="fileInput?.$el.click()"
            >
              选择图片
            </v-btn>
            <v-file-input
              ref="fileInput"
              v-model="images"
              multiple
              accept="image/*"
              style="display: none"
              @change="handleImageUpload"
            ></v-file-input>
            <div class="image-grid-container rounded-lg">
              <div class="image-grid">
                <div 
                  v-for="(preview, index) in imagePreviews" 
                  :key="index" 
                  class="image-item dashed-border"
                >
                  <div class="placeholder-content">
                    <v-icon size="48" color="grey">mdi-image</v-icon>
                    <div class="placeholder-text">图片 {{ index + 1 }}</div>
                  </div>
                  <v-btn
                    icon
                    small
                    color="error"
                    class="delete-btn"
                    @click="removeImage(index)"
                  >
                    <v-icon>mdi-delete</v-icon>
                  </v-btn>
                </div>
                <div 
                  v-for="n in 4 - imagePreviews.length" 
                  :key="'empty-' + n" 
                  class="image-item empty dashed-border"
                >
                  <v-icon size="48" color="grey">mdi-plus</v-icon>
                </div>
              </div>
            </div>
      
            <div class="text-caption text-grey mt-2">
              最多可上传4张图片，建议上传清晰、真实的商品图片
            </div>
          </v-card>
        </v-col>

        <!-- 右侧：商品信息表单 -->
        <v-col cols="12" md="8" class="pa-0">
          <div class="d-flex h-100">
            <v-divider vertical class="mr-4 custom-vertical-divider"></v-divider>
            <div class="pa-6 flex-grow-1">
              <h2 class="text-h6 mb-4">商品标题</h2>
              <v-text-field
                v-model="productTitle"
                label="请输入商品名称"
                variant="outlined"
                required
                class="right-input-border" 
              ></v-text-field>

              <h2 class="text-h6 mb-4 mt-6">商品描述</h2>
              <v-textarea
                v-model="productDescription"
                label="商品描述"
                variant="outlined"
                rows="3"
                placeholder="请详细描述商品信息，包括新旧程度、使用情况等"
                required
                class="right-input-border" 
              ></v-textarea>

              <h2 class="text-h6 mb-4 mt-6">基础价格</h2>
              <v-text-field
                v-model="price"
                label="价格"
                variant="outlined"
                type="number"
                prefix="¥"
                required
                class="right-input-border" 
              ></v-text-field>

              <h2 class="text-h6 mb-4 mt-6">商品分类</h2>
              <v-select
                v-model="category"
                :items="categories"
                label="商品分类"
                variant="outlined"
                placeholder="请选择分类"
                required
                class="right-input-border" 
              ></v-select>

              <v-card-actions class="px-0 mt-8">
                <v-spacer></v-spacer>
                <v-btn color="primary" size="large" @click="submitProduct" class="submit-btn-class">发布商品</v-btn>
              </v-card-actions>
            </div>
          </div>
        </v-col>
      </v-row>
    </v-card>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import type { VFileInput } from 'vuetify/components';

// 图片上传相关
const images = ref<File[]>([]);
const imagePreviews = ref<string[]>([]);
const fileInput = ref<InstanceType<typeof VFileInput>>();

// 商品信息
const productTitle = ref('');
const productDescription = ref('');
const price = ref<number | null>(null);
const category = ref('');

// 商品分类选项
const categories = ref([
  '电子产品',
  '书籍资料',
  '服装配饰',
  '生活用品',
  '体育用品',
  '其他'
]);

// 处理图片上传
const handleImageUpload = (files: File[]) => {
  imagePreviews.value = [];
  if (files) {
    // 限制最多上传4张图片
    const filesToUpload = files.slice(0, 4);
    filesToUpload.forEach(file => {
      const reader = new FileReader();
      reader.onload = (e) => {
        if (e.target?.result) {
          imagePreviews.value.push(e.target.result as string);
        }
      };
      reader.readAsDataURL(file);
    });
    images.value = filesToUpload;
  }
};

// 删除图片
const removeImage = (index: number) => {
  imagePreviews.value.splice(index, 1);
  images.value.splice(index, 1);
};

// 提交商品
const submitProduct = () => {
  if (!productTitle.value || !productDescription.value || price.value === null || !category.value) {
    alert('请填写完整商品信息');
    return;
  }

  if (imagePreviews.value.length === 0) {
    alert('请至少上传一张商品图片');
    return;
  }

  const productData = {
    title: productTitle.value,
    description: productDescription.value,
    price: price.value,
    category: category.value,
    images: images.value
  };

  console.log('提交的商品数据:', productData);
  alert('商品发布成功!');
};
</script>

<style scoped>
/* 全局字体设置 */
body, .v-application {
  font-family: "Microsoft YaHei", "微软雅黑", sans-serif !important;
}

/* 导航栏样式 */
.navbar {
  position: relative; 
  width: 100%;
  height: 60px;
  margin:  0;
  padding: 0 16px;
  background-color:#cadefc;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  z-index: 1000; 
  display: flex;
  align-items: center;
  justify-content:flex-start;
  font-family: "Microsoft YaHei", "微软雅黑", sans-serif;
}

/* 图标和标题 */
.icon svg {
  width: 48px;
  height: 48px;
}

.title {
  font-size: 32px;
  font-weight: bold;
  margin-left: 16px;
  color: white !important;
  font-family: "Microsoft YaHei", "微软雅黑", sans-serif;
}

/* 发布商品标题容器 */
.title-container {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 60px;
}

/* 发布商品标题样式 */
.product-title {
  font-size: 1.8rem !important;
  font-weight: 500;
  width: 100%;
  text-align: center;
  padding: 0 !important;
  margin: 0 !important;
}

.select-image-btn {
  border: 2px dashed #1e88e5 !important;
  background-color: #cadefc !important;
  color: #1e88e5 !important;
  font-weight: bold;
  text-transform: none;
}

.select-image-btn:hover {
  background-color: #bbdefb !important;
  border-style: solid !important;
}

.submit-btn-class{
  background-color: #cadefc !important;
  color: white !important;
  font-weight: bold !important;
  border-radius: 8px !important;
  box-shadow: 0 3px 5px rgba(76, 175, 80, 0.3) !important;
  transition: all 0.2s ease !important;
}

/* 分割线样式 */
.v-divider {
  margin-top: 0 !important;
  margin-bottom: 8px !important;
}

.custom-divider {
  border-color: #fb7c7c !important;
  border-width: 2px !important;
}

.custom-vertical-divider {
  border-color: #1f1b1b !important;
  border-width: 1px !important;
  margin-right: 16px !important;
  height: 100% !important;
  align-self: stretch !important;
}

/* 页面布局样式 */
.full-page {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  overflow: auto;
  background-color: #f5f5f5;
}

.full-height {
  height: 100%;
}

.v-card {
  height: 100%;
  border-radius: 0 !important;
}

.v-col {
  overflow-y: auto;
}

/* 移除所有默认边距 */
body {
  margin: 0;
  padding: 0;
}

/* 右侧输入框统一边框样式 */
.right-input-border :deep(.v-field) {
  border: 1px solid #cadefc !important;
  border-radius: 8px !important;
  transition: all 0.3s ease;
}

/* 聚焦状态 */
.right-input-border :deep(.v-field--focused) {
  border-color: #1e88e5 !important;
  box-shadow: 0 0 0 3px rgba(30, 136, 229, 0.2) !important;
}

/* 悬停效果 */
.right-input-border :deep(.v-field:hover) {
  border-color: #90caf9 !important;
}

/* 图片上传卡片边框样式 */
.v-card--outlined {
  border-color: rgba(0, 0, 0, 0.12) !important;
}

/* 新增的图片网格样式 */
.image-grid-container {
  height: 400px;
}

.image-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  grid-template-rows: repeat(2, 1fr);
  gap: 12px; /* 增加网格项之间的间距 */
  background-color: transparent; /* 移除背景色 */
}

.image-item {
  position: relative;
  aspect-ratio: 1/1;
  background-color: white;
  display: flex;
  align-items: center;
  justify-content: center;
}

/* 虚线边框样式 */
.dashed-border {
  border: 2px dashed #c4c4c4;
  border-radius: 4px;
}

.placeholder-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

.placeholder-text {
  margin-top: 8px;
  font-size: 0.8rem;
  color: #757575;
}

.empty {
  background-color: #f9f9f9;
}

.delete-btn {
  position: absolute;
  top: 4px;
  right: 4px;
  z-index: 2;
}

.image-upload-card {
  height: auto !important; /* 覆盖原有的固定高度 */
  min-height: 400px; /* 设置最小高度以确保良好的视觉效果 */
  background-color: #f0f2f6 !important; 
    border: 1px solid #cadefc !important; 
}
</style>