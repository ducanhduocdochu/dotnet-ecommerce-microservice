using Product.Domain.Entities;

namespace Product.Application.Interfaces;

public interface IProductVariantRepository
{
    Task<List<ProductVariant>> GetByProductIdAsync(Guid productId);
    Task<ProductVariant?> GetByIdAsync(Guid id);
    Task AddAsync(ProductVariant variant);
    Task UpdateAsync(ProductVariant variant);
    Task RemoveAsync(ProductVariant variant);
    Task SaveChangesAsync();
}

