using Product.Domain.Entities;

namespace Product.Application.Interfaces;

public interface IBrandRepository
{
    Task<List<Brand>> GetAllAsync(int page, int pageSize, string? search = null);
    Task<int> GetCountAsync(string? search = null);
    Task<Brand?> GetByIdAsync(Guid id);
    Task<Brand?> GetBySlugAsync(string slug);
    Task AddAsync(Brand brand);
    Task UpdateAsync(Brand brand);
    Task RemoveAsync(Brand brand);
    Task SaveChangesAsync();
}

