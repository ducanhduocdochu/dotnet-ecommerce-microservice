namespace Product.Application.DTOs;

// Requests
public record CreateCategoryRequest(
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    Guid? ParentId,
    int SortOrder = 0
);

public record UpdateCategoryRequest(
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    Guid? ParentId,
    int SortOrder,
    bool IsActive
);

// Responses
public record CategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    Guid? ParentId,
    bool IsActive,
    int SortOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CategoryTreeResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    Guid? ParentId,
    int SortOrder,
    List<CategoryTreeResponse> Children
);

