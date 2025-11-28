namespace User.Api.Middleware;

// Middleware để validate internal requests từ Gateway
public class InternalAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _internalSecret;
    private readonly ILogger<InternalAuthMiddleware> _logger;

    public InternalAuthMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<InternalAuthMiddleware> logger)
    {
        _next = next;
        _internalSecret = configuration["Internal:Secret"] ?? throw new ArgumentNullException("Internal:Secret");
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip validation cho public endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.StartsWith("/health") || path.StartsWith("/swagger"))
        {
            await _next(context);
            return;
        }

        // Check nếu request đến từ Gateway
        var isGatewayRequest = context.Request.Headers["X-Gateway-Request"].FirstOrDefault();
        
        if (isGatewayRequest == "true")
        {
            // Validate internal key từ Gateway
            var internalKey = context.Request.Headers["X-Internal-Key"].FirstOrDefault();
            
            if (internalKey != _internalSecret)
            {
                _logger.LogWarning("Unauthorized internal request from {IP}", context.Connection.RemoteIpAddress);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Forbidden: Invalid internal authentication");
                return;
            }
            
            _logger.LogDebug("Validated internal request from Gateway for {Path}", path);
        }
        // Nếu không có Gateway header, service sẽ validate JWT như bình thường

        await _next(context);
    }
}

