using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IGatewayConfigRepository
{
    Task<PaymentGatewayConfig?> GetByCodeAsync(string gatewayCode);
    Task<List<PaymentGatewayConfig>> GetAllAsync();
    Task<List<PaymentGatewayConfig>> GetActiveAsync();
    Task UpdateAsync(PaymentGatewayConfig config);
    Task SaveChangesAsync();
}

