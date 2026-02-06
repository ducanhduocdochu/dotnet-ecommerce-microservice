using Discount.Application.Interfaces;
using Discount.Domain.Entities;
using Discount.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;

namespace Discount.Infrastructure.Repositories;

public class DiscountRepository : IDiscountRepository
{
    private readonly DiscountDbContext _context;

    public DiscountRepository(DiscountDbContext context)
    {
        _context = context;
    }

    public async Task<DiscountEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Discounts
            .Include(d => d.DiscountProducts)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<DiscountEntity?> GetByCodeAsync(string code)
    {
        return await _context.Discounts
            .Include(d => d.DiscountProducts)
            .Include(d => d.DiscountCategories)
            .Include(d => d.DiscountUsers)
            .FirstOrDefaultAsync(d => d.Code == code.ToUpperInvariant());
    }

    public async Task<List<DiscountEntity>> GetAllAsync(int page, int pageSize, string? type, bool? isActive, string? search)
    {
        var query = _context.Discounts.AsQueryable();

        if (!string.IsNullOrEmpty(type))
            query = query.Where(d => d.Type == type);

        if (isActive.HasValue)
            query = query.Where(d => d.IsActive == isActive.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(d => d.Code.Contains(search) || d.Name.Contains(search));

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? type, bool? isActive, string? search)
    {
        var query = _context.Discounts.AsQueryable();

        if (!string.IsNullOrEmpty(type))
            query = query.Where(d => d.Type == type);

        if (isActive.HasValue)
            query = query.Where(d => d.IsActive == isActive.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(d => d.Code.Contains(search) || d.Name.Contains(search));

        return await query.CountAsync();
    }

    public async Task<List<DiscountEntity>> GetActivePublicDiscountsAsync(int page, int pageSize)
    {
        var now = DateTime.UtcNow;
        return await _context.Discounts
            .Where(d => d.IsActive && d.IsPublic && d.StartDate <= now && d.EndDate >= now)
            .OrderBy(d => d.Priority)
            .ThenByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetActivePublicDiscountsCountAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Discounts
            .Where(d => d.IsActive && d.IsPublic && d.StartDate <= now && d.EndDate >= now)
            .CountAsync();
    }

    public async Task<List<DiscountEntity>> GetDiscountsForUserAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        return await _context.Discounts
            .Include(d => d.DiscountUsers)
            .Where(d => d.IsActive && d.StartDate <= now && d.EndDate >= now &&
                        d.Scope == "SPECIFIC_USERS" && d.DiscountUsers.Any(u => u.UserId == userId))
            .ToListAsync();
    }

    public async Task<List<DiscountEntity>> GetDiscountsForProductsAsync(List<Guid> productIds)
    {
        var now = DateTime.UtcNow;
        return await _context.Discounts
            .Include(d => d.DiscountProducts)
            .Where(d => d.IsActive && d.StartDate <= now && d.EndDate >= now &&
                        (d.Scope == "ALL" || d.DiscountProducts.Any(p => productIds.Contains(p.ProductId))))
            .ToListAsync();
    }

    public async Task AddAsync(DiscountEntity discount)
    {
        await _context.Discounts.AddAsync(discount);
    }

    public Task UpdateAsync(DiscountEntity discount)
    {
        _context.Discounts.Update(discount);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DiscountEntity discount)
    {
        _context.Discounts.Remove(discount);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

