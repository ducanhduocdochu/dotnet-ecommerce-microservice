namespace Product.Application.DTOs;

// Requests
public record CreateBrandRequest(
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    string? WebsiteUrl
);

public record UpdateBrandRequest(
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    string? WebsiteUrl,
    bool IsActive
);

// Responses
public record BrandResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    string? WebsiteUrl,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

