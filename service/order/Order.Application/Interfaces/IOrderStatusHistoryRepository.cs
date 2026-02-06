using Order.Domain.Entities;

namespace Order.Application.Interfaces;

public interface IOrderStatusHistoryRepository
{
    Task<List<OrderStatusHistory>> GetByOrderIdAsync(Guid orderId);
    Task AddAsync(OrderStatusHistory history);
    Task SaveChangesAsync();
}

