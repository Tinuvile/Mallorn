using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Infrastructure.Utils.Notificate
{
    /// <summary>
    /// 通知实体扩展方法
    /// </summary>
    public static class NotificationExtensions
    {
        /// <summary>
        /// 获取渲染后的通知内容
        /// </summary>
        /// <param name="notification">通知实体</param>
        /// <returns>渲染后的内容</returns>
        public static string GetRenderedContent(this Notification notification)
        {
            if (notification?.Template?.TemplateContent == null)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(notification.TemplateParams))
                return notification.Template.TemplateContent;

            try
            {
                return Notifihelper.ReplaceTemplateParams(
                    notification.Template.TemplateContent,
                    notification.TemplateParams
                );
            }
            catch
            {
                // 如果渲染失败，返回原模板内容
                return notification.Template.TemplateContent;
            }
        }
    }
}
