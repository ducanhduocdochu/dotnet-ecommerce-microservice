using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Infrastructure.DB;

namespace Payment.Infrastructure.Repositories;

public class PaymentLogRepository : IPaymentLogRepository
{
    private readonly PaymentDbContext _context;

    public PaymentLogRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<List<PaymentLog>> GetByTransactionIdAsync(Guid transactionId)
    {
        return await _context.PaymentLogs
            .Where(l => l.TransactionId == transactionId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<PaymentLog>> GetByRefundIdAsync(Guid refundId)
    {
        return await _context.PaymentLogs
            .Where(l => l.RefundId == refundId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(PaymentLog log)
    {
        await _context.PaymentLogs.AddAsync(log);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

