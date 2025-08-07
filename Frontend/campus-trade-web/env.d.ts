/// <reference types="vite/client" />
interface ImportMetaEnv {
  readonly BASE_URL: string
  // 其他你项目中使用的环境变量...
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}

declare module '*.vue' {
  import type { DefineComponent } from 'vue'
  const component: DefineComponent<{}, {}, any>
  export default component
}
