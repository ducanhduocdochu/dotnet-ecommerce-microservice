using Microsoft.EntityFrameworkCore;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.DB;

namespace Inventory.Infrastructure.Repositories;

public class StockReservationRepository : IStockReservationRepository
{
    private readonly InventoryDbContext _context;

    public StockReservationRepository(InventoryDbContext context) => _context = context;

    public async Task<List<StockReservation>> GetByOrderIdAsync(Guid orderId) =>
        await _context.StockReservations.Where(r => r.OrderId == orderId).ToListAsync();

    public async Task<List<StockReservation>> GetByInventoryIdAsync(Guid inventoryId) =>
        await _context.StockReservations.Where(r => r.InventoryId == inventoryId).ToListAsync();

    public async Task<List<StockReservation>> GetExpiredAsync() =>
        await _context.StockReservations
            .Where(r => r.Status == "RESERVED" && r.ExpiredAt != null && r.ExpiredAt < DateTime.UtcNow)
            .ToListAsync();

    public async Task<StockReservation?> GetByIdAsync(Guid id) =>
        await _context.StockReservations.FirstOrDefaultAsync(r => r.Id == id);

    public async Task AddAsync(StockReservation reservation) => await _context.StockReservations.AddAsync(reservation);
    public async Task AddRangeAsync(List<StockReservation> reservations) => await _context.StockReservations.AddRangeAsync(reservations);
    public Task UpdateAsync(StockReservation reservation) { _context.StockReservations.Update(reservation); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}

