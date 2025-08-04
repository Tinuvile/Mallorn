using CampusTrade.API.Data;
using CampusTrade.API.Models.DTOs.Auth;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Utils.Security;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Services.Auth
{
    public interface IAuthService
    {
        Task<TokenResponse?> LoginWithTokenAsync(LoginWithDeviceRequest loginRequest, string? ipAddress = null, string? userAgent = null);
        Task<User?> RegisterAsync(RegisterDto registerDto);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<bool> ValidateStudentAsync(string studentId, string name);
        Task<bool> LogoutAsync(string refreshToken, string? reason = null);
        Task<bool> LogoutAllDevicesAsync(int userId, string? reason = null);
        Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest refreshTokenRequest);
    }

    public class AuthService : IAuthService
    {
        private readonly CampusTradeDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            CampusTradeDbContext context,
            IConfiguration configuration,
            ITokenService tokenService,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<User?> RegisterAsync(RegisterDto registerDto)
        {
            // 1. 验证学生身份
            var isValidStudent = await ValidateStudentAsync(registerDto.StudentId, registerDto.Name);
            if (!isValidStudent)
            {
                throw new ArgumentException("学生身份验证失败，请检查学号和姓名是否正确");
            }

            // 2. 检查学号是否已被注册
            var existingUserByStudent = await _context.Users
                .FirstOrDefaultAsync(u => u.StudentId == registerDto.StudentId);
            if (existingUserByStudent != null)
            {
                throw new ArgumentException("该学号已被注册");
            }

            // 3. 检查邮箱是否已存在
            var existingUserByEmail = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == registerDto.Email);
            if (existingUserByEmail != null)
            {
                throw new ArgumentException("该邮箱已被注册");
            }

            // 4. 检查用户名是否已存在（如果提供了用户名）
            if (!string.IsNullOrEmpty(registerDto.Username))
            {
                var existingUserByUsername = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == registerDto.Username);
                if (existingUserByUsername != null)
                {
                    throw new ArgumentException("该用户名已被使用");
                }
            }

            // 5. 创建新用户
            var user = new User
            {
                StudentId = registerDto.StudentId,
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                Username = registerDto.Username,
                FullName = registerDto.Name,
                Phone = registerDto.Phone,
                CreditScore = 60.0m, // 新用户默认信用分
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = 1,
                LoginCount = 0,
                IsLocked = 0,
                FailedLoginAttempts = 0,
                TwoFactorEnabled = 0,
                EmailVerified = 0,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u =>
                    (u.Username == username || u.Email == username)
                    && u.IsActive == 1);
        }

        public async Task<bool> ValidateStudentAsync(string studentId, string name)
        {
            try
            {
                // 验证学生信息是否在预存的学生表中
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentId == studentId && s.Name == name);

                return student != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"验证学生身份时发生错误: StudentId={studentId}, Name={name}");
                return false;
            }
        }

        /// <summary>
        /// 使用TokenService进行登录
        /// </summary>
        public async Task<TokenResponse?> LoginWithTokenAsync(LoginWithDeviceRequest loginRequest, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                // 支持用户名或邮箱登录
                var user = await _context.Users
                    .Include(u => u.Student)
                    .FirstOrDefaultAsync(u =>
                        (u.Username == loginRequest.Username || u.Email == loginRequest.Username)
                        && u.IsActive == 1);

                if (user == null)
                {
                    _logger.LogWarning("登录失败：用户不存在或已禁用，用户名: {Username}", loginRequest.Username);
                    return null;
                }

                // 验证密码
                if (!BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
                {
                    // 增加失败登录次数
                    user.FailedLoginAttempts++;

                    // 检查是否需要锁定账户（例如失败5次后锁定1小时）
                    if (user.FailedLoginAttempts >= 5)
                    {
                        user.IsLocked = 1;
                        user.LockoutEnd = DateTime.UtcNow.AddHours(1);
                        _logger.LogWarning("账户因多次登录失败被锁定，用户ID: {UserId}", user.UserId);
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogWarning("登录失败：密码错误，用户名: {Username}", loginRequest.Username);
                    return null;
                }

                // 检查账户是否被锁定
                if (user.IsLocked == 1 && user.LockoutEnd > DateTime.UtcNow)
                {
                    _logger.LogWarning("登录失败：账户被锁定，用户ID: {UserId}, 锁定至: {LockoutEnd}", user.UserId, user.LockoutEnd);
                    return null;
                }

                // 清除锁定状态和失败次数
                if (user.IsLocked == 1 && user.LockoutEnd <= DateTime.UtcNow)
                {
                    user.IsLocked = 0;
                    user.LockoutEnd = null;
                }
                user.FailedLoginAttempts = 0;

                // 生成Token响应
                var tokenResponse = await _tokenService.GenerateTokenResponseAsync(
                    user,
                    ipAddress,
                    userAgent,
                    loginRequest.DeviceId);

                _logger.LogInformation("用户登录成功，用户ID: {UserId}, 设备ID: {DeviceId}", user.UserId, tokenResponse.DeviceId);
                return tokenResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登录过程中发生异常，用户名: {Username}", loginRequest.Username);
                return null;
            }
        }

        /// <summary>
        /// 注销登录（撤销RefreshToken）
        /// </summary>
        public async Task<bool> LogoutAsync(string refreshToken, string? reason = null)
        {
            try
            {
                var result = await _tokenService.RevokeRefreshTokenAsync(refreshToken, reason ?? "用户注销");
                _logger.LogInformation("用户注销，Token: {Token}, 结果: {Result}",
                    SecurityHelper.ObfuscateSensitive(refreshToken), result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注销失败，Token: {Token}", SecurityHelper.ObfuscateSensitive(refreshToken));
                return false;
            }
        }

        /// <summary>
        /// 注销所有设备
        /// </summary>
        public async Task<bool> LogoutAllDevicesAsync(int userId, string? reason = null)
        {
            try
            {
                var revokedCount = await _tokenService.RevokeAllUserTokensAsync(userId, reason ?? "注销所有设备");
                _logger.LogInformation("用户注销所有设备，用户ID: {UserId}, 撤销数量: {Count}", userId, revokedCount);
                return revokedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注销所有设备失败，用户ID: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// 刷新Token
        /// </summary>
        public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest refreshTokenRequest)
        {
            try
            {
                var tokenResponse = await _tokenService.RefreshTokenAsync(refreshTokenRequest);
                _logger.LogInformation("Token刷新成功，用户ID: {UserId}", tokenResponse.UserId);
                return tokenResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token刷新失败");
                throw;
            }
        }


    }
}
