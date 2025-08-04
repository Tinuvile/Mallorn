using CampusTrade.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Data
{
    public class CampusTradeDbContext : DbContext
    {
        public CampusTradeDbContext(DbContextOptions<CampusTradeDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<CreditHistory> CreditHistories { get; set; }
        public DbSet<LoginLogs> LoginLogs { get; set; }
        public DbSet<EmailVerification> EmailVerifications { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<AbstractOrder> AbstractOrders { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<VirtualAccount> VirtualAccounts { get; set; }
        public DbSet<RechargeRecord> RechargeRecords { get; set; }
        public DbSet<Negotiation> Negotiations { get; set; }
        public DbSet<ExchangeRequest> ExchangeRequests { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Reports> Reports { get; set; }
        public DbSet<ReportEvidence> ReportEvidences { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置学生表
            modelBuilder.Entity<Student>(entity =>
            {
                entity.ToTable("STUDENTS");
                entity.HasKey(e => e.StudentId);

                // 主键配置 - 对应Oracle中的student_id字段
                entity.Property(e => e.StudentId)
                    .HasColumnName("STUDENT_ID")
                    .HasColumnType("VARCHAR2(20)")
                    .IsRequired()
                    .HasMaxLength(20);

                // 姓名字段配置 - 必填
                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .HasColumnType("VARCHAR2(50)")
                    .IsRequired()
                    .HasMaxLength(50);

                // 院系字段配置 - 可为空
                entity.Property(e => e.Department)
                    .HasColumnName("DEPARTMENT")
                    .HasColumnType("VARCHAR2(50)")
                    .HasMaxLength(50);
            });

            // 配置用户表
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("USERS");
                entity.HasKey(e => e.UserId);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.StudentId).IsUnique();

                entity.Property(e => e.UserId)
                    .HasColumnName("USER_ID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Email)
                    .HasColumnName("EMAIL")
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.CreditScore)
                    .HasColumnName("CREDIT_SCORE")
                    .HasPrecision(3, 1)
                    .HasDefaultValue(60.0m);

                entity.Property(e => e.PasswordHash)
                    .HasColumnName("PASSWORD_HASH")
                    .IsRequired()
                    .HasMaxLength(128);

                entity.Property(e => e.StudentId)
                    .HasColumnName("STUDENT_ID")
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Username)
                    .HasColumnName("USERNAME")
                    .HasMaxLength(50);

                entity.Property(e => e.FullName)
                    .HasColumnName("FULL_NAME")
                    .HasMaxLength(100);

                entity.Property(e => e.Phone)
                    .HasColumnName("PHONE")
                    .HasMaxLength(20);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("CREATED_AT")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("UPDATED_AT")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.IsActive)
                    .HasColumnName("IS_ACTIVE")
                    .HasDefaultValue(1);

                // JWT Token相关字段配置
                entity.Property(e => e.LastLoginAt)
                    .HasColumnName("LAST_LOGIN_AT");

                entity.Property(e => e.LastLoginIp)
                    .HasColumnName("LAST_LOGIN_IP")
                    .HasMaxLength(45);

                entity.Property(e => e.LoginCount)
                    .HasColumnName("LOGIN_COUNT")
                    .HasDefaultValue(0);

                entity.Property(e => e.IsLocked)
                    .HasColumnName("IS_LOCKED")
                    .HasDefaultValue(0);

                entity.Property(e => e.LockoutEnd)
                    .HasColumnName("LOCKOUT_END");

                entity.Property(e => e.FailedLoginAttempts)
                    .HasColumnName("FAILED_LOGIN_ATTEMPTS")
                    .HasDefaultValue(0);

                entity.Property(e => e.TwoFactorEnabled)
                    .HasColumnName("TWO_FACTOR_ENABLED")
                    .HasDefaultValue(0);

                entity.Property(e => e.PasswordChangedAt)
                    .HasColumnName("PASSWORD_CHANGED_AT");

                entity.Property(e => e.SecurityStamp)
                    .HasColumnName("SECURITY_STAMP")
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.EmailVerified)
                    .HasColumnName("EMAIL_VERIFIED")
                    .HasDefaultValue(0);

                entity.Property(e => e.EmailVerificationToken)
                    .HasColumnName("EMAIL_VERIFICATION_TOKEN")
                    .HasMaxLength(256);

                // 索引配置
                entity.HasIndex(e => e.LastLoginAt)
                    .HasDatabaseName("IX_USERS_LAST_LOGIN_AT");

                entity.HasIndex(e => e.IsLocked)
                    .HasDatabaseName("IX_USERS_IS_LOCKED");

                entity.HasIndex(e => e.SecurityStamp)
                    .HasDatabaseName("IX_USERS_SECURITY_STAMP");

                // 配置外键关系 - 对应Oracle中的FK_USERS_STUDENT约束
                entity.HasOne(e => e.Student)
                    .WithOne(s => s.User)
                    .HasForeignKey<User>(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_USERS_STUDENT");

                // 配置与RefreshToken的一对多关系
                entity.HasMany(e => e.RefreshTokens)
                    .WithOne(r => r.User)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // 配置与CreditHistory的一对多关系
                entity.HasMany(e => e.CreditHistories)
                    .WithOne(c => c.User)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_CREDIT_USER");

                // 配置与LoginLogs的一对多关系
                entity.HasMany(e => e.LoginLogs)
                    .WithOne(l => l.User)
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_LOGIN_USER");

                // 配置与EmailVerification的一对多关系
                entity.HasMany(e => e.EmailVerifications)
                    .WithOne(e => e.User)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_EMAIL_VERIFICATION_USER");

                // 配置与Product的一对多关系
                entity.HasMany(e => e.Products)
                    .WithOne(p => p.User)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_PRODUCT_USER");

                // 配置与Order的一对多关系 - 买家
                entity.HasMany(e => e.BuyerOrders)
                    .WithOne(o => o.Buyer)
                    .HasForeignKey(o => o.BuyerId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_ORDER_BUYER");

                // 配置与Order的一对多关系 - 卖家
                entity.HasMany(e => e.SellerOrders)
                    .WithOne(o => o.Seller)
                    .HasForeignKey(o => o.SellerId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_ORDER_SELLER");
            });

            // 配置RefreshToken表
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("REFRESH_TOKENS");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Token)
                    .HasColumnName("TOKEN")
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.UserId)
                    .HasColumnName("USER_ID")
                    .IsRequired();

                entity.Property(e => e.ExpiryDate)
                    .HasColumnName("EXPIRY_DATE")
                    .IsRequired();

                entity.Property(e => e.IsRevoked)
                    .HasColumnName("IS_REVOKED")
                    .HasDefaultValue(0);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("CREATED_AT")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.RevokedAt)
                    .HasColumnName("REVOKED_AT");

                entity.Property(e => e.IpAddress)
                    .HasColumnName("IP_ADDRESS")
                    .HasMaxLength(45);

                entity.Property(e => e.UserAgent)
                    .HasColumnName("USER_AGENT")
                    .HasMaxLength(500);

                entity.Property(e => e.DeviceId)
                    .HasColumnName("DEVICE_ID")
                    .HasMaxLength(100);

                entity.Property(e => e.ReplacedByToken)
                    .HasColumnName("REPLACED_BY_TOKEN")
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY");

                entity.Property(e => e.LastUsedAt)
                    .HasColumnName("LAST_USED_AT");

                entity.Property(e => e.RevokedBy)
                    .HasColumnName("REVOKED_BY");

                entity.Property(e => e.RevokeReason)
                    .HasColumnName("REVOKE_REASON")
                    .HasMaxLength(200);

                // 索引配置
                entity.HasIndex(e => e.Token)
                    .IsUnique()
                    .HasDatabaseName("IX_REFRESH_TOKENS_TOKEN");

                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_REFRESH_TOKENS_USER_ID");

                entity.HasIndex(e => e.ExpiryDate)
                    .HasDatabaseName("IX_REFRESH_TOKENS_EXPIRY_DATE");

                entity.HasIndex(e => e.IsRevoked)
                    .HasDatabaseName("IX_REFRESH_TOKENS_IS_REVOKED");

                entity.HasIndex(e => e.DeviceId)
                    .HasDatabaseName("IX_REFRESH_TOKENS_DEVICE_ID");

                // 复合索引（用于查询优化）
                entity.HasIndex(e => new { e.UserId, e.IsRevoked, e.ExpiryDate })
                    .HasDatabaseName("IX_REFRESH_TOKENS_USER_STATUS_EXPIRY");

                entity.HasIndex(e => new { e.DeviceId, e.UserId, e.IsRevoked })
                    .HasDatabaseName("IX_REFRESH_TOKENS_DEVICE_USER_STATUS");

                // 外键关系已在User实体中配置
            });

            // 配置信用变更记录表
            modelBuilder.Entity<CreditHistory>(entity =>
            {
                entity.ToTable("CREDIT_HISTORY", t =>
                {
                    t.HasCheckConstraint("CK_CREDIT_HISTORY_CHANGE_TYPE",
                        "CHANGE_TYPE IN ('交易完成', '举报处罚', '好评奖励')");
                    t.HasCheckConstraint("CK_CREDIT_HISTORY_SCORE_RANGE",
                        "NEW_SCORE >= 0.0 AND NEW_SCORE <= 100.0");
                });
                entity.HasKey(e => e.LogId);

                // 主键配置 - 自增ID
                entity.Property(e => e.LogId)
                    .HasColumnName("LOG_ID")
                    .HasColumnType("NUMBER")
                    .ValueGeneratedOnAdd();

                // 用户ID外键配置
                entity.Property(e => e.UserId)
                    .HasColumnName("USER_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 变更类型配置 - 带检查约束
                entity.Property(e => e.ChangeType)
                    .HasColumnName("CHANGE_TYPE")
                    .HasColumnType("VARCHAR2(20)")
                    .IsRequired()
                    .HasMaxLength(20);

                // 新信用分数配置 - 精确的数字类型
                entity.Property(e => e.NewScore)
                    .HasColumnName("NEW_SCORE")
                    .HasColumnType("NUMBER(3,1)")
                    .HasPrecision(3, 1)
                    .IsRequired();

                // 创建时间配置 - 默认当前时间
                entity.Property(e => e.CreatedAt)
                    .HasColumnName("CREATED_AT")
                    .HasColumnType("TIMESTAMP")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 索引配置
                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_CREDIT_HISTORY_USER_ID");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_CREDIT_HISTORY_CREATED_AT");

                entity.HasIndex(e => e.ChangeType)
                    .HasDatabaseName("IX_CREDIT_HISTORY_CHANGE_TYPE");

                // 复合索引（用于查询优化）
                entity.HasIndex(e => new { e.UserId, e.CreatedAt })
                    .HasDatabaseName("IX_CREDIT_HISTORY_USER_TIME");

                // 外键关系配置已在User实体中设置
            });

            // 配置登录日志表
            modelBuilder.Entity<LoginLogs>(entity =>
            {
                entity.ToTable("LOGIN_LOGS", t =>
                {
                    t.HasCheckConstraint("CK_LOGIN_LOGS_RISK_LEVEL",
                        "RISK_LEVEL IN (0,1,2)");
                    t.HasCheckConstraint("CK_LOGIN_LOGS_DEVICE_TYPE",
                        "DEVICE_TYPE IN ('Mobile','PC','Tablet')");
                });
                entity.HasKey(e => e.LogId);

                // 主键配置 - 自增ID
                entity.Property(e => e.LogId)
                    .HasColumnName("LOG_ID")
                    .HasColumnType("NUMBER")
                    .ValueGeneratedOnAdd();

                // 用户ID外键配置
                entity.Property(e => e.UserId)
                    .HasColumnName("USER_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // IP地址配置 - 可为空
                entity.Property(e => e.IpAddress)
                    .HasColumnName("IP_ADDRESS")
                    .HasColumnType("VARCHAR2(45)")
                    .HasMaxLength(45);

                // 登录时间配置 - 默认当前时间
                entity.Property(e => e.LogTime)
                    .HasColumnName("LOG_TIME")
                    .HasColumnType("TIMESTAMP")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 设备类型配置 - 必填，检查约束限制值
                entity.Property(e => e.DeviceType)
                    .HasColumnName("DEVICE_TYPE")
                    .HasColumnType("VARCHAR2(20)")
                    .IsRequired()
                    .HasMaxLength(20);

                // 风险等级配置 - 可为空，检查约束限制范围
                entity.Property(e => e.RiskLevel)
                    .HasColumnName("RISK_LEVEL")
                    .HasColumnType("NUMBER");

                // 索引配置
                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_LOGIN_LOGS_USER_ID");

                entity.HasIndex(e => e.LogTime)
                    .HasDatabaseName("IX_LOGIN_LOGS_LOG_TIME");

                entity.HasIndex(e => e.DeviceType)
                    .HasDatabaseName("IX_LOGIN_LOGS_DEVICE_TYPE");

                entity.HasIndex(e => e.RiskLevel)
                    .HasDatabaseName("IX_LOGIN_LOGS_RISK_LEVEL");

                // 外键关系配置已在User实体中设置
            });

            // 配置邮箱验证表
            modelBuilder.Entity<EmailVerification>(entity =>
            {
                entity.ToTable("EMAIL_VERIFICATION", t =>
                {
                    t.HasCheckConstraint("CK_EMAIL_VERIFICATION_IS_USED",
                        "IS_USED IN (0,1)");
                });
                entity.HasKey(e => e.VerificationId);

                // 主键配置 - 自增ID
                entity.Property(e => e.VerificationId)
                    .HasColumnName("VERIFICATION_ID")
                    .HasColumnType("NUMBER")
                    .ValueGeneratedOnAdd();

                // 用户ID外键配置
                entity.Property(e => e.UserId)
                    .HasColumnName("USER_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 邮箱地址配置
                entity.Property(e => e.Email)
                    .HasColumnName("EMAIL")
                    .HasColumnType("VARCHAR2(100)")
                    .IsRequired()
                    .HasMaxLength(100);

                // 6位数字验证码配置 - 可为空
                entity.Property(e => e.VerificationCode)
                    .HasColumnName("VERIFICATION_CODE")
                    .HasColumnType("VARCHAR2(6)")
                    .HasMaxLength(6);

                // 64位验证令牌配置 - 可为空
                entity.Property(e => e.Token)
                    .HasColumnName("TOKEN")
                    .HasColumnType("VARCHAR2(64)")
                    .HasMaxLength(64);

                // 过期时间配置 - 可为空
                entity.Property(e => e.ExpireTime)
                    .HasColumnName("EXPIRE_TIME")
                    .HasColumnType("TIMESTAMP");

                // 使用状态配置 - 默认值0
                entity.Property(e => e.IsUsed)
                    .HasColumnName("IS_USED")
                    .HasDefaultValue(0);

                // 创建时间配置 - 默认当前时间
                entity.Property(e => e.CreatedAt)
                    .HasColumnName("CREATED_AT")
                    .HasColumnType("TIMESTAMP")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 索引配置
                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_EMAIL_VERIFICATION_USER_ID");

                entity.HasIndex(e => e.Email)
                    .HasDatabaseName("IX_EMAIL_VERIFICATION_EMAIL");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_EMAIL_VERIFICATION_CREATED_AT");

                entity.HasIndex(e => e.IsUsed)
                    .HasDatabaseName("IX_EMAIL_VERIFICATION_IS_USED");

                // 复合索引（用于查询优化）
                entity.HasIndex(e => new { e.UserId, e.IsUsed })
                    .HasDatabaseName("IX_EMAIL_VERIFICATION_USER_USED");

                entity.HasIndex(e => new { e.Email, e.IsUsed })
                    .HasDatabaseName("IX_EMAIL_VERIFICATION_EMAIL_USED");

                // 外键关系配置已在User实体中设置
            });

            // 配置分类表
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("CATEGORIES");
                entity.HasKey(e => e.CategoryId);

                // 主键配置 - 自增ID
                entity.Property(e => e.CategoryId)
                    .HasColumnName("CATEGORY_ID")
                    .HasColumnType("NUMBER")
                    .ValueGeneratedOnAdd();

                // 父分类ID配置 - 可为空
                entity.Property(e => e.ParentId)
                    .HasColumnName("PARENT_ID")
                    .HasColumnType("NUMBER");

                // 分类名称配置
                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .HasColumnType("VARCHAR2(50)")
                    .IsRequired()
                    .HasMaxLength(50);

                // 配置自引用关系 - 父子分类
                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_CATEGORY_PARENT");

                // 配置与Product的一对多关系
                entity.HasMany(e => e.Products)
                    .WithOne(p => p.Category)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_PRODUCT_CATEGORY");

                // 索引配置
                entity.HasIndex(e => e.ParentId)
                    .HasDatabaseName("IX_CATEGORIES_PARENT_ID");

                entity.HasIndex(e => e.Name)
                    .HasDatabaseName("IX_CATEGORIES_NAME");

                // 复合索引（同级分类名称唯一性检查优化）
                entity.HasIndex(e => new { e.ParentId, e.Name })
                    .HasDatabaseName("IX_CATEGORIES_PARENT_NAME");
            });

            // 配置商品表
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("PRODUCTS", t =>
                {
                    t.HasCheckConstraint("CK_PRODUCTS_STATUS",
                        "STATUS IN ('在售','已下架','交易中')");
                });
                entity.HasKey(e => e.ProductId);

                // 主键配置 - 由序列和触发器生成
                entity.Property(e => e.ProductId)
                    .HasColumnName("PRODUCT_ID")
                    .HasColumnType("NUMBER")
                    .ValueGeneratedOnAdd();

                // 用户ID外键配置
                entity.Property(e => e.UserId)
                    .HasColumnName("USER_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 分类ID外键配置
                entity.Property(e => e.CategoryId)
                    .HasColumnName("CATEGORY_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 商品标题配置
                entity.Property(e => e.Title)
                    .HasColumnName("TITLE")
                    .HasColumnType("VARCHAR2(100)")
                    .IsRequired()
                    .HasMaxLength(100);

                // 商品描述配置 - CLOB类型
                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasColumnType("CLOB");

                // 基础价格配置 - 精确的数字类型
                entity.Property(e => e.BasePrice)
                    .HasColumnName("BASE_PRICE")
                    .HasColumnType("NUMBER(10,2)")
                    .HasPrecision(10, 2)
                    .IsRequired();

                // 发布时间配置 - 默认当前时间
                entity.Property(e => e.PublishTime)
                    .HasColumnName("PUBLISH_TIME")
                    .HasColumnType("TIMESTAMP")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 浏览次数配置 - 默认值0
                entity.Property(e => e.ViewCount)
                    .HasColumnName("VIEW_COUNT")
                    .HasColumnType("NUMBER")
                    .HasDefaultValue(0);

                // 自动下架时间配置 - 可为空
                entity.Property(e => e.AutoRemoveTime)
                    .HasColumnName("AUTO_REMOVE_TIME")
                    .HasColumnType("TIMESTAMP");

                // 商品状态配置 - 默认值"在售"
                entity.Property(e => e.Status)
                    .HasColumnName("STATUS")
                    .HasColumnType("VARCHAR2(20)")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("在售");

                // 索引配置
                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_PRODUCTS_USER_ID");

                entity.HasIndex(e => e.CategoryId)
                    .HasDatabaseName("IX_PRODUCTS_CATEGORY_ID");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_PRODUCTS_STATUS");

                entity.HasIndex(e => e.PublishTime)
                    .HasDatabaseName("IX_PRODUCTS_PUBLISH_TIME");

                entity.HasIndex(e => e.BasePrice)
                    .HasDatabaseName("IX_PRODUCTS_BASE_PRICE");

                // 复合索引（用于查询优化）
                entity.HasIndex(e => new { e.Status, e.PublishTime })
                    .HasDatabaseName("IX_PRODUCTS_STATUS_TIME");

                entity.HasIndex(e => new { e.CategoryId, e.Status })
                    .HasDatabaseName("IX_PRODUCTS_CATEGORY_STATUS");

                // 配置与ProductImage的一对多关系
                entity.HasMany(e => e.ProductImages)
                    .WithOne(i => i.Product)
                    .HasForeignKey(i => i.ProductId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_IMAGE_PRODUCT");

                // User关系已在User实体中配置
            });

            // 配置商品图片表
            modelBuilder.Entity<ProductImage>(entity =>
            {
                entity.ToTable("PRODUCT_IMAGES");
                entity.HasKey(e => e.ImageId);

                // 主键配置 - 自增ID
                entity.Property(e => e.ImageId)
                    .HasColumnName("IMAGE_ID")
                    .HasColumnType("NUMBER")
                    .ValueGeneratedOnAdd();

                // 商品ID外键配置
                entity.Property(e => e.ProductId)
                    .HasColumnName("PRODUCT_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 图片URL配置
                entity.Property(e => e.ImageUrl)
                    .HasColumnName("IMAGE_URL")
                    .HasColumnType("VARCHAR2(200)")
                    .IsRequired()
                    .HasMaxLength(200);

                // 索引配置
                entity.HasIndex(e => e.ProductId)
                    .HasDatabaseName("IX_PRODUCT_IMAGES_PRODUCT_ID");

                // Product关系已在Product实体中配置
            });

            // 配置抽象订单表
            modelBuilder.Entity<AbstractOrder>(entity =>
            {
                entity.ToTable("ABSTRACT_ORDERS", t =>
                {
                    t.HasCheckConstraint("CK_ABSTRACT_ORDERS_ORDER_TYPE",
                        "ORDER_TYPE IN ('normal','exchange')");
                });
                entity.HasKey(e => e.AbstractOrderId);

                // 主键配置 - 由ORDER_SEQ序列生成
                entity.Property(e => e.AbstractOrderId)
                    .HasColumnName("ABSTRACT_ORDER_ID")
                    .HasColumnType("NUMBER")
                    .ValueGeneratedOnAdd();

                // 订单类型配置
                entity.Property(e => e.OrderType)
                    .HasColumnName("ORDER_TYPE")
                    .HasColumnType("VARCHAR2(20)")
                    .IsRequired()
                    .HasMaxLength(20);

                // 配置与Order的一对一关系
                entity.HasOne(e => e.Order)
                    .WithOne(o => o.AbstractOrder)
                    .HasForeignKey<Order>(o => o.OrderId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ORDER_ABSTRACT");

                // 索引配置
                entity.HasIndex(e => e.OrderType)
                    .HasDatabaseName("IX_ABSTRACT_ORDERS_ORDER_TYPE");
            });

            // 配置订单表
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("ORDERS", t =>
                {
                    t.HasCheckConstraint("CK_ORDERS_STATUS",
                        "STATUS IN ('待付款','已付款','已发货','已送达','已完成','已取消')");
                });
                entity.HasKey(e => e.OrderId);

                // 主键配置 - 外键引用AbstractOrder
                entity.Property(e => e.OrderId)
                    .HasColumnName("ORDER_ID")
                    .HasColumnType("NUMBER");

                // 买家ID配置
                entity.Property(e => e.BuyerId)
                    .HasColumnName("BUYER_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 卖家ID配置
                entity.Property(e => e.SellerId)
                    .HasColumnName("SELLER_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 商品ID配置
                entity.Property(e => e.ProductId)
                    .HasColumnName("PRODUCT_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 订单总金额配置 - 可为空
                entity.Property(e => e.TotalAmount)
                    .HasColumnName("TOTAL_AMOUNT")
                    .HasColumnType("NUMBER(10,2)")
                    .HasPrecision(10, 2);

                // 订单状态配置 - 默认值"待付款"
                entity.Property(e => e.Status)
                    .HasColumnName("STATUS")
                    .HasColumnType("VARCHAR2(20)")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("待付款");

                // 创建时间配置 - 默认当前时间
                entity.Property(e => e.CreateTime)
                    .HasColumnName("CREATE_TIME")
                    .HasColumnType("TIMESTAMP")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 过期时间配置 - 触发器设置
                entity.Property(e => e.ExpireTime)
                    .HasColumnName("EXPIRE_TIME")
                    .HasColumnType("TIMESTAMP");

                // 最终价格配置 - 可为空
                entity.Property(e => e.FinalPrice)
                    .HasColumnName("FINAL_PRICE")
                    .HasColumnType("NUMBER(10,2)")
                    .HasPrecision(10, 2);

                // 配置与Product的多对一关系
                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_ORDER_PRODUCT");

                // 索引配置
                entity.HasIndex(e => e.BuyerId)
                    .HasDatabaseName("IX_ORDERS_BUYER_ID");

                entity.HasIndex(e => e.SellerId)
                    .HasDatabaseName("IX_ORDERS_SELLER_ID");

                entity.HasIndex(e => e.ProductId)
                    .HasDatabaseName("IX_ORDERS_PRODUCT_ID");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_ORDERS_STATUS");

                entity.HasIndex(e => e.CreateTime)
                    .HasDatabaseName("IX_ORDERS_CREATE_TIME");

                entity.HasIndex(e => e.ExpireTime)
                    .HasDatabaseName("IX_ORDERS_EXPIRE_TIME");

                // 复合索引（用于查询优化）
                entity.HasIndex(e => new { e.BuyerId, e.Status })
                    .HasDatabaseName("IX_ORDERS_BUYER_STATUS");

                entity.HasIndex(e => new { e.SellerId, e.Status })
                    .HasDatabaseName("IX_ORDERS_SELLER_STATUS");

                entity.HasIndex(e => new { e.Status, e.CreateTime })
                    .HasDatabaseName("IX_ORDERS_STATUS_TIME");

                // User关系已在User实体中配置
                // AbstractOrder关系已在AbstractOrder实体中配置
            });

            // 配置虚拟账户表
            modelBuilder.Entity<VirtualAccount>(entity =>
            {
                entity.ToTable("VIRTUAL_ACCOUNTS");
                entity.HasKey(e => e.AccountId);

                // 主键配置 - 自增ID
                entity.Property(e => e.AccountId)
                    .HasColumnName("ACCOUNT_ID")
                    .HasColumnType("NUMBER")
                    .ValueGeneratedOnAdd();

                // 用户ID外键配置 - 唯一约束
                entity.Property(e => e.UserId)
                    .HasColumnName("USER_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 余额配置 - 精确的数字类型
                entity.Property(e => e.Balance)
                    .HasColumnName("BALANCE")
                    .HasColumnType("NUMBER(10,2)")
                    .HasPrecision(10, 2)
                    .IsRequired()
                    .HasDefaultValue(0.00m);

                // 创建时间配置 - 默认当前时间
                entity.Property(e => e.CreatedAt)
                    .HasColumnName("CREATED_AT")
                    .HasColumnType("TIMESTAMP")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 唯一约束
                entity.HasIndex(e => e.UserId)
                    .IsUnique()
                    .HasDatabaseName("IX_VIRTUAL_ACCOUNTS_USER_ID");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_VIRTUAL_ACCOUNTS_CREATED_AT");

                // 配置与User的一对一关系
                entity.HasOne(e => e.User)
                    .WithOne(u => u.VirtualAccount)
                    .HasForeignKey<VirtualAccount>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ACCOUNT_USER");

                // 配置与RechargeRecord的一对多关系
                entity.HasMany(e => e.RechargeRecords)
                    .WithOne(r => r.VirtualAccount)
                    .HasForeignKey(r => r.UserId)
                    .HasPrincipalKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 配置充值记录表
            modelBuilder.Entity<RechargeRecord>(entity =>
            {
                entity.ToTable("RECHARGE_RECORDS", t =>
                {
                    t.HasCheckConstraint("CK_RECHARGE_RECORDS_STATUS",
                        "STATUS IN ('处理中','成功','失败')");
                    t.HasCheckConstraint("CK_RECHARGE_RECORDS_AMOUNT",
                        "AMOUNT > 0");
                });
                entity.HasKey(e => e.RechargeId);

                // 主键配置 - 自增ID
                entity.Property(e => e.RechargeId)
                    .HasColumnName("RECHARGE_ID")
                    .HasColumnType("NUMBER")
                    .ValueGeneratedOnAdd();

                // 用户ID外键配置
                entity.Property(e => e.UserId)
                    .HasColumnName("USER_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 充值金额配置 - 精确的数字类型
                entity.Property(e => e.Amount)
                    .HasColumnName("AMOUNT")
                    .HasColumnType("NUMBER(10,2)")
                    .HasPrecision(10, 2)
                    .IsRequired();

                // 状态配置 - 默认值"处理中"
                entity.Property(e => e.Status)
                    .HasColumnName("STATUS")
                    .HasColumnType("VARCHAR2(20)")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("处理中");

                // 创建时间配置 - 默认当前时间
                entity.Property(e => e.CreateTime)
                    .HasColumnName("CREATE_TIME")
                    .HasColumnType("TIMESTAMP")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 完成时间配置 - 可为空
                entity.Property(e => e.CompleteTime)
                    .HasColumnName("COMPLETE_TIME")
                    .HasColumnType("TIMESTAMP");

                // 索引配置
                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_RECHARGE_RECORDS_USER_ID");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_RECHARGE_RECORDS_STATUS");

                entity.HasIndex(e => e.CreateTime)
                    .HasDatabaseName("IX_RECHARGE_RECORDS_CREATE_TIME");

                entity.HasIndex(e => e.Amount)
                    .HasDatabaseName("IX_RECHARGE_RECORDS_AMOUNT");

                // 复合索引（用于查询优化）
                entity.HasIndex(e => new { e.UserId, e.Status })
                    .HasDatabaseName("IX_RECHARGE_RECORDS_USER_STATUS");

                entity.HasIndex(e => new { e.Status, e.CreateTime })
                    .HasDatabaseName("IX_RECHARGE_RECORDS_STATUS_TIME");

                // 配置与User的多对一关系
                entity.HasOne(e => e.User)
                    .WithMany(u => u.RechargeRecords)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_RECHARGE_USER");

                // VirtualAccount关系已在VirtualAccount实体中配置
            });

            // 配置议价表
            modelBuilder.Entity<Negotiation>(entity =>
            {
                entity.ToTable("NEGOTIATIONS", t =>
                {
                    t.HasCheckConstraint("CK_NEGOTIATIONS_STATUS",
                        "STATUS IN ('等待回应','接受','拒绝','反报价')");
                    t.HasCheckConstraint("CK_NEGOTIATIONS_PROPOSED_PRICE",
                        "PROPOSED_PRICE > 0");
                });
                entity.HasKey(e => e.NegotiationId);

                // 主键配置 - 自增ID
                entity.Property(e => e.NegotiationId)
                    .HasColumnName("NEGOTIATION_ID")
                    .HasColumnType("NUMBER")
                    .ValueGeneratedOnAdd();

                // 订单ID外键配置
                entity.Property(e => e.OrderId)
                    .HasColumnName("ORDER_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 提议价格配置 - 精确的数字类型
                entity.Property(e => e.ProposedPrice)
                    .HasColumnName("PROPOSED_PRICE")
                    .HasColumnType("NUMBER(10,2)")
                    .HasPrecision(10, 2)
                    .IsRequired();

                // 状态配置 - 默认值"等待回应"
                entity.Property(e => e.Status)
                    .HasColumnName("STATUS")
                    .HasColumnType("VARCHAR2(20)")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("等待回应");

                // 创建时间配置 - 默认当前时间
                entity.Property(e => e.CreatedAt)
                    .HasColumnName("CREATED_AT")
                    .HasColumnType("TIMESTAMP")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 索引配置
                entity.HasIndex(e => e.OrderId)
                    .HasDatabaseName("IX_NEGOTIATIONS_ORDER_ID");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_NEGOTIATIONS_STATUS");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_NEGOTIATIONS_CREATED_AT");

                entity.HasIndex(e => e.ProposedPrice)
                    .HasDatabaseName("IX_NEGOTIATIONS_PROPOSED_PRICE");

                // 复合索引（用于查询优化）
                entity.HasIndex(e => new { e.OrderId, e.Status })
                    .HasDatabaseName("IX_NEGOTIATIONS_ORDER_STATUS");

                entity.HasIndex(e => new { e.Status, e.CreatedAt })
                    .HasDatabaseName("IX_NEGOTIATIONS_STATUS_TIME");

                // 配置与Order的多对一关系
                entity.HasOne(e => e.Order)
                    .WithMany(o => o.Negotiations)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_NEGOTIATION_ORDER");
            });

            // 配置换物请求表
            modelBuilder.Entity<ExchangeRequest>(entity =>
            {
                entity.ToTable("EXCHANGE_REQUESTS", t =>
                {
                    t.HasCheckConstraint("CK_EXCHANGE_REQUESTS_STATUS",
                        "STATUS IN ('等待回应','接受','拒绝','反报价')");
                });
                entity.HasKey(e => e.ExchangeId);

                // 主键配置 - 外键引用AbstractOrder
                entity.Property(e => e.ExchangeId)
                    .HasColumnName("EXCHANGE_ID")
                    .HasColumnType("NUMBER");

                // 提供商品ID配置
                entity.Property(e => e.OfferProductId)
                    .HasColumnName("OFFER_PRODUCT_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 请求商品ID配置
                entity.Property(e => e.RequestProductId)
                    .HasColumnName("REQUEST_PRODUCT_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 交换条件配置 - CLOB类型
                entity.Property(e => e.Terms)
                    .HasColumnName("TERMS")
                    .HasColumnType("CLOB");

                // 状态配置 - 默认值"等待回应"
                entity.Property(e => e.Status)
                    .HasColumnName("STATUS")
                    .HasColumnType("VARCHAR2(20)")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("等待回应");

                // 创建时间配置 - 默认当前时间
                entity.Property(e => e.CreatedAt)
                    .HasColumnName("CREATED_AT")
                    .HasColumnType("TIMESTAMP")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 配置与AbstractOrder的一对一关系
                entity.HasOne(e => e.AbstractOrder)
                    .WithOne(a => a.ExchangeRequest)
                    .HasForeignKey<ExchangeRequest>(e => e.ExchangeId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_EXCHANGE_ABSTRACT");

                // 配置与Product的多对一关系 - 提供商品
                entity.HasOne(e => e.OfferProduct)
                    .WithMany(p => p.OfferExchangeRequests)
                    .HasForeignKey(e => e.OfferProductId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_EXCHANGE_OFFER");

                // 配置与Product的多对一关系 - 请求商品
                entity.HasOne(e => e.RequestProduct)
                    .WithMany(p => p.RequestExchangeRequests)
                    .HasForeignKey(e => e.RequestProductId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_EXCHANGE_REQUEST");

                // 索引配置
                entity.HasIndex(e => e.OfferProductId)
                    .HasDatabaseName("IX_EXCHANGE_REQUESTS_OFFER_PRODUCT_ID");

                entity.HasIndex(e => e.RequestProductId)
                    .HasDatabaseName("IX_EXCHANGE_REQUESTS_REQUEST_PRODUCT_ID");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_EXCHANGE_REQUESTS_STATUS");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_EXCHANGE_REQUESTS_CREATED_AT");

                // 复合索引（用于查询优化）
                entity.HasIndex(e => new { e.OfferProductId, e.Status })
                    .HasDatabaseName("IX_EXCHANGE_REQUESTS_OFFER_STATUS");

                entity.HasIndex(e => new { e.RequestProductId, e.Status })
                    .HasDatabaseName("IX_EXCHANGE_REQUESTS_REQUEST_STATUS");

                entity.HasIndex(e => new { e.Status, e.CreatedAt })
                    .HasDatabaseName("IX_EXCHANGE_REQUESTS_STATUS_TIME");
            });

            // 配置管理员表
            modelBuilder.Entity<Admin>(entity =>
            {
                entity.ToTable("ADMINS", t =>
                {
                    t.HasCheckConstraint("CK_ADMINS_ROLE",
                        "ROLE IN ('super','category_admin','report_admin')");
                    t.HasCheckConstraint("CK_ADMINS_CATEGORY_ASSIGNMENT",
                        "(ROLE = 'category_admin' AND ASSIGNED_CATEGORY IS NOT NULL) OR " +
                        "(ROLE != 'category_admin' AND ASSIGNED_CATEGORY IS NULL)");
                });
                entity.HasKey(e => e.AdminId);

                // 主键配置 - 自增ID
                entity.Property(e => e.AdminId)
                    .HasColumnName("ADMIN_ID")
                    .HasColumnType("NUMBER")
                    .ValueGeneratedOnAdd();

                // 用户ID外键配置 - 唯一约束
                entity.Property(e => e.UserId)
                    .HasColumnName("USER_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 角色配置
                entity.Property(e => e.Role)
                    .HasColumnName("ROLE")
                    .HasColumnType("VARCHAR2(20)")
                    .IsRequired()
                    .HasMaxLength(20);

                // 分配分类配置 - 可为空
                entity.Property(e => e.AssignedCategory)
                    .HasColumnName("ASSIGNED_CATEGORY")
                    .HasColumnType("NUMBER");

                // 创建时间配置 - 默认当前时间
                entity.Property(e => e.CreatedAt)
                    .HasColumnName("CREATED_AT")
                    .HasColumnType("TIMESTAMP")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 唯一约束
                entity.HasIndex(e => e.UserId)
                    .IsUnique()
                    .HasDatabaseName("IX_ADMINS_USER_ID");

                // 索引配置
                entity.HasIndex(e => e.Role)
                    .HasDatabaseName("IX_ADMINS_ROLE");

                entity.HasIndex(e => e.AssignedCategory)
                    .HasDatabaseName("IX_ADMINS_ASSIGNED_CATEGORY");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_ADMINS_CREATED_AT");

                // 复合索引（用于查询优化）
                entity.HasIndex(e => new { e.Role, e.AssignedCategory })
                    .HasDatabaseName("IX_ADMINS_ROLE_CATEGORY");

                // 配置与User的一对一关系
                entity.HasOne(e => e.User)
                    .WithOne(u => u.Admin)
                    .HasForeignKey<Admin>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ADMIN_USER");

                // 配置与Category的多对一关系
                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Admins)
                    .HasForeignKey(e => e.AssignedCategory)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_ADMIN_CATEGORY");

                // 配置与AuditLog的一对多关系
                entity.HasMany(e => e.AuditLogs)
                    .WithOne(a => a.Admin)
                    .HasForeignKey(a => a.AdminId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_AUDIT_ADMIN");
            });

            // 配置审计日志表
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AUDIT_LOGS", t =>
                {
                    t.HasCheckConstraint("CK_AUDIT_LOGS_ACTION_TYPE",
                        "ACTION_TYPE IN ('封禁用户','修改权限','处理举报')");
                });
                entity.HasKey(e => e.LogId);

                // 主键配置 - 自增ID
                entity.Property(e => e.LogId)
                    .HasColumnName("LOG_ID")
                    .HasColumnType("NUMBER")
                    .ValueGeneratedOnAdd();

                // 管理员ID外键配置
                entity.Property(e => e.AdminId)
                    .HasColumnName("ADMIN_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 操作类型配置
                entity.Property(e => e.ActionType)
                    .HasColumnName("ACTION_TYPE")
                    .HasColumnType("VARCHAR2(20)")
                    .IsRequired()
                    .HasMaxLength(20);

                // 目标ID配置 - 可为空
                entity.Property(e => e.TargetId)
                    .HasColumnName("TARGET_ID")
                    .HasColumnType("NUMBER");

                // 操作详情配置 - CLOB类型
                entity.Property(e => e.LogDetail)
                    .HasColumnName("LOG_DETAIL")
                    .HasColumnType("CLOB");

                // 操作时间配置 - 默认当前时间
                entity.Property(e => e.LogTime)
                    .HasColumnName("LOG_TIME")
                    .HasColumnType("TIMESTAMP")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 索引配置
                entity.HasIndex(e => e.AdminId)
                    .HasDatabaseName("IX_AUDIT_LOGS_ADMIN_ID");

                entity.HasIndex(e => e.ActionType)
                    .HasDatabaseName("IX_AUDIT_LOGS_ACTION_TYPE");

                entity.HasIndex(e => e.LogTime)
                    .HasDatabaseName("IX_AUDIT_LOGS_LOG_TIME");

                entity.HasIndex(e => e.TargetId)
                    .HasDatabaseName("IX_AUDIT_LOGS_TARGET_ID");

                // 复合索引（用于查询优化）
                entity.HasIndex(e => new { e.AdminId, e.LogTime })
                    .HasDatabaseName("IX_AUDIT_LOGS_ADMIN_TIME");

                entity.HasIndex(e => new { e.ActionType, e.LogTime })
                    .HasDatabaseName("IX_AUDIT_LOGS_ACTION_TIME");

                entity.HasIndex(e => new { e.TargetId, e.ActionType })
                    .HasDatabaseName("IX_AUDIT_LOGS_TARGET_ACTION");

                // Admin关系已在Admin实体中配置
            });

            // 配置通知模板表
            modelBuilder.Entity<NotificationTemplate>(entity =>
            {
                entity.ToTable("NOTIFICATION_TEMPLATES", t =>
                {
                    t.HasCheckConstraint("CK_NOTIFICATION_TEMPLATES_TYPE",
                        "TEMPLATE_TYPE IN ('商品相关','交易相关','评价相关','系统通知')");
                    t.HasCheckConstraint("CK_NOTIFICATION_TEMPLATES_PRIORITY",
                        "PRIORITY BETWEEN 1 AND 5");
                    t.HasCheckConstraint("CK_NOTIFICATION_TEMPLATES_IS_ACTIVE",
                        "IS_ACTIVE IN (0,1)");
                });
                entity.HasKey(e => e.TemplateId);

                // 主键配置 - 自增ID
                entity.Property(e => e.TemplateId)
                    .HasColumnName("TEMPLATE_ID")
                    .HasColumnType("NUMBER")
                    .ValueGeneratedOnAdd();

                // 模板名称配置
                entity.Property(e => e.TemplateName)
                    .HasColumnName("TEMPLATE_NAME")
                    .HasColumnType("VARCHAR2(100)")
                    .IsRequired()
                    .HasMaxLength(100);

                // 模板类型配置
                entity.Property(e => e.TemplateType)
                    .HasColumnName("TEMPLATE_TYPE")
                    .HasColumnType("VARCHAR2(20)")
                    .IsRequired()
                    .HasMaxLength(20);

                // 模板内容配置 - CLOB类型
                entity.Property(e => e.TemplateContent)
                    .HasColumnName("TEMPLATE_CONTENT")
                    .HasColumnType("CLOB")
                    .IsRequired();

                // 模板描述配置
                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasColumnType("VARCHAR2(500)")
                    .HasMaxLength(500);

                // 优先级配置 - 默认值2
                entity.Property(e => e.Priority)
                    .HasColumnName("PRIORITY")
                    .HasColumnType("NUMBER")
                    .IsRequired()
                    .HasDefaultValue(2);

                // 是否启用配置 - 默认值1
                entity.Property(e => e.IsActive)
                    .HasColumnName("IS_ACTIVE")
                    .IsRequired()
                    .HasDefaultValue(1);

                // 创建时间配置 - 默认当前时间
                entity.Property(e => e.CreatedAt)
                    .HasColumnName("CREATED_AT")
                    .HasColumnType("TIMESTAMP")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 更新时间配置
                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("UPDATED_AT")
                    .HasColumnType("TIMESTAMP");

                // 创建者ID配置
                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasColumnType("NUMBER");

                // 索引配置
                entity.HasIndex(e => e.TemplateType)
                    .HasDatabaseName("IX_NOTIFICATION_TEMPLATES_TYPE");

                entity.HasIndex(e => e.IsActive)
                    .HasDatabaseName("IX_NOTIFICATION_TEMPLATES_IS_ACTIVE");

                entity.HasIndex(e => e.Priority)
                    .HasDatabaseName("IX_NOTIFICATION_TEMPLATES_PRIORITY");

                entity.HasIndex(e => e.CreatedBy)
                    .HasDatabaseName("IX_NOTIFICATION_TEMPLATES_CREATED_BY");

                // 复合索引（用于查询优化）
                entity.HasIndex(e => new { e.TemplateType, e.IsActive })
                    .HasDatabaseName("IX_NOTIFICATION_TEMPLATES_TYPE_ACTIVE");

                entity.HasIndex(e => new { e.IsActive, e.Priority })
                    .HasDatabaseName("IX_NOTIFICATION_TEMPLATES_ACTIVE_PRIORITY");

                // 配置与User的多对一关系（创建者）
                entity.HasOne(e => e.Creator)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_TEMPLATE_CREATOR");

                // 配置与Notification的一对多关系
                entity.HasMany(e => e.Notifications)
                    .WithOne(n => n.Template)
                    .HasForeignKey(n => n.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_NOTIFICATION_TEMPLATE");
            });

            // 配置通知表
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("NOTIFICATIONS", t =>
                {
                    t.HasCheckConstraint("CK_NOTIFICATIONS_SEND_STATUS",
                        "SEND_STATUS IN ('待发送','成功','失败')");
                    t.HasCheckConstraint("CK_NOTIFICATIONS_RETRY_COUNT",
                        "RETRY_COUNT >= 0");
                });
                entity.HasKey(e => e.NotificationId);

                // 主键配置 - 自增ID
                entity.Property(e => e.NotificationId)
                    .HasColumnName("NOTIFICATION_ID")
                    .HasColumnType("NUMBER")
                    .ValueGeneratedOnAdd();

                // 模板ID外键配置
                entity.Property(e => e.TemplateId)
                    .HasColumnName("TEMPLATE_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 接收者ID外键配置
                entity.Property(e => e.RecipientId)
                    .HasColumnName("RECIPIENT_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 订单ID外键配置 - 可选
                entity.Property(e => e.OrderId)
                    .HasColumnName("ORDER_ID")
                    .HasColumnType("NUMBER");

                // 模板参数配置 - CLOB类型
                entity.Property(e => e.TemplateParams)
                    .HasColumnName("TEMPLATE_PARAMS")
                    .HasColumnType("CLOB");

                // 发送状态配置 - 默认值"待发送"
                entity.Property(e => e.SendStatus)
                    .HasColumnName("SEND_STATUS")
                    .HasColumnType("VARCHAR2(20)")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("待发送");

                // 重试次数配置 - 默认值0
                entity.Property(e => e.RetryCount)
                    .HasColumnName("RETRY_COUNT")
                    .HasColumnType("NUMBER")
                    .IsRequired()
                    .HasDefaultValue(0);

                // 最后尝试时间配置 - 默认当前时间
                entity.Property(e => e.LastAttemptTime)
                    .HasColumnName("LAST_ATTEMPT_TIME")
                    .HasColumnType("TIMESTAMP")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 创建时间配置 - 默认当前时间
                entity.Property(e => e.CreatedAt)
                    .HasColumnName("CREATED_AT")
                    .HasColumnType("TIMESTAMP")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 发送成功时间配置
                entity.Property(e => e.SentAt)
                    .HasColumnName("SENT_AT")
                    .HasColumnType("TIMESTAMP");

                // 索引配置
                entity.HasIndex(e => e.TemplateId)
                    .HasDatabaseName("IX_NOTIFICATIONS_TEMPLATE_ID");

                entity.HasIndex(e => e.RecipientId)
                    .HasDatabaseName("IX_NOTIFICATIONS_RECIPIENT_ID");

                entity.HasIndex(e => e.OrderId)
                    .HasDatabaseName("IX_NOTIFICATIONS_ORDER_ID");

                entity.HasIndex(e => e.SendStatus)
                    .HasDatabaseName("IX_NOTIFICATIONS_SEND_STATUS");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_NOTIFICATIONS_CREATED_AT");

                entity.HasIndex(e => e.LastAttemptTime)
                    .HasDatabaseName("IX_NOTIFICATIONS_LAST_ATTEMPT_TIME");

                entity.HasIndex(e => e.RetryCount)
                    .HasDatabaseName("IX_NOTIFICATIONS_RETRY_COUNT");

                // 复合索引（用于查询优化）
                entity.HasIndex(e => new { e.RecipientId, e.SendStatus })
                    .HasDatabaseName("IX_NOTIFICATIONS_RECIPIENT_STATUS");

                entity.HasIndex(e => new { e.SendStatus, e.LastAttemptTime })
                    .HasDatabaseName("IX_NOTIFICATIONS_STATUS_TIME");

                entity.HasIndex(e => new { e.SendStatus, e.RetryCount })
                    .HasDatabaseName("IX_NOTIFICATIONS_STATUS_RETRY");

                entity.HasIndex(e => new { e.RecipientId, e.CreatedAt })
                    .HasDatabaseName("IX_NOTIFICATIONS_RECIPIENT_TIME");

                // 配置与User的多对一关系（接收者）
                entity.HasOne(e => e.Recipient)
                    .WithMany(u => u.ReceivedNotifications)
                    .HasForeignKey(e => e.RecipientId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_NOTIFICATION_RECIPIENT");

                // 配置与AbstractOrder的多对一关系（可选）
                entity.HasOne(e => e.AbstractOrder)
                    .WithMany(a => a.Notifications)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_NOTIFICATION_ORDER");

                // Template关系已在NotificationTemplate实体中配置
            });

            // 配置评价表
            modelBuilder.Entity<Review>(entity =>
            {
                entity.ToTable("REVIEWS", t =>
                {
                    t.HasCheckConstraint("CK_REVIEWS_RATING",
                        "RATING IS NULL OR (RATING >= 1 AND RATING <= 5)");
                    t.HasCheckConstraint("CK_REVIEWS_DESC_ACCURACY",
                        "DESC_ACCURACY IS NULL OR (DESC_ACCURACY >= 1 AND DESC_ACCURACY <= 5)");
                    t.HasCheckConstraint("CK_REVIEWS_SERVICE_ATTITUDE",
                        "SERVICE_ATTITUDE IS NULL OR (SERVICE_ATTITUDE >= 1 AND SERVICE_ATTITUDE <= 5)");
                    t.HasCheckConstraint("CK_REVIEWS_IS_ANONYMOUS",
                        "IS_ANONYMOUS IN (0,1)");
                });
                entity.HasKey(e => e.ReviewId);

                // 主键配置 - 自增ID
                entity.Property(e => e.ReviewId)
                    .HasColumnName("REVIEW_ID")
                    .HasColumnType("NUMBER")
                    .ValueGeneratedOnAdd();

                // 订单ID外键配置
                entity.Property(e => e.OrderId)
                    .HasColumnName("ORDER_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 总体评分配置 - 精确数字类型
                entity.Property(e => e.Rating)
                    .HasColumnName("RATING")
                    .HasColumnType("NUMBER(2,1)")
                    .HasPrecision(2, 1);

                // 描述准确性评分配置
                entity.Property(e => e.DescAccuracy)
                    .HasColumnName("DESC_ACCURACY")
                    .HasColumnType("NUMBER(2,0)");

                // 服务态度评分配置
                entity.Property(e => e.ServiceAttitude)
                    .HasColumnName("SERVICE_ATTITUDE")
                    .HasColumnType("NUMBER(2,0)");

                // 匿名状态配置 - 默认值0
                entity.Property(e => e.IsAnonymous)
                    .HasColumnName("IS_ANONYMOUS")
                    .IsRequired()
                    .HasDefaultValue(0);

                // 创建时间配置 - 默认当前时间
                entity.Property(e => e.CreateTime)
                    .HasColumnName("CREATE_TIME")
                    .HasColumnType("TIMESTAMP")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 卖家回复配置 - CLOB类型
                entity.Property(e => e.SellerReply)
                    .HasColumnName("SELLER_REPLY")
                    .HasColumnType("CLOB");

                // 评价内容配置 - CLOB类型
                entity.Property(e => e.Content)
                    .HasColumnName("CONTENT")
                    .HasColumnType("CLOB");

                // 索引配置
                entity.HasIndex(e => e.OrderId)
                    .HasDatabaseName("IX_REVIEWS_ORDER_ID");

                entity.HasIndex(e => e.Rating)
                    .HasDatabaseName("IX_REVIEWS_RATING");

                entity.HasIndex(e => e.CreateTime)
                    .HasDatabaseName("IX_REVIEWS_CREATE_TIME");

                entity.HasIndex(e => e.IsAnonymous)
                    .HasDatabaseName("IX_REVIEWS_IS_ANONYMOUS");

                // 复合索引（用于查询优化）
                entity.HasIndex(e => new { e.OrderId, e.CreateTime })
                    .HasDatabaseName("IX_REVIEWS_ORDER_TIME");

                entity.HasIndex(e => new { e.Rating, e.CreateTime })
                    .HasDatabaseName("IX_REVIEWS_RATING_TIME");

                entity.HasIndex(e => new { e.IsAnonymous, e.CreateTime })
                    .HasDatabaseName("IX_REVIEWS_ANONYMOUS_TIME");

                // 配置与AbstractOrder的多对一关系
                entity.HasOne(e => e.AbstractOrder)
                    .WithMany(a => a.Reviews)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_REVIEW_ORDER");
            });

            // 配置举报表
            modelBuilder.Entity<Reports>(entity =>
            {
                entity.ToTable("REPORTS", t =>
                {
                    t.HasCheckConstraint("CK_REPORTS_TYPE",
                        "TYPE IN ('商品问题','服务问题','欺诈','虚假描述','其他')");
                    t.HasCheckConstraint("CK_REPORTS_PRIORITY",
                        "PRIORITY IS NULL OR (PRIORITY >= 1 AND PRIORITY <= 10)");
                    t.HasCheckConstraint("CK_REPORTS_STATUS",
                        "STATUS IN ('待处理','处理中','已处理','已关闭')");
                });
                entity.HasKey(e => e.ReportId);

                // 主键配置 - 自增ID
                entity.Property(e => e.ReportId)
                    .HasColumnName("REPORT_ID")
                    .HasColumnType("NUMBER")
                    .ValueGeneratedOnAdd();

                // 订单ID外键配置
                entity.Property(e => e.OrderId)
                    .HasColumnName("ORDER_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 举报人ID外键配置
                entity.Property(e => e.ReporterId)
                    .HasColumnName("REPORTER_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 举报类型配置
                entity.Property(e => e.Type)
                    .HasColumnName("TYPE")
                    .HasColumnType("VARCHAR2(50)")
                    .IsRequired()
                    .HasMaxLength(50);

                // 优先级配置
                entity.Property(e => e.Priority)
                    .HasColumnName("PRIORITY")
                    .HasColumnType("NUMBER(2,0)");

                // 举报描述配置 - CLOB类型
                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasColumnType("CLOB");

                // 处理状态配置 - 默认值"待处理"
                entity.Property(e => e.Status)
                    .HasColumnName("STATUS")
                    .HasColumnType("VARCHAR2(20)")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("待处理");

                // 创建时间配置 - 默认当前时间
                entity.Property(e => e.CreateTime)
                    .HasColumnName("CREATE_TIME")
                    .HasColumnType("TIMESTAMP")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 索引配置
                entity.HasIndex(e => e.OrderId)
                    .HasDatabaseName("IX_REPORTS_ORDER_ID");

                entity.HasIndex(e => e.ReporterId)
                    .HasDatabaseName("IX_REPORTS_REPORTER_ID");

                entity.HasIndex(e => e.Type)
                    .HasDatabaseName("IX_REPORTS_TYPE");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_REPORTS_STATUS");

                entity.HasIndex(e => e.Priority)
                    .HasDatabaseName("IX_REPORTS_PRIORITY");

                entity.HasIndex(e => e.CreateTime)
                    .HasDatabaseName("IX_REPORTS_CREATE_TIME");

                // 复合索引（用于查询优化）
                entity.HasIndex(e => new { e.Status, e.Priority })
                    .HasDatabaseName("IX_REPORTS_STATUS_PRIORITY");

                entity.HasIndex(e => new { e.ReporterId, e.CreateTime })
                    .HasDatabaseName("IX_REPORTS_REPORTER_TIME");

                entity.HasIndex(e => new { e.Type, e.Status })
                    .HasDatabaseName("IX_REPORTS_TYPE_STATUS");

                // 配置与AbstractOrder的多对一关系
                entity.HasOne(e => e.AbstractOrder)
                    .WithMany(a => a.Reports)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_REPORT_ORDER");

                // 配置与User的多对一关系（举报人）
                entity.HasOne(e => e.Reporter)
                    .WithMany(u => u.Reports)
                    .HasForeignKey(e => e.ReporterId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_REPORT_USER");

                // 配置与ReportEvidence的一对多关系
                entity.HasMany(e => e.Evidences)
                    .WithOne(r => r.Report)
                    .HasForeignKey(r => r.ReportId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_EVIDENCE_REPORT");
            });

            // 配置举报证据表
            modelBuilder.Entity<ReportEvidence>(entity =>
            {
                entity.ToTable("REPORT_EVIDENCE", t =>
                {
                    t.HasCheckConstraint("CK_REPORT_EVIDENCE_FILE_TYPE",
                        "FILE_TYPE IN ('图片','视频','文档')");
                });
                entity.HasKey(e => e.EvidenceId);

                // 主键配置 - 自增ID
                entity.Property(e => e.EvidenceId)
                    .HasColumnName("EVIDENCE_ID")
                    .HasColumnType("NUMBER")
                    .ValueGeneratedOnAdd();

                // 举报ID外键配置
                entity.Property(e => e.ReportId)
                    .HasColumnName("REPORT_ID")
                    .HasColumnType("NUMBER")
                    .IsRequired();

                // 文件类型配置
                entity.Property(e => e.FileType)
                    .HasColumnName("FILE_TYPE")
                    .HasColumnType("VARCHAR2(20)")
                    .IsRequired()
                    .HasMaxLength(20);

                // 文件URL配置
                entity.Property(e => e.FileUrl)
                    .HasColumnName("FILE_URL")
                    .HasColumnType("VARCHAR2(200)")
                    .IsRequired()
                    .HasMaxLength(200);

                // 上传时间配置 - 默认当前时间
                entity.Property(e => e.UploadedAt)
                    .HasColumnName("UPLOADED_AT")
                    .HasColumnType("TIMESTAMP")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 索引配置
                entity.HasIndex(e => e.ReportId)
                    .HasDatabaseName("IX_REPORT_EVIDENCE_REPORT_ID");

                entity.HasIndex(e => e.FileType)
                    .HasDatabaseName("IX_REPORT_EVIDENCE_FILE_TYPE");

                entity.HasIndex(e => e.UploadedAt)
                    .HasDatabaseName("IX_REPORT_EVIDENCE_UPLOADED_AT");

                // 复合索引（用于查询优化）
                entity.HasIndex(e => new { e.ReportId, e.FileType })
                    .HasDatabaseName("IX_REPORT_EVIDENCE_REPORT_TYPE");

                entity.HasIndex(e => new { e.FileType, e.UploadedAt })
                    .HasDatabaseName("IX_REPORT_EVIDENCE_TYPE_TIME");

                // Report关系已在Reports实体中配置
            });
        }
    }
}
