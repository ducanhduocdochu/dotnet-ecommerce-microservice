namespace Inventory.Domain.Entities;

public class StockReservation
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid InventoryId { get; private set; }
    
    // Order Reference
    public Guid OrderId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public string? OrderNumber { get; private set; }
    
    // Quantity
    public int Quantity { get; private set; }
    
    // Status: RESERVED, COMMITTED, RELEASED, EXPIRED
    public string Status { get; private set; } = "RESERVED";
    
    // Expiration
    public DateTime? ExpiredAt { get; private set; }
    
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? CommittedAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }

    // Navigation
    public InventoryItem? Inventory { get; set; }

    private StockReservation() { }

    public StockReservation(Guid inventoryId, Guid orderId, Guid orderItemId, int quantity, string? orderNumber = null, int expirationMinutes = 30)
    {
        InventoryId = inventoryId;
        OrderId = orderId;
        OrderItemId = orderItemId;
        Quantity = quantity;
        OrderNumber = orderNumber;
        ExpiredAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
    }

    public bool IsExpired => Status == "RESERVED" && ExpiredAt.HasValue && ExpiredAt.Value < DateTime.UtcNow;

    public void Commit()
    {
        Status = "COMMITTED";
        CommittedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Release()
    {
        Status = "RELEASED";
        ReleasedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        Status = "EXPIRED";
        ReleasedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

