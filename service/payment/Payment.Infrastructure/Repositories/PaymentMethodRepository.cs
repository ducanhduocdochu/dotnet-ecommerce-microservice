using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Infrastructure.DB;

namespace Payment.Infrastructure.Repositories;

public class PaymentMethodRepository : IPaymentMethodRepository
{
    private readonly PaymentDbContext _context;

    public PaymentMethodRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentMethodEntity?> GetByIdAsync(Guid id)
    {
        return await _context.PaymentMethods.FindAsync(id);
    }

    public async Task<List<PaymentMethodEntity>> GetByUserIdAsync(Guid userId)
    {
        return await _context.PaymentMethods
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.IsDefault)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<PaymentMethodEntity?> GetDefaultByUserIdAsync(Guid userId)
    {
        return await _context.PaymentMethods
            .FirstOrDefaultAsync(m => m.UserId == userId && m.IsDefault);
    }

    public async Task AddAsync(PaymentMethodEntity method)
    {
        await _context.PaymentMethods.AddAsync(method);
    }

    public Task UpdateAsync(PaymentMethodEntity method)
    {
        _context.PaymentMethods.Update(method);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(PaymentMethodEntity method)
    {
        _context.PaymentMethods.Remove(method);
        return Task.CompletedTask;
    }

    public async Task ClearDefaultAsync(Guid userId)
    {
        var defaultMethod = await GetDefaultByUserIdAsync(userId);
        if (defaultMethod != null)
        {
            defaultMethod.SetDefault(false);
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

