using Inventory.Domain.Entities;

namespace Inventory.Application.Interfaces;

public interface IInventoryTransactionRepository
{
    Task<List<InventoryTransaction>> GetByInventoryIdAsync(Guid inventoryId, int page, int pageSize);
    Task<List<InventoryTransaction>> GetByReferenceAsync(string referenceType, Guid referenceId);
    Task<int> GetCountByInventoryIdAsync(Guid inventoryId);
    Task AddAsync(InventoryTransaction transaction);
    Task SaveChangesAsync();
}

