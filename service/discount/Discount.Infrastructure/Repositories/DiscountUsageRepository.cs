using Discount.Application.Interfaces;
using Discount.Domain.Entities;
using Discount.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;

namespace Discount.Infrastructure.Repositories;

public class DiscountUsageRepository : IDiscountUsageRepository
{
    private readonly DiscountDbContext _context;

    public DiscountUsageRepository(DiscountDbContext context)
    {
        _context = context;
    }

    public async Task<DiscountUsage?> GetByOrderIdAsync(Guid orderId)
    {
        return await _context.DiscountUsages
            .FirstOrDefaultAsync(du => du.OrderId == orderId);
    }

    public async Task<List<DiscountUsage>> GetByDiscountIdAsync(Guid discountId, int limit = 10)
    {
        return await _context.DiscountUsages
            .Where(du => du.DiscountId == discountId)
            .OrderByDescending(du => du.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> GetUsageCountByUserAsync(Guid discountId, Guid userId)
    {
        return await _context.DiscountUsages
            .Where(du => du.DiscountId == discountId && du.UserId == userId)
            .CountAsync();
    }

    public async Task<(int TotalUsage, decimal TotalAmount, int UniqueUsers)> GetStatisticsAsync(Guid discountId)
    {
        var usages = await _context.DiscountUsages
            .Where(du => du.DiscountId == discountId)
            .ToListAsync();

        return (
            usages.Count,
            usages.Sum(u => u.DiscountAmount),
            usages.Select(u => u.UserId).Distinct().Count()
        );
    }

    public async Task<List<(DateTime Date, int Count, decimal Amount)>> GetUsageByDateAsync(Guid discountId, DateTime startDate, DateTime endDate)
    {
        var usages = await _context.DiscountUsages
            .Where(du => du.DiscountId == discountId && du.CreatedAt >= startDate && du.CreatedAt <= endDate)
            .ToListAsync();

        return usages
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => (g.Key, g.Count(), g.Sum(u => u.DiscountAmount)))
            .OrderBy(x => x.Key)
            .ToList();
    }

    public async Task AddAsync(DiscountUsage usage)
    {
        await _context.DiscountUsages.AddAsync(usage);
    }

    public Task DeleteAsync(DiscountUsage usage)
    {
        _context.DiscountUsages.Remove(usage);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

