namespace Payment.Domain.Entities;

public class PaymentLog
{
    public Guid Id { get; private set; }
    public Guid? TransactionId { get; private set; }
    public Guid? RefundId { get; private set; }
    
    public string Action { get; private set; } = string.Empty;
    public string? Status { get; private set; }
    
    public string? RequestData { get; private set; }
    public string? ResponseData { get; private set; }
    
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    
    // Navigation
    public PaymentTransaction? PaymentTransaction { get; private set; }
    public RefundTransaction? RefundTransaction { get; private set; }
    
    private PaymentLog() { }
    
    public PaymentLog(
        Guid? transactionId,
        Guid? refundId,
        string action,
        string? status,
        string? requestData,
        string? responseData,
        string? ipAddress,
        string? userAgent)
    {
        Id = Guid.NewGuid();
        TransactionId = transactionId;
        RefundId = refundId;
        Action = action;
        Status = status;
        RequestData = requestData;
        ResponseData = responseData;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CreatedAt = DateTime.UtcNow;
    }
}

public static class PaymentLogActions
{
    public const string Create = "CREATE";
    public const string Process = "PROCESS";
    public const string Callback = "CALLBACK";
    public const string Query = "QUERY";
    public const string Refund = "REFUND";
    public const string Cancel = "CANCEL";
}

