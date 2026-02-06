namespace Shared.Messaging.Events;

/// <summary>
/// Event published when payment is successful
/// Consumed by: Order (update status to CONFIRMED), Notification (send confirmation)
/// </summary>
public record PaymentSuccessEvent
{
    public Guid TransactionId { get; init; }
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    
    /// <summary>
    /// Payment details
    /// </summary>
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "VND";
    public string PaymentGateway { get; init; } = string.Empty; // VNPAY, MOMO, ZALOPAY
    public string PaymentMethod { get; init; } = string.Empty;  // BANK_TRANSFER, WALLET, CREDIT_CARD
    
    /// <summary>
    /// Gateway response
    /// </summary>
    public string? GatewayTransactionId { get; init; }
    public string? GatewayResponseCode { get; init; }
    
    public DateTime PaidAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when payment fails
/// Consumed by: Order (update status to PAYMENT_FAILED), Inventory (release reservation), Notification (send failure notice)
/// </summary>
public record PaymentFailedEvent
{
    public Guid TransactionId { get; init; }
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    
    /// <summary>
    /// Payment details
    /// </summary>
    public decimal Amount { get; init; }
    public string PaymentGateway { get; init; } = string.Empty;
    
    /// <summary>
    /// Error information
    /// </summary>
    public string ErrorCode { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public string FailureReason { get; init; } = string.Empty; // USER_CANCELLED, INSUFFICIENT_FUNDS, TIMEOUT, GATEWAY_ERROR
    
    /// <summary>
    /// Reservation IDs to release (passed from order)
    /// </summary>
    public List<Guid> ReservationIds { get; init; } = new();
    
    public DateTime FailedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a refund is processed
/// Consumed by: Order (update refund status), Notification (send refund confirmation)
/// </summary>
public record PaymentRefundedEvent
{
    public Guid RefundId { get; init; }
    public Guid TransactionId { get; init; }
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    
    /// <summary>
    /// Refund details
    /// </summary>
    public decimal RefundAmount { get; init; }
    public string RefundReason { get; init; } = string.Empty;
    public string RefundType { get; init; } = "FULL"; // FULL, PARTIAL
    
    /// <summary>
    /// Gateway response
    /// </summary>
    public string? GatewayRefundId { get; init; }
    public string RefundStatus { get; init; } = "COMPLETED"; // PENDING, COMPLETED, FAILED
    
    public DateTime RefundedAt { get; init; } = DateTime.UtcNow;
}

