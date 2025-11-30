using Discount.Domain.Entities;

namespace Discount.Application.Interfaces;

public interface IPromotionRepository
{
    Task<Promotion?> GetByIdAsync(Guid id);
    Task<Promotion?> GetByIdWithDiscountsAsync(Guid id);
    Task<List<Promotion>> GetAllAsync(int page, int pageSize, bool? isActive);
    Task<int> GetTotalCountAsync(bool? isActive);
    Task<List<Promotion>> GetActivePromotionsAsync();
    Task AddAsync(Promotion promotion);
    Task UpdateAsync(Promotion promotion);
    Task DeleteAsync(Promotion promotion);
    Task SaveChangesAsync();
}

