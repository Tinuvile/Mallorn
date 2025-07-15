-- ================================================================
-- 校园交易平台 - 通知模板数据插入脚本
-- 说明: 包含系统预定义的所有通知模板，支持参数化消息
-- ================================================================

-- 设置容器到 XEPDB1
ALTER SESSION SET CONTAINER=XEPDB1;

-- 连接到用户
CONNECT CAMPUS_TRADE_USER/"CampusTrade123!@XEPDB1";

-- ================================================================
-- 清理现有模板数据（可选，用于重新初始化）
-- ================================================================
-- DELETE FROM notification_templates WHERE template_id > 0;
-- COMMIT;

-- ================================================================
-- 交易相关通知模板
-- ================================================================

-- 1. 订单状态更新模板
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '订单状态更新', 
    '交易相关', 
    '您的订单 #{orderId} 状态已更新为：{status}。{additionalInfo}', 
    '订单状态变更时发送给买家和卖家的通知', 
    3, 1, 1
);

-- 2. 支付提醒模板  
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '支付提醒', 
    '交易相关', 
    '您的订单 #{orderId} 即将过期，请尽快完成支付。过期时间：{expireTime}，订单金额：￥{amount}', 
    '订单快要过期时提醒买家支付', 
    4, 1, 1
);

-- 3. 支付成功确认
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '支付成功确认', 
    '交易相关', 
    '订单 #{orderId} 支付成功！支付金额：￥{amount}，卖家将尽快为您发货。', 
    '买家支付成功后的确认通知', 
    3, 1, 1
);

-- 4. 发货通知模板
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '发货通知', 
    '交易相关', 
    '您的订单 #{orderId} 已发货！{trackingInfo}预计 {deliveryTime} 送达，请保持手机畅通。', 
    '卖家发货后通知买家', 
    3, 1, 1
);

-- 5. 确认收货提醒
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '确认收货提醒', 
    '交易相关', 
    '您的订单 #{orderId} 已送达，请确认收货。如有问题请及时联系卖家。', 
    '商品送达后提醒买家确认收货', 
    3, 1, 1
);

-- 6. 交易完成祝贺
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '交易完成', 
    '交易相关', 
    '恭喜！您的订单 #{orderId} 交易已完成。感谢您使用校园交易平台，期待您的下次光临！', 
    '订单完成后的祝贺通知', 
    2, 1, 1
);

-- 7. 订单取消通知
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '订单取消通知', 
    '交易相关', 
    '您的订单 #{orderId} 已取消。取消原因：{reason}。如有疑问请联系客服。', 
    '订单被取消时的通知', 
    3, 1, 1
);

-- 8. 退款通知
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '退款通知', 
    '交易相关', 
    '您的订单 #{orderId} 退款已处理完成。退款金额：￥{refundAmount}，预计1-3个工作日到账。', 
    '退款处理完成通知', 
    4, 1, 1
);

-- ================================================================
-- 商品相关通知模板  
-- ================================================================

-- 9. 商品上架成功
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '商品上架成功', 
    '商品相关', 
    '您发布的商品 "{productTitle}" 已成功上架！商品编号：{productId}，快去查看吧～', 
    '商品发布成功通知', 
    2, 1, 1
);

-- 10. 商品下架通知
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '商品下架通知', 
    '商品相关', 
    '您的商品 "{productTitle}" 已被下架。下架原因：{reason}。如有疑问请联系管理员。', 
    '商品被下架时通知卖家', 
    3, 1, 1
);

-- 11. 商品有新询问
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '商品有新询问', 
    '商品相关', 
    '您的商品 "{productTitle}" 收到新的买家询问，请及时回复以促成交易。', 
    '有人对商品感兴趣时通知卖家', 
    3, 1, 1
);

-- 12. 商品价格变动
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '商品价格变动', 
    '商品相关', 
    '您关注的商品 "{productTitle}" 价格已调整：由 ￥{oldPrice} 调整为 ￥{newPrice}', 
    '关注的商品价格发生变化时通知', 
    2, 1, 1
);

-- 13. 商品即将过期
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '商品即将过期', 
    '商品相关', 
    '您的商品 "{productTitle}" 即将过期下架（过期时间：{expireTime}），请及时续期或调整。', 
    '商品快要过期时提醒卖家', 
    3, 1, 1
);

-- ================================================================
-- 议价相关通知模板
-- ================================================================

-- 14. 收到议价请求
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '收到议价请求', 
    '交易相关', 
    '您的商品 "{productTitle}" 收到议价请求：买家出价 ￥{proposedPrice}（原价 ￥{originalPrice}），请及时回复。', 
    '卖家收到买家议价请求', 
    3, 1, 1
);

-- 15. 议价被接受
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '议价被接受', 
    '交易相关', 
    '好消息！您对商品 "{productTitle}" 的议价请求已被接受，成交价格：￥{finalPrice}，请尽快完成支付。', 
    '买家的议价被卖家接受', 
    3, 1, 1
);

-- 16. 议价被拒绝  
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '议价被拒绝', 
    '交易相关', 
    '很遗憾，您对商品 "{productTitle}" 的议价请求未被接受。{reason}您可以按原价购买或重新议价。', 
    '买家的议价被卖家拒绝', 
    2, 1, 1
);

-- 17. 收到反报价
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '收到反报价', 
    '交易相关', 
    '卖家对您的议价给出反报价：￥{counterPrice}（您的出价：￥{yourPrice}），是否接受？', 
    '卖家给出反报价时通知买家', 
    3, 1, 1
);

-- ================================================================
-- 评价相关通知模板
-- ================================================================

-- 18. 收到新评价
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '收到新评价', 
    '评价相关', 
    '您收到了新的评价！评分：{rating}分，来自订单 #{orderId}。{reviewContent}', 
    '收到买家评价时通知卖家', 
    2, 1, 1
);

-- 19. 评价提醒
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '评价提醒', 
    '评价相关', 
    '您的订单 #{orderId} 已完成，快来评价一下这次购物体验吧！您的评价对其他用户很重要。', 
    '提醒买家对完成的订单进行评价', 
    2, 1, 1
);

-- 20. 卖家回复评价
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active, created_by
) VALUES (
    '卖家回复评价', 
    '评价相关', 
    '卖家已回复您的评价（订单 #{orderId}）：{replyContent}', 
    '卖家回复买家评价时通知', 
    2, 1, 1
);

-- ================================================================
-- 系统通知模板
-- ================================================================

-- 21. 账户安全提醒
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active
) VALUES (
    '账户安全提醒', 
    '系统通知', 
    '检测到您的账户在 {loginTime} 从 {ipAddress} ({location}) 登录，如非本人操作请立即修改密码并联系客服。', 
    '异常登录安全提醒', 
    5, 1
);

-- 22. 密码修改成功
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active
) VALUES (
    '密码修改成功', 
    '系统通知', 
    '您的账户密码已成功修改（{changeTime}）。如非本人操作，请立即联系客服。', 
    '密码修改后的安全确认', 
    4, 1
);

-- 23. 系统维护通知
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active
) VALUES (
    '系统维护通知', 
    '系统通知', 
    '系统将于 {maintenanceTime} 进行维护，预计耗时 {duration}。维护期间将暂停服务，请合理安排交易时间。', 
    '系统维护前的提前通知', 
    4, 1
);

-- 24. 欢迎新用户
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active
) VALUES (
    '欢迎新用户', 
    '系统通知', 
    '欢迎 {userName} 加入校园交易平台！🎉 您已获得新用户专享权益，快去发布您的第一个商品或开始购物吧！', 
    '新用户注册成功欢迎消息', 
    1, 1
);

-- 25. 信用分变更
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active
) VALUES (
    '信用分变更', 
    '系统通知', 
    '您的信用分发生变更：{changeType}，变更值：{changeValue}，当前信用分：{newScore}分。{reason}', 
    '信用分变化时通知用户', 
    3, 1
);

-- 26. 账户余额变动
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active
) VALUES (
    '账户余额变动', 
    '系统通知', 
    '您的账户余额发生变动：{changeType} ￥{amount}，当前余额：￥{currentBalance}。交易时间：{transactionTime}', 
    '账户余额变化时通知', 
    3, 1
);

-- 27. 充值成功通知
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active
) VALUES (
    '充值成功通知', 
    '系统通知', 
    '充值成功！金额：￥{amount}，当前余额：￥{currentBalance}，交易单号：{transactionId}', 
    '充值成功后的确认通知', 
    3, 1
);

-- ================================================================
-- 管理员相关通知模板
-- ================================================================

-- 28. 举报处理结果
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active
) VALUES (
    '举报处理结果', 
    '系统通知', 
    '您的举报（举报编号：{reportId}）已处理完成。处理结果：{result}。感谢您对平台的监督。', 
    '举报处理完成后通知举报人', 
    3, 1
);

-- 29. 违规处罚通知
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active
) VALUES (
    '违规处罚通知', 
    '系统通知', 
    '经核实，您的行为违反了平台规定。处罚措施：{punishment}，处罚期限：{duration}。如有异议请联系客服。', 
    '用户违规被处罚时的通知', 
    5, 1
);

-- 30. 申诉处理结果
INSERT INTO notification_templates (
    template_name, template_type, template_content, description, priority, is_active
) VALUES (
    '申诉处理结果', 
    '系统通知', 
    '您的申诉（申诉编号：{appealId}）已处理完成。处理结果：{result}。详情：{details}', 
    '申诉处理完成后的结果通知', 
    4, 1
);

-- ================================================================
-- 提交所有更改
-- ================================================================
COMMIT;

-- ================================================================
-- 验证插入结果
-- ================================================================
SELECT 'Notification templates inserted successfully:' AS message FROM dual;
SELECT template_type, COUNT(*) AS template_count 
FROM notification_templates 
GROUP BY template_type 
ORDER BY template_type;

SELECT 'Total templates count:' AS message, COUNT(*) AS total_count 
FROM notification_templates;

-- 显示所有模板概览
SELECT template_id, template_name, template_type, priority, 
       CASE WHEN is_active = 1 THEN '启用' ELSE '禁用' END AS status,
       SUBSTR(template_content, 1, 50) || '...' AS content_preview
FROM notification_templates 
ORDER BY template_type, template_id;

SELECT 'Notification templates initialization complete!' AS final_message FROM dual; 