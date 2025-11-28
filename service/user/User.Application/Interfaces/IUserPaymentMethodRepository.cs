using User.Domain.Entities;

namespace User.Application.Interfaces;

public interface IUserPaymentMethodRepository
{
    Task<List<UserPaymentMethod>> GetByUserIdAsync(Guid userId);
    Task<UserPaymentMethod?> GetByIdAsync(Guid id);
    Task<UserPaymentMethod?> GetDefaultByUserIdAsync(Guid userId);
    Task AddAsync(UserPaymentMethod paymentMethod);
    Task UpdateAsync(UserPaymentMethod paymentMethod);
    Task RemoveAsync(UserPaymentMethod paymentMethod);
    Task SaveChangesAsync();
}

