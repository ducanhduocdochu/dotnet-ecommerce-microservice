namespace Order.Application.DTOs;

// Requests
public record CreateOrderRequest(
    List<CreateOrderItemRequest> Items,
    ShippingAddressRequest Shipping,
    string? ShippingMethod = "STANDARD",
    string? PaymentMethod = "COD",
    string? DiscountCode = null,
    string? CustomerNote = null,
    string? UserName = null,
    string? UserEmail = null,
    string? UserPhone = null
);

public record CreateOrderItemRequest(
    Guid ProductId,
    string ProductName,
    Guid SellerId,
    decimal UnitPrice,
    int Quantity,
    string? ProductSlug = null,
    string? ProductImage = null,
    string? ProductSku = null,
    Guid? VariantId = null,
    string? VariantName = null,
    string? VariantOptions = null,
    string? SellerName = null,
    decimal? SalePrice = null
);

public record ShippingAddressRequest(
    string Name,
    string Phone,
    string Address,
    string? Ward = null,
    string? District = null,
    string? City = null,
    string? Country = "Vietnam",
    string? PostalCode = null
);

public record CancelOrderRequest(string Reason);

public record ShipOrderRequest(
    string ShippingCarrier,
    string TrackingNumber,
    DateTime? EstimatedDelivery = null
);

public record UpdateOrderStatusRequest(
    string Status,
    string? Note = null
);

public record UpdatePaymentStatusRequest(
    string PaymentStatus,
    string? TransactionId = null,
    string? Note = null
);

// Responses
public record OrderListResponse(
    Guid Id,
    string OrderNumber,
    string Status,
    string PaymentStatus,
    decimal TotalAmount,
    int TotalItems,
    DateTime CreatedAt
);

public record OrderDetailResponse(
    Guid Id,
    string OrderNumber,
    string Status,
    string PaymentStatus,
    Guid UserId,
    string? UserName,
    string? UserEmail,
    string? UserPhone,
    decimal Subtotal,
    decimal ShippingFee,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string? DiscountCode,
    List<OrderItemResponse> Items,
    ShippingInfoResponse Shipping,
    string? ShippingMethod,
    string? ShippingCarrier,
    string? TrackingNumber,
    DateTime? EstimatedDelivery,
    List<OrderStatusHistoryResponse> StatusHistory,
    string? CustomerNote,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    DateTime? ShippedAt,
    DateTime? DeliveredAt
);

public record OrderItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ProductImage,
    string? VariantName,
    decimal UnitPrice,
    decimal? SalePrice,
    int Quantity,
    decimal TotalPrice,
    string Status
);

public record ShippingInfoResponse(
    string Name,
    string Phone,
    string Address,
    string? Ward,
    string? District,
    string? City,
    string? Country,
    string? PostalCode
);

public record OrderStatusHistoryResponse(
    string Status,
    string? PreviousStatus,
    string? Note,
    string? ChangedByName,
    DateTime CreatedAt
);

public record OrderCreatedResponse(
    Guid Id,
    string OrderNumber,
    string Status,
    string PaymentStatus,
    decimal Subtotal,
    decimal ShippingFee,
    decimal DiscountAmount,
    decimal TotalAmount,
    List<OrderItemResponse> Items,
    ShippingInfoResponse Shipping,
    string? PaymentUrl
);

// Checkout DTOs
public record CheckoutRequest(
    List<CheckoutItemRequest> Items,
    ShippingAddressRequest Shipping,
    string? ShippingMethod = "STANDARD",
    string? PaymentMethod = "BANK_TRANSFER",
    string? PaymentGateway = "VNPAY",
    string? DiscountCode = null,
    string? CustomerNote = null,
    string? UserName = null,
    string? UserEmail = null,
    string? UserPhone = null,
    string? ReturnUrl = null,
    string? CancelUrl = null
);

public record CheckoutItemRequest(
    Guid ProductId,
    string ProductName,
    Guid SellerId,
    decimal UnitPrice,
    int Quantity,
    Guid? CategoryId = null,
    string? ProductSlug = null,
    string? ProductImage = null,
    string? ProductSku = null,
    Guid? VariantId = null,
    string? VariantName = null,
    string? VariantOptions = null,
    string? SellerName = null,
    decimal? SalePrice = null
);

public record CheckoutResult(
    bool Success,
    OrderCreatedResponse? Order,
    string? PaymentUrl,
    string Message
);

// Payment callback DTOs
public record PaymentCallbackRequest(
    Guid OrderId,
    Guid TransactionId,
    bool Success,
    string? ErrorCode = null,
    string? ErrorMessage = null
);

