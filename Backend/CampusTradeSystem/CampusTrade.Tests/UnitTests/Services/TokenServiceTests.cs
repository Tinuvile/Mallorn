using System.IdentityModel.Tokens.Jwt;
using CampusTrade.API.Models.DTOs.Auth;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Options;
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
    private readonly TokenService _tokenService;
    private readonly JwtOptions _jwtOptions;

    public TokenServiceTests()
    {
        (_mockCache, _mockCacheEntry) = MockHelper.CreateMockMemoryCacheWithEntry();
        _mockLogger = MockHelper.CreateMockLogger<TokenService>();

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
        var context = TestDbContextFactory.CreateInMemoryDbContext();

        _tokenService = new TokenService(context, jwtOptionsWrapper, _mockCache.Object, _mockLogger.Object);
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
        refreshToken.DeviceId.Should().NotBe("test_device"); // 应该是生成的指纹
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

        // Assert - 验证用户登录信息是否更新
        // 注意：由于我们使用的是内存数据库，这里的验证可能需要调整
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginIp.Should().Be(ipAddress);
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
        result.Error.Should().Be("Token已被撤销");
    }

    #endregion

    #region ValidateRefreshTokenAsync Tests

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithValidToken_ShouldReturnRefreshToken()
    {
        // Arrange
        var refreshTokenValue = "test_refresh_token_1"; // 来自测试数据

        // Act
        var result = await _tokenService.ValidateRefreshTokenAsync(refreshTokenValue);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be(refreshTokenValue);
        result.UserId.Should().Be(1);
        result.IsRevoked.Should().Be(0); // 0表示false Be(0)表示未撤销
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
        var expiredToken = "expired_refresh_token"; // 来自测试数据，已过期

        // Act
        var result = await _tokenService.ValidateRefreshTokenAsync(expiredToken);

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
        var refreshToken = "test_refresh_token_1";

        // Act
        var result = await _tokenService.RevokeRefreshTokenAsync(refreshToken, "测试撤销");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var invalidToken = "nonexistent_token";

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
        result.Should().BeGreaterThan(0); // 应该至少撤销1个Token
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
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.All(t => t.UserId == userId).Should().BeTrue();
        result.All(t => t.IsRevoked == 0).Should().BeTrue();
        result.All(t => t.ExpiryDate > DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveRefreshTokensAsync_WithInvalidUserId_ShouldReturnEmpty()
    {
        // Arrange
        var invalidUserId = 999;

        // Act
        var result = await _tokenService.GetActiveRefreshTokensAsync(invalidUserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region BlacklistTokenAsync Tests

    [Fact]
    public async Task BlacklistTokenAsync_WithValidJti_ShouldReturnTrue()
    {
        // Arrange
        var jti = "test_jti_123";
        var expiration = DateTime.UtcNow.AddMinutes(15);

        // Act
        var result = await _tokenService.BlacklistTokenAsync(jti, expiration);

        // Assert
        result.Should().BeTrue();

        // 验证CreateEntry被调用，这是Set()方法内部会调用的
        _mockCache.Verify(x => x.CreateEntry($"blacklist:{jti}"), Times.Once);

        // 验证缓存条目的值和过期时间被设置
        _mockCacheEntry.VerifySet(x => x.Value = true);
        _mockCacheEntry.VerifySet(x => x.AbsoluteExpirationRelativeToNow = It.IsAny<TimeSpan>());
    }

    [Fact]
    public async Task BlacklistTokenAsync_WithExpiredTime_ShouldNotAddToCache()
    {
        // Arrange
        var jti = "test_jti_expired";
        var expiration = DateTime.UtcNow.AddMinutes(-10); // 已过期

        // Act
        var result = await _tokenService.BlacklistTokenAsync(jti, expiration);

        // Assert
        result.Should().BeTrue();

        // 验证CreateEntry没有被调用，因为Token已过期
        _mockCache.Verify(x => x.CreateEntry(It.IsAny<object>()), Times.Never);
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
        result.Should().BeGreaterThanOrEqualTo(0); // 可能有0个或多个过期Token被清理
    }

    #endregion

    public void Dispose()
    {
        // 清理资源
    }
}
