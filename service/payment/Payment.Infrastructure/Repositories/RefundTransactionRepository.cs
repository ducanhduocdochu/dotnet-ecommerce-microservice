using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Infrastructure.DB;

namespace Payment.Infrastructure.Repositories;

public class RefundTransactionRepository : IRefundTransactionRepository
{
    private readonly PaymentDbContext _context;

    public RefundTransactionRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<RefundTransaction?> GetByIdAsync(Guid id)
    {
        return await _context.RefundTransactions.FindAsync(id);
    }

    public async Task<List<RefundTransaction>> GetByPaymentIdAsync(Guid paymentId)
    {
        return await _context.RefundTransactions
            .Where(r => r.PaymentTransactionId == paymentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<RefundTransaction>> GetByUserIdAsync(Guid userId, int page, int pageSize, string? status)
    {
        var query = _context.RefundTransactions.Where(r => r.UserId == userId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountByUserIdAsync(Guid userId, string? status)
    {
        var query = _context.RefundTransactions.Where(r => r.UserId == userId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        return await query.CountAsync();
    }

    public async Task<List<RefundTransaction>> GetAllAsync(int page, int pageSize, string? status)
    {
        var query = _context.RefundTransactions.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? status)
    {
        var query = _context.RefundTransactions.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        return await query.CountAsync();
    }

    public async Task AddAsync(RefundTransaction refund)
    {
        await _context.RefundTransactions.AddAsync(refund);
    }

    public Task UpdateAsync(RefundTransaction refund)
    {
        _context.RefundTransactions.Update(refund);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

