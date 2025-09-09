-- ================================================================
-- 消息已读状态表结构升级脚本
-- 简化设计：与通知一一对应，移除消息类型字段
-- ================================================================

-- 设置容器到 XEPDB1
ALTER SESSION SET CONTAINER=XEPDB1;
ALTER SESSION SET CURRENT_SCHEMA=CAMPUS_TRADE_USER;

-- 启用DBMS_OUTPUT
SET SERVEROUTPUT ON;

-- ================================================================
-- 1. 备份现有数据（如果表存在）
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('开始备份现有数据...');
    
    -- 检查表是否存在
    FOR table_rec IN (
        SELECT table_name FROM user_tables WHERE table_name = 'MESSAGE_READ_STATUS'
    ) LOOP
        -- 创建备份表
        EXECUTE IMMEDIATE 'CREATE TABLE message_read_status_backup AS SELECT * FROM message_read_status';
        DBMS_OUTPUT.PUT_LINE('✓ 已备份现有数据到 message_read_status_backup 表');
        EXIT;
    END LOOP;
    
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('备份数据时出错（可能是表不存在）: ' || SQLERRM);
END;
/

-- ================================================================
-- 2. 删除现有表和索引
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('开始清理现有结构...');
    
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
        EXECUTE IMMEDIATE 'DROP TABLE message_read_status PURGE';
        DBMS_OUTPUT.PUT_LINE('✓ 删除现有表: MESSAGE_READ_STATUS');
        EXIT;
    END LOOP;
    
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('清理现有结构时出错: ' || SQLERRM);
END;
/

-- ================================================================
-- 3. 创建新的消息已读状态表
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('开始创建新的消息已读状态表...');
    
    EXECUTE IMMEDIATE '
        CREATE TABLE message_read_status (
            read_status_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            user_id NUMBER NOT NULL,
            notification_id NUMBER NOT NULL,
            is_read NUMBER(1) DEFAULT 0 CHECK (is_read IN (0, 1)),
            read_at TIMESTAMP,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            CONSTRAINT uk_message_read_status UNIQUE (user_id, notification_id),
            CONSTRAINT fk_message_read_user FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
            CONSTRAINT fk_message_read_notification FOREIGN KEY (notification_id) REFERENCES notifications(notification_id) ON DELETE CASCADE
        )';
    
    DBMS_OUTPUT.PUT_LINE('✓ 消息已读状态表创建成功');
    
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('创建消息已读状态表时出错: ' || SQLERRM);
        RAISE;
END;
/

-- ================================================================
-- 4. 创建索引
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('开始创建索引...');
    
    -- 用户查询索引
    EXECUTE IMMEDIATE 'CREATE INDEX IX_MESSAGE_READ_USER ON message_read_status(user_id)';
    DBMS_OUTPUT.PUT_LINE('✓ 创建 IX_MESSAGE_READ_USER 索引');
    
    -- 通知查询索引
    EXECUTE IMMEDIATE 'CREATE INDEX IX_MESSAGE_READ_NOTIFICATION ON message_read_status(notification_id)';
    DBMS_OUTPUT.PUT_LINE('✓ 创建 IX_MESSAGE_READ_NOTIFICATION 索引');
    
    -- 已读状态索引
    EXECUTE IMMEDIATE 'CREATE INDEX IX_MESSAGE_READ_STATUS ON message_read_status(is_read)';
    DBMS_OUTPUT.PUT_LINE('✓ 创建 IX_MESSAGE_READ_STATUS 索引');
    
    -- 复合索引（查询优化）
    EXECUTE IMMEDIATE 'CREATE INDEX IX_MESSAGE_READ_COMPOUND ON message_read_status(user_id, is_read)';
    DBMS_OUTPUT.PUT_LINE('✓ 创建 IX_MESSAGE_READ_COMPOUND 索引');
    
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('创建索引时出错: ' || SQLERRM);
        -- 索引创建失败不影响主要功能，继续执行
END;
/

-- ================================================================
-- 5. 添加表注释
-- ================================================================
BEGIN
    EXECUTE IMMEDIATE 'COMMENT ON TABLE message_read_status IS ''消息已读状态表 - 与通知一一对应的已读状态管理''';
    EXECUTE IMMEDIATE 'COMMENT ON COLUMN message_read_status.read_status_id IS ''已读状态ID''';
    EXECUTE IMMEDIATE 'COMMENT ON COLUMN message_read_status.user_id IS ''用户ID''';
    EXECUTE IMMEDIATE 'COMMENT ON COLUMN message_read_status.notification_id IS ''通知ID（对应通知表的主键）''';
    EXECUTE IMMEDIATE 'COMMENT ON COLUMN message_read_status.is_read IS ''是否已读：0=未读, 1=已读''';
    EXECUTE IMMEDIATE 'COMMENT ON COLUMN message_read_status.read_at IS ''已读时间''';
    EXECUTE IMMEDIATE 'COMMENT ON COLUMN message_read_status.created_at IS ''创建时间''';
    
    DBMS_OUTPUT.PUT_LINE('✓ 表注释添加完成');
END;
/

-- ================================================================
-- 6. 初始化现有通知的已读状态
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('开始初始化现有通知的已读状态...');
    
    -- 为所有现有通知创建已读状态记录
    -- 发送状态为"成功"的通知标记为已读，其他为未读
    INSERT INTO message_read_status (user_id, notification_id, is_read, read_at, created_at)
    SELECT 
        recipient_id,
        notification_id,
        CASE WHEN send_status = '成功' THEN 1 ELSE 0 END,
        CASE WHEN send_status = '成功' THEN sent_at ELSE NULL END,
        CURRENT_TIMESTAMP
    FROM notifications
    WHERE NOT EXISTS (
        SELECT 1 FROM message_read_status mrs
        WHERE mrs.user_id = notifications.recipient_id
        AND mrs.notification_id = notifications.notification_id
    );
    
    DBMS_OUTPUT.PUT_LINE('✓ 通知已读状态初始化完成，共处理 ' || SQL%ROWCOUNT || ' 条记录');
    
    COMMIT;
    
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('初始化已读状态时出错: ' || SQLERRM);
        RAISE;
END;
/

-- ================================================================
-- 7. 清理备份表（可选）
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('清理备份表...');
    
    FOR table_rec IN (
        SELECT table_name FROM user_tables WHERE table_name = 'MESSAGE_READ_STATUS_BACKUP'
    ) LOOP
        EXECUTE IMMEDIATE 'DROP TABLE message_read_status_backup PURGE';
        DBMS_OUTPUT.PUT_LINE('✓ 已删除备份表 message_read_status_backup');
        EXIT;
    END LOOP;
    
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('清理备份表时出错: ' || SQLERRM);
        -- 备份表清理失败不影响主要功能
END;
/

-- ================================================================
-- 8. 验证升级结果
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('========================================');
    DBMS_OUTPUT.PUT_LINE('升级验证');
    DBMS_OUTPUT.PUT_LINE('========================================');
    
    -- 验证表是否创建成功
    FOR table_rec IN (
        SELECT table_name, num_rows
        FROM user_tables 
        WHERE table_name = 'MESSAGE_READ_STATUS'
    ) LOOP
        DBMS_OUTPUT.PUT_LINE('✓ 表: ' || table_rec.table_name || ' 创建成功');
    END LOOP;
    
    -- 验证索引是否创建成功
    FOR idx_rec IN (
        SELECT index_name, table_name, uniqueness 
        FROM user_indexes 
        WHERE table_name = 'MESSAGE_READ_STATUS'
        ORDER BY index_name
    ) LOOP
        DBMS_OUTPUT.PUT_LINE('✓ 索引: ' || idx_rec.index_name);
    END LOOP;
    
    -- 显示数据统计
    FOR total_rec IN (
        SELECT 
            COUNT(*) as total_messages,
            COUNT(CASE WHEN is_read = 1 THEN 1 END) as total_read,
            COUNT(CASE WHEN is_read = 0 THEN 1 END) as total_unread
        FROM message_read_status
    ) LOOP
        DBMS_OUTPUT.PUT_LINE('');
        DBMS_OUTPUT.PUT_LINE('统计信息:');
        DBMS_OUTPUT.PUT_LINE('  总消息数: ' || total_rec.total_messages);
        DBMS_OUTPUT.PUT_LINE('  已读消息: ' || total_rec.total_read);
        DBMS_OUTPUT.PUT_LINE('  未读消息: ' || total_rec.total_unread);
    END LOOP;
    
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('========================================');
    DBMS_OUTPUT.PUT_LINE('消息已读状态系统升级完成！');
    DBMS_OUTPUT.PUT_LINE('简化设计：与通知一一对应的已读状态管理');
    DBMS_OUTPUT.PUT_LINE('========================================');
    
END;
/
