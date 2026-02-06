using User.Domain.Entities;

namespace User.Application.Interfaces;

public interface IUserWishlistRepository
{
    Task<List<UserWishlist>> GetByUserIdAsync(Guid userId, int page, int pageSize);
    Task<int> GetCountByUserIdAsync(Guid userId);
    Task<UserWishlist?> GetByUserIdAndProductIdAsync(Guid userId, Guid productId);
    Task<bool> ExistsAsync(Guid userId, Guid productId);
    Task AddAsync(UserWishlist wishlist);
    Task RemoveAsync(UserWishlist wishlist);
    Task SaveChangesAsync();
}

