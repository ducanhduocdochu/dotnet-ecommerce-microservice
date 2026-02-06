using Microsoft.EntityFrameworkCore;
using Product.Application.Interfaces;
using Product.Domain.Entities;
using Product.Infrastructure.DB;

namespace Product.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ProductDbContext _context;

    public CategoryRepository(ProductDbContext context) => _context = context;

    public async Task<List<Category>> GetAllAsync() =>
        await _context.Categories.OrderBy(c => c.SortOrder).ToListAsync();

    public async Task<Category?> GetByIdAsync(Guid id) =>
        await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Category?> GetBySlugAsync(string slug) =>
        await _context.Categories.FirstOrDefaultAsync(c => c.Slug == slug);

    public async Task<List<Category>> GetByParentIdAsync(Guid? parentId) =>
        await _context.Categories.Where(c => c.ParentId == parentId).OrderBy(c => c.SortOrder).ToListAsync();

    public async Task AddAsync(Category category) => await _context.Categories.AddAsync(category);
    public Task UpdateAsync(Category category) { _context.Categories.Update(category); return Task.CompletedTask; }
    public Task RemoveAsync(Category category) { _context.Categories.Remove(category); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}

