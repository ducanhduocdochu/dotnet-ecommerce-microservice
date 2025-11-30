using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IPaymentMethodRepository
{
    Task<PaymentMethodEntity?> GetByIdAsync(Guid id);
    Task<List<PaymentMethodEntity>> GetByUserIdAsync(Guid userId);
    Task<PaymentMethodEntity?> GetDefaultByUserIdAsync(Guid userId);
    Task AddAsync(PaymentMethodEntity method);
    Task UpdateAsync(PaymentMethodEntity method);
    Task DeleteAsync(PaymentMethodEntity method);
    Task ClearDefaultAsync(Guid userId);
    Task SaveChangesAsync();
}

