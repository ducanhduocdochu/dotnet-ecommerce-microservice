using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Yarp.ReverseProxy.Configuration;
// upload image

var builder = WebApplication.CreateBuilder(args);

// Add YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// JWT Authentication for Gateway
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        var secret = builder.Configuration["Jwt:Secret"] ?? "ducanhdeptrai123_ducanhdeptrai123";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret))
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Public routes (no auth required)
    options.AddPolicy("Public", policy => policy.RequireAssertion(_ => true));
    
    // Authenticated routes
    options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
    
    // Admin routes
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// JWT Validation Middleware - Validate JWT token trước khi forward
app.UseMiddleware<Gateway.Api.Middleware.JwtValidationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Gateway Auth Middleware - Thêm internal headers khi forward request
app.UseMiddleware<Gateway.Api.Middleware.GatewayAuthMiddleware>();

// Health check endpoint (public)
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "gateway" }))
    .WithName("HealthCheck")
    .WithTags("Health")
    .AllowAnonymous();

// Map reverse proxy routes
app.MapReverseProxy();

// Helper endpoint để test Gateway
app.MapGet("/api/gateway/info", (IConfiguration config) =>
{
    var services = config.GetSection("Services").GetChildren()
        .ToDictionary(x => x.Key, x => x.Value);
    
    return Results.Ok(new
    {
        service = "API Gateway",
        version = "1.0.0",
        services = services,
        timestamp = DateTime.UtcNow
    });
})
.RequireAuthorization("Authenticated")
.WithName("GatewayInfo")
.WithTags("Gateway");

app.Run();

