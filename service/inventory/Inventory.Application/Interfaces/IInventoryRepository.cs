using Inventory.Domain.Entities;

namespace Inventory.Application.Interfaces;

public interface IInventoryRepository
{
    Task<List<InventoryItem>> GetAllAsync(Guid? warehouseId, bool? lowStock, int page, int pageSize);
    Task<int> GetCountAsync(Guid? warehouseId, bool? lowStock);
    Task<InventoryItem?> GetByIdAsync(Guid id);
    Task<InventoryItem?> GetByIdWithDetailsAsync(Guid id);
    Task<InventoryItem?> GetByProductAndWarehouseAsync(Guid productId, Guid? variantId, Guid warehouseId);
    Task<List<InventoryItem>> GetByProductIdAsync(Guid productId, Guid? variantId = null);
    Task<List<InventoryItem>> GetLowStockAsync(Guid? warehouseId = null);
    Task AddAsync(InventoryItem item);
    Task UpdateAsync(InventoryItem item);
    Task SaveChangesAsync();
}

