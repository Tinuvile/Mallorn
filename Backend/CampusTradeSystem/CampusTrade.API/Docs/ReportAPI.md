# 举报功能 API 测试文档

## 前置条件
- 确保已启动后端服务
- 需要有效的JWT Token
- 需要先上传证据文件（使用现有的文件上传接口）

## API 接口说明

### 1. 获取举报类型列表
```http
GET /api/report/types
Authorization: Bearer {your_jwt_token}
```

### 2. 创建举报
```http
POST /api/report
Content-Type: application/json
Authorization: Bearer {your_jwt_token}

{
  "orderId": 1,
  "type": "商品问题",
  "description": "收到的商品与描述不符，存在明显质量问题",
  "evidenceFiles": [
    {
      "fileType": "图片",
      "fileUrl": "/uploads/report-evidence/evidence1.jpg"
    },
    {
      "fileType": "图片", 
      "fileUrl": "/uploads/report-evidence/evidence2.jpg"
    }
  ]
}
```

### 3. 添加举报证据
```http
POST /api/report/{reportId}/evidence
Content-Type: application/json
Authorization: Bearer {your_jwt_token}

{
  "evidenceFiles": [
    {
      "fileType": "图片",
      "fileUrl": "/uploads/report-evidence/additional_evidence.jpg"
    }
  ]
}
```

### 4. 获取当前用户的举报列表
```http
GET /api/report/my-reports?pageIndex=0&pageSize=10
Authorization: Bearer {your_jwt_token}
```

### 5. 获取举报详情
```http
GET /api/report/{reportId}
Authorization: Bearer {your_jwt_token}
```

### 6. 获取举报证据列表
```http
GET /api/report/{reportId}/evidence
Authorization: Bearer {your_jwt_token}
```

### 7. 撤销举报
```http
POST /api/report/{reportId}/cancel
Authorization: Bearer {your_jwt_token}
```

## 上传举报证据文件（使用现有接口）
```http
POST /api/file/upload/report-evidence
Content-Type: multipart/form-data
Authorization: Bearer {your_jwt_token}

Form Data:
file: [选择文件]
```

## 完整的举报流程示例

### 步骤1: 上传证据文件
```bash
curl -X POST "http://localhost:5000/api/file/upload/report-evidence" \
  -H "Authorization: Bearer {your_jwt_token}" \
  -F "file=@evidence1.jpg"
```

### 步骤2: 创建举报
```bash
curl -X POST "http://localhost:5000/api/report" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {your_jwt_token}" \
  -d '{
    "orderId": 1,
    "type": "商品问题",
    "description": "商品质量有问题",
    "evidenceFiles": [
      {
        "fileType": "图片",
        "fileUrl": "/uploads/report-evidence/evidence1.jpg"
      }
    ]
  }'
```

### 步骤3: 查看举报列表
```bash
curl -X GET "http://localhost:5000/api/report/my-reports" \
  -H "Authorization: Bearer {your_jwt_token}"
```

## 响应格式

### 成功响应
```json
{
  "success": true,
  "message": "操作成功",
  "data": {
    // 具体数据
  }
}
```

### 错误响应
```json
{
  "success": false,
  "message": "错误信息"
}
```

## 举报状态说明
- `待处理`: 新创建的举报，等待管理员处理
- `处理中`: 管理员正在处理
- `已处理`: 处理完成
- `已关闭`: 已撤销或关闭

## 举报类型及优先级
- `欺诈` - 优先级: 9（最高）
- `虚假描述` - 优先级: 7
- `商品问题` - 优先级: 5
- `服务问题` - 优先级: 4
- `其他` - 优先级: 3

## 注意事项
1. 用户只能查看自己提交的举报
2. 只有待处理状态的举报可以撤销
3. 每个用户对同一订单只能提交一个未关闭的举报
4. 举报证据支持图片、视频、文档三种类型
5. 系统会根据举报类型自动分配优先级
