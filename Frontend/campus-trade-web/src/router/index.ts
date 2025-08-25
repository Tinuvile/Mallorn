import { createRouter, createWebHistory } from 'vue-router'
import { useUserStore } from '@/stores/user'
import HomeView from '@/views/HomeView.vue'
import LoginView from '@/views/LoginView.vue'
import UserDetailView from '@/views/UserDetailView.vue'
import DataWatchingView from '@/views/dataWatchingView.vue'
import Welcome from '@/views/Welcome.vue'
import OrderView from '@/views/order.vue'
import GoodsDetails from '@/views/GoodsDetails.vue'
import ConfirmOrderView from '@/views/ConfirmOrderView.vue'
import GoodsReleaseView from '@/views/GoodsReleaseView.vue'
import AdminModeratorView from '@/views/AdminModeratorView.vue'
import SystemAuditView from '@/views/SystemAuditView.vue'
import AdminDashboardView from '@/views/AdminDashboardView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    ///{
    ///  path: '/',
    ///   name: 'welcome',
    ///   component: Welcome,
    ///   meta: { guest: true },
    /// },
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
      path: '/datawatchingview',
      name: 'datawatchingview',
      component: DataWatchingView,
    },
    {
      path: '/confirmorderview',
      name: 'confirmorderview',
      component: ConfirmOrderView,
    },
    {
      path: '/goodsreleaseview',
      name: 'goodsreleaseview',
      component: GoodsReleaseView,
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
    {
    path: '/goods/:id',  // 动态路由参数:商品ID
    name: 'goodsDetails',
    component: GoodsDetails,
    props: true  // 允许将路由参数作为props传递
    },
    {
      path: '/admin',
      name: 'adminDashboard', 
      component: AdminDashboardView,
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/moderator',
      name: 'adminModerator',
      component: AdminModeratorView,
      meta: { requiresAuth: true, requiresRole: 'moderator' }
    },
    {
      path: '/admin/system-audit',
      name: 'systemAudit',
      component: SystemAuditView,
      meta: { requiresAuth: true, requiresRole: 'system_admin' }
    }
  ],
   // 添加滚动行为配置
  scrollBehavior(to, from, savedPosition) {
    // 始终滚动到顶部
    return { top: 0 }
  }
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
