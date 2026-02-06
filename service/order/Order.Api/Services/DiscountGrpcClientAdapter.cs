using Order.Application.Clients;

namespace Order.Api.Services;

/// <summary>
/// Adapter to use gRPC DiscountGrpcClient as IDiscountClient
/// This allows seamless switch from HTTP to gRPC without changing OrderService
/// </summary>
public class DiscountGrpcClientAdapter : IDiscountClient
{
    private readonly DiscountGrpcClient _grpcClient;
    private readonly ILogger<DiscountGrpcClientAdapter> _logger;

    public DiscountGrpcClientAdapter(
        DiscountGrpcClient grpcClient,
        ILogger<DiscountGrpcClientAdapter> logger)
    {
        _grpcClient = grpcClient;
        _logger = logger;
    }

    public async Task<DiscountValidationResult> ValidateAsync(ValidateDiscountRequest request)
    {
        try
        {
            _logger.LogInformation("🔍 [gRPC] Validating discount code: {Code}", request.Code);

            // Need userId - this is a limitation of the adapter pattern
            // In real scenario, userId should be passed from OrderService
            var userId = Guid.Empty.ToString(); // TODO: Get actual user ID from context

            // Convert to gRPC format
            var items = request.Items.Select(i => (
                ProductId: i.ProductId.ToString(),
                CategoryId: i.CategoryId?.ToString(),
                Quantity: i.Quantity,
                UnitPrice: i.Price
            )).ToList();

            var grpcResponse = await _grpcClient.ValidateDiscountAsync(
                request.Code,
                userId,
                request.OrderAmount,
                items
            );

            if (!grpcResponse.Valid)
            {
                _logger.LogWarning("⚠️ [gRPC] Discount validation failed: {Message}", grpcResponse.Message);
                return new DiscountValidationResult(false, null, 0, grpcResponse.Message);
            }

            // Convert back to domain model
            DiscountInfo? discountInfo = null;
            if (grpcResponse.Discount != null)
            {
                discountInfo = new DiscountInfo(
                    Id: Guid.Parse(grpcResponse.Discount.Id),
                    Code: grpcResponse.Discount.Code,
                    Name: grpcResponse.Discount.Name,
                    Type: grpcResponse.Discount.Type,
                    Value: (decimal)grpcResponse.Discount.Value
                );
            }

            var discountAmount = (decimal)(grpcResponse.DiscountAmount?.Amount ?? 0) / 100m; // Convert from cents

            _logger.LogInformation("✅ [gRPC] Discount validated: {Code}, amount: {Amount}", request.Code, discountAmount);

            return new DiscountValidationResult(true, discountInfo, discountAmount, grpcResponse.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [gRPC] Error validating discount");
            return new DiscountValidationResult(false, null, 0, "Discount service error (gRPC)");
        }
    }

    public async Task<DiscountApplyResult> ApplyAsync(ApplyDiscountRequest request)
    {
        try
        {
            _logger.LogInformation("💰 [gRPC] Applying discount code: {Code} for order: {OrderId}", 
                request.Code, request.OrderId);

            // Need userId - this is a limitation of the adapter pattern
            var userId = Guid.Empty.ToString(); // TODO: Get actual user ID from context

            // Convert to gRPC format
            var items = request.Items.Select(i => (
                ProductId: i.ProductId.ToString(),
                CategoryId: i.CategoryId?.ToString(),
                Quantity: i.Quantity,
                UnitPrice: i.Price
            )).ToList();

            var grpcResponse = await _grpcClient.ApplyDiscountAsync(
                request.Code,
                userId,
                request.OrderId.ToString(),
                request.OrderNumber ?? "",
                request.OrderAmount,
                items
            );

            if (!grpcResponse.Success)
            {
                _logger.LogWarning("⚠️ [gRPC] Discount application failed: {Message}", grpcResponse.Message);
                return new DiscountApplyResult(false, null, 0, grpcResponse.Message);
            }

            var discountId = string.IsNullOrEmpty(grpcResponse.DiscountId) 
                ? (Guid?)null 
                : Guid.Parse(grpcResponse.DiscountId);
            
            var discountAmount = (decimal)(grpcResponse.DiscountAmount?.Amount ?? 0) / 100m; // Convert from cents

            _logger.LogInformation("✅ [gRPC] Discount applied successfully: {Code}, amount: {Amount}", 
                request.Code, discountAmount);

            return new DiscountApplyResult(true, discountId, discountAmount, grpcResponse.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [gRPC] Error applying discount");
            return new DiscountApplyResult(false, null, 0, "Discount service error (gRPC)");
        }
    }
}

