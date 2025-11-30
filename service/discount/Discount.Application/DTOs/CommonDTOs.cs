namespace Discount.Application.DTOs;

public record PagedResult<T>(
    List<T> Items,
    int Total,
    int Page,
    int PageSize
);

public record RecordUsageRequest(
    Guid DiscountId,
    Guid UserId,
    Guid OrderId,
    string? OrderNumber,
    decimal OrderAmount,
    decimal DiscountAmount
);

public record RollbackUsageRequest(
    Guid OrderId
);

public record GetProductDiscountsRequest(
    List<Guid> ProductIds
);

public record ProductDiscountResponse(
    Guid ProductId,
    List<DiscountResponse> Discounts
);

