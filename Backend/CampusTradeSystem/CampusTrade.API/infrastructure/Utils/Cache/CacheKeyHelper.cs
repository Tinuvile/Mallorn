using System.Text;

namespace CampusTrade.API.Infrastructure.Utils.Cache
{
    /// <summary>
    /// 缓存键生成器（统一命名规范）
    /// </summary>
    public static class CacheKeyHelper
    {
        // 全局前缀（可选，用于多环境隔离）
        private const string GlobalPrefix = "CT_"; // CampusTrade缩写

        // 业务模块前缀
        private const string ProductPrefix = "product:";
        private const string UserPrefix = "user:";
        private const string CategoryPrefix = "category:";
        private const string ConfigPrefix = "config:";

        /// <summary>
        /// 生成商品缓存键
        /// </summary>
        /// <param name="productId">商品ID</param>
        public static string ProductKey(int productId)
            => BuildKey(ProductPrefix, productId.ToString());

        /// <summary>
        /// 生成商品列表键（带分页和分类筛选）
        /// </summary>
        /// <param name="categoryId">分类ID（可选）</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="sortBy">排序字段</param>
        public static string ProductListKey(int? categoryId, int pageIndex, int pageSize, string? sortBy = null)
        {
            var sb = new StringBuilder()
                .Append(ProductPrefix)
                .Append("list:");

            if (categoryId.HasValue)
                sb.Append($"cat:{categoryId}:");

            sb.Append($"p:{pageIndex}:s:{pageSize}");

            if (!string.IsNullOrEmpty(sortBy))
                sb.Append($":sort={sortBy}");

            return BuildKey(sb.ToString());
        }

        /// <summary>
        /// 生成商品统计信息键
        /// </summary>
        public static string ProductStatsKey(int productId)
            => BuildKey(ProductPrefix, $"{productId}:stats");

        /// <summary>
        /// 生成用户信息键
        /// </summary>
        public static string UserKey(int userId)
            => BuildKey(UserPrefix, userId.ToString());

        /// <summary>
        /// 生成用户权限键
        /// </summary>
        public static string UserPermissionsKey(int userId)
            => BuildKey(UserPrefix, $"{userId}:perms");

        /// <summary>
        /// 生成分类树缓存键
        /// </summary>
        public static string CategoryTreeKey()
            => BuildKey(CategoryPrefix, "tree");

        /// <summary>
        /// 生成分类子项列表键
        /// </summary>
        public static string CategoryChildrenKey(int parentId)
            => BuildKey(CategoryPrefix, $"{parentId}:children");

        /// <summary>
        /// 生成系统配置键
        /// </summary>
        public static string ConfigKey(string configName)
            => BuildKey(ConfigPrefix, configName);

        /// <summary>
        /// 生成用户安全信息键
        /// </summary>
        public static string UserSecurityKey(int userId)
            => BuildKey(UserPrefix, $"{userId}:security");

        /// <summary>
        /// 生成根据用户名/邮箱查询用户的缓存键
        /// </summary>
        public static string UserByUsernameKey(string username)
            => BuildKey(UserPrefix, $"username:{username.ToLower()}");

        /// <summary>
        /// 生成学生验证缓存键
        /// </summary>
        public static string StudentValidationKey(string studentId, string name)
            => BuildKey("student:", $"validate:{studentId}:{name.ToLower()}");

        /// <summary>
        /// 获取用户相关缓存键前缀（用于批量删除）
        /// </summary>
        public static string GetUserCachePrefix(int userId)
            => BuildKey(UserPrefix, $"{userId}:");

        /// <summary>
        /// 获取学生验证缓存键前缀
        /// </summary>
        public static string GetStudentValidationPrefix()
            => BuildKey("student:", "validate:");

        // 私有方法：统一构建键格式
        private static string BuildKey(params string[] parts)
        {
            var sb = new StringBuilder(GlobalPrefix);
            foreach (var part in parts)
            {
                if (!string.IsNullOrEmpty(part))
                {
                    if (sb.Length > GlobalPrefix.Length)
                        sb.Append(':');
                    sb.Append(part);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 验证键合法性（防止注入）
        /// </summary>
        public static bool IsValidKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            // 禁止包含的字符
            var invalidChars = new[] { ' ', '\n', '\r', '\t', '#', '*' };
            return key.IndexOfAny(invalidChars) == -1;
        }

        /// <summary>
        /// 提取键中的业务ID（反向解析）
        /// </summary>
        public static bool TryExtractId(string cacheKey, string prefix, out int id)
        {
            id = 0;
            if (!cacheKey.StartsWith(GlobalPrefix + prefix))
                return false;

            var idPart = cacheKey[(GlobalPrefix.Length + prefix.Length)..];
            return int.TryParse(idPart, out id);
        }

        /// <summary>
        /// 生成分类产品列表缓存键前缀（用于批量删除）
        /// 格式：CT_product:list:cat:{categoryId}:
        /// </summary>
        public static string GetCategoryProductsCachePrefix(int categoryId)
        {
            // 使用StringBuilder避免暴露私有字段
            var sb = new StringBuilder()
            .Append("product:")  // 硬编码对应私有ProductPrefix
            .Append("list:")
            .Append($"cat:{categoryId}:");

            // 使用私有BuildKey方法（通过方法内联访问）
            return BuildKey(sb.ToString());
        }

        /// <summary>
        /// 获取所有配置缓存键的完整前缀
        /// </summary>
        public static string GetConfigKeysPrefix()
        {
            return BuildKey(ConfigPrefix); // 会自动包含GlobalPrefix
        }

        /// <summary>
        /// 判断一个缓存键是否是配置键
        /// </summary>
        public static bool IsConfigKey(string cacheKey)
        {
            return cacheKey.StartsWith(GetConfigKeysPrefix());
        }

        /// <summary>
        /// 从完整缓存键中提取原始配置名
        /// </summary>
        public static bool TryExtractConfigName(string cacheKey, out string? configName)
        {
            configName = null;
            if (!IsConfigKey(cacheKey)) return false;

            var prefix = GetConfigKeysPrefix();
            configName = cacheKey[prefix.Length..]; // 去掉前缀部分
            return true;
        }
    }
}
