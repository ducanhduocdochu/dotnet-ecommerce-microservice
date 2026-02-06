using Microsoft.EntityFrameworkCore;
using Order.Application.Interfaces;
using Order.Domain.Entities;
using Order.Infrastructure.DB;

namespace Order.Infrastructure.Repositories;

public class OrderRefundRepository : IOrderRefundRepository
{
    private readonly OrderDbContext _context;

    public OrderRefundRepository(OrderDbContext context) => _context = context;

    public async Task<List<OrderRefund>> GetAllAsync(int page, int pageSize, string? status = null)
    {
        var query = _context.OrderRefunds.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);
        return await query.OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(string? status = null)
    {
        var query = _context.OrderRefunds.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);
        return await query.CountAsync();
    }

    public async Task<List<OrderRefund>> GetByOrderIdAsync(Guid orderId) =>
        await _context.OrderRefunds.Where(r => r.OrderId == orderId).ToListAsync();

    public async Task<OrderRefund?> GetByIdAsync(Guid id) =>
        await _context.OrderRefunds.FirstOrDefaultAsync(r => r.Id == id);

    public async Task AddAsync(OrderRefund refund) => await _context.OrderRefunds.AddAsync(refund);
    public Task UpdateAsync(OrderRefund refund) { _context.OrderRefunds.Update(refund); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}

