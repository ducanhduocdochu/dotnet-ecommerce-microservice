namespace Gateway.Api.Middleware;

// Middleware để thêm internal authentication header khi forward request đến services
public class GatewayAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GatewayAuthMiddleware> _logger;

    public GatewayAuthMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<GatewayAuthMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Thêm internal authentication headers cho requests đến backend services
        // Chỉ thêm nếu request đang được forward (không phải từ client trực tiếp)
        if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            var internalSecret = _configuration["Internal:Secret"] ?? "internal-gateway-secret-key-2024";
            
            // Thêm internal key để services biết request đến từ Gateway
            context.Request.Headers.Add("X-Internal-Key", internalSecret);
            context.Request.Headers.Add("X-Gateway-Request", "true");
            context.Request.Headers.Add("X-Gateway-Timestamp", DateTime.UtcNow.ToString("O"));
            
            _logger.LogDebug("Added internal authentication headers for request to {Path}", context.Request.Path);
        }

        await _next(context);
    }
}

