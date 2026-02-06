using Order.Application.Clients;

namespace Order.Api.Services;

/// <summary>
/// Adapter to use gRPC InventoryGrpcClient as IInventoryClient
/// This allows seamless switch from HTTP to gRPC without changing OrderService
/// </summary>
public class InventoryGrpcClientAdapter : IInventoryClient
{
    private readonly InventoryGrpcClient _grpcClient;
    private readonly ILogger<InventoryGrpcClientAdapter> _logger;

    public InventoryGrpcClientAdapter(
        InventoryGrpcClient grpcClient,
        ILogger<InventoryGrpcClientAdapter> logger)
    {
        _grpcClient = grpcClient;
        _logger = logger;
    }

    public async Task<StockCheckResult> CheckStockAsync(CheckStockRequest request)
    {
        try
        {
            _logger.LogInformation("🔍 [gRPC] Checking stock for {Count} items", request.Items.Count);

            // Convert to gRPC format and call batch check
            var items = request.Items.Select(i => (
                ProductId: i.ProductId.ToString(),
                VariantId: i.VariantId?.ToString(),
                Quantity: i.Quantity
            )).ToList();

            var grpcResponse = await _grpcClient.CheckStockBatchAsync(items);

            // Convert back to domain model
            var results = grpcResponse.Items.Select(item => new StockCheckItemResult(
                ProductId: Guid.Parse(item.ProductId),
                VariantId: string.IsNullOrEmpty(item.VariantId) ? null : Guid.Parse(item.VariantId),
                RequestedQuantity: item.RequestedQuantity,
                AvailableQuantity: item.AvailableQuantity,
                IsAvailable: item.IsAvailable,
                WarehouseId: string.IsNullOrEmpty(item.WarehouseId) ? null : Guid.Parse(item.WarehouseId)
            )).ToList();

            var message = grpcResponse.AllAvailable ? "All items in stock" : "Some items out of stock";
            
            _logger.LogInformation("✅ [gRPC] Stock check completed - AllAvailable: {AllAvailable}", grpcResponse.AllAvailable);

            return new StockCheckResult(grpcResponse.AllAvailable, results, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [gRPC] Error checking stock");
            return new StockCheckResult(false, new List<StockCheckItemResult>(), "Inventory service error (gRPC)");
        }
    }

    public async Task<StockReserveResult> ReserveAsync(ReserveStockRequest request)
    {
        try
        {
            _logger.LogInformation("🔒 [gRPC] Reserving stock for order: {OrderId}", request.OrderId);

            // Convert to gRPC format
            var items = request.Items.Select(i => (
                ProductId: i.ProductId.ToString(),
                VariantId: i.VariantId?.ToString(),
                Quantity: i.Quantity
            )).ToList();

            var grpcResponse = await _grpcClient.ReserveStockAsync(
                request.OrderId.ToString(),
                request.OrderNumber ?? "",
                items,
                request.ExpirationMinutes
            );

            if (!grpcResponse.Success)
            {
                _logger.LogWarning("⚠️ [gRPC] Stock reservation failed: {Message}", grpcResponse.Message);
                return new StockReserveResult(false, new List<ReservationInfo>(), grpcResponse.Message);
            }

            // Convert back to domain model
            var reservations = grpcResponse.ReservationIds.Select(id => new ReservationInfo(
                ReservationId: Guid.Parse(id),
                ProductId: Guid.Empty, // Not provided in gRPC response
                VariantId: null,
                Quantity: 0, // Not provided in gRPC response
                WarehouseId: Guid.Empty, // Not provided in gRPC response
                ExpiresAt: grpcResponse.ExpiresAt.ToDateTime()
            )).ToList();

            _logger.LogInformation("✅ [gRPC] Stock reserved successfully for order: {OrderId}", request.OrderId);

            return new StockReserveResult(true, reservations, "Stock reserved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [gRPC] Error reserving stock");
            return new StockReserveResult(false, new List<ReservationInfo>(), "Inventory service error (gRPC)");
        }
    }

    public async Task<bool> ReleaseReservationAsync(ReleaseReservationRequest request)
    {
        try
        {
            _logger.LogInformation("🔓 [gRPC] Releasing reservation for order: {OrderId}", request.OrderId);

            var grpcResponse = await _grpcClient.ReleaseStockAsync(
                request.OrderId.ToString(),
                request.ReservationIds?.Select(id => id.ToString()).ToList() ?? new List<string>(),
                "ORDER_CANCELLED"
            );

            _logger.LogInformation("✅ [gRPC] Reservation released successfully for order: {OrderId}", request.OrderId);

            return grpcResponse.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [gRPC] Error releasing reservation");
            return false;
        }
    }
}

