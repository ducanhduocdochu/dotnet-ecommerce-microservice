using Discount.Application.Interfaces;
using Discount.Domain.Entities;
using Discount.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;

namespace Discount.Infrastructure.Repositories;

public class FlashSaleItemRepository : IFlashSaleItemRepository
{
    private readonly DiscountDbContext _context;

    public FlashSaleItemRepository(DiscountDbContext context)
    {
        _context = context;
    }

    public async Task<FlashSaleItem?> GetByIdAsync(Guid id)
    {
        return await _context.FlashSaleItems.FindAsync(id);
    }

    public async Task<FlashSaleItem?> GetByProductAsync(Guid flashSaleId, Guid productId, Guid? variantId)
    {
        return await _context.FlashSaleItems
            .FirstOrDefaultAsync(i => i.FlashSaleId == flashSaleId && 
                                      i.ProductId == productId && 
                                      i.VariantId == variantId);
    }

    public async Task<List<FlashSaleItem>> GetByFlashSaleIdAsync(Guid flashSaleId)
    {
        return await _context.FlashSaleItems
            .Where(i => i.FlashSaleId == flashSaleId)
            .ToListAsync();
    }

    public async Task AddAsync(FlashSaleItem item)
    {
        await _context.FlashSaleItems.AddAsync(item);
    }

    public async Task AddRangeAsync(IEnumerable<FlashSaleItem> items)
    {
        await _context.FlashSaleItems.AddRangeAsync(items);
    }

    public Task UpdateAsync(FlashSaleItem item)
    {
        _context.FlashSaleItems.Update(item);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(FlashSaleItem item)
    {
        _context.FlashSaleItems.Remove(item);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

