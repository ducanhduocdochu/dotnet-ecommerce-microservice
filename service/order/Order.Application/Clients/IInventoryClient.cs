namespace Order.Application.Clients;

/// <summary>
/// HTTP client for Inventory Service
/// </summary>
public interface IInventoryClient
{
    /// <summary>
    /// Check stock availability for multiple products
    /// </summary>
    Task<StockCheckResult> CheckStockAsync(CheckStockRequest request);
    
    /// <summary>
    /// Reserve stock for checkout (temporary hold)
    /// </summary>
    Task<StockReserveResult> ReserveAsync(ReserveStockRequest request);
    
    /// <summary>
    /// Release reservation (when payment fails/timeout/cancel before payment)
    /// </summary>
    Task<bool> ReleaseReservationAsync(ReleaseReservationRequest request);
}

// Request/Response models
public record CheckStockRequest(
    List<CheckStockItem> Items
);

public record CheckStockItem(
    Guid ProductId,
    Guid? VariantId,
    int Quantity,
    Guid? WarehouseId = null
);

public record StockCheckResult(
    bool AllAvailable,
    List<StockCheckItemResult> Items,
    string? Message
);

public record StockCheckItemResult(
    Guid ProductId,
    Guid? VariantId,
    int RequestedQuantity,
    int AvailableQuantity,
    bool IsAvailable,
    Guid? WarehouseId
);

public record ReserveStockRequest(
    Guid OrderId,
    string? OrderNumber,
    Guid UserId,
    List<ReserveStockItem> Items,
    int ExpirationMinutes = 15
);

public record ReserveStockItem(
    Guid ProductId,
    Guid? VariantId,
    int Quantity,
    Guid? WarehouseId = null
);

public record StockReserveResult(
    bool Success,
    List<ReservationInfo> Reservations,
    string? Message
);

public record ReservationInfo(
    Guid ReservationId,
    Guid ProductId,
    Guid? VariantId,
    int Quantity,
    Guid WarehouseId,
    DateTime ExpiresAt
);

public record ReleaseReservationRequest(
    Guid OrderId,
    List<Guid>? ReservationIds = null
);

