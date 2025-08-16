using System.Security.Cryptography;
using System.Text;

namespace CampusTrade.API.Infrastructure.Utils.Security;

/// <summary>
/// 安全相关工具类
/// </summary>
public static class SecurityHelper
{
    /// <summary>
    /// 生成随机密钥
    /// </summary>
    /// <param name="length">密钥长度（字节）</param>
    /// <returns>Base64编码的密钥</returns>
    public static string GenerateSecretKey(int length = 32)
    {
        using var rng = RandomNumberGenerator.Create();
        var keyBytes = new byte[length];
        rng.GetBytes(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }

    /// <summary>
    /// 生成随机Token
    /// </summary>
    /// <param name="length">Token长度（字节）</param>
    /// <returns>Hex格式的Token</returns>
    public static string GenerateRandomToken(int length = 32)
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[length];
        rng.GetBytes(tokenBytes);
        return Convert.ToHexString(tokenBytes).ToLower();
    }

    /// <summary>
    /// 生成GUID格式的Token
    /// </summary>
    /// <returns>GUID字符串</returns>
    public static string GenerateGuidToken()
    {
        return Guid.NewGuid().ToString("N"); // 32位无分隔符格式
    }

    /// <summary>
    /// 计算字符串的SHA256哈希值
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>Hex格式的哈希值</returns>
    public static string ComputeSha256Hash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        using var sha256 = SHA256.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha256.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    /// <summary>
    /// 生成JWT JTI（JWT ID）
    /// </summary>
    /// <returns>JWT JTI</returns>
    public static string GenerateJwtId()
    {
        return Guid.NewGuid().ToString("D"); // 标准GUID格式
    }

    /// <summary>
    /// 生成设备指纹
    /// </summary>
    /// <param name="userAgent">用户代理</param>
    /// <param name="ipAddress">IP地址</param>
    /// <returns>设备指纹</returns>
    public static string GenerateDeviceFingerprint(string? userAgent, string? ipAddress)
    {
        var combined = $"{userAgent ?? "unknown"}|{ipAddress ?? "unknown"}";
        return ComputeSha256Hash(combined)[..16]; // 取前16位作为指纹
    }

    /// <summary>
    /// 混淆敏感信息（用于日志）
    /// </summary>
    /// <param name="sensitive">敏感信息</param>
    /// <param name="showLength">显示的字符数</param>
    /// <returns>混淆后的字符串</returns>
    public static string ObfuscateSensitive(string sensitive, int showLength = 4)
    {
        if (string.IsNullOrWhiteSpace(sensitive))
            return "***";

        if (sensitive.Length <= showLength)
            return new string('*', sensitive.Length);

        return sensitive[..showLength] + new string('*', Math.Max(0, sensitive.Length - showLength));
    }
}
