using Product.Application.DTOs;
using Product.Application.Services;
using Shared.Caching.Interfaces;
using Shared.Caching.Constants;

namespace Product.Api.Services;

/// <summary>
/// Cached wrapper for ProductService - implements Cache-Aside pattern
/// </summary>
public class CachedProductService
{
    private readonly ProductService _productService;
    private readonly ICacheService _cache;
    private readonly ILogger<CachedProductService> _logger;

    public CachedProductService(
        ProductService productService,
        ICacheService cache,
        ILogger<CachedProductService> logger)
    {
        _productService = productService;
        _cache = cache;
        _logger = logger;
    }

    // ============ Category Methods (Cache 24h) ============
    public async Task<List<CategoryTreeResponse>> GetCategoryTreeAsync()
    {
        return await _cache.GetOrSetAsync(
            CacheKeys.Category.Tree(),
            async () => await _productService.GetCategoryTreeAsync(),
            CacheTTL.Category
        );
    }

    public async Task<CategoryResponse?> GetCategoryByIdAsync(Guid id)
    {
        return await _cache.GetOrSetAsync(
            CacheKeys.Category.ById(id),
            async () => await _productService.GetCategoryByIdAsync(id),
            CacheTTL.Category
        );
    }

    public async Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var result = await _productService.CreateCategoryAsync(request);
        await InvalidateCategoryCache();
        return result;
    }

    public async Task<CategoryResponse?> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request)
    {
        var result = await _productService.UpdateCategoryAsync(id, request);
        if (result != null)
        {
            await InvalidateCategoryCache();
        }
        return result;
    }

    public async Task<bool> DeleteCategoryAsync(Guid id)
    {
        var result = await _productService.DeleteCategoryAsync(id);
        if (result)
        {
            await InvalidateCategoryCache();
        }
        return result;
    }

    // ============ Brand Methods (Cache 24h) ============
    public async Task<PagedResponse<BrandResponse>> GetBrandsAsync(int page, int pageSize, string? search)
    {
        // Don't cache search results
        if (!string.IsNullOrEmpty(search))
        {
            return await _productService.GetBrandsAsync(page, pageSize, search);
        }

        var cacheKey = $"brands:list:p{page}:s{pageSize}";
        return await _cache.GetOrSetAsync(
            cacheKey,
            async () => await _productService.GetBrandsAsync(page, pageSize, null),
            CacheTTL.Brand
        );
    }

    public async Task<BrandResponse?> GetBrandByIdAsync(Guid id)
    {
        return await _cache.GetOrSetAsync(
            CacheKeys.Brand.ById(id),
            async () => await _productService.GetBrandByIdAsync(id),
            CacheTTL.Brand
        );
    }

    public async Task<BrandResponse> CreateBrandAsync(CreateBrandRequest request)
    {
        var result = await _productService.CreateBrandAsync(request);
        await InvalidateBrandCache();
        return result;
    }

    public async Task<BrandResponse?> UpdateBrandAsync(Guid id, UpdateBrandRequest request)
    {
        var result = await _productService.UpdateBrandAsync(id, request);
        if (result != null)
        {
            await InvalidateBrandCache();
        }
        return result;
    }

    public async Task<bool> DeleteBrandAsync(Guid id)
    {
        var result = await _productService.DeleteBrandAsync(id);
        if (result)
        {
            await InvalidateBrandCache();
        }
        return result;
    }

    // ============ Product Methods (Cache 1h for detail, 15m for list) ============
    public async Task<PagedResponse<ProductListResponse>> GetProductsAsync(ProductFilterRequest filter)
    {
        // Cache per filter combination
        var cacheKey = CacheKeys.Product.List(filter.Page, filter.PageSize);
        
        // For now, don't cache filtered results (complex to invalidate)
        // Only cache default listing
        if (filter.CategoryId.HasValue || filter.BrandId.HasValue || !string.IsNullOrEmpty(filter.Status))
        {
            return await _productService.GetProductsAsync(filter);
        }

        return await _cache.GetOrSetAsync(
            cacheKey,
            async () => await _productService.GetProductsAsync(filter),
            CacheTTL.ProductList
        );
    }

    public async Task<ProductDetailResponse?> GetProductByIdAsync(Guid id)
    {
        return await _cache.GetOrSetAsync(
            CacheKeys.Product.ById(id),
            async () => await _productService.GetProductByIdAsync(id),
            CacheTTL.Product
        );
    }

    public async Task<ProductDetailResponse?> GetProductBySlugAsync(string slug)
    {
        return await _cache.GetOrSetAsync(
            CacheKeys.Product.BySlug(slug),
            async () => await _productService.GetProductBySlugAsync(slug),
            CacheTTL.Product
        );
    }

    public async Task<PagedResponse<ProductListResponse>> GetProductsBySellerAsync(Guid sellerId, int page, int pageSize, string? status)
    {
        // Don't cache seller-specific listings (too many variants)
        return await _productService.GetProductsBySellerAsync(sellerId, page, pageSize, status);
    }

    public async Task<List<ProductListResponse>> GetFeaturedProductsAsync(int limit)
    {
        return await _cache.GetOrSetAsync(
            CacheKeys.Product.Featured(),
            async () => await _productService.GetFeaturedProductsAsync(limit),
            CacheTTL.ProductFeatured
        );
    }

    public async Task<PagedResponse<ProductListResponse>> SearchProductsAsync(string keyword, int page, int pageSize)
    {
        // Don't cache search results (too many variants, real-time expectations)
        return await _productService.SearchProductsAsync(keyword, page, pageSize);
    }

    public async Task<ProductDetailResponse> CreateProductAsync(Guid sellerId, CreateProductRequest request)
    {
        var result = await _productService.CreateProductAsync(sellerId, request);
        await InvalidateProductListCache();
        return result;
    }

    public async Task<ProductDetailResponse?> UpdateProductAsync(Guid id, Guid sellerId, UpdateProductRequest request)
    {
        var result = await _productService.UpdateProductAsync(id, sellerId, request);
        if (result != null)
        {
            await InvalidateProductCache(id);
        }
        return result;
    }

    public async Task<bool> DeleteProductAsync(Guid id, Guid sellerId)
    {
        var result = await _productService.DeleteProductAsync(id, sellerId);
        if (result)
        {
            await InvalidateProductCache(id);
        }
        return result;
    }

    public async Task<bool> SubmitProductAsync(Guid id, Guid sellerId)
    {
        var result = await _productService.SubmitProductAsync(id, sellerId);
        if (result)
        {
            await InvalidateProductCache(id);
        }
        return result;
    }

    public async Task<PagedResponse<ProductListResponse>> GetPendingProductsAsync(int page, int pageSize)
    {
        // Don't cache admin pending lists (needs real-time)
        return await _productService.GetPendingProductsAsync(page, pageSize);
    }

    public async Task<bool> ApproveProductAsync(Guid id)
    {
        var result = await _productService.ApproveProductAsync(id);
        if (result)
        {
            await InvalidateProductCache(id);
        }
        return result;
    }

    public async Task<bool> RejectProductAsync(Guid id)
    {
        var result = await _productService.RejectProductAsync(id);
        if (result)
        {
            await InvalidateProductCache(id);
        }
        return result;
    }

    public async Task<bool> SetFeaturedAsync(Guid id, bool featured)
    {
        var result = await _productService.SetFeaturedAsync(id, featured);
        if (result)
        {
            await InvalidateProductCache(id);
            await _cache.RemoveAsync(CacheKeys.Product.Featured());
        }
        return result;
    }

    // ============ Review Methods ============
    public async Task<PagedResponse<ReviewResponse>> GetReviewsAsync(Guid productId, int page, int pageSize, int? rating)
    {
        // Only cache default view (no rating filter)
        if (rating.HasValue)
        {
            return await _productService.GetReviewsAsync(productId, page, pageSize, rating);
        }

        var cacheKey = $"product:reviews:{productId}:p{page}:s{pageSize}";
        return await _cache.GetOrSetAsync(
            cacheKey,
            async () => await _productService.GetReviewsAsync(productId, page, pageSize, null),
            TimeSpan.FromMinutes(15)
        );
    }

    public async Task<ReviewSummaryResponse> GetReviewSummaryAsync(Guid productId)
    {
        var cacheKey = $"product:rating:{productId}";
        return await _cache.GetOrSetAsync(
            cacheKey,
            async () => await _productService.GetReviewSummaryAsync(productId),
            TimeSpan.FromMinutes(15)
        );
    }

    public async Task<ReviewResponse?> CreateReviewAsync(Guid productId, Guid userId, CreateReviewRequest request)
    {
        var result = await _productService.CreateReviewAsync(productId, userId, request);
        if (result != null)
        {
            await InvalidateReviewCache(productId);
        }
        return result;
    }

    // ============ Cache Invalidation Helpers ============
    private async Task InvalidateCategoryCache()
    {
        _logger.LogInformation("🧹 Invalidating category cache");
        await _cache.RemoveByPatternAsync(CacheKeys.Category.AllPattern());
    }

    private async Task InvalidateBrandCache()
    {
        _logger.LogInformation("🧹 Invalidating brand cache");
        await _cache.RemoveByPatternAsync(CacheKeys.Brand.AllPattern());
    }

    private async Task InvalidateProductCache(Guid productId)
    {
        _logger.LogInformation("🧹 Invalidating product cache for ID: {ProductId}", productId);
        await _cache.RemoveAsync(CacheKeys.Product.ById(productId));
        await InvalidateProductListCache();
    }

    private async Task InvalidateProductListCache()
    {
        _logger.LogInformation("🧹 Invalidating product list cache");
        await _cache.RemoveByPatternAsync(CacheKeys.Product.AllPattern());
    }

    private async Task InvalidateReviewCache(Guid productId)
    {
        _logger.LogInformation("🧹 Invalidating review cache for product: {ProductId}", productId);
        await _cache.RemoveByPatternAsync($"product:reviews:{productId}:*");
        await _cache.RemoveAsync($"product:rating:{productId}");
        await _cache.RemoveAsync(CacheKeys.Product.ById(productId)); // Also invalidate product (rating changed)
    }
}

