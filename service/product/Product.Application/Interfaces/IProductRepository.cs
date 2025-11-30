using Product.Application.DTOs;
using Product.Domain.Entities;

namespace Product.Application.Interfaces;

public interface IProductRepository
{
    Task<List<ProductEntity>> GetAllAsync(ProductFilterRequest filter);
    Task<int> GetCountAsync(ProductFilterRequest filter);
    Task<ProductEntity?> GetByIdAsync(Guid id);
    Task<ProductEntity?> GetByIdWithDetailsAsync(Guid id);
    Task<ProductEntity?> GetBySlugAsync(string slug);
    Task<List<ProductEntity>> GetBySellerIdAsync(Guid sellerId, int page, int pageSize, string? status = null);
    Task<int> GetCountBySellerIdAsync(Guid sellerId, string? status = null);
    Task<List<ProductEntity>> GetByCategoryIdAsync(Guid categoryId, int page, int pageSize);
    Task<int> GetCountByCategoryIdAsync(Guid categoryId);
    Task<List<ProductEntity>> GetFeaturedAsync(int limit);
    Task<List<ProductEntity>> SearchAsync(string keyword, int page, int pageSize);
    Task<int> SearchCountAsync(string keyword);
    Task<List<ProductEntity>> GetPendingAsync(int page, int pageSize);
    Task<int> GetPendingCountAsync();
    Task AddAsync(ProductEntity product);
    Task UpdateAsync(ProductEntity product);
    Task RemoveAsync(ProductEntity product);
    Task SaveChangesAsync();
}

