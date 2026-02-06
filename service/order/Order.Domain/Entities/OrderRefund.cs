namespace Order.Domain.Entities;

public class OrderRefund
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public Guid? OrderItemId { get; private set; }
    
    public decimal Amount { get; private set; }
    public string Reason { get; private set; } = null!;
    public string Status { get; private set; } = "PENDING"; // PENDING, APPROVED, REJECTED, COMPLETED
    
    public string? RefundMethod { get; private set; } // ORIGINAL_PAYMENT, BANK_TRANSFER, WALLET
    public string? BankName { get; private set; }
    public string? BankAccount { get; private set; }
    public string? BankAccountName { get; private set; }
    
    public Guid? ProcessedBy { get; private set; }
    public string? ProcessedByName { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    
    public string? AdminNote { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public OrderEntity? Order { get; set; }
    public OrderItem? OrderItem { get; set; }

    private OrderRefund() { }

    public OrderRefund(Guid orderId, decimal amount, string reason, string? refundMethod = null, Guid? orderItemId = null)
    {
        OrderId = orderId;
        OrderItemId = orderItemId;
        Amount = amount;
        Reason = reason;
        RefundMethod = refundMethod;
    }

    public void SetBankInfo(string? bankName, string? bankAccount, string? bankAccountName)
    {
        BankName = bankName;
        BankAccount = bankAccount;
        BankAccountName = bankAccountName;
    }

    public void Approve(Guid processedBy, string? processedByName, string? adminNote = null)
    {
        Status = "APPROVED";
        ProcessedBy = processedBy;
        ProcessedByName = processedByName;
        ProcessedAt = DateTime.UtcNow;
        AdminNote = adminNote;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(Guid processedBy, string? processedByName, string? adminNote = null)
    {
        Status = "REJECTED";
        ProcessedBy = processedBy;
        ProcessedByName = processedByName;
        ProcessedAt = DateTime.UtcNow;
        AdminNote = adminNote;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = "COMPLETED";
        UpdatedAt = DateTime.UtcNow;
    }
}

