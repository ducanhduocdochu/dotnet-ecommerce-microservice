using Microsoft.EntityFrameworkCore;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.DB;

namespace Inventory.Infrastructure.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly InventoryDbContext _context;

    public InventoryRepository(InventoryDbContext context) => _context = context;

    public async Task<List<InventoryItem>> GetAllAsync(Guid? warehouseId, bool? lowStock, int page, int pageSize)
    {
        var query = _context.Inventory.Include(i => i.Warehouse).AsQueryable();
        if (warehouseId.HasValue)
            query = query.Where(i => i.WarehouseId == warehouseId.Value);
        if (lowStock == true)
            query = query.Where(i => (i.Quantity - i.ReservedQuantity) <= i.LowStockThreshold);
        return await query.OrderBy(i => i.Sku)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(Guid? warehouseId, bool? lowStock)
    {
        var query = _context.Inventory.AsQueryable();
        if (warehouseId.HasValue)
            query = query.Where(i => i.WarehouseId == warehouseId.Value);
        if (lowStock == true)
            query = query.Where(i => (i.Quantity - i.ReservedQuantity) <= i.LowStockThreshold);
        return await query.CountAsync();
    }

    public async Task<InventoryItem?> GetByIdAsync(Guid id) =>
        await _context.Inventory.Include(i => i.Warehouse).FirstOrDefaultAsync(i => i.Id == id);

    public async Task<InventoryItem?> GetByIdWithDetailsAsync(Guid id) =>
        await _context.Inventory
            .Include(i => i.Warehouse)
            .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<InventoryItem?> GetByProductAndWarehouseAsync(Guid productId, Guid? variantId, Guid warehouseId)
    {
        if (variantId.HasValue)
            return await _context.Inventory.Include(i => i.Warehouse)
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.VariantId == variantId && i.WarehouseId == warehouseId);
        return await _context.Inventory.Include(i => i.Warehouse)
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.VariantId == null && i.WarehouseId == warehouseId);
    }

    public async Task<List<InventoryItem>> GetByProductIdAsync(Guid productId, Guid? variantId = null)
    {
        var query = _context.Inventory.Include(i => i.Warehouse).Where(i => i.ProductId == productId);
        if (variantId.HasValue)
            query = query.Where(i => i.VariantId == variantId);
        return await query.ToListAsync();
    }

    public async Task<List<InventoryItem>> GetLowStockAsync(Guid? warehouseId = null)
    {
        var query = _context.Inventory.Include(i => i.Warehouse)
            .Where(i => (i.Quantity - i.ReservedQuantity) <= i.LowStockThreshold && i.IsActive);
        if (warehouseId.HasValue)
            query = query.Where(i => i.WarehouseId == warehouseId.Value);
        return await query.ToListAsync();
    }

    public async Task AddAsync(InventoryItem item) => await _context.Inventory.AddAsync(item);
    public Task UpdateAsync(InventoryItem item) { _context.Inventory.Update(item); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}

