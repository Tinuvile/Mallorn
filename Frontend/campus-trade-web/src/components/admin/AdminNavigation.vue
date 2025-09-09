<template>
  <div class="admin-navigation">
    <h3>管理员功能</h3>
    <nav class="nav-menu">
      <router-link to="/admin" class="nav-link" :class="{ active: $route.path === '/admin' }">
        <span class="nav-icon">📊</span>
        <span class="nav-text">数据看板</span>
      </router-link>
      
      <router-link to="/admin/moderator" class="nav-link" :class="{ active: $route.path === '/admin/moderator' }">
        <span class="nav-icon">🛠️</span>
        <span class="nav-text">内容管理</span>
      </router-link>
    </nav>
  </div>
</template>

<style scoped>
.admin-navigation {
  background-color: white;
  border-radius: 12px;
  padding: 20px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
  margin-bottom: 24px;
}

.admin-navigation h3 {
  margin: 0 0 16px 0;
  font-size: 18px;
  color: #333;
  font-weight: 600;
}

.nav-menu {
  display: flex;
  gap: 12px;
}

.nav-link {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 16px;
  text-decoration: none;
  color: #666;
  background-color: #f9f9f9;
  border-radius: 8px;
  transition: all 0.2s ease;
  font-size: 14px;
}

.nav-link:hover {
  background-color: #e9f5ff;
  color: #1976d2;
}

.nav-link.active {
  background-color: #FF85A2;
  color: white;
}

.nav-icon {
  font-size: 16px;
}

.nav-text {
  font-weight: 500;
}

@media (max-width: 768px) {
  .nav-menu {
    flex-direction: column;
  }
  
  .nav-link {
    justify-content: center;
  }
}
</style>
