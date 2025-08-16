using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CampusTrade.API.Infrastructure.Utils.Notificate
{
    /// <summary>
    /// 通知模板参数替换工具
    /// </summary>
    public static class Notifihelper
    {
        private const int MaxParamCount = 20;
        private const int MaxKeyLength = 50;
        /// <summary>
        /// 用参数字典替换模板内容中的占位符
        /// </summary>
        /// <param name="templateContent">模板内容（如"你好，{username}，你的订单号是{orderNo}"）</param>
        /// <param name="templateParamsJson">模板参数（JSON格式）</param>
        /// <returns>替换后的内容</returns>
        public static string ReplaceTemplateParams(string templateContent, string? templateParamsJson)
        {
            if (string.IsNullOrWhiteSpace(templateContent))
                throw new ArgumentException("Template content cannot be empty", nameof(templateContent));

            if (string.IsNullOrWhiteSpace(templateParamsJson))
                throw new ArgumentException("Template parameters cannot be empty", nameof(templateParamsJson));

            Dictionary<string, object> dict;
            try
            {
                dict = JsonSerializer.Deserialize<Dictionary<string, object>>(templateParamsJson)
                    ?? throw new ArgumentException("Template parameter dictionary is empty", nameof(templateParamsJson));
            }
            catch (JsonException)
            {
                // 只有 JSON 解析错误时返回原模板
                return templateContent;
            }

            if (dict.Count == 0)
                throw new ArgumentException("Template parameter dictionary is empty", nameof(templateParamsJson));

            // 限制参数数量，防止性能问题
            if (dict.Count > MaxParamCount)
                throw new ArgumentException($"Template parameter count exceeds limit ({MaxParamCount})", nameof(templateParamsJson));

            // 提取模板中的所有占位符
            var placeholderMatches = System.Text.RegularExpressions.Regex.Matches(templateContent, "{(.*?)}");
            var placeholderSet = new HashSet<string>();
            foreach (System.Text.RegularExpressions.Match match in placeholderMatches)
            {
                if (match.Success && match.Groups.Count > 1)
                    placeholderSet.Add(match.Groups[1].Value);
            }

            // 只检查是否有占位符缺少对应的参数
            var missingParams = placeholderSet.Where(placeholder => !dict.ContainsKey(placeholder)).ToList();
            if (missingParams.Any())
                throw new ArgumentException($"Missing parameters for placeholders: {string.Join(", ", missingParams)}", nameof(templateParamsJson));

            string result = templateContent;
            foreach (var kv in dict)
            {
                if (kv.Key.Length > MaxKeyLength) continue; // 防止恶意参数
                string placeholder = "{" + kv.Key + "}";
                result = result.Replace(placeholder, kv.Value?.ToString() ?? string.Empty);
            }
            return result;
        }
    }
}
