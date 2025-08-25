<template>
  <v-app>
    <!-- 顶部导航栏 -->
    <v-app-bar color="primary" dark height="64">
      <v-toolbar-title class="text-h6">
        Campus Secondhand - 管理员控制台
      </v-toolbar-title>
      <v-spacer></v-spacer>
      <v-btn color="white" text to="/">
        <v-icon left>mdi-home</v-icon>
        返回主页
      </v-btn>
    </v-app-bar>

    <!-- 主内容区 -->
    <v-main>
      <v-container>
        <AdminDashboard />
      </v-container>
    </v-main>
  </v-app>
</template>

<script setup lang="ts">
import AdminDashboard from '@/components/AdminDashboard.vue'
</script>
