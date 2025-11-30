using Product.Domain.Entities;

namespace Product.Application.Interfaces;

public interface ITagRepository
{
    Task<Tag?> GetByIdAsync(Guid id);
    Task<Tag?> GetBySlugAsync(string slug);
    Task<Tag?> GetByNameAsync(string name);
    Task<List<Tag>> GetByProductIdAsync(Guid productId);
    Task AddAsync(Tag tag);
    Task SaveChangesAsync();
}

