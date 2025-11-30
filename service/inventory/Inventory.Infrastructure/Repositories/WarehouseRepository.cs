using Microsoft.EntityFrameworkCore;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.DB;

namespace Inventory.Infrastructure.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly InventoryDbContext _context;

    public WarehouseRepository(InventoryDbContext context) => _context = context;

    public async Task<List<Warehouse>> GetAllAsync() =>
        await _context.Warehouses.OrderBy(w => w.Name).ToListAsync();

    public async Task<Warehouse?> GetByIdAsync(Guid id) =>
        await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == id);

    public async Task<Warehouse?> GetByCodeAsync(string code) =>
        await _context.Warehouses.FirstOrDefaultAsync(w => w.Code == code);

    public async Task<Warehouse?> GetDefaultAsync() =>
        await _context.Warehouses.FirstOrDefaultAsync(w => w.IsDefault && w.IsActive);

    public async Task AddAsync(Warehouse warehouse) => await _context.Warehouses.AddAsync(warehouse);
    public Task UpdateAsync(Warehouse warehouse) { _context.Warehouses.Update(warehouse); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}

