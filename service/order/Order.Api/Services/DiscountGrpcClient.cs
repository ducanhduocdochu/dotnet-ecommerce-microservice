using Grpc.Net.Client;
using Shared.Protos.Discount.V1;
using Shared.Protos.Common;

namespace Order.Api.Services;

public class DiscountGrpcClient
{
    private readonly ILogger<DiscountGrpcClient> _logger;
    private readonly string _discountGrpcUrl;

    public DiscountGrpcClient(ILogger<DiscountGrpcClient> logger, IConfiguration configuration)
    {
        _logger = logger;
        _discountGrpcUrl = configuration["GrpcServices:Discount"] ?? "http://localhost:5016";
    }

    // ============================================
    // ValidateDiscount - Validate discount code
    // ============================================
    public async Task<ValidateDiscountResponse> ValidateDiscountAsync(
        string code,
        string userId,
        decimal orderAmount,
        List<(string ProductId, string? CategoryId, int Quantity, decimal UnitPrice)> items)
    {
        using var channel = GrpcChannel.ForAddress(_discountGrpcUrl);
        var client = new DiscountService.DiscountServiceClient(channel);

        var request = new ValidateDiscountRequest
        {
            Code = code,
            UserId = userId,
            OrderAmount = new Money
            {
                Amount = (long)(orderAmount * 100), // Convert to cents
                Currency = "VND"
            }
        };

        foreach (var item in items)
        {
            request.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                CategoryId = item.CategoryId ?? "",
                Quantity = item.Quantity,
                UnitPrice = new Money
                {
                    Amount = (long)(item.UnitPrice * 100),
                    Currency = "VND"
                }
            });
        }

        try
        {
            _logger.LogInformation(
                "Calling Discount gRPC - ValidateDiscount for code {Code}, userId {UserId}",
                code, userId);

            var response = await client.ValidateDiscountAsync(request);

            _logger.LogInformation(
                "Discount gRPC ValidateDiscount result - Valid: {Valid}, Amount: {Amount}",
                response.Valid, response.DiscountAmount?.Amount ?? 0);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Discount gRPC ValidateDiscount");
            throw;
        }
    }

    // ============================================
    // ApplyDiscount - Apply discount to order
    // ============================================
    public async Task<ApplyDiscountResponse> ApplyDiscountAsync(
        string code,
        string userId,
        string orderId,
        string orderNumber,
        decimal orderAmount,
        List<(string ProductId, string? CategoryId, int Quantity, decimal UnitPrice)> items)
    {
        using var channel = GrpcChannel.ForAddress(_discountGrpcUrl);
        var client = new DiscountService.DiscountServiceClient(channel);

        var request = new ApplyDiscountRequest
        {
            Code = code,
            UserId = userId,
            OrderId = orderId,
            OrderNumber = orderNumber,
            OrderAmount = new Money
            {
                Amount = (long)(orderAmount * 100),
                Currency = "VND"
            }
        };

        foreach (var item in items)
        {
            request.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                CategoryId = item.CategoryId ?? "",
                Quantity = item.Quantity,
                UnitPrice = new Money
                {
                    Amount = (long)(item.UnitPrice * 100),
                    Currency = "VND"
                }
            });
        }

        try
        {
            _logger.LogInformation(
                "Calling Discount gRPC - ApplyDiscount for code {Code}, order {OrderId}",
                code, orderId);

            var response = await client.ApplyDiscountAsync(request);

            _logger.LogInformation(
                "Discount gRPC ApplyDiscount result - Success: {Success}, Amount: {Amount}",
                response.Success, response.DiscountAmount?.Amount ?? 0);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Discount gRPC ApplyDiscount");
            throw;
        }
    }

    // ============================================
    // RollbackUsage - Rollback discount usage when order cancelled
    // ============================================
    public async Task<RollbackUsageResponse> RollbackUsageAsync(
        string orderId,
        string discountId,
        string reason = "ORDER_CANCELLED")
    {
        using var channel = GrpcChannel.ForAddress(_discountGrpcUrl);
        var client = new DiscountService.DiscountServiceClient(channel);

        var request = new RollbackUsageRequest
        {
            OrderId = orderId,
            DiscountId = discountId,
            Reason = reason
        };

        try
        {
            _logger.LogInformation(
                "Calling Discount gRPC - RollbackUsage for order {OrderId}, discount {DiscountId}",
                orderId, discountId);

            var response = await client.RollbackUsageAsync(request);

            _logger.LogInformation(
                "Discount gRPC RollbackUsage result - Success: {Success}",
                response.Success);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Discount gRPC RollbackUsage");
            throw;
        }
    }

    // ============================================
    // GetActiveDiscounts - Get active discounts for user
    // ============================================
    public async Task<GetActiveDiscountsResponse> GetActiveDiscountsAsync(
        string userId,
        int page = 1,
        int pageSize = 20)
    {
        using var channel = GrpcChannel.ForAddress(_discountGrpcUrl);
        var client = new DiscountService.DiscountServiceClient(channel);

        var request = new GetActiveDiscountsRequest
        {
            UserId = userId,
            Page = page,
            PageSize = pageSize
        };

        try
        {
            _logger.LogInformation(
                "Calling Discount gRPC - GetActiveDiscounts for user {UserId}",
                userId);

            var response = await client.GetActiveDiscountsAsync(request);

            _logger.LogInformation(
                "Discount gRPC GetActiveDiscounts result - Count: {Count}",
                response.Discounts.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Discount gRPC GetActiveDiscounts");
            throw;
        }
    }
}

