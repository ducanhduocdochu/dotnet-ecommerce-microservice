namespace User.Application.DTOs;

public record CreatePaymentMethodRequest(
    string PaymentType,
    string? Provider,
    string? CardNumber,
    string? CardHolderName,
    int? ExpiryMonth,
    int? ExpiryYear,
    string? Cvv,
    Guid? BillingAddressId,
    bool IsDefault
);

