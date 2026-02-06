using Order.Domain.Entities;

namespace Order.Application.Interfaces;

public interface IOrderItemRepository
{
    Task<List<OrderItem>> GetByOrderIdAsync(Guid orderId);
    Task<List<OrderItem>> GetBySellerIdAsync(Guid sellerId);
    Task<OrderItem?> GetByIdAsync(Guid id);
    Task AddAsync(OrderItem item);
    Task AddRangeAsync(List<OrderItem> items);
    Task UpdateAsync(OrderItem item);
    Task SaveChangesAsync();
}

