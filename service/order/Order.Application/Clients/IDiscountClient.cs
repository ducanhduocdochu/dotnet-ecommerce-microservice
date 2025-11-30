namespace Order.Application.Clients;

/// <summary>
/// HTTP client for Discount Service
/// </summary>
public interface IDiscountClient
{
    /// <summary>
    /// Validate discount code before checkout
    /// </summary>
    Task<DiscountValidationResult> ValidateAsync(ValidateDiscountRequest request);
    
    /// <summary>
    /// Apply discount to order (locks the usage)
    /// </summary>
    Task<DiscountApplyResult> ApplyAsync(ApplyDiscountRequest request);
}

// Request/Response models
public record ValidateDiscountRequest(
    string Code,
    decimal OrderAmount,
    List<ValidateDiscountItem> Items
);

public record ValidateDiscountItem(
    Guid ProductId,
    Guid? CategoryId,
    int Quantity,
    decimal Price
);

public record ApplyDiscountRequest(
    string Code,
    Guid OrderId,
    string? OrderNumber,
    decimal OrderAmount,
    List<ValidateDiscountItem> Items
);

public record DiscountValidationResult(
    bool Valid,
    DiscountInfo? Discount,
    decimal DiscountAmount,
    string Message
);

public record DiscountApplyResult(
    bool Success,
    Guid? DiscountId,
    decimal DiscountAmount,
    string Message
);

public record DiscountInfo(
    Guid Id,
    string Code,
    string Name,
    string Type,
    decimal Value
);

