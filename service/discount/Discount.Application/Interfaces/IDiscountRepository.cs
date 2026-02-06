using Discount.Domain.Entities;

namespace Discount.Application.Interfaces;

public interface IDiscountRepository
{
    Task<DiscountEntity?> GetByIdAsync(Guid id);
    Task<DiscountEntity?> GetByCodeAsync(string code);
    Task<List<DiscountEntity>> GetAllAsync(int page, int pageSize, string? type, bool? isActive, string? search);
    Task<int> GetTotalCountAsync(string? type, bool? isActive, string? search);
    Task<List<DiscountEntity>> GetActivePublicDiscountsAsync(int page, int pageSize);
    Task<int> GetActivePublicDiscountsCountAsync();
    Task<List<DiscountEntity>> GetDiscountsForUserAsync(Guid userId);
    Task<List<DiscountEntity>> GetDiscountsForProductsAsync(List<Guid> productIds);
    Task AddAsync(DiscountEntity discount);
    Task UpdateAsync(DiscountEntity discount);
    Task DeleteAsync(DiscountEntity discount);
    Task SaveChangesAsync();
}

