namespace Discount.Application.DTOs;

// ============================================
// REQUEST DTOs
// ============================================

public record CreatePromotionRequest(
    string Code,
    string Name,
    string? Description,
    string? BannerUrl,
    string? ThumbnailUrl,
    DateTime StartDate,
    DateTime EndDate,
    bool IsFeatured,
    int DisplayOrder,
    List<Guid>? DiscountIds
);

public record UpdatePromotionRequest(
    string Name,
    string? Description,
    string? BannerUrl,
    string? ThumbnailUrl,
    DateTime StartDate,
    DateTime EndDate,
    bool IsFeatured,
    int DisplayOrder,
    List<Guid>? DiscountIds
);

// ============================================
// RESPONSE DTOs
// ============================================

public record PromotionResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? BannerUrl,
    string? ThumbnailUrl,
    DateTime StartDate,
    DateTime EndDate,
    bool IsFeatured,
    List<DiscountResponse> Discounts
);

public record PromotionDetailResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? BannerUrl,
    string? ThumbnailUrl,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive,
    bool IsFeatured,
    int DisplayOrder,
    List<DiscountResponse> Discounts,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

