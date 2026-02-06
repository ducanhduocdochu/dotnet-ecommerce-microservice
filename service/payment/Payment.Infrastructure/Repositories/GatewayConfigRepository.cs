using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Infrastructure.DB;

namespace Payment.Infrastructure.Repositories;

public class GatewayConfigRepository : IGatewayConfigRepository
{
    private readonly PaymentDbContext _context;

    public GatewayConfigRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentGatewayConfig?> GetByCodeAsync(string gatewayCode)
    {
        return await _context.PaymentGatewayConfigs
            .FirstOrDefaultAsync(c => c.GatewayCode == gatewayCode);
    }

    public async Task<List<PaymentGatewayConfig>> GetAllAsync()
    {
        return await _context.PaymentGatewayConfigs.ToListAsync();
    }

    public async Task<List<PaymentGatewayConfig>> GetActiveAsync()
    {
        return await _context.PaymentGatewayConfigs
            .Where(c => c.IsActive)
            .ToListAsync();
    }

    public Task UpdateAsync(PaymentGatewayConfig config)
    {
        _context.PaymentGatewayConfigs.Update(config);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

