namespace Inventory.Application.DTOs;

// Requests
public record CreateInventoryRequest(
    Guid ProductId,
    Guid WarehouseId,
    int Quantity = 0,
    Guid? VariantId = null,
    string? Sku = null,
    int LowStockThreshold = 10,
    string? Location = null
);

public record UpdateInventoryRequest(
    int? Quantity = null,
    int? LowStockThreshold = null,
    int? ReorderPoint = null,
    int? ReorderQuantity = null,
    string? Location = null,
    string? Reason = null
);

public record AdjustInventoryRequest(
    int Adjustment,
    string Reason
);

public record CheckStockRequest(
    List<CheckStockItem> Items
);

public record CheckStockItem(
    Guid ProductId,
    Guid? VariantId,
    int Quantity
);

public record ReserveStockRequest(
    Guid OrderId,
    string? OrderNumber,
    List<ReserveStockItem> Items,
    int ExpirationMinutes = 30
);

public record ReserveStockItem(
    Guid ProductId,
    Guid? VariantId,
    Guid OrderItemId,
    int Quantity
);

public record ReleaseStockRequest(
    Guid OrderId,
    string? Reason = null
);

public record DeductStockRequest(
    Guid OrderId,
    string? OrderNumber,
    List<DeductStockItem> Items
);

public record DeductStockItem(
    Guid ProductId,
    Guid? VariantId,
    int Quantity
);

public record ReturnStockRequest(
    Guid OrderId,
    string? OrderNumber,
    List<ReturnStockItem> Items
);

public record ReturnStockItem(
    Guid ProductId,
    Guid? VariantId,
    int Quantity,
    string? Reason = null
);

public record TransferStockRequest(
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    List<TransferStockItem> Items,
    string? Note = null
);

public record TransferStockItem(
    Guid ProductId,
    Guid? VariantId,
    int Quantity
);

// Responses
public record InventoryResponse(
    Guid Id,
    Guid ProductId,
    Guid? VariantId,
    string? Sku,
    Guid WarehouseId,
    string? WarehouseName,
    int Quantity,
    int ReservedQuantity,
    int AvailableQuantity,
    int LowStockThreshold,
    bool IsLowStock,
    string? Location,
    DateTime UpdatedAt
);

public record InventoryDetailResponse(
    Guid Id,
    Guid ProductId,
    Guid? VariantId,
    string? Sku,
    WarehouseResponse Warehouse,
    int Quantity,
    int ReservedQuantity,
    int AvailableQuantity,
    int LowStockThreshold,
    int ReorderPoint,
    int ReorderQuantity,
    bool IsLowStock,
    string? Location,
    List<TransactionResponse> RecentTransactions
);

public record StockCheckResponse(
    bool Available,
    List<StockCheckItemResponse> Items
);

public record StockCheckItemResponse(
    Guid ProductId,
    Guid? VariantId,
    int Requested,
    int Available,
    bool IsAvailable
);

public record ProductStockResponse(
    Guid ProductId,
    Guid? VariantId,
    int TotalQuantity,
    int TotalReserved,
    int TotalAvailable,
    List<WarehouseStockResponse> Warehouses
);

public record WarehouseStockResponse(
    Guid WarehouseId,
    string WarehouseName,
    int Quantity,
    int Available
);

public record ReserveStockResponse(
    bool Success,
    List<Guid> ReservationIds,
    DateTime ExpiresAt
);

public record LowStockAlertResponse(
    Guid ProductId,
    string? Sku,
    string? WarehouseName,
    int AvailableQuantity,
    int LowStockThreshold,
    int ReorderPoint,
    int SuggestedReorder
);

