using Product.Domain.Entities;

namespace Product.Application.Interfaces;

public interface IProductImageRepository
{
    Task<List<ProductImage>> GetByProductIdAsync(Guid productId);
    Task<ProductImage?> GetByIdAsync(Guid id);
    Task AddAsync(ProductImage image);
    Task UpdateAsync(ProductImage image);
    Task RemoveAsync(ProductImage image);
    Task SaveChangesAsync();
}

