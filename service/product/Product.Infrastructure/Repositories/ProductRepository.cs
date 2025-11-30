using Microsoft.EntityFrameworkCore;
using Product.Application.DTOs;
using Product.Application.Interfaces;
using Product.Domain.Entities;
using Product.Infrastructure.DB;

namespace Product.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context) => _context = context;

    public async Task<List<ProductEntity>> GetAllAsync(ProductFilterRequest filter)
    {
        var query = BuildFilterQuery(filter);
        query = ApplySort(query, filter.Sort);
        return await query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .Include(p => p.Images).ToListAsync();
    }

    public async Task<int> GetCountAsync(ProductFilterRequest filter) =>
        await BuildFilterQuery(filter).CountAsync();

    private IQueryable<ProductEntity> BuildFilterQuery(ProductFilterRequest filter)
    {
        var query = _context.Products.Where(p => p.IsActive && p.Status == "PUBLISHED");
        
        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filter.CategoryId);
        if (filter.BrandId.HasValue)
            query = query.Where(p => p.BrandId == filter.BrandId);
        if (filter.MinPrice.HasValue)
            query = query.Where(p => p.BasePrice >= filter.MinPrice || (p.SalePrice.HasValue && p.SalePrice >= filter.MinPrice));
        if (filter.MaxPrice.HasValue)
            query = query.Where(p => p.BasePrice <= filter.MaxPrice || (p.SalePrice.HasValue && p.SalePrice <= filter.MaxPrice));
        if (!string.IsNullOrEmpty(filter.Search))
            query = query.Where(p => p.Name.ToLower().Contains(filter.Search.ToLower()) || (p.Description != null && p.Description.ToLower().Contains(filter.Search.ToLower())));
        
        return query;
    }

    private IQueryable<ProductEntity> ApplySort(IQueryable<ProductEntity> query, string sort) => sort switch
    {
        "oldest" => query.OrderBy(p => p.CreatedAt),
        "price_asc" => query.OrderBy(p => p.SalePrice ?? p.BasePrice),
        "price_desc" => query.OrderByDescending(p => p.SalePrice ?? p.BasePrice),
        "best_selling" => query.OrderByDescending(p => p.SoldCount),
        "rating" => query.OrderByDescending(p => p.RatingAverage),
        _ => query.OrderByDescending(p => p.CreatedAt) // newest
    };

    public async Task<ProductEntity?> GetByIdAsync(Guid id) =>
        await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<ProductEntity?> GetByIdWithDetailsAsync(Guid id) =>
        await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Include(p => p.Variants).ThenInclude(v => v.Options)
            .Include(p => p.Attributes.OrderBy(a => a.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<ProductEntity?> GetBySlugAsync(string slug) =>
        await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Include(p => p.Variants).ThenInclude(v => v.Options)
            .Include(p => p.Attributes.OrderBy(a => a.SortOrder))
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive && p.Status == "PUBLISHED");

    public async Task<List<ProductEntity>> GetBySellerIdAsync(Guid sellerId, int page, int pageSize, string? status = null)
    {
        var query = _context.Products.Where(p => p.SellerId == sellerId);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);
        return await query.OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Include(p => p.Images).ToListAsync();
    }

    public async Task<int> GetCountBySellerIdAsync(Guid sellerId, string? status = null)
    {
        var query = _context.Products.Where(p => p.SellerId == sellerId);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);
        return await query.CountAsync();
    }

    public async Task<List<ProductEntity>> GetByCategoryIdAsync(Guid categoryId, int page, int pageSize) =>
        await _context.Products.Where(p => p.CategoryId == categoryId && p.IsActive && p.Status == "PUBLISHED")
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Include(p => p.Images).ToListAsync();

    public async Task<int> GetCountByCategoryIdAsync(Guid categoryId) =>
        await _context.Products.CountAsync(p => p.CategoryId == categoryId && p.IsActive && p.Status == "PUBLISHED");

    public async Task<List<ProductEntity>> GetFeaturedAsync(int limit) =>
        await _context.Products.Where(p => p.IsFeatured && p.IsActive && p.Status == "PUBLISHED")
            .OrderByDescending(p => p.CreatedAt).Take(limit)
            .Include(p => p.Images).ToListAsync();

    public async Task<List<ProductEntity>> SearchAsync(string keyword, int page, int pageSize) =>
        await _context.Products.Where(p => p.IsActive && p.Status == "PUBLISHED" &&
            (p.Name.ToLower().Contains(keyword.ToLower()) || (p.Description != null && p.Description.ToLower().Contains(keyword.ToLower()))))
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Include(p => p.Images).ToListAsync();

    public async Task<int> SearchCountAsync(string keyword) =>
        await _context.Products.CountAsync(p => p.IsActive && p.Status == "PUBLISHED" &&
            (p.Name.ToLower().Contains(keyword.ToLower()) || (p.Description != null && p.Description.ToLower().Contains(keyword.ToLower()))));

    public async Task<List<ProductEntity>> GetPendingAsync(int page, int pageSize) =>
        await _context.Products.Where(p => p.Status == "PENDING")
            .OrderBy(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Include(p => p.Images).ToListAsync();

    public async Task<int> GetPendingCountAsync() =>
        await _context.Products.CountAsync(p => p.Status == "PENDING");

    public async Task AddAsync(ProductEntity product) => await _context.Products.AddAsync(product);
    public Task UpdateAsync(ProductEntity product) { _context.Products.Update(product); return Task.CompletedTask; }
    public Task RemoveAsync(ProductEntity product) { _context.Products.Remove(product); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}

