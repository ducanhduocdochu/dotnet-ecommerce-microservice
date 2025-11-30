namespace Discount.Application.DTOs;

// ============================================
// REQUEST DTOs
// ============================================

public record CreateFlashSaleRequest(
    string Name,
    DateTime StartTime,
    DateTime EndTime,
    List<CreateFlashSaleItemRequest> Items
);

public record CreateFlashSaleItemRequest(
    Guid ProductId,
    Guid? VariantId,
    decimal OriginalPrice,
    decimal SalePrice,
    int QuantityLimit,
    int LimitPerUser
);

public record UpdateFlashSaleRequest(
    string Name,
    DateTime StartTime,
    DateTime EndTime,
    bool IsActive
);

public record UpdateFlashSaleItemRequest(
    decimal SalePrice,
    int QuantityLimit,
    int LimitPerUser
);

// ============================================
// RESPONSE DTOs
// ============================================

public record FlashSaleResponse(
    Guid Id,
    string Name,
    DateTime StartTime,
    DateTime EndTime,
    bool IsActive,
    List<FlashSaleItemResponse> Items
);

public record FlashSaleItemResponse(
    Guid Id,
    Guid ProductId,
    Guid? VariantId,
    decimal OriginalPrice,
    decimal SalePrice,
    decimal? DiscountPercent,
    int QuantityLimit,
    int QuantitySold,
    int QuantityRemaining,
    int LimitPerUser
);

public record FlashSaleAvailabilityResponse(
    bool Available,
    int QuantityRemaining,
    decimal SalePrice,
    int LimitPerUser
);

