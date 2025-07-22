using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
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
    // 模拟对象
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<ICacheEntry> _mockCacheEntry;
    private readonly Mock<ILogger<TokenService>> _mockLogger;
    // 测试目标服务
    private readonly TokenService _tokenService;
    // JWT配置选项
    private readonly JwtOptions _jwtOptions;

    public TokenServiceTests()
    {
        // 初始化模拟对象
        (_mockCache, _mockCacheEntry) = MockHelper.CreateMockMemoryCacheWithEntry();
        _mockLogger = MockHelper.CreateMockLogger<TokenService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();

        // 配置UnitOfWork与仓储的关系
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.RefreshTokens).Returns(_mockRefreshTokenRepository.Object);

        // 配置JWT选项
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

        // 配置默认测试数据
        SetupDefaultTestData();

        // 初始化TokenService（依赖模拟的UnitOfWork、缓存、日志）
        _tokenService = new TokenService(
            _mockUnitOfWork.Object,
            jwtOptionsWrapper,
            _mockCache.Object
        );
    }

    /// <summary>
    /// 配置模拟仓储的默认返回值（基于TestDbContextFactory的测试对象）
    /// </summary>
    private void SetupDefaultTestData()
    {
        // 测试用户和令牌对象（复用TestDbContextFactory的测试数据）
        var testUser = TestDbContextFactory.GetTestUser(1);
        var validRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "test_refresh_token_1",
            UserId = testUser.UserId,
            DeviceId = "test_device_1",
            IpAddress = "192.168.1.1",
            UserAgent = "Test Browser",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            ExpiryDate = DateTime.UtcNow.AddDays(7), // 有效令牌
            IsRevoked = 0
        };
        var expiredRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "expired_refresh_token",
            UserId = testUser.UserId,
            ExpiryDate = DateTime.UtcNow.AddDays(-1), // 已过期
            IsRevoked = 0
        };
        var revokedRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "revoked_refresh_token",
            UserId = testUser.UserId,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            IsRevoked = 1 // 已撤销
        };

        // 1. 用户仓储模拟配置
        _mockUserRepository.Setup(r => r.GetByPrimaryKeyAsync(testUser.UserId))
            .ReturnsAsync(testUser);
        _mockUserRepository.Setup(r => r.UpdateLastLoginAsync(testUser.UserId, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // 2. 刷新令牌仓储模拟配置
        // 按令牌值查询（有效令牌）
        _mockRefreshTokenRepository.Setup(r => r.GetWithIncludeAsync(
            It.Is<Expression<Func<RefreshToken, bool>>>(expr => expr.ToString().Contains(validRefreshToken.Token)),
            It.IsAny<Func<IQueryable<RefreshToken>, IOrderedQueryable<RefreshToken>>>(),
            It.IsAny<Expression<Func<RefreshToken, object>>[]>()
        )).ReturnsAsync(new List<RefreshToken> { validRefreshToken });

        // 按令牌值查询（无效/过期/撤销令牌）
        _mockRefreshTokenRepository.Setup(r => r.GetWithIncludeAsync(
            It.Is<Expression<Func<RefreshToken, bool>>>(expr => expr.ToString().Contains("invalid_token")),
            It.IsAny<Func<IQueryable<RefreshToken>, IOrderedQueryable<RefreshToken>>>(),
            It.IsAny<Expression<Func<RefreshToken, object>>[]>()
        )).ReturnsAsync(new List<RefreshToken>()); // 无效令牌返回空
        _mockRefreshTokenRepository.Setup(r => r.GetWithIncludeAsync(
            It.Is<Expression<Func<RefreshToken, bool>>>(expr => expr.ToString().Contains(expiredRefreshToken.Token)),
            It.IsAny<Func<IQueryable<RefreshToken>, IOrderedQueryable<RefreshToken>>>(),
            It.IsAny<Expression<Func<RefreshToken, object>>[]>()
        )).ReturnsAsync(new List<RefreshToken> { expiredRefreshToken });
        _mockRefreshTokenRepository.Setup(r => r.GetWithIncludeAsync(
            It.Is<Expression<Func<RefreshToken, bool>>>(expr => expr.ToString().Contains(revokedRefreshToken.Token)),
            It.IsAny<Func<IQueryable<RefreshToken>, IOrderedQueryable<RefreshToken>>>(),
            It.IsAny<Expression<Func<RefreshToken, object>>[]>()
        )).ReturnsAsync(new List<RefreshToken> { revokedRefreshToken });

        // 新增令牌
        _mockRefreshTokenRepository.Setup(r => r.AddAsync(It.IsAny<RefreshToken>()))
            .ReturnsAsync((RefreshToken rt) => rt);

        // 查询用户的所有令牌
        _mockRefreshTokenRepository.Setup(r => r.FindAsync(
            It.Is<Expression<Func<RefreshToken, bool>>>(expr => expr.ToString().Contains($"UserId == {testUser.UserId}"))
        )).ReturnsAsync(new List<RefreshToken> { validRefreshToken, expiredRefreshToken });

        // 清理过期令牌
        _mockRefreshTokenRepository.Setup(r => r.CleanupExpiredTokensAsync())
            .ReturnsAsync(1); // 模拟清理1个过期令牌

        // 3. 工作单元模拟配置
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1); // 模拟保存成功
    }


    #region GenerateAccessTokenAsync Tests

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
        username.Should().Be(user.Username);
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

    #endregion


    #region GenerateRefreshTokenAsync Tests

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
        refreshToken.DeviceId.Should().NotBe("test_device"); // 应为自动生成的指纹
    }

    #endregion


    #region GenerateTokenResponseAsync Tests

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

        // Assert - 验证用户登录信息更新方法被调用
        _mockUserRepository.Verify(r => r.UpdateLastLoginAsync(user.UserId, ipAddress), Times.Once);
    }

    #endregion


    #region ValidateAccessTokenAsync Tests

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

    #endregion


    #region ValidateRefreshTokenAsync Tests

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithValidToken_ShouldReturnRefreshToken()
    {
        // Arrange
        var validToken = "test_refresh_token_1";

        // Act
        var result = await _tokenService.ValidateRefreshTokenAsync(validToken);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be(validToken);
        result.UserId.Should().Be(1);
        result.IsRevoked.Should().Be(0); // 未撤销
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "invalid_refresh_token";

        // Act
        var result = await _tokenService.ValidateRefreshTokenAsync(invalidToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithExpiredToken_ShouldReturnNull()
    {
        // Arrange
        var expiredToken = "expired_refresh_token";

        // Act
        var result = await _tokenService.ValidateRefreshTokenAsync(expiredToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithRevokedToken_ShouldReturnNull()
    {
        // Arrange
        var revokedToken = "revoked_refresh_token";

        // Act
        var result = await _tokenService.ValidateRefreshTokenAsync(revokedToken);

        // Assert
        result.Should().BeNull();
    }

    #endregion


    #region RefreshTokenAsync Tests

    [Fact]
    public async Task RefreshTokenAsync_WithValidRequest_ShouldReturnNewTokenResponse()
    {
        // Arrange
        var refreshTokenRequest = new RefreshTokenRequest
        {
            RefreshToken = "test_refresh_token_1",
            DeviceId = "test_device_1"
        };

        // Act
        var result = await _tokenService.RefreshTokenAsync(refreshTokenRequest);

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
        var refreshTokenRequest = new RefreshTokenRequest
        {
            RefreshToken = "invalid_refresh_token"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _tokenService.RefreshTokenAsync(refreshTokenRequest));
        exception.Message.Should().Contain("无效的刷新令牌");
    }

    #endregion


    #region RevokeRefreshTokenAsync Tests

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var validToken = "test_refresh_token_1";

        // Act
        var result = await _tokenService.RevokeRefreshTokenAsync(validToken, "测试撤销");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var invalidToken = "invalid_refresh_token";

        // Act
        var result = await _tokenService.RevokeRefreshTokenAsync(invalidToken);

        // Assert
        result.Should().BeFalse();
    }

    #endregion


    #region RevokeAllUserTokensAsync Tests

    [Fact]
    public async Task RevokeAllUserTokensAsync_WithValidUserId_ShouldReturnRevokedCount()
    {
        // Arrange
        var userId = 1;

        // Act
        var result = await _tokenService.RevokeAllUserTokensAsync(userId, "测试撤销所有");

        // Assert
        result.Should().BeGreaterThan(0); // 至少撤销1个令牌
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_WithInvalidUserId_ShouldReturnZero()
    {
        // Arrange
        var invalidUserId = 999;

        // Act
        var result = await _tokenService.RevokeAllUserTokensAsync(invalidUserId);

        // Assert
        result.Should().Be(0);
    }

    #endregion


    #region GetActiveRefreshTokensAsync Tests

    [Fact]
    public async Task GetActiveRefreshTokensAsync_WithValidUserId_ShouldReturnActiveTokens()
    {
        // Arrange
        var userId = 1;

        // Act
        var result = await _tokenService.GetActiveRefreshTokensAsync(userId);

        // Assert
        result.Should().NotBeEmpty();
        result.All(t => t.UserId == userId).Should().BeTrue();
        result.All(t => t.IsRevoked == 0).Should().BeTrue(); // 未撤销
        result.All(t => t.ExpiryDate > DateTime.UtcNow).Should().BeTrue(); // 未过期
    }

    [Fact]
    public async Task GetActiveRefreshTokensAsync_WithInvalidUserId_ShouldReturnEmpty()
    {
        // Arrange
        var invalidUserId = 999;

        // Act
        var result = await _tokenService.GetActiveRefreshTokensAsync(invalidUserId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion


    #region BlacklistTokenAsync Tests

    [Fact]
    public async Task BlacklistTokenAsync_WithValidJti_ShouldAddToCache()
    {
        // Arrange
        var jti = "test_jti_123";
        var expiration = DateTime.UtcNow.AddMinutes(15); // 未过期

        // Act
        var result = await _tokenService.BlacklistTokenAsync(jti, expiration);

        // Assert
        result.Should().BeTrue();
        _mockCache.Verify(c => c.CreateEntry($"blacklist:{jti}"), Times.Once); // 验证缓存被设置
        _mockCacheEntry.VerifySet(e => e.Value = true); // 验证缓存值
    }

    [Fact]
    public async Task BlacklistTokenAsync_WithExpiredToken_ShouldNotAddToCache()
    {
        // Arrange
        var jti = "expired_jti";
        var expiration = DateTime.UtcNow.AddMinutes(-10); // 已过期

        // Act
        var result = await _tokenService.BlacklistTokenAsync(jti, expiration);

        // Assert
        result.Should().BeTrue();
        _mockCache.Verify(c => c.CreateEntry(It.IsAny<string>()), Times.Never); // 不设置缓存
    }

    #endregion


    #region IsTokenBlacklistedAsync Tests

    [Fact]
    public async Task IsTokenBlacklistedAsync_WithBlacklistedToken_ShouldReturnTrue()
    {
        // Arrange
        var jti = "blacklisted_jti";
        MockHelper.SetupMockCacheContains(_mockCache, $"blacklist:{jti}", true);

        // Act
        var result = await _tokenService.IsTokenBlacklistedAsync(jti);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsTokenBlacklistedAsync_WithValidToken_ShouldReturnFalse()
    {
        // Arrange
        var jti = "valid_jti";
        MockHelper.SetupMockCacheContains(_mockCache, $"blacklist:{jti}", false);

        // Act
        var result = await _tokenService.IsTokenBlacklistedAsync(jti);

        // Assert
        result.Should().BeFalse();
    }

    #endregion


    #region CleanupExpiredTokensAsync Tests

    [Fact]
    public async Task CleanupExpiredTokensAsync_ShouldRemoveExpiredTokens()
    {
        // Act
        var result = await _tokenService.CleanupExpiredTokensAsync();

        // Assert
        result.Should().BeGreaterThanOrEqualTo(1); // 至少清理1个过期令牌
        _mockRefreshTokenRepository.Verify(r => r.CleanupExpiredTokensAsync(), Times.Once); // 验证清理方法被调用
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once); // 验证保存
    }

    #endregion


    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
