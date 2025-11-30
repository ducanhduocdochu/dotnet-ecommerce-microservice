using Discount.Application.Interfaces;
using Discount.Domain.Entities;
using Discount.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;

namespace Discount.Infrastructure.Repositories;

public class PromotionDiscountRepository : IPromotionDiscountRepository
{
    private readonly DiscountDbContext _context;

    public PromotionDiscountRepository(DiscountDbContext context)
    {
        _context = context;
    }

    public async Task<List<PromotionDiscount>> GetByPromotionIdAsync(Guid promotionId)
    {
        return await _context.PromotionDiscounts
            .Where(pd => pd.PromotionId == promotionId)
            .OrderBy(pd => pd.DisplayOrder)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<PromotionDiscount> promotionDiscounts)
    {
        await _context.PromotionDiscounts.AddRangeAsync(promotionDiscounts);
    }

    public async Task DeleteByPromotionIdAsync(Guid promotionId)
    {
        var items = await _context.PromotionDiscounts
            .Where(pd => pd.PromotionId == promotionId)
            .ToListAsync();
        _context.PromotionDiscounts.RemoveRange(items);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

