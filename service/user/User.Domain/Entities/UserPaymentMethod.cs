namespace User.Domain.Entities;

public class UserPaymentMethod
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string PaymentType { get; private set; } // CREDIT_CARD, DEBIT_CARD, PAYPAL, BANK_TRANSFER
    public string? Provider { get; private set; } // VISA, MASTERCARD, PAYPAL, etc.
    public string? CardLastFour { get; private set; }
    public string? CardHolderName { get; private set; }
    public int? ExpiryMonth { get; private set; }
    public int? ExpiryYear { get; private set; }
    public bool IsDefault { get; private set; } = false;
    public bool IsActive { get; private set; } = true;
    public Guid? BillingAddressId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public UserPaymentMethod(
        Guid userId,
        string paymentType,
        string? provider,
        string? cardLastFour,
        string? cardHolderName,
        int? expiryMonth,
        int? expiryYear,
        bool isDefault,
        Guid? billingAddressId)
    {
        UserId = userId;
        PaymentType = paymentType;
        Provider = provider;
        CardLastFour = cardLastFour;
        CardHolderName = cardHolderName;
        ExpiryMonth = expiryMonth;
        ExpiryYear = expiryYear;
        IsDefault = isDefault;
        BillingAddressId = billingAddressId;
    }

    public void Update(
        string? cardHolderName,
        int? expiryMonth,
        int? expiryYear,
        Guid? billingAddressId,
        bool isDefault,
        bool isActive)
    {
        CardHolderName = cardHolderName;
        ExpiryMonth = expiryMonth;
        ExpiryYear = expiryYear;
        BillingAddressId = billingAddressId;
        IsDefault = isDefault;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAsDefault() => IsDefault = true;
    public void RemoveDefault() => IsDefault = false;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

