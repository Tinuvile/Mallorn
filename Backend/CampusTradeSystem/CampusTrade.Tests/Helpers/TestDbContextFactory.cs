using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace CampusTrade.Tests.Helpers;

/// <summary>
/// 测试数据库上下文
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// 创建内存测试数据库上下文
    /// </summary>
    public static CampusTradeDbContext CreateInMemoryDbContext(string? databaseName = null, bool seedData = true)
    {
        var dbName = databaseName ?? Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<CampusTradeDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new CampusTradeDbContext(options);

        if (seedData)
        {
            SeedTestData(context);
        }
        else
        {
            // 只确保数据库创建，不播种数据
            context.Database.EnsureCreated();
        }

        return context;
    }

    /// <summary>
    /// 创建测试专用的DbContext配置
    /// </summary>
    public static DbContextOptions<CampusTradeDbContext> CreateTestDbContextOptions(string? databaseName = null)
    {
        var dbName = databaseName ?? "TestDb_" + Guid.NewGuid().ToString();

        return new DbContextOptionsBuilder<CampusTradeDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .EnableSensitiveDataLogging()
            .EnableServiceProviderCaching(false)
            .EnableDetailedErrors()
            .ConfigureWarnings(warnings =>
            {
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning);
                warnings.Ignore(CoreEventId.RowLimitingOperationWithoutOrderByWarning);
            })
            .Options;
    }

    /// <summary>
    /// 种子测试数据
    /// </summary>
    public static void SeedTestData(CampusTradeDbContext context)
    {
        // 清除现有数据
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        // 添加测试学生数据
        var students = new[]
        {
            new Student { StudentId = "2025001", Name = "张三", Department = "计算机科学与技术学院" },
            new Student { StudentId = "2025002", Name = "李四", Department = "电子信息工程学院" },
            new Student { StudentId = "2025003", Name = "王五", Department = "机械工程学院" },
            new Student { StudentId = "2025004", Name = "赵六", Department = "经济管理学院" },
            new Student { StudentId = "2025005", Name = "集成测试用户", Department = "计算机科学与技术学院" },
            // 添加集成测试需要的额外学生数据
            new Student { StudentId = "2025010", Name = "完整流程测试", Department = "计算机科学与技术学院" },
            new Student { StudentId = "2025011", Name = "最大长度测试用户", Department = "计算机科学与技术学院" },
            new Student { StudentId = "2025012", Name = "并发注册测试", Department = "计算机科学与技术学院" },
            new Student { StudentId = "STU100", Name = "压力测试用户", Department = "软件工程学院" }
        };

        context.Students.AddRange(students);

        // 添加测试用户数据
        var users = new[]
        {
            new User
            {
                UserId = 1,
                StudentId = "2025001",
                Username = "zhangsan",
                Email = "zhangsan@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                FullName = "张三",
                Phone = "13800138001",
                CreditScore = 100,
                IsActive = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                LastLoginAt = DateTime.UtcNow.AddDays(-1),
                LoginCount = 5
            },
            new User
            {
                UserId = 2,
                StudentId = "2025002",
                Username = "lisi",
                Email = "lisi@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                FullName = "李四",
                Phone = "13800138002",
                CreditScore = 95,
                IsActive = 0, // 测试禁用用户
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                LastLoginAt = DateTime.UtcNow.AddDays(-5),
                LoginCount = 2
            }
        };

        context.Users.AddRange(users);

        // 添加测试RefreshToken数据
        var refreshTokens = new[]
        {
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "test_refresh_token_1",
                UserId = 1,
                DeviceId = "test_device_1",
                IpAddress = "192.168.1.100",
                UserAgent = "Test Browser",
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                IsRevoked = 0 // 0 = not revoked
            },
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "expired_refresh_token",
                UserId = 1,
                DeviceId = "test_device_2",
                IpAddress = "192.168.1.101",
                UserAgent = "Test Mobile",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                ExpiryDate = DateTime.UtcNow.AddDays(-3), // 已过期
                IsRevoked = 0 // 0 = not revoked
            }
        };

        context.RefreshTokens.AddRange(refreshTokens);

        context.SaveChanges();
    }

    /// <summary>
    /// 为已存在的上下文播种测试数据（不重新创建数据库）
    /// </summary>
    public static void SeedTestDataToContext(CampusTradeDbContext context)
    {
        try
        {
            // 只为集成测试环境播种数据
            // 检查是否已有数据，避免重复播种
            // 使用Count()代替Any()避免Oracle布尔值问题
            if (context.Students.Count() > 0)
            {
                return;
            }

            // 添加测试学生数据
            var students = new[]
            {
                new Student { StudentId = "2025001", Name = "张三", Department = "计算机科学与技术学院" },
                new Student { StudentId = "2025002", Name = "李四", Department = "电子信息工程学院" },
                new Student { StudentId = "2025003", Name = "王五", Department = "机械工程学院" },
                new Student { StudentId = "2025004", Name = "赵六", Department = "经济管理学院" },
                new Student { StudentId = "2025005", Name = "集成测试用户", Department = "计算机科学与技术学院" },
                // 添加集成测试需要的额外学生数据
                new Student { StudentId = "2025010", Name = "完整流程测试", Department = "计算机科学与技术学院" },
                new Student { StudentId = "2025011", Name = "最大长度测试用户", Department = "计算机科学与技术学院" },
                new Student { StudentId = "2025012", Name = "并发注册测试", Department = "计算机科学与技术学院" },
                new Student { StudentId = "2025013", Name = "性能测试用户", Department = "计算机科学与技术学院" },
                new Student { StudentId = "2025014", Name = "压力测试用户", Department = "计算机科学与技术学院" },
                new Student { StudentId = "2025015", Name = "最小数据测试", Department = "计算机科学与技术学院" }
            };
            context.Students.AddRange(students);

            // 添加测试用户数据  
            var users = new[]
            {
                GetTestUser(1), // zhangsan用户
                GetTestUser(2)  // lisi用户
            };
            context.Users.AddRange(users);

            // 添加测试分类数据
            var categories = new[]
            {
                new Category { CategoryId = 1, Name = "电子设备" },
                new Category { CategoryId = 2, Name = "生活用品" },
                new Category { CategoryId = 3, Name = "学习用品" }
            };
            context.Categories.AddRange(categories);

            context.SaveChanges();
        }
        catch (Exception ex)
        {
            // 为测试环境提供降级处理
            Console.WriteLine($"SeedTestDataToContext failed: {ex.Message}");
            // 对于内存数据库，忽略播种失败
        }
    }

    /// <summary>
    /// 获取测试用户
    /// </summary>
    public static User GetTestUser(int userId = 1)
    {
        return userId switch
        {
            1 => new User
            {
                UserId = 1,
                StudentId = "2025001",
                Username = "zhangsan",
                Email = "zhangsan@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                FullName = "张三",
                Phone = "13800138001",
                CreditScore = 100,
                IsActive = 1
            },
            2 => new User
            {
                UserId = 2,
                StudentId = "2025002",
                Username = "lisi",
                Email = "lisi@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                FullName = "李四",
                Phone = "13800138002",
                CreditScore = 95,
                IsActive = 0
            },
            _ => throw new ArgumentException($"No test user with ID {userId}")
        };
    }

    /// <summary>
    /// 获取测试学生
    /// </summary>
    public static Student GetTestStudent(string studentId = "2025001")
    {
        return studentId switch
        {
            "2025001" => new Student { StudentId = "2025001", Name = "张三", Department = "计算机科学与技术学院" },
            "2025002" => new Student { StudentId = "2025002", Name = "李四", Department = "电子信息工程学院" },
            "2025003" => new Student { StudentId = "2025003", Name = "王五", Department = "机械工程学院" },
            "2025005" => new Student { StudentId = "2025005", Name = "集成测试用户", Department = "计算机科学与技术学院" },
            _ => throw new ArgumentException($"No test student with ID {studentId}")
        };
    }
}
