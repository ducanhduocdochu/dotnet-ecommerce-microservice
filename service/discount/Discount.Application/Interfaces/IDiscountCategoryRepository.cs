using Discount.Domain.Entities;

namespace Discount.Application.Interfaces;

public interface IDiscountCategoryRepository
{
    Task<List<DiscountCategory>> GetByDiscountIdAsync(Guid discountId);
    Task AddRangeAsync(IEnumerable<DiscountCategory> discountCategories);
    Task DeleteByDiscountIdAsync(Guid discountId);
    Task SaveChangesAsync();
}

