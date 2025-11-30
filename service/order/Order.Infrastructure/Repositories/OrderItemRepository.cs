using Microsoft.EntityFrameworkCore;
using Order.Application.Interfaces;
using Order.Domain.Entities;
using Order.Infrastructure.DB;

namespace Order.Infrastructure.Repositories;

public class OrderItemRepository : IOrderItemRepository
{
    private readonly OrderDbContext _context;

    public OrderItemRepository(OrderDbContext context) => _context = context;

    public async Task<List<OrderItem>> GetByOrderIdAsync(Guid orderId) =>
        await _context.OrderItems.Where(i => i.OrderId == orderId).ToListAsync();

    public async Task<List<OrderItem>> GetBySellerIdAsync(Guid sellerId) =>
        await _context.OrderItems.Where(i => i.SellerId == sellerId).ToListAsync();

    public async Task<OrderItem?> GetByIdAsync(Guid id) =>
        await _context.OrderItems.FirstOrDefaultAsync(i => i.Id == id);

    public async Task AddAsync(OrderItem item) => await _context.OrderItems.AddAsync(item);
    public async Task AddRangeAsync(List<OrderItem> items) => await _context.OrderItems.AddRangeAsync(items);
    public Task UpdateAsync(OrderItem item) { _context.OrderItems.Update(item); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}

