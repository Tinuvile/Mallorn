using System.Text.Json.Serialization;

namespace CampusTrade.API.Models.DTOs.Product;

/// <summary>
/// 分类响应DTO
/// </summary>
public class CategoryDto
{
    /// <summary>
    /// 分类ID
    /// </summary>
    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 父分类ID
    /// </summary>
    [JsonPropertyName("parent_id")]
    public int? ParentId { get; set; }

    /// <summary>
    /// 分类层级（1=一级分类，2=二级分类，3=三级分类）
    /// </summary>
    [JsonPropertyName("level")]
    public int Level { get; set; }

    /// <summary>
    /// 完整分类路径
    /// </summary>
    [JsonPropertyName("full_path")]
    public string FullPath { get; set; } = string.Empty;

    /// <summary>
    /// 子分类列表
    /// </summary>
    [JsonPropertyName("children")]
    public List<CategoryDto> Children { get; set; } = new();

    /// <summary>
    /// 该分类下的商品数量
    /// </summary>
    [JsonPropertyName("product_count")]
    public int ProductCount { get; set; }

    /// <summary>
    /// 该分类下的活跃商品数量
    /// </summary>
    [JsonPropertyName("active_product_count")]
    public int ActiveProductCount { get; set; }
}

/// <summary>
/// 分类树响应DTO
/// </summary>
public class CategoryTreeDto
{
    /// <summary>
    /// 一级分类列表
    /// </summary>
    [JsonPropertyName("root_categories")]
    public List<CategoryDto> RootCategories { get; set; } = new();

    /// <summary>
    /// 总分类数
    /// </summary>
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    [JsonPropertyName("last_update_time")]
    public DateTime LastUpdateTime { get; set; }
}

/// <summary>
/// 分类面包屑导航DTO
/// </summary>
public class CategoryBreadcrumbDto
{
    /// <summary>
    /// 分类路径
    /// </summary>
    [JsonPropertyName("breadcrumb")]
    public List<CategoryBreadcrumbItemDto> Breadcrumb { get; set; } = new();
}

/// <summary>
/// 分类面包屑项DTO
/// </summary>
public class CategoryBreadcrumbItemDto
{
    /// <summary>
    /// 分类ID
    /// </summary>
    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类层级
    /// </summary>
    [JsonPropertyName("level")]
    public int Level { get; set; }
}



/// <summary>
/// 预定义分类 - 三级分类体系
/// </summary>
public static class PredefinedCategories
{
    /// <summary>
    /// 一级分类：教材
    /// </summary>
    public const string TEXTBOOK = "教材";

    /// <summary>
    /// 一级分类：数码
    /// </summary>
    public const string DIGITAL = "数码";

    /// <summary>
    /// 一级分类：日用
    /// </summary>
    public const string DAILY = "日用";

    /// <summary>
    /// 获取所有一级分类
    /// </summary>
    public static List<string> GetRootCategories()
    {
        return new List<string> { TEXTBOOK, DIGITAL, DAILY };
    }

    /// <summary>
    /// 获取教材类的二级分类
    /// </summary>
    public static List<string> GetTextbookSubCategories()
    {
        return new List<string>
        {
            "计算机科学", "数学", "英语", "物理", "化学", "生物", "经济学", "管理学", "法学", "文学", "其他"
        };
    }

    /// <summary>
    /// 获取数码类的二级分类
    /// </summary>
    public static List<string> GetDigitalSubCategories()
    {
        return new List<string>
        {
            "手机", "电脑", "平板", "耳机", "音响", "相机", "游戏设备", "智能设备", "配件", "其他"
        };
    }

    /// <summary>
    /// 获取日用类的二级分类
    /// </summary>
    public static List<string> GetDailySubCategories()
    {
        return new List<string>
        {
            "服装", "鞋子", "包包", "化妆品", "护肤品", "生活用品", "文具", "体育用品", "食品", "清洁用品", "其他"
        };
    }

    /// <summary>
    /// 获取教材类的三级分类示例（按二级分类）
    /// </summary>
    public static Dictionary<string, List<string>> GetTextbookThirdLevelCategories()
    {
        return new Dictionary<string, List<string>>
        {
            { "计算机科学", new List<string> { "编程语言", "数据结构", "算法设计", "数据库", "网络技术", "人工智能", "软件工程", "其他" } },
            { "数学", new List<string> { "高等数学", "线性代数", "概率统计", "离散数学", "数学分析", "应用数学", "其他" } },
            { "英语", new List<string> { "基础英语", "大学英语", "专业英语", "口语教材", "听力教材", "语法教材", "其他" } },
            { "物理", new List<string> { "大学物理", "理论物理", "实验物理", "量子物理", "电磁学", "力学", "其他" } },
            { "化学", new List<string> { "无机化学", "有机化学", "物理化学", "分析化学", "生物化学", "化学实验", "其他" } },
            { "生物", new List<string> { "普通生物学", "分子生物学", "细胞生物学", "遗传学", "生态学", "生物化学", "微生物学", "其他" } },
            { "经济学", new List<string> { "微观经济学", "宏观经济学", "计量经济学", "国际经济学", "金融学", "投资学", "财政学", "其他" } },
            { "管理学", new List<string> { "管理学原理", "市场营销", "人力资源", "财务管理", "运营管理", "战略管理", "项目管理", "其他" } },
            { "法学", new List<string> { "法理学", "宪法学", "民法学", "刑法学", "商法学", "国际法", "诉讼法", "其他" } },
            { "文学", new List<string> { "中国文学", "外国文学", "古代文学", "现代文学", "文学理论", "写作教程", "诗歌散文", "其他" } },
            { "其他", new List<string> { "哲学", "历史", "艺术", "心理学", "社会学", "政治学", "教育学", "其他" } }
        };
    }

    /// <summary>
    /// 获取数码类的三级分类示例（按二级分类）
    /// </summary>
    public static Dictionary<string, List<string>> GetDigitalThirdLevelCategories()
    {
        return new Dictionary<string, List<string>>
        {
            { "手机", new List<string> { "iPhone", "安卓手机", "功能机", "手机配件", "手机壳", "充电器", "其他" } },
            { "电脑", new List<string> { "笔记本电脑", "台式机", "游戏本", "商务本", "超级本", "工作站", "其他" } },
            { "平板", new List<string> { "iPad", "安卓平板", "Windows平板", "电子书阅读器", "绘画板", "平板配件", "其他" } },
            { "耳机", new List<string> { "有线耳机", "无线耳机", "蓝牙耳机", "游戏耳机", "运动耳机", "降噪耳机", "其他" } },
            { "音响", new List<string> { "蓝牙音响", "有线音箱", "便携音响", "智能音箱", "专业音响", "音响配件", "其他" } },
            { "相机", new List<string> { "数码相机", "单反相机", "微单相机", "拍立得", "摄像头", "相机配件", "其他" } },
            { "游戏设备", new List<string> { "游戏主机", "掌机", "游戏手柄", "VR设备", "游戏键鼠", "游戏配件", "其他" } },
            { "智能设备", new List<string> { "智能手表", "智能手环", "智能家居", "无人机", "平衡车", "智能配件", "其他" } },
            { "配件", new List<string> { "数据线", "充电宝", "键盘", "鼠标", "U盘", "硬盘", "路由器", "其他" } },
            { "其他", new List<string> { "电子组件", "开发板", "传感器", "电池", "线材", "工具", "其他" } }
        };
    }

    /// <summary>
    /// 获取日用类的三级分类示例（按二级分类）
    /// </summary>
    public static Dictionary<string, List<string>> GetDailyThirdLevelCategories()
    {
        return new Dictionary<string, List<string>>
        {
            { "服装", new List<string> { "上衣", "下装", "外套", "内衣", "睡衣", "运动服", "其他" } },
            { "鞋子", new List<string> { "运动鞋", "休闲鞋", "皮鞋", "凉鞋", "拖鞋", "靴子", "其他" } },
            { "包包", new List<string> { "背包", "手提包", "单肩包", "钱包", "行李箱", "书包", "其他" } },
            { "化妆品", new List<string> { "面部彩妆", "眼部彩妆", "唇部彩妆", "美甲用品", "化妆工具", "卸妆用品", "其他" } },
            { "护肤品", new List<string> { "洁面用品", "爽肤水", "乳液面霜", "精华", "面膜", "防晒", "其他" } },
            { "生活用品", new List<string> { "餐具", "杯子", "床上用品", "毛巾", "收纳用品", "小家电", "其他" } },
            { "文具", new List<string> { "笔类", "纸张", "本册", "文件夹", "计算器", "办公用品", "其他" } },
            { "体育用品", new List<string> { "健身器材", "球类用品", "户外装备", "运动护具", "瑜伽用品", "游泳用品", "其他" } },
            { "食品", new List<string> { "零食", "饮料", "保健品", "茶叶", "咖啡", "调料", "其他" } },
            { "清洁用品", new List<string> { "洗洁剂", "洗衣液", "清洁剂", "纸巾", "洗手液", "消毒用品", "其他" } },
            { "其他", new List<string> { "宠物用品", "园艺用品", "汽车用品", "手工材料", "装饰品", "礼品", "其他" } }
        };
    }

    /// <summary>
    /// 获取指定二级分类的三级分类列表
    /// </summary>
    /// <param name="firstLevel">一级分类名称</param>
    /// <param name="secondLevel">二级分类名称</param>
    /// <returns>三级分类列表</returns>
    public static List<string> GetThirdLevelCategories(string firstLevel, string secondLevel)
    {
        var categories = firstLevel switch
        {
            TEXTBOOK => GetTextbookThirdLevelCategories(),
            DIGITAL => GetDigitalThirdLevelCategories(),
            DAILY => GetDailyThirdLevelCategories(),
            _ => new Dictionary<string, List<string>>()
        };

        return categories.TryGetValue(secondLevel, out var thirdLevel)
            ? thirdLevel
            : new List<string> { "其他" };
    }
}
