using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Discount.Api.Consumers;
using Discount.Api.Services;
using Discount.Application.DTOs;
using Discount.Application.Interfaces;
using Discount.Application.Services;
using Discount.Infrastructure.DB;
using Discount.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Messaging.Extensions;
using Shared.Caching.Extensions;
// upload image

var builder = WebApplication.CreateBuilder(args);

// ============================================
// SERVICES
// ============================================

// Database
builder.Services.AddDbContext<DiscountDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DBConnectParam")));

// Redis Caching
builder.Services.AddRedisCaching(builder.Configuration);

// RabbitMQ
builder.Services.AddRabbitMQ(builder.Configuration);

// Repositories
builder.Services.AddScoped<IDiscountRepository, DiscountRepository>();
builder.Services.AddScoped<IDiscountProductRepository, DiscountProductRepository>();
builder.Services.AddScoped<IDiscountCategoryRepository, DiscountCategoryRepository>();
builder.Services.AddScoped<IDiscountUserRepository, DiscountUserRepository>();
builder.Services.AddScoped<IDiscountUsageRepository, DiscountUsageRepository>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<IPromotionDiscountRepository, PromotionDiscountRepository>();
builder.Services.AddScoped<IFlashSaleRepository, FlashSaleRepository>();
builder.Services.AddScoped<IFlashSaleItemRepository, FlashSaleItemRepository>();

// Services
builder.Services.AddScoped<DiscountService>();

// gRPC
builder.Services.AddGrpc();

// Consumers
builder.Services.AddHostedService<OrderConfirmedConsumer>();
builder.Services.AddHostedService<OrderCancelledConsumer>();

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "ducanhdeptrai123_ducanhdeptrai123";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("AdminOrManager", policy => policy.RequireRole("Admin", "Manager"));
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Discount Service API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ============================================
// Check all service connections on startup
// ============================================
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    // 1. Check PostgreSQL connection
    var dbContext = scope.ServiceProvider.GetRequiredService<Discount.Infrastructure.DB.DiscountDbContext>();
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

    // 2. Check RabbitMQ connection
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

// ============================================
// MIDDLEWARE
// ============================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Map gRPC Services (Port 5016)
app.MapGrpcService<DiscountGrpcService>();
app.MapGet("/", () => "Discount Service - REST API (5006) & gRPC (5016)");

// ============================================
// HELPER METHODS
// ============================================

Guid GetUserId(ClaimsPrincipal user)
{
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                   ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? user.FindFirst("user_id")?.Value;
    return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
}

string? GetUserName(ClaimsPrincipal user)
{
    return user.FindFirst(ClaimTypes.Name)?.Value 
        ?? user.FindFirst("name")?.Value;
}

// ============================================
// DATABASE CONNECTION TEST
// ============================================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DiscountDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        await db.Database.EnsureCreatedAsync();
        logger.LogInformation("✅ Database connection successful - discount_db");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Database connection failed");
    }
}

// ============================================
// PUBLIC APIs - DISCOUNTS
// ============================================

app.MapGet("/api/discounts", async (
    int page,
    int pageSize,
    DiscountService service) =>
{
    var result = await service.GetPublicDiscountsAsync(page, pageSize);
    return Results.Ok(result);
});

app.MapPost("/api/discounts/validate", async (
    ValidateDiscountRequest request,
    ClaimsPrincipal user,
    DiscountService service) =>
{
    var userId = GetUserId(user);
    var result = await service.ValidateDiscountAsync(request, userId);
    return Results.Ok(result);
}).RequireAuthorization();

app.MapPost("/api/discounts/apply", async (
    ApplyDiscountRequest request,
    ClaimsPrincipal user,
    DiscountService service) =>
{
    var userId = GetUserId(user);
    var result = await service.ApplyDiscountAsync(request, userId);
    return Results.Ok(result);
}).RequireAuthorization();

app.MapGet("/api/discounts/me", async (
    ClaimsPrincipal user,
    DiscountService service) =>
{
    var userId = GetUserId(user);
    var result = await service.GetUserDiscountsAsync(userId);
    return Results.Ok(new { items = result });
}).RequireAuthorization();

// ============================================
// PUBLIC APIs - PROMOTIONS
// ============================================

app.MapGet("/api/discounts/promotions", async (DiscountService service) =>
{
    var result = await service.GetActivePromotionsAsync();
    return Results.Ok(new { items = result });
});

app.MapGet("/api/discounts/promotions/{id:guid}", async (
    Guid id,
    DiscountService service) =>
{
    var result = await service.GetPromotionDetailAsync(id);
    return result != null ? Results.Ok(result) : Results.NotFound();
});

// ============================================
// PUBLIC APIs - FLASH SALES
// ============================================

app.MapGet("/api/discounts/flash-sales", async (DiscountService service) =>
{
    var result = await service.GetActiveFlashSalesAsync();
    return Results.Ok(new { items = result });
});

app.MapGet("/api/discounts/flash-sales/{id:guid}", async (
    Guid id,
    DiscountService service) =>
{
    var result = await service.GetFlashSaleDetailAsync(id);
    return result != null ? Results.Ok(result) : Results.NotFound();
});

app.MapGet("/api/discounts/flash-sales/{id:guid}/items/{productId:guid}", async (
    Guid id,
    Guid productId,
    Guid? variantId,
    DiscountService service) =>
{
    var result = await service.CheckFlashSaleItemAvailabilityAsync(id, productId, variantId);
    return result != null ? Results.Ok(result) : Results.NotFound();
});

// ============================================
// ADMIN APIs - DISCOUNT MANAGEMENT
// ============================================

app.MapGet("/api/discounts/admin", async (
    int page,
    int pageSize,
    string? type,
    bool? isActive,
    string? search,
    DiscountService service) =>
{
    var result = await service.GetAllDiscountsAsync(page, pageSize, type, isActive, search);
    return Results.Ok(result);
}).RequireAuthorization("AdminOrManager");

app.MapGet("/api/discounts/admin/{id:guid}", async (
    Guid id,
    DiscountService service) =>
{
    var result = await service.GetDiscountDetailAsync(id);
    return result != null ? Results.Ok(result) : Results.NotFound();
}).RequireAuthorization("AdminOrManager");

app.MapPost("/api/discounts/admin", async (
    CreateDiscountRequest request,
    ClaimsPrincipal user,
    DiscountService service) =>
{
    try
    {
        var userId = GetUserId(user);
        var userName = GetUserName(user);
        var id = await service.CreateDiscountAsync(request, userId, userName);
        return Results.Ok(new { id, message = "Discount created" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
}).RequireAuthorization("AdminOrManager");

app.MapPut("/api/discounts/admin/{id:guid}", async (
    Guid id,
    UpdateDiscountRequest request,
    DiscountService service) =>
{
    try
    {
        await service.UpdateDiscountAsync(id, request);
        return Results.Ok(new { id, message = "Discount updated" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
}).RequireAuthorization("AdminOrManager");

app.MapDelete("/api/discounts/admin/{id:guid}", async (
    Guid id,
    DiscountService service) =>
{
    try
    {
        await service.DeleteDiscountAsync(id);
        return Results.Ok(new { message = "Discount deleted" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
}).RequireAuthorization("AdminOrManager");

app.MapPut("/api/discounts/admin/{id:guid}/toggle", async (
    Guid id,
    DiscountService service) =>
{
    try
    {
        await service.ToggleDiscountStatusAsync(id);
        return Results.Ok(new { id, message = "Discount status updated" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
}).RequireAuthorization("AdminOrManager");

app.MapGet("/api/discounts/admin/{id:guid}/statistics", async (
    Guid id,
    DiscountService service) =>
{
    var result = await service.GetDiscountStatisticsAsync(id);
    return Results.Ok(result);
}).RequireAuthorization("AdminOrManager");

// ============================================
// ADMIN APIs - PROMOTION MANAGEMENT
// ============================================

app.MapGet("/api/discounts/admin/promotions", async (
    int page,
    int pageSize,
    bool? isActive,
    DiscountService service) =>
{
    var result = await service.GetAllPromotionsAsync(page, pageSize, isActive);
    return Results.Ok(result);
}).RequireAuthorization("AdminOrManager");

app.MapPost("/api/discounts/admin/promotions", async (
    CreatePromotionRequest request,
    ClaimsPrincipal user,
    DiscountService service) =>
{
    var userId = GetUserId(user);
    var userName = GetUserName(user);
    var id = await service.CreatePromotionAsync(request, userId, userName);
    return Results.Ok(new { id, message = "Promotion created" });
}).RequireAuthorization("AdminOrManager");

app.MapPut("/api/discounts/admin/promotions/{id:guid}", async (
    Guid id,
    UpdatePromotionRequest request,
    DiscountService service) =>
{
    try
    {
        await service.UpdatePromotionAsync(id, request);
        return Results.Ok(new { id, message = "Promotion updated" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
}).RequireAuthorization("AdminOrManager");

app.MapDelete("/api/discounts/admin/promotions/{id:guid}", async (
    Guid id,
    DiscountService service) =>
{
    try
    {
        await service.DeletePromotionAsync(id);
        return Results.Ok(new { message = "Promotion deleted" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
}).RequireAuthorization("AdminOrManager");

// ============================================
// ADMIN APIs - FLASH SALE MANAGEMENT
// ============================================

app.MapGet("/api/discounts/admin/flash-sales", async (
    int page,
    int pageSize,
    DiscountService service) =>
{
    var result = await service.GetAllFlashSalesAsync(page, pageSize);
    return Results.Ok(result);
}).RequireAuthorization("AdminOrManager");

app.MapPost("/api/discounts/admin/flash-sales", async (
    CreateFlashSaleRequest request,
    DiscountService service) =>
{
    var id = await service.CreateFlashSaleAsync(request);
    return Results.Ok(new { id, message = "Flash sale created" });
}).RequireAuthorization("AdminOrManager");

app.MapPut("/api/discounts/admin/flash-sales/{id:guid}", async (
    Guid id,
    UpdateFlashSaleRequest request,
    DiscountService service) =>
{
    try
    {
        await service.UpdateFlashSaleAsync(id, request);
        return Results.Ok(new { id, message = "Flash sale updated" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
}).RequireAuthorization("AdminOrManager");

app.MapPost("/api/discounts/admin/flash-sales/{id:guid}/items", async (
    Guid id,
    CreateFlashSaleItemRequest request,
    DiscountService service) =>
{
    try
    {
        var itemId = await service.AddFlashSaleItemAsync(id, request);
        return Results.Ok(new { id = itemId, message = "Item added to flash sale" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
}).RequireAuthorization("AdminOrManager");

app.MapPut("/api/discounts/admin/flash-sales/{id:guid}/items/{itemId:guid}", async (
    Guid id,
    Guid itemId,
    UpdateFlashSaleItemRequest request,
    DiscountService service) =>
{
    try
    {
        await service.UpdateFlashSaleItemAsync(itemId, request);
        return Results.Ok(new { id = itemId, message = "Flash sale item updated" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
}).RequireAuthorization("AdminOrManager");

app.MapDelete("/api/discounts/admin/flash-sales/{id:guid}/items/{itemId:guid}", async (
    Guid id,
    Guid itemId,
    DiscountService service) =>
{
    try
    {
        await service.DeleteFlashSaleItemAsync(itemId);
        return Results.Ok(new { message = "Item removed from flash sale" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
}).RequireAuthorization("AdminOrManager");

// ============================================
// INTERNAL APIs
// ============================================

app.MapPost("/api/discounts/internal/record-usage", async (
    RecordUsageRequest request,
    DiscountService service) =>
{
    await service.RecordUsageAsync(request);
    return Results.Ok(new { success = true });
});

app.MapPost("/api/discounts/internal/rollback-usage", async (
    RollbackUsageRequest request,
    DiscountService service) =>
{
    await service.RollbackUsageAsync(request.OrderId);
    return Results.Ok(new { success = true });
});

app.MapPost("/api/discounts/internal/products", async (
    GetProductDiscountsRequest request,
    DiscountService service) =>
{
    var result = await service.GetDiscountsForProductsAsync(request.ProductIds);
    return Results.Ok(new { items = result });
});

app.Run();
