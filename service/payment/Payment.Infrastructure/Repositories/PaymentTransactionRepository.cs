using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Infrastructure.DB;

namespace Payment.Infrastructure.Repositories;

public class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly PaymentDbContext _context;

    public PaymentTransactionRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentTransaction?> GetByIdAsync(Guid id)
    {
        return await _context.PaymentTransactions
            .Include(t => t.Refunds)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<PaymentTransaction?> GetByTransactionCodeAsync(string transactionCode)
    {
        return await _context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.TransactionCode == transactionCode);
    }

    public async Task<PaymentTransaction?> GetByOrderIdAsync(Guid orderId)
    {
        return await _context.PaymentTransactions
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(t => t.OrderId == orderId);
    }

    public async Task<List<PaymentTransaction>> GetByUserIdAsync(Guid userId, int page, int pageSize, string? status)
    {
        var query = _context.PaymentTransactions.Where(t => t.UserId == userId);
        
        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountByUserIdAsync(Guid userId, string? status)
    {
        var query = _context.PaymentTransactions.Where(t => t.UserId == userId);
        
        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        return await query.CountAsync();
    }

    public async Task<List<PaymentTransaction>> GetAllAsync(int page, int pageSize, string? status, string? gateway, DateTime? from, DateTime? to)
    {
        var query = _context.PaymentTransactions.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);
        
        if (!string.IsNullOrEmpty(gateway))
            query = query.Where(t => t.PaymentGateway == gateway);
        
        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value);
        
        if (to.HasValue)
            query = query.Where(t => t.CreatedAt <= to.Value);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? status, string? gateway, DateTime? from, DateTime? to)
    {
        var query = _context.PaymentTransactions.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);
        
        if (!string.IsNullOrEmpty(gateway))
            query = query.Where(t => t.PaymentGateway == gateway);
        
        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value);
        
        if (to.HasValue)
            query = query.Where(t => t.CreatedAt <= to.Value);

        return await query.CountAsync();
    }

    public async Task AddAsync(PaymentTransaction transaction)
    {
        await _context.PaymentTransactions.AddAsync(transaction);
    }

    public Task UpdateAsync(PaymentTransaction transaction)
    {
        _context.PaymentTransactions.Update(transaction);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

