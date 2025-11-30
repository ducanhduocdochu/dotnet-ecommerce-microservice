using Microsoft.EntityFrameworkCore;
using Product.Application.Interfaces;
using Product.Domain.Entities;
using Product.Infrastructure.DB;

namespace Product.Infrastructure.Repositories;

public class ProductImageRepository : IProductImageRepository
{
    private readonly ProductDbContext _context;

    public ProductImageRepository(ProductDbContext context) => _context = context;

    public async Task<List<ProductImage>> GetByProductIdAsync(Guid productId) =>
        await _context.ProductImages.Where(i => i.ProductId == productId).OrderBy(i => i.SortOrder).ToListAsync();

    public async Task<ProductImage?> GetByIdAsync(Guid id) =>
        await _context.ProductImages.FirstOrDefaultAsync(i => i.Id == id);

    public async Task AddAsync(ProductImage image) => await _context.ProductImages.AddAsync(image);
    public Task UpdateAsync(ProductImage image) { _context.ProductImages.Update(image); return Task.CompletedTask; }
    public Task RemoveAsync(ProductImage image) { _context.ProductImages.Remove(image); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}

