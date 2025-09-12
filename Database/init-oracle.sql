-- ================================================================
-- 校园交易平台数据库初始化脚本 (Oracle)
-- 根据数据库设计文档创建完整的数据库结构
-- 整合了所有字段修复和表结构完善
-- ================================================================

-- 设置容器到 XEPDB1
ALTER SESSION SET CONTAINER=XEPDB1;

-- ================================================================
-- 设置数据库时区配置 (确保所有连接使用 +08:00 时区)
-- ================================================================
-- 设置当前会话时区
ALTER SESSION SET TIME_ZONE = '+08:00';

-- 设置数据库默认时区 (需要在PDB级别设置)
BEGIN
    EXECUTE IMMEDIATE 'ALTER DATABASE SET TIME_ZONE = ''+08:00''';
    DBMS_OUTPUT.PUT_LINE('Database timezone set to +08:00 successfully');
EXCEPTION
    WHEN OTHERS THEN
        -- 如果无法设置数据库级别时区，至少记录警告
        DBMS_OUTPUT.PUT_LINE('Warning: Could not set database timezone: ' || SQLERRM);
        DBMS_OUTPUT.PUT_LINE('Session timezone will be set to +08:00 for current operations');
END;
/

-- 创建用户和授权
-- 设置Oracle脚本模式，允许在PDB中创建用户
BEGIN
    EXECUTE IMMEDIATE 'ALTER SESSION SET "_ORACLE_SCRIPT"=true';
    DBMS_OUTPUT.PUT_LINE('Oracle script mode enabled');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Warning: Could not set Oracle script mode: ' || SQLERRM);
END;
/

-- 启用DBMS_OUTPUT
SET SERVEROUTPUT ON;

-- ================================================================
-- 清理现有用户和表空间
-- ================================================================
-- 删除现有用户 (如果存在)
BEGIN
    EXECUTE IMMEDIATE 'DROP USER CAMPUS_TRADE_USER CASCADE';
    DBMS_OUTPUT.PUT_LINE('User CAMPUS_TRADE_USER dropped successfully');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE = -1918 THEN
            DBMS_OUTPUT.PUT_LINE('User CAMPUS_TRADE_USER does not exist, skipping...');
        ELSE
            DBMS_OUTPUT.PUT_LINE('Error dropping user: ' || SQLERRM);
        END IF;
END;
/

-- 删除现有表空间 (如果存在)
BEGIN
    EXECUTE IMMEDIATE 'DROP TABLESPACE CAMPUS_TRADE_DATA INCLUDING CONTENTS AND DATAFILES';
    DBMS_OUTPUT.PUT_LINE('Tablespace CAMPUS_TRADE_DATA dropped successfully');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE = -959 THEN
            DBMS_OUTPUT.PUT_LINE('Tablespace CAMPUS_TRADE_DATA does not exist, skipping...');
        ELSE
            DBMS_OUTPUT.PUT_LINE('Error dropping tablespace: ' || SQLERRM);
        END IF;
END;
/

-- ================================================================
-- 重新创建表空间和用户
-- ================================================================
-- 创建表空间
CREATE TABLESPACE CAMPUS_TRADE_DATA
DATAFILE '/opt/oracle/oradata/XE/XEPDB1/campus_trade_data.dbf' SIZE 200M
AUTOEXTEND ON NEXT 20M MAXSIZE 2G;

-- 创建用户
CREATE USER CAMPUS_TRADE_USER IDENTIFIED BY "CampusTrade123!"
DEFAULT TABLESPACE CAMPUS_TRADE_DATA
TEMPORARY TABLESPACE TEMP
QUOTA UNLIMITED ON CAMPUS_TRADE_DATA;

-- 授权
GRANT CONNECT, RESOURCE, DBA TO CAMPUS_TRADE_USER;
GRANT CREATE SESSION TO CAMPUS_TRADE_USER;
GRANT CREATE TABLE TO CAMPUS_TRADE_USER;
GRANT CREATE SEQUENCE TO CAMPUS_TRADE_USER;
GRANT CREATE TRIGGER TO CAMPUS_TRADE_USER;

-- 输出用户创建成功信息
SELECT 'User CAMPUS_TRADE_USER created and granted permissions successfully!' AS user_status FROM dual;

-- 连接到用户
-- 使用更兼容的连接方式
CONNECT CAMPUS_TRADE_USER/"CampusTrade123!"@XEPDB1;

-- 验证连接
SELECT 'Successfully connected as: ' || USER AS connection_status FROM dual;

-- 启用DBMS_OUTPUT (用户级别)
SET SERVEROUTPUT ON;

-- 为用户会话设置时区 (确保所有后续操作使用正确时区)
ALTER SESSION SET TIME_ZONE = '+08:00';
SELECT 'User session timezone set to: ' || SESSIONTIMEZONE AS timezone_status FROM dual;

-- ================================================================
-- 清空数据库 - 删除所有表和序列
-- ================================================================
-- 删除表时使用CASCADE CONSTRAINTS来处理外键约束
-- 使用PURGE来彻底删除，避免进入回收站

-- 删除所有表 (按依赖关系逆序删除)
DECLARE
    table_count NUMBER := 0;
BEGIN
    FOR cur_rec IN (SELECT table_name FROM user_tables) LOOP
        EXECUTE IMMEDIATE 'DROP TABLE ' || cur_rec.table_name || ' CASCADE CONSTRAINTS PURGE';
        table_count := table_count + 1;
        DBMS_OUTPUT.PUT_LINE('Dropped table: ' || cur_rec.table_name);
    END LOOP;
    
    IF table_count = 0 THEN
        DBMS_OUTPUT.PUT_LINE('No tables to drop');
    ELSE
        DBMS_OUTPUT.PUT_LINE('Total tables dropped: ' || table_count);
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Error dropping tables: ' || SQLERRM);
END;
/

-- 删除所有序列
DECLARE
    seq_count NUMBER := 0;
BEGIN
    FOR cur_rec IN (SELECT sequence_name FROM user_sequences) LOOP
        EXECUTE IMMEDIATE 'DROP SEQUENCE ' || cur_rec.sequence_name;
        seq_count := seq_count + 1;
        DBMS_OUTPUT.PUT_LINE('Dropped sequence: ' || cur_rec.sequence_name);
    END LOOP;
    
    IF seq_count = 0 THEN
        DBMS_OUTPUT.PUT_LINE('No sequences to drop');
    ELSE
        DBMS_OUTPUT.PUT_LINE('Total sequences dropped: ' || seq_count);
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Error dropping sequences: ' || SQLERRM);
END;
/

-- 删除所有触发器
DECLARE
    trigger_count NUMBER := 0;
BEGIN
    FOR cur_rec IN (SELECT trigger_name FROM user_triggers) LOOP
        EXECUTE IMMEDIATE 'DROP TRIGGER ' || cur_rec.trigger_name;
        trigger_count := trigger_count + 1;
        DBMS_OUTPUT.PUT_LINE('Dropped trigger: ' || cur_rec.trigger_name);
    END LOOP;
    
    IF trigger_count = 0 THEN
        DBMS_OUTPUT.PUT_LINE('No triggers to drop');
    ELSE
        DBMS_OUTPUT.PUT_LINE('Total triggers dropped: ' || trigger_count);
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Error dropping triggers: ' || SQLERRM);
END;
/

-- 输出清理完成信息
SELECT 'Database cleanup completed - all tables, sequences, and triggers removed!' AS cleanup_message FROM dual;

-- ================================================================
-- 创建序列 (用于自增ID)
-- ================================================================
CREATE SEQUENCE USER_SEQ START WITH 1 INCREMENT BY 1 NOCACHE;
CREATE SEQUENCE PRODUCT_SEQ START WITH 1 INCREMENT BY 1 NOCACHE;
CREATE SEQUENCE ABSTRACT_ORDER_SEQ START WITH 1 INCREMENT BY 1 NOCACHE;
CREATE SEQUENCE CATEGORY_SEQ START WITH 1 INCREMENT BY 1 NOCACHE;

-- ================================================================
-- 1. 学生信息表 (students)
-- ================================================================
CREATE TABLE students (
    student_id VARCHAR2(20) PRIMARY KEY,
    name VARCHAR2(50) NOT NULL,
    department VARCHAR2(50)
);

-- ================================================================
-- 2. 用户表 (users) - 完整版本包含所有Entity Framework字段
-- ================================================================
CREATE TABLE users (
    user_id NUMBER PRIMARY KEY,
    email VARCHAR2(100) NOT NULL UNIQUE,
    credit_score NUMBER(3,1) DEFAULT 60.0,
    password_hash VARCHAR2(128) NOT NULL,
    student_id VARCHAR2(20) NOT NULL UNIQUE,
    username VARCHAR2(50),
    full_name VARCHAR2(100),
    phone VARCHAR2(20),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_active NUMBER(1) DEFAULT 1 CHECK (is_active IN (0,1)),
    -- 额外字段
    last_login_at TIMESTAMP,
    last_login_ip VARCHAR2(45),
    login_count NUMBER DEFAULT 0,
    is_locked NUMBER(1) DEFAULT 0 CHECK (is_locked IN (0,1)),
    lockout_end TIMESTAMP,
    failed_login_attempts NUMBER DEFAULT 0,
    two_factor_enabled NUMBER(1) DEFAULT 0 CHECK (two_factor_enabled IN (0,1)),
    password_changed_at TIMESTAMP,
    security_stamp VARCHAR2(256),
    email_verified NUMBER(1) DEFAULT 0 CHECK (email_verified IN (0,1)),
    email_verification_token VARCHAR2(256),
    CONSTRAINT fk_users_student FOREIGN KEY (student_id) REFERENCES students(student_id)
);

-- ================================================================
-- 3. 刷新令牌表 (refresh_tokens) - Entity Framework 需要
-- ================================================================
CREATE TABLE refresh_tokens (
    id VARCHAR2(36) PRIMARY KEY,
    token VARCHAR2(500) NOT NULL,
    user_id NUMBER NOT NULL,
    expiry_date TIMESTAMP NOT NULL,
    is_revoked NUMBER(1) DEFAULT 0 CHECK (is_revoked IN (0,1)),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
    revoked_at TIMESTAMP,
    ip_address VARCHAR2(45),
    user_agent VARCHAR2(500),
    device_id VARCHAR2(100),
    replaced_by_token VARCHAR2(500),
    created_by NUMBER,
    last_used_at TIMESTAMP,
    revoked_by NUMBER,
    revoke_reason VARCHAR2(200),
    CONSTRAINT fk_refresh_tokens_user FOREIGN KEY (user_id) REFERENCES users(user_id),
    CONSTRAINT fk_refresh_tokens_created_by FOREIGN KEY (created_by) REFERENCES users(user_id),
    CONSTRAINT fk_refresh_tokens_revoked_by FOREIGN KEY (revoked_by) REFERENCES users(user_id)
);

-- 创建刷新令牌表的索引
CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX idx_refresh_tokens_token ON refresh_tokens(token);
CREATE INDEX idx_refresh_tokens_expiry ON refresh_tokens(expiry_date);
CREATE INDEX idx_refresh_tokens_device ON refresh_tokens(device_id);

-- ================================================================
-- 4. 信用变更记录表 (credit_history)
-- ================================================================
CREATE TABLE credit_history (
    log_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id NUMBER NOT NULL,
    change_type VARCHAR2(20) CHECK (change_type IN ('交易完成','举报处罚','好评奖励')),
    new_score NUMBER(3,1) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_credit_user FOREIGN KEY (user_id) REFERENCES users(user_id)
);

-- ================================================================
-- 5. 登录日志表 (login_logs)
-- ================================================================
CREATE TABLE login_logs (
    log_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id NUMBER NOT NULL,
    ip_address VARCHAR2(45),
    log_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    device_type VARCHAR2(20) CHECK (device_type IN ('Mobile','PC','Tablet')),
    risk_level NUMBER CHECK (risk_level IN (0,1,2)),
    CONSTRAINT fk_login_user FOREIGN KEY (user_id) REFERENCES users(user_id)
);

-- ================================================================
-- 6. 邮箱验证表 (email_verification)
-- ================================================================
CREATE TABLE email_verification (
    verification_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id NUMBER NOT NULL,
    email VARCHAR2(100) NOT NULL,
    verification_code VARCHAR2(6),
    token VARCHAR2(64),
    expire_time TIMESTAMP,
    is_used NUMBER(1) DEFAULT 0 CHECK (is_used IN (0,1)),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_verification_user FOREIGN KEY (user_id) REFERENCES users(user_id)
);

-- ================================================================
-- 7. 分类表 (categories)
-- ================================================================
CREATE TABLE categories (
    category_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    parent_id NUMBER,
    name VARCHAR2(50) NOT NULL,
    CONSTRAINT fk_category_parent FOREIGN KEY (parent_id) REFERENCES categories(category_id)
);

-- ================================================================
-- 8. 商品表 (products)
-- ================================================================
CREATE TABLE products (
    product_id NUMBER PRIMARY KEY,
    user_id NUMBER NOT NULL,
    category_id NUMBER NOT NULL,
    title VARCHAR2(100) NOT NULL,
    description CLOB,
    base_price NUMBER(10,2) NOT NULL,
    publish_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    view_count NUMBER DEFAULT 0,
    auto_remove_time TIMESTAMP,
    status VARCHAR2(20) DEFAULT '在售' CHECK (status IN ('在售','已下架','交易中')),
    CONSTRAINT fk_product_user FOREIGN KEY (user_id) REFERENCES users(user_id),
    CONSTRAINT fk_product_category FOREIGN KEY (category_id) REFERENCES categories(category_id)
);

-- ================================================================
-- 9. 商品图片表 (product_images)
-- ================================================================
CREATE TABLE product_images (
    image_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    product_id NUMBER NOT NULL,
    image_url VARCHAR2(200) NOT NULL,
    CONSTRAINT fk_image_product FOREIGN KEY (product_id) REFERENCES products(product_id)
);

-- ================================================================
-- 10. 抽象订单表 (abstract_orders)
-- ================================================================
CREATE TABLE abstract_orders (
    abstract_order_id NUMBER PRIMARY KEY,
    order_type VARCHAR2(20) CHECK (order_type IN ('normal','exchange'))
);

-- ================================================================
-- 11. 订单表 (orders)
-- ================================================================
CREATE TABLE orders (
    order_id NUMBER PRIMARY KEY,
    buyer_id NUMBER NOT NULL,
    seller_id NUMBER NOT NULL,
    product_id NUMBER NOT NULL,
    total_amount NUMBER(10,2),
    status VARCHAR2(20) DEFAULT '待付款' CHECK (status IN ('议价中','待付款','已付款','已发货','已送达','已完成','已取消')),
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expire_time TIMESTAMP,
    final_price NUMBER(10,2),
    CONSTRAINT fk_order_abstract FOREIGN KEY (order_id) REFERENCES abstract_orders(abstract_order_id),
    CONSTRAINT fk_order_buyer FOREIGN KEY (buyer_id) REFERENCES users(user_id),
    CONSTRAINT fk_order_seller FOREIGN KEY (seller_id) REFERENCES users(user_id),
    CONSTRAINT fk_order_product FOREIGN KEY (product_id) REFERENCES products(product_id)
);

-- ================================================================
-- 12. 虚拟账户表 (virtual_accounts)
-- ================================================================
CREATE TABLE virtual_accounts (
    account_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id NUMBER NOT NULL UNIQUE,
    balance NUMBER(10,2) DEFAULT 0.00,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_account_user FOREIGN KEY (user_id) REFERENCES users(user_id)
);

-- ================================================================
-- 13. 充值记录表 (recharge_records)
-- ================================================================
CREATE TABLE recharge_records (
    recharge_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id NUMBER NOT NULL,
    amount NUMBER(10,2) NOT NULL,
    status VARCHAR2(20) DEFAULT '处理中' CHECK (status IN ('处理中','成功','失败')),
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    complete_time TIMESTAMP,
    CONSTRAINT fk_recharge_user FOREIGN KEY (user_id) REFERENCES users(user_id)
);

-- ================================================================
-- 14. 议价表 (negotiations)
-- ================================================================
CREATE TABLE negotiations (
    negotiation_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    order_id NUMBER NOT NULL,
    proposed_price NUMBER(10,2) NOT NULL,
    status VARCHAR2(20) DEFAULT '等待回应' CHECK (status IN ('等待回应','接受','拒绝','反报价')),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_negotiation_order FOREIGN KEY (order_id) REFERENCES orders(order_id)
);

-- ================================================================
-- 15. 管理员表 (admins)
-- ================================================================
CREATE TABLE admins (
    admin_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id NUMBER NOT NULL UNIQUE,
    role VARCHAR2(20) CHECK (role IN ('super','category_admin','report_admin')),
    assigned_category NUMBER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_admin_user FOREIGN KEY (user_id) REFERENCES users(user_id),
    CONSTRAINT fk_admin_category FOREIGN KEY (assigned_category) REFERENCES categories(category_id),
    CONSTRAINT chk_admin_category CHECK (
        (role = 'category_admin' AND assigned_category IS NOT NULL) OR 
        (role != 'category_admin' AND assigned_category IS NULL)
    )
);

-- ================================================================
-- 16. 审计日志表 (audit_logs)
-- ================================================================
CREATE TABLE audit_logs (
    log_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    admin_id NUMBER NOT NULL,
    action_type VARCHAR2(20) CHECK (action_type IN ('封禁用户','修改权限','处理举报','更新商品','删除商品','批量操作')),
    target_id NUMBER,
    log_detail CLOB,
    log_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_audit_admin FOREIGN KEY (admin_id) REFERENCES admins(admin_id)
);

-- ================================================================
-- 17. 换物请求表 (exchange_requests)
-- ================================================================
CREATE TABLE exchange_requests (
    exchange_id NUMBER PRIMARY KEY,
    offer_product_id NUMBER NOT NULL,
    request_product_id NUMBER NOT NULL,
    terms CLOB,
    status VARCHAR2(20) DEFAULT '等待回应' CHECK (status IN ('等待回应','接受','拒绝','反报价')),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_exchange_abstract FOREIGN KEY (exchange_id) REFERENCES abstract_orders(abstract_order_id),
    CONSTRAINT fk_exchange_offer FOREIGN KEY (offer_product_id) REFERENCES products(product_id),
    CONSTRAINT fk_exchange_request FOREIGN KEY (request_product_id) REFERENCES products(product_id)
);

-- ================================================================
-- 18. 通知模板表 (notification_templates)
-- ================================================================
CREATE TABLE notification_templates (
    template_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    template_name VARCHAR2(100) NOT NULL,
    template_type VARCHAR2(20) CHECK (template_type IN ('商品相关','交易相关','评价相关','系统通知')),
    template_content CLOB NOT NULL,
    description VARCHAR2(500),
    priority NUMBER DEFAULT 2 CHECK (priority BETWEEN 1 AND 5),
    is_active NUMBER(1) DEFAULT 1 CHECK (is_active IN (0,1)),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by NUMBER,
    CONSTRAINT fk_template_creator FOREIGN KEY (created_by) REFERENCES users(user_id)
);

-- ================================================================
-- 19. 通知表 (notifications)
-- ================================================================
CREATE TABLE notifications (
    notification_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    template_id NUMBER NOT NULL,
    recipient_id NUMBER NOT NULL,
    order_id NUMBER,
    template_params CLOB,
    send_status VARCHAR2(20) DEFAULT '待发送' CHECK (send_status IN ('待发送','成功','失败')),
    retry_count NUMBER DEFAULT 0,
    last_attempt_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    sent_at TIMESTAMP,
    is_read NUMBER(1) DEFAULT 0 CHECK (is_read IN (0, 1)),
    read_at TIMESTAMP,
    -- 分离式发送状态跟踪字段
    signalr_status VARCHAR2(20) DEFAULT '待发送' CHECK (signalr_status IN ('待发送','成功','失败')),
    email_status VARCHAR2(20) DEFAULT '待发送' CHECK (email_status IN ('待发送','成功','失败')),
    signalr_retry_count NUMBER DEFAULT 0 CHECK (signalr_retry_count >= 0),
    email_retry_count NUMBER DEFAULT 0 CHECK (email_retry_count >= 0),
    signalr_last_attempt TIMESTAMP,
    email_last_attempt TIMESTAMP,
    CONSTRAINT fk_notification_template FOREIGN KEY (template_id) REFERENCES notification_templates(template_id),
    CONSTRAINT fk_notification_recipient FOREIGN KEY (recipient_id) REFERENCES users(user_id),
    CONSTRAINT fk_notification_order FOREIGN KEY (order_id) REFERENCES abstract_orders(abstract_order_id)
);

-- ================================================================
-- 20. SignalR通知表 (signalr_notifications)
-- ================================================================
CREATE TABLE signalr_notifications (
    signalr_notification_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    notification_id NUMBER NOT NULL,
    connection_id VARCHAR2(100),
    group_name VARCHAR2(50),
    send_status VARCHAR2(20) DEFAULT '待发送' CHECK (send_status IN ('待发送','成功','失败')),
    retry_count NUMBER DEFAULT 0 CHECK (retry_count >= 0),
    last_attempt_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    sent_at TIMESTAMP,
    error_message VARCHAR2(500),
    CONSTRAINT fk_signalr_notification FOREIGN KEY (notification_id) REFERENCES notifications(notification_id) ON DELETE CASCADE
);

-- ================================================================
-- 21. 邮件通知表 (email_notifications)
-- ================================================================
CREATE TABLE email_notifications (
    email_notification_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    email_type VARCHAR2(20) NOT NULL CHECK (email_type IN ('通知','验证码')),
    notification_id NUMBER,
    recipient_email VARCHAR2(100) NOT NULL,
    subject VARCHAR2(200) NOT NULL,
    content CLOB NOT NULL,
    verification_code VARCHAR2(10),
    code_expires_at TIMESTAMP,
    send_status VARCHAR2(20) DEFAULT '待发送' CHECK (send_status IN ('待发送','成功','失败')),
    retry_count NUMBER DEFAULT 0 CHECK (retry_count >= 0),
    last_attempt_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    sent_at TIMESTAMP,
    error_message VARCHAR2(500),
    CONSTRAINT fk_email_notification FOREIGN KEY (notification_id) REFERENCES notifications(notification_id) ON DELETE SET NULL
);

-- ================================================================
-- 22. 评价表 (reviews)
-- ================================================================
CREATE TABLE reviews (
    review_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    order_id NUMBER NOT NULL,
    rating NUMBER(2,1) CHECK (rating BETWEEN 1 AND 5),
    desc_accuracy NUMBER(2,0) CHECK (desc_accuracy BETWEEN 1 AND 5),
    service_attitude NUMBER(2,0) CHECK (service_attitude BETWEEN 1 AND 5),
    is_anonymous NUMBER(1) DEFAULT 0 CHECK (is_anonymous IN (0,1)),
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    seller_reply CLOB,
    content CLOB,
    CONSTRAINT fk_review_order FOREIGN KEY (order_id) REFERENCES abstract_orders(abstract_order_id)
);

-- ================================================================
-- 23. 举报表 (reports)
-- ================================================================
CREATE TABLE reports (
    report_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    order_id NUMBER NOT NULL,
    reporter_id NUMBER NOT NULL,
    type VARCHAR2(50) CHECK (type IN ('商品问题','服务问题','欺诈','虚假描述','争议评价','其他')),
    priority NUMBER(2,0) CHECK (priority BETWEEN 1 AND 10),
    description CLOB,
    status VARCHAR2(20) DEFAULT '待处理' CHECK (status IN ('待处理','处理中','已处理','已关闭','已驳回','需要补充信息','待审核')),
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_report_order FOREIGN KEY (order_id) REFERENCES abstract_orders(abstract_order_id),
    CONSTRAINT fk_report_user FOREIGN KEY (reporter_id) REFERENCES users(user_id)
);

-- ================================================================
-- 24. 举报证据表 (report_evidence)
-- ================================================================
CREATE TABLE report_evidence (
    evidence_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    report_id NUMBER NOT NULL,
    file_type VARCHAR2(20) CHECK (file_type IN ('图片','视频','文档')),
    file_url VARCHAR2(200) NOT NULL,
    uploaded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_evidence_report FOREIGN KEY (report_id) REFERENCES reports(report_id)
);

-- ================================================================
-- 创建索引 (针对新增的通知渠道表)
-- ================================================================

-- SignalR通知表索引
CREATE INDEX idx_signalr_notifications_notification_id ON signalr_notifications(notification_id);
CREATE INDEX idx_signalr_notifications_send_status ON signalr_notifications(send_status);
CREATE INDEX idx_signalr_notifications_connection_id ON signalr_notifications(connection_id);
CREATE INDEX idx_signalr_notifications_created_at ON signalr_notifications(created_at);

-- 邮件通知表索引
CREATE INDEX idx_email_notifications_notification_id ON email_notifications(notification_id);
CREATE INDEX idx_email_notifications_email_type ON email_notifications(email_type);
CREATE INDEX idx_email_notifications_send_status ON email_notifications(send_status);
CREATE INDEX idx_email_notifications_recipient_email ON email_notifications(recipient_email);
CREATE INDEX idx_email_notifications_created_at ON email_notifications(created_at);
CREATE INDEX idx_email_notifications_verification_code ON email_notifications(verification_code);
CREATE INDEX idx_email_notifications_code_expires ON email_notifications(code_expires_at);

-- 复合索引
CREATE INDEX idx_email_notifications_type_status ON email_notifications(email_type, send_status);
CREATE INDEX idx_email_notifications_email_type_comb ON email_notifications(recipient_email, email_type);

-- 通知表已读状态索引
CREATE INDEX idx_notifications_is_read ON notifications(is_read);
CREATE INDEX idx_notifications_read_at ON notifications(read_at);
CREATE INDEX idx_notifications_user_unread ON notifications(recipient_id, is_read);

-- 分离式发送状态索引
CREATE INDEX idx_notifications_signalr_status ON notifications(signalr_status);
CREATE INDEX idx_notifications_email_status ON notifications(email_status);
CREATE INDEX idx_notifications_signalr_retry ON notifications(signalr_status, signalr_retry_count, signalr_last_attempt);
CREATE INDEX idx_notifications_email_retry ON notifications(email_status, email_retry_count, email_last_attempt);

-- ================================================================
-- 创建触发器
-- ================================================================

-- 用户登录时自动设置时区触发器 (确保所有连接使用 +08:00 时区)
CREATE OR REPLACE TRIGGER campus_trade_logon_trigger
    AFTER LOGON ON SCHEMA
BEGIN
    -- 为每个新连接的会话设置时区
    EXECUTE IMMEDIATE 'ALTER SESSION SET TIME_ZONE = ''+08:00''';
EXCEPTION
    WHEN OTHERS THEN
        -- 如果设置时区失败，记录到系统日志 (不阻止登录)
        NULL;
END;
/

-- 用户ID自增触发器 (改进版本，支持手动指定和自动生成)
CREATE OR REPLACE TRIGGER users_id_trigger
    BEFORE INSERT ON users
    FOR EACH ROW
BEGIN
    IF :NEW.user_id IS NULL THEN
        :NEW.user_id := USER_SEQ.NEXTVAL;
    END IF;
END;
/

-- 用户更新时间触发器
CREATE OR REPLACE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW
BEGIN
    :NEW.updated_at := CURRENT_TIMESTAMP;
END;
/

-- 商品ID自增触发器
CREATE OR REPLACE TRIGGER trg_products_id
    BEFORE INSERT ON products
    FOR EACH ROW
BEGIN
    IF :NEW.product_id IS NULL THEN
        :NEW.product_id := PRODUCT_SEQ.NEXTVAL;
    END IF;
END;
/

-- 抽象订单ID自增触发器
CREATE OR REPLACE TRIGGER trg_abstract_orders_id
    BEFORE INSERT ON abstract_orders
    FOR EACH ROW
BEGIN
    IF :NEW.abstract_order_id IS NULL THEN
        :NEW.abstract_order_id := ABSTRACT_ORDER_SEQ.NEXTVAL;
    END IF;
END;
/

-- 订单过期时间触发器（默认30分钟）
CREATE OR REPLACE TRIGGER trg_orders_expire_time
    BEFORE INSERT ON orders
    FOR EACH ROW
BEGIN
    IF :NEW.expire_time IS NULL THEN
        :NEW.expire_time := CURRENT_TIMESTAMP + INTERVAL '30' MINUTE;
    END IF;
END;

-- 换物请求抽象订单触发器已移除
-- 现在由ExchangeService手动管理抽象订单的创建
-- CREATE OR REPLACE TRIGGER trg_exchange_abstract
--     BEFORE INSERT ON exchange_requests
--     FOR EACH ROW
-- BEGIN
--     -- 先插入抽象订单
--     INSERT INTO abstract_orders (abstract_order_id, order_type) 
--     VALUES (:NEW.exchange_id, 'exchange');
-- END;
-- /
/

-- 用户注册时自动创建虚拟账户
CREATE OR REPLACE TRIGGER trg_users_virtual_account
    AFTER INSERT ON users
    FOR EACH ROW
BEGIN
    INSERT INTO virtual_accounts (user_id, balance)
    VALUES (:NEW.user_id, 0.00);
END;
/

-- ================================================================
-- 添加表注释 (针对新增的通知渠道表)
-- ================================================================
COMMENT ON TABLE signalr_notifications IS 'SignalR通知发送记录表';
COMMENT ON COLUMN signalr_notifications.signalr_notification_id IS 'SignalR通知ID';
COMMENT ON COLUMN signalr_notifications.notification_id IS '关联的通知ID';
COMMENT ON COLUMN signalr_notifications.connection_id IS 'SignalR连接ID';
COMMENT ON COLUMN signalr_notifications.group_name IS '用户组名称（用于群发）';
COMMENT ON COLUMN signalr_notifications.send_status IS '发送状态：待发送、成功、失败';
COMMENT ON COLUMN signalr_notifications.retry_count IS '重试次数';
COMMENT ON COLUMN signalr_notifications.last_attempt_time IS '最后尝试发送时间';
COMMENT ON COLUMN signalr_notifications.created_at IS '创建时间';
COMMENT ON COLUMN signalr_notifications.sent_at IS '发送成功时间';
COMMENT ON COLUMN signalr_notifications.error_message IS '错误信息';

COMMENT ON TABLE email_notifications IS '邮件通知发送记录表';
COMMENT ON COLUMN email_notifications.email_notification_id IS '邮件通知ID';
COMMENT ON COLUMN email_notifications.email_type IS '邮件类型：通知、验证码';
COMMENT ON COLUMN email_notifications.notification_id IS '关联的通知ID（验证码类型时可为空）';
COMMENT ON COLUMN email_notifications.recipient_email IS '收件人邮箱地址';
COMMENT ON COLUMN email_notifications.subject IS '邮件主题';
COMMENT ON COLUMN email_notifications.content IS '邮件内容（HTML格式）';
COMMENT ON COLUMN email_notifications.verification_code IS '验证码（仅验证码类型使用）';
COMMENT ON COLUMN email_notifications.code_expires_at IS '验证码过期时间（仅验证码类型使用）';
COMMENT ON COLUMN email_notifications.send_status IS '发送状态：待发送、成功、失败';
COMMENT ON COLUMN email_notifications.retry_count IS '重试次数';
COMMENT ON COLUMN email_notifications.last_attempt_time IS '最后尝试发送时间';
COMMENT ON COLUMN email_notifications.created_at IS '创建时间';
COMMENT ON COLUMN email_notifications.sent_at IS '发送成功时间';
COMMENT ON COLUMN email_notifications.error_message IS '错误信息';

-- 通知表已读状态字段注释
COMMENT ON COLUMN notifications.is_read IS '是否已读：0=未读, 1=已读';
COMMENT ON COLUMN notifications.read_at IS '已读时间';

-- 分离式发送状态字段注释
COMMENT ON COLUMN notifications.signalr_status IS 'SignalR发送状态：待发送、成功、失败';
COMMENT ON COLUMN notifications.email_status IS '邮件发送状态：待发送、成功、失败';
COMMENT ON COLUMN notifications.signalr_retry_count IS 'SignalR重试次数';
COMMENT ON COLUMN notifications.email_retry_count IS '邮件重试次数';
COMMENT ON COLUMN notifications.signalr_last_attempt IS 'SignalR最后尝试发送时间';
COMMENT ON COLUMN notifications.email_last_attempt IS '邮件最后尝试发送时间';

-- ================================================================
-- 插入基础数据
-- ================================================================

-- 插入学生信息
INSERT INTO students (student_id, name, department) VALUES ('ADMIN001', '系统管理员', '计算机学院');
INSERT INTO students (student_id, name, department) VALUES ('STU001', '张三', '计算机学院');
INSERT INTO students (student_id, name, department) VALUES ('STU002', '李四', '电子信息学院');
INSERT INTO students (student_id, name, department) VALUES ('STU003', '王五', '机械工程学院');

-- 更多学生信息
INSERT INTO students (student_id, name, department) VALUES ('2352495', '张竹和', '计算机科学与技术学院');
INSERT INTO students (student_id, name, department) VALUES ('2353018', '钱宝强', '计算机科学与技术学院');
INSERT INTO students (student_id, name, department) VALUES ('2351427', '缪语欣', '计算机科学与技术学院');
INSERT INTO students (student_id, name, department) VALUES ('2354177', '陈雷诗语', '计算机科学与技术学院');
INSERT INTO students (student_id, name, department) VALUES ('2352755', '刘奕含', '计算机科学与技术学院');
INSERT INTO students (student_id, name, department) VALUES ('2352491', '郭艺', '计算机科学与技术学院');
INSERT INTO students (student_id, name, department) VALUES ('2351284', '李思远', '计算机科学与技术学院');
INSERT INTO students (student_id, name, department) VALUES ('2354269', '刘笑云', '计算机科学与技术学院');
INSERT INTO students (student_id, name, department) VALUES ('2352749', '戴湘宁', '计算机科学与技术学院');
INSERT INTO students (student_id, name, department) VALUES ('2351588', '谭鹏翀', '计算机科学与技术学院');

-- ================================================================
-- 插入校园交易系统标准分类数据
-- ================================================================

-- 插入一级分类（校园交易系统专用分类）
INSERT INTO categories (parent_id, name) VALUES (NULL, '教材');
INSERT INTO categories (parent_id, name) VALUES (NULL, '数码');
INSERT INTO categories (parent_id, name) VALUES (NULL, '日用');
INSERT INTO categories (parent_id, name) VALUES (NULL, '服装');
INSERT INTO categories (parent_id, name) VALUES (NULL, '运动');
INSERT INTO categories (parent_id, name) VALUES (NULL, '其他');

-- 插入二级分类（使用查询获取父分类ID）
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

-- 插入三级标签（特殊属性标签）
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

-- 插入用户数据
-- 密码为 "password" 的 BCrypt 哈希值: $2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi
INSERT INTO users (username, email, password_hash, full_name, student_id, credit_score, login_count, is_locked, failed_login_attempts, two_factor_enabled, email_verified, security_stamp) 
VALUES ('admin', 'admin@campus.edu', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', '系统管理员', 'ADMIN001', 99.9, 0, 0, 0, 0, 1, SYS_GUID());

-- 设置管理员
INSERT INTO admins (user_id, role) VALUES (1, 'super');

-- 提交所有更改
COMMIT;

-- ================================================================
-- 验证表创建结果
-- ================================================================
SELECT '========================================' AS separator FROM dual;
SELECT 'DATABASE CREATION SUMMARY' AS title FROM dual;
SELECT '========================================' AS separator FROM dual;

-- 验证时区设置
SELECT 'Current Database Timezone: ' || DBTIMEZONE AS db_timezone FROM dual;
SELECT 'Current Session Timezone: ' || SESSIONTIMEZONE AS session_timezone FROM dual;
SELECT 'Current Timestamp with Timezone: ' || CURRENT_TIMESTAMP AS current_time FROM dual;

-- 显示创建的表
SELECT 'Tables created successfully:' AS message FROM dual;
SELECT table_name FROM user_tables ORDER BY table_name;

-- 显示创建的序列
SELECT 'Sequences created:' AS message FROM dual;
SELECT sequence_name FROM user_sequences ORDER BY sequence_name;

-- 显示创建的触发器
SELECT 'Triggers created:' AS message FROM dual;
SELECT trigger_name FROM user_triggers ORDER BY trigger_name;

-- 验证数据插入结果
SELECT '========================================' AS separator FROM dual;
SELECT 'SAMPLE DATA VERIFICATION' AS title FROM dual;
SELECT '========================================' AS separator FROM dual;

SELECT COUNT(*) AS student_count FROM students;
SELECT COUNT(*) AS user_count FROM users;
SELECT COUNT(*) AS category_count FROM categories;
SELECT COUNT(*) AS product_count FROM products;
SELECT COUNT(*) AS virtual_account_count FROM virtual_accounts;
SELECT COUNT(*) AS refresh_token_count FROM refresh_tokens;

-- 显示校园交易系统标准分类结构
SELECT '========================================' AS separator FROM dual;
SELECT 'CAMPUS TRADING SYSTEM CATEGORIES' AS title FROM dual;
SELECT '========================================' AS separator FROM dual;

-- 显示一级分类
SELECT '一级分类:' AS level, name FROM categories WHERE parent_id IS NULL ORDER BY name;

-- 显示二级分类
SELECT '二级分类:' AS level, c1.name || ' -> ' || c2.name AS category_path
FROM categories c1
JOIN categories c2 ON c1.category_id = c2.parent_id
WHERE c1.parent_id IS NULL
ORDER BY c1.name, c2.name;

-- 显示三级标签
SELECT '三级标签:' AS level, c1.name || ' -> ' || c2.name || ' -> ' || c3.name AS category_path
FROM categories c1
JOIN categories c2 ON c1.category_id = c2.parent_id
JOIN categories c3 ON c2.category_id = c3.parent_id
WHERE c1.parent_id IS NULL
ORDER BY c1.name, c2.name, c3.name;

-- 显示用户和对应的虚拟账户
SELECT '========================================' AS separator FROM dual;
SELECT 'USER ACCOUNTS WITH VIRTUAL BALANCES' AS title FROM dual;
SELECT '========================================' AS separator FROM dual;

SELECT u.username, u.full_name, u.credit_score, va.balance 
FROM users u 
LEFT JOIN virtual_accounts va ON u.user_id = va.user_id;

-- 验证通知表已读状态结构
SELECT '========================================' AS separator FROM dual;
SELECT 'NOTIFICATIONS TABLE WITH READ STATUS' AS title FROM dual;
SELECT '========================================' AS separator FROM dual;

SELECT 'Notifications table structure (read status fields):' AS message FROM dual;
SELECT column_name, data_type, nullable, data_default 
FROM user_tab_columns 
WHERE table_name = 'NOTIFICATIONS' 
AND column_name IN ('IS_READ', 'READ_AT')
ORDER BY column_name;

SELECT 'Notifications table structure (separated send status fields):' AS message FROM dual;
SELECT column_name, data_type, nullable, data_default 
FROM user_tab_columns 
WHERE table_name = 'NOTIFICATIONS' 
AND column_name IN ('SIGNALR_STATUS', 'EMAIL_STATUS', 'SIGNALR_RETRY_COUNT', 
                   'EMAIL_RETRY_COUNT', 'SIGNALR_LAST_ATTEMPT', 'EMAIL_LAST_ATTEMPT')
ORDER BY column_name;

-- 验证USERS表结构
SELECT '========================================' AS separator FROM dual;
SELECT 'USERS TABLE STRUCTURE' AS title FROM dual;
SELECT '========================================' AS separator FROM dual;
DESC users;

SELECT '========================================' AS separator FROM dual;
SELECT 'Database initialization complete with campus trading system categories and message read status!' AS final_message FROM dual; 
SELECT 'Ready for application usage!' AS status FROM dual;
SELECT '✅ 分类体系：教材 | 数码 | 日用 | 服装 | 运动 | 其他' AS category_system FROM dual;
SELECT '✅ 消息已读状态：已整合到 notifications 表 (is_read, read_at 字段)' AS message_read_status FROM dual;
SELECT '✅ 分离式发送状态：已整合到 notifications 表 (signalr_status, email_status 等字段)' AS separated_send_status FROM dual;
SELECT '✅ 时区配置：所有连接自动使用 +08:00 时区 (数据库级别 + 登录触发器)' AS timezone_config FROM dual;
SELECT '========================================' AS separator FROM dual; 