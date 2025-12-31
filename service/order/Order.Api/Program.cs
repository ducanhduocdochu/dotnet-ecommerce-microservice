using Microsoft.EntityFrameworkCore;
using Order.Api.Consumers;
using Order.Api.Services;
using Order.Application.Clients;
using Order.Application.DTOs;
using Order.Application.Interfaces;
using Order.Application.Services;
using Order.Infrastructure.Clients;
using Order.Infrastructure.DB;
using Order.Infrastructure.Repositories;
using Shared.Messaging.Extensions;
using Shared.Messaging.RabbitMQ;
// upload image

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DBConnectParam")));

// RabbitMQ
builder.Services.AddRabbitMQ(builder.Configuration);

// Feature flags for gRPC
var useGrpcForInventory = builder.Configuration.GetValue<bool>("Features:UseGrpcForInventory", false);
var useGrpcForDiscount = builder.Configuration.GetValue<bool>("Features:UseGrpcForDiscount", false);

// HTTP Clients (fallback or when gRPC disabled)
if (!useGrpcForDiscount)
{
    builder.Services.AddHttpClient<IDiscountClient, DiscountClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:Discount"] ?? "http://localhost:5006");
        client.Timeout = TimeSpan.FromSeconds(10);
    });
}

if (!useGrpcForInventory)
{
    builder.Services.AddHttpClient<IInventoryClient, InventoryClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:Inventory"] ?? "http://localhost:5005");
        client.Timeout = TimeSpan.FromSeconds(10);
    });
}

builder.Services.AddHttpClient<IPaymentClient, PaymentClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:Payment"] ?? "http://localhost:5007");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderStatusHistoryRepository, OrderStatusHistoryRepository>();
builder.Services.AddScoped<IOrderRefundRepository, OrderRefundRepository>();

// Services
builder.Services.AddScoped<OrderService>();

// gRPC Clients (raw clients)
builder.Services.AddScoped<InventoryGrpcClient>();
builder.Services.AddScoped<DiscountGrpcClient>();

// gRPC Adapters (implements IInventoryClient/IDiscountClient using gRPC)
if (useGrpcForInventory)
{
    builder.Services.AddScoped<IInventoryClient, InventoryGrpcClientAdapter>();
}

if (useGrpcForDiscount)
{
    builder.Services.AddScoped<IDiscountClient, DiscountGrpcClientAdapter>();
}

// Consumers
builder.Services.AddHostedService<UserProfileUpdatedConsumer>();
builder.Services.AddHostedService<PaymentSuccessConsumer>();
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
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
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

// Helper to get UserId from JWT
// Helper to get user ID from JWT
Guid? GetUserId(HttpContext ctx) =>
    Guid.TryParse(ctx.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

// ============================================
// Cart APIs
// ============================================

app.MapGet("/api/cart", async (OrderService service, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();
    var cart = await service.GetCartAsync(userId.Value);
    return Results.Ok(cart);
}).RequireAuthorization().WithName("GetCart").WithTags("Cart");

app.MapPost("/api/cart", async (OrderService service, AddToCartRequest request, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();
    var item = await service.AddToCartAsync(userId.Value, request);
    return Results.Ok(new { item, message = "Item added to cart" });
}).RequireAuthorization().WithName("AddToCart").WithTags("Cart");

app.MapPut("/api/cart/{itemId}", async (Guid itemId, OrderService service, UpdateCartItemRequest request, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();
    var result = await service.UpdateCartItemAsync(userId.Value, itemId, request.Quantity);
    return result != null ? Results.Ok(new { item = result, message = "Cart updated" }) : Results.Ok(new { message = "Item removed" });
}).RequireAuthorization().WithName("UpdateCartItem").WithTags("Cart");

app.MapDelete("/api/cart/{itemId}", async (Guid itemId, OrderService service, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();
    var success = await service.RemoveCartItemAsync(userId.Value, itemId);
    return success ? Results.Ok(new { message = "Item removed from cart" }) : Results.NotFound();
}).RequireAuthorization().WithName("RemoveCartItem").WithTags("Cart");

app.MapDelete("/api/cart", async (OrderService service, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();
    await service.ClearCartAsync(userId.Value);
    return Results.Ok(new { message = "Cart cleared" });
}).RequireAuthorization().WithName("ClearCart").WithTags("Cart");

// ============================================
// Checkout API (New - with HTTP clients)
// ============================================

app.MapPost("/api/orders/checkout", async (OrderService service, CheckoutRequest request, HttpContext ctx, ILogger<Program> logger) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();
    
    try
    {
        var result = await service.CheckoutAsync(userId.Value, request);
        if (result.Success)
        {
            return Results.Ok(result);
        }
        return Results.BadRequest(new { message = result.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Checkout failed for user {UserId}", userId);
        return Results.BadRequest(new { message = "Checkout failed. Please try again." });
    }
}).RequireAuthorization().WithName("Checkout").WithTags("Checkout");

// ============================================
// Payment Callback API (Internal)
// ============================================

app.MapPost("/api/orders/payment-callback", async (OrderService service, PaymentCallbackRequest request) =>
{
    if (request.Success)
    {
        var result = await service.HandlePaymentSuccessAsync(request.OrderId, request.TransactionId);
        return result 
            ? Results.Ok(new { message = "Payment confirmed", order_id = request.OrderId })
            : Results.BadRequest(new { message = "Failed to confirm payment" });
    }
    else
    {
        var result = await service.HandlePaymentFailedAsync(request.OrderId, request.ErrorMessage ?? "Payment failed");
        return result 
            ? Results.Ok(new { message = "Payment failure recorded", order_id = request.OrderId })
            : Results.BadRequest(new { message = "Failed to record payment failure" });
    }
}).WithName("PaymentCallback").WithTags("Internal");

// ============================================
// Customer Order APIs
// ============================================

app.MapPost("/api/orders", async (OrderService service, CreateOrderRequest request, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();
    var order = await service.CreateOrderAsync(userId.Value, request);
    return Results.Created($"/api/orders/{order.Id}", order);
}).RequireAuthorization().WithName("CreateOrder").WithTags("Orders");

app.MapGet("/api/orders/me", async (OrderService service, int page = 1, int pageSize = 10, string? status = null, HttpContext ctx = null!) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();
    var orders = await service.GetUserOrdersAsync(userId.Value, page, pageSize, status);
    return Results.Ok(orders);
}).RequireAuthorization().WithName("GetMyOrders").WithTags("Orders");

app.MapGet("/api/orders/{id}", async (Guid id, OrderService service, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();
    var order = await service.GetOrderByIdAsync(id, userId.Value);
    return order != null ? Results.Ok(order) : Results.NotFound();
}).RequireAuthorization().WithName("GetOrderById").WithTags("Orders");

app.MapGet("/api/orders/number/{orderNumber}", async (string orderNumber, OrderService service, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();
    var order = await service.GetOrderByNumberAsync(orderNumber, userId.Value);
    return order != null ? Results.Ok(order) : Results.NotFound();
}).RequireAuthorization().WithName("GetOrderByNumber").WithTags("Orders");

app.MapPost("/api/orders/{id}/cancel", async (Guid id, OrderService service, CancelOrderRequest request, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();
    var success = await service.CancelOrderAsync(id, userId.Value, request.Reason);
    return success ? Results.Ok(new { message = "Order cancelled", status = "CANCELLED" }) : Results.BadRequest(new { message = "Cannot cancel this order" });
}).RequireAuthorization().WithName("CancelOrder").WithTags("Orders");

app.MapPost("/api/orders/{id}/refund", async (Guid id, OrderService service, CreateRefundRequest request, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();
    var refund = await service.CreateRefundAsync(id, userId.Value, request);
    return refund != null ? Results.Ok(new { refund, message = "Refund request submitted" }) : Results.BadRequest(new { message = "Cannot create refund" });
}).RequireAuthorization().WithName("RequestRefund").WithTags("Orders");

// ============================================
// Seller Order APIs
// ============================================

app.MapGet("/api/orders/seller", async (OrderService service, int page = 1, int pageSize = 10, string? status = null, HttpContext ctx = null!) =>
{
    var userId = GetUserId(ctx);
    if (userId == null) return Results.Unauthorized();
    var orders = await service.GetSellerOrdersAsync(userId.Value, page, pageSize, status);
    return Results.Ok(orders);
}).RequireAuthorization("SellerOnly").WithName("GetSellerOrders").WithTags("Seller Orders");

app.MapPost("/api/orders/seller/{id}/confirm", async (Guid id, OrderService service, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    var userName = ctx.User.FindFirst("name")?.Value;
    var success = await service.ConfirmOrderAsync(id, userId, userName);
    return success ? Results.Ok(new { message = "Order confirmed", status = "CONFIRMED" }) : Results.BadRequest(new { message = "Cannot confirm this order" });
}).RequireAuthorization("SellerOnly").WithName("ConfirmOrder").WithTags("Seller Orders");

app.MapPost("/api/orders/seller/{id}/process", async (Guid id, OrderService service, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    var userName = ctx.User.FindFirst("name")?.Value;
    var success = await service.ProcessOrderAsync(id, userId, userName);
    return success ? Results.Ok(new { message = "Order processing", status = "PROCESSING" }) : Results.BadRequest(new { message = "Cannot process this order" });
}).RequireAuthorization("SellerOnly").WithName("ProcessOrder").WithTags("Seller Orders");

app.MapPost("/api/orders/seller/{id}/ship", async (Guid id, OrderService service, ShipOrderRequest request, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    var userName = ctx.User.FindFirst("name")?.Value;
    var success = await service.ShipOrderAsync(id, request, userId, userName);
    return success ? Results.Ok(new { message = "Order shipped", status = "SHIPPED", tracking_number = request.TrackingNumber }) : Results.BadRequest(new { message = "Cannot ship this order" });
}).RequireAuthorization("SellerOnly").WithName("ShipOrder").WithTags("Seller Orders");

// ============================================
// Admin Order APIs
// ============================================

app.MapGet("/api/orders/admin", async (OrderService service, int page = 1, int pageSize = 20, string? status = null, Guid? userId = null, DateTime? from = null, DateTime? to = null) =>
{
    var orders = await service.GetAllOrdersAsync(page, pageSize, status, userId, from, to);
    return Results.Ok(orders);
}).RequireAuthorization("AdminOnly").WithName("GetAllOrders").WithTags("Admin");

app.MapGet("/api/orders/admin/{id}", async (Guid id, OrderService service) =>
{
    var order = await service.GetOrderByIdAsync(id);
    return order != null ? Results.Ok(order) : Results.NotFound();
}).RequireAuthorization("AdminOnly").WithName("AdminGetOrderById").WithTags("Admin");

app.MapPost("/api/orders/admin/{id}/deliver", async (Guid id, OrderService service, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    var userName = ctx.User.FindFirst("name")?.Value;
    var success = await service.DeliverOrderAsync(id, userId, userName);
    return success ? Results.Ok(new { message = "Order delivered", status = "DELIVERED" }) : Results.BadRequest(new { message = "Cannot mark as delivered" });
}).RequireAuthorization("AdminOnly").WithName("DeliverOrder").WithTags("Admin");

app.MapGet("/api/orders/admin/refunds", async (OrderService service, int page = 1, int pageSize = 20, string? status = null) =>
{
    var refunds = await service.GetRefundsAsync(page, pageSize, status);
    return Results.Ok(refunds);
}).RequireAuthorization("AdminOnly").WithName("GetRefunds").WithTags("Admin");

app.MapPut("/api/orders/admin/refunds/{refundId}", async (Guid refundId, OrderService service, ProcessRefundRequest request, HttpContext ctx) =>
{
    var userId = GetUserId(ctx);
    var userName = ctx.User.FindFirst("name")?.Value;
    var success = await service.ProcessRefundAsync(refundId, request, userId ?? Guid.Empty, userName);
    return success ? Results.Ok(new { message = "Refund processed", status = request.Status }) : Results.BadRequest(new { message = "Cannot process refund" });
}).RequireAuthorization("AdminOnly").WithName("ProcessRefund").WithTags("Admin");

// ============================================
// Internal APIs
// ============================================

app.MapPost("/api/orders/internal/sync-user-info", async (OrderService service, SyncUserInfoRequest request) =>
{
    await service.SyncUserInfoAsync(request.UserId, request.FullName, request.Email, request.Phone);
    return Results.Ok(new { message = "User info synced", user_id = request.UserId });
}).WithName("SyncUserInfo").WithTags("Internal");

// ============================================
// gRPC Test APIs
// ============================================

app.MapGet("/api/orders/grpc-test/inventory/{productId}", async (Guid productId, InventoryGrpcClient grpcClient) =>
{
    try
    {
        var response = await grpcClient.GetStockAsync(productId.ToString());
        return Results.Ok(new
        {
            method = "gRPC",
            product_id = response.ProductId,
            in_stock = response.InStock,
            available = response.AvailableQuantity,
            reserved = response.ReservedQuantity,
            warehouses = response.Warehouses.Count
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"gRPC Error: {ex.Message}");
    }
}).WithName("TestInventoryGrpc").WithTags("gRPC Test");

app.MapPost("/api/orders/grpc-test/discount/validate", async (TestDiscountRequest request, DiscountGrpcClient grpcClient) =>
{
    try
    {
        var items = request.Items.Select(i => (
            ProductId: i.ProductId.ToString(),
            CategoryId: i.CategoryId?.ToString(),
            Quantity: i.Quantity,
            UnitPrice: i.UnitPrice
        )).ToList();

        var response = await grpcClient.ValidateDiscountAsync(
            request.Code,
            request.UserId.ToString(),
            request.OrderAmount,
            items
        );

        return Results.Ok(new
        {
            method = "gRPC",
            valid = response.Valid,
            message = response.Message,
            discount_amount = response.DiscountAmount?.Amount ?? 0,
            discount_code = request.Code
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"gRPC Error: {ex.Message}");
    }
}).WithName("TestDiscountGrpc").WithTags("gRPC Test");

app.MapGet("/api/orders/grpc-test/status", () =>
{
    return Results.Ok(new
    {
        message = "Order Service - gRPC Implementation Active",
        grpc_enabled = new
        {
            inventory = useGrpcForInventory,
            discount = useGrpcForDiscount
        },
        grpc_urls = new
        {
            inventory = builder.Configuration["GrpcServices:Inventory"],
            discount = builder.Configuration["GrpcServices:Discount"]
        }
    });
}).WithName("GrpcStatus").WithTags("gRPC Test");

app.Run();

// Test DTOs
public record TestDiscountRequest(
    string Code,
    Guid UserId,
    decimal OrderAmount,
    List<TestOrderItem> Items
);

public record TestOrderItem(
    Guid ProductId,
    Guid? CategoryId,
    int Quantity,
    decimal UnitPrice
);
