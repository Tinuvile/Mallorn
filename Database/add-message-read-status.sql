-- ================================================================
-- 消息已读状态表创建脚本
-- 用于统一管理所有类型消息的已读状态
-- ================================================================

-- 设置容器到 XEPDB1
ALTER SESSION SET CONTAINER=XEPDB1;
ALTER SESSION SET CURRENT_SCHEMA=CAMPUS_TRADE_USER;

-- 启用DBMS_OUTPUT
SET SERVEROUTPUT ON;

-- ================================================================
-- 1. 创建消息已读状态表
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('开始创建消息已读状态表...');
    
    EXECUTE IMMEDIATE '
        CREATE TABLE message_read_status (
            read_status_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            user_id NUMBER NOT NULL,
            message_type VARCHAR2(20) NOT NULL CHECK (message_type IN (''notification'', ''bargain'', ''swap'', ''exchange'')),
            message_id NUMBER NOT NULL,
            is_read NUMBER(1) DEFAULT 0 CHECK (is_read IN (0, 1)),
            read_at TIMESTAMP,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            CONSTRAINT uk_message_read_status UNIQUE (user_id, message_type, message_id),
            CONSTRAINT fk_message_read_user FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
        )';
    
    DBMS_OUTPUT.PUT_LINE('✓ 消息已读状态表创建成功');
    
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('创建消息已读状态表时出错: ' || SQLERRM);
        RAISE;
END;
/

-- ================================================================
-- 2. 创建索引
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('开始创建索引...');
    
    -- 用户消息查询索引
    EXECUTE IMMEDIATE 'CREATE INDEX IX_MESSAGE_READ_USER_TYPE ON message_read_status(user_id, message_type)';
    DBMS_OUTPUT.PUT_LINE('✓ 创建 IX_MESSAGE_READ_USER_TYPE 索引');
    
    -- 消息类型索引
    EXECUTE IMMEDIATE 'CREATE INDEX IX_MESSAGE_READ_TYPE ON message_read_status(message_type)';
    DBMS_OUTPUT.PUT_LINE('✓ 创建 IX_MESSAGE_READ_TYPE 索引');
    
    -- 已读状态索引
    EXECUTE IMMEDIATE 'CREATE INDEX IX_MESSAGE_READ_STATUS ON message_read_status(is_read)';
    DBMS_OUTPUT.PUT_LINE('✓ 创建 IX_MESSAGE_READ_STATUS 索引');
    
    -- 复合索引（查询优化）
    EXECUTE IMMEDIATE 'CREATE INDEX IX_MESSAGE_READ_COMPOUND ON message_read_status(user_id, message_type, is_read)';
    DBMS_OUTPUT.PUT_LINE('✓ 创建 IX_MESSAGE_READ_COMPOUND 索引');
    
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('创建索引时出错: ' || SQLERRM);
        -- 索引创建失败不影响主要功能，继续执行
END;
/

-- ================================================================
-- 3. 添加表注释
-- ================================================================
BEGIN
    EXECUTE IMMEDIATE 'COMMENT ON TABLE message_read_status IS ''消息已读状态表 - 统一管理所有类型消息的已读状态''';
    EXECUTE IMMEDIATE 'COMMENT ON COLUMN message_read_status.read_status_id IS ''已读状态ID''';
    EXECUTE IMMEDIATE 'COMMENT ON COLUMN message_read_status.user_id IS ''用户ID''';
    EXECUTE IMMEDIATE 'COMMENT ON COLUMN message_read_status.message_type IS ''消息类型：notification, bargain, swap, exchange, review''';
    EXECUTE IMMEDIATE 'COMMENT ON COLUMN message_read_status.message_id IS ''消息ID（对应各类型消息的主键）''';
    EXECUTE IMMEDIATE 'COMMENT ON COLUMN message_read_status.is_read IS ''是否已读：0=未读, 1=已读''';
    EXECUTE IMMEDIATE 'COMMENT ON COLUMN message_read_status.read_at IS ''已读时间''';
    EXECUTE IMMEDIATE 'COMMENT ON COLUMN message_read_status.created_at IS ''创建时间''';
    
    DBMS_OUTPUT.PUT_LINE('✓ 表注释添加完成');
END;
/

-- ================================================================
-- 4. 初始化现有消息的已读状态
-- ================================================================
BEGIN
    DBMS_OUTPUT.PUT_LINE('开始初始化现有消息的已读状态...');
    
    -- 初始化通知消息的已读状态
    -- 发送状态为"成功"的通知标记为已读
    INSERT INTO message_read_status (user_id, message_type, message_id, is_read, read_at, created_at)
    SELECT 
        recipient_id,
        'notification',
        notification_id,
        CASE WHEN send_status = '成功' THEN 1 ELSE 0 END,
        CASE WHEN send_status = '成功' THEN sent_at ELSE NULL END,
        CURRENT_TIMESTAMP
    FROM notifications
    WHERE NOT EXISTS (
        SELECT 1 FROM message_read_status mrs
        WHERE mrs.user_id = notifications.recipient_id
        AND mrs.message_type = 'notification'
        AND mrs.message_id = notifications.notification_id
    );
    
    DBMS_OUTPUT.PUT_LINE('✓ 通知消息已读状态初始化完成，共处理 ' || SQL%ROWCOUNT || ' 条记录');
    
    -- 初始化议价消息的已读状态
    -- 状态不为"等待回应"的议价标记为已读
    INSERT INTO message_read_status (user_id, message_type, message_id, is_read, read_at, created_at)
    SELECT DISTINCT
        o.buyer_id as user_id,
        'bargain',
        n.negotiation_id,
        CASE WHEN n.status != '等待回应' THEN 1 ELSE 0 END,
        CASE WHEN n.status != '等待回应' THEN n.created_at ELSE NULL END,
        CURRENT_TIMESTAMP
    FROM negotiations n
    JOIN abstract_orders o ON n.order_id = o.abstract_order_id
    WHERE NOT EXISTS (
        SELECT 1 FROM message_read_status mrs
        WHERE mrs.user_id = o.buyer_id
        AND mrs.message_type = 'bargain'
        AND mrs.message_id = n.negotiation_id
    );
    
    DBMS_OUTPUT.PUT_LINE('✓ 买家议价消息已读状态初始化完成，共处理 ' || SQL%ROWCOUNT || ' 条记录');
    
    -- 初始化卖家的议价消息已读状态
    INSERT INTO message_read_status (user_id, message_type, message_id, is_read, read_at, created_at)
    SELECT DISTINCT
        o.seller_id as user_id,
        'bargain',
        n.negotiation_id,
        CASE WHEN n.status != '等待回应' THEN 1 ELSE 0 END,
        CASE WHEN n.status != '等待回应' THEN n.created_at ELSE NULL END,
        CURRENT_TIMESTAMP
    FROM negotiations n
    JOIN abstract_orders o ON n.order_id = o.abstract_order_id
    WHERE NOT EXISTS (
        SELECT 1 FROM message_read_status mrs
        WHERE mrs.user_id = o.seller_id
        AND mrs.message_type = 'bargain'
        AND mrs.message_id = n.negotiation_id
    );
    
    DBMS_OUTPUT.PUT_LINE('✓ 卖家议价消息已读状态初始化完成，共处理 ' || SQL%ROWCOUNT || ' 条记录');
    
    -- 初始化换物消息的已读状态（接收者）
    INSERT INTO message_read_status (user_id, message_type, message_id, is_read, read_at, created_at)
    SELECT 
        rp.user_id as user_id,
        'swap',
        er.exchange_id,
        CASE WHEN er.status != '等待回应' THEN 1 ELSE 0 END,
        CASE WHEN er.status != '等待回应' THEN er.created_at ELSE NULL END,
        CURRENT_TIMESTAMP
    FROM exchange_requests er
    JOIN products rp ON er.request_product_id = rp.product_id  -- 接收换物请求的商品所有者
    WHERE NOT EXISTS (
        SELECT 1 FROM message_read_status mrs
        WHERE mrs.user_id = rp.user_id
        AND mrs.message_type = 'swap'
        AND mrs.message_id = er.exchange_id
    );
    
    DBMS_OUTPUT.PUT_LINE('✓ 换物消息接收者已读状态初始化完成，共处理 ' || SQL%ROWCOUNT || ' 条记录');
    
    -- 初始化换物消息的已读状态（发起者 - 标记为已读）
    INSERT INTO message_read_status (user_id, message_type, message_id, is_read, read_at, created_at)
    SELECT 
        op.user_id as user_id,
        'swap',
        er.exchange_id,
        1, -- 发起者的换物请求始终标记为已读
        er.created_at,
        CURRENT_TIMESTAMP
    FROM exchange_requests er
    JOIN products op ON er.offer_product_id = op.product_id  -- 发起换物请求的商品所有者
    WHERE NOT EXISTS (
        SELECT 1 FROM message_read_status mrs
        WHERE mrs.user_id = op.user_id
        AND mrs.message_type = 'swap'
        AND mrs.message_id = er.exchange_id
    );
    
    DBMS_OUTPUT.PUT_LINE('✓ 换物消息发起者已读状态初始化完成，共处理 ' || SQL%ROWCOUNT || ' 条记录');
    
    COMMIT;
    
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('初始化已读状态时出错: ' || SQLERRM);
        RAISE;
END;
/

-- ================================================================
-- 5. 验证升级结果
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
    FOR stat_rec IN (
        SELECT 
            message_type,
            COUNT(*) as total_count,
            COUNT(CASE WHEN is_read = 1 THEN 1 END) as read_count,
            COUNT(CASE WHEN is_read = 0 THEN 1 END) as unread_count
        FROM message_read_status
        GROUP BY message_type
        ORDER BY message_type
    ) LOOP
        DBMS_OUTPUT.PUT_LINE('');
        DBMS_OUTPUT.PUT_LINE('消息类型: ' || stat_rec.message_type);
        DBMS_OUTPUT.PUT_LINE('  总计: ' || stat_rec.total_count || 
                             ', 已读: ' || stat_rec.read_count || 
                             ', 未读: ' || stat_rec.unread_count);
    END LOOP;
    
    -- 显示总体统计
    FOR total_rec IN (
        SELECT 
            COUNT(*) as total_messages,
            COUNT(CASE WHEN is_read = 1 THEN 1 END) as total_read,
            COUNT(CASE WHEN is_read = 0 THEN 1 END) as total_unread
        FROM message_read_status
    ) LOOP
        DBMS_OUTPUT.PUT_LINE('');
        DBMS_OUTPUT.PUT_LINE('总体统计:');
        DBMS_OUTPUT.PUT_LINE('  总消息数: ' || total_rec.total_messages);
        DBMS_OUTPUT.PUT_LINE('  已读消息: ' || total_rec.total_read);
        DBMS_OUTPUT.PUT_LINE('  未读消息: ' || total_rec.total_unread);
    END LOOP;
    
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('========================================');
    DBMS_OUTPUT.PUT_LINE('消息已读状态系统升级完成！');
    DBMS_OUTPUT.PUT_LINE('统一的消息已读状态管理功能已启用');
    DBMS_OUTPUT.PUT_LINE('========================================');
    
END;
/
