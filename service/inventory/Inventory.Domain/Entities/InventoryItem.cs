namespace Inventory.Domain.Entities;

public class InventoryItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    
    // Product Reference
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public string? Sku { get; private set; }
    
    // Warehouse
    public Guid WarehouseId { get; private set; }
    
    // Stock Levels
    public int Quantity { get; private set; } = 0;
    public int ReservedQuantity { get; private set; } = 0;
    public int AvailableQuantity => Quantity - ReservedQuantity;
    
    // Thresholds
    public int LowStockThreshold { get; private set; } = 10;
    public int ReorderPoint { get; private set; } = 20;
    public int ReorderQuantity { get; private set; } = 100;
    
    // Location in warehouse
    public string? Location { get; private set; }
    
    // Status
    public bool IsActive { get; private set; } = true;
    
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public Warehouse? Warehouse { get; set; }
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<StockReservation> Reservations { get; set; } = new List<StockReservation>();

    private InventoryItem() { }

    public InventoryItem(Guid productId, Guid warehouseId, int quantity = 0, Guid? variantId = null, string? sku = null, string? location = null)
    {
        ProductId = productId;
        VariantId = variantId;
        Sku = sku;
        WarehouseId = warehouseId;
        Quantity = quantity;
        Location = location;
    }

    public bool IsLowStock => AvailableQuantity <= LowStockThreshold;
    public bool IsOutOfStock => AvailableQuantity <= 0;

    public void SetQuantity(int quantity, string? reason = null)
    {
        Quantity = quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddQuantity(int amount)
    {
        Quantity += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SubtractQuantity(int amount)
    {
        if (Quantity < amount)
            throw new InvalidOperationException("Insufficient stock");
        Quantity -= amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reserve(int amount)
    {
        if (AvailableQuantity < amount)
            throw new InvalidOperationException("Insufficient available stock");
        ReservedQuantity += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Release(int amount)
    {
        ReservedQuantity = Math.Max(0, ReservedQuantity - amount);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Commit(int amount)
    {
        // Stock was already reserved, now it's committed (shipped)
        // Optionally: Quantity -= amount; ReservedQuantity -= amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deduct(int amount)
    {
        if (Quantity < amount)
            throw new InvalidOperationException("Insufficient stock");
        Quantity -= amount;
        ReservedQuantity = Math.Max(0, ReservedQuantity - amount);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateThresholds(int? lowStockThreshold, int? reorderPoint, int? reorderQuantity)
    {
        if (lowStockThreshold.HasValue) LowStockThreshold = lowStockThreshold.Value;
        if (reorderPoint.HasValue) ReorderPoint = reorderPoint.Value;
        if (reorderQuantity.HasValue) ReorderQuantity = reorderQuantity.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLocation(string? location)
    {
        Location = location;
        UpdatedAt = DateTime.UtcNow;
    }
}

