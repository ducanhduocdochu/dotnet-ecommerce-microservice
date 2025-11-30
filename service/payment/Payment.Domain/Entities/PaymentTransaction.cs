namespace Payment.Domain.Entities;

public class PaymentTransaction
{
    public Guid Id { get; private set; }
    public string TransactionCode { get; private set; } = string.Empty;
    
    // Order Reference
    public Guid OrderId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    
    // User Info
    public Guid UserId { get; private set; }
    public string? UserEmail { get; private set; }
    public string? UserPhone { get; private set; }
    
    // Amount
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "VND";
    
    // Payment Info
    public string PaymentMethod { get; private set; } = string.Empty;
    public string? PaymentGateway { get; private set; }
    
    // Status
    public string Status { get; private set; } = "PENDING";
    
    // Gateway Response
    public string? GatewayTransactionId { get; private set; }
    public string? GatewayResponseCode { get; private set; }
    public string? GatewayResponseMessage { get; private set; }
    public string? GatewayResponseData { get; private set; } // JSON
    
    // URLs
    public string? PaymentUrl { get; private set; }
    public string? ReturnUrl { get; private set; }
    public string? CallbackUrl { get; private set; }
    
    // Timestamps
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? ExpiredAt { get; private set; }
    
    // Metadata
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Description { get; private set; }
    
    // Navigation
    public ICollection<RefundTransaction> Refunds { get; private set; } = new List<RefundTransaction>();
    public ICollection<PaymentLog> Logs { get; private set; } = new List<PaymentLog>();
    
    private PaymentTransaction() { }
    
    public PaymentTransaction(
        Guid orderId,
        string orderNumber,
        Guid userId,
        decimal amount,
        string paymentMethod,
        string? paymentGateway,
        string? returnUrl,
        string? description,
        string? userEmail,
        string? userPhone,
        string? ipAddress,
        string? userAgent)
    {
        Id = Guid.NewGuid();
        TransactionCode = GenerateTransactionCode();
        OrderId = orderId;
        OrderNumber = orderNumber;
        UserId = userId;
        Amount = amount;
        PaymentMethod = paymentMethod;
        PaymentGateway = paymentGateway;
        ReturnUrl = returnUrl;
        Description = description;
        UserEmail = userEmail;
        UserPhone = userPhone;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        ExpiredAt = DateTime.UtcNow.AddMinutes(30);
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    private static string GenerateTransactionCode()
    {
        return $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
    
    public void SetPaymentUrl(string paymentUrl, string? callbackUrl = null)
    {
        PaymentUrl = paymentUrl;
        CallbackUrl = callbackUrl;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void SetProcessing()
    {
        Status = "PROCESSING";
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Complete(string? gatewayTransactionId, string? responseCode, string? responseMessage, string? responseData)
    {
        Status = "COMPLETED";
        GatewayTransactionId = gatewayTransactionId;
        GatewayResponseCode = responseCode;
        GatewayResponseMessage = responseMessage;
        GatewayResponseData = responseData;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Fail(string? responseCode, string? responseMessage, string? responseData)
    {
        Status = "FAILED";
        GatewayResponseCode = responseCode;
        GatewayResponseMessage = responseMessage;
        GatewayResponseData = responseData;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Cancel()
    {
        Status = "CANCELLED";
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkRefunded()
    {
        Status = "REFUNDED";
        UpdatedAt = DateTime.UtcNow;
    }
    
    public bool CanCancel() => Status == "PENDING";
    public bool CanRefund() => Status == "COMPLETED";
    public bool IsExpired() => ExpiredAt.HasValue && DateTime.UtcNow > ExpiredAt.Value;
}

public static class PaymentStatus
{
    public const string Pending = "PENDING";
    public const string Processing = "PROCESSING";
    public const string Completed = "COMPLETED";
    public const string Failed = "FAILED";
    public const string Cancelled = "CANCELLED";
    public const string Refunded = "REFUNDED";
}

public static class PaymentMethods
{
    public const string Cod = "COD";
    public const string BankTransfer = "BANK_TRANSFER";
    public const string CreditCard = "CREDIT_CARD";
    public const string DebitCard = "DEBIT_CARD";
    public const string EWallet = "E_WALLET";
}

public static class PaymentGateways
{
    public const string VnPay = "VNPAY";
    public const string Momo = "MOMO";
    public const string ZaloPay = "ZALOPAY";
    public const string Cod = "COD";
    public const string BankTransfer = "BANK_TRANSFER";
}

