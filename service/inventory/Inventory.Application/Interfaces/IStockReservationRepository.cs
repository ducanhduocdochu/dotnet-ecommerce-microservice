using Inventory.Domain.Entities;

namespace Inventory.Application.Interfaces;

public interface IStockReservationRepository
{
    Task<List<StockReservation>> GetByOrderIdAsync(Guid orderId);
    Task<List<StockReservation>> GetByInventoryIdAsync(Guid inventoryId);
    Task<List<StockReservation>> GetExpiredAsync();
    Task<StockReservation?> GetByIdAsync(Guid id);
    Task AddAsync(StockReservation reservation);
    Task AddRangeAsync(List<StockReservation> reservations);
    Task UpdateAsync(StockReservation reservation);
    Task SaveChangesAsync();
}

