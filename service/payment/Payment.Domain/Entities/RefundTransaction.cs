namespace Payment.Domain.Entities;

public class RefundTransaction
{
    public Guid Id { get; private set; }
    public string RefundCode { get; private set; } = string.Empty;
    
    // Original Transaction
    public Guid PaymentTransactionId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid? OrderRefundId { get; private set; }
    
    // User Info
    public Guid UserId { get; private set; }
    
    // Amount
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "VND";
    
    // Refund Method
    public string RefundMethod { get; private set; } = string.Empty;
    
    // Bank Transfer Info
    public string? BankCode { get; private set; }
    public string? BankName { get; private set; }
    public string? BankAccount { get; private set; }
    public string? BankAccountName { get; private set; }
    
    // Status
    public string Status { get; private set; } = "PENDING";
    
    // Gateway Response
    public string? GatewayRefundId { get; private set; }
    public string? GatewayResponseCode { get; private set; }
    public string? GatewayResponseMessage { get; private set; }
    public string? GatewayResponseData { get; private set; }
    
    // Reason & Notes
    public string Reason { get; private set; } = string.Empty;
    public string? AdminNote { get; private set; }
    
    // Processed By
    public Guid? ProcessedBy { get; private set; }
    public string? ProcessedByName { get; private set; }
    
    // Timestamps
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    // Navigation
    public PaymentTransaction? PaymentTransaction { get; private set; }
    
    private RefundTransaction() { }
    
    public RefundTransaction(
        Guid paymentTransactionId,
        Guid orderId,
        Guid userId,
        decimal amount,
        string reason,
        string refundMethod,
        Guid? orderRefundId = null)
    {
        Id = Guid.NewGuid();
        RefundCode = GenerateRefundCode();
        PaymentTransactionId = paymentTransactionId;
        OrderId = orderId;
        UserId = userId;
        Amount = amount;
        Reason = reason;
        RefundMethod = refundMethod;
        OrderRefundId = orderRefundId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    private static string GenerateRefundCode()
    {
        return $"REF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
    
    public void SetBankInfo(string? bankCode, string? bankName, string? bankAccount, string? bankAccountName)
    {
        BankCode = bankCode;
        BankName = bankName;
        BankAccount = bankAccount;
        BankAccountName = bankAccountName;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Approve(Guid processedBy, string? processedByName, string? adminNote)
    {
        Status = "APPROVED";
        ProcessedBy = processedBy;
        ProcessedByName = processedByName;
        AdminNote = adminNote;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Reject(Guid processedBy, string? processedByName, string? adminNote)
    {
        Status = "REJECTED";
        ProcessedBy = processedBy;
        ProcessedByName = processedByName;
        AdminNote = adminNote;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Process()
    {
        Status = "PROCESSING";
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Complete(string? gatewayRefundId, string? responseCode, string? responseMessage)
    {
        Status = "COMPLETED";
        GatewayRefundId = gatewayRefundId;
        GatewayResponseCode = responseCode;
        GatewayResponseMessage = responseMessage;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Fail(string? responseCode, string? responseMessage)
    {
        Status = "FAILED";
        GatewayResponseCode = responseCode;
        GatewayResponseMessage = responseMessage;
        UpdatedAt = DateTime.UtcNow;
    }
}

public static class RefundStatus
{
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Processing = "PROCESSING";
    public const string Completed = "COMPLETED";
    public const string Rejected = "REJECTED";
    public const string Failed = "FAILED";
}

public static class RefundMethods
{
    public const string OriginalPayment = "ORIGINAL_PAYMENT";
    public const string BankTransfer = "BANK_TRANSFER";
    public const string Wallet = "WALLET";
}

