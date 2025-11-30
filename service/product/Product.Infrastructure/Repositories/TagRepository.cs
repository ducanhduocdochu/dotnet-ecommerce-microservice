using Microsoft.EntityFrameworkCore;
using Product.Application.Interfaces;
using Product.Domain.Entities;
using Product.Infrastructure.DB;

namespace Product.Infrastructure.Repositories;

public class TagRepository : ITagRepository
{
    private readonly ProductDbContext _context;

    public TagRepository(ProductDbContext context) => _context = context;

    public async Task<Tag?> GetByIdAsync(Guid id) =>
        await _context.Tags.FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Tag?> GetBySlugAsync(string slug) =>
        await _context.Tags.FirstOrDefaultAsync(t => t.Slug == slug);

    public async Task<Tag?> GetByNameAsync(string name) =>
        await _context.Tags.FirstOrDefaultAsync(t => t.Name == name);

    public async Task<List<Tag>> GetByProductIdAsync(Guid productId) =>
        await _context.ProductTags.Where(pt => pt.ProductId == productId)
            .Include(pt => pt.Tag).Select(pt => pt.Tag!).ToListAsync();

    public async Task AddAsync(Tag tag) => await _context.Tags.AddAsync(tag);
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}

