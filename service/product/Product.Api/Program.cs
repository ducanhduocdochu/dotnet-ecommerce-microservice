using Microsoft.EntityFrameworkCore;
using Product.Api.Consumers;
using Product.Api.Services;
using Product.Application.DTOs;
using Product.Application.Interfaces;
using Product.Application.Services;
using Product.Infrastructure.DB;
using Product.Infrastructure.Repositories;
using Shared.Messaging.Extensions;
using Shared.Caching.Extensions;
// upload image

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DBConnectParam")));

// Redis Caching
builder.Services.AddRedisCaching(builder.Configuration);

// RabbitMQ for event consuming
builder.Services.AddRabbitMQ(builder.Configuration);
builder.Services.AddHostedService<UserProfileUpdatedConsumer>();

// Repositories
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IBrandRepository, BrandRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductImageRepository, ProductImageRepository>();
builder.Services.AddScoped<IProductVariantRepository, ProductVariantRepository>();
builder.Services.AddScoped<IProductReviewRepository, ProductReviewRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();

// Services
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CachedProductService>();

// Authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        var secret = builder.Configuration["Jwt:Secret"] ?? "ducanhdeptrai123_ducanhdeptrai123";
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.ASCII.GetBytes(secret))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("SellerOnly", policy => policy.RequireRole("Seller", "Admin"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ============================================
// Check all service connections on startup
// ============================================
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    // 1. Check PostgreSQL connection
    var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    try
    {
        if (await dbContext.Database.CanConnectAsync())
        {
            logger.LogInformation("✅ PostgreSQL connection successful!");
        }
        else
        {
            logger.LogError("❌ PostgreSQL connection failed!");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ PostgreSQL connection error: {Message}", ex.Message);
    }

    // 2. Check Redis connection
    try
    {
        var redis = scope.ServiceProvider.GetService<StackExchange.Redis.IConnectionMultiplexer>();
        if (redis != null && redis.IsConnected)
        {
            logger.LogInformation("✅ Redis connection successful!");
        }
        else
        {
            logger.LogWarning("⚠️ Redis not connected - caching will be unavailable");
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "⚠️ Redis connection error: {Message}", ex.Message);
    }

    // 3. Check RabbitMQ connection
    try
    {
        var rabbitMQ = scope.ServiceProvider.GetService<Shared.Messaging.RabbitMQ.IRabbitMQConnection>();
        if (rabbitMQ != null)
        {
            if (rabbitMQ.TryConnect())
            {
                logger.LogInformation("✅ RabbitMQ connection successful!");
            }
            else
            {
                logger.LogWarning("⚠️ RabbitMQ not connected - messaging will be unavailable");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "⚠️ RabbitMQ connection error: {Message}", ex.Message);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Helper to get user ID from JWT
Guid? GetUserId(HttpContext ctx) =>
    Guid.TryParse(ctx.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

// ============================================
// Category APIs (Public)
// ============================================

app.MapGet("/api/categories", async (CachedProductService cachedService) =>
    Results.Ok(await cachedService.GetCategoryTreeAsync()))
.WithName("GetCategories").WithTags("Categories");

app.MapGet("/api/categories/{id}", async (Guid id, CachedProductService cachedService) =>
{
    var category = await cachedService.GetCategoryByIdAsync(id);
    return category == null ? Results.NotFound() : Results.Ok(category);
}).WithName("GetCategoryById").WithTags("Categories");

app.MapPost("/api/categories", async (CachedProductService cachedService, CreateCategoryRequest request) =>
{
    var category = await cachedService.CreateCategoryAsync(request);
    return Results.Created($"/api/categories/{category.Id}", category);
}).RequireAuthorization("AdminOnly").WithName("CreateCategory").WithTags("Categories");

app.MapPut("/api/categories/{id}", async (Guid id, CachedProductService cachedService, UpdateCategoryRequest request) =>
{
    var category = await cachedService.UpdateCategoryAsync(id, request);
    return category == null ? Results.NotFound() : Results.Ok(category);
}).RequireAuthorization("AdminOnly").WithName("UpdateCategory").WithTags("Categories");

app.MapDelete("/api/categories/{id}", async (Guid id, CachedProductService cachedService) =>
{
    var success = await cachedService.DeleteCategoryAsync(id);
    return success ? Results.Ok(new { message = "Category deleted" }) : Results.NotFound();
}).RequireAuthorization("AdminOnly").WithName("DeleteCategory").WithTags("Categories");

// ============================================
// Brand APIs (Public)
// ============================================

app.MapGet("/api/brands", async (ProductService service, int page = 1, int pageSize = 20, string? search = null) =>
    Results.Ok(await service.GetBrandsAsync(page, pageSize, search)))
.WithName("GetBrands").WithTags("Brands");

app.MapGet("/api/brands/{id}", async (Guid id, ProductService service) =>
{
    var brand = await service.GetBrandByIdAsync(id);
    return brand == null ? Results.NotFound() : Results.Ok(brand);
}).WithName("GetBrandById").WithTags("Brands");

app.MapPost("/api/brands", async (ProductService service, CreateBrandRequest request) =>
{
    var brand = await service.CreateBrandAsync(request);
    return Results.Created($"/api/brands/{brand.Id}", brand);
}).RequireAuthorization("AdminOnly").WithName("CreateBrand").WithTags("Brands");

app.MapPut("/api/brands/{id}", async (Guid id, ProductService service, UpdateBrandRequest request) =>
{
    var brand = await service.UpdateBrandAsync(id, request);
    return brand == null ? Results.NotFound() : Results.Ok(brand);
}).RequireAuthorization("AdminOnly").WithName("UpdateBrand").WithTags("Brands");

app.MapDelete("/api/brands/{id}", async (Guid id, ProductService service) =>
{
    var success = await service.DeleteBrandAsync(id);
    return success ? Results.Ok(new { message = "Brand deleted" }) : Results.NotFound();
}).RequireAuthorization("AdminOnly").WithName("DeleteBrand").WithTags("Brands");

// ============================================
// Product APIs (Public)
// ============================================

app.MapGet("/api/products", async (CachedProductService cachedService, Guid? category_id, Guid? brand_id, decimal? min_price, decimal? max_price, string? search, string sort = "newest", int page = 1, int pageSize = 20) =>
{
    var filter = new ProductFilterRequest(category_id, brand_id, min_price, max_price, search, null, sort, page, pageSize);
    return Results.Ok(await cachedService.GetProductsAsync(filter));
}).WithName("GetProducts").WithTags("Products");

app.MapGet("/api/products/featured", async (CachedProductService cachedService, int limit = 10) =>
    Results.Ok(await cachedService.GetFeaturedProductsAsync(limit)))
.WithName("GetFeaturedProducts").WithTags("Products");

app.MapGet("/api/products/search", async (CachedProductService cachedService, string q, int page = 1, int pageSize = 20) =>
    Results.Ok(await cachedService.SearchProductsAsync(q, page, pageSize)))
.WithName("SearchProducts").WithTags("Products");

app.MapGet("/api/products/{id}", async (Guid id, CachedProductService cachedService) =>
{
    var product = await cachedService.GetProductByIdAsync(id);
    return product == null ? Results.NotFound() : Results.Ok(product);
}).WithName("GetProductById").WithTags("Products");

app.MapGet("/api/products/slug/{slug}", async (string slug, CachedProductService cachedService) =>
{
    var product = await cachedService.GetProductBySlugAsync(slug);
    return product == null ? Results.NotFound() : Results.Ok(product);
}).WithName("GetProductBySlug").WithTags("Products");

// ============================================
// Product Management APIs (Seller)
// ============================================

app.MapGet("/api/products/me", async (ProductService service, HttpContext ctx, string? status = null, int page = 1, int pageSize = 20) =>
{
    var sellerId = GetUserId(ctx);
    if (sellerId == null) return Results.Unauthorized();
    return Results.Ok(await service.GetProductsBySellerAsync(sellerId.Value, page, pageSize, status));
}).RequireAuthorization("SellerOnly").WithName("GetMyProducts").WithTags("Product Management");

app.MapPost("/api/products", async (ProductService service, CreateProductRequest request, HttpContext ctx) =>
{
    var sellerId = GetUserId(ctx);
    if (sellerId == null) return Results.Unauthorized();
    var product = await service.CreateProductAsync(sellerId.Value, request);
    return Results.Created($"/api/products/{product.Id}", product);
}).RequireAuthorization("SellerOnly").WithName("CreateProduct").WithTags("Product Management");

app.MapPut("/api/products/{id}", async (Guid id, ProductService service, UpdateProductRequest request, HttpContext ctx) =>
{
    var sellerId = GetUserId(ctx);
    if (sellerId == null) return Results.Unauthorized();
    var product = await service.UpdateProductAsync(id, sellerId.Value, request);
    return product == null ? Results.NotFound() : Results.Ok(product);
}).RequireAuthorization("SellerOnly").WithName("UpdateProduct").WithTags("Product Management");

app.MapDelete("/api/products/{id}", async (Guid id, ProductService service, HttpContext ctx) =>
{
    var sellerId = GetUserId(ctx);
    if (sellerId == null) return Results.Unauthorized();
    var success = await service.DeleteProductAsync(id, sellerId.Value);
    return success ? Results.Ok(new { message = "Product deleted" }) : Results.NotFound();
}).RequireAuthorization("SellerOnly").WithName("DeleteProduct").WithTags("Product Management");

app.MapPost("/api/products/{id}/submit", async (Guid id, ProductService service, HttpContext ctx) =>
{
    var sellerId = GetUserId(ctx);
    if (sellerId == null) return Results.Unauthorized();
    var success = await service.SubmitProductAsync(id, sellerId.Value);
    return success ? Results.Ok(new { message = "Product submitted for approval" }) : Results.NotFound();
}).RequireAuthorization("SellerOnly").WithName("SubmitProduct").WithTags("Product Management");

// ============================================
// Product Review APIs
// ============================================

app.MapGet("/api/products/{id}/reviews", async (Guid id, CachedProductService cachedService, int page = 1, int pageSize = 10, int? rating = null) =>
    Results.Ok(await cachedService.GetReviewsAsync(id, page, pageSize, rating)))
.WithName("GetProductReviews").WithTags("Reviews");

app.MapGet("/api/products/{id}/reviews/summary", async (Guid id, CachedProductService cachedService) =>
    Results.Ok(await cachedService.GetReviewSummaryAsync(id)))
.WithName("GetReviewSummary").WithTags("Reviews");

app.MapPost("/api/products/{id}/reviews", async (Guid id, ProductService service, CreateReviewRequest request, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();
    var review = await service.CreateReviewAsync(id, userId.Value, request);
    return review == null ? Results.BadRequest(new { message = "Already reviewed" }) : Results.Created($"/api/products/{id}/reviews/{review.Id}", review);
}).RequireAuthorization().WithName("CreateReview").WithTags("Reviews");

app.MapPost("/api/products/{productId}/reviews/{reviewId}/reply", async (Guid productId, Guid reviewId, ProductService service, ReplyReviewRequest request) =>
{
    var success = await service.ReplyToReviewAsync(reviewId, request.Reply);
    return success ? Results.Ok(new { message = "Reply added" }) : Results.NotFound();
}).RequireAuthorization("SellerOnly").WithName("ReplyToReview").WithTags("Reviews");

// ============================================
// Admin APIs
// ============================================

app.MapGet("/api/products/admin/pending", async (ProductService service, int page = 1, int pageSize = 20) =>
    Results.Ok(await service.GetPendingProductsAsync(page, pageSize)))
.RequireAuthorization("AdminOnly").WithName("GetPendingProducts").WithTags("Admin");

app.MapPost("/api/products/admin/{id}/approve", async (Guid id, ProductService service) =>
{
    var success = await service.ApproveProductAsync(id);
    return success ? Results.Ok(new { message = "Product approved" }) : Results.NotFound();
}).RequireAuthorization("AdminOnly").WithName("ApproveProduct").WithTags("Admin");

app.MapPost("/api/products/admin/{id}/reject", async (Guid id, ProductService service) =>
{
    var success = await service.RejectProductAsync(id);
    return success ? Results.Ok(new { message = "Product rejected" }) : Results.NotFound();
}).RequireAuthorization("AdminOnly").WithName("RejectProduct").WithTags("Admin");

app.MapPatch("/api/products/admin/{id}/feature", async (Guid id, ProductService service, bool is_featured) =>
{
    var success = await service.SetFeaturedAsync(id, is_featured);
    return success ? Results.Ok(new { message = "Featured status updated" }) : Results.NotFound();
}).RequireAuthorization("AdminOnly").WithName("SetFeatured").WithTags("Admin");

app.Run();
