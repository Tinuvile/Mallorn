using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Implementations;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Repositories
{
    public class UnitOfWorkTests : IDisposable
    {
        private readonly CampusTradeDbContext _context;
        private readonly IUnitOfWork _unitOfWork;

        public UnitOfWorkTests()
        {
            var databaseName = $"TestDb_{Guid.NewGuid()}";
            _context = TestDbContextFactory.CreateInMemoryDbContext(databaseName, seedData: false);
            _unitOfWork = new UnitOfWork(_context);
        }

        #region Repository属性测试

        [Fact]
        public void Users_ReturnsUserRepository()
        {
            // Act
            var userRepository = _unitOfWork.Users;

            // Assert
            Assert.NotNull(userRepository);
            Assert.IsType<UserRepository>(userRepository);
        }

        [Fact]
        public void Students_ReturnsStudentRepository()
        {
            // Act
            var studentRepository = _unitOfWork.Students;

            // Assert
            Assert.NotNull(studentRepository);
            Assert.IsAssignableFrom<IRepository<Student>>(studentRepository);
        }

        [Fact]
        public void RefreshTokens_ReturnsRefreshTokenRepository()
        {
            // Act
            var refreshTokenRepository = _unitOfWork.RefreshTokens;

            // Assert
            Assert.NotNull(refreshTokenRepository);
            Assert.IsAssignableFrom<IRepository<RefreshToken>>(refreshTokenRepository);
        }

        [Fact]
        public void Categories_ReturnsCategoryRepository()
        {
            // Act
            var categoryRepository = _unitOfWork.Categories;

            // Assert
            Assert.NotNull(categoryRepository);
            Assert.IsAssignableFrom<IRepository<Category>>(categoryRepository);
        }

        [Fact]
        public void Products_ReturnsProductRepository()
        {
            // Act
            var productRepository = _unitOfWork.Products;

            // Assert
            Assert.NotNull(productRepository);
            Assert.IsAssignableFrom<IRepository<Product>>(productRepository);
        }

        [Fact]
        public void Orders_ReturnsOrderRepository()
        {
            // Act
            var orderRepository = _unitOfWork.Orders;

            // Assert
            Assert.NotNull(orderRepository);
            Assert.IsAssignableFrom<IRepository<Order>>(orderRepository);
        }

        [Fact]
        public void VirtualAccounts_ReturnsVirtualAccountRepository()
        {
            // Act
            var virtualAccountRepository = _unitOfWork.VirtualAccounts;

            // Assert
            Assert.NotNull(virtualAccountRepository);
            Assert.IsAssignableFrom<IRepository<VirtualAccount>>(virtualAccountRepository);
        }

        [Fact]
        public void Notifications_ReturnsNotificationRepository()
        {
            // Act
            var notificationRepository = _unitOfWork.Notifications;

            // Assert
            Assert.NotNull(notificationRepository);
            Assert.IsAssignableFrom<IRepository<Notification>>(notificationRepository);
        }

        #endregion

        #region 事务管理测试

        [Fact]
        public async Task SaveChangesAsync_WithChanges_ReturnsSavedCount()
        {
            // Arrange
            var user = new User
            {
                StudentId = "2023001",
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "hashedpassword",
                FullName = "测试用户",
                CreditScore = 60.0m,
                IsActive = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            await _unitOfWork.Users.AddAsync(user);

            // Act
            var result = await _unitOfWork.SaveChangesAsync();

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task SaveChangesAsync_WithoutChanges_ReturnsZero()
        {
            // Act
            var result = await _unitOfWork.SaveChangesAsync();

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task BeginTransactionAsync_StartsTransaction()
        {
            // Act & Assert (should not throw)
            await _unitOfWork.BeginTransactionAsync();
        }

        [Fact]
        public async Task CommitTransactionAsync_WithTransaction_CommitsSuccessfully()
        {
            // Arrange
            await _unitOfWork.BeginTransactionAsync();

            var user = new User
            {
                StudentId = "2023002",
                Email = "test2@example.com",
                Username = "testuser2",
                PasswordHash = "hashedpassword2",
                FullName = "测试用户2",
                CreditScore = 60.0m,
                IsActive = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            await _unitOfWork.Users.AddAsync(user);

            // Act
            await _unitOfWork.CommitTransactionAsync();

            // Assert
            var savedUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == "test2@example.com");
            Assert.NotNull(savedUser);
        }

        [Fact]
        public async Task RollbackTransactionAsync_WithTransaction_RollsBackSuccessfully()
        {
            // Arrange
            await _unitOfWork.BeginTransactionAsync();

            var user = new User
            {
                StudentId = "2023003",
                Email = "test3@example.com",
                Username = "testuser3",
                PasswordHash = "hashedpassword3",
                FullName = "测试用户3",
                CreditScore = 60.0m,
                IsActive = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            await _unitOfWork.Users.AddAsync(user);

            // Act
            await _unitOfWork.RollbackTransactionAsync();

            // Assert
            var savedUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == "test3@example.com");
            Assert.Null(savedUser);
        }

        #endregion

        #region 批量操作测试

        [Fact]
        public async Task BulkInsertAsync_MultipleEntities_InsertsSuccessfully()
        {
            // Arrange
            var users = new List<User>
            {
                new User
                {
                    StudentId = "2023004",
                    Email = "test4@example.com",
                    Username = "testuser4",
                    PasswordHash = "hashedpassword4",
                    FullName = "测试用户4",
                    CreditScore = 60.0m,
                    IsActive = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString()
                },
                new User
                {
                    StudentId = "2023005",
                    Email = "test5@example.com",
                    Username = "testuser5",
                    PasswordHash = "hashedpassword5",
                    FullName = "测试用户5",
                    CreditScore = 60.0m,
                    IsActive = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString()
                }
            };

            // Act
            await _unitOfWork.BulkInsertAsync(users);
            await _unitOfWork.SaveChangesAsync();

            // Assert
            var userCount = await _unitOfWork.Users.CountAsync();
            Assert.Equal(2, userCount);
        }

        [Fact]
        public async Task BulkUpdateAsync_MultipleEntities_UpdatesSuccessfully()
        {
            // Arrange
            var users = new List<User>
            {
                new User
                {
                    StudentId = "2023006",
                    Email = "test6@example.com",
                    Username = "testuser6",
                    PasswordHash = "hashedpassword6",
                    FullName = "测试用户6",
                    CreditScore = 60.0m,
                    IsActive = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString()
                }
            };

            await _unitOfWork.BulkInsertAsync(users);
            await _unitOfWork.SaveChangesAsync();

            // 修改数据
            users[0].FullName = "更新后的用户名";
            users[0].UpdatedAt = DateTime.UtcNow;

            // Act
            await _unitOfWork.BulkUpdateAsync(users);
            await _unitOfWork.SaveChangesAsync();

            // Assert
            var updatedUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == "test6@example.com");
            Assert.NotNull(updatedUser);
            Assert.Equal("更新后的用户名", updatedUser.FullName);
        }

        [Fact]
        public async Task BulkDeleteAsync_MultipleEntities_DeletesSuccessfully()
        {
            // Arrange
            var users = new List<User>
            {
                new User
                {
                    StudentId = "2023007",
                    Email = "test7@example.com",
                    Username = "testuser7",
                    PasswordHash = "hashedpassword7",
                    FullName = "测试用户7",
                    CreditScore = 60.0m,
                    IsActive = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString()
                }
            };

            await _unitOfWork.BulkInsertAsync(users);
            await _unitOfWork.SaveChangesAsync();

            // Act
            await _unitOfWork.BulkDeleteAsync(users);
            await _unitOfWork.SaveChangesAsync();

            // Assert
            var userCount = await _unitOfWork.Users.CountAsync();
            Assert.Equal(0, userCount);
        }

        #endregion

        #region 缓存管理测试

        [Fact]
        public async Task ClearChangeTracker_ClearsTrackedEntities()
        {
            // Arrange
            var user = new User
            {
                StudentId = "2023008",
                Email = "test8@example.com",
                Username = "testuser8",
                PasswordHash = "hashedpassword8",
                FullName = "测试用户8",
                CreditScore = 60.0m,
                IsActive = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            await _unitOfWork.Users.AddAsync(user);
            Assert.True(_unitOfWork.HasPendingChanges());

            // Act
            _unitOfWork.ClearChangeTracker();

            // Assert
            Assert.False(_unitOfWork.HasPendingChanges());
        }

        [Fact]
        public async Task DetachEntity_DetachesEntity()
        {
            // Arrange
            var user = new User
            {
                StudentId = "2023009",
                Email = "test9@example.com",
                Username = "testuser9",
                PasswordHash = "hashedpassword9",
                FullName = "测试用户9",
                CreditScore = 60.0m,
                IsActive = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            await _unitOfWork.Users.AddAsync(user);

            // Act
            _unitOfWork.DetachEntity(user);

            // Assert
            var entityState = _unitOfWork.GetEntityState(user);
            Assert.Equal(EntityState.Detached, entityState);
        }

        [Fact]
        public async Task GetEntityState_ReturnsCorrectState()
        {
            // Arrange
            var user = new User
            {
                StudentId = "2023010",
                Email = "test10@example.com",
                Username = "testuser10",
                PasswordHash = "hashedpassword10",
                FullName = "测试用户10",
                CreditScore = 60.0m,
                IsActive = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            await _unitOfWork.Users.AddAsync(user);

            // Act
            var entityState = _unitOfWork.GetEntityState(user);

            // Assert
            Assert.Equal(EntityState.Added, entityState);
        }

        #endregion

        #region 性能监控测试

        [Fact]
        public async Task GetPendingChangesCount_ReturnsCorrectCount()
        {
            // Arrange
            var user1 = new User
            {
                StudentId = "2023011",
                Email = "test11@example.com",
                Username = "testuser11",
                PasswordHash = "hashedpassword11",
                FullName = "测试用户11",
                CreditScore = 60.0m,
                IsActive = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var user2 = new User
            {
                StudentId = "2023012",
                Email = "test12@example.com",
                Username = "testuser12",
                PasswordHash = "hashedpassword12",
                FullName = "测试用户12",
                CreditScore = 60.0m,
                IsActive = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            await _unitOfWork.Users.AddAsync(user1);
            await _unitOfWork.Users.AddAsync(user2);

            // Act
            var pendingChangesCount = _unitOfWork.GetPendingChangesCount();

            // Assert
            Assert.Equal(2, pendingChangesCount);
        }

        [Fact]
        public async Task HasPendingChanges_WithChanges_ReturnsTrue()
        {
            // Arrange
            var user = new User
            {
                StudentId = "2023013",
                Email = "test13@example.com",
                Username = "testuser13",
                PasswordHash = "hashedpassword13",
                FullName = "测试用户13",
                CreditScore = 60.0m,
                IsActive = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            await _unitOfWork.Users.AddAsync(user);

            // Act
            var hasPendingChanges = _unitOfWork.HasPendingChanges();

            // Assert
            Assert.True(hasPendingChanges);
        }

        [Fact]
        public void HasPendingChanges_WithoutChanges_ReturnsFalse()
        {
            // Act
            var hasPendingChanges = _unitOfWork.HasPendingChanges();

            // Assert
            Assert.False(hasPendingChanges);
        }

        #endregion

        #region 并发控制测试

        [Fact]
        public async Task ConcurrentOperations_WithTransaction_HandlesCorrectly()
        {
            // Arrange
            await _unitOfWork.BeginTransactionAsync();
            var user = new User
            {
                StudentId = "2023001",
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "hash",
                FullName = "测试用户",
                CreditScore = 60.0m
            };

            // Act & Assert
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // 模拟并发更新
            var task1 = UpdateUserCreditScoreAsync(user.UserId, 70.0m);
            var task2 = UpdateUserCreditScoreAsync(user.UserId, 80.0m);

            await Task.WhenAll(task1, task2);

            var updatedUser = await _unitOfWork.Users.GetByPrimaryKeyAsync(user.UserId);
            Assert.True(updatedUser.CreditScore == 70.0m || updatedUser.CreditScore == 80.0m);
        }

        private async Task UpdateUserCreditScoreAsync(int userId, decimal newScore)
        {
            using var scope = new UnitOfWork(_context);
            await scope.BeginTransactionAsync();
            try
            {
                var user = await scope.Users.GetByPrimaryKeyAsync(userId);
                user.CreditScore = newScore;
                await scope.SaveChangesAsync();
                await scope.CommitTransactionAsync();
            }
            catch
            {
                await scope.RollbackTransactionAsync();
                throw;
            }
        }

        #endregion

        #region 性能监控测试

        [Fact]
        public async Task BulkOperations_WithPerformanceTracking_WorksCorrectly()
        {
            // Arrange
            var users = Enumerable.Range(1, 100).Select(i => new User
            {
                StudentId = $"2023{i:D3}",
                Email = $"test{i}@example.com",
                Username = $"testuser{i}",
                PasswordHash = "hash",
                FullName = $"测试用户{i}",
                CreditScore = 60.0m
            }).ToList();

            // Act
            var startTime = DateTime.UtcNow;
            await _unitOfWork.BulkInsertAsync(users);
            var endTime = DateTime.UtcNow;

            // Assert
            Assert.True((endTime - startTime).TotalSeconds < 5); // 批量插入应在5秒内完成
            Assert.Equal(100, await _unitOfWork.Users.CountAsync());
        }

        #endregion

        public void Dispose()
        {
            _unitOfWork.Dispose();
            _context.Dispose();
        }
    }
}
