using Grpc.Net.Client;
using Shared.Protos.Inventory.V1;

namespace Order.Api.Services;

public class InventoryGrpcClient
{
    private readonly ILogger<InventoryGrpcClient> _logger;
    private readonly string _inventoryGrpcUrl;

    public InventoryGrpcClient(ILogger<InventoryGrpcClient> logger, IConfiguration configuration)
    {
        _logger = logger;
        _inventoryGrpcUrl = configuration["GrpcServices:Inventory"] ?? "http://localhost:5015";
    }

    // ============================================
    // CheckStock - Check availability for single product
    // ============================================
    public async Task<CheckStockResponse> CheckStockAsync(
        string productId,
        string? variantId,
        int quantity)
    {
        using var channel = GrpcChannel.ForAddress(_inventoryGrpcUrl);
        var client = new InventoryService.InventoryServiceClient(channel);

        var request = new CheckStockRequest
        {
            ProductId = productId,
            VariantId = variantId ?? "",
            Quantity = quantity
        };

        try
        {
            _logger.LogInformation(
                "Calling Inventory gRPC - CheckStock for product {ProductId}, quantity {Quantity}",
                productId, quantity);

            var response = await client.CheckStockAsync(request);
            
            _logger.LogInformation(
                "Inventory gRPC CheckStock result - Available: {Available}, Quantity: {Quantity}",
                response.Available, response.AvailableQuantity);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Inventory gRPC CheckStock");
            throw;
        }
    }

    // ============================================
    // CheckStockBatch - Check stock for multiple products
    // ============================================
    public async Task<CheckStockBatchResponse> CheckStockBatchAsync(
        List<(string ProductId, string? VariantId, int Quantity)> items)
    {
        using var channel = GrpcChannel.ForAddress(_inventoryGrpcUrl);
        var client = new InventoryService.InventoryServiceClient(channel);

        var request = new CheckStockBatchRequest();
        foreach (var item in items)
        {
            request.Items.Add(new StockItem
            {
                ProductId = item.ProductId,
                VariantId = item.VariantId ?? "",
                Quantity = item.Quantity
            });
        }

        try
        {
            _logger.LogInformation(
                "Calling Inventory gRPC - CheckStockBatch for {Count} items",
                items.Count);

            var response = await client.CheckStockBatchAsync(request);

            _logger.LogInformation(
                "Inventory gRPC CheckStockBatch result - AllAvailable: {AllAvailable}",
                response.AllAvailable);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Inventory gRPC CheckStockBatch");
            throw;
        }
    }

    // ============================================
    // ReserveStock - Reserve stock for order
    // ============================================
    public async Task<ReserveStockResponse> ReserveStockAsync(
        string orderId,
        string orderNumber,
        List<(string ProductId, string? VariantId, int Quantity)> items,
        int expirationMinutes = 30)
    {
        using var channel = GrpcChannel.ForAddress(_inventoryGrpcUrl);
        var client = new InventoryService.InventoryServiceClient(channel);

        var request = new ReserveStockRequest
        {
            OrderId = orderId,
            OrderNumber = orderNumber,
            ExpirationMinutes = expirationMinutes
        };

        foreach (var item in items)
        {
            request.Items.Add(new StockItem
            {
                ProductId = item.ProductId,
                VariantId = item.VariantId ?? "",
                Quantity = item.Quantity
            });
        }

        try
        {
            _logger.LogInformation(
                "Calling Inventory gRPC - ReserveStock for order {OrderId}, {Count} items",
                orderId, items.Count);

            var response = await client.ReserveStockAsync(request);

            _logger.LogInformation(
                "Inventory gRPC ReserveStock result - Success: {Success}, Reservations: {Count}",
                response.Success, response.ReservationIds.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Inventory gRPC ReserveStock");
            throw;
        }
    }

    // ============================================
    // CommitStock - Commit reserved stock after payment success
    // ============================================
    public async Task<CommitStockResponse> CommitStockAsync(
        string orderId,
        List<string> reservationIds)
    {
        using var channel = GrpcChannel.ForAddress(_inventoryGrpcUrl);
        var client = new InventoryService.InventoryServiceClient(channel);

        var request = new CommitStockRequest
        {
            OrderId = orderId
        };
        request.ReservationIds.AddRange(reservationIds);

        try
        {
            _logger.LogInformation(
                "Calling Inventory gRPC - CommitStock for order {OrderId}",
                orderId);

            var response = await client.CommitStockAsync(request);

            _logger.LogInformation(
                "Inventory gRPC CommitStock result - Success: {Success}",
                response.Success);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Inventory gRPC CommitStock");
            throw;
        }
    }

    // ============================================
    // ReleaseStock - Release reserved stock on cancel/failure
    // ============================================
    public async Task<ReleaseStockResponse> ReleaseStockAsync(
        string orderId,
        List<string> reservationIds,
        string reason = "ORDER_CANCELLED")
    {
        using var channel = GrpcChannel.ForAddress(_inventoryGrpcUrl);
        var client = new InventoryService.InventoryServiceClient(channel);

        var request = new ReleaseStockRequest
        {
            OrderId = orderId,
            Reason = reason
        };
        request.ReservationIds.AddRange(reservationIds);

        try
        {
            _logger.LogInformation(
                "Calling Inventory gRPC - ReleaseStock for order {OrderId}, reason: {Reason}",
                orderId, reason);

            var response = await client.ReleaseStockAsync(request);

            _logger.LogInformation(
                "Inventory gRPC ReleaseStock result - Success: {Success}",
                response.Success);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Inventory gRPC ReleaseStock");
            throw;
        }
    }

    // ============================================
    // GetStock - Get stock information for display
    // ============================================
    public async Task<GetStockResponse> GetStockAsync(
        string productId,
        string? variantId = null)
    {
        using var channel = GrpcChannel.ForAddress(_inventoryGrpcUrl);
        var client = new InventoryService.InventoryServiceClient(channel);

        var request = new GetStockRequest
        {
            ProductId = productId,
            VariantId = variantId ?? ""
        };

        try
        {
            _logger.LogInformation(
                "Calling Inventory gRPC - GetStock for product {ProductId}",
                productId);

            var response = await client.GetStockAsync(request);

            _logger.LogInformation(
                "Inventory gRPC GetStock result - InStock: {InStock}, Available: {Available}",
                response.InStock, response.AvailableQuantity);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Inventory gRPC GetStock");
            throw;
        }
    }
}

