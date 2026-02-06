using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IPaymentLogRepository
{
    Task<List<PaymentLog>> GetByTransactionIdAsync(Guid transactionId);
    Task<List<PaymentLog>> GetByRefundIdAsync(Guid refundId);
    Task AddAsync(PaymentLog log);
    Task SaveChangesAsync();
}

