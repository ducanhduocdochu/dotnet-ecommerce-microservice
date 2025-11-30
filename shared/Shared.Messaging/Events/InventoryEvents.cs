namespace Shared.Messaging.Events;

/// <summary>
/// Event published when stock is reserved for an order
/// Used for tracking and analytics
/// </summary>
public record StockReservedEvent
{
    public Guid ReservationId { get; init; }
    public Guid OrderId { get; init; }
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public Guid WarehouseId { get; init; }
    public int Quantity { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime ReservedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when reserved stock is committed (deducted from inventory)
/// </summary>
public record StockCommittedEvent
{
    public Guid ReservationId { get; init; }
    public Guid OrderId { get; init; }
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public Guid WarehouseId { get; init; }
    public int Quantity { get; init; }
    public int RemainingStock { get; init; }
    public DateTime CommittedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when stock reservation is released (payment failed/timeout/cancelled)
/// </summary>
public record StockReleasedEvent
{
    public Guid ReservationId { get; init; }
    public Guid OrderId { get; init; }
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public Guid WarehouseId { get; init; }
    public int Quantity { get; init; }
    public string ReleaseReason { get; init; } = string.Empty; // PAYMENT_FAILED, TIMEOUT, ORDER_CANCELLED
    public DateTime ReleasedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when stock falls below threshold
/// Consumed by: Notification (alert admin/seller)
/// </summary>
public record StockLowEvent
{
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public string? ProductName { get; init; }
    public string? Sku { get; init; }
    public Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public int CurrentStock { get; init; }
    public int LowStockThreshold { get; init; }
    public Guid? SellerId { get; init; }
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
}

