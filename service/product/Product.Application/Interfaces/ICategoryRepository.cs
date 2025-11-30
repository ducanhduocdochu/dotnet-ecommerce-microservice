using Product.Domain.Entities;

namespace Product.Application.Interfaces;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(Guid id);
    Task<Category?> GetBySlugAsync(string slug);
    Task<List<Category>> GetByParentIdAsync(Guid? parentId);
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task RemoveAsync(Category category);
    Task SaveChangesAsync();
}

