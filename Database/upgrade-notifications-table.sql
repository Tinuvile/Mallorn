-- ================================================================
-- 通知表升级脚本 - 添加分离式发送状态跟踪字段
-- 执行前请备份数据库
-- ================================================================

-- 设置容器到 XEPDB1
ALTER SESSION SET CONTAINER=XEPDB1;

-- 连接到用户
CONNECT CAMPUS_TRADE_USER/"CampusTrade123!"@XEPDB1;

-- 启用DBMS_OUTPUT
SET SERVEROUTPUT ON;

-- ================================================================
-- 1. 添加新字段
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('开始添加新字段到 NOTIFICATIONS 表...');
    
    -- 添加 SignalR 状态字段
    EXECUTE IMMEDIATE 'ALTER TABLE NOTIFICATIONS ADD (
        SIGNALR_STATUS VARCHAR2(20) DEFAULT ''待发送'' CHECK (SIGNALR_STATUS IN (''待发送'',''成功'',''失败''))
    )';
    DBMS_OUTPUT.PUT_LINE('✓ 添加 SIGNALR_STATUS 字段');
    
    -- 添加邮件状态字段
    EXECUTE IMMEDIATE 'ALTER TABLE NOTIFICATIONS ADD (
        EMAIL_STATUS VARCHAR2(20) DEFAULT ''待发送'' CHECK (EMAIL_STATUS IN (''待发送'',''成功'',''失败''))
    )';
    DBMS_OUTPUT.PUT_LINE('✓ 添加 EMAIL_STATUS 字段');
    
    -- 添加 SignalR 重试次数字段
    EXECUTE IMMEDIATE 'ALTER TABLE NOTIFICATIONS ADD (
        SIGNALR_RETRY_COUNT NUMBER DEFAULT 0 CHECK (SIGNALR_RETRY_COUNT >= 0)
    )';
    DBMS_OUTPUT.PUT_LINE('✓ 添加 SIGNALR_RETRY_COUNT 字段');
    
    -- 添加邮件重试次数字段
    EXECUTE IMMEDIATE 'ALTER TABLE NOTIFICATIONS ADD (
        EMAIL_RETRY_COUNT NUMBER DEFAULT 0 CHECK (EMAIL_RETRY_COUNT >= 0)
    )';
    DBMS_OUTPUT.PUT_LINE('✓ 添加 EMAIL_RETRY_COUNT 字段');
    
    -- 添加 SignalR 最后尝试时间字段
    EXECUTE IMMEDIATE 'ALTER TABLE NOTIFICATIONS ADD (
        SIGNALR_LAST_ATTEMPT TIMESTAMP
    )';
    DBMS_OUTPUT.PUT_LINE('✓ 添加 SIGNALR_LAST_ATTEMPT 字段');
    
    -- 添加邮件最后尝试时间字段
    EXECUTE IMMEDIATE 'ALTER TABLE NOTIFICATIONS ADD (
        EMAIL_LAST_ATTEMPT TIMESTAMP
    )';
    DBMS_OUTPUT.PUT_LINE('✓ 添加 EMAIL_LAST_ATTEMPT 字段');
    
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('添加字段时出错: ' || SQLERRM);
        RAISE;
END;
/

-- ================================================================
-- 2. 初始化新字段数据
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('开始初始化新字段数据...');
    
    -- 根据现有的发送状态初始化新字段
    -- 如果整体状态是成功，则两个通道都设为成功
    UPDATE NOTIFICATIONS 
    SET SIGNALR_STATUS = CASE 
            WHEN SEND_STATUS = '成功' THEN '成功'
            WHEN SEND_STATUS = '失败' THEN '失败'
            ELSE '待发送'
        END,
        EMAIL_STATUS = CASE 
            WHEN SEND_STATUS = '成功' THEN '成功'
            WHEN SEND_STATUS = '失败' THEN '失败'
            ELSE '待发送'
        END,
        SIGNALR_RETRY_COUNT = CASE 
            WHEN SEND_STATUS = '失败' THEN RETRY_COUNT
            ELSE 0
        END,
        EMAIL_RETRY_COUNT = CASE 
            WHEN SEND_STATUS = '失败' THEN RETRY_COUNT
            ELSE 0
        END,
        SIGNALR_LAST_ATTEMPT = CASE 
            WHEN SEND_STATUS != '待发送' THEN LAST_ATTEMPT_TIME
            ELSE NULL
        END,
        EMAIL_LAST_ATTEMPT = CASE 
            WHEN SEND_STATUS != '待发送' THEN LAST_ATTEMPT_TIME
            ELSE NULL
        END;
    
    COMMIT;
    
    DBMS_OUTPUT.PUT_LINE('✓ 数据初始化完成，共更新 ' || SQL%ROWCOUNT || ' 条记录');
    
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('数据初始化时出错: ' || SQLERRM);
        RAISE;
END;
/

-- ================================================================
-- 3. 创建索引
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('开始创建索引...');
    
    -- SignalR 状态索引
    EXECUTE IMMEDIATE 'CREATE INDEX IX_NOTIFICATIONS_SIGNALR_STATUS ON NOTIFICATIONS(SIGNALR_STATUS)';
    DBMS_OUTPUT.PUT_LINE('✓ 创建 IX_NOTIFICATIONS_SIGNALR_STATUS 索引');
    
    -- 邮件状态索引
    EXECUTE IMMEDIATE 'CREATE INDEX IX_NOTIFICATIONS_EMAIL_STATUS ON NOTIFICATIONS(EMAIL_STATUS)';
    DBMS_OUTPUT.PUT_LINE('✓ 创建 IX_NOTIFICATIONS_EMAIL_STATUS 索引');
    
    -- SignalR 重试复合索引
    EXECUTE IMMEDIATE 'CREATE INDEX IX_NOTIFICATIONS_SIGNALR_RETRY ON NOTIFICATIONS(SIGNALR_STATUS, SIGNALR_RETRY_COUNT, SIGNALR_LAST_ATTEMPT)';
    DBMS_OUTPUT.PUT_LINE('✓ 创建 IX_NOTIFICATIONS_SIGNALR_RETRY 索引');
    
    -- 邮件重试复合索引
    EXECUTE IMMEDIATE 'CREATE INDEX IX_NOTIFICATIONS_EMAIL_RETRY ON NOTIFICATIONS(EMAIL_STATUS, EMAIL_RETRY_COUNT, EMAIL_LAST_ATTEMPT)';
    DBMS_OUTPUT.PUT_LINE('✓ 创建 IX_NOTIFICATIONS_EMAIL_RETRY 索引');
    
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('创建索引时出错: ' || SQLERRM);
        -- 索引创建失败不影响主要功能，继续执行
END;
/

-- ================================================================
-- 4. 验证升级结果
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('========================================');
    DBMS_OUTPUT.PUT_LINE('升级验证');
    DBMS_OUTPUT.PUT_LINE('========================================');
    
    -- 验证字段是否添加成功
    FOR col_rec IN (
        SELECT column_name, data_type, nullable, data_default 
        FROM user_tab_columns 
        WHERE table_name = 'NOTIFICATIONS' 
        AND column_name IN ('SIGNALR_STATUS', 'EMAIL_STATUS', 'SIGNALR_RETRY_COUNT', 
                           'EMAIL_RETRY_COUNT', 'SIGNALR_LAST_ATTEMPT', 'EMAIL_LAST_ATTEMPT')
        ORDER BY column_name
    ) LOOP
        DBMS_OUTPUT.PUT_LINE('✓ 字段: ' || col_rec.column_name || ' - 类型: ' || col_rec.data_type || ' - 可空: ' || col_rec.nullable);
    END LOOP;
    
    -- 验证索引是否创建成功
    FOR idx_rec IN (
        SELECT index_name, table_name, uniqueness 
        FROM user_indexes 
        WHERE table_name = 'NOTIFICATIONS' 
        AND index_name LIKE 'IX_NOTIFICATIONS_%'
        ORDER BY index_name
    ) LOOP
        DBMS_OUTPUT.PUT_LINE('✓ 索引: ' || idx_rec.index_name);
    END LOOP;
    
    -- 显示数据统计
    FOR stat_rec IN (
        SELECT 
            COUNT(*) as total_notifications,
            COUNT(CASE WHEN SIGNALR_STATUS = '待发送' THEN 1 END) as signalr_pending,
            COUNT(CASE WHEN SIGNALR_STATUS = '成功' THEN 1 END) as signalr_success,
            COUNT(CASE WHEN SIGNALR_STATUS = '失败' THEN 1 END) as signalr_failed,
            COUNT(CASE WHEN EMAIL_STATUS = '待发送' THEN 1 END) as email_pending,
            COUNT(CASE WHEN EMAIL_STATUS = '成功' THEN 1 END) as email_success,
            COUNT(CASE WHEN EMAIL_STATUS = '失败' THEN 1 END) as email_failed
        FROM NOTIFICATIONS
    ) LOOP
        DBMS_OUTPUT.PUT_LINE('');
        DBMS_OUTPUT.PUT_LINE('数据统计:');
        DBMS_OUTPUT.PUT_LINE('  总通知数: ' || stat_rec.total_notifications);
        DBMS_OUTPUT.PUT_LINE('  SignalR状态 - 待发送: ' || stat_rec.signalr_pending || ', 成功: ' || stat_rec.signalr_success || ', 失败: ' || stat_rec.signalr_failed);
        DBMS_OUTPUT.PUT_LINE('  邮件状态 - 待发送: ' || stat_rec.email_pending || ', 成功: ' || stat_rec.email_success || ', 失败: ' || stat_rec.email_failed);
    END LOOP;
    
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('========================================');
    DBMS_OUTPUT.PUT_LINE('通知表升级完成！');
    DBMS_OUTPUT.PUT_LINE('新的分离式发送状态跟踪功能已启用');
    DBMS_OUTPUT.PUT_LINE('========================================');
    
END;
/
