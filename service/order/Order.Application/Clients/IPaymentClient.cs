namespace Order.Application.Clients;

/// <summary>
/// HTTP client for Payment Service
/// </summary>
public interface IPaymentClient
{
    /// <summary>
    /// Create payment transaction and get payment URL
    /// </summary>
    Task<CreatePaymentResult> CreatePaymentAsync(CreatePaymentRequest request);
    
    /// <summary>
    /// Get payment status
    /// </summary>
    Task<PaymentStatusResult> GetStatusAsync(Guid transactionId);
}

// Request/Response models
public record CreatePaymentRequest(
    Guid OrderId,
    string OrderNumber,
    Guid UserId,
    decimal Amount,
    string Currency,
    string PaymentGateway,     // VNPAY, MOMO, ZALOPAY
    string? PaymentMethod,     // BANK_TRANSFER, WALLET, CREDIT_CARD
    string? Description,
    string ReturnUrl,
    string CancelUrl,
    PaymentOrderInfo OrderInfo
);

public record PaymentOrderInfo(
    string CustomerName,
    string? CustomerEmail,
    string? CustomerPhone,
    List<PaymentOrderItem> Items
);

public record PaymentOrderItem(
    string Name,
    int Quantity,
    decimal Price
);

public record CreatePaymentResult(
    bool Success,
    Guid? TransactionId,
    string? PaymentUrl,
    string? Message
);

public record PaymentStatusResult(
    Guid TransactionId,
    Guid OrderId,
    string Status,              // PENDING, COMPLETED, FAILED, CANCELLED
    decimal Amount,
    string? GatewayTransactionId,
    DateTime? PaidAt
);

