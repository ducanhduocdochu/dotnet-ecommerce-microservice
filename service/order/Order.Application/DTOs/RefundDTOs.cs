namespace Order.Application.DTOs;

// Requests
public record CreateRefundRequest(
    decimal Amount,
    string Reason,
    string? RefundMethod = "ORIGINAL_PAYMENT",
    Guid? OrderItemId = null,
    string? BankName = null,
    string? BankAccount = null,
    string? BankAccountName = null
);

public record ProcessRefundRequest(
    string Status, // APPROVED or REJECTED
    string? AdminNote = null
);

// Responses
public record RefundResponse(
    Guid Id,
    Guid OrderId,
    string OrderNumber,
    Guid? OrderItemId,
    decimal Amount,
    string Reason,
    string Status,
    string? RefundMethod,
    string? BankName,
    string? BankAccount,
    string? ProcessedByName,
    DateTime? ProcessedAt,
    string? AdminNote,
    DateTime CreatedAt
);

