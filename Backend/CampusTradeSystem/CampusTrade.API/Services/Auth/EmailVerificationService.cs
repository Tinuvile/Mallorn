using System;
using System.Linq;
using System.Threading.Tasks;
using CampusTrade.API.Infrastructure.Utils.Security;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Options;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CampusTrade.API.Services.Auth
{
    public class EmailVerificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly EmailService _emailService;
        private readonly EmailVerificationOptions _options;
        private readonly ILogger<EmailVerificationService> _logger;

        public EmailVerificationService(
            IUnitOfWork unitOfWork,
            EmailService emailService,
            IOptions<EmailVerificationOptions> options,
            ILogger<EmailVerificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// 生成6位验证码并发送邮件
        /// </summary>
        public async Task<(bool Success, string Message)> SendVerificationCodeAsync(int userId, string email)
        {
            try
            {
                // 检查用户是否存在
                var user = await _unitOfWork.Users.GetByPrimaryKeyAsync(userId);
                if (user == null)
                    return (false, "用户不存在");

                // 限制发送频率
                var recentVerification = await _unitOfWork.EmailVerifications
                    .GetRecentVerificationAsync(userId, email, _options.SendFrequencyLimitMinutes);
                if (recentVerification != null)
                    return (false, $"验证码发送过频繁，请{_options.SendFrequencyLimitMinutes}分钟后再试");

                // 生成安全的6位数字验证码
                var verificationCode = VerificationCodeGenerator.GenerateNumericCode(6);
                var expireTime = DateTime.Now.AddMinutes(_options.CodeExpirationMinutes);

                // 保存验证码记录
                var verification = new EmailVerification
                {
                    UserId = userId,
                    Email = email,
                    VerificationCode = verificationCode,
                    ExpireTime = expireTime,
                    IsUsed = 0, // 未使用
                    CreatedAt = DateTime.Now
                };
                await _unitOfWork.EmailVerifications.AddAsync(verification);
                await _unitOfWork.SaveChangesAsync();

                // 发送验证邮件
                var subject = "校园交易系统 - 邮箱验证码";
                var body = $"您的邮箱验证码为：{verificationCode}，{_options.CodeExpirationMinutes}分钟内有效，请勿泄露给他人。";
                var sendResult = await _emailService.SendEmailAsync(email, subject, body);

                return (sendResult.Success, sendResult.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送验证码失败，用户ID: {UserId}, 邮箱: {Email}", userId, email);
                return (false, "发送验证码时发生错误");
            }
        }

        /// <summary>
        /// 生成验证链接（令牌）并发送邮件
        /// </summary>
        public async Task<(bool Success, string Message)> SendVerificationLinkAsync(int userId, string email)
        {
            try
            {
                // 检查用户是否存在
                var user = await _unitOfWork.Users.GetByPrimaryKeyAsync(userId);
                if (user == null)
                    return (false, "用户不存在");

                // 生成安全的64位随机令牌
                var token = VerificationCodeGenerator.GenerateSecureToken();
                var expireTime = DateTime.Now.AddHours(_options.TokenExpirationHours);
                var verifyUrl = _options.GetVerifyEmailUrl(token);

                // 保存令牌记录
                var verification = new EmailVerification
                {
                    UserId = userId,
                    Email = email,
                    Token = token,
                    ExpireTime = expireTime,
                    IsUsed = 0, // 未使用
                    CreatedAt = DateTime.Now
                };
                await _unitOfWork.EmailVerifications.AddAsync(verification);
                await _unitOfWork.SaveChangesAsync();

                // 发送验证邮件（HTML格式支持链接点击）
                var subject = "校园交易系统 - 邮箱验证链接";
                var body = $@"
                    <p>请点击以下链接完成邮箱验证：</p>
                    <a href='{verifyUrl}'>{verifyUrl}</a>
                    <p>链接{_options.TokenExpirationHours}小时内有效，如非本人操作请忽略。</p>";
                var sendResult = await _emailService.SendEmailAsync(email, subject, body);

                return sendResult.Success
                    ? (true, "验证链接已发送，请查收邮件")
                    : (false, sendResult.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送验证链接失败，用户ID: {UserId}, 邮箱: {Email}", userId, email);
                return (false, "发送验证链接时发生错误");
            }
        }

        /// <summary>
        /// 验证用户提交的验证码
        /// </summary>
        public async Task<(bool Valid, string Message)> VerifyCodeAsync(int userId, string code)
        {
            try
            {
                // 查询有效的验证码记录
                var verification = await _unitOfWork.EmailVerifications
                    .GetByVerificationCodeAsync(userId, code);

                if (verification == null)
                    return (false, "验证码无效或已过期");

                // 开始事务
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // 标记验证码为已使用
                    verification.IsUsed = 1; // 已使用

                    // 更新用户邮箱验证状态
                    await _unitOfWork.Users.SetEmailVerifiedAsync(userId, true);

                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();

                    return (true, "邮箱验证成功");
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证码验证失败，用户ID: {UserId}, 验证码: {Code}", userId, code);
                return (false, "验证码验证时发生错误");
            }
        }

        /// <summary>
        /// 验证用户点击的令牌链接
        /// </summary>
        public async Task<(bool Valid, string Message, int UserId)> VerifyTokenAsync(string token)
        {
            try
            {
                // 查询有效的令牌记录
                var verification = await _unitOfWork.EmailVerifications
                    .GetByTokenAsync(token);

                if (verification == null)
                    return (false, "验证链接无效或已过期", 0);

                // 开始事务
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // 标记令牌为已使用
                    verification.IsUsed = 1; // 已使用

                    // 更新用户邮箱验证状态
                    await _unitOfWork.Users.SetEmailVerifiedAsync(verification.UserId, true);

                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();

                    return (true, "邮箱验证成功", verification.UserId);
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "令牌验证失败，令牌: {Token}", token);
                return (false, "令牌验证时发生错误", 0);
            }
        }
    }
}