-- ================================================================
-- 合并消息已读状态到通知表的数据库升级脚本
-- 将 message_read_status 表的功能合并到 notifications 表中
-- ================================================================

-- 设置容器到 XEPDB1
ALTER SESSION SET CONTAINER=XEPDB1;
ALTER SESSION SET CURRENT_SCHEMA=CAMPUS_TRADE_USER;

-- 启用DBMS_OUTPUT
SET SERVEROUTPUT ON;

-- ================================================================
-- 1. 开始合并操作
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('========================================');
    DBMS_OUTPUT.PUT_LINE('开始合并消息已读状态到通知表');
    DBMS_OUTPUT.PUT_LINE('========================================');
    DBMS_OUTPUT.PUT_LINE('开始数据库结构升级...');
    
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('初始化时出错: ' || SQLERRM);
END;
/

-- ================================================================
-- 2. 在通知表中添加新字段（如果不存在）
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('开始在 NOTIFICATIONS 表中添加已读状态字段...');
    
    -- 检查IS_READ字段是否已存在
    BEGIN
        EXECUTE IMMEDIATE 'ALTER TABLE NOTIFICATIONS ADD (
            IS_READ NUMBER(1) DEFAULT 0 CHECK (IS_READ IN (0, 1))
        )';
        DBMS_OUTPUT.PUT_LINE('✓ 添加 IS_READ 字段');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -1430 THEN -- 字段已存在
                DBMS_OUTPUT.PUT_LINE('✓ IS_READ 字段已存在，跳过添加');
            ELSE
                RAISE;
            END IF;
    END;
    
    -- 检查READ_AT字段是否已存在
    BEGIN
        EXECUTE IMMEDIATE 'ALTER TABLE NOTIFICATIONS ADD (
            READ_AT TIMESTAMP NULL
        )';
        DBMS_OUTPUT.PUT_LINE('✓ 添加 READ_AT 字段');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -1430 THEN -- 字段已存在
                DBMS_OUTPUT.PUT_LINE('✓ READ_AT 字段已存在，跳过添加');
            ELSE
                RAISE;
            END IF;
    END;
    
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('添加字段时出错: ' || SQLERRM);
        RAISE;
END;
/

-- ================================================================
-- 3. 迁移现有的已读状态数据（如果源表存在）
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('开始迁移现有的已读状态数据...');
    
    -- 检查是否存在message_read_status表
    FOR table_rec IN (
        SELECT table_name FROM user_tables WHERE table_name = 'MESSAGE_READ_STATUS'
    ) LOOP
        -- 更新通知表中的已读状态
        UPDATE notifications n 
        SET is_read = (
            SELECT COALESCE(mrs.is_read, 0) 
            FROM message_read_status mrs 
            WHERE mrs.notification_id = n.notification_id
            AND mrs.user_id = n.recipient_id
        ),
        read_at = (
            SELECT mrs.read_at 
            FROM message_read_status mrs 
            WHERE mrs.notification_id = n.notification_id
            AND mrs.user_id = n.recipient_id
            AND mrs.is_read = 1
        );
        
        DBMS_OUTPUT.PUT_LINE('✓ 已迁移已读状态数据，共更新 ' || SQL%ROWCOUNT || ' 条记录');
        COMMIT;
        
        EXIT;
    END LOOP;
    
    -- 如果源表不存在，说明已经迁移过了
    IF SQL%NOTFOUND THEN
        DBMS_OUTPUT.PUT_LINE('✓ MESSAGE_READ_STATUS 表不存在，数据已迁移或无需迁移');
    END IF;
    
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('迁移数据时出错: ' || SQLERRM);
        RAISE;
END;
/

-- ================================================================
-- 4. 创建优化索引（如果不存在）
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('开始创建优化索引...');
    
    -- 已读状态索引
    BEGIN
        EXECUTE IMMEDIATE 'CREATE INDEX IX_NOTIFICATIONS_IS_READ ON NOTIFICATIONS(IS_READ)';
        DBMS_OUTPUT.PUT_LINE('✓ 创建 IX_NOTIFICATIONS_IS_READ 索引');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -955 THEN -- 索引已存在
                DBMS_OUTPUT.PUT_LINE('✓ IX_NOTIFICATIONS_IS_READ 索引已存在');
            ELSE
                DBMS_OUTPUT.PUT_LINE('创建 IX_NOTIFICATIONS_IS_READ 索引失败: ' || SQLERRM);
            END IF;
    END;
    
    -- 用户未读消息复合索引
    BEGIN
        EXECUTE IMMEDIATE 'CREATE INDEX IX_NOTIFICATIONS_USER_UNREAD ON NOTIFICATIONS(RECIPIENT_ID, IS_READ)';
        DBMS_OUTPUT.PUT_LINE('✓ 创建 IX_NOTIFICATIONS_USER_UNREAD 索引');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -955 THEN -- 索引已存在
                DBMS_OUTPUT.PUT_LINE('✓ IX_NOTIFICATIONS_USER_UNREAD 索引已存在');
            ELSE
                DBMS_OUTPUT.PUT_LINE('创建 IX_NOTIFICATIONS_USER_UNREAD 索引失败: ' || SQLERRM);
            END IF;
    END;
    
    -- 用户已读时间索引
    BEGIN
        EXECUTE IMMEDIATE 'CREATE INDEX IX_NOTIFICATIONS_READ_AT ON NOTIFICATIONS(READ_AT)';
        DBMS_OUTPUT.PUT_LINE('✓ 创建 IX_NOTIFICATIONS_READ_AT 索引');
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -955 THEN -- 索引已存在
                DBMS_OUTPUT.PUT_LINE('✓ IX_NOTIFICATIONS_READ_AT 索引已存在');
            ELSE
                DBMS_OUTPUT.PUT_LINE('创建 IX_NOTIFICATIONS_READ_AT 索引失败: ' || SQLERRM);
            END IF;
    END;
    
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('创建索引时出错: ' || SQLERRM);
        -- 索引创建失败不影响主要功能，继续执行
END;
/

-- ================================================================
-- 5. 删除原有的message_read_status表
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('开始删除原有的 MESSAGE_READ_STATUS 表...');
    
    -- 删除外键约束
    FOR fk_rec IN (
        SELECT constraint_name 
        FROM user_constraints 
        WHERE table_name = 'MESSAGE_READ_STATUS' 
        AND constraint_type = 'R'
    ) LOOP
        BEGIN
            EXECUTE IMMEDIATE 'ALTER TABLE MESSAGE_READ_STATUS DROP CONSTRAINT ' || fk_rec.constraint_name;
            DBMS_OUTPUT.PUT_LINE('✓ 删除外键约束: ' || fk_rec.constraint_name);
        EXCEPTION
            WHEN OTHERS THEN
                DBMS_OUTPUT.PUT_LINE('删除外键约束失败: ' || fk_rec.constraint_name || ' - ' || SQLERRM);
        END;
    END LOOP;
    
    -- 删除索引
    FOR idx_rec IN (
        SELECT index_name FROM user_indexes 
        WHERE table_name = 'MESSAGE_READ_STATUS' 
        AND index_name NOT LIKE 'SYS_%'
    ) LOOP
        BEGIN
            EXECUTE IMMEDIATE 'DROP INDEX ' || idx_rec.index_name;
            DBMS_OUTPUT.PUT_LINE('✓ 删除索引: ' || idx_rec.index_name);
        EXCEPTION
            WHEN OTHERS THEN
                DBMS_OUTPUT.PUT_LINE('删除索引失败: ' || idx_rec.index_name || ' - ' || SQLERRM);
        END;
    END LOOP;
    
    -- 删除表
    FOR table_rec IN (
        SELECT table_name FROM user_tables WHERE table_name = 'MESSAGE_READ_STATUS'
    ) LOOP
        EXECUTE IMMEDIATE 'DROP TABLE MESSAGE_READ_STATUS PURGE';
        DBMS_OUTPUT.PUT_LINE('✓ 已删除 MESSAGE_READ_STATUS 表');
        EXIT;
    END LOOP;
    
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('删除表时出错: ' || SQLERRM);
        -- 删除失败不影响主要功能，继续执行
END;
/

-- ================================================================
-- 6. 添加表注释
-- ================================================================
BEGIN
    EXECUTE IMMEDIATE 'COMMENT ON COLUMN NOTIFICATIONS.IS_READ IS ''是否已读：0=未读, 1=已读''';
    EXECUTE IMMEDIATE 'COMMENT ON COLUMN NOTIFICATIONS.READ_AT IS ''已读时间''';
    
    DBMS_OUTPUT.PUT_LINE('✓ 表注释添加完成');
END;
/

-- ================================================================
-- 7. 验证结果
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('========================================');
    DBMS_OUTPUT.PUT_LINE('合并结果验证');
    DBMS_OUTPUT.PUT_LINE('========================================');
    
    -- 验证字段是否添加成功
    FOR col_rec IN (
        SELECT column_name, data_type, nullable, data_default 
        FROM user_tab_columns 
        WHERE table_name = 'NOTIFICATIONS' 
        AND column_name IN ('IS_READ', 'READ_AT')
        ORDER BY column_name
    ) LOOP
        DBMS_OUTPUT.PUT_LINE('✓ 字段: ' || col_rec.column_name || ' - 类型: ' || col_rec.data_type || ' - 可空: ' || col_rec.nullable);
    END LOOP;
    
    -- 验证索引是否创建成功
    FOR idx_rec IN (
        SELECT index_name, table_name, uniqueness 
        FROM user_indexes 
        WHERE table_name = 'NOTIFICATIONS' 
        AND index_name LIKE 'IX_NOTIFICATIONS_%READ%'
        ORDER BY index_name
    ) LOOP
        DBMS_OUTPUT.PUT_LINE('✓ 索引: ' || idx_rec.index_name);
    END LOOP;
    
    -- 显示数据统计
    FOR stat_rec IN (
        SELECT 
            COUNT(*) as total_notifications,
            COUNT(CASE WHEN is_read = 1 THEN 1 END) as read_notifications,
            COUNT(CASE WHEN is_read = 0 THEN 1 END) as unread_notifications,
            COUNT(CASE WHEN read_at IS NOT NULL THEN 1 END) as with_read_time
        FROM NOTIFICATIONS
    ) LOOP
        DBMS_OUTPUT.PUT_LINE('');
        DBMS_OUTPUT.PUT_LINE('数据统计:');
        DBMS_OUTPUT.PUT_LINE('  总通知数: ' || stat_rec.total_notifications);
        DBMS_OUTPUT.PUT_LINE('  已读通知: ' || stat_rec.read_notifications);
        DBMS_OUTPUT.PUT_LINE('  未读通知: ' || stat_rec.unread_notifications);
        DBMS_OUTPUT.PUT_LINE('  有已读时间: ' || stat_rec.with_read_time);
    END LOOP;
    
    -- 检查message_read_status表是否已删除
    DECLARE
        table_exists NUMBER := 0;
    BEGIN
        SELECT COUNT(*) INTO table_exists 
        FROM user_tables 
        WHERE table_name = 'MESSAGE_READ_STATUS';
        
        IF table_exists > 0 THEN
            DBMS_OUTPUT.PUT_LINE('⚠️ MESSAGE_READ_STATUS 表仍然存在');
        ELSE
            DBMS_OUTPUT.PUT_LINE('✓ MESSAGE_READ_STATUS 表已成功删除');
        END IF;
    END;
    
    -- 检查备份表是否存在并清理
    FOR table_rec IN (
        SELECT table_name FROM user_tables 
        WHERE table_name IN ('NOTIFICATIONS_BACKUP', 'MESSAGE_READ_STATUS_BACKUP')
        ORDER BY table_name
    ) LOOP
        EXECUTE IMMEDIATE 'DROP TABLE ' || table_rec.table_name || ' PURGE';
        DBMS_OUTPUT.PUT_LINE('✓ 已清理备份表: ' || table_rec.table_name);
    END LOOP;
    
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('========================================');
    DBMS_OUTPUT.PUT_LINE('消息已读状态合并完成！');
    DBMS_OUTPUT.PUT_LINE('主要变化：');
    DBMS_OUTPUT.PUT_LINE('  - 在 NOTIFICATIONS 表中新增 IS_READ 和 READ_AT 字段');
    DBMS_OUTPUT.PUT_LINE('  - 迁移了原有的已读状态数据');
    DBMS_OUTPUT.PUT_LINE('  - 删除了 MESSAGE_READ_STATUS 表');
    DBMS_OUTPUT.PUT_LINE('  - 创建了相关的优化索引');
    DBMS_OUTPUT.PUT_LINE('  - 已清理所有临时备份表');
    DBMS_OUTPUT.PUT_LINE('========================================');
    
END;
/

-- 升级完成
COMMIT;
