using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.Api.Middleware;

// Middleware để validate JWT token trước khi forward request đến services
public class JwtValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtValidationMiddleware> _logger;

    public JwtValidationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<JwtValidationMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        // ============================================
        // PUBLIC ROUTES - Không cần JWT validation
        // ============================================
        var publicRoutes = new[]
        {
            // Health check
            "/health",
            "/swagger",
            
            // Auth
            "/api/auth/register",
            "/api/auth/login",
            "/api/auth/refresh",
            "/api/auth/send-verification-email",
            "/api/auth/verify-email",
            "/api/auth/logout",
            
            // Payment Callbacks (Webhooks từ Payment Gateways)
            "/api/payments/callback",
            "/api/payments/return",
            
            // Inventory check (for product display)
            "/api/inventory/check",
            "/api/inventory/product",
            "/api/inventory/products"
        };
        
        // ============================================
        // PUBLIC GET ROUTES - GET requests không cần đăng nhập
        // ============================================
        var publicGetRoutes = new[]
        {
            "/api/products",          // View products
            "/api/categories",        // View categories
            "/api/discounts",         // View discounts/promotions
            "/api/discounts/promotions",
            "/api/discounts/flash-sales"
        };
        
        // Cho phép GET requests đến public GET routes
        if (context.Request.Method == "GET" && publicGetRoutes.Any(route => path.StartsWith(route)))
        {
            await _next(context);
            return;
        }

        // Cho phép các public routes
        if (publicRoutes.Any(route => path.StartsWith(route)))
        {
            await _next(context);
            return;
        }

        // ============================================
        // VALIDATE JWT TOKEN
        // ============================================
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            _logger.LogWarning("Unauthorized request to {Path} - No JWT token", path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { 
                error = "Unauthorized",
                message = "JWT token required" 
            });
            return;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        
        try
        {
            var secret = _configuration["Jwt:Secret"] ?? "ducanhdeptrai123_ducanhdeptrai123";
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            context.User = principal;

            _logger.LogDebug("JWT token validated successfully for {Path}", path);
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("JWT token expired for {Path}", path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { 
                error = "TokenExpired",
                message = "JWT token has expired" 
            });
            return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("JWT validation failed for {Path}: {Error}", path, ex.Message);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { 
                error = "InvalidToken",
                message = "Invalid JWT token" 
            });
            return;
        }

        await _next(context);
    }
}
