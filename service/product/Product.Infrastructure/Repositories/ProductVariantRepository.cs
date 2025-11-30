using Microsoft.EntityFrameworkCore;
using Product.Application.Interfaces;
using Product.Domain.Entities;
using Product.Infrastructure.DB;

namespace Product.Infrastructure.Repositories;

public class ProductVariantRepository : IProductVariantRepository
{
    private readonly ProductDbContext _context;

    public ProductVariantRepository(ProductDbContext context) => _context = context;

    public async Task<List<ProductVariant>> GetByProductIdAsync(Guid productId) =>
        await _context.ProductVariants.Where(v => v.ProductId == productId).Include(v => v.Options).ToListAsync();

    public async Task<ProductVariant?> GetByIdAsync(Guid id) =>
        await _context.ProductVariants.Include(v => v.Options).FirstOrDefaultAsync(v => v.Id == id);

    public async Task AddAsync(ProductVariant variant) => await _context.ProductVariants.AddAsync(variant);
    public Task UpdateAsync(ProductVariant variant) { _context.ProductVariants.Update(variant); return Task.CompletedTask; }
    public Task RemoveAsync(ProductVariant variant) { _context.ProductVariants.Remove(variant); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}

