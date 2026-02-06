using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Application.Services;
using Payment.Infrastructure.DB;
using Payment.Infrastructure.Repositories;
using Shared.Messaging.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// SERVICES
// ============================================

// Database
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DBConnectParam")));

// RabbitMQ
builder.Services.AddRabbitMQ(builder.Configuration);

// Repositories
builder.Services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
builder.Services.AddScoped<IRefundTransactionRepository, RefundTransactionRepository>();
builder.Services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
builder.Services.AddScoped<IPaymentLogRepository, PaymentLogRepository>();
builder.Services.AddScoped<IGatewayConfigRepository, GatewayConfigRepository>();

// Services
builder.Services.AddScoped<PaymentService>();

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
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Payment Service API", Version = "v1" });
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
    var dbContext = scope.ServiceProvider.GetRequiredService<Payment.Infrastructure.DB.PaymentDbContext>();
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
    return user.FindFirst(ClaimTypes.Name)?.Value ?? user.FindFirst("name")?.Value;
}

// ============================================
// DATABASE CONNECTION TEST
// ============================================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        await db.Database.EnsureCreatedAsync();
        logger.LogInformation("✅ Database connection successful - payment_db");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Database connection failed");
    }
}

// ============================================
// PAYMENT TRANSACTION APIs
// ============================================

// Create payment (called by Order service)
app.MapPost("/api/payments/create", async (
    CreatePaymentRequest request,
    ClaimsPrincipal user,
    PaymentService service,
    HttpContext ctx) =>
{
    var userId = GetUserId(user);
    var ipAddress = ctx.Connection.RemoteIpAddress?.ToString();
    var userAgent = ctx.Request.Headers.UserAgent.ToString();
    
    var result = await service.CreatePaymentAsync(userId, request, ipAddress, userAgent);
    return Results.Ok(result);
}).RequireAuthorization();

// Get payment by ID
app.MapGet("/api/payments/{id:guid}", async (
    Guid id,
    ClaimsPrincipal user,
    PaymentService service) =>
{
    var userId = GetUserId(user);
    var result = await service.GetPaymentByIdAsync(id, userId);
    return result != null ? Results.Ok(result) : Results.NotFound();
}).RequireAuthorization();

// Get payment by transaction code
app.MapGet("/api/payments/code/{transactionCode}", async (
    string transactionCode,
    ClaimsPrincipal user,
    PaymentService service) =>
{
    var userId = GetUserId(user);
    var result = await service.GetPaymentByCodeAsync(transactionCode, userId);
    return result != null ? Results.Ok(result) : Results.NotFound();
}).RequireAuthorization();

// Get payment status (for Order service)
app.MapGet("/api/payments/{id:guid}/status", async (
    Guid id,
    PaymentService service) =>
{
    var result = await service.GetPaymentByIdAsync(id);
    if (result == null) return Results.NotFound();
    return Results.Ok(new PaymentStatusResult(result.Id, result.OrderId, result.Status, result.Amount, result.GatewayTransactionId, result.PaidAt));
});

// Get my payment history
app.MapGet("/api/payments/me", async (
    int page,
    int pageSize,
    string? status,
    ClaimsPrincipal user,
    PaymentService service) =>
{
    var userId = GetUserId(user);
    var result = await service.GetUserPaymentsAsync(userId, page, pageSize, status);
    return Results.Ok(result);
}).RequireAuthorization();

// Cancel payment
app.MapPost("/api/payments/{id:guid}/cancel", async (
    Guid id,
    ClaimsPrincipal user,
    PaymentService service) =>
{
    var userId = GetUserId(user);
    var success = await service.CancelPaymentAsync(id, userId);
    return success 
        ? Results.Ok(new { message = "Payment cancelled", status = "CANCELLED" })
        : Results.BadRequest(new { message = "Cannot cancel this payment" });
}).RequireAuthorization();

// ============================================
// PAYMENT CALLBACK APIs (Webhooks)
// ============================================

// VNPay IPN callback
app.MapGet("/api/payments/callback/vnpay", async (
    HttpContext ctx,
    PaymentService service) =>
{
    var vnpParams = ctx.Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
    var ipAddress = ctx.Connection.RemoteIpAddress?.ToString();
    
    var (success, message) = await service.ProcessVnPayCallbackAsync(vnpParams, ipAddress);
    return Results.Ok(new VnPayCallbackResult(success ? "00" : "99", message));
});

// VNPay return URL
app.MapGet("/api/payments/return/vnpay", async (
    HttpContext ctx,
    PaymentService service,
    IConfiguration config) =>
{
    var vnpParams = ctx.Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
    var responseCode = vnpParams.GetValueOrDefault("vnp_ResponseCode");
    var txnRef = vnpParams.GetValueOrDefault("vnp_TxnRef");
    
    var frontendUrl = config["FrontendUrl"] ?? "http://localhost:3000";
    var status = responseCode == "00" ? "success" : "failed";
    
    return Results.Redirect($"{frontendUrl}/payment/result?status={status}&txn_ref={txnRef}");
});

// MoMo IPN callback
app.MapPost("/api/payments/callback/momo", async (
    HttpContext ctx,
    PaymentService service) =>
{
    var body = await ctx.Request.ReadFromJsonAsync<MomoCallbackRequest>();
    if (body == null) return Results.BadRequest();
    
    var (success, message) = await service.ProcessMomoCallbackAsync(
        body.PartnerCode, body.OrderId, body.ResultCode, body.Message, body.TransId);
    
    return Results.Ok(new MomoCallbackResult(success ? 0 : 1, message));
});

// ============================================
// SAVED PAYMENT METHODS APIs
// ============================================

app.MapGet("/api/payments/methods", async (
    ClaimsPrincipal user,
    PaymentService service) =>
{
    var userId = GetUserId(user);
    var result = await service.GetUserPaymentMethodsAsync(userId);
    return Results.Ok(new { items = result });
}).RequireAuthorization();

app.MapPost("/api/payments/methods", async (
    AddPaymentMethodRequest request,
    ClaimsPrincipal user,
    PaymentService service) =>
{
    var userId = GetUserId(user);
    var result = await service.AddPaymentMethodAsync(userId, request);
    return Results.Ok(new { method = result, message = "Payment method added" });
}).RequireAuthorization();

app.MapPut("/api/payments/methods/{id:guid}", async (
    Guid id,
    UpdatePaymentMethodRequest request,
    ClaimsPrincipal user,
    PaymentService service) =>
{
    var userId = GetUserId(user);
    var success = await service.UpdatePaymentMethodAsync(id, userId, request);
    return success 
        ? Results.Ok(new { id, message = "Payment method updated" })
        : Results.NotFound();
}).RequireAuthorization();

app.MapDelete("/api/payments/methods/{id:guid}", async (
    Guid id,
    ClaimsPrincipal user,
    PaymentService service) =>
{
    var userId = GetUserId(user);
    var success = await service.DeletePaymentMethodAsync(id, userId);
    return success 
        ? Results.Ok(new { message = "Payment method removed" })
        : Results.NotFound();
}).RequireAuthorization();

app.MapPut("/api/payments/methods/{id:guid}/default", async (
    Guid id,
    ClaimsPrincipal user,
    PaymentService service) =>
{
    var userId = GetUserId(user);
    var success = await service.SetDefaultPaymentMethodAsync(id, userId);
    return success 
        ? Results.Ok(new { message = "Default payment method updated" })
        : Results.NotFound();
}).RequireAuthorization();

// ============================================
// REFUND APIs
// ============================================

app.MapPost("/api/payments/{paymentId:guid}/refund", async (
    Guid paymentId,
    CreateRefundRequest request,
    ClaimsPrincipal user,
    PaymentService service) =>
{
    var userId = GetUserId(user);
    var result = await service.CreateRefundAsync(paymentId, userId, request);
    return result != null 
        ? Results.Ok(new { refund = result, message = "Refund request submitted" })
        : Results.BadRequest(new { message = "Cannot create refund" });
}).RequireAuthorization();

app.MapGet("/api/payments/refunds/{refundId:guid}", async (
    Guid refundId,
    ClaimsPrincipal user,
    PaymentService service) =>
{
    var userId = GetUserId(user);
    var result = await service.GetRefundByIdAsync(refundId, userId);
    return result != null ? Results.Ok(result) : Results.NotFound();
}).RequireAuthorization();

app.MapGet("/api/payments/refunds/me", async (
    int page,
    int pageSize,
    string? status,
    ClaimsPrincipal user,
    PaymentService service) =>
{
    var userId = GetUserId(user);
    var result = await service.GetUserRefundsAsync(userId, page, pageSize, status);
    return Results.Ok(result);
}).RequireAuthorization();

// ============================================
// ADMIN APIs
// ============================================

app.MapGet("/api/payments/admin/transactions", async (
    int page,
    int pageSize,
    string? status,
    string? gateway,
    DateTime? from,
    DateTime? to,
    PaymentService service) =>
{
    var result = await service.GetAllTransactionsAsync(page, pageSize, status, gateway, from, to);
    return Results.Ok(result);
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/payments/admin/transactions/{id:guid}", async (
    Guid id,
    PaymentService service) =>
{
    var result = await service.GetPaymentByIdAsync(id);
    return result != null ? Results.Ok(result) : Results.NotFound();
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/payments/admin/refunds", async (
    int page,
    int pageSize,
    string? status,
    PaymentService service) =>
{
    var result = await service.GetAllRefundsAsync(page, pageSize, status);
    return Results.Ok(result);
}).RequireAuthorization("AdminOnly");

app.MapPut("/api/payments/admin/refunds/{refundId:guid}", async (
    Guid refundId,
    ProcessRefundRequest request,
    ClaimsPrincipal user,
    PaymentService service) =>
{
    var userId = GetUserId(user);
    var userName = GetUserName(user);
    var success = await service.ProcessRefundAsync(refundId, request, userId, userName);
    return success 
        ? Results.Ok(new { id = refundId, message = $"Refund {request.Action.ToLower()}d" })
        : Results.BadRequest(new { message = "Cannot process refund" });
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/payments/admin/refunds/{refundId:guid}/execute", async (
    Guid refundId,
    PaymentService service) =>
{
    var success = await service.ExecuteRefundAsync(refundId);
    return success 
        ? Results.Ok(new { id = refundId, message = "Refund executed" })
        : Results.BadRequest(new { message = "Cannot execute refund" });
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/payments/admin/gateways", async (PaymentService service) =>
{
    var result = await service.GetGatewayConfigsAsync();
    return Results.Ok(new { items = result });
}).RequireAuthorization("AdminOnly");

app.MapPut("/api/payments/admin/gateways/{gatewayCode}", async (
    string gatewayCode,
    UpdateGatewayConfigRequest request,
    PaymentService service) =>
{
    var success = await service.UpdateGatewayConfigAsync(gatewayCode, request);
    return success 
        ? Results.Ok(new { gateway_code = gatewayCode, message = "Gateway config updated" })
        : Results.NotFound();
}).RequireAuthorization("AdminOnly");

// ============================================
// INTERNAL APIs
// ============================================

app.MapGet("/api/payments/internal/verify/{orderId:guid}", async (
    Guid orderId,
    PaymentService service) =>
{
    var result = await service.VerifyPaymentAsync(orderId);
    return Results.Ok(result);
});

app.Run();

// ============================================
// MOMO CALLBACK DTO
// ============================================
public record MomoCallbackRequest(
    string PartnerCode,
    string OrderId,
    string RequestId,
    long Amount,
    string OrderInfo,
    string OrderType,
    string TransId,
    int ResultCode,
    string Message,
    string PayType,
    long ResponseTime,
    string ExtraData,
    string Signature
);
