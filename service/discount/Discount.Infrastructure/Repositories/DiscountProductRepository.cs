using Discount.Application.Interfaces;
using Discount.Domain.Entities;
using Discount.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;

namespace Discount.Infrastructure.Repositories;

public class DiscountProductRepository : IDiscountProductRepository
{
    private readonly DiscountDbContext _context;

    public DiscountProductRepository(DiscountDbContext context)
    {
        _context = context;
    }

    public async Task<List<DiscountProduct>> GetByDiscountIdAsync(Guid discountId)
    {
        return await _context.DiscountProducts
            .Where(dp => dp.DiscountId == discountId)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<DiscountProduct> discountProducts)
    {
        await _context.DiscountProducts.AddRangeAsync(discountProducts);
    }

    public async Task DeleteByDiscountIdAsync(Guid discountId)
    {
        var items = await _context.DiscountProducts
            .Where(dp => dp.DiscountId == discountId)
            .ToListAsync();
        _context.DiscountProducts.RemoveRange(items);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

