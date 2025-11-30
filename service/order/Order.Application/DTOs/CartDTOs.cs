namespace Order.Application.DTOs;

// Requests
public record AddToCartRequest(
    Guid ProductId,
    string ProductName,
    decimal ProductPrice,
    Guid SellerId,
    int Quantity = 1,
    string? ProductImage = null,
    Guid? VariantId = null,
    string? VariantName = null,
    decimal? VariantPrice = null,
    string? SellerName = null
);

public record UpdateCartItemRequest(int Quantity);

// Responses
public record CartItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ProductImage,
    decimal ProductPrice,
    Guid? VariantId,
    string? VariantName,
    decimal? VariantPrice,
    Guid SellerId,
    string? SellerName,
    int Quantity,
    decimal TotalPrice
);

public record CartResponse(
    List<CartItemResponse> Items,
    int TotalItems,
    decimal Subtotal
);

