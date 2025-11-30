namespace Order.Application.DTOs;

public record PagedResponse<T>(
    List<T> Items,
    int Total,
    int Page,
    int PageSize
);

public record OrderStatisticsResponse(
    int TotalOrders,
    decimal TotalRevenue,
    Dictionary<string, int> OrdersByStatus,
    Dictionary<string, int> OrdersByPayment,
    List<DailyOrderStats> DailyOrders
);

public record DailyOrderStats(
    DateTime Date,
    int Count,
    decimal Revenue
);

// Sync request from other services
public record SyncUserInfoRequest(
    Guid UserId,
    string? FullName,
    string? Email,
    string? Phone
);

