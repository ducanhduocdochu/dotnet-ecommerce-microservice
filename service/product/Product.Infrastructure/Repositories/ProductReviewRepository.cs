using Microsoft.EntityFrameworkCore;
using Product.Application.Interfaces;
using Product.Domain.Entities;
using Product.Infrastructure.DB;

namespace Product.Infrastructure.Repositories;

public class ProductReviewRepository : IProductReviewRepository
{
    private readonly ProductDbContext _context;

    public ProductReviewRepository(ProductDbContext context) => _context = context;

    public async Task<List<ProductReview>> GetByProductIdAsync(Guid productId, int page, int pageSize, int? rating = null)
    {
        var query = _context.ProductReviews.Where(r => r.ProductId == productId && r.IsApproved);
        if (rating.HasValue)
            query = query.Where(r => r.Rating == rating);
        return await query.OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> GetCountByProductIdAsync(Guid productId, int? rating = null)
    {
        var query = _context.ProductReviews.Where(r => r.ProductId == productId && r.IsApproved);
        if (rating.HasValue)
            query = query.Where(r => r.Rating == rating);
        return await query.CountAsync();
    }

    public async Task<ProductReview?> GetByIdAsync(Guid id) =>
        await _context.ProductReviews.FirstOrDefaultAsync(r => r.Id == id);

    public async Task<ProductReview?> GetByUserAndProductAsync(Guid userId, Guid productId) =>
        await _context.ProductReviews.FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId);

    public async Task<List<ProductReview>> GetByUserIdAsync(Guid userId) =>
        await _context.ProductReviews.Where(r => r.UserId == userId).ToListAsync();

    public async Task<Dictionary<int, int>> GetRatingDistributionAsync(Guid productId)
    {
        var reviews = await _context.ProductReviews
            .Where(r => r.ProductId == productId && r.IsApproved)
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync();

        var distribution = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 } };
        foreach (var r in reviews)
            distribution[r.Rating] = r.Count;
        return distribution;
    }

    public async Task AddAsync(ProductReview review) => await _context.ProductReviews.AddAsync(review);
    public Task UpdateAsync(ProductReview review) { _context.ProductReviews.Update(review); return Task.CompletedTask; }
    public Task RemoveAsync(ProductReview review) { _context.ProductReviews.Remove(review); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}

