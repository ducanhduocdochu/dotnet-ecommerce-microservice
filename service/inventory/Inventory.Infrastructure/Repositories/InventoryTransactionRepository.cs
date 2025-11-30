using Microsoft.EntityFrameworkCore;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.DB;

namespace Inventory.Infrastructure.Repositories;

public class InventoryTransactionRepository : IInventoryTransactionRepository
{
    private readonly InventoryDbContext _context;

    public InventoryTransactionRepository(InventoryDbContext context) => _context = context;

    public async Task<List<InventoryTransaction>> GetByInventoryIdAsync(Guid inventoryId, int page, int pageSize) =>
        await _context.InventoryTransactions
            .Where(t => t.InventoryId == inventoryId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

    public async Task<List<InventoryTransaction>> GetByReferenceAsync(string referenceType, Guid referenceId) =>
        await _context.InventoryTransactions
            .Where(t => t.ReferenceType == referenceType && t.ReferenceId == referenceId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public async Task<int> GetCountByInventoryIdAsync(Guid inventoryId) =>
        await _context.InventoryTransactions.CountAsync(t => t.InventoryId == inventoryId);

    public async Task AddAsync(InventoryTransaction transaction) => await _context.InventoryTransactions.AddAsync(transaction);
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}

