using Order.Domain.Entities;

namespace Order.Application.Interfaces;

public interface ICartRepository
{
    Task<List<CartItem>> GetByUserIdAsync(Guid userId);
    Task<CartItem?> GetByIdAsync(Guid id);
    Task<CartItem?> GetByUserAndProductAsync(Guid userId, Guid productId, Guid? variantId = null);
    Task AddAsync(CartItem item);
    Task UpdateAsync(CartItem item);
    Task RemoveAsync(CartItem item);
    Task ClearByUserIdAsync(Guid userId);
    Task SaveChangesAsync();
}

