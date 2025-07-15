# 校园交易平台启动脚本说明

本目录包含了校园交易平台项目的各种启动和管理脚本。

## 脚本列表

### 1. `start-all.ps1` - 启动完整项目

**功能**：启动整个项目（数据库 + 后端 API + 前端）
**使用方式**：

```powershell
.\scripts\start-all.ps1
```

**说明**：

- 使用混合模式启动（数据库和后端用 Docker，前端本地运行）
- 自动检查 Docker 状态
- 等待后端 API 启动完成后再启动前端
- 前端支持热重载开发

**访问地址**：

- 前端：http://localhost:5173
- 后端 API：http://localhost:5085
- Swagger 文档：http://localhost:5085/swagger

### 2. `start-backend.ps1` - 单独启动后端

**功能**：单独启动后端 API 项目
**使用方式**：

```powershell
.\scripts\start-backend.ps1
```

**选项**：

1. **Docker 方式**：包含数据库，完整的 Docker 环境
2. **本地.NET 方式**：使用 dotnet 命令本地运行，支持热重载

**说明**：

- Docker 方式会同时启动 Oracle 数据库
- 本地方式需要外部数据库连接
- 本地方式支持`dotnet watch`热重载

### 3. `start-frontend.ps1` - 单独启动前端

**功能**：单独启动前端开发服务器
**使用方式**：

```powershell
.\scripts\start-frontend.ps1
```

**功能特性**：

- 自动检查并安装 npm 依赖
- 检查后端 API 连接状态
- 支持 Vite 热重载
- 自动打开浏览器
- 显示配置信息

### 4. `update-database.ps1` - 数据库管理工具

**功能**：管理数据库和 SQL 文件
**使用方式**：

```powershell
.\scripts\update-database.ps1
```

**功能选项**：

1. **重新初始化数据库**：删除现有数据，重新创建
2. **执行特定 SQL 文件**：选择并执行 Database 目录中的 SQL 文件
3. **交互式 SQL 会话**：连接数据库执行自定义 SQL 命令
4. **查看 SQL 文件内容**：预览 SQL 文件内容

### 5. `stop-all.ps1` - 停止所有服务

**功能**：停止所有运行中的项目服务
**使用方式**：

```powershell
.\scripts\stop-all.ps1
```

**功能特性**：

- 停止所有 Docker 容器
- 停止前端开发服务器进程
- 停止后端 API 进程
- 检查端口占用情况
- 提供清理资源的建议命令
