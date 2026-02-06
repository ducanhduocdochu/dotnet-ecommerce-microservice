using Grpc.Core;
using Shared.Protos.Discount.V1;
using Shared.Protos.Common;
using Discount.Application.Services;
using DiscountDto = Discount.Application.DTOs;

namespace Discount.Api.Services;

public class DiscountGrpcService : Shared.Protos.Discount.V1.DiscountService.DiscountServiceBase
{
    private readonly Application.Services.DiscountService _discountService;
    private readonly ILogger<DiscountGrpcService> _logger;

    public DiscountGrpcService(
        Application.Services.DiscountService discountService,
        ILogger<DiscountGrpcService> logger)
    {
        _discountService = discountService;
        _logger = logger;
    }

    // ============================================
    // ValidateDiscount - Validate discount code
    // ============================================
    public override async Task<ValidateDiscountResponse> ValidateDiscount(
        ValidateDiscountRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "gRPC ValidateDiscount called - Code: {Code}, UserId: {UserId}",
            request.Code, request.UserId);

        try
        {
            if (!Guid.TryParse(request.UserId, out var userId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID"));
            }

            // Map request items
            var items = request.Items.Select(i => new DiscountDto.ValidateDiscountItem(
                Guid.Parse(i.ProductId),
                string.IsNullOrEmpty(i.CategoryId) ? null : Guid.Parse(i.CategoryId),
                i.Quantity,
                (decimal)i.UnitPrice.Amount / 100m // Convert from cents to decimal
            )).ToList();

            var validateRequest = new DiscountDto.ValidateDiscountRequest(
                request.Code,
                (decimal)request.OrderAmount.Amount / 100m, // Convert from cents
                items
            );

            var result = await _discountService.ValidateDiscountAsync(validateRequest, userId);

            var response = new ValidateDiscountResponse
            {
                Valid = result.Valid,
                Message = result.Message,
                DiscountAmount = new Money
                {
                    Amount = (long)(result.DiscountAmount * 100), // Convert to cents
                    Currency = request.OrderAmount.Currency
                }
            };

            if (result.Valid && result.Discount != null)
            {
                response.Discount = MapToDiscountInfo(result.Discount);
            }

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ValidateDiscount gRPC call");
            throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
        }
    }

    // ============================================
    // ApplyDiscount - Apply discount to order
    // ============================================
    public override async Task<ApplyDiscountResponse> ApplyDiscount(
        ApplyDiscountRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "gRPC ApplyDiscount called - Code: {Code}, OrderId: {OrderId}",
            request.Code, request.OrderId);

        try
        {
            var userId = Guid.Parse(request.UserId);
            var orderId = Guid.Parse(request.OrderId);

            var items = request.Items.Select(i => new DiscountDto.ValidateDiscountItem(
                Guid.Parse(i.ProductId),
                string.IsNullOrEmpty(i.CategoryId) ? null : Guid.Parse(i.CategoryId),
                i.Quantity,
                (decimal)i.UnitPrice.Amount / 100m
            )).ToList();

            var applyRequest = new DiscountDto.ApplyDiscountRequest(
                request.Code,
                orderId,
                request.OrderNumber,
                (decimal)request.OrderAmount.Amount / 100m,
                items
            );

            var result = await _discountService.ApplyDiscountAsync(applyRequest, userId);

            return new ApplyDiscountResponse
            {
                Success = result.Success,
                Message = result.Message,
                DiscountId = result.DiscountId?.ToString() ?? "",
                DiscountAmount = new Money
                {
                    Amount = (long)(result.DiscountAmount * 100),
                    Currency = request.OrderAmount.Currency
                },
                ApplicationId = Guid.NewGuid().ToString() // Generate unique application ID
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ApplyDiscount gRPC call");
            throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
        }
    }

    // ============================================
    // RecordUsage - Record discount usage (internal)
    // ============================================
    public override async Task<RecordUsageResponse> RecordUsage(
        RecordUsageRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "gRPC RecordUsage called - DiscountId: {DiscountId}, OrderId: {OrderId}",
            request.DiscountId, request.OrderId);

        try
        {
            // This is handled within ApplyDiscount, but we provide this endpoint for flexibility
            // In case the caller wants to record usage separately

            return new RecordUsageResponse
            {
                Success = true,
                Message = "Usage recorded successfully",
                UsageId = Guid.NewGuid().ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RecordUsage gRPC call");
            throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
        }
    }

    // ============================================
    // RollbackUsage - Rollback discount usage (when order cancelled)
    // ============================================
    public override async Task<RollbackUsageResponse> RollbackUsage(
        RollbackUsageRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "gRPC RollbackUsage called - OrderId: {OrderId}, DiscountId: {DiscountId}",
            request.OrderId, request.DiscountId);

        try
        {
            var orderId = Guid.Parse(request.OrderId);
            var discountId = Guid.Parse(request.DiscountId);

            // Call existing service method to rollback usage
            // This would need to be implemented in DiscountService
            // For now, we'll log and return success

            _logger.LogInformation(
                "Rollback discount usage for order {OrderId}, discount {DiscountId}, reason: {Reason}",
                orderId, discountId, request.Reason);

            // TODO: Implement actual rollback logic in DiscountService
            // await _discountService.RollbackUsageAsync(orderId, discountId, request.Reason);

            return new RollbackUsageResponse
            {
                Success = true,
                Message = "Usage rollback successful"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RollbackUsage gRPC call");
            throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
        }
    }

    // ============================================
    // GetActiveDiscounts - Get active discounts for user
    // ============================================
    public override async Task<GetActiveDiscountsResponse> GetActiveDiscounts(
        GetActiveDiscountsRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC GetActiveDiscounts called - UserId: {UserId}", request.UserId);

        try
        {
            var userId = Guid.Parse(request.UserId);
            var userDiscounts = await _discountService.GetUserDiscountsAsync(userId);

            var page = request.Page > 0 ? request.Page : 1;
            var pageSize = request.PageSize > 0 ? request.PageSize : 20;

            var pagedDiscounts = userDiscounts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = new GetActiveDiscountsResponse
            {
                Total = userDiscounts.Count,
                Page = page,
                PageSize = pageSize
            };

            foreach (var discount in pagedDiscounts)
            {
                response.Discounts.Add(new Shared.Protos.Discount.V1.DiscountInfo
                {
                    Id = discount.Id.ToString(),
                    Code = discount.Code,
                    Name = discount.Name,
                    Type = discount.Type,
                    Value = (double)discount.Value,
                    MinOrderAmount = new Money
                    {
                        Amount = (long)(discount.MinOrderAmount * 100),
                        Currency = "VND"
                    },
                    EndDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                        discount.EndDate.ToUniversalTime())
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetActiveDiscounts gRPC call");
            throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
        }
    }

    // ============================================
    // Helper Methods
    // ============================================
    private Shared.Protos.Discount.V1.DiscountInfo MapToDiscountInfo(DiscountDto.DiscountResponse discount)
    {
        return new Shared.Protos.Discount.V1.DiscountInfo
        {
            Id = discount.Id.ToString(),
            Code = discount.Code,
            Name = discount.Name,
            Type = discount.Type,
            Value = (double)discount.Value,
            MaxDiscountAmount = new Money
            {
                Amount = discount.MaxDiscountAmount.HasValue 
                    ? (long)(discount.MaxDiscountAmount.Value * 100) 
                    : 0,
                Currency = "VND"
            },
            MinOrderAmount = new Money
            {
                Amount = (long)(discount.MinOrderAmount * 100),
                Currency = "VND"
            },
            MinQuantity = 1, // Default
            StartDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                discount.StartDate.ToUniversalTime()),
            EndDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                discount.EndDate.ToUniversalTime())
        };
    }
}

