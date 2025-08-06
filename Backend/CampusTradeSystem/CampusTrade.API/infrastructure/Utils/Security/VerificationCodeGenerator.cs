using System;
using System.Security.Cryptography;
using System.Text;

namespace CampusTrade.API.Infrastructure.Utils.Security
{
    /// <summary>
    /// 验证码生成器 - 用于生成安全的验证码和令牌
    /// </summary>
    public static class VerificationCodeGenerator
    {
        /// <summary>
        /// 生成指定长度的数字验证码
        /// </summary>
        /// <param name="length">验证码长度</param>
        /// <returns>数字验证码字符串</returns>
        public static string GenerateNumericCode(int length = 6)
        {
            if (length <= 0)
                throw new ArgumentException("验证码长度必须大于0", nameof(length));

            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            var result = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                rng.GetBytes(bytes);
                var randomNumber = Math.Abs(BitConverter.ToInt32(bytes, 0));
                result.Append((randomNumber % 10).ToString());
            }

            return result.ToString();
        }

        /// <summary>
        /// 生成安全的随机令牌（Base64编码）
        /// </summary>
        /// <param name="byteLength">令牌字节长度，默认48字节（生成64字符的Base64字符串）</param>
        /// <returns>Base64编码的令牌字符串</returns>
        public static string GenerateSecureToken(int byteLength = 48)
        {
            if (byteLength <= 0)
                throw new ArgumentException("令牌字节长度必须大于0", nameof(byteLength));

            using var rng = RandomNumberGenerator.Create();
            var tokenBytes = new byte[byteLength];
            rng.GetBytes(tokenBytes);

            // 转换为Base64并移除可能的填充字符，确保URL安全
            return Convert.ToBase64String(tokenBytes)
                          .Replace('+', '-')
                          .Replace('/', '_')
                          .TrimEnd('=');
        }

        /// <summary>
        /// 生成指定长度的字母数字混合验证码
        /// </summary>
        /// <param name="length">验证码长度</param>
        /// <param name="includeUppercase">是否包含大写字母</param>
        /// <param name="includeLowercase">是否包含小写字母</param>
        /// <param name="includeNumbers">是否包含数字</param>
        /// <returns>字母数字混合验证码</returns>
        public static string GenerateAlphaNumericCode(
            int length = 8,
            bool includeUppercase = true,
            bool includeLowercase = true,
            bool includeNumbers = true)
        {
            if (length <= 0)
                throw new ArgumentException("验证码长度必须大于0", nameof(length));

            var chars = new StringBuilder();
            if (includeUppercase) chars.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            if (includeLowercase) chars.Append("abcdefghijklmnopqrstuvwxyz");
            if (includeNumbers) chars.Append("0123456789");

            if (chars.Length == 0)
                throw new ArgumentException("至少需要包含一种字符类型");

            var charArray = chars.ToString().ToCharArray();
            using var rng = RandomNumberGenerator.Create();
            var result = new StringBuilder(length);
            var bytes = new byte[4];

            for (int i = 0; i < length; i++)
            {
                rng.GetBytes(bytes);
                var randomIndex = Math.Abs(BitConverter.ToInt32(bytes, 0)) % charArray.Length;
                result.Append(charArray[randomIndex]);
            }

            return result.ToString();
        }

        /// <summary>
        /// 验证数字验证码格式
        /// </summary>
        /// <param name="code">要验证的验证码</param>
        /// <param name="expectedLength">期望的长度</param>
        /// <returns>是否为有效的数字验证码</returns>
        public static bool IsValidNumericCode(string code, int expectedLength = 6)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            if (code.Length != expectedLength)
                return false;

            foreach (char c in code)
            {
                if (!char.IsDigit(c))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 验证令牌格式（Base64编码格式）
        /// </summary>
        /// <param name="token">要验证的令牌</param>
        /// <param name="minLength">最小长度</param>
        /// <returns>是否为有效的令牌格式</returns>
        public static bool IsValidToken(string token, int minLength = 32)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            if (token.Length < minLength)
                return false;

            // 检查是否为有效的Base64URL格式
            foreach (char c in token)
            {
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                    return false;
            }

            return true;
        }
    }
}