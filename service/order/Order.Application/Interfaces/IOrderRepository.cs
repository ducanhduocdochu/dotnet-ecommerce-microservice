using Order.Domain.Entities;

namespace Order.Application.Interfaces;

public interface IOrderRepository
{
    Task<List<OrderEntity>> GetByUserIdAsync(Guid userId, int page, int pageSize, string? status = null);
    Task<int> GetCountByUserIdAsync(Guid userId, string? status = null);
    Task<List<OrderEntity>> GetBySellerIdAsync(Guid sellerId, int page, int pageSize, string? status = null);
    Task<int> GetCountBySellerIdAsync(Guid sellerId, string? status = null);
    Task<List<OrderEntity>> GetAllAsync(int page, int pageSize, string? status = null, Guid? userId = null, DateTime? from = null, DateTime? to = null);
    Task<int> GetCountAsync(string? status = null, Guid? userId = null, DateTime? from = null, DateTime? to = null);
    Task<OrderEntity?> GetByIdAsync(Guid id);
    Task<OrderEntity?> GetByIdWithDetailsAsync(Guid id);
    Task<OrderEntity?> GetByOrderNumberAsync(string orderNumber);
    Task AddAsync(OrderEntity order);
    Task UpdateAsync(OrderEntity order);
    Task DeleteAsync(OrderEntity order);
    Task SaveChangesAsync();
}

