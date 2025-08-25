-- ================================================================
-- 校园交易平台 - 前端模拟数据插入脚本
-- 说明: 将前端Vue组件中硬编码的数据迁移到数据库中，完全按照fix-categories-structure.sql的分类结构
-- 执行前请确保已运行 init-oracle.sql 和 insert-notification-templates.sql
-- ================================================================

-- 启用DBMS_OUTPUT
SET SERVEROUTPUT ON;
SET ECHO ON;

-- ================================================================
-- 1. 修复分类结构和商品关联问题 - 按照fix-categories-structure.sql的结构
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('=== 开始按照校园交易系统标准修复分类 ===');
END;
/

-- 先备份现有商品数据到临时表
CREATE TABLE temp_products_backup AS SELECT * FROM products;
DBMS_OUTPUT.PUT_LINE('已备份现有商品数据');

-- 暂时禁用外键约束
ALTER TABLE products DISABLE CONSTRAINT fk_product_category;
DBMS_OUTPUT.PUT_LINE('已禁用外键约束');

-- 清理现有分类数据，重建分类结构
DELETE FROM categories;
DBMS_OUTPUT.PUT_LINE('已清理现有分类数据');

-- ================================================================
-- 2. 插入一级分类（校园交易系统专用分类）
-- ================================================================
-- 一级分类：教材
INSERT INTO categories (parent_id, name) VALUES (NULL, '教材');

-- 一级分类：数码
INSERT INTO categories (parent_id, name) VALUES (NULL, '数码');

-- 一级分类：日用
INSERT INTO categories (parent_id, name) VALUES (NULL, '日用');

-- 一级分类：服装
INSERT INTO categories (parent_id, name) VALUES (NULL, '服装');

-- 一级分类：运动
INSERT INTO categories (parent_id, name) VALUES (NULL, '运动');

-- 一级分类：其他
INSERT INTO categories (parent_id, name) VALUES (NULL, '其他');

DBMS_OUTPUT.PUT_LINE('已插入6个一级分类：教材、数码、日用、服装、运动、其他');

-- ================================================================
-- 3. 插入二级分类（使用查询获取父分类ID）
-- ================================================================
DECLARE
    v_parent_id NUMBER;
BEGIN
    DBMS_OUTPUT.PUT_LINE('开始插入二级分类...');
    
    -- 教材类的二级分类
    SELECT category_id INTO v_parent_id FROM categories WHERE name = '教材' AND parent_id IS NULL;
    
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '计算机科学');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '数学');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '英语');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '物理');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '化学');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '其他教材');
    
    -- 数码类的二级分类
    SELECT category_id INTO v_parent_id FROM categories WHERE name = '数码' AND parent_id IS NULL;
    
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '手机通讯');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '电脑配件');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '影音设备');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '智能设备');
    
    -- 日用类的二级分类
    SELECT category_id INTO v_parent_id FROM categories WHERE name = '日用' AND parent_id IS NULL;
    
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '文具用品');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '生活用品');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '护理用品');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '食品饮料');
    
    -- 服装类的二级分类
    SELECT category_id INTO v_parent_id FROM categories WHERE name = '服装' AND parent_id IS NULL;
    
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '男装');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '女装');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '鞋包配饰');
    
    -- 运动类的二级分类
    SELECT category_id INTO v_parent_id FROM categories WHERE name = '运动' AND parent_id IS NULL;
    
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '运动鞋服');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '健身器材');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '户外用品');
    
    -- 其他类的二级分类
    SELECT category_id INTO v_parent_id FROM categories WHERE name = '其他' AND parent_id IS NULL;
    
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '乐器');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '收藏品');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '其他商品');
    
    DBMS_OUTPUT.PUT_LINE('已插入所有二级分类');
END;
/

-- ================================================================
-- 4. 插入三级标签（特殊属性标签）
-- ================================================================
DECLARE
    v_parent_id NUMBER;
BEGIN
    DBMS_OUTPUT.PUT_LINE('开始插入三级标签...');
    
    -- 为主要二级分类添加标签
    -- 计算机科学标签
    SELECT category_id INTO v_parent_id FROM categories WHERE name = '计算机科学';
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '急出');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '可议价');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '支持换物');
    
    -- 手机通讯标签
    SELECT category_id INTO v_parent_id FROM categories WHERE name = '手机通讯';
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '急出');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '可议价');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '支持换物');
    
    -- 电脑配件标签
    SELECT category_id INTO v_parent_id FROM categories WHERE name = '电脑配件';
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '急出');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '可议价');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '支持换物');
    
    -- 运动鞋服标签
    SELECT category_id INTO v_parent_id FROM categories WHERE name = '运动鞋服';
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '急出');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '可议价');
    INSERT INTO categories (parent_id, name) VALUES (v_parent_id, '支持换物');
    
    DBMS_OUTPUT.PUT_LINE('已插入三级标签');
END;
/

-- ================================================================
-- 5. 智能修复现有商品的分类关联
-- ================================================================
DECLARE
    v_textbook_id NUMBER;
    v_digital_id NUMBER;
    v_sports_id NUMBER;
    v_clothing_id NUMBER;
    v_daily_id NUMBER;
    v_other_id NUMBER;
    v_updated_count NUMBER;
BEGIN
    DBMS_OUTPUT.PUT_LINE('=== 开始智能修复商品分类关联 ===');
    
    -- 获取一级分类ID
    SELECT category_id INTO v_textbook_id FROM categories WHERE name = '教材' AND parent_id IS NULL;
    SELECT category_id INTO v_digital_id FROM categories WHERE name = '数码' AND parent_id IS NULL;
    SELECT category_id INTO v_sports_id FROM categories WHERE name = '运动' AND parent_id IS NULL;
    SELECT category_id INTO v_clothing_id FROM categories WHERE name = '服装' AND parent_id IS NULL;
    SELECT category_id INTO v_daily_id FROM categories WHERE name = '日用' AND parent_id IS NULL;
    SELECT category_id INTO v_other_id FROM categories WHERE name = '其他' AND parent_id IS NULL;
    
    -- 更新教材类商品
    UPDATE products 
    SET category_id = v_textbook_id 
    WHERE (
        UPPER(title) LIKE '%教材%' OR 
        UPPER(title) LIKE '%数据结构%' OR
        UPPER(title) LIKE '%算法%' OR
        UPPER(title) LIKE '%高等数学%' OR
        UPPER(title) LIKE '%线性代数%' OR
        UPPER(title) LIKE '%概率论%' OR
        UPPER(title) LIKE '%英语%' OR
        UPPER(title) LIKE '%JAVA%' OR
        UPPER(title) LIKE '%编程%' OR
        UPPER(title) LIKE '%托福%' OR
        UPPER(title) LIKE '%雅思%'
    );
    v_updated_count := SQL%ROWCOUNT;
    DBMS_OUTPUT.PUT_LINE('已修复 ' || v_updated_count || ' 个教材类商品');
    
    -- 更新数码类商品
    UPDATE products 
    SET category_id = v_digital_id 
    WHERE category_id = 64 AND (
        UPPER(title) LIKE '%AIRPODS%' OR
        UPPER(title) LIKE '%SONY%' OR
        UPPER(title) LIKE '%JBL%' OR
        UPPER(title) LIKE '%BEATS%' OR
        UPPER(title) LIKE '%耳机%' OR
        UPPER(title) LIKE '%音响%' OR
        UPPER(title) LIKE '%APPLE%' OR
        UPPER(title) LIKE '%IPHONE%' OR
        UPPER(title) LIKE '%MACBOOK%' OR
        UPPER(title) LIKE '%IPAD%' OR
        UPPER(title) LIKE '%WATCH%'
    );
    v_updated_count := SQL%ROWCOUNT;
    DBMS_OUTPUT.PUT_LINE('已修复 ' || v_updated_count || ' 个数码类商品');
    
    -- 更新运动类商品
    UPDATE products 
    SET category_id = v_sports_id 
    WHERE category_id = 64 AND (
        UPPER(title) LIKE '%NIKE%' OR
        UPPER(title) LIKE '%ADIDAS%' OR
        UPPER(title) LIKE '%NEW BALANCE%' OR
        UPPER(title) LIKE '%CONVERSE%' OR
        UPPER(title) LIKE '%VANS%' OR
        UPPER(title) LIKE '%PUMA%' OR
        UPPER(title) LIKE '%ASICS%' OR
        UPPER(title) LIKE '%REEBOK%' OR
        UPPER(title) LIKE '%鞋%' OR
        UPPER(title) LIKE '%运动%' OR
        UPPER(title) LIKE '%哑铃%' OR
        UPPER(title) LIKE '%健身%'
    );
    v_updated_count := SQL%ROWCOUNT;
    DBMS_OUTPUT.PUT_LINE('已修复 ' || v_updated_count || ' 个运动类商品');
    
    -- 将剩余孤立商品分配到其他分类
    UPDATE products 
    SET category_id = v_other_id 
    WHERE category_id = 64 OR category_id NOT IN (SELECT category_id FROM categories);
    v_updated_count := SQL%ROWCOUNT;
    DBMS_OUTPUT.PUT_LINE('已将剩余 ' || v_updated_count || ' 个商品分配到其他分类');
    
END;
/

-- 重新启用外键约束
ALTER TABLE products ENABLE CONSTRAINT fk_product_category;
DBMS_OUTPUT.PUT_LINE('已重新启用外键约束');

-- ================================================================
-- 6. 跳过轮播图表创建（删除不需要的轮播图功能）
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('跳过轮播图表创建');
END;

-- ================================================================
-- 7. 插入校园热销商品数据（按正确分类分配）
-- ================================================================
DECLARE
    v_textbook_id NUMBER;
    v_digital_id NUMBER;
    v_sports_id NUMBER;
    v_daily_id NUMBER;
    v_clothing_id NUMBER;
BEGIN
    -- 获取分类ID
    SELECT category_id INTO v_textbook_id FROM categories WHERE name = '教材' AND parent_id IS NULL;
    SELECT category_id INTO v_digital_id FROM categories WHERE name = '数码' AND parent_id IS NULL;
    SELECT category_id INTO v_sports_id FROM categories WHERE name = '运动' AND parent_id IS NULL;
    SELECT category_id INTO v_daily_id FROM categories WHERE name = '日用' AND parent_id IS NULL;
    SELECT category_id INTO v_clothing_id FROM categories WHERE name = '服装' AND parent_id IS NULL;
    
    -- 删除可能存在的模拟数据
    DELETE FROM product_images WHERE product_id BETWEEN 100 AND 130;
    DELETE FROM products WHERE product_id BETWEEN 100 AND 130;
    
    -- 插入数码类热销商品
    INSERT INTO products (user_id, category_id, title, description, base_price, status, view_count) 
    VALUES (2, v_digital_id, 'iPhone 13 Pro', '128GB，深空灰色，9成新，原装充电器', 4990.00, '在售', 289);
    
    INSERT INTO products (user_id, category_id, title, description, base_price, status, view_count) 
    VALUES (3, v_digital_id, 'MacBook Air M1', '13英寸，256GB，银色，学生自用', 6990.00, '在售', 235);
    
    INSERT INTO products (user_id, category_id, title, description, base_price, status, view_count) 
    VALUES (4, v_digital_id, 'AirPods Pro', '主动降噪，原装正品，使用半年', 1390.00, '在售', 267);
    
    INSERT INTO products (user_id, category_id, title, description, base_price, status, view_count) 
    VALUES (2, v_digital_id, 'iPad 第9代', '64GB WiFi版，深空灰色，含保护套', 2190.00, '在售', 198);
    
    -- 插入运动类热销商品
    INSERT INTO products (user_id, category_id, title, description, base_price, status, view_count) 
    VALUES (3, v_sports_id, 'Nike Air Jordan 1', '经典黑红配色，42码，8成新', 899.00, '在售', 189);
    
    INSERT INTO products (user_id, category_id, title, description, base_price, status, view_count) 
    VALUES (4, v_sports_id, 'Adidas Stan Smith', '小白鞋经典款，41码，几乎全新', 459.00, '在售', 145);
    
    INSERT INTO products (user_id, category_id, title, description, base_price, status, view_count) 
    VALUES (2, v_sports_id, '健身哑铃组合', '可调节5-20kg，适合宿舍健身', 299.00, '在售', 123);
    
    -- 插入教材类热销商品
    INSERT INTO products (user_id, category_id, title, description, base_price, status, view_count) 
    VALUES (3, v_textbook_id, '数据结构与算法', '计算机专业必修，9成新无涂写', 45.00, '在售', 156);
    
    DBMS_OUTPUT.PUT_LINE('已插入热销商品数据');
END;
/

-- ================================================================
-- 8. 插入推荐商品数据
-- ================================================================
DECLARE
    v_textbook_id NUMBER;
    v_digital_id NUMBER;
    v_sports_id NUMBER;
    v_daily_id NUMBER;
    v_clothing_id NUMBER;
BEGIN
    -- 获取分类ID
    SELECT category_id INTO v_textbook_id FROM categories WHERE name = '教材' AND parent_id IS NULL;
    SELECT category_id INTO v_digital_id FROM categories WHERE name = '数码' AND parent_id IS NULL;
    SELECT category_id INTO v_sports_id FROM categories WHERE name = '运动' AND parent_id IS NULL;
    SELECT category_id INTO v_daily_id FROM categories WHERE name = '日用' AND parent_id IS NULL;
    SELECT category_id INTO v_clothing_id FROM categories WHERE name = '服装' AND parent_id IS NULL;
    
    -- 插入推荐教材
    INSERT INTO products (user_id, category_id, title, description, base_price, status, view_count) 
    VALUES (4, v_textbook_id, '高等数学同济版', '上下册全套，有笔记，适合复习', 39.00, '在售', 234);
    
    INSERT INTO products (user_id, category_id, title, description, base_price, status, view_count) 
    VALUES (2, v_textbook_id, 'Java核心技术', '第11版，程序员必读经典', 79.00, '在售', 189);
    
    INSERT INTO products (user_id, category_id, title, description, base_price, status, view_count) 
    VALUES (3, v_textbook_id, '大学英语综合教程', '全套4册，新视野系列', 65.00, '在售', 167);
    
    -- 插入推荐数码产品
    INSERT INTO products (user_id, category_id, title, description, base_price, status, view_count) 
    VALUES (4, v_digital_id, 'Sony WH-1000XM4', '降噪耳机，黑色，音质极佳', 1899.00, '在售', 145);
    
    INSERT INTO products (user_id, category_id, title, description, base_price, status, view_count) 
    VALUES (2, v_digital_id, 'Kindle Paperwhite', '第11代，8GB，护眼阅读', 699.00, '在售', 89);
    
    -- 插入推荐日用品
    INSERT INTO products (user_id, category_id, title, description, base_price, status, view_count) 
    VALUES (3, v_daily_id, '无印良品文具套装', '笔袋、笔、便签本全套', 89.00, '在售', 178);
    
    INSERT INTO products (user_id, category_id, title, description, base_price, status, view_count) 
    VALUES (4, v_daily_id, '宿舍护理用品套装', '洗护全套，适合新生', 129.00, '在售', 134);
    
    -- 插入推荐服装
    INSERT INTO products (user_id, category_id, title, description, base_price, status, view_count) 
    VALUES (2, v_clothing_id, 'Uniqlo羽绒服', '轻薄款，黑色，L码，冬季必备', 199.00, '在售', 156);
    
    DBMS_OUTPUT.PUT_LINE('已插入推荐商品数据');
END;
/

-- ================================================================
-- 9. 为新插入的商品添加图片
-- ================================================================
DECLARE
    v_counter NUMBER := 1;
BEGIN
    -- 为热销商品添加图片
    FOR rec IN (
        SELECT product_id FROM products 
        WHERE title LIKE '%iPhone%' OR title LIKE '%MacBook%' OR title LIKE '%AirPods%' 
        OR title LIKE '%iPad%' OR title LIKE '%Nike%' OR title LIKE '%Adidas%' 
        OR title LIKE '%哑铃%' OR title LIKE '%数据结构%'
        ORDER BY product_id
    ) LOOP
        INSERT INTO product_images (product_id, image_url) 
        VALUES (rec.product_id, '/images/hot' || v_counter || '.webp');
        
        v_counter := v_counter + 1;
        IF v_counter > 8 THEN
            v_counter := 1;
        END IF;
    END LOOP;
    
    -- 为推荐商品添加图片
    v_counter := 1;
    FOR rec IN (
        SELECT product_id FROM products 
        WHERE title LIKE '%高等数学%' OR title LIKE '%Java%' OR title LIKE '%英语%' 
        OR title LIKE '%Sony%' OR title LIKE '%Kindle%' OR title LIKE '%无印%' 
        OR title LIKE '%护理%' OR title LIKE '%羽绒服%'
        ORDER BY product_id
    ) LOOP
        INSERT INTO product_images (product_id, image_url) 
        VALUES (rec.product_id, '/images/rec' || v_counter || '.webp');
        
        v_counter := v_counter + 1;
        IF v_counter > 12 THEN
            v_counter := 1;
        END IF;
    END LOOP;
    
    DBMS_OUTPUT.PUT_LINE('已为商品添加图片');
END;
/

-- ================================================================
-- 10. 跳过商品标签系统创建（删除不需要的标签功能）
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('跳过商品标签系统创建');
END;

-- ================================================================
-- 11. 跳过首页配置表创建（删除不需要的配置功能）
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('跳过首页配置表创建');
END;

-- ================================================================
-- 提交更改并验证结果
-- ================================================================
COMMIT;

-- 验证修复结果
BEGIN
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('========================================');
    DBMS_OUTPUT.PUT_LINE('🎓 校园交易平台数据初始化完成！');
    DBMS_OUTPUT.PUT_LINE('========================================');
    
    DECLARE
        v_orphan_products NUMBER;
        v_total_products NUMBER;
        v_categories_count NUMBER;
        v_banners_count NUMBER;
    BEGIN
        -- 检查孤立商品
        SELECT COUNT(*) INTO v_orphan_products 
        FROM products p 
        WHERE NOT EXISTS (SELECT 1 FROM categories c WHERE c.category_id = p.category_id);
        
        SELECT COUNT(*) INTO v_total_products FROM products;
        SELECT COUNT(*) INTO v_categories_count FROM categories;
        v_banners_count := 0;
        
        DBMS_OUTPUT.PUT_LINE('📊 数据统计:');
        DBMS_OUTPUT.PUT_LINE('- 分类总数: ' || v_categories_count);
        DBMS_OUTPUT.PUT_LINE('- 商品总数: ' || v_total_products);
        DBMS_OUTPUT.PUT_LINE('- 孤立商品: ' || v_orphan_products);
        DBMS_OUTPUT.PUT_LINE('- 轮播图: ' || v_banners_count || ' 个');
        
        IF v_orphan_products = 0 THEN
            DBMS_OUTPUT.PUT_LINE('✅ 所有商品都已正确分类！');
        ELSE
            DBMS_OUTPUT.PUT_LINE('⚠️ 仍有 ' || v_orphan_products || ' 个孤立商品');
        END IF;
        
        -- 显示校园交易系统标准分类商品分布
        DBMS_OUTPUT.PUT_LINE('');
        DBMS_OUTPUT.PUT_LINE('🏫 校园交易系统分类商品分布:');
        FOR rec IN (
            SELECT c.name, COUNT(p.product_id) as product_count
            FROM categories c
            LEFT JOIN products p ON c.category_id = p.category_id
            WHERE c.parent_id IS NULL
            GROUP BY c.name
            ORDER BY CASE c.name 
                WHEN '教材' THEN 1 
                WHEN '数码' THEN 2 
                WHEN '日用' THEN 3 
                WHEN '服装' THEN 4 
                WHEN '运动' THEN 5 
                WHEN '其他' THEN 6 
                ELSE 7 END
        ) LOOP
            DBMS_OUTPUT.PUT_LINE('📁 ' || rec.name || ': ' || rec.product_count || ' 个商品');
        END LOOP;
        
    END;
END;
/

-- 删除临时备份表
DROP TABLE temp_products_backup;

BEGIN
    DBMS_OUTPUT.PUT_LINE('已清理临时数据');
END;
/

BEGIN
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('🎉 校园交易平台前后端数据连接完成！');
    DBMS_OUTPUT.PUT_LINE('现在可以测试前端分类显示功能了。');
    DBMS_OUTPUT.PUT_LINE('✅ 分类体系：教材 | 数码 | 日用 | 服装 | 运动 | 其他');
END;
/