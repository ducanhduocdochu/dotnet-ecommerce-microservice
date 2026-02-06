namespace Inventory.Application.DTOs;

public record TransactionResponse(
    Guid Id,
    string Type,
    int QuantityChange,
    int QuantityBefore,
    int QuantityAfter,
    string? ReferenceType,
    string? ReferenceCode,
    string? Reason,
    string? CreatedByName,
    DateTime CreatedAt
);

public record PagedResponse<T>(
    List<T> Items,
    int Total,
    int Page,
    int PageSize
);

