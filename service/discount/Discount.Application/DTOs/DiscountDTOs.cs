namespace Discount.Application.DTOs;

// ============================================
// REQUEST DTOs
// ============================================

public record CreateDiscountRequest(
    string Code,
    string Name,
    string? Description,
    string Type,
    decimal Value,
    decimal? MaxDiscountAmount,
    decimal MinOrderAmount,
    int MinQuantity,
    int? BuyQuantity,
    int? GetQuantity,
    decimal? GetDiscountPercent,
    int? UsageLimit,
    int UsageLimitPerUser,
    DateTime StartDate,
    DateTime EndDate,
    string Scope,
    List<Guid>? ProductIds,
    List<Guid>? CategoryIds,
    List<Guid>? UserIds,
    bool IsPublic,
    bool IsStackable,
    int Priority
);

public record UpdateDiscountRequest(
    string Name,
    string? Description,
    decimal Value,
    decimal? MaxDiscountAmount,
    decimal MinOrderAmount,
    int MinQuantity,
    int? UsageLimit,
    int UsageLimitPerUser,
    DateTime StartDate,
    DateTime EndDate,
    string Scope,
    List<Guid>? ProductIds,
    List<Guid>? CategoryIds,
    List<Guid>? UserIds,
    bool IsPublic,
    bool IsStackable,
    int Priority
);

public record ValidateDiscountRequest(
    string Code,
    decimal OrderAmount,
    List<ValidateDiscountItem> Items
);

public record ValidateDiscountItem(
    Guid ProductId,
    Guid? CategoryId,
    int Quantity,
    decimal Price
);

public record ApplyDiscountRequest(
    string Code,
    Guid OrderId,
    string? OrderNumber,
    decimal OrderAmount,
    List<ValidateDiscountItem> Items
);

// ============================================
// RESPONSE DTOs
// ============================================

public record DiscountResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Type,
    decimal Value,
    decimal? MaxDiscountAmount,
    decimal MinOrderAmount,
    DateTime StartDate,
    DateTime EndDate
);

public record DiscountDetailResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Type,
    decimal Value,
    decimal? MaxDiscountAmount,
    decimal MinOrderAmount,
    int MinQuantity,
    int? BuyQuantity,
    int? GetQuantity,
    decimal? GetDiscountPercent,
    int? UsageLimit,
    int UsageLimitPerUser,
    int UsageCount,
    DateTime StartDate,
    DateTime EndDate,
    string Scope,
    bool IsActive,
    bool IsPublic,
    bool IsStackable,
    int Priority,
    List<Guid> ProductIds,
    List<Guid> CategoryIds,
    List<Guid> UserIds,
    List<DiscountUsageResponse> RecentUsages,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record DiscountUsageResponse(
    Guid Id,
    Guid UserId,
    Guid OrderId,
    string? OrderNumber,
    decimal OrderAmount,
    decimal DiscountAmount,
    DateTime CreatedAt
);

public record ValidateDiscountResponse(
    bool Valid,
    DiscountResponse? Discount,
    decimal DiscountAmount,
    string Message
);

public record ApplyDiscountResponse(
    bool Success,
    Guid? DiscountId,
    decimal DiscountAmount,
    string Message
);

public record UserDiscountResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Type,
    decimal Value,
    decimal MinOrderAmount,
    DateTime EndDate,
    int UsageRemaining
);

public record DiscountStatisticsResponse(
    int TotalUsage,
    decimal TotalDiscountAmount,
    int UniqueUsers,
    List<UsageByDateResponse> UsageByDate
);

public record UsageByDateResponse(
    DateTime Date,
    int Count,
    decimal Amount
);

