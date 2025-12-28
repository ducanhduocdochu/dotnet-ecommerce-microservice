using Microsoft.EntityFrameworkCore;
using Order.Application.Interfaces;
using Order.Domain.Entities;
using Order.Infrastructure.DB;

namespace Order.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly OrderDbContext _context;

    public CartRepository(OrderDbContext context) => _context = context;

    public async Task<List<CartItem>> GetByUserIdAsync(Guid userId) =>
        await _context.CartItems.Where(c => c.UserId == userId).OrderByDescending(c => c.CreatedAt).ToListAsync();

    public async Task<CartItem?> GetByIdAsync(Guid id) =>
        await _context.CartItems.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<CartItem?> GetByUserAndProductAsync(Guid userId, Guid productId, Guid? variantId = null)
    {
        Console.WriteLine($"userId: {userId}");
        Console.WriteLine($"productId: {productId}");
        Console.WriteLine($"variantId: {variantId}");
        if (variantId.HasValue)
            return await _context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId && c.VariantId == variantId);
        return await _context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId && c.VariantId == null);
    }

    public async Task AddAsync(CartItem item) => await _context.CartItems.AddAsync(item);
    public Task UpdateAsync(CartItem item) { _context.CartItems.Update(item); return Task.CompletedTask; }
    public Task RemoveAsync(CartItem item) { _context.CartItems.Remove(item); return Task.CompletedTask; }
    
    public async Task ClearByUserIdAsync(Guid userId)
    {
        var items = await _context.CartItems.Where(c => c.UserId == userId).ToListAsync();
        _context.CartItems.RemoveRange(items);
    }

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}

