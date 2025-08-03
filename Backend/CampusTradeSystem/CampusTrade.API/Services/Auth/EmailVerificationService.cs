using System;
using System.Linq;
using System.Threading.Tasks;
using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CampusTrade.API.Services.Auth
{
    public class EmailVerificationService
    {
        private readonly CampusTradeDbContext _dbContext;
        private readonly EmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<EmailVerificationService> _logger;

        // 验证码有效期（10分钟）
        private const int CodeExpireMinutes = 10;
        // 验证链接有效期（24小时）
        private const int TokenExpireHours = 24;

        public EmailVerificationService(
            CampusTradeDbContext dbContext,
            EmailService emailService,
            IUserRepository userRepository,
            ILogger<EmailVerificationService> logger)
        {
            _dbContext = dbContext;
            _emailService = emailService;
            _userRepository = userRepository;
            _logger = logger;
        }

        /// <summary>
        /// 生成6位验证码并发送邮件
        /// </summary>
        public async Task<(bool Success, string Message)> SendVerificationCodeAsync(int userId, string email)
        {
            // 检查用户是否存在
            var user = await _userRepository.GetByPrimaryKeyAsync(userId);
            if (user == null)
                return (false, "用户不存在");

            // 限制发送频率（1分钟内不重复发送）
            var recentVerification = await _dbContext.EmailVerifications
                .Where(v => v.UserId == userId
                            && v.Email == email
                            && v.IsUsed == 0
                            && v.CreatedAt >= DateTime.Now.AddMinutes(-1))
                .AnyAsync();
            if (recentVerification)
                return (false, "验证码发送过频繁，请1分钟后再试");

            // 生成6位数字验证码
            var verificationCode = new Random().Next(100000, 999999).ToString();
            var expireTime = DateTime.Now.AddMinutes(CodeExpireMinutes);

            // 保存验证码记录
            var verification = new EmailVerification
            {
                UserId = userId,
                Email = email,
                VerificationCode = verificationCode,
                ExpireTime = expireTime,
                IsUsed = 0,
                CreatedAt = DateTime.Now
            };
            _dbContext.EmailVerifications.Add(verification);
            await _dbContext.SaveChangesAsync();

            // 发送验证邮件
            var subject = "校园交易系统 - 邮箱验证码";
            var body = $"您的邮箱验证码为：{verificationCode}，{CodeExpireMinutes}分钟内有效，请勿泄露给他人。";
            var sendResult = await _emailService.SendEmailAsync(email, subject, body);

            return (sendResult.Success, sendResult.Message);
        }

        /// <summary>
        /// 生成验证链接（令牌）并发送邮件
        /// </summary>
        public async Task<(bool Success, string Message)> SendVerificationLinkAsync(int userId, string email)
        {
            // 检查用户是否存在
            var user = await _userRepository.GetByPrimaryKeyAsync(userId);
            if (user == null)
                return (false, "用户不存在");

            // 生成64位随机令牌
            var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"); // 32+32=64位
            var expireTime = DateTime.Now.AddHours(TokenExpireHours);
            var verifyUrl = $"https://your-domain.com/verify-email?token={token}"; // 替换为实际前端地址

            // 保存令牌记录
            var verification = new EmailVerification
            {
                UserId = userId,
                Email = email,
                Token = token,
                ExpireTime = expireTime,
                IsUsed = 0,
                CreatedAt = DateTime.Now
            };
            _dbContext.EmailVerifications.Add(verification);
            await _dbContext.SaveChangesAsync();

            // 发送验证邮件（HTML格式支持链接点击）
            var subject = "校园交易系统 - 邮箱验证链接";
            var body = $@"
                <p>请点击以下链接完成邮箱验证：</p>
                <a href='{verifyUrl}'>{verifyUrl}</a>
                <p>链接{TokenExpireHours}小时内有效，如非本人操作请忽略。</p>";
            var sendResult = await _emailService.SendEmailAsync(email, subject, body);

            // 兼容EmailService默认非HTML格式的问题（可选：修改EmailService支持IsBodyHtml参数）
            if (!sendResult.Success)
                return (false, sendResult.Message);

            return (true, "验证链接已发送，请查收邮件");
        }

        /// <summary>
        /// 验证用户提交的验证码
        /// </summary>
        public async Task<(bool Valid, string Message)> VerifyCodeAsync(int userId, string code)
        {
            // 查询有效的验证码记录
            var verification = await _dbContext.EmailVerifications
                .FirstOrDefaultAsync(v => v.UserId == userId
                                            && v.VerificationCode == code
                                            && v.IsUsed == 0
                                            && v.ExpireTime >= DateTime.Now);

            if (verification == null)
                return (false, "验证码无效或已过期");

            // 标记验证码为已使用
            verification.IsUsed = 1;
            _dbContext.EmailVerifications.Update(verification);
            await _dbContext.SaveChangesAsync();

            // 更新用户邮箱验证状态
            await _userRepository.SetEmailVerifiedAsync(userId, true);
            return (true, "邮箱验证成功");
        }

        /// <summary>
        /// 验证用户点击的令牌链接
        /// </summary>
        public async Task<(bool Valid, string Message, int UserId)> VerifyTokenAsync(string token)
        {
            // 查询有效的令牌记录
            var verification = await _dbContext.EmailVerifications
                .FirstOrDefaultAsync(v => v.Token == token
                                            && v.IsUsed == 0
                                            && v.ExpireTime >= DateTime.Now);

            if (verification == null)
                return (false, "验证链接无效或已过期", 0);

            // 标记令牌为已使用
            verification.IsUsed = 1;
            _dbContext.EmailVerifications.Update(verification);
            await _dbContext.SaveChangesAsync();

            // 更新用户邮箱验证状态
            await _userRepository.SetEmailVerifiedAsync(verification.UserId, true);
            return (true, "邮箱验证成功", verification.UserId);
        }
    }
}
