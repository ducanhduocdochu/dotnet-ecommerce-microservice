using Microsoft.EntityFrameworkCore;
using Order.Application.Interfaces;
using Order.Domain.Entities;
using Order.Infrastructure.DB;

namespace Order.Infrastructure.Repositories;

public class OrderStatusHistoryRepository : IOrderStatusHistoryRepository
{
    private readonly OrderDbContext _context;

    public OrderStatusHistoryRepository(OrderDbContext context) => _context = context;

    public async Task<List<OrderStatusHistory>> GetByOrderIdAsync(Guid orderId) =>
        await _context.OrderStatusHistory
            .Where(h => h.OrderId == orderId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(OrderStatusHistory history) => await _context.OrderStatusHistory.AddAsync(history);
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}

