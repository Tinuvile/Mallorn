<template>
  <v-app>
    <!-- 导航栏 -->
    <header class="navbar">
      <span class="icon">
        <svg
          width="48px"
          height="48px"
          stroke-width="1.5"
          viewBox="0 0 24 24"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
          color="#ffffff"
        >
          <circle cx="12" cy="12" r="10" stroke="#ffffff" stroke-width="1.5"></circle>
          <path
            d="M7.63262 3.06689C8.98567 3.35733 9.99999 4.56025 9.99999 6.00007C9.99999 7.65693 8.65685 9.00007 6.99999 9.00007C5.4512 9.00007 4.17653 7.82641 4.01685 6.31997"
            stroke="#ffffff"
            stroke-width="1.5"
          ></path>
          <path
            d="M22 13.0505C21.3647 12.4022 20.4793 12 19.5 12C17.567 12 16 13.567 16 15.5C16 17.2632 17.3039 18.7219 19 18.9646"
            stroke="#ffffff"
            stroke-width="1.5"
          ></path>
          <path
            d="M14.5 8.51L14.51 8.49889"
            stroke="#ffffff"
            stroke-width="1.5"
            stroke-linecap="round"
            stroke-linejoin="round"
          ></path>
          <path
            d="M10 17C11.1046 17 12 16.1046 12 15C12 13.8954 11.1046 13 10 13C8.89543 13 8 13.8954 8 15C8 16.1046 8.89543 17 10 17Z"
            stroke="#ffffff"
            stroke-width="1.5"
            stroke-linecap="round"
            stroke-linejoin="round"
          ></path>
        </svg>
      </span>
      <span class="title">Campus Secondhand</span>
      <!-- 新增退出按钮 -->
      <v-btn class="logout-btn ml-auto" icon to="/order">
        <v-icon color="black">mdi-exit-to-app</v-icon>
      </v-btn>
    </header>

    <div class="simple-title-section">
      <h2>确认订单</h2>
      <v-divider color="#fb7c7c" thickness="4" class="mx-auto" style="width: 95%"></v-divider>
    </div>

    <!-- 主体内容 -->
    <v-main class="bg-gray-100 main-content">
      <v-container class="pa-0 h-full" fluid>
        <v-row class="h-full ma-0">
          <!-- 收货地址部分 -->
          <v-col cols="12" md="3" sm="12" class="h-full pl-2 pr-1 px-4">
            <v-card elevation="2" class="rounded-lg h-full d-flex flex-column">
              <!-- 收货地址部分 -->
              <!-- 收货地址卡片 -->
              <v-card-title class="bg-pink-50 text-pink-800 py-4 d-flex align-center">
                <!-- 保留默认 padding -->
                <span class="title-bar flex-grow-1 d-flex align-center justify-space-between">
                  <span class="address-title">收货地址</span>
                  <v-btn
                    color="pink darken-1"
                    class="ml-2 rounded-pill text-white"
                    small
                    min-width="60"
                    @click="editAddress = true"
                  >
                    修改
                  </v-btn>
                </span>
              </v-card-title>
              <v-card-text class="py-6 px-6 flex-grow-1">
                <div v-if="!address.recipient" class="text-center text-grey py-8">
                  <v-icon size="48" color="grey" class="mb-2">mdi-map-marker-off</v-icon>
                  <p>请添加收货地址</p>
                </div>
                <div v-else>
                  <p class="mb-3 text-body-1" style="font-weight: 450">
                    <strong>收件人：</strong>{{ address.recipient }}
                  </p>
                  <p class="mb-3 text-body-1" style="font-weight: 450">
                    <strong>电话：</strong>{{ address.phone }}
                  </p>
                  <p class="text-body-1" style="font-weight: 450">
                    <strong>地址：</strong>{{ address.location }}
                  </p>
                </div>
              </v-card-text>
            </v-card>
          </v-col>

          <!-- 订单信息部分 -->
          <v-col cols="12" md="6" sm="12" class="h-full px-4">
            <v-card elevation="2" class="rounded-lg h-full d-flex flex-column">
              <v-card-title class="bg-pink-50 text-pink-800 py-4 d-flex align-center">
                <span class="title-bar flex-grow-1">订单信息</span>
              </v-card-title>
              <v-card-text class="py-2 px-4 flex-grow-1 overflow-y-auto">
                <!-- 商品列表 -->
                <v-list dense class="py-0">
                  <v-list-item v-for="(item, index) in orderItems" :key="index" class="py-4 px-0">
                    <v-row class="align-start" dense>
                      <v-col cols="3" class="pr-0">
                        <v-img
                          :src="item.imageUrl"
                          :alt="item.name"
                          class="rounded-lg"
                          height="120"
                          width="120"
                        ></v-img>
                      </v-col>
                      <v-col cols="9" class="text-wrap pl-2">
                        <div class="font-medium text-body-1 mb-2" style="font-weight: 450">
                          商品名称：{{ item.name }}
                        </div>
                        <div class="text-gray-500 text-body-1 mb-2" style="font-weight: 450">
                          规格：{{ item.specification }}
                        </div>
                        <div class="text-gray-800 text-body-1 mb-2" style="font-weight: 450">
                          单价：¥{{ item.price.toFixed(2) }}
                        </div>
                        <div class="text-gray-800 text-body-1 mb-2" style="font-weight: 450">
                          数量：{{ item.quantity }}
                        </div>
                        <div
                          class="text-gray-800 text-body-1 font-bold mb-2"
                          style="font-weight: 450"
                        >
                          小计：¥{{ (item.price * item.quantity).toFixed(2) }}
                        </div>
                      </v-col>
                    </v-row>
                    <v-divider
                      v-if="index < orderItems.length - 1"
                      class="my-3 custom-divider"
                    ></v-divider>
                  </v-list-item>
                </v-list>
              </v-card-text>
            </v-card>
          </v-col>

          <!-- 费用明细部分 -->
          <v-col cols="12" md="3" sm="12" class="h-full pl-1 pr-2 px-4">
            <v-card elevation="2" class="rounded-lg h-full d-flex flex-column">
              <v-card-title class="bg-pink-50 text-pink-800 py-4 d-flex align-center">
                <span class="title-bar flex-grow-1">费用明细</span>
              </v-card-title>
              <v-card-text
                class="py-4 px-4 flex-grow-1 d-flex flex-column"
                style="height: calc(100% - 64px)"
              >
                <v-list dense class="flex-grow-0">
                  <v-list-item class="px-0">
                    <v-list-item-content class="d-flex justify-space-between align-center py-2">
                      <span class="text-body-1" style="font-weight: 450">商品金额</span>
                      <span class="font-medium text-body-1" style="font-weight: 450"
                        >¥{{ totalAmount.toFixed(2) }}</span
                      >
                    </v-list-item-content>
                  </v-list-item>
                  <v-list-item class="px-0">
                    <v-list-item-content class="d-flex justify-space-between align-center py-2">
                      <span class="text-body-1" style="font-weight: 450">优惠金额</span>
                      <span class="font-medium text-body-1 text-green-600" style="font-weight: 450"
                        >-¥{{ discountAmount.toFixed(2) }}</span
                      >
                    </v-list-item-content>
                  </v-list-item>
                  <v-list-item class="px-0">
                    <v-list-item-content class="d-flex justify-space-between align-center py-2">
                      <span class="text-body-1" style="font-weight: 450">运费</span>
                      <span class="font-medium text-body-1" style="font-weight: 450"
                        >¥{{ shippingFee.toFixed(2) }}</span
                      >
                    </v-list-item-content>
                  </v-list-item>
                  <v-divider class="my-3 custom-divider"></v-divider>
                  <v-list-item class="px-0">
                    <v-list-item-content class="d-flex justify-space-between align-center py-2">
                      <span
                        class="text-body-1 font-bold"
                        style="color: #ffdfdf; font-weight: 600; font-size: 1.25rem !important"
                        >实付款</span
                      >
                      <span
                        class="font-bold text-h6"
                        style="color: #ffdfdf; font-weight: 600; font-size: 1.5rem"
                        >¥{{ finalPayment.toFixed(2) }}</span
                      >
                    </v-list-item-content>
                  </v-list-item>
                </v-list>
                <v-btn
                  color="#ffdfdf"
                  class="mt-4 w-full text-white text-h6 align-center justify-center custom-btn-text"
                  height="48"
                  @click="submitOrder"
                >
                  <span style="font-weight: 600">提交订单</span>
                </v-btn>
              </v-card-text>
            </v-card>
          </v-col>
        </v-row>
      </v-container>
    </v-main>

    <!-- 新增：支付确认对话框 -->
    <v-dialog v-model="paymentConfirm" max-width="500px">
      <v-card>
        <v-card-title class="bg-pink-50 text-pink-800 py-3 d-flex align-center">
          <span class="title-bar flex-grow-1 d-flex align-center">支付确认</span>
        </v-card-title>
        <v-card-text class="py-1 px-4">
          <div class="mb-3 text-body-1">
            <p class="mt-2" style="font-size: 1.15rem !important; font-weight: 600">
              当前余额:
              <span class="font-bold" style="font-size: 1.15rem !important; font-weight: 600"
                >¥{{ currentBalance.toFixed(2) }}</span
              >
            </p>
            <p class="mt-2" style="font-size: 1.15rem !important; font-weight: 600">
              支付金额:
              <span class="font-bold" style="font-size: 1.15rem !important; font-weight: 600"
                >¥{{ finalPayment.toFixed(2) }}</span
              >
            </p>
            <p class="mt-2" style="font-size: 1.15rem !important; font-weight: 600">
              结后余额:
              <span class="font-bold" style="font-size: 1.15rem !important; font-weight: 600"
                >¥{{ (currentBalance - finalPayment).toFixed(2) }}</span
              >
            </p>
          </div>
        </v-card-text>
        <v-card-actions class="py-2 px-4 justify-space-between">
          <v-btn text @click="paymentConfirm = false" class="cancel-btn"> 取消 </v-btn>
          <v-btn color="grey" @click="deferPayment" class="defer-btn"> 暂缓支付 </v-btn>
          <v-btn color="pink" @click="confirmPayment" class="confirm-btn"> 确定支付 </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- 地址编辑对话框 -->
    <v-dialog v-model="editAddress" max-width="500px">
      <v-card>
        <v-card-title>编辑收货地址</v-card-title>
        <v-card-text>
          <v-form>
            <v-text-field
              v-model="tempAddress.recipient"
              label="收件人"
              required
              class="mb-4"
            ></v-text-field>
            <v-text-field
              v-model="tempAddress.phone"
              label="电话"
              required
              class="mb-4"
            ></v-text-field>
            <v-text-field v-model="tempAddress.location" label="地址" required></v-text-field>
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-btn text @click="editAddress = false">取消</v-btn>
          <v-btn color="pink" @click="saveAddress">保存</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
    <!-- 在模板部分添加余额不足弹窗 -->
    <v-dialog v-model="insufficientBalance" max-width="500px">
      <v-card>
        <v-card-title class="bg-red-50 text-red-800 py-3 d-flex align-center">
          <span class="title-bar flex-grow-1 d-flex align-center">余额不足</span>
        </v-card-title>
        <v-card-text class="py-4 px-4">
          <div class="text-body-1">
            <p class="mt-2" style="font-size: 1.15rem !important; font-weight: 600">
              您的当前余额不足，无法完成支付！
            </p>
            <p class="mt-2" style="font-size: 1.15rem !important; font-weight: 600">
              当前余额: ¥{{ currentBalance.toFixed(2) }}
            </p>
            <p class="mt-2" style="font-size: 1.15rem !important; font-weight: 600">
              需支付金额: ¥{{ finalPayment.toFixed(2) }}
            </p>
          </div>
        </v-card-text>
        <v-card-actions class="justify-end py-2 px-4">
          <v-btn color="red" @click="insufficientBalance = false" class="confirm-btn"> 确定 </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
    <!-- 提交成功对话框 -->
    <v-dialog v-model="orderSubmitted" max-width="500px">
      <v-card>
        <v-card-text class="text-center py-6">
          <v-icon color="green" size="64" class="mb-4">mdi-check-circle</v-icon>
          <h3 class="text-h6">订单提交成功！</h3>
          <p class="text-gray-500 mt-2">您的订单已成功提交，我们将尽快为您处理。</p>
        </v-card-text>
        <v-card-actions>
          <v-btn color="pink" to="/order" class="confirm-btn"> 确认 </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-app>
</template>

<script setup lang="ts">
  import { ref, computed, watch, onMounted } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import { useOrderStore } from '@/stores/order'
  import type { CreateOrderRequest, OrderAddressResponse } from '@/services/api'

  const route = useRoute()
  const router = useRouter()
  const orderStore = useOrderStore()

  // 地址类型定义
  interface Address {
    recipient: string
    phone: string
    location: string
  }

  // 订单项类型定义
  interface OrderItem {
    id: number
    name: string
    specification: string
    price: number
    quantity: number
    imageUrl: string
  }

  // 从路由参数获取商品信息
  const productFromRoute = computed(() => {
    return {
      id: Number(route.query.productId) || 0,
      name: (route.query.productName as string) || '未知商品',
      specification: (route.query.selectedColor as string) || '默认规格',
      price: Number(route.query.productPrice) || 0,
      quantity: Number(route.query.quantity) || 1,
      imageUrl: (route.query.productImage as string) || '',
    }
  })

  // 订单项数据 - 根据路由参数动态生成
  const orderItems = ref<OrderItem[]>([])

  // 初始化订单数据
  const initializeOrderData = () => {
    if (productFromRoute.value.id) {
      // 如果有商品信息，使用路由参数
      orderItems.value = [productFromRoute.value]
    } else {
      // 如果没有商品信息，使用默认数据（用于直接访问页面时）
      orderItems.value = [
        {
          id: 1,
          name: '请选择商品',
          specification: '默认规格',
          price: 0,
          quantity: 1,
          imageUrl: '',
        },
      ]
    }
  }

  // 组件挂载时初始化数据
  onMounted(() => {
    initializeOrderData()
  })

  // 监听路由变化
  watch(
    () => route.query,
    () => {
      initializeOrderData()
    }
  )

  // 收货地址数据 - 初始为空
  const address = ref<Address>({
    recipient: '',
    phone: '',
    location: '',
  })

  // 临时地址数据（用于编辑）
  const tempAddress = ref<Address>({ ...address.value })

  // 费用相关数据
  const discountAmount = ref(0)
  const shippingFee = ref(0)

  // 计算总金额
  const totalAmount = computed(() => {
    return orderItems.value.reduce((sum, item) => {
      return sum + item.price * item.quantity
    }, 0)
  })

  // 计算实付款
  const finalPayment = computed(() => {
    return totalAmount.value - discountAmount.value + shippingFee.value
  })

  // 对话框状态
  const editAddress = ref(false)
  const orderSubmitted = ref(false)
  const paymentConfirm = ref(false)
  const insufficientBalance = ref(false)
  const currentBalance = ref(5000.0)

  // 保存地址
  const saveAddress = () => {
    address.value = { ...tempAddress.value }
    editAddress.value = false
  }

  const submitOrder = async () => {
    // 检查地址是否填写完整
    if (!address.value.recipient || !address.value.phone || !address.value.location) {
      alert('请先完善收货地址信息')
      editAddress.value = true
      return
    }

    if (currentBalance.value >= finalPayment.value) {
      paymentConfirm.value = true
    } else {
      insufficientBalance.value = true
    }
  }

  const confirmPayment = async () => {
    paymentConfirm.value = false

    try {
      // 构建订单请求数据
      const orderRequest: CreateOrderRequest = {
        productId: orderItems.value[0].id,
        productName: orderItems.value[0].name,
        productImage: orderItems.value[0].imageUrl,
        specification: orderItems.value[0].specification,
        price: orderItems.value[0].price,
        quantity: orderItems.value[0].quantity,
        address: {
          recipient: address.value.recipient,
          phone: address.value.phone,
          location: address.value.location,
        } as OrderAddressResponse,
        totalAmount: totalAmount.value,
        discountAmount: discountAmount.value,
        shippingFee: shippingFee.value,
        finalPayment: finalPayment.value,
      }

      // 调用创建订单API
      const result = await orderStore.createOrder(orderRequest)

      if (result.success) {
        // 支付成功，扣除余额
        currentBalance.value -= finalPayment.value

        // 显示成功提示
        orderSubmitted.value = true

        // 3秒后跳转到订单页面
        setTimeout(() => {
          router.push('/order')
        }, 3000)
      } else {
        alert(`创建订单失败: ${result.message}`)
      }
    } catch (error) {
      console.error('创建订单错误:', error)
      alert('创建订单失败，请重试')
    }
  }

  const deferPayment = async () => {
    paymentConfirm.value = false

    try {
      // 构建订单请求数据（不支付）
      const orderRequest: CreateOrderRequest = {
        productId: orderItems.value[0].id,
        productName: orderItems.value[0].name,
        productImage: orderItems.value[0].imageUrl,
        specification: orderItems.value[0].specification,
        price: orderItems.value[0].price,
        quantity: orderItems.value[0].quantity,
        address: {
          recipient: address.value.recipient,
          phone: address.value.phone,
          location: address.value.location,
        } as OrderAddressResponse,
        totalAmount: totalAmount.value,
        discountAmount: discountAmount.value,
        shippingFee: shippingFee.value,
        finalPayment: finalPayment.value,
      }

      // 调用创建订单API
      const result = await orderStore.createOrder(orderRequest)

      if (result.success) {
        // 显示成功提示
        orderSubmitted.value = true

        // 3秒后跳转到订单页面
        setTimeout(() => {
          router.push('/order')
        }, 3000)
      } else {
        alert(`创建订单失败: ${result.message}`)
      }
    } catch (error) {
      console.error('创建订单错误:', error)
      alert('创建订单失败，请重试')
    }
  }

  // 编辑地址前复制当前地址到临时地址
  watch(editAddress, newVal => {
    if (newVal) {
      tempAddress.value = { ...address.value }
    }
  })
</script>

<style scoped>
  /* 全局重置 */
  body,
  .v-application {
    margin: 0;
    padding: 0;
  }

  .custom-btn-text .v-btn__content {
    font-weight: 600 !important;
  }

  /* 导航栏样式 */
  .navbar {
    position: relative;
    width: 100%;
    height: 60px;
    margin: 0;
    padding: 0 16px;
    background-color: #cadefc;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    z-index: 1000;
    display: flex;
    align-items: center;
    justify-content: flex-start;
  }

  /* 图标和标题放大 */
  .icon svg {
    width: 48px;
    height: 48px;
  }

  .custom-divider {
    border-color: #74a7f5 !important; /* 直接指定颜色 */
    border-width: 2px !important; /* 调整粗细 */
  }

  .address-title {
    font-family: 'PingFang SC', 'Microsoft YaHei', sans-serif !important;
    font-weight: 600 !important;
    font-size: 1.25rem !important;
    letter-spacing: 0.5px !important;
  }

  .text-wrap > div {
    white-space: normal;
    word-break: break-word;
    padding-right: 8px;
  }

  .title {
    font-size: 32px;
    font-weight: bold;
    margin-left: 16px;
    color: white !important;
  }

  .title-bar {
    background-color: #ffdfdf;
    color: #0a0608;
    font-weight: 600;
    font-size: 1.25rem;
    padding: 8px 12px;
    border-radius: 4px;
    display: inline-flex;
    align-items: center;
  }

  .title-bar .v-btn {
    min-width: 80px;
    height: 32px;
    font-size: 0.875rem;
    text-transform: none;
    box-shadow: none;
    margin-left: 8px;
    background-color: #cadefc !important;
  }

  .edit-btn {
    min-width: 60px !important;
    height: 28px !important;
    font-size: 0.8rem !important;
    margin-left: auto;
  }

  .main-content {
    min-height: calc(100vh - 168px) !important;
    background-color: #fcf6f6;
  }

  .v-application--wrap {
    height: 100%;
  }
  .v-card {
    min-height: 630px;
    height: auto;
    box-shadow:
      0 4px 6px -1px rgba(0, 0, 0, 0.1),
      0 2px 4px -1px rgba(0, 0, 0, 0.06) !important;
  }
  .v-card-text {
    padding: 40px !important;
    overflow-y: auto;
    flex: 1;
  }

  .v-card-title {
    font-family: 'PingFang SC', 'Microsoft YaHei', sans-serif !important;
    font-weight: 1000 !important;
    font-size: 1.25rem !important;
    letter-spacing: 0.5px;
  }
  .v-container {
    padding-left: 8px !important;
    padding-right: 8px !important;
  }

  /* 添加按钮样式 */
  .cancel-btn {
    background-color: #ff2e63 !important;
    border: 1px solid #e0e0e0 !important;
    border-radius: 4px !important;
    padding: 0 16px !important;
    height: 36px !important;
    color: white !important;
    min-width: 80px !important;
  }

  .defer-btn {
    background-color: #e0e0e0 !important;
    border-radius: 4px !important;
    padding: 0 16px !important;
    height: 36px !important;
    min-width: 100px !important;
    color: #616161 !important;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1) !important;
  }

  .confirm-btn {
    background-color: #cadefc !important;
    border-radius: 4px !important;
    padding: 0 16px !important;
    height: 36px !important;
    min-width: 80px !important;
    color: white !important;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1) !important;
  }

  /* 按钮悬停效果 */
  .cancel-btn:hover {
    background-color: #eeeeee !important;
  }

  .defer-btn:hover {
    background-color: #bdbdbd !important;
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15) !important;
  }

  .confirm-btn:hover {
    background-color: #f50057 !important;
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15) !important;
  }

  /* 滚动条样式 */
  ::-webkit-scrollbar {
    width: 8px;
  }
  ::-webkit-scrollbar-track {
    background: #f1f1f1;
    border-radius: 4px;
  }
  ::-webkit-scrollbar-thumb {
    background: #c1c1c1;
    border-radius: 4px;
  }
  ::-webkit-scrollbar-thumb:hover {
    background: #a8a8a8;
  }

  /* 添加空状态样式 */
  .text-center.text-grey {
    color: #9e9e9e;
  }

  .simple-title-section {
    text-align: center;
    padding: 20px 0 10px 0;
    background-color: #fcf6f6;
  }

  .simple-title-section h2 {
    font-size: 1.8rem;
    font-weight: 500;
    color: #333;
    margin-bottom: 16px;
  }

  /* 商品图片占位符 */
  .v-img {
    background-color: #f5f5f5;
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .v-img:before {
    content: '🛒';
    font-size: 24px;
    color: #bdbdbd;
  }
</style>
