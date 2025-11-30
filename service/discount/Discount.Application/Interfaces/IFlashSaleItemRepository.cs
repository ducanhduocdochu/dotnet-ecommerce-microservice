using Discount.Domain.Entities;

namespace Discount.Application.Interfaces;

public interface IFlashSaleItemRepository
{
    Task<FlashSaleItem?> GetByIdAsync(Guid id);
    Task<FlashSaleItem?> GetByProductAsync(Guid flashSaleId, Guid productId, Guid? variantId);
    Task<List<FlashSaleItem>> GetByFlashSaleIdAsync(Guid flashSaleId);
    Task AddAsync(FlashSaleItem item);
    Task AddRangeAsync(IEnumerable<FlashSaleItem> items);
    Task UpdateAsync(FlashSaleItem item);
    Task DeleteAsync(FlashSaleItem item);
    Task SaveChangesAsync();
}

