namespace User.Application.DTOs;

public record UserPaymentMethodResponse(
    Guid Id,
    Guid UserId,
    string PaymentType,
    string? Provider,
    string? CardLastFour,
    string? CardHolderName,
    int? ExpiryMonth,
    int? ExpiryYear,
    bool IsDefault,
    bool IsActive,
    Guid? BillingAddressId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

