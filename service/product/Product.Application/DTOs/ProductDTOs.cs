namespace Product.Application.DTOs;

// Requests
public record CreateProductRequest(
    string Name,
    string Slug,
    decimal BasePrice,
    string? SellerName = null,          // Denormalized seller info
    string? SellerAvatar = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    string? Description = null,
    string? ShortDescription = null,
    string? Sku = null,
    decimal? SalePrice = null,
    decimal? CostPrice = null,
    int Quantity = 0,
    decimal? Weight = null,
    decimal? Length = null,
    decimal? Width = null,
    decimal? Height = null,
    bool IsDigital = false,
    List<CreateProductImageRequest>? Images = null,
    List<CreateProductVariantRequest>? Variants = null,
    List<CreateProductAttributeRequest>? Attributes = null,
    List<string>? Tags = null
);

public record UpdateProductRequest(
    string Name,
    string Slug,
    decimal BasePrice,
    Guid? CategoryId,
    Guid? BrandId,
    string? Description,
    string? ShortDescription,
    string? Sku,
    decimal? SalePrice,
    decimal? CostPrice,
    int Quantity,
    decimal? Weight,
    decimal? Length,
    decimal? Width,
    decimal? Height,
    bool IsDigital
);

public record CreateProductImageRequest(
    string ImageUrl,
    string? AltText,
    bool IsPrimary = false,
    int SortOrder = 0
);

public record CreateProductVariantRequest(
    string Name,
    string? Sku,
    decimal? Price,
    int Quantity,
    string? ImageUrl,
    List<VariantOptionRequest>? Options
);

public record UpdateProductVariantRequest(
    string Name,
    string? Sku,
    decimal? Price,
    int Quantity,
    string? ImageUrl,
    bool IsActive
);

public record VariantOptionRequest(
    string OptionName,
    string OptionValue
);

public record CreateProductAttributeRequest(
    string AttributeName,
    string AttributeValue,
    int SortOrder = 0
);

// Responses
public record ProductListResponse(
    Guid Id,
    Guid SellerId,
    string? SellerName,
    string? SellerAvatar,
    string Name,
    string Slug,
    string? ShortDescription,
    decimal BasePrice,
    decimal? SalePrice,
    string? PrimaryImage,
    decimal RatingAverage,
    int SoldCount,
    string Status
);

public record ProductDetailResponse(
    Guid Id,
    Guid SellerId,
    string? SellerName,
    string? SellerAvatar,
    string Name,
    string Slug,
    string? Description,
    string? ShortDescription,
    decimal BasePrice,
    decimal? SalePrice,
    string Currency,
    int Quantity,
    string? Sku,
    decimal? Weight,
    bool IsDigital,
    bool IsFeatured,
    string Status,
    decimal RatingAverage,
    int RatingCount,
    int SoldCount,
    int ViewCount,
    CategoryResponse? Category,
    BrandResponse? Brand,
    List<ProductImageResponse> Images,
    List<ProductVariantResponse> Variants,
    List<ProductAttributeResponse> Attributes,
    List<string> Tags,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record ProductImageResponse(
    Guid Id,
    string ImageUrl,
    string? AltText,
    bool IsPrimary,
    int SortOrder
);

public record ProductVariantResponse(
    Guid Id,
    string Name,
    string? Sku,
    decimal? Price,
    int Quantity,
    string? ImageUrl,
    bool IsActive,
    List<VariantOptionResponse> Options
);

public record VariantOptionResponse(
    string OptionName,
    string OptionValue
);

public record ProductAttributeResponse(
    Guid Id,
    string AttributeName,
    string AttributeValue,
    int SortOrder
);

