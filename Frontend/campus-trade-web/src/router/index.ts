import { createRouter, createWebHistory } from 'vue-router'
import { useUserStore } from '@/stores/user'
import HomeView from '@/views/HomeView.vue'
import LoginView from '@/views/LoginView.vue'
import UserDetailView from '@/views/UserDetailView.vue'
import Welcome from '@/views/Welcome.vue'
import OrderView from '@/views/order.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
       name: 'welcome',
       component: Welcome,
       meta: { guest: true },
     },
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/login',
      name: 'login',
      component: LoginView,
      meta: { guest: true },
    },
    {
      path: '/userdetailview',
      name: 'userdetailview',
      component: UserDetailView,
    },
    {
      path: '/about',
      name: 'about',
      component: () => import('@/views/AboutView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/order',
      name: 'order',
      component: OrderView,
    },
  ],
})

// 路由守卫
router.beforeEach((to, from, next) => {
  const userStore = useUserStore()

  // 初始化认证状态
  userStore.initializeAuth()

  const isLoggedIn = userStore.isLoggedIn

  // 如果已登录用户访问登录页面，重定向到首页
  if (to.meta.guest && isLoggedIn) {
    next('/')
    return
  }

  next()
})

export default router
