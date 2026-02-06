using Microsoft.EntityFrameworkCore;
using User.Application.Interfaces;
using User.Domain.Entities;
using User.Infrastructure.DB;

namespace User.Infrastructure.Repositories;

public class UserWishlistRepository : IUserWishlistRepository
{
    private readonly UserDbContext _context;

    public UserWishlistRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserWishlist>> GetByUserIdAsync(Guid userId, int page, int pageSize)
    {
        return await _context.UserWishlist
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountByUserIdAsync(Guid userId)
    {
        return await _context.UserWishlist.CountAsync(w => w.UserId == userId);
    }

    public async Task<UserWishlist?> GetByUserIdAndProductIdAsync(Guid userId, Guid productId)
    {
        return await _context.UserWishlist
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid productId)
    {
        return await _context.UserWishlist
            .AnyAsync(w => w.UserId == userId && w.ProductId == productId);
    }

    public async Task AddAsync(UserWishlist wishlist)
    {
        await _context.UserWishlist.AddAsync(wishlist);
    }

    public async Task RemoveAsync(UserWishlist wishlist)
    {
        _context.UserWishlist.Remove(wishlist);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

