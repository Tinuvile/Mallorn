#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
校园交易平台测试数据生成脚本
生成大量学生、用户、商品和订单数据
"""

import os
import random
import string
import json
from datetime import datetime, timedelta
from typing import List, Dict, Tuple
import hashlib

# 配置参数
STUDENTS_COUNT = 500          # 学生数量
PRODUCTS_PER_USER_MIN = 20    # 每个用户最少发布商品数
PRODUCTS_PER_USER_MAX = 50    # 每个用户最多发布商品数
ORDERS_COUNT = 10000          # 订单数量
OUTPUT_FILE = "campus_trade_test_data.sql"

# 读取配置文件获取BaseUrl
def load_config():
    """从appsettings.json读取配置"""
    config_path = "../Backend/CampusTradeSystem/CampusTrade.API/appsettings.json"
    try:
        with open(config_path, 'r', encoding='utf-8') as f:
            config = json.load(f)
            return config.get('FileStorage', {}).get('BaseUrl', 'http://8.222.141.205:8080')
    except (FileNotFoundError, json.JSONDecodeError, KeyError):
        print(f"警告: 无法读取配置文件 {config_path}，使用默认BaseUrl")
        return 'http://8.222.141.205:8080'

BASE_URL = load_config()

# 已有的学生ID，需要避免冲突
EXISTING_STUDENT_IDS = {
    'ADMIN001', 'STU001', 'STU002', 'STU003',  # init-oracle.sql中的学生
    '2352495', '2353018', '2351427', '2354177', '2352755', 
    '2352491', '2351284', '2354269', '2352749', '2351588'  # add-students.sql中的学生
}

# 学院列表
DEPARTMENTS = [
    "计算机科学与技术学院", "软件学院", "电子信息学院", "机械工程学院", 
    "自动化学院", "材料科学与工程学院", "化学工程学院", "土木工程学院",
    "建筑学院", "经济管理学院", "外国语学院", "理学院", "环境科学与工程学院",
    "生物工程学院", "艺术设计学院", "体育学院", "马克思主义学院"
]

# 姓名生成用的姓氏和名字
SURNAMES = ["王", "李", "张", "刘", "陈", "杨", "赵", "黄", "周", "吴", "徐", "孙", "胡", "朱", "高", 
           "林", "何", "郭", "马", "罗", "梁", "宋", "郑", "谢", "韩", "唐", "冯", "于", "董", "萧",
           "程", "曹", "袁", "邓", "许", "傅", "沈", "曾", "彭", "吕", "苏", "卢", "蒋", "蔡", "贾"]

GIVEN_NAMES = ["伟", "芳", "娜", "敏", "静", "丽", "强", "磊", "军", "洋", "勇", "艳", "杰", "娟", "涛",
              "明", "超", "秀英", "霞", "平", "刚", "桂英", "辉", "梅", "华", "健", "斌", "英", "慧", "峰",
              "浩", "宇", "欣", "雨", "晨", "阳", "雪", "月", "星", "云", "风", "海", "山", "川", "林",
              "思", "志", "远", "博", "文", "武", "德", "义", "智", "信", "礼", "乐", "和", "谐", "美"]

# 分类ID映射（基于init-oracle.sql中的分类结构和插入顺序）
# 一级分类: 1=教材, 2=数码, 3=日用, 4=服装, 5=运动, 6=其他
# 二级分类从ID=7开始按插入顺序
CATEGORY_MAPPING = {
    7: "教材-计算机科学",  # 教材类二级分类，按插入顺序
    8: "教材-数学", 
    9: "教材-英语",
    10: "教材-物理",
    11: "教材-化学",
    12: "教材-其他教材",
    13: "数码-手机通讯",   # 数码类二级分类
    14: "数码-电脑配件", 
    15: "数码-影音设备",
    16: "数码-智能设备",
    17: "日用-文具用品",   # 日用类二级分类
    18: "日用-生活用品",
    19: "日用-护理用品",
    20: "日用-食品饮料",
    21: "服装-男装",      # 服装类二级分类
    22: "服装-女装",
    23: "服装-鞋包配饰",
    24: "运动-运动鞋服",   # 运动类二级分类
    25: "运动-健身器材",
    26: "运动-户外用品"
}

# 各分类对应的商品名称模板（匹配修正后的分类ID）
PRODUCT_NAMES = {
    7: ["Java编程思想", "数据结构与算法", "计算机网络", "操作系统概念", "数据库系统概论", "软件工程", "计算机组成原理", "编译原理"],
    8: ["高等数学", "线性代数", "概率论与数理统计", "离散数学", "数学分析", "高等代数", "数学建模", "复变函数"],
    9: ["大学英语", "新概念英语", "英语语法", "托福词汇", "雅思真题", "英语听力", "英语写作", "商务英语"],
    10: ["大学物理", "理论力学", "电磁学", "量子物理", "热力学", "光学", "物理实验", "现代物理"],
    11: ["无机化学", "有机化学", "物理化学", "分析化学", "化学实验", "生物化学", "化工原理", "材料化学"],
    12: ["专业课教材", "参考书", "习题集", "实验指导", "课程设计", "毕业设计参考", "学习资料", "考试复习"],
    13: ["iPhone", "华为手机", "小米手机", "OPPO手机", "vivo手机", "三星手机", "一加手机", "红米手机"],
    14: ["笔记本电脑", "台式机", "显示器", "键盘", "鼠标", "耳机", "音响", "摄像头"],
    15: ["蓝牙耳机", "音响", "录音笔", "MP3播放器", "便携音响", "游戏耳机", "降噪耳机", "运动耳机"],
    16: ["智能手表", "平板电脑", "电子书", "智能音响", "投影仪", "无人机", "VR眼镜", "智能手环"],
    17: ["笔记本", "钢笔", "圆珠笔", "文件夹", "书包", "计算器", "尺子", "橡皮"],
    18: ["水杯", "毛巾", "床上用品", "洗漱用品", "台灯", "收纳盒", "衣架", "垃圾桶"],
    19: ["洗发水", "沐浴露", "护肤品", "牙刷", "牙膏", "面膜", "化妆品", "护手霜"],
    20: ["零食", "饮料", "方便面", "牛奶", "面包", "水果", "坚果", "咖啡"],
    21: ["T恤", "衬衫", "裤子", "外套", "毛衣", "夹克", "牛仔裤", "运动裤"],
    22: ["连衣裙", "上衣", "裙子", "外套", "毛衣", "牛仔裤", "leggings", "卫衣"],
    23: ["运动鞋", "帆布鞋", "皮鞋", "背包", "钱包", "帽子", "围巾", "手套"],
    24: ["运动鞋", "运动服", "篮球", "足球", "羽毛球拍", "网球拍", "游泳用品", "瑜伽垫"],
    25: ["哑铃", "跑步机", "健身器材", "瑜伽用品", "拉力器", "仰卧板", "跳绳", "健身球"],
    26: ["帐篷", "睡袋", "登山包", "户外用品", "手电筒", "指南针", "野餐用品", "折叠椅"]
}

def generate_student_id() -> str:
    """生成学号（7位数字），避免与现有学生ID冲突"""
    while True:
        student_id = f"23{random.randint(10000, 99999)}"
        if student_id not in EXISTING_STUDENT_IDS:
            return student_id

def generate_name() -> str:
    """生成随机姓名"""
    surname = random.choice(SURNAMES)
    given_name = random.choice(GIVEN_NAMES)
    if random.random() < 0.3:  # 30%概率生成两个字的名字
        given_name += random.choice(GIVEN_NAMES)
    return surname + given_name

def generate_email(name: str, student_id: str) -> str:
    """根据姓名和学号生成邮箱"""
    # 使用拼音首字母 + 学号
    domains = ["163.com", "qq.com", "gmail.com", "126.com", "sina.com"]
    username = f"user{student_id}" 
    return f"{username}@{random.choice(domains)}"

def get_images_for_category(category_id: int) -> List[str]:
    """获取指定分类的所有图片文件，返回完整的URL格式"""
    images_dir = f"images/{category_id}"
    if not os.path.exists(images_dir):
        return []
    
    images = []
    for filename in os.listdir(images_dir):
        if filename.lower().endswith(('.jpg', '.jpeg', '.png', '.gif')):
            # 使用新的URL格式：BaseUrl/files/products/图片文件名
            image_url = f"{BASE_URL}/files/products/{filename}"
            images.append(image_url)
    return images

def generate_product_description(category_id: int, title: str) -> str:
    """生成商品描述"""
    descriptions = [
        f"{title}，质量很好，使用时间不长，现在出售。",
        f"九成新{title}，功能正常，价格实惠。",
        f"全新{title}，买重了，低价转让。",
        f"{title}转让，品质保证，支持面交。",
        f"闲置{title}，八成新，诚心出售。",
        f"{title}出售，使用次数少，外观完好。",
        f"急出{title}，价格可小刀，同城优先。",
        f"{title}转让，正品保证，无拆无修。"
    ]
    return random.choice(descriptions)

def generate_students_and_users(count: int) -> Tuple[List[Dict], List[Dict]]:
    """生成学生和用户数据"""
    students = []
    users = []
    used_student_ids = set()
    used_emails = set()
    
    print(f"正在生成 {count} 个学生和用户...")
    
    for i in range(count):
        # 生成唯一学号
        while True:
            student_id = generate_student_id()
            if student_id not in used_student_ids:
                used_student_ids.add(student_id)
                break
        
        name = generate_name()
        department = random.choice(DEPARTMENTS)
        
        # 生成唯一邮箱
        while True:
            email = generate_email(name, student_id)
            if email not in used_emails:
                used_emails.add(email)
                break
        
        # 学生数据
        student = {
            'student_id': student_id,
            'name': name,
            'department': department
        }
        students.append(student)
        
        # 用户数据 - 不指定user_id，由数据库触发器自动生成
        username = f"user{student_id}"
        password_hash = "$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi"  # 固定密码hash
        credit_score = round(random.uniform(50.0, 99.9), 1)
        
        user = {
            'index': i,  # 保存索引以便后续引用
            'email': email,
            'credit_score': credit_score,
            'password_hash': password_hash,
            'student_id': student_id,
            'username': username,
            'full_name': name,
            'phone': f"1{random.randint(300000000, 999999999):09d}",
            'login_count': random.randint(0, 100),
            'is_active': 1,
            'is_locked': 0,
            'failed_login_attempts': 0,
            'two_factor_enabled': 0,
            'email_verified': 1,
            'security_stamp': ''.join(random.choices(string.ascii_letters + string.digits, k=32))
        }
        users.append(user)
        
        if (i + 1) % 100 == 0:
            print(f"已生成 {i + 1} 个学生和用户...")
    
    return students, users

def generate_products(users: List[Dict]) -> Tuple[List[Dict], List[Dict]]:
    """生成商品和商品图片数据"""
    products = []
    product_images = []
    product_id_counter = 1
    
    print(f"正在为 {len(users)} 个用户生成商品...")
    
    for user in users:
        user_index = user['index']  # 使用用户索引而不是user_id
        product_count = random.randint(PRODUCTS_PER_USER_MIN, PRODUCTS_PER_USER_MAX)
        user_product_names = {}  # 记录该用户已使用的商品名称
        
        for _ in range(product_count):
            # 随机选择分类
            category_id = random.choice(list(CATEGORY_MAPPING.keys()))
            category_images = get_images_for_category(category_id)
            
            if not category_images:
                continue  # 如果该分类没有图片，跳过
            
            # 生成商品标题
            base_names = PRODUCT_NAMES.get(category_id, ["商品"])
            base_name = random.choice(base_names)
            
            # 确保同一用户不发布同名商品
            title = base_name
            counter = 1
            while title in user_product_names:
                counter += 1
                title = f"{base_name}{counter}"
            user_product_names[title] = True
            
            # 生成商品数据
            base_price = round(random.uniform(1, 1000), 2)
            publish_time = datetime.now() - timedelta(days=random.randint(0, 365))
            view_count = random.randint(0, 500)
            
            # 随机设置自动下架时间（30%的概率设置）
            auto_remove_time = None
            if random.random() < 0.3:
                auto_remove_time = publish_time + timedelta(days=random.randint(30, 90))
            
            product = {
                'product_id': product_id_counter,
                'user_index': user_index,  # 使用索引而不是user_id
                'category_id': category_id,
                'title': title,
                'description': generate_product_description(category_id, title),
                'base_price': base_price,
                'publish_time': publish_time,
                'view_count': view_count,
                'auto_remove_time': auto_remove_time,
                'status': '在售'  # 默认状态，订单生成时会更新
            }
            products.append(product)
            
            # 为商品随机选择1-3张图片
            selected_images = random.sample(category_images, min(random.randint(1, 3), len(category_images)))
            for image_url in selected_images:
                product_images.append({
                    'product_id': product_id_counter,
                    'image_url': image_url
                })
            
            product_id_counter += 1
    
    print(f"共生成 {len(products)} 个商品，{len(product_images)} 张图片")
    return products, product_images

def generate_orders(users: List[Dict], products: List[Dict]) -> List[Dict]:
    """生成订单数据并更新商品状态"""
    orders = []
    # 创建商品状态副本，避免修改原始数据
    products_status = {p['product_id']: p['status'] for p in products}
    available_products = [p for p in products if p['status'] == '在售']
    used_products = set()  # 记录已经被订单使用过的商品，不允许重复上架
    
    print(f"正在生成 {ORDERS_COUNT} 个订单，可用商品数: {len(available_products)}")
    
    for order_id in range(1, ORDERS_COUNT + 1):
        if not available_products:
            print(f"已生成 {order_id-1} 个订单，无更多可用商品")
            break
            
        # 随机选择一个在售商品
        product = random.choice(available_products)
        seller_index = product['user_index']
        
        # 随机选择买家（不能是卖家自己）
        buyers = [u for u in users if u['index'] != seller_index]
        if not buyers:
            continue
            
        buyer = random.choice(buyers)
        buyer_index = buyer['index']
        
        # 生成订单状态
        statuses = ['议价中', '待付款', '已付款', '已发货', '已送达', '已完成', '已取消']
        weights = [0.05, 0.15, 0.15, 0.15, 0.15, 0.25, 0.10]  # 权重分布
        status = random.choices(statuses, weights=weights)[0]
        
        # 生成订单时间
        create_time = datetime.now() - timedelta(days=random.randint(0, 180))
        
        # 生成过期时间（创建时间后30分钟）
        expire_time = create_time + timedelta(minutes=30)
        
        # 生成最终价格（基于基础价格的80%-120%）
        base_price = product['base_price']
        final_price = round(base_price * random.uniform(0.8, 1.2), 2)
        
        order = {
            'order_id': order_id,
            'buyer_index': buyer_index,
            'seller_index': seller_index,
            'product_id': product['product_id'],
            'total_amount': final_price,
            'status': status,
            'create_time': create_time,
            'expire_time': expire_time,
            'final_price': final_price
        }
        orders.append(order)
        
        # 将商品标记为已使用，不允许再次上架
        used_products.add(product['product_id'])
        available_products.remove(product)
        
        # 更新商品状态
        if status in ['已付款', '已发货', '已送达']:
            products_status[product['product_id']] = '交易中'
        elif status == '已完成':
            products_status[product['product_id']] = '已下架'
        elif status == '已取消':
            products_status[product['product_id']] = '在售'
            # 取消的订单商品保持已使用状态，不重新加入可用列表
        else:
            # 对于议价中和待付款状态，设为交易中
            products_status[product['product_id']] = '交易中'
        
        if order_id % 10000 == 0:
            print(f"已生成 {order_id} 个订单，剩余可用商品: {len(available_products)}")
    
    # 更新原始商品数据的状态
    for product in products:
        product['status'] = products_status[product['product_id']]
    
    return orders

def generate_sql_file(students: List[Dict], users: List[Dict], products: List[Dict], 
                     product_images: List[Dict], orders: List[Dict]):
    """生成SQL插入文件"""
    print("正在生成SQL文件...")
    
    with open(OUTPUT_FILE, 'w', encoding='utf-8') as f:
        f.write("-- ================================================================\n")
        f.write("-- 校园交易平台测试数据插入脚本\n")
        f.write(f"-- 生成时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")
        f.write("-- ================================================================\n\n")
        
        f.write("-- 设置Oracle脚本模式\n")
        f.write("SET SERVEROUTPUT ON;\n")
        f.write("ALTER SESSION SET TIME_ZONE = '+08:00';\n\n")
        
        # 插入学生数据
        f.write("-- ================================================================\n")
        f.write(f"-- 插入学生数据 ({len(students)} 条记录)\n")
        f.write("-- ================================================================\n")
        f.write("INSERT ALL\n")
        
        for i, student in enumerate(students):
            f.write(f"  INTO students (student_id, name, department) VALUES ('{student['student_id']}', '{student['name']}', '{student['department']}')\n")
            
            # 每1000条记录执行一次
            if (i + 1) % 1000 == 0 or (i + 1) == len(students):
                f.write("SELECT 1 FROM dual;\n\n")
                if (i + 1) != len(students):
                    f.write("INSERT ALL\n")
        
        # 插入用户数据
        f.write("-- ================================================================\n")
        f.write(f"-- 插入用户数据 ({len(users)} 条记录)\n")
        f.write("-- ================================================================\n")
        
        for i, user in enumerate(users):
            create_time = "CURRENT_TIMESTAMP"
            updated_time = "CURRENT_TIMESTAMP"
            
            f.write(f"INSERT INTO users (email, credit_score, password_hash, student_id, username, full_name, phone, created_at, updated_at, is_active, login_count, is_locked, failed_login_attempts, two_factor_enabled, email_verified, security_stamp) VALUES ")
            f.write(f"('{user['email']}', {user['credit_score']}, '{user['password_hash']}', '{user['student_id']}', '{user['username']}', '{user['full_name']}', '{user['phone']}', {create_time}, {updated_time}, {user['is_active']}, {user['login_count']}, {user['is_locked']}, {user['failed_login_attempts']}, {user['two_factor_enabled']}, {user['email_verified']}, '{user['security_stamp']}');\n")
        
        f.write("\n-- 提交用户数据\nCOMMIT;\n\n")
        
        # 插入抽象订单数据（为所有商品创建）
        f.write("-- ================================================================\n")
        f.write(f"-- 插入抽象订单数据 ({len(products)} 条记录)\n")
        f.write("-- ================================================================\n")
        f.write("INSERT ALL\n")
        
        for i, product in enumerate(products):
            f.write(f"  INTO abstract_orders (abstract_order_id, order_type) VALUES ({product['product_id']}, 'normal')\n")
            
            # 每1000条记录执行一次
            if (i + 1) % 1000 == 0 or (i + 1) == len(products):
                f.write("SELECT 1 FROM dual;\n\n")
                if (i + 1) != len(products):
                    f.write("INSERT ALL\n")
        
        # 插入商品数据
        f.write("-- ================================================================\n")
        f.write(f"-- 插入商品数据 ({len(products)} 条记录)\n")
        f.write("-- ================================================================\n")
        
        for i, product in enumerate(products):
            user_student_id = users[product['user_index']]['student_id']
            publish_time = f"TIMESTAMP '{product['publish_time'].strftime('%Y-%m-%d %H:%M:%S')}'"
            auto_remove_time = f"TIMESTAMP '{product['auto_remove_time'].strftime('%Y-%m-%d %H:%M:%S')}'" if product['auto_remove_time'] else "NULL"
            
            # 转义单引号
            title = product['title'].replace("'", "''")
            description = product['description'].replace("'", "''")
            
            # 使用唯一的标题来标识商品，不指定product_id让数据库自动生成
            unique_title = f"{title}_{i}_{user_student_id}"
            
            f.write(f"INSERT INTO products (user_id, category_id, title, description, base_price, publish_time, view_count, auto_remove_time, status) VALUES ")
            f.write(f"((SELECT user_id FROM users WHERE student_id = '{user_student_id}'), {product['category_id']}, '{unique_title}', '{description}', {product['base_price']}, {publish_time}, {product['view_count']}, {auto_remove_time}, '{product['status']}');\n")
            
            # 每1000条记录提交一次
            if (i + 1) % 1000 == 0:
                f.write("COMMIT;\n")
        
        f.write("COMMIT;\n\n")
        
        # 插入商品图片数据
        f.write("-- ================================================================\n")
        f.write(f"-- 插入商品图片数据 ({len(product_images)} 条记录)\n")
        f.write("-- ================================================================\n")
        
        for i, image in enumerate(product_images):
            # 找到对应的商品索引来构建唯一标题
            product_index = None
            for j, product in enumerate(products):
                if product['product_id'] == image['product_id']:
                    product_index = j
                    break
            
            if product_index is not None:
                user_student_id = users[products[product_index]['user_index']]['student_id']
                title = products[product_index]['title'].replace("'", "''")
                unique_title = f"{title}_{product_index}_{user_student_id}"
                
                f.write(f"INSERT INTO product_images (product_id, image_url) VALUES ")
                f.write(f"((SELECT product_id FROM products WHERE title = '{unique_title}'), '{image['image_url']}');\n")
            
            # 每1000条记录提交一次
            if (i + 1) % 1000 == 0:
                f.write("COMMIT;\n")
        
        f.write("COMMIT;\n\n")
        
        # 插入抽象订单数据（为所有订单创建）
        f.write("-- ================================================================\n")
        f.write(f"-- 插入抽象订单数据 ({len(orders)} 条记录)\n")
        f.write("-- ================================================================\n")
        
        for i, order in enumerate(orders):
            # 订单的abstract_order_id = 商品数量 + order_id（避免冲突）
            abstract_order_id = len(products) + order['order_id']
            f.write(f"INSERT INTO abstract_orders (abstract_order_id, order_type) VALUES ({abstract_order_id}, 'normal');\n")
            
            # 每1000条记录提交一次
            if (i + 1) % 1000 == 0:
                f.write("COMMIT;\n")
        
        f.write("COMMIT;\n\n")
        
        # 插入订单数据
        f.write("-- ================================================================\n")
        f.write(f"-- 插入订单数据 ({len(orders)} 条记录)\n")
        f.write("-- ================================================================\n")
        
        for i, order in enumerate(orders):
            # 订单的abstract_order_id = 商品数量 + order_id（避免冲突）
            abstract_order_id = len(products) + order['order_id']
            buyer_student_id = users[order['buyer_index']]['student_id']
            seller_student_id = users[order['seller_index']]['student_id']
            create_time = f"TIMESTAMP '{order['create_time'].strftime('%Y-%m-%d %H:%M:%S')}'"
            expire_time = f"TIMESTAMP '{order['expire_time'].strftime('%Y-%m-%d %H:%M:%S')}'"
            
            f.write(f"INSERT INTO orders (order_id, buyer_id, seller_id, product_id, total_amount, status, create_time, expire_time, final_price) VALUES ")
            f.write(f"({abstract_order_id}, (SELECT user_id FROM users WHERE student_id = '{buyer_student_id}'), (SELECT user_id FROM users WHERE student_id = '{seller_student_id}'), {order['product_id']}, {order['total_amount']}, '{order['status']}', {create_time}, {expire_time}, {order['final_price']});\n")
            
            # 每1000条记录提交一次
            if (i + 1) % 1000 == 0:
                f.write("COMMIT;\n")
        
        f.write("COMMIT;\n\n")
        
        f.write("-- ================================================================\n")
        f.write("-- 数据插入完成\n")
        f.write("-- ================================================================\n")
        f.write(f"SELECT 'Test data generation completed!' AS status,\n")
        f.write(f"       {len(students)} AS students_count,\n")
        f.write(f"       {len(users)} AS users_count,\n") 
        f.write(f"       {len(products)} AS products_count,\n")
        f.write(f"       {len(product_images)} AS product_images_count,\n")
        f.write(f"       {len(orders)} AS orders_count\n")
        f.write("FROM dual;\n\n")
        
        f.write("-- ================================================================\n")
        f.write("-- 重置序列以避免主键冲突\n")
        f.write("-- ================================================================\n")
        f.write("-- 将产品序列设置到一个安全的起始值\n")
        f.write("DROP SEQUENCE PRODUCT_SEQ;\n")
        f.write("CREATE SEQUENCE PRODUCT_SEQ START WITH 100000 INCREMENT BY 1 NOCACHE;\n")
        f.write("-- 将抽象订单序列设置到一个安全的起始值\n") 
        f.write("DROP SEQUENCE ABSTRACT_ORDER_SEQ;\n")
        f.write("CREATE SEQUENCE ABSTRACT_ORDER_SEQ START WITH 100000 INCREMENT BY 1 NOCACHE;\n")
    
    print(f"SQL文件已生成: {OUTPUT_FILE}")

def main():
    """主函数"""
    print("=== 校园交易平台测试数据生成器 ===")
    print(f"配置:")
    print(f"  学生数量: {STUDENTS_COUNT}")
    print(f"  每用户商品数: {PRODUCTS_PER_USER_MIN}-{PRODUCTS_PER_USER_MAX}")
    print(f"  订单数量: {ORDERS_COUNT}")
    print(f"  图片Base URL: {BASE_URL}")
    print()
    
    # 检查images目录
    if not os.path.exists("images"):
        print("错误: 找不到images目录")
        return
    
    # 生成学生和用户数据
    students, users = generate_students_and_users(STUDENTS_COUNT)
    
    # 生成商品和图片数据
    products, product_images = generate_products(users)
    
    # 生成订单数据
    orders = generate_orders(users, products)
    
    # 生成SQL文件
    generate_sql_file(students, users, products, product_images, orders)
    
    print("\n=== 数据生成完成 ===")
    print(f"学生数量: {len(students)}")
    print(f"用户数量: {len(users)}")
    print(f"商品数量: {len(products)}")
    print(f"商品图片数量: {len(product_images)}")
    print(f"订单数量: {len(orders)}")
    print(f"SQL文件: {OUTPUT_FILE}")

if __name__ == "__main__":
    main()