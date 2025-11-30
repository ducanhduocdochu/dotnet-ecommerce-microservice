using Discount.Application.Interfaces;
using Discount.Domain.Entities;
using Discount.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;

namespace Discount.Infrastructure.Repositories;

public class DiscountCategoryRepository : IDiscountCategoryRepository
{
    private readonly DiscountDbContext _context;

    public DiscountCategoryRepository(DiscountDbContext context)
    {
        _context = context;
    }

    public async Task<List<DiscountCategory>> GetByDiscountIdAsync(Guid discountId)
    {
        return await _context.DiscountCategories
            .Where(dc => dc.DiscountId == discountId)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<DiscountCategory> discountCategories)
    {
        await _context.DiscountCategories.AddRangeAsync(discountCategories);
    }

    public async Task DeleteByDiscountIdAsync(Guid discountId)
    {
        var items = await _context.DiscountCategories
            .Where(dc => dc.DiscountId == discountId)
            .ToListAsync();
        _context.DiscountCategories.RemoveRange(items);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

