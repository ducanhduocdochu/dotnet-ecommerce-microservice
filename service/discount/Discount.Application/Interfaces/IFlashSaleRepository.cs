using Discount.Domain.Entities;

namespace Discount.Application.Interfaces;

public interface IFlashSaleRepository
{
    Task<FlashSale?> GetByIdAsync(Guid id);
    Task<FlashSale?> GetByIdWithItemsAsync(Guid id);
    Task<List<FlashSale>> GetAllAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task<List<FlashSale>> GetActiveFlashSalesAsync();
    Task AddAsync(FlashSale flashSale);
    Task UpdateAsync(FlashSale flashSale);
    Task DeleteAsync(FlashSale flashSale);
    Task SaveChangesAsync();
}

