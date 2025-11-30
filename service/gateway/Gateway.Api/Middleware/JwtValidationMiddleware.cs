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
        
        // Public routes không cần JWT validation
        var publicRoutes = new[]
        {
            "/health",
            "/swagger",
            "/api/auth/register",
            "/api/auth/login",
            "/api/auth/refresh",
            "/api/auth/send-verification-email",
            "/api/auth/verify-email",
            "/api/auth/logout"  // Logout cũng nên public vì chỉ cần refresh_token
        };
        
        // Routes cho phép GET public (xem sản phẩm, danh mục không cần đăng nhập)
        var publicGetRoutes = new[]
        {
            "/api/products",
            "/api/discounts"
        };
        
        // Cho phép GET requests đến public GET routes
        if (context.Request.Method == "GET" && publicGetRoutes.Any(route => path.StartsWith(route)))
        {
            await _next(context);
            return;
        }

        if (publicRoutes.Any(route => path.StartsWith(route)))
        {
            await _next(context);
            return;
        }

        // Validate JWT token cho các routes cần authentication
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            _logger.LogWarning("Unauthorized request to {Path} - No JWT token", path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized: JWT token required");
            return;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        
        try
        {
            var secret = _configuration["Jwt:Secret"] ?? "ducanhdeptrai123";
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
        catch (Exception ex)
        {
            _logger.LogWarning("JWT validation failed for {Path}: {Error}", path, ex.Message);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized: Invalid JWT token");
            return;
        }

        await _next(context);
    }
}

