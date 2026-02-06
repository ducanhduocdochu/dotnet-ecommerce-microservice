namespace Inventory.Domain.Entities;

public class InventoryTransaction
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid InventoryId { get; private set; }
    
    // Transaction Type: IMPORT, EXPORT, RESERVE, RELEASE, ADJUST, TRANSFER, RETURN
    public string Type { get; private set; } = null!;
    
    // Quantity Change
    public int QuantityChange { get; private set; }
    public int QuantityBefore { get; private set; }
    public int QuantityAfter { get; private set; }
    
    // Reference
    public string? ReferenceType { get; private set; } // ORDER, PURCHASE_ORDER, ADJUSTMENT, TRANSFER
    public Guid? ReferenceId { get; private set; }
    public string? ReferenceCode { get; private set; }
    
    // For transfers
    public Guid? FromWarehouseId { get; private set; }
    public Guid? ToWarehouseId { get; private set; }
    
    // Reason & Notes
    public string? Reason { get; private set; }
    public string? Note { get; private set; }
    
    // Who
    public Guid? CreatedBy { get; private set; }
    public string? CreatedByName { get; private set; }
    
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public InventoryItem? Inventory { get; set; }

    private InventoryTransaction() { }

    public InventoryTransaction(
        Guid inventoryId,
        string type,
        int quantityChange,
        int quantityBefore,
        int quantityAfter,
        string? referenceType = null,
        Guid? referenceId = null,
        string? referenceCode = null,
        string? reason = null,
        string? note = null,
        Guid? createdBy = null,
        string? createdByName = null)
    {
        InventoryId = inventoryId;
        Type = type;
        QuantityChange = quantityChange;
        QuantityBefore = quantityBefore;
        QuantityAfter = quantityAfter;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        ReferenceCode = referenceCode;
        Reason = reason;
        Note = note;
        CreatedBy = createdBy;
        CreatedByName = createdByName;
    }

    public void SetTransferWarehouses(Guid? fromWarehouseId, Guid? toWarehouseId)
    {
        FromWarehouseId = fromWarehouseId;
        ToWarehouseId = toWarehouseId;
    }
}

