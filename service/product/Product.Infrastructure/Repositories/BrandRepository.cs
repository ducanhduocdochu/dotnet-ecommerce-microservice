using Microsoft.EntityFrameworkCore;
using Product.Application.Interfaces;
using Product.Domain.Entities;
using Product.Infrastructure.DB;

namespace Product.Infrastructure.Repositories;

public class BrandRepository : IBrandRepository
{
    private readonly ProductDbContext _context;

    public BrandRepository(ProductDbContext context) => _context = context;

    public async Task<List<Brand>> GetAllAsync(int page, int pageSize, string? search = null)
    {
        var query = _context.Brands.Where(b => b.IsActive);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(b => b.Name.ToLower().Contains(search.ToLower()));
        return await query.OrderBy(b => b.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> GetCountAsync(string? search = null)
    {
        var query = _context.Brands.Where(b => b.IsActive);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(b => b.Name.ToLower().Contains(search.ToLower()));
        return await query.CountAsync();
    }

    public async Task<Brand?> GetByIdAsync(Guid id) =>
        await _context.Brands.FirstOrDefaultAsync(b => b.Id == id);

    public async Task<Brand?> GetBySlugAsync(string slug) =>
        await _context.Brands.FirstOrDefaultAsync(b => b.Slug == slug);

    public async Task AddAsync(Brand brand) => await _context.Brands.AddAsync(brand);
    public Task UpdateAsync(Brand brand) { _context.Brands.Update(brand); return Task.CompletedTask; }
    public Task RemoveAsync(Brand brand) { _context.Brands.Remove(brand); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}

