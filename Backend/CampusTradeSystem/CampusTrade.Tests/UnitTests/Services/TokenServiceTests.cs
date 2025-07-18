using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CampusTrade.API.Models.DTOs.Auth;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Options;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Auth;
using CampusTrade.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Services;

/// <summary>
/// TokenService单元测试
/// </summary>
public class TokenServiceTests : IDisposable
{
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<ICacheEntry> _mockCacheEntry;
    private readonly Mock<ILogger<TokenService>> _mockLogger;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly TokenService _tokenService;
    private readonly JwtOptions _jwtOptions;

    public TokenServiceTests()
    {
        (_mockCache, _mockCacheEntry) = MockHelper.CreateMockMemoryCacheWithEntry();
        _mockLogger = MockHelper.CreateMockLogger<TokenService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();

        // 设置UnitOfWork返回Mock repositories
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.RefreshTokens).Returns(_mockRefreshTokenRepository.Object);

        _jwtOptions = new JwtOptions
        {
            SecretKey = "YourSecretKeyForCampusTradingPlatformProduction2025!MustBe32CharactersLong",
            Issuer = "CampusTrade.API",
            Audience = "CampusTrade.Client",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7,
            MaxActiveDevices = 3,
            RefreshTokenRotation = true,
            RevokeDescendantRefreshTokens = true,
            EnableTokenBlacklist = true
        };

        var jwtOptionsWrapper = MockHelper.CreateMockOptions(_jwtOptions);

        // 设置默认的测试数据
        SetupDefaultTestData();

        _tokenService = new TokenService(_mockUnitOfWork.Object, jwtOptionsWrapper, _mockCache.Object, _mockLogger.Object);
    }

    private void SetupDefaultTestData()
    {
        var testUser = TestDbContextFactory.GetTestUser(1);
        var testRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "test-refresh-token-123",
            UserId = 1,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            IsRevoked = 0,
            IpAddress = "192.168.1.1",
            UserAgent = "Test Browser",
            DeviceId = "test_device",
            CreatedAt = DateTime.UtcNow
        };

        // 设置用户查询Mock
        _mockUserRepository.Setup(r => r.GetByPrimaryKeyAsync(1))
            .ReturnsAsync(testUser);
        _mockUserRepository.Setup(r => r.UpdateLastLoginAsync(1, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // 设置RefreshToken查询Mock
        _mockRefreshTokenRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<RefreshToken, bool>>>()))
            .ReturnsAsync(testRefreshToken);
        _mockRefreshTokenRepository.Setup(r => r.AddAsync(It.IsAny<RefreshToken>()))
            .ReturnsAsync((RefreshToken rt) => rt);
        _mockRefreshTokenRepository.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<RefreshToken, bool>>>()))
            .ReturnsAsync(new List<RefreshToken> { testRefreshToken });

        // 设置UnitOfWork保存Mock
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_WithValidUser_ShouldReturnValidJwtToken()
    {
        // Arrange
        var user = TestDbContextFactory.GetTestUser(1);

        // Act
        var token = await _tokenService.GenerateAccessTokenAsync(user);

        // Assert
        token.Should().NotBeEmpty();
        JwtTestHelper.IsValidJwtFormat(token).Should().BeTrue();

        var userId = JwtTestHelper.ExtractUserId(token);
        userId.Should().Be(user.UserId);

        var username = JwtTestHelper.ExtractUsername(token);
        username.Should().Be(user.Username ?? user.Email); // 如果用户名为null，使用邮箱
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_WithAdditionalClaims_ShouldIncludeExtraClaims()
    {
        // Arrange
        var user = TestDbContextFactory.GetTestUser(1);
        var additionalClaims = new[]
        {
            new System.Security.Claims.Claim("role", "admin"),
            new System.Security.Claims.Claim("permission", "manage_users")
        };

        // Act
        var token = await _tokenService.GenerateAccessTokenAsync(user, additionalClaims);

        // Assert
        token.Should().NotBeEmpty();

        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);

        jsonToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == "admin");
        jsonToken.Claims.Should().Contain(c => c.Type == "permission" && c.Value == "manage_users");
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_WithValidUserId_ShouldReturnRefreshToken()
    {
        // Arrange
        var userId = 1;
        var ipAddress = "192.168.1.1";
        var userAgent = "Test Browser";
        var deviceId = "test_device";

        // Act
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(userId, ipAddress, userAgent, deviceId);

        // Assert
        refreshToken.Should().NotBeNull();
        refreshToken.UserId.Should().Be(userId);
        refreshToken.IpAddress.Should().Be(ipAddress);
        refreshToken.UserAgent.Should().Be(userAgent);
        refreshToken.DeviceId.Should().Be(deviceId);
        refreshToken.Token.Should().NotBeEmpty();
        refreshToken.ExpiryDate.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_WithoutDeviceId_ShouldGenerateDeviceFingerprint()
    {
        // Arrange
        var userId = 1;
        var ipAddress = "192.168.1.1";
        var userAgent = "Test Browser";

        // Act
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(userId, ipAddress, userAgent);

        // Assert
        refreshToken.Should().NotBeNull();
        refreshToken.DeviceId.Should().NotBeEmpty();
        refreshToken.DeviceId.Should().NotBe("test_device"); // 应该是生成的指纹
    }

    [Fact]
    public async Task GenerateTokenResponseAsync_WithValidUser_ShouldReturnCompleteTokenResponse()
    {
        // Arrange
        var user = TestDbContextFactory.GetTestUser(1);
        var ipAddress = "192.168.1.1";
        var userAgent = "Test Browser";
        var deviceId = "test_device";

        // Act
        var tokenResponse = await _tokenService.GenerateTokenResponseAsync(user, ipAddress, userAgent, deviceId);

        // Assert
        tokenResponse.Should().NotBeNull();
        tokenResponse.AccessToken.Should().NotBeEmpty();
        tokenResponse.RefreshToken.Should().NotBeEmpty();
        tokenResponse.TokenType.Should().Be("Bearer");
        tokenResponse.ExpiresIn.Should().Be(900); // 15分钟 = 900秒
        tokenResponse.UserId.Should().Be(user.UserId);
        tokenResponse.Username.Should().Be(user.Username);
        tokenResponse.Email.Should().Be(user.Email);
        tokenResponse.DeviceId.Should().Be(deviceId);
    }

    [Fact]
    public async Task GenerateTokenResponseAsync_ShouldUpdateUserLoginInfo()
    {
        // Arrange
        var user = TestDbContextFactory.GetTestUser(1);
        var ipAddress = "192.168.1.1";

        // Act
        await _tokenService.GenerateTokenResponseAsync(user, ipAddress);

        // Assert - 验证用户登录信息是否更新
        _mockUserRepository.Verify(r => r.UpdateLastLoginAsync(user.UserId, ipAddress), Times.Once);
    }

    [Fact]
    public async Task ValidateAccessTokenAsync_WithValidToken_ShouldReturnValidResponse()
    {
        // Arrange
        var user = TestDbContextFactory.GetTestUser(1);
        var token = JwtTestHelper.GenerateTestJwtToken(user);

        // Act
        var result = await _tokenService.ValidateAccessTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.UserId.Should().Be(user.UserId);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAccessTokenAsync_WithExpiredToken_ShouldReturnInvalidResponse()
    {
        // Arrange
        var user = TestDbContextFactory.GetTestUser(1);
        var expiredToken = JwtTestHelper.GenerateExpiredJwtToken(user);

        // Act
        var result = await _tokenService.ValidateAccessTokenAsync(expiredToken);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Error.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateAccessTokenAsync_WithInvalidSignature_ShouldReturnInvalidResponse()
    {
        // Arrange
        var user = TestDbContextFactory.GetTestUser(1);
        var invalidToken = JwtTestHelper.GenerateInvalidJwtToken(user);

        // Act
        var result = await _tokenService.ValidateAccessTokenAsync(invalidToken);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Error.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateAccessTokenAsync_WithBlacklistedToken_ShouldReturnInvalidResponse()
    {
        // Arrange
        var user = TestDbContextFactory.GetTestUser(1);
        var token = JwtTestHelper.GenerateTestJwtToken(user);
        var jti = JwtTestHelper.ExtractJti(token);

        // 设置Token在黑名单中
        MockHelper.SetupMockCacheContains(_mockCache, $"blacklist:{jti}", true);

        // Act
        var result = await _tokenService.ValidateAccessTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("Token已被撤销");
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithValidToken_ShouldReturnRefreshToken()
    {
        // Arrange
        var tokenString = "test-refresh-token-123";
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = tokenString,
            UserId = 1,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            IsRevoked = 0,
            CreatedAt = DateTime.UtcNow,
            User = TestDbContextFactory.GetTestUser(1) // 添加用户信息
        };

        _mockRefreshTokenRepository.Setup(r => r.GetWithIncludeAsync(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<RefreshToken, bool>>>(),
            It.IsAny<Func<IQueryable<RefreshToken>, IOrderedQueryable<RefreshToken>>>(),
            It.IsAny<System.Linq.Expressions.Expression<System.Func<RefreshToken, object>>[]>()))
            .ReturnsAsync(new List<RefreshToken> { refreshToken });

        // Act
        var result = await _tokenService.ValidateRefreshTokenAsync(tokenString);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be(tokenString);
        result.UserId.Should().Be(1);
        result.IsRevoked.Should().Be(0); // 0表示false Be(0)表示未撤销
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var tokenString = "invalid-token";
        _mockRefreshTokenRepository.Setup(r => r.GetWithIncludeAsync(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<RefreshToken, bool>>>(),
            It.IsAny<Func<IQueryable<RefreshToken>, IOrderedQueryable<RefreshToken>>>(),
            It.IsAny<System.Linq.Expressions.Expression<System.Func<RefreshToken, object>>[]>()))
            .ReturnsAsync(new List<RefreshToken>());

        // Act
        var result = await _tokenService.ValidateRefreshTokenAsync(tokenString);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithExpiredToken_ShouldReturnNull()
    {
        // Arrange
        var tokenString = "expired-token";
        var expiredToken = new RefreshToken
        {
            Token = tokenString,
            ExpiryDate = DateTime.UtcNow.AddDays(-1), // 过期
            IsRevoked = 0
        };
        _mockRefreshTokenRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<RefreshToken, bool>>>()))
            .ReturnsAsync(expiredToken);

        // Act
        var result = await _tokenService.ValidateRefreshTokenAsync(tokenString);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithRevokedToken_ShouldReturnNull()
    {
        // Arrange
        var tokenString = "revoked-token";
        var revokedToken = new RefreshToken
        {
            Token = tokenString,
            ExpiryDate = DateTime.UtcNow.AddDays(1),
            IsRevoked = 1
        };
        _mockRefreshTokenRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<RefreshToken, bool>>>()))
            .ReturnsAsync(revokedToken);

        // Act
        var result = await _tokenService.ValidateRefreshTokenAsync(tokenString);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidRequest_ShouldReturnNewTokenResponse()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "test-refresh-token-123",
            DeviceId = "test_device"
        };

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "test-refresh-token-123",
            UserId = 1,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            IsRevoked = 0,
            CreatedAt = DateTime.UtcNow
        };

        var testUser = TestDbContextFactory.GetTestUser(1);
        refreshToken.User = testUser; // 设置用户信息

        _mockRefreshTokenRepository.Setup(r => r.GetWithIncludeAsync(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<RefreshToken, bool>>>(),
            It.IsAny<Func<IQueryable<RefreshToken>, IOrderedQueryable<RefreshToken>>>(),
            It.IsAny<System.Linq.Expressions.Expression<System.Func<RefreshToken, object>>[]>()))
            .ReturnsAsync(new List<RefreshToken> { refreshToken });
        _mockUserRepository.Setup(r => r.GetByPrimaryKeyAsync(1))
            .ReturnsAsync(testUser);
        _mockRefreshTokenRepository.Setup(r => r.Update(It.IsAny<RefreshToken>()));
        _mockRefreshTokenRepository.Setup(r => r.AddAsync(It.IsAny<RefreshToken>()))
            .ReturnsAsync(It.IsAny<RefreshToken>());
        _mockRefreshTokenRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _tokenService.RefreshTokenAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeEmpty();
        result.RefreshToken.Should().NotBeEmpty();
        result.UserId.Should().Be(1);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "invalid-token",
            DeviceId = "test_device"
        };

        _mockRefreshTokenRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<RefreshToken, bool>>>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _tokenService.RefreshTokenAsync(request));
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var tokenString = "test-refresh-token-123";

        // Act
        var result = await _tokenService.RevokeRefreshTokenAsync(tokenString);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var tokenString = "invalid-token";
        _mockRefreshTokenRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<RefreshToken, bool>>>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _tokenService.RevokeRefreshTokenAsync(tokenString);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_WithValidUserId_ShouldReturnRevokedCount()
    {
        // Arrange
        var userId = 1;
        var tokens = new List<RefreshToken>
        {
            new RefreshToken { Id = Guid.NewGuid(), UserId = userId, IsRevoked = 0 },
            new RefreshToken { Id = Guid.NewGuid(), UserId = userId, IsRevoked = 0 }
        };

        _mockRefreshTokenRepository.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<RefreshToken, bool>>>()))
            .ReturnsAsync(tokens);

        // Act
        var result = await _tokenService.RevokeAllUserTokensAsync(userId);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_WithNoTokens_ShouldReturnZero()
    {
        // Arrange
        var userId = 999;
        _mockRefreshTokenRepository.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<RefreshToken, bool>>>()))
            .ReturnsAsync(new List<RefreshToken>());

        // Act
        var result = await _tokenService.RevokeAllUserTokensAsync(userId);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetActiveRefreshTokensAsync_WithValidUserId_ShouldReturnActiveTokens()
    {
        // Arrange
        var userId = 1;
        var tokens = new List<RefreshToken>
        {
            new RefreshToken { Id = Guid.NewGuid(), UserId = userId, IsRevoked = 0, ExpiryDate = DateTime.UtcNow.AddDays(1) },
            new RefreshToken { Id = Guid.NewGuid(), UserId = userId, IsRevoked = 0, ExpiryDate = DateTime.UtcNow.AddDays(2) }
        };

        _mockRefreshTokenRepository.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<RefreshToken, bool>>>()))
            .ReturnsAsync(tokens);

        // Act
        var result = await _tokenService.GetActiveRefreshTokensAsync(userId);

        // Assert
        result.Should().NotBeEmpty();
        result.All(t => t.UserId == userId).Should().BeTrue();
        result.All(t => t.IsRevoked == 0).Should().BeTrue();
        result.All(t => t.ExpiryDate > DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveRefreshTokensAsync_WithNoActiveTokens_ShouldReturnEmpty()
    {
        // Arrange
        var userId = 999;
        _mockRefreshTokenRepository.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<RefreshToken, bool>>>()))
            .ReturnsAsync(new List<RefreshToken>());

        // Act
        var result = await _tokenService.GetActiveRefreshTokensAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CleanupExpiredTokensAsync_WithExpiredTokens_ShouldRemoveExpiredTokens()
    {
        // Arrange
        var expiredTokens = new List<RefreshToken>
        {
            new RefreshToken { Id = Guid.NewGuid(), ExpiryDate = DateTime.UtcNow.AddDays(-1) },
            new RefreshToken { Id = Guid.NewGuid(), ExpiryDate = DateTime.UtcNow.AddDays(-2) }
        };

        _mockRefreshTokenRepository.Setup(r => r.CleanupExpiredTokensAsync())
            .ReturnsAsync(2);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(2);

        // Act
        var result = await _tokenService.CleanupExpiredTokensAsync();

        // Assert
        result.Should().Be(2);
        _mockRefreshTokenRepository.Verify(r => r.CleanupExpiredTokensAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task BlacklistTokenAsync_WithValidJti_ShouldAddToBlacklist()
    {
        // Arrange
        var jti = "test-jti-123";
        var expiryDate = DateTime.UtcNow.AddMinutes(15);

        // Act
        await _tokenService.BlacklistTokenAsync(jti, expiryDate);

        // Assert
        // 验证缓存设置
        _mockCache.Verify(c => c.CreateEntry($"blacklist:{jti}"), Times.Once);
    }

    [Fact]
    public async Task IsTokenBlacklistedAsync_WithBlacklistedToken_ShouldReturnTrue()
    {
        // Arrange
        var jti = "blacklisted-jti";
        MockHelper.SetupMockCacheContains(_mockCache, $"blacklist:{jti}", true);

        // Act
        var result = await _tokenService.IsTokenBlacklistedAsync(jti);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsTokenBlacklistedAsync_WithNonBlacklistedToken_ShouldReturnFalse()
    {
        // Arrange
        var jti = "valid-jti";
        MockHelper.SetupMockCacheContains(_mockCache, $"blacklist:{jti}", false);

        // Act
        var result = await _tokenService.IsTokenBlacklistedAsync(jti);

        // Assert
        result.Should().BeFalse();
    }

    public void Dispose()
    {
        // 清理资源
    }
}
