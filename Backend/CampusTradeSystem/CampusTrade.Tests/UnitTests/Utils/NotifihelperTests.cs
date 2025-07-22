using System;
using System.Collections.Generic;
using CampusTrade.API.Infrastructure.Utils.Notificate;
using Xunit;

namespace CampusTrade.Tests.UnitTests.Utils
{
    /// <summary>
    /// 通知工具类测试
    /// </summary>
    public class NotifihelperTests
    {
        [Fact]
        public void ReplaceTemplateParams_ValidInput_ShouldReplaceCorrectly()
        {
            // Arrange
            var templateContent = "你好，{username}，你的订单号是{orderNo}";
            var templateParamsJson = "{\"username\":\"张三\",\"orderNo\":\"12345\"}";

            // Act
            var result = Notifihelper.ReplaceTemplateParams(templateContent, templateParamsJson);

            // Assert
            Assert.Equal("你好，张三，你的订单号是12345", result);
        }

        [Fact]
        public void ReplaceTemplateParams_EmptyTemplateContent_ShouldThrowException()
        {
            // Arrange
            var templateContent = "";
            var templateParamsJson = "{\"username\":\"张三\"}";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                Notifihelper.ReplaceTemplateParams(templateContent, templateParamsJson));
            Assert.Equal("Template content cannot be empty (Parameter 'templateContent')", exception.Message);
        }

        [Fact]
        public void ReplaceTemplateParams_NullTemplateParams_ShouldThrowException()
        {
            // Arrange
            var templateContent = "你好，{username}";
            string? templateParamsJson = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                Notifihelper.ReplaceTemplateParams(templateContent, templateParamsJson));
            Assert.Equal("Template parameters cannot be empty (Parameter 'templateParamsJson')", exception.Message);
        }

        [Fact]
        public void ReplaceTemplateParams_InvalidJson_ShouldReturnOriginalTemplate()
        {
            // Arrange
            var templateContent = "你好，{username}";
            var invalidJson = "invalid json";

            // Act
            var result = Notifihelper.ReplaceTemplateParams(templateContent, invalidJson);

            // Assert
            Assert.Equal(templateContent, result);
        }

        [Fact]
        public void ReplaceTemplateParams_EmptyDictionary_ShouldThrowException()
        {
            // Arrange
            var templateContent = "你好，{username}";
            var emptyJson = "{}";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                Notifihelper.ReplaceTemplateParams(templateContent, emptyJson));
            Assert.Equal("Template parameter dictionary is empty (Parameter 'templateParamsJson')", exception.Message);
        }

        [Fact]
        public void ReplaceTemplateParams_MissingParameters_ShouldThrowException()
        {
            // Arrange
            var templateContent = "你好，{username}，你的订单号是{orderNo}";
            var templateParamsJson = "{\"username\":\"张三\"}"; // 缺少 orderNo

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                Notifihelper.ReplaceTemplateParams(templateContent, templateParamsJson));
            Assert.Contains("Missing parameters for placeholders: orderNo", exception.Message);
        }

        [Fact]
        public void ReplaceTemplateParams_ExtraParameters_ShouldIgnoreExtra()
        {
            // Arrange
            var templateContent = "你好，{username}";
            var templateParamsJson = "{\"username\":\"张三\",\"extra\":\"ignored\"}";

            // Act
            var result = Notifihelper.ReplaceTemplateParams(templateContent, templateParamsJson);

            // Assert
            Assert.Equal("你好，张三", result);
        }

        [Fact]
        public void ReplaceTemplateParams_NullParameterValue_ShouldReplaceWithEmpty()
        {
            // Arrange
            var templateContent = "你好，{username}";
            var templateParamsJson = "{\"username\":null}";

            // Act
            var result = Notifihelper.ReplaceTemplateParams(templateContent, templateParamsJson);

            // Assert
            Assert.Equal("你好，", result);
        }

        [Fact]
        public void ReplaceTemplateParams_TooManyParameters_ShouldThrowException()
        {
            // Arrange
            var templateContent = "测试模板";
            var tooManyParams = new Dictionary<string, object>();
            for (int i = 0; i < 25; i++) // 超过 MaxParamCount (20)
            {
                tooManyParams.Add($"param{i}", $"value{i}");
            }
            var templateParamsJson = System.Text.Json.JsonSerializer.Serialize(tooManyParams);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                Notifihelper.ReplaceTemplateParams(templateContent, templateParamsJson));
            Assert.Contains("Template parameter count exceeds limit (20)", exception.Message);
        }

        [Fact]
        public void ReplaceTemplateParams_ComplexTemplate_ShouldReplaceCorrectly()
        {
            // Arrange
            var templateContent = "尊敬的 {customerName}，您的订单 {orderNo} 已于 {date} 发货，快递单号：{trackingNo}，预计 {estimatedDelivery} 送达。";
            var templateParamsJson = "{\"customerName\":\"李四\",\"orderNo\":\"ORD123456\",\"date\":\"2024-01-15\",\"trackingNo\":\"SF1234567890\",\"estimatedDelivery\":\"3-5个工作日内\"}";

            // Act
            var result = Notifihelper.ReplaceTemplateParams(templateContent, templateParamsJson);

            // Assert
            Assert.Equal("尊敬的 李四，您的订单 ORD123456 已于 2024-01-15 发货，快递单号：SF1234567890，预计 3-5个工作日内 送达。", result);
        }

        [Fact]
        public void ReplaceTemplateParams_NoPlaceholders_ShouldReturnOriginal()
        {
            // Arrange
            var templateContent = "这是一个没有占位符的模板";
            var templateParamsJson = "{\"username\":\"张三\"}";

            // Act
            var result = Notifihelper.ReplaceTemplateParams(templateContent, templateParamsJson);

            // Assert
            Assert.Equal(templateContent, result);
        }
    }
}
