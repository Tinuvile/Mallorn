using System;

namespace CampusTrade.API.infrastructure.Utils
{
    /// <summary>
    /// 时间帮助类，统一使用北京时间
    /// </summary>
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo _beijingTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");

        /// <summary>
        /// 获取当前北京时间
        /// </summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(TimeHelper.UtcNow, _beijingTimeZone);

        /// <summary>
        /// 获取当前北京时间的日期部分
        /// </summary>
        public static DateTime Today => Now.Date;

        /// <summary>
        /// 将UTC时间转换为北京时间
        /// </summary>
        /// <param name="utcTime">UTC时间</param>
        /// <returns>北京时间</returns>
        public static DateTime ConvertFromUtc(DateTime utcTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, _beijingTimeZone);
        }

        /// <summary>
        /// 将北京时间转换为UTC时间
        /// </summary>
        /// <param name="beijingTime">北京时间</param>
        /// <returns>UTC时间</returns>
        public static DateTime ConvertToUtc(DateTime beijingTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(beijingTime, _beijingTimeZone);
        }

        /// <summary>
        /// 添加指定天数到当前北京时间
        /// </summary>
        /// <param name="days">天数</param>
        /// <returns>添加天数后的北京时间</returns>
        public static DateTime AddDays(int days)
        {
            return Now.AddDays(days);
        }

        /// <summary>
        /// 添加指定小时数到当前北京时间
        /// </summary>
        /// <param name="hours">小时数</param>
        /// <returns>添加小时数后的北京时间</returns>
        public static DateTime AddHours(double hours)
        {
            return Now.AddHours(hours);
        }

        /// <summary>
        /// 添加指定分钟数到当前北京时间
        /// </summary>
        /// <param name="minutes">分钟数</param>
        /// <returns>添加分钟数后的北京时间</returns>
        public static DateTime AddMinutes(double minutes)
        {
            return Now.AddMinutes(minutes);
        }

        /// <summary>
        /// 添加指定月数到当前北京时间
        /// </summary>
        /// <param name="months">月数</param>
        /// <returns>添加月数后的北京时间</returns>
        public static DateTime AddMonths(int months)
        {
            return Now.AddMonths(months);
        }
    }
}
