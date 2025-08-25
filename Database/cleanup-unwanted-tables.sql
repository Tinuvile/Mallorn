-- ================================================================
-- 清理不需要的表脚本
-- 删除 insert-frontend-mock-data.sql 创建的额外表
-- ================================================================

-- 启用DBMS_OUTPUT
SET SERVEROUTPUT ON;
SET ECHO ON;

BEGIN
    DBMS_OUTPUT.PUT_LINE('=== 开始清理不需要的表 ===');
END;
/

-- ================================================================
-- 删除商品标签相关表
-- ================================================================
BEGIN
    DECLARE
        table_exists NUMBER;
    BEGIN
        -- 删除商品标签关系表
        SELECT COUNT(*) INTO table_exists FROM user_tables WHERE table_name = 'PRODUCT_TAG_RELATIONS';
        IF table_exists > 0 THEN
            EXECUTE IMMEDIATE 'DROP TABLE product_tag_relations CASCADE CONSTRAINTS PURGE';
            DBMS_OUTPUT.PUT_LINE('✅ 已删除表: PRODUCT_TAG_RELATIONS');
        ELSE
            DBMS_OUTPUT.PUT_LINE('ℹ️ 表 PRODUCT_TAG_RELATIONS 不存在，跳过');
        END IF;
        
        -- 删除商品标签表
        SELECT COUNT(*) INTO table_exists FROM user_tables WHERE table_name = 'PRODUCT_TAGS';
        IF table_exists > 0 THEN
            EXECUTE IMMEDIATE 'DROP TABLE product_tags CASCADE CONSTRAINTS PURGE';
            DBMS_OUTPUT.PUT_LINE('✅ 已删除表: PRODUCT_TAGS');
        ELSE
            DBMS_OUTPUT.PUT_LINE('ℹ️ 表 PRODUCT_TAGS 不存在，跳过');
        END IF;
    END;
END;
/

-- ================================================================
-- 删除轮播图表
-- ================================================================
BEGIN
    DECLARE
        table_exists NUMBER;
    BEGIN
        SELECT COUNT(*) INTO table_exists FROM user_tables WHERE table_name = 'CAROUSEL_BANNERS';
        IF table_exists > 0 THEN
            EXECUTE IMMEDIATE 'DROP TABLE carousel_banners CASCADE CONSTRAINTS PURGE';
            DBMS_OUTPUT.PUT_LINE('✅ 已删除表: CAROUSEL_BANNERS');
        ELSE
            DBMS_OUTPUT.PUT_LINE('ℹ️ 表 CAROUSEL_BANNERS 不存在，跳过');
        END IF;
    END;
END;
/

-- ================================================================
-- 删除首页配置表
-- ================================================================
BEGIN
    DECLARE
        table_exists NUMBER;
    BEGIN
        SELECT COUNT(*) INTO table_exists FROM user_tables WHERE table_name = 'HOMEPAGE_CONFIG';
        IF table_exists > 0 THEN
            EXECUTE IMMEDIATE 'DROP TABLE homepage_config CASCADE CONSTRAINTS PURGE';
            DBMS_OUTPUT.PUT_LINE('✅ 已删除表: HOMEPAGE_CONFIG');
        ELSE
            DBMS_OUTPUT.PUT_LINE('ℹ️ 表 HOMEPAGE_CONFIG 不存在，跳过');
        END IF;
    END;
END;
/

-- ================================================================
-- 删除可能存在的其他杂表
-- ================================================================
BEGIN
    DECLARE
        table_exists NUMBER;
    BEGIN
        -- 删除分类备份表
        SELECT COUNT(*) INTO table_exists FROM user_tables WHERE table_name = 'CATEGORIES_BACKUP';
        IF table_exists > 0 THEN
            EXECUTE IMMEDIATE 'DROP TABLE categories_backup CASCADE CONSTRAINTS PURGE';
            DBMS_OUTPUT.PUT_LINE('✅ 已删除表: CATEGORIES_BACKUP');
        ELSE
            DBMS_OUTPUT.PUT_LINE('ℹ️ 表 CATEGORIES_BACKUP 不存在，跳过');
        END IF;
        
        -- 删除系统配置表
        SELECT COUNT(*) INTO table_exists FROM user_tables WHERE table_name = 'SYSTEM_CONFIG';
        IF table_exists > 0 THEN
            EXECUTE IMMEDIATE 'DROP TABLE system_config CASCADE CONSTRAINTS PURGE';
            DBMS_OUTPUT.PUT_LINE('✅ 已删除表: SYSTEM_CONFIG');
        ELSE
            DBMS_OUTPUT.PUT_LINE('ℹ️ 表 SYSTEM_CONFIG 不存在，跳过');
        END IF;
        
        -- 删除临时产品备份表（如果还存在）
        SELECT COUNT(*) INTO table_exists FROM user_tables WHERE table_name = 'TEMP_PRODUCTS_BACKUP';
        IF table_exists > 0 THEN
            EXECUTE IMMEDIATE 'DROP TABLE temp_products_backup CASCADE CONSTRAINTS PURGE';
            DBMS_OUTPUT.PUT_LINE('✅ 已删除表: TEMP_PRODUCTS_BACKUP');
        ELSE
            DBMS_OUTPUT.PUT_LINE('ℹ️ 表 TEMP_PRODUCTS_BACKUP 不存在，跳过');
        END IF;
    END;
END;
/

-- ================================================================
-- 验证清理结果
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('=== 清理完成，当前用户表列表 ===');
    
    DECLARE
        table_count NUMBER;
    BEGIN
        SELECT COUNT(*) INTO table_count FROM user_tables;
        DBMS_OUTPUT.PUT_LINE('当前表总数: ' || table_count);
        DBMS_OUTPUT.PUT_LINE('');
        DBMS_OUTPUT.PUT_LINE('剩余表列表:');
        
        FOR rec IN (SELECT table_name FROM user_tables ORDER BY table_name) LOOP
            DBMS_OUTPUT.PUT_LINE('📋 ' || rec.table_name);
        END LOOP;
    END;
END;
/

COMMIT;

BEGIN
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('🎉 不需要的表清理完成！');
    DBMS_OUTPUT.PUT_LINE('现在数据库只保留核心业务表。');
END;
/
