using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IRefundTransactionRepository
{
    Task<RefundTransaction?> GetByIdAsync(Guid id);
    Task<List<RefundTransaction>> GetByPaymentIdAsync(Guid paymentId);
    Task<List<RefundTransaction>> GetByUserIdAsync(Guid userId, int page, int pageSize, string? status);
    Task<int> GetCountByUserIdAsync(Guid userId, string? status);
    Task<List<RefundTransaction>> GetAllAsync(int page, int pageSize, string? status);
    Task<int> GetTotalCountAsync(string? status);
    Task AddAsync(RefundTransaction refund);
    Task UpdateAsync(RefundTransaction refund);
    Task SaveChangesAsync();
}

