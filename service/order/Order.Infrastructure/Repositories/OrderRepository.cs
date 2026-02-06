using Microsoft.EntityFrameworkCore;
using Order.Application.Interfaces;
using Order.Domain.Entities;
using Order.Infrastructure.DB;

namespace Order.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context) => _context = context;

    public async Task<List<OrderEntity>> GetByUserIdAsync(Guid userId, int page, int pageSize, string? status = null)
    {
        var query = _context.Orders.Where(o => o.UserId == userId);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);
        return await query.OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Include(o => o.Items)
            .ToListAsync();
    }

    public async Task<int> GetCountByUserIdAsync(Guid userId, string? status = null)
    {
        var query = _context.Orders.Where(o => o.UserId == userId);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);
        return await query.CountAsync();
    }

    public async Task<List<OrderEntity>> GetBySellerIdAsync(Guid sellerId, int page, int pageSize, string? status = null)
    {
        var query = _context.Orders.Where(o => o.Items.Any(i => i.SellerId == sellerId));
        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);
        return await query.OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Include(o => o.Items.Where(i => i.SellerId == sellerId))
            .ToListAsync();
    }

    public async Task<int> GetCountBySellerIdAsync(Guid sellerId, string? status = null)
    {
        var query = _context.Orders.Where(o => o.Items.Any(i => i.SellerId == sellerId));
        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);
        return await query.CountAsync();
    }

    public async Task<List<OrderEntity>> GetAllAsync(int page, int pageSize, string? status = null, Guid? userId = null, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.Orders.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);
        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId.Value);
        if (from.HasValue)
            query = query.Where(o => o.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(o => o.CreatedAt <= to.Value);
        return await query.OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Include(o => o.Items)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(string? status = null, Guid? userId = null, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.Orders.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);
        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId.Value);
        if (from.HasValue)
            query = query.Where(o => o.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(o => o.CreatedAt <= to.Value);
        return await query.CountAsync();
    }

    public async Task<OrderEntity?> GetByIdAsync(Guid id) =>
        await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);

    public async Task<OrderEntity?> GetByIdWithDetailsAsync(Guid id) =>
        await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory.OrderByDescending(h => h.CreatedAt))
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<OrderEntity?> GetByOrderNumberAsync(string orderNumber) =>
        await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory.OrderByDescending(h => h.CreatedAt))
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

    public async Task AddAsync(OrderEntity order) => await _context.Orders.AddAsync(order);
    public Task UpdateAsync(OrderEntity order) { _context.Orders.Update(order); return Task.CompletedTask; }
    public Task DeleteAsync(OrderEntity order) { _context.Orders.Remove(order); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}

