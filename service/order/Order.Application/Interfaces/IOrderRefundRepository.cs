using Order.Domain.Entities;

namespace Order.Application.Interfaces;

public interface IOrderRefundRepository
{
    Task<List<OrderRefund>> GetAllAsync(int page, int pageSize, string? status = null);
    Task<int> GetCountAsync(string? status = null);
    Task<List<OrderRefund>> GetByOrderIdAsync(Guid orderId);
    Task<OrderRefund?> GetByIdAsync(Guid id);
    Task AddAsync(OrderRefund refund);
    Task UpdateAsync(OrderRefund refund);
    Task SaveChangesAsync();
}

