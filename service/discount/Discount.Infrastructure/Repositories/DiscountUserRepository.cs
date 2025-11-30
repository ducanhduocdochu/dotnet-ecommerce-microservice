using Discount.Application.Interfaces;
using Discount.Domain.Entities;
using Discount.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;

namespace Discount.Infrastructure.Repositories;

public class DiscountUserRepository : IDiscountUserRepository
{
    private readonly DiscountDbContext _context;

    public DiscountUserRepository(DiscountDbContext context)
    {
        _context = context;
    }

    public async Task<List<DiscountUser>> GetByDiscountIdAsync(Guid discountId)
    {
        return await _context.DiscountUsers
            .Where(du => du.DiscountId == discountId)
            .ToListAsync();
    }

    public async Task<List<DiscountUser>> GetByUserIdAsync(Guid userId)
    {
        return await _context.DiscountUsers
            .Where(du => du.UserId == userId)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<DiscountUser> discountUsers)
    {
        await _context.DiscountUsers.AddRangeAsync(discountUsers);
    }

    public async Task DeleteByDiscountIdAsync(Guid discountId)
    {
        var items = await _context.DiscountUsers
            .Where(du => du.DiscountId == discountId)
            .ToListAsync();
        _context.DiscountUsers.RemoveRange(items);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

