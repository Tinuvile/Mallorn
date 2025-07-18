using CampusTrade.API.Models.DTOs.Auth;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Utils;
using CampusTrade.API.Utils.Security;

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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            ITokenService tokenService,
            ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<User?> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                // 1. 验证学生身份
                var isValidStudent = await ValidateStudentAsync(registerDto.StudentId, registerDto.Name);
                if (!isValidStudent)
                {
                    throw new ArgumentException("学生身份验证失败，请检查学号和姓名是否正确");
                }

                // 2. 检查学号是否已被注册
                var existingUserByStudent = await _unitOfWork.Users.GetByStudentIdAsync(registerDto.StudentId);
                if (existingUserByStudent != null)
                {
                    throw new ArgumentException("该学号已被注册");
                }

                // 3. 检查邮箱是否已存在
                var userByEmail = await _unitOfWork.Users.GetByEmailAsync(registerDto.Email);
                if (userByEmail != null)
                {
                    throw new ArgumentException("该邮箱已被注册");
                }

                // 4. 检查用户名是否已存在（如果提供了用户名）
                if (!string.IsNullOrEmpty(registerDto.Username))
                {
                    var existingUserByUsername = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Username == registerDto.Username);
                    if (existingUserByUsername != null)
                    {
                        throw new ArgumentException("该用户名已被使用");
                    }
                }

                // 5. 使用事务创建新用户
                await _unitOfWork.BeginTransactionAsync();

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

                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("用户注册成功: {Email}, 学号: {StudentId}", registerDto.Email, registerDto.StudentId);
                return user;
            }
            catch (Exception ex)
            {
                // 回滚事务
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "用户注册失败: {Email}, 学号: {StudentId}", registerDto.Email, registerDto.StudentId);
                throw;
            }
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                // 支持邮箱或用户名查找
                var userByEmail = await _unitOfWork.Users.GetByEmailAsync(username);
                if (userByEmail != null && userByEmail.IsActive == 1)
                {
                    return await _unitOfWork.Users.GetUserWithStudentAsync(userByEmail.UserId);
                }

                // 按用户名查找
                var userByUsername = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive == 1);
                if (userByUsername != null)
                {
                    return await _unitOfWork.Users.GetUserWithStudentAsync(userByUsername.UserId);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户信息失败: {Username}", username);
                return null;
            }
        }

        public async Task<bool> ValidateStudentAsync(string studentId, string name)
        {
            try
            {
                // 验证学生信息是否在预存的学生表中
                var student = await _unitOfWork.Students.FirstOrDefaultAsync(s => s.StudentId == studentId && s.Name == name);
                return student != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证学生身份时发生错误: StudentId={StudentId}, Name={Name}", studentId, name);
                return false;
            }
        }

        public async Task<TokenResponse?> LoginWithTokenAsync(LoginWithDeviceRequest loginRequest, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                // 支持用户名或邮箱登录
                var user = await GetUserByUsernameAsync(loginRequest.Username);
                if (user == null)
                {
                    _logger.LogWarning("登录失败：用户不存在或已禁用，用户名: {Username}", loginRequest.Username);
                    return null;
                }

                // 验证密码
                if (!BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
                {
                    // 增加失败登录次数
                    await _unitOfWork.Users.IncrementFailedLoginAttemptsAsync(user.UserId);

                    // 检查是否需要锁定账户（例如失败5次后锁定1小时）
                    if (user.FailedLoginAttempts >= 4) // 已经有了4次失败，这次是第5次
                    {
                        await _unitOfWork.Users.LockUserAsync(user.UserId, DateTime.UtcNow.AddHours(1));
                        _logger.LogWarning("账户因多次登录失败被锁定，用户ID: {UserId}", user.UserId);
                    }

                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogWarning("登录失败：密码错误，用户名: {Username}", loginRequest.Username);
                    return null;
                }

                // 检查账户是否被锁定
                var userEntity = await _unitOfWork.Users.GetByPrimaryKeyAsync(user.UserId);
                if (userEntity != null && userEntity.IsLocked == 1)
                {
                    _logger.LogWarning("登录失败：账户被锁定，用户ID: {UserId}", user.UserId);
                    return null;
                }

                // 清除锁定状态和失败次数
                await _unitOfWork.Users.ResetFailedLoginAttemptsAsync(user.UserId);
                await _unitOfWork.Users.UnlockUserAsync(user.UserId);

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
