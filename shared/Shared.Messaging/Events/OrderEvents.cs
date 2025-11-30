namespace Shared.Messaging.Events;

/// <summary>
/// Event published when an order is confirmed (payment successful)
/// Consumed by: Inventory (commit stock), Discount (record usage), Notification (send email), Product (update sold count)
/// </summary>
public record OrderConfirmedEvent
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string? UserName { get; init; }
    public string? UserEmail { get; init; }
    public string? UserPhone { get; init; }
    
    /// <summary>
    /// Order items for inventory commit and product sold count update
    /// </summary>
    public List<OrderConfirmedItem> Items { get; init; } = new();
    
    /// <summary>
    /// Discount info for recording usage
    /// </summary>
    public Guid? DiscountId { get; init; }
    public string? DiscountCode { get; init; }
    public decimal DiscountAmount { get; init; }
    
    /// <summary>
    /// Order totals
    /// </summary>
    public decimal SubTotal { get; init; }
    public decimal ShippingFee { get; init; }
    public decimal TotalAmount { get; init; }
    
    public DateTime ConfirmedAt { get; init; } = DateTime.UtcNow;
}

public record OrderConfirmedItem
{
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public string? ProductName { get; init; }
    public string? Sku { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public Guid SellerId { get; init; }
    public string? SellerName { get; init; }
    
    /// <summary>
    /// Reservation ID from inventory service (for commit)
    /// </summary>
    public Guid? ReservationId { get; init; }
    
    /// <summary>
    /// Warehouse ID where stock was reserved
    /// </summary>
    public Guid? WarehouseId { get; init; }
}

/// <summary>
/// Event published when an order is cancelled
/// Consumed by: Inventory (release stock), Discount (rollback usage), Notification (send email), Payment (process refund)
/// </summary>
public record OrderCancelledEvent
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string? UserName { get; init; }
    public string? UserEmail { get; init; }
    
    /// <summary>
    /// Cancellation reason
    /// </summary>
    public string? Reason { get; init; }
    public string CancelledBy { get; init; } = "Customer"; // Customer, Admin, System
    
    /// <summary>
    /// Order items for inventory release
    /// </summary>
    public List<OrderCancelledItem> Items { get; init; } = new();
    
    /// <summary>
    /// Discount info for rollback usage
    /// </summary>
    public Guid? DiscountId { get; init; }
    
    /// <summary>
    /// Payment info for refund processing
    /// </summary>
    public Guid? PaymentTransactionId { get; init; }
    public decimal RefundAmount { get; init; }
    public bool RequiresRefund { get; init; }
    
    public DateTime CancelledAt { get; init; } = DateTime.UtcNow;
}

public record OrderCancelledItem
{
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public int Quantity { get; init; }
    
    /// <summary>
    /// Reservation ID to release (if payment not yet confirmed)
    /// </summary>
    public Guid? ReservationId { get; init; }
    
    /// <summary>
    /// Warehouse ID (for stock return if already committed)
    /// </summary>
    public Guid? WarehouseId { get; init; }
    
    /// <summary>
    /// Whether stock was already committed (deducted)
    /// If true: need to return stock
    /// If false: need to release reservation
    /// </summary>
    public bool StockCommitted { get; init; }
}

/// <summary>
/// Event published when an order is created (before payment)
/// Used for analytics and tracking
/// </summary>
public record OrderCreatedEvent
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public int ItemCount { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

