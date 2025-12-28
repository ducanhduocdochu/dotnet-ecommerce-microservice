using Product.Application.DTOs;
using Product.Application.Interfaces;
using Product.Domain.Entities;

namespace Product.Application.Services;

public class ProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly IProductImageRepository _imageRepository;
    private readonly IProductVariantRepository _variantRepository;
    private readonly IProductReviewRepository _reviewRepository;
    private readonly ITagRepository _tagRepository;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IBrandRepository brandRepository,
        IProductImageRepository imageRepository,
        IProductVariantRepository variantRepository,
        IProductReviewRepository reviewRepository,
        ITagRepository tagRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _brandRepository = brandRepository;
        _imageRepository = imageRepository;
        _variantRepository = variantRepository;
        _reviewRepository = reviewRepository;
        _tagRepository = tagRepository;
    }

    // ============ Category Methods ============
    public async Task<List<CategoryTreeResponse>> GetCategoryTreeAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        return BuildCategoryTree(categories, null);
    }

    private List<CategoryTreeResponse> BuildCategoryTree(List<Category> all, Guid? parentId)
    {
        return all.Where(c => c.ParentId == parentId && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategoryTreeResponse(
                c.Id, c.Name, c.Slug, c.Description, c.ImageUrl, c.ParentId, c.SortOrder,
                BuildCategoryTree(all, c.Id)
            )).ToList();
    }

    public async Task<CategoryResponse?> GetCategoryByIdAsync(Guid id)
    {
        var cat = await _categoryRepository.GetByIdAsync(id);
        if (cat == null) return null;
        return new CategoryResponse(cat.Id, cat.Name, cat.Slug, cat.Description, cat.ImageUrl, cat.ParentId, cat.IsActive, cat.SortOrder, cat.CreatedAt, cat.UpdatedAt);
    }

    public async Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var category = new Category(request.Name, request.Slug, request.Description, request.ImageUrl, request.ParentId, request.SortOrder);
        await _categoryRepository.AddAsync(category);
        await _categoryRepository.SaveChangesAsync();
        return new CategoryResponse(category.Id, category.Name, category.Slug, category.Description, category.ImageUrl, category.ParentId, category.IsActive, category.SortOrder, category.CreatedAt, category.UpdatedAt);
    }

    public async Task<CategoryResponse?> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null) return null;
        category.Update(request.Name, request.Slug, request.Description, request.ImageUrl, request.ParentId, request.SortOrder, request.IsActive);
        await _categoryRepository.UpdateAsync(category);
        await _categoryRepository.SaveChangesAsync();
        return new CategoryResponse(category.Id, category.Name, category.Slug, category.Description, category.ImageUrl, category.ParentId, category.IsActive, category.SortOrder, category.CreatedAt, category.UpdatedAt);
    }

    public async Task<bool> DeleteCategoryAsync(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null) return false;
        await _categoryRepository.RemoveAsync(category);
        await _categoryRepository.SaveChangesAsync();
        return true;
    }

    // ============ Brand Methods ============
    public async Task<PagedResponse<BrandResponse>> GetBrandsAsync(int page, int pageSize, string? search)
    {
        var brands = await _brandRepository.GetAllAsync(page, pageSize, search);
        var total = await _brandRepository.GetCountAsync(search);
        return new PagedResponse<BrandResponse>(
            brands.Select(b => new BrandResponse(b.Id, b.Name, b.Slug, b.Description, b.LogoUrl, b.WebsiteUrl, b.IsActive, b.CreatedAt, b.UpdatedAt)).ToList(),
            total, page, pageSize
        );
    }

    public async Task<BrandResponse?> GetBrandByIdAsync(Guid id)
    {
        var brand = await _brandRepository.GetByIdAsync(id);
        if (brand == null) return null;
        return new BrandResponse(brand.Id, brand.Name, brand.Slug, brand.Description, brand.LogoUrl, brand.WebsiteUrl, brand.IsActive, brand.CreatedAt, brand.UpdatedAt);
    }

    public async Task<BrandResponse> CreateBrandAsync(CreateBrandRequest request)
    {
        var brand = new Brand(request.Name, request.Slug, request.Description, request.LogoUrl, request.WebsiteUrl);
        await _brandRepository.AddAsync(brand);
        await _brandRepository.SaveChangesAsync();
        return new BrandResponse(brand.Id, brand.Name, brand.Slug, brand.Description, brand.LogoUrl, brand.WebsiteUrl, brand.IsActive, brand.CreatedAt, brand.UpdatedAt);
    }

    public async Task<BrandResponse?> UpdateBrandAsync(Guid id, UpdateBrandRequest request)
    {
        var brand = await _brandRepository.GetByIdAsync(id);
        if (brand == null) return null;
        brand.Update(request.Name, request.Slug, request.Description, request.LogoUrl, request.WebsiteUrl, request.IsActive);
        await _brandRepository.UpdateAsync(brand);
        await _brandRepository.SaveChangesAsync();
        return new BrandResponse(brand.Id, brand.Name, brand.Slug, brand.Description, brand.LogoUrl, brand.WebsiteUrl, brand.IsActive, brand.CreatedAt, brand.UpdatedAt);
    }

    public async Task<bool> DeleteBrandAsync(Guid id)
    {
        var brand = await _brandRepository.GetByIdAsync(id);
        if (brand == null) return false;
        await _brandRepository.RemoveAsync(brand);
        await _brandRepository.SaveChangesAsync();
        return true;
    }

    // ============ Product Methods ============
    public async Task<PagedResponse<ProductListResponse>> GetProductsAsync(ProductFilterRequest filter)
    {
        var products = await _productRepository.GetAllAsync(filter);
        var total = await _productRepository.GetCountAsync(filter);
        return new PagedResponse<ProductListResponse>(
            products.Select(p => MapToListResponse(p)).ToList(),
            total, filter.Page, filter.PageSize
        );
    }

    public async Task<ProductDetailResponse?> GetProductByIdAsync(Guid id)
    {
        var product = await _productRepository.GetByIdWithDetailsAsync(id);
        if (product == null) return null;
        product.IncrementViewCount();
        await _productRepository.SaveChangesAsync();
        return await MapToDetailResponse(product);
    }

    public async Task<ProductDetailResponse?> GetProductBySlugAsync(string slug)
    {
        var product = await _productRepository.GetBySlugAsync(slug);
        if (product == null) return null;
        product.IncrementViewCount();
        await _productRepository.SaveChangesAsync();
        return await MapToDetailResponse(product);
    }

    public async Task<PagedResponse<ProductListResponse>> GetProductsBySellerAsync(Guid sellerId, int page, int pageSize, string? status)
    {
        var products = await _productRepository.GetBySellerIdAsync(sellerId, page, pageSize, status);
        var total = await _productRepository.GetCountBySellerIdAsync(sellerId, status);
        return new PagedResponse<ProductListResponse>(
            products.Select(p => MapToListResponse(p)).ToList(),
            total, page, pageSize
        );
    }

    public async Task<List<ProductListResponse>> GetFeaturedProductsAsync(int limit)
    {
        var products = await _productRepository.GetFeaturedAsync(limit);
        return products.Select(p => MapToListResponse(p)).ToList();
    }

    public async Task<PagedResponse<ProductListResponse>> SearchProductsAsync(string keyword, int page, int pageSize)
    {
        var products = await _productRepository.SearchAsync(keyword, page, pageSize);
        var total = await _productRepository.SearchCountAsync(keyword);
        return new PagedResponse<ProductListResponse>(
            products.Select(p => MapToListResponse(p)).ToList(),
            total, page, pageSize
        );
    }

    public async Task<ProductDetailResponse> CreateProductAsync(Guid sellerId, CreateProductRequest request)
    {
        Console.WriteLine($"Request: {request.CategoryId}");
        var product = new ProductEntity(sellerId, request.Name, request.Slug, request.BasePrice, request.SellerName, request.SellerAvatar, request.CategoryId, request.BrandId, request.Description, request.ShortDescription, request.Sku);
        Console.WriteLine($"Request: {request.BrandId}");
        await _productRepository.AddAsync(product);
        await _productRepository.SaveChangesAsync();

        Console.WriteLine($"Product: {product.CategoryId}");
        Console.WriteLine($"Product: {product.BrandId}");

        // Add images
        if (request.Images != null)
        {
            foreach (var img in request.Images)
            {
                var image = new ProductImage(product.Id, img.ImageUrl, img.AltText, img.IsPrimary, img.SortOrder);
                await _imageRepository.AddAsync(image);
            }
            await _imageRepository.SaveChangesAsync();
        }

        Console.WriteLine($"Images: {product.Images.Count}");

        // Add variants
        if (request.Variants != null)
        {
            foreach (var v in request.Variants)
            {
                var variant = new ProductVariant(product.Id, v.Name, v.Sku, v.Price, v.Quantity, v.ImageUrl);
                await _variantRepository.AddAsync(variant);
            }
            await _variantRepository.SaveChangesAsync();
        }

        return (await MapToDetailResponse(product))!;
    }

    public async Task<ProductDetailResponse?> UpdateProductAsync(Guid id, Guid sellerId, UpdateProductRequest request)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null || product.SellerId != sellerId) return null;

        product.Update(request.Name, request.Slug, request.Description, request.ShortDescription, request.CategoryId, request.BrandId, request.BasePrice, request.SalePrice, request.CostPrice, request.Sku, request.Quantity, request.Weight, request.Length, request.Width, request.Height, request.IsDigital);
        await _productRepository.UpdateAsync(product);
        await _productRepository.SaveChangesAsync();
        return await MapToDetailResponse(product);
    }

    public async Task<bool> DeleteProductAsync(Guid id, Guid sellerId)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null || product.SellerId != sellerId) return false;
        await _productRepository.RemoveAsync(product);
        await _productRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SubmitProductAsync(Guid id, Guid sellerId)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null || product.SellerId != sellerId) return false;
        product.Submit();
        await _productRepository.SaveChangesAsync();
        return true;
    }

    // Admin methods
    public async Task<PagedResponse<ProductListResponse>> GetPendingProductsAsync(int page, int pageSize)
    {
        var products = await _productRepository.GetPendingAsync(page, pageSize);
        var total = await _productRepository.GetPendingCountAsync();
        return new PagedResponse<ProductListResponse>(
            products.Select(p => MapToListResponse(p)).ToList(),
            total, page, pageSize
        );
    }

    public async Task<bool> ApproveProductAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null) return false;
        product.Approve();
        await _productRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectProductAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null) return false;
        product.Reject();
        await _productRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetFeaturedAsync(Guid id, bool featured)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null) return false;
        product.SetFeatured(featured);
        await _productRepository.SaveChangesAsync();
        return true;
    }

    // ============ Review Methods ============
    public async Task<PagedResponse<ReviewResponse>> GetReviewsAsync(Guid productId, int page, int pageSize, int? rating)
    {
        var reviews = await _reviewRepository.GetByProductIdAsync(productId, page, pageSize, rating);
        var total = await _reviewRepository.GetCountByProductIdAsync(productId, rating);
        return new PagedResponse<ReviewResponse>(
            reviews.Select(r => new ReviewResponse(r.Id, r.ProductId, r.UserId, r.ReviewerName, r.ReviewerAvatar, r.Rating, r.Title, r.Content, r.Images, r.IsVerifiedPurchase, r.HelpfulCount, r.SellerReply, r.SellerReplyAt, r.CreatedAt, r.UpdatedAt)).ToList(),
            total, page, pageSize
        );
    }

    public async Task<ReviewSummaryResponse> GetReviewSummaryAsync(Guid productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        var distribution = await _reviewRepository.GetRatingDistributionAsync(productId);
        return new ReviewSummaryResponse(product?.RatingAverage ?? 0, product?.RatingCount ?? 0, distribution);
    }

    public async Task<ReviewResponse?> CreateReviewAsync(Guid productId, Guid userId, CreateReviewRequest request)
    {
        var existing = await _reviewRepository.GetByUserAndProductAsync(userId, productId);
        if (existing != null) return null; // Already reviewed

        var review = new ProductReview(productId, userId, request.Rating, request.ReviewerName, request.ReviewerAvatar, request.Title, request.Content, null, request.Images);
        await _reviewRepository.AddAsync(review);
        await _reviewRepository.SaveChangesAsync();

        // Update product rating
        await UpdateProductRatingAsync(productId);

        return new ReviewResponse(review.Id, review.ProductId, review.UserId, review.ReviewerName, review.ReviewerAvatar, review.Rating, review.Title, review.Content, review.Images, review.IsVerifiedPurchase, review.HelpfulCount, review.SellerReply, review.SellerReplyAt, review.CreatedAt, review.UpdatedAt);
    }

    public async Task<bool> ReplyToReviewAsync(Guid reviewId, string reply)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review == null) return false;
        review.AddSellerReply(reply);
        await _reviewRepository.SaveChangesAsync();
        return true;
    }

    private async Task UpdateProductRatingAsync(Guid productId)
    {
        var distribution = await _reviewRepository.GetRatingDistributionAsync(productId);
        var totalCount = distribution.Values.Sum();
        var totalScore = distribution.Sum(d => d.Key * d.Value);
        var average = totalCount > 0 ? (decimal)totalScore / totalCount : 0;

        var product = await _productRepository.GetByIdAsync(productId);
        product?.UpdateRating(Math.Round(average, 2), totalCount);
        await _productRepository.SaveChangesAsync();
    }

    // ============ Helper Methods ============
    private ProductListResponse MapToListResponse(ProductEntity p)
    {
        var primaryImage = p.Images?.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? p.Images?.FirstOrDefault()?.ImageUrl;
        return new ProductListResponse(p.Id, p.SellerId, p.SellerName, p.SellerAvatar, p.Name, p.Slug, p.ShortDescription, p.BasePrice, p.SalePrice, primaryImage, p.RatingAverage, p.SoldCount, p.Status);
    }

    private async Task<ProductDetailResponse?> MapToDetailResponse(ProductEntity p)
    {
        var tags = await _tagRepository.GetByProductIdAsync(p.Id);
        CategoryResponse? categoryResponse = null;
        BrandResponse? brandResponse = null;

        if (p.Category != null)
            categoryResponse = new CategoryResponse(p.Category.Id, p.Category.Name, p.Category.Slug, p.Category.Description, p.Category.ImageUrl, p.Category.ParentId, p.Category.IsActive, p.Category.SortOrder, p.Category.CreatedAt, p.Category.UpdatedAt);
        
        if (p.Brand != null)
            brandResponse = new BrandResponse(p.Brand.Id, p.Brand.Name, p.Brand.Slug, p.Brand.Description, p.Brand.LogoUrl, p.Brand.WebsiteUrl, p.Brand.IsActive, p.Brand.CreatedAt, p.Brand.UpdatedAt);

        return new ProductDetailResponse(
            p.Id, p.SellerId, p.SellerName, p.SellerAvatar, p.Name, p.Slug, p.Description, p.ShortDescription, p.BasePrice, p.SalePrice, p.Currency, p.Quantity, p.Sku, p.Weight, p.IsDigital, p.IsFeatured, p.Status, p.RatingAverage, p.RatingCount, p.SoldCount, p.ViewCount,
            categoryResponse, brandResponse,
            p.Images?.Select(i => new ProductImageResponse(i.Id, i.ImageUrl, i.AltText, i.IsPrimary, i.SortOrder)).ToList() ?? new(),
            p.Variants?.Select(v => new ProductVariantResponse(v.Id, v.Name, v.Sku, v.Price, v.Quantity, v.ImageUrl, v.IsActive, v.Options?.Select(o => new VariantOptionResponse(o.OptionName, o.OptionValue)).ToList() ?? new())).ToList() ?? new(),
            p.Attributes?.Select(a => new ProductAttributeResponse(a.Id, a.AttributeName, a.AttributeValue, a.SortOrder)).ToList() ?? new(),
            tags.Select(t => t.Name).ToList(),
            p.CreatedAt, p.UpdatedAt
        );
    }
}

