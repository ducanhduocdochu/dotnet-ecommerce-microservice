using Discount.Domain.Entities;

namespace Discount.Application.Interfaces;

public interface IPromotionDiscountRepository
{
    Task<List<PromotionDiscount>> GetByPromotionIdAsync(Guid promotionId);
    Task AddRangeAsync(IEnumerable<PromotionDiscount> promotionDiscounts);
    Task DeleteByPromotionIdAsync(Guid promotionId);
    Task SaveChangesAsync();
}

