using Grpc.Core;
using Shared.Protos.Inventory.V1;
using Inventory.Application.Services;
using Shared.Caching.Interfaces;
using Shared.Caching.Constants;
using InventoryDto = Inventory.Application.DTOs;

namespace Inventory.Api.Services;

public class InventoryGrpcService : Shared.Protos.Inventory.V1.InventoryService.InventoryServiceBase
{
    private readonly Application.Services.InventoryService _inventoryService;
    private readonly ICacheService _cache;
    private readonly ILogger<InventoryGrpcService> _logger;

    public InventoryGrpcService(
        Application.Services.InventoryService inventoryService,
        ICacheService cache,
        ILogger<InventoryGrpcService> logger)
    {
        _inventoryService = inventoryService;
        _cache = cache;
        _logger = logger;
    }

    // ============================================
    // CheckStock - Check availability for single product
    // ============================================
    public override async Task<CheckStockResponse> CheckStock(
        CheckStockRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "gRPC CheckStock called - ProductId: {ProductId}, Quantity: {Quantity}",
            request.ProductId, request.Quantity);

        try
        {
            if (!Guid.TryParse(request.ProductId, out var productId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid product ID"));
            }

            Guid? variantId = string.IsNullOrEmpty(request.VariantId) ? null : Guid.Parse(request.VariantId);

            // Create DTO request
            var checkRequest = new InventoryDto.CheckStockRequest(
                new List<InventoryDto.CheckStockItem>
                {
                    new InventoryDto.CheckStockItem(productId, variantId, request.Quantity)
                }
            );

            var result = await _inventoryService.CheckStockAsync(checkRequest);
            var item = result.Items.First();

            return new CheckStockResponse
            {
                Available = item.IsAvailable,
                AvailableQuantity = item.Available,
                WarehouseId = request.WarehouseId ?? "",
                Message = item.IsAvailable ? "In stock" : "Insufficient stock"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CheckStock gRPC call");
            throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
        }
    }

    // ============================================
    // CheckStockBatch - Check availability for multiple products
    // ============================================
    public override async Task<CheckStockBatchResponse> CheckStockBatch(
        CheckStockBatchRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC CheckStockBatch called - {Count} items", request.Items.Count);

        try
        {
            var items = request.Items.Select(i => new InventoryDto.CheckStockItem(
                Guid.Parse(i.ProductId),
                string.IsNullOrEmpty(i.VariantId) ? null : Guid.Parse(i.VariantId),
                i.Quantity
            )).ToList();

            var checkRequest = new InventoryDto.CheckStockRequest(items);
            var result = await _inventoryService.CheckStockAsync(checkRequest);

            var response = new CheckStockBatchResponse
            {
                AllAvailable = result.Available
            };

            foreach (var item in result.Items)
            {
                response.Items.Add(new StockItemAvailability
                {
                    ProductId = item.ProductId.ToString(),
                    VariantId = item.VariantId?.ToString() ?? "",
                    RequestedQuantity = item.Requested,
                    AvailableQuantity = item.Available,
                    IsAvailable = item.IsAvailable,
                    WarehouseId = ""
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CheckStockBatch gRPC call");
            throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
        }
    }

    // ============================================
    // ReserveStock - Reserve stock for order
    // ============================================
    public override async Task<ReserveStockResponse> ReserveStock(
        ReserveStockRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "gRPC ReserveStock called - OrderId: {OrderId}, Items: {Count}",
            request.OrderId, request.Items.Count);

        try
        {
            var orderId = Guid.Parse(request.OrderId);
            
            var items = request.Items.Select(i => new InventoryDto.ReserveStockItem(
                Guid.Parse(i.ProductId),
                string.IsNullOrEmpty(i.VariantId) ? null : Guid.Parse(i.VariantId),
                Guid.NewGuid(), // OrderItemId - generate new if not provided
                i.Quantity
            )).ToList();

            var reserveRequest = new InventoryDto.ReserveStockRequest(
                orderId,
                request.OrderNumber,
                items,
                request.ExpirationMinutes > 0 ? request.ExpirationMinutes : 30
            );

            var result = await _inventoryService.ReserveStockAsync(reserveRequest);

            return new ReserveStockResponse
            {
                Success = result.Success,
                ReservationIds = { result.ReservationIds.Select(id => id.ToString()) },
                ExpiresAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                    result.ExpiresAt.ToUniversalTime()),
                Message = "Stock reserved successfully"
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Stock reservation failed");
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ReserveStock gRPC call");
            throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
        }
    }

    // ============================================
    // CommitStock - Commit reserved stock after payment success
    // ============================================
    public override async Task<CommitStockResponse> CommitStock(
        CommitStockRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC CommitStock called - OrderId: {OrderId}", request.OrderId);

        try
        {
            var orderId = Guid.Parse(request.OrderId);
            var success = await _inventoryService.CommitStockAsync(orderId);

            if (!success)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "No reservations found for order"));
            }

            return new CommitStockResponse
            {
                Success = true,
                Message = "Stock committed successfully",
                ItemsCommitted = request.ReservationIds.Count
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CommitStock gRPC call");
            throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
        }
    }

    // ============================================
    // ReleaseStock - Release reserved stock on order cancel/payment fail
    // ============================================
    public override async Task<ReleaseStockResponse> ReleaseStock(
        ReleaseStockRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC ReleaseStock called - OrderId: {OrderId}", request.OrderId);

        try
        {
            var orderId = Guid.Parse(request.OrderId);
            
            var releaseRequest = new InventoryDto.ReleaseStockRequest(
                orderId,
                request.Reason
            );

            var success = await _inventoryService.ReleaseStockAsync(releaseRequest);

            if (!success)
            {
                _logger.LogWarning("No reservations found to release for order {OrderId}", orderId);
            }

            return new ReleaseStockResponse
            {
                Success = success,
                Message = success ? "Stock released successfully" : "No reservations found",
                ItemsReleased = request.ReservationIds.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ReleaseStock gRPC call");
            throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
        }
    }

    // ============================================
    // GetStock - Get stock information for display
    // ============================================
    public override async Task<GetStockResponse> GetStock(
        GetStockRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC GetStock called - ProductId: {ProductId}", request.ProductId);

        try
        {
            var productId = Guid.Parse(request.ProductId);
            Guid? variantId = string.IsNullOrEmpty(request.VariantId) ? null : Guid.Parse(request.VariantId);

            var result = await _inventoryService.GetProductStockAsync(productId, variantId);

            if (result == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Stock not found"));
            }

            var response = new GetStockResponse
            {
                ProductId = result.ProductId.ToString(),
                VariantId = result.VariantId?.ToString() ?? "",
                TotalQuantity = result.TotalQuantity,
                ReservedQuantity = result.TotalReserved,
                AvailableQuantity = result.TotalAvailable,
                InStock = result.TotalAvailable > 0,
                LowStock = result.TotalAvailable < 10,
                LowStockThreshold = 10
            };

            foreach (var warehouse in result.Warehouses)
            {
                response.Warehouses.Add(new WarehouseStock
                {
                    WarehouseId = warehouse.WarehouseId.ToString(),
                    WarehouseName = warehouse.WarehouseName,
                    Quantity = warehouse.Quantity,
                    Available = warehouse.Available,
                    Location = ""
                });
            }

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetStock gRPC call");
            throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
        }
    }
}

