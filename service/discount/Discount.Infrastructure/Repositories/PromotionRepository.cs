using Discount.Application.Interfaces;
using Discount.Domain.Entities;
using Discount.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;

namespace Discount.Infrastructure.Repositories;

public class PromotionRepository : IPromotionRepository
{
    private readonly DiscountDbContext _context;

    public PromotionRepository(DiscountDbContext context)
    {
        _context = context;
    }

    public async Task<Promotion?> GetByIdAsync(Guid id)
    {
        return await _context.Promotions.FindAsync(id);
    }

    public async Task<Promotion?> GetByIdWithDiscountsAsync(Guid id)
    {
        return await _context.Promotions
            .Include(p => p.PromotionDiscounts)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Promotion>> GetAllAsync(int page, int pageSize, bool? isActive)
    {
        var query = _context.Promotions.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        return await query
            .OrderBy(p => p.DisplayOrder)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(bool? isActive)
    {
        var query = _context.Promotions.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        return await query.CountAsync();
    }

    public async Task<List<Promotion>> GetActivePromotionsAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Promotions
            .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();
    }

    public async Task AddAsync(Promotion promotion)
    {
        await _context.Promotions.AddAsync(promotion);
    }

    public Task UpdateAsync(Promotion promotion)
    {
        _context.Promotions.Update(promotion);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Promotion promotion)
    {
        _context.Promotions.Remove(promotion);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

