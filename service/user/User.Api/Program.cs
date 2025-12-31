using Microsoft.EntityFrameworkCore;
using User.Application.DTOs;
using User.Application.Interfaces;
using User.Application.Services;
using User.Infrastructure.DB;
using User.Infrastructure.Repositories;
using Shared.Messaging.Extensions;
using Shared.Caching.Extensions;
// upload image

var builder = WebApplication.CreateBuilder(args);
// test github action
// Database
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DBConnectParam")));

// Redis Caching
builder.Services.AddRedisCaching(builder.Configuration);

// RabbitMQ for event publishing
builder.Services.AddRabbitMQ(builder.Configuration);

// Repositories
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<IUserAddressRepository, UserAddressRepository>();
builder.Services.AddScoped<IUserPaymentMethodRepository, UserPaymentMethodRepository>();
builder.Services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();
builder.Services.AddScoped<IUserWishlistRepository, UserWishlistRepository>();
builder.Services.AddScoped<IUserActivityLogRepository, UserActivityLogRepository>();

// Services
builder.Services.AddScoped<UserService>();

// Authentication - JWT validation
// Note: Nếu request đến từ Gateway (có X-Gateway-Request header), Gateway đã validate JWT
// Service chỉ cần validate internal key. Nếu không có Gateway, service tự validate JWT.
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
        
        // Skip JWT validation nếu request đến từ Gateway (Gateway đã validate rồi)
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var isGatewayRequest = context.HttpContext.Request.Headers["X-Gateway-Request"].FirstOrDefault();
                if (isGatewayRequest == "true")
                {
                    // Gateway đã validate JWT, chỉ cần check internal key (done in middleware)
                    // Skip JWT validation nhưng vẫn extract claims từ token nếu có
                    context.Token = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
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
    var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
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

// Internal Auth Middleware - Validate requests từ Gateway
app.UseMiddleware<User.Api.Middleware.InternalAuthMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Helper method to get user ID from JWT token
Guid? GetUserId(HttpContext context)
{
    var userIdClaim = context.User.Claims
        .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
}

// ============================================
// User Profile APIs
// ============================================

app.MapGet("/api/users/me/profile", async (UserService userService, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var profile = await userService.GetProfileAsync(userId.Value);
    if (profile == null) return Results.NotFound();
    return Results.Ok(profile);
})
.RequireAuthorization()
.WithName("GetUserProfile")
.WithTags("User Profile");

app.MapPut("/api/users/me/profile", async (UserService userService, UpdateProfileRequest request, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var profile = await userService.CreateOrUpdateProfileAsync(userId.Value, request);
    return Results.Ok(profile);
})
.RequireAuthorization()
.WithName("UpdateUserProfile")
.WithTags("User Profile");

app.MapPost("/api/users/me/avatar", async (UserService userService, string avatarUrl, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var url = await userService.UpdateAvatarAsync(userId.Value, avatarUrl);
    return Results.Ok(new { avatar_url = url });
})
.RequireAuthorization()
.WithName("UpdateAvatar")
.WithTags("User Profile");

// ============================================
// User Addresses APIs
// ============================================

app.MapGet("/api/users/me/addresses", async (UserService userService, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var addresses = await userService.GetAddressesAsync(userId.Value);
    return Results.Ok(addresses);
})
.RequireAuthorization()
.WithName("GetUserAddresses")
.WithTags("User Addresses");

app.MapGet("/api/users/me/addresses/{addressId}", async (Guid addressId, UserService userService, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var address = await userService.GetAddressAsync(userId.Value, addressId);
    if (address == null) return Results.NotFound();
    return Results.Ok(address);
})
.RequireAuthorization()
.WithName("GetUserAddress")
.WithTags("User Addresses");

app.MapPost("/api/users/me/addresses", async (UserService userService, CreateAddressRequest request, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var address = await userService.CreateAddressAsync(userId.Value, request);
    return Results.Created($"/api/users/me/addresses/{address.Id}", address);
})
.RequireAuthorization()
.WithName("CreateUserAddress")
.WithTags("User Addresses");

app.MapPut("/api/users/me/addresses/{addressId}", async (Guid addressId, UserService userService, UpdateAddressRequest request, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var address = await userService.UpdateAddressAsync(userId.Value, addressId, request);
    if (address == null) return Results.NotFound();
    return Results.Ok(address);
})
.RequireAuthorization()
.WithName("UpdateUserAddress")
.WithTags("User Addresses");

app.MapDelete("/api/users/me/addresses/{addressId}", async (Guid addressId, UserService userService, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var success = await userService.DeleteAddressAsync(userId.Value, addressId);
    if (!success) return Results.NotFound();
    return Results.Ok(new { message = "Address deleted successfully" });
})
.RequireAuthorization()
.WithName("DeleteUserAddress")
.WithTags("User Addresses");

app.MapPatch("/api/users/me/addresses/{addressId}/set-default", async (Guid addressId, UserService userService, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var success = await userService.SetDefaultAddressAsync(userId.Value, addressId);
    if (!success) return Results.NotFound();
    return Results.Ok(new { message = "Default address updated" });
})
.RequireAuthorization()
.WithName("SetDefaultAddress")
.WithTags("User Addresses");

// ============================================
// User Payment Methods APIs
// ============================================

app.MapGet("/api/users/me/payment-methods", async (UserService userService, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var methods = await userService.GetPaymentMethodsAsync(userId.Value);
    return Results.Ok(methods);
})
.RequireAuthorization()
.WithName("GetUserPaymentMethods")
.WithTags("User Payment Methods");

app.MapGet("/api/users/me/payment-methods/{paymentMethodId}", async (Guid paymentMethodId, UserService userService, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var method = await userService.GetPaymentMethodAsync(userId.Value, paymentMethodId);
    if (method == null) return Results.NotFound();
    return Results.Ok(method);
})
.RequireAuthorization()
.WithName("GetUserPaymentMethod")
.WithTags("User Payment Methods");

app.MapPost("/api/users/me/payment-methods", async (UserService userService, CreatePaymentMethodRequest request, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var method = await userService.CreatePaymentMethodAsync(userId.Value, request);
    return Results.Created($"/api/users/me/payment-methods/{method.Id}", method);
})
.RequireAuthorization()
.WithName("CreateUserPaymentMethod")
.WithTags("User Payment Methods");

app.MapPut("/api/users/me/payment-methods/{paymentMethodId}", async (Guid paymentMethodId, UserService userService, UpdatePaymentMethodRequest request, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var method = await userService.UpdatePaymentMethodAsync(userId.Value, paymentMethodId, request);
    if (method == null) return Results.NotFound();
    return Results.Ok(method);
})
.RequireAuthorization()
.WithName("UpdateUserPaymentMethod")
.WithTags("User Payment Methods");

app.MapDelete("/api/users/me/payment-methods/{paymentMethodId}", async (Guid paymentMethodId, UserService userService, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var success = await userService.DeletePaymentMethodAsync(userId.Value, paymentMethodId);
    if (!success) return Results.NotFound();
    return Results.Ok(new { message = "Payment method deleted successfully" });
})
.RequireAuthorization()
.WithName("DeleteUserPaymentMethod")
.WithTags("User Payment Methods");

app.MapPatch("/api/users/me/payment-methods/{paymentMethodId}/set-default", async (Guid paymentMethodId, UserService userService, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var success = await userService.SetDefaultPaymentMethodAsync(userId.Value, paymentMethodId);
    if (!success) return Results.NotFound();
    return Results.Ok(new { message = "Default payment method updated" });
})
.RequireAuthorization()
.WithName("SetDefaultPaymentMethod")
.WithTags("User Payment Methods");

// ============================================
// User Preferences APIs
// ============================================

app.MapGet("/api/users/me/preferences", async (UserService userService, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var preferences = await userService.GetPreferencesAsync(userId.Value);
    if (preferences == null) return Results.NotFound();
    return Results.Ok(preferences);
})
.RequireAuthorization()
.WithName("GetUserPreferences")
.WithTags("User Preferences");

app.MapPut("/api/users/me/preferences", async (UserService userService, UpdatePreferenceRequest request, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var preferences = await userService.CreateOrUpdatePreferencesAsync(userId.Value, request);
    return Results.Ok(preferences);
})
.RequireAuthorization()
.WithName("UpdateUserPreferences")
.WithTags("User Preferences");

// ============================================
// User Wishlist APIs
// ============================================

app.MapGet("/api/users/me/wishlist", async (UserService userService, int page = 1, int pageSize = 20, HttpContext ctx = null!) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var wishlist = await userService.GetWishlistAsync(userId.Value, page, pageSize);
    return Results.Ok(wishlist);
})
.RequireAuthorization()
.WithName("GetUserWishlist")
.WithTags("User Wishlist");

app.MapPost("/api/users/me/wishlist", async (UserService userService, AddWishlistRequest request, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var wishlist = await userService.AddToWishlistAsync(userId.Value, request);
    if (wishlist == null) return Results.BadRequest(new { message = "Product already in wishlist" });
    return Results.Created($"/api/users/me/wishlist/{wishlist.ProductId}", wishlist);
})
.RequireAuthorization()
.WithName("AddToWishlist")
.WithTags("User Wishlist");

app.MapDelete("/api/users/me/wishlist/{productId}", async (Guid productId, UserService userService, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var success = await userService.RemoveFromWishlistAsync(userId.Value, productId);
    if (!success) return Results.NotFound();
    return Results.Ok(new { message = "Product removed from wishlist" });
})
.RequireAuthorization()
.WithName("RemoveFromWishlist")
.WithTags("User Wishlist");

app.MapGet("/api/users/me/wishlist/check/{productId}", async (Guid productId, UserService userService, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var isInWishlist = await userService.IsInWishlistAsync(userId.Value, productId);
    return Results.Ok(new { is_in_wishlist = isInWishlist });
})
.RequireAuthorization()
.WithName("CheckWishlist")
.WithTags("User Wishlist");

// ============================================
// User Activity Logs APIs
// ============================================

app.MapGet("/api/users/me/activity-logs", async (UserService userService, int page = 1, int pageSize = 20, string? activityType = null, HttpContext ctx = null!) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();

    var logs = await userService.GetActivityLogsAsync(userId.Value, page, pageSize, activityType);
    return Results.Ok(logs);
})
.RequireAuthorization()
.WithName("GetUserActivityLogs")
.WithTags("User Activity Logs");

app.Run();
