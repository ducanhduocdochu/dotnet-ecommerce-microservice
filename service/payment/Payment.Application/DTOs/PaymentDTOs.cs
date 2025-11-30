namespace Payment.Application.DTOs;

// ============================================
// PAYMENT TRANSACTION DTOs
// ============================================

public record CreatePaymentRequest(
    Guid OrderId,
    string OrderNumber,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string? PaymentGateway,
    string? ReturnUrl,
    string? Description,
    string? UserEmail,
    string? UserPhone,
    Guid? SavedMethodId
);

public record PaymentResponse(
    Guid Id,
    string TransactionCode,
    Guid OrderId,
    string OrderNumber,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string? PaymentGateway,
    string Status,
    string? GatewayTransactionId,
    string? PaymentUrl,
    DateTime? PaidAt,
    DateTime? ExpiredAt,
    DateTime CreatedAt
);

public record CreatePaymentResult(
    Guid Id,
    string TransactionCode,
    string Status,
    string? PaymentUrl,
    DateTime? ExpiredAt
);

public record PaymentStatusResult(
    Guid Id,
    Guid OrderId,
    string Status,
    decimal Amount,
    string? GatewayTransactionId,
    DateTime? PaidAt
);

// ============================================
// SAVED PAYMENT METHOD DTOs
// ============================================

public record AddPaymentMethodRequest(
    string Type,
    string? Provider,
    string? CardToken,
    string? CardLast4,
    string? CardBrand,
    int? CardExpMonth,
    int? CardExpYear,
    string? CardHolderName,
    string? BankCode,
    string? BankName,
    string? AccountNumber,
    string? AccountHolderName,
    string? WalletPhone,
    string? Nickname,
    bool IsDefault
);

public record UpdatePaymentMethodRequest(
    string? Nickname,
    bool? IsDefault
);

public record PaymentMethodResponse(
    Guid Id,
    string Type,
    string? Provider,
    string? CardLast4,
    string? CardBrand,
    int? CardExpMonth,
    int? CardExpYear,
    string? BankName,
    string? AccountNumberMasked,
    string? WalletPhone,
    bool IsDefault,
    bool IsVerified,
    string? Nickname
);

// ============================================
// REFUND DTOs
// ============================================

public record CreateRefundRequest(
    decimal Amount,
    string Reason,
    string RefundMethod,
    Guid? OrderRefundId,
    string? BankCode,
    string? BankName,
    string? BankAccount,
    string? BankAccountName
);

public record RefundResponse(
    Guid Id,
    string RefundCode,
    Guid PaymentTransactionId,
    Guid OrderId,
    decimal Amount,
    string Status,
    string RefundMethod,
    string Reason,
    string? AdminNote,
    DateTime? CompletedAt,
    DateTime CreatedAt
);

public record ProcessRefundRequest(
    string Action, // APPROVE, REJECT
    string? AdminNote
);

// ============================================
// CALLBACK DTOs
// ============================================

public record VnPayCallbackResult(
    string RspCode,
    string Message
);

public record MomoCallbackResult(
    int Status,
    string Message
);

public record ZaloPayCallbackResult(
    int ReturnCode,
    string ReturnMessage
);

// ============================================
// ADMIN DTOs
// ============================================

public record PaymentStatisticsResponse(
    int TotalTransactions,
    decimal TotalAmount,
    decimal SuccessRate,
    Dictionary<string, int> TransactionsByGateway,
    Dictionary<string, int> TransactionsByMethod,
    List<DailyStatResponse> DailyStats
);

public record DailyStatResponse(
    DateTime Date,
    int Count,
    decimal Amount,
    int SuccessCount
);

public record GatewayConfigResponse(
    Guid Id,
    string GatewayCode,
    string GatewayName,
    bool IsActive,
    bool IsSandbox,
    string[]? SupportedMethods,
    decimal FeePercent,
    decimal FeeFixed,
    decimal MinAmount,
    decimal MaxAmount
);

public record UpdateGatewayConfigRequest(
    bool? IsActive,
    bool? IsSandbox,
    string? ConfigData,
    decimal? FeePercent,
    decimal? FeeFixed
);

// ============================================
// COMMON DTOs
// ============================================

public record PagedResult<T>(
    List<T> Items,
    int Total,
    int Page,
    int PageSize
);

public record InternalVerifyResponse(
    Guid OrderId,
    bool IsPaid,
    Guid? PaymentId,
    DateTime? PaidAt,
    decimal? Amount
);

