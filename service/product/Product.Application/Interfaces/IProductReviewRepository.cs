using Product.Domain.Entities;

namespace Product.Application.Interfaces;

public interface IProductReviewRepository
{
    Task<List<ProductReview>> GetByProductIdAsync(Guid productId, int page, int pageSize, int? rating = null);
    Task<int> GetCountByProductIdAsync(Guid productId, int? rating = null);
    Task<ProductReview?> GetByIdAsync(Guid id);
    Task<ProductReview?> GetByUserAndProductAsync(Guid userId, Guid productId);
    Task<List<ProductReview>> GetByUserIdAsync(Guid userId);
    Task<Dictionary<int, int>> GetRatingDistributionAsync(Guid productId);
    Task AddAsync(ProductReview review);
    Task UpdateAsync(ProductReview review);
    Task RemoveAsync(ProductReview review);
    Task SaveChangesAsync();
}

