using Discount.Application.Interfaces;
using Discount.Domain.Entities;
using Discount.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;

namespace Discount.Infrastructure.Repositories;

public class FlashSaleRepository : IFlashSaleRepository
{
    private readonly DiscountDbContext _context;

    public FlashSaleRepository(DiscountDbContext context)
    {
        _context = context;
    }

    public async Task<FlashSale?> GetByIdAsync(Guid id)
    {
        return await _context.FlashSales.FindAsync(id);
    }

    public async Task<FlashSale?> GetByIdWithItemsAsync(Guid id)
    {
        return await _context.FlashSales
            .Include(f => f.FlashSaleItems)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<List<FlashSale>> GetAllAsync(int page, int pageSize)
    {
        return await _context.FlashSales
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.FlashSales.CountAsync();
    }

    public async Task<List<FlashSale>> GetActiveFlashSalesAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.FlashSales
            .Where(f => f.IsActive && f.StartTime <= now && f.EndTime >= now)
            .OrderBy(f => f.StartTime)
            .ToListAsync();
    }

    public async Task AddAsync(FlashSale flashSale)
    {
        await _context.FlashSales.AddAsync(flashSale);
    }

    public Task UpdateAsync(FlashSale flashSale)
    {
        _context.FlashSales.Update(flashSale);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(FlashSale flashSale)
    {
        _context.FlashSales.Remove(flashSale);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

