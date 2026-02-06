using Discount.Domain.Entities;

namespace Discount.Application.Interfaces;

public interface IDiscountProductRepository
{
    Task<List<DiscountProduct>> GetByDiscountIdAsync(Guid discountId);
    Task AddRangeAsync(IEnumerable<DiscountProduct> discountProducts);
    Task DeleteByDiscountIdAsync(Guid discountId);
    Task SaveChangesAsync();
}

