namespace CampusTrade.API.Models.DTOs;

public class DashboardStatsDto
{
    // 月度交易数据
    public List<MonthlyTransactionDto> MonthlyTransactions { get; set; } = new();

    // 热门商品排行
    public List<PopularProductDto> PopularProducts { get; set; } = new();

    // 用户活跃度数据
    public List<UserActivityDto> UserActivities { get; set; } = new();
}

public class MonthlyTransactionDto
{
    public string Month { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class PopularProductDto
{
    public int ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public int OrderCount { get; set; }
}

public class UserActivityDto
{
    public DateTime Date { get; set; }
    public int ActiveUserCount { get; set; }
    public int NewUserCount { get; set; }
}
