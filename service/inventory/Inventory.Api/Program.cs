using Microsoft.EntityFrameworkCore;
using Inventory.Api.Consumers;
using Inventory.Api.Services;
using Inventory.Application.DTOs;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Infrastructure.DB;
using Inventory.Infrastructure.Repositories;
using Shared.Messaging.Events;
using Shared.Messaging.Extensions;
using Shared.Caching.Extensions;
// upload image

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DBConnectParam")));

// Redis Caching
builder.Services.AddRedisCaching(builder.Configuration);

// RabbitMQ
builder.Services.AddRabbitMQ(builder.Configuration);

// Repositories
builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IInventoryTransactionRepository, InventoryTransactionRepository>();
builder.Services.AddScoped<IStockReservationRepository, StockReservationRepository>();

// Services
builder.Services.AddScoped<InventoryService>();

// gRPC
builder.Services.AddGrpc();

// Consumers
builder.Services.AddHostedService<OrderConfirmedConsumer>();
builder.Services.AddHostedService<OrderCancelledConsumer>();
builder.Services.AddHostedService<PaymentFailedConsumer>();

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
    options.AddPolicy("SellerOrAdmin", policy => policy.RequireRole("Seller", "Admin"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Inventory Service API", Version = "v1" });
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
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
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
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Map gRPC Services (Port 5015)
app.MapGrpcService<InventoryGrpcService>();
app.MapGet("/", () => "Inventory Service - REST API (5005) & gRPC (5015)");

// Helper to get UserId from JWT
static Guid? GetUserId(HttpContext ctx)
{
    var userIdClaim = ctx.User.FindFirst("sub") ?? ctx.User.FindFirst("nameid") ?? ctx.User.FindFirst("user_id");
    return userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId) ? userId : null;
}

// ============================================
// Stock Check APIs (Public/Internal)
// ============================================

app.MapPost("/api/inventory/check", async (InventoryService service, CheckStockRequest request) =>
{
    var result = await service.CheckStockAsync(request);
    return Results.Ok(result);
}).WithName("CheckStock").WithTags("Stock Check");

app.MapGet("/api/inventory/product/{productId}", async (Guid productId, InventoryService service, Guid? variantId = null) =>
{
    var result = await service.GetProductStockAsync(productId, variantId);
    return result != null ? Results.Ok(result) : Results.NotFound();
}).WithName("GetProductStock").WithTags("Stock Check");

app.MapPost("/api/inventory/products", async (InventoryService service, List<Guid> productIds) =>
{
    var results = new List<object>();
    foreach (var productId in productIds)
    {
        var stock = await service.GetProductStockAsync(productId);
        if (stock != null)
            results.Add(new { stock.ProductId, stock.VariantId, stock.TotalAvailable });
    }
    return Results.Ok(new { items = results });
}).WithName("GetProductsStock").WithTags("Stock Check");

// ============================================
// Reservation APIs (Called by Order Service)
// ============================================

app.MapPost("/api/inventory/reserve", async (InventoryService service, ReserveStockRequest request) =>
{
    try
    {
        var result = await service.ReserveStockAsync(request);
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message, isReserved = false });
    }
}).WithName("ReserveStock").WithTags("Reservations");

app.MapPost("/api/inventory/commit", async (InventoryService service, Guid orderId) =>
{
    var success = await service.CommitStockAsync(orderId);
    return success ? Results.Ok(new { message = "Stock committed" }) : Results.NotFound();
}).WithName("CommitStock").WithTags("Reservations");

app.MapPost("/api/inventory/release", async (InventoryService service, ReleaseStockRequest request) =>
{
    var success = await service.ReleaseStockAsync(request);
    return success 
        ? Results.Ok(new { message = "Stock released", isReleased = true }) 
        : Results.Ok(new { message = "No reservations found", isReleased = false });
}).WithName("ReleaseStock").WithTags("Reservations");

app.MapPost("/api/inventory/deduct", async (InventoryService service, DeductStockRequest request) =>
{
    try
    {
        var success = await service.DeductStockAsync(request);
        return Results.Ok(new { success });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
}).WithName("DeductStock").WithTags("Reservations");

app.MapPost("/api/inventory/return", async (InventoryService service, ReturnStockRequest request) =>
{
    var success = await service.ReturnStockAsync(request);
    return Results.Ok(new { success });
}).WithName("ReturnStock").WithTags("Reservations");

// ============================================
// Warehouse APIs (Admin)
// ============================================

app.MapGet("/api/inventory/warehouses", async (InventoryService service) =>
{
    var warehouses = await service.GetWarehousesAsync();
    return Results.Ok(new { items = warehouses });
}).RequireAuthorization().WithName("GetWarehouses").WithTags("Warehouses");

app.MapGet("/api/inventory/warehouses/{id}", async (Guid id, InventoryService service) =>
{
    var warehouse = await service.GetWarehouseByIdAsync(id);
    return warehouse != null ? Results.Ok(warehouse) : Results.NotFound();
}).RequireAuthorization().WithName("GetWarehouseById").WithTags("Warehouses");

app.MapPost("/api/inventory/warehouses", async (InventoryService service, CreateWarehouseRequest request) =>
{
    var warehouse = await service.CreateWarehouseAsync(request);
    return Results.Created($"/api/inventory/warehouses/{warehouse.Id}", new { warehouse, message = "Warehouse created" });
}).RequireAuthorization("AdminOnly").WithName("CreateWarehouse").WithTags("Warehouses");

app.MapPut("/api/inventory/warehouses/{id}", async (Guid id, InventoryService service, UpdateWarehouseRequest request) =>
{
    var warehouse = await service.UpdateWarehouseAsync(id, request);
    return warehouse != null ? Results.Ok(new { warehouse, message = "Warehouse updated" }) : Results.NotFound();
}).RequireAuthorization("AdminOnly").WithName("UpdateWarehouse").WithTags("Warehouses");

// ============================================
// Inventory Management APIs (Admin/Seller)
// ============================================

app.MapGet("/api/inventory/stocks", async (InventoryService service, Guid? warehouseId = null, bool? lowStock = null, int page = 1, int pageSize = 20) =>
{
    var result = await service.GetInventoryAsync(warehouseId, lowStock, page, pageSize);
    return Results.Ok(result);
}).RequireAuthorization().WithName("GetInventory").WithTags("Inventory");

app.MapGet("/api/inventory/stocks/{id}", async (Guid id, InventoryService service) =>
{
    var item = await service.GetInventoryByIdAsync(id);
    return item != null ? Results.Ok(item) : Results.NotFound();
}).RequireAuthorization().WithName("GetInventoryById").WithTags("Inventory");

app.MapPost("/api/inventory/stocks", async (InventoryService service, CreateInventoryRequest request) =>
{
    try
    {
        var item = await service.CreateInventoryAsync(request);
        return Results.Created($"/api/inventory/stocks/{item.Id}", new { item, message = "Inventory created" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
}).RequireAuthorization("SellerOrAdmin").WithName("CreateInventory").WithTags("Inventory");

app.MapPut("/api/inventory/stocks/{id}", async (Guid id, InventoryService service, UpdateInventoryRequest request, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    var userName = ctx.User.FindFirst("name")?.Value;
    var item = await service.UpdateInventoryAsync(id, request, userId, userName);
    return item != null ? Results.Ok(new { item, message = "Inventory updated" }) : Results.NotFound();
}).RequireAuthorization("SellerOrAdmin").WithName("UpdateInventory").WithTags("Inventory");

app.MapPost("/api/inventory/stocks/{id}/adjust", async (Guid id, InventoryService service, AdjustInventoryRequest request, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    var userName = ctx.User.FindFirst("name")?.Value;
    var item = await service.AdjustInventoryAsync(id, request, userId, userName);
    return item != null ? Results.Ok(new { item, message = "Inventory adjusted" }) : Results.NotFound();
}).RequireAuthorization("SellerOrAdmin").WithName("AdjustInventory").WithTags("Inventory");

// ============================================
// Transfer APIs (Admin)
// ============================================

app.MapPost("/api/inventory/transfer", async (InventoryService service, TransferStockRequest request, HttpContext ctx) =>
{
    try
    {
        var userId = GetUserId(ctx);
        var userName = ctx.User.FindFirst("name")?.Value;
        var success = await service.TransferStockAsync(request, userId, userName);
        return Results.Ok(new { success, message = "Stock transferred" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
}).RequireAuthorization("AdminOnly").WithName("TransferStock").WithTags("Transfer");

// ============================================
// Low Stock Alerts (Admin)
// ============================================

app.MapGet("/api/inventory/alerts/low-stock", async (InventoryService service, Guid? warehouseId = null) =>
{
    var alerts = await service.GetLowStockAlertsAsync(warehouseId);
    return Results.Ok(new { items = alerts, total_low_stock_items = alerts.Count });
}).RequireAuthorization().WithName("GetLowStockAlerts").WithTags("Alerts");

// ============================================
// Internal APIs
// ============================================

app.MapPost("/api/inventory/internal/release-expired", async (InventoryService service) =>
{
    var count = await service.ReleaseExpiredReservationsAsync();
    return Results.Ok(new { released_count = count });
}).WithName("ReleaseExpiredReservations").WithTags("Internal");

// Internal commit endpoint (called via message queue or HTTP)
app.MapPost("/api/inventory/internal/commit", async (InventoryService service, InternalCommitRequest request) =>
{
    var success = await service.CommitStockAsync(request.OrderId);
    return success 
        ? Results.Ok(new { success = true, message = "Stock committed" }) 
        : Results.Ok(new { success = false, message = "No reservations found" });
}).WithName("InternalCommitStock").WithTags("Internal");

app.Run();

// Internal DTOs
public record InternalCommitRequest(Guid OrderId, Guid UserId, List<Guid>? ReservationIds);
