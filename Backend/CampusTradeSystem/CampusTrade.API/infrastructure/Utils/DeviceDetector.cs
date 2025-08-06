using System.Text.RegularExpressions;
using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Infrastructure.Utils;

public static class DeviceDetector
{
    /// <summary>
    /// 从UserAgent解析设备类型（Mobile/PC/Tablet）
    /// </summary>
    public static string GetDeviceType(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return LoginLogs.DeviceTypes.PC;

        var lowerAgent = userAgent.ToLower();

        // 平板设备检测
        if (Regex.IsMatch(lowerAgent, @"(ipad|tablet|playbook|silk)|(android(?!.*mobile))"))
            return LoginLogs.DeviceTypes.Tablet;

        // 移动设备检测
        if (Regex.IsMatch(lowerAgent, @"iphone|ipod|android|blackberry|opera mini|opera mobi|windows phone"))
            return LoginLogs.DeviceTypes.Mobile;

        // 默认PC
        return LoginLogs.DeviceTypes.PC;
    }
}
