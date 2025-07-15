using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
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
    public static CampusTradeDbContext CreateInMemoryDbContext(string? databaseName = null)
    {
        var dbName = databaseName ?? Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<CampusTradeDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new CampusTradeDbContext(options);
        SeedTestData(context);
        return context;
    }

    /// <summary>
    /// 种子测试数据
    /// </summary>
    private static void SeedTestData(CampusTradeDbContext context)
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
            new Student { StudentId = "2025004", Name = "赵六", Department = "经济管理学院" }
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
                IsRevoked = false
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
                IsRevoked = false
            }
        };

        context.RefreshTokens.AddRange(refreshTokens);

        context.SaveChanges();
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
            _ => throw new ArgumentException($"No test student with ID {studentId}")
        };
    }
}
