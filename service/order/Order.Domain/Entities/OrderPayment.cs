namespace Order.Domain.Entities;

public class OrderPayment
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public Guid? PaymentId { get; private set; } // Reference to Payment service
    
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "VND";
    public string PaymentMethod { get; private set; } = null!;
    public string PaymentStatus { get; private set; } = "PENDING";
    
    public string? TransactionId { get; private set; }
    public string? PaymentGateway { get; private set; }
    public string? GatewayResponse { get; private set; } // JSON
    
    public DateTime? PaidAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public OrderEntity? Order { get; set; }

    private OrderPayment() { }

    public OrderPayment(Guid orderId, decimal amount, string paymentMethod, string? paymentGateway = null)
    {
        OrderId = orderId;
        Amount = amount;
        PaymentMethod = paymentMethod;
        PaymentGateway = paymentGateway;
    }

    public void MarkAsPaid(string? transactionId, string? gatewayResponse = null)
    {
        PaymentStatus = "COMPLETED";
        TransactionId = transactionId;
        GatewayResponse = gatewayResponse;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string? gatewayResponse = null)
    {
        PaymentStatus = "FAILED";
        GatewayResponse = gatewayResponse;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsRefunded()
    {
        PaymentStatus = "REFUNDED";
        UpdatedAt = DateTime.UtcNow;
    }
}

