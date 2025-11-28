namespace User.Application.DTOs;

public record UpdatePaymentMethodRequest(
    string? CardHolderName,
    int? ExpiryMonth,
    int? ExpiryYear,
    Guid? BillingAddressId,
    bool IsDefault,
    bool IsActive
);

