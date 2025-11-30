using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IPaymentTransactionRepository
{
    Task<PaymentTransaction?> GetByIdAsync(Guid id);
    Task<PaymentTransaction?> GetByTransactionCodeAsync(string transactionCode);
    Task<PaymentTransaction?> GetByOrderIdAsync(Guid orderId);
    Task<List<PaymentTransaction>> GetByUserIdAsync(Guid userId, int page, int pageSize, string? status);
    Task<int> GetCountByUserIdAsync(Guid userId, string? status);
    Task<List<PaymentTransaction>> GetAllAsync(int page, int pageSize, string? status, string? gateway, DateTime? from, DateTime? to);
    Task<int> GetTotalCountAsync(string? status, string? gateway, DateTime? from, DateTime? to);
    Task AddAsync(PaymentTransaction transaction);
    Task UpdateAsync(PaymentTransaction transaction);
    Task SaveChangesAsync();
}

