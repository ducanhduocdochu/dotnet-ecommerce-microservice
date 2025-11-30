using Discount.Domain.Entities;

namespace Discount.Application.Interfaces;

public interface IDiscountUserRepository
{
    Task<List<DiscountUser>> GetByDiscountIdAsync(Guid discountId);
    Task<List<DiscountUser>> GetByUserIdAsync(Guid userId);
    Task AddRangeAsync(IEnumerable<DiscountUser> discountUsers);
    Task DeleteByDiscountIdAsync(Guid discountId);
    Task SaveChangesAsync();
}

