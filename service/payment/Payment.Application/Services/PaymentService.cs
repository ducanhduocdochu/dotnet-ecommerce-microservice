using System.Text.Json;
using Microsoft.Extensions.Logging;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Shared.Messaging.Events;
using Shared.Messaging.RabbitMQ;

namespace Payment.Application.Services;

public class PaymentService
{
    private readonly IPaymentTransactionRepository _transactionRepository;
    private readonly IRefundTransactionRepository _refundRepository;
    private readonly IPaymentMethodRepository _methodRepository;
    private readonly IPaymentLogRepository _logRepository;
    private readonly IGatewayConfigRepository _gatewayConfigRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentTransactionRepository transactionRepository,
        IRefundTransactionRepository refundRepository,
        IPaymentMethodRepository methodRepository,
        IPaymentLogRepository logRepository,
        IGatewayConfigRepository gatewayConfigRepository,
        IEventPublisher eventPublisher,
        ILogger<PaymentService> logger)
    {
        _transactionRepository = transactionRepository;
        _refundRepository = refundRepository;
        _methodRepository = methodRepository;
        _logRepository = logRepository;
        _gatewayConfigRepository = gatewayConfigRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    // ============================================
    // PAYMENT TRANSACTION
    // ============================================

    public async Task<CreatePaymentResult> CreatePaymentAsync(Guid userId, CreatePaymentRequest request, string? ipAddress, string? userAgent)
    {
        _logger.LogInformation("💳 Creating payment for order {OrderId}, amount: {Amount}", request.OrderId, request.Amount);

        // Check if payment already exists for this order
        var existingPayment = await _transactionRepository.GetByOrderIdAsync(request.OrderId);
        if (existingPayment != null && existingPayment.Status == PaymentStatus.Pending)
        {
            // Return existing pending payment
            return new CreatePaymentResult(
                existingPayment.Id,
                existingPayment.TransactionCode,
                existingPayment.Status,
                existingPayment.PaymentUrl,
                existingPayment.ExpiredAt
            );
        }

        var transaction = new PaymentTransaction(
            request.OrderId,
            request.OrderNumber,
            userId,
            request.Amount,
            request.PaymentMethod,
            request.PaymentGateway,
            request.ReturnUrl,
            request.Description,
            request.UserEmail,
            request.UserPhone,
            ipAddress,
            userAgent
        );

        await _transactionRepository.AddAsync(transaction);
        await _transactionRepository.SaveChangesAsync();

        // Generate payment URL based on gateway
        string? paymentUrl = null;
        if (request.PaymentGateway != null && request.PaymentGateway != PaymentGateways.Cod)
        {
            paymentUrl = await GeneratePaymentUrlAsync(transaction, request.PaymentGateway);
            transaction.SetPaymentUrl(paymentUrl);
            await _transactionRepository.SaveChangesAsync();
        }

        // Log
        await LogPaymentActionAsync(transaction.Id, null, PaymentLogActions.Create, PaymentStatus.Pending,
            JsonSerializer.Serialize(request), null, ipAddress, userAgent);

        _logger.LogInformation("✅ Payment created: {TransactionCode}", transaction.TransactionCode);

        return new CreatePaymentResult(
            transaction.Id,
            transaction.TransactionCode,
            transaction.Status,
            paymentUrl,
            transaction.ExpiredAt
        );
    }

    public async Task<PaymentResponse?> GetPaymentByIdAsync(Guid id, Guid? userId = null)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id);
        if (transaction == null) return null;
        if (userId.HasValue && transaction.UserId != userId.Value) return null;
        return MapToResponse(transaction);
    }

    public async Task<PaymentResponse?> GetPaymentByCodeAsync(string transactionCode, Guid? userId = null)
    {
        var transaction = await _transactionRepository.GetByTransactionCodeAsync(transactionCode);
        if (transaction == null) return null;
        if (userId.HasValue && transaction.UserId != userId.Value) return null;
        return MapToResponse(transaction);
    }

    public async Task<PagedResult<PaymentResponse>> GetUserPaymentsAsync(Guid userId, int page, int pageSize, string? status)
    {
        var transactions = await _transactionRepository.GetByUserIdAsync(userId, page, pageSize, status);
        var total = await _transactionRepository.GetCountByUserIdAsync(userId, status);
        return new PagedResult<PaymentResponse>(
            transactions.Select(MapToResponse).ToList(),
            total, page, pageSize
        );
    }

    public async Task<bool> CancelPaymentAsync(Guid id, Guid userId)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id);
        if (transaction == null || transaction.UserId != userId || !transaction.CanCancel())
            return false;

        transaction.Cancel();
        await _transactionRepository.SaveChangesAsync();

        // Publish PaymentFailed event
        await PublishPaymentFailedEventAsync(transaction, "USER_CANCELLED", "Payment cancelled by user");

        _logger.LogInformation("❌ Payment cancelled: {TransactionCode}", transaction.TransactionCode);
        return true;
    }

    // ============================================
    // PAYMENT CALLBACKS
    // ============================================

    public async Task<(bool Success, string Message)> ProcessVnPayCallbackAsync(Dictionary<string, string> vnpParams, string? ipAddress)
    {
        _logger.LogInformation("📥 Processing VNPay callback");

        var txnRef = vnpParams.GetValueOrDefault("vnp_TxnRef");
        var responseCode = vnpParams.GetValueOrDefault("vnp_ResponseCode");
        var transactionNo = vnpParams.GetValueOrDefault("vnp_TransactionNo");
        var amount = vnpParams.GetValueOrDefault("vnp_Amount");

        if (string.IsNullOrEmpty(txnRef))
            return (false, "Invalid request");

        var transaction = await _transactionRepository.GetByTransactionCodeAsync(txnRef);
        if (transaction == null)
            return (false, "Transaction not found");

        // Log callback
        await LogPaymentActionAsync(transaction.Id, null, PaymentLogActions.Callback, null,
            JsonSerializer.Serialize(vnpParams), null, ipAddress, null);

        if (responseCode == "00") // Success
        {
            transaction.Complete(transactionNo, responseCode, "Success", JsonSerializer.Serialize(vnpParams));
            await _transactionRepository.SaveChangesAsync();

            // Publish PaymentSuccess event
            await PublishPaymentSuccessEventAsync(transaction);

            _logger.LogInformation("✅ VNPay payment success: {TransactionCode}", transaction.TransactionCode);
            return (true, "Confirm Success");
        }
        else
        {
            transaction.Fail(responseCode, GetVnPayErrorMessage(responseCode), JsonSerializer.Serialize(vnpParams));
            await _transactionRepository.SaveChangesAsync();

            // Publish PaymentFailed event
            await PublishPaymentFailedEventAsync(transaction, responseCode, GetVnPayErrorMessage(responseCode));

            _logger.LogWarning("❌ VNPay payment failed: {TransactionCode}, code: {Code}", transaction.TransactionCode, responseCode);
            return (false, "Transaction failed");
        }
    }

    public async Task<(bool Success, string Message)> ProcessMomoCallbackAsync(string partnerCode, string orderId, int resultCode, string message, string transId)
    {
        _logger.LogInformation("📥 Processing Momo callback for order: {OrderId}", orderId);

        var transaction = await _transactionRepository.GetByTransactionCodeAsync(orderId);
        if (transaction == null)
            return (false, "Transaction not found");

        if (resultCode == 0) // Success
        {
            transaction.Complete(transId, resultCode.ToString(), message, null);
            await _transactionRepository.SaveChangesAsync();

            await PublishPaymentSuccessEventAsync(transaction);

            _logger.LogInformation("✅ Momo payment success: {TransactionCode}", transaction.TransactionCode);
            return (true, "success");
        }
        else
        {
            transaction.Fail(resultCode.ToString(), message, null);
            await _transactionRepository.SaveChangesAsync();

            await PublishPaymentFailedEventAsync(transaction, resultCode.ToString(), message);

            _logger.LogWarning("❌ Momo payment failed: {TransactionCode}", transaction.TransactionCode);
            return (false, message);
        }
    }

    // ============================================
    // PAYMENT METHODS
    // ============================================

    public async Task<List<PaymentMethodResponse>> GetUserPaymentMethodsAsync(Guid userId)
    {
        var methods = await _methodRepository.GetByUserIdAsync(userId);
        return methods.Select(m => new PaymentMethodResponse(
            m.Id, m.Type, m.Provider, m.CardLast4, m.CardBrand,
            m.CardExpMonth, m.CardExpYear, m.BankName, m.AccountNumberMasked,
            m.WalletPhone, m.IsDefault, m.IsVerified, m.Nickname
        )).ToList();
    }

    public async Task<PaymentMethodResponse> AddPaymentMethodAsync(Guid userId, AddPaymentMethodRequest request)
    {
        if (request.IsDefault)
        {
            await _methodRepository.ClearDefaultAsync(userId);
        }

        var method = new PaymentMethodEntity(userId, request.Type, request.Provider, request.Nickname, request.IsDefault);

        if (request.Type == "CREDIT_CARD" || request.Type == "DEBIT_CARD")
        {
            method.SetCardInfo(request.CardToken, request.CardLast4, request.CardBrand,
                request.CardExpMonth, request.CardExpYear, request.CardHolderName);
        }
        else if (request.Type == "BANK_ACCOUNT")
        {
            var maskedAccount = request.AccountNumber != null
                ? "****" + request.AccountNumber[^4..]
                : null;
            method.SetBankInfo(request.BankCode, request.BankName, maskedAccount, request.AccountHolderName);
        }
        else if (request.Type == "E_WALLET")
        {
            method.SetWalletInfo(null, request.WalletPhone);
        }

        await _methodRepository.AddAsync(method);
        await _methodRepository.SaveChangesAsync();

        return new PaymentMethodResponse(
            method.Id, method.Type, method.Provider, method.CardLast4, method.CardBrand,
            method.CardExpMonth, method.CardExpYear, method.BankName, method.AccountNumberMasked,
            method.WalletPhone, method.IsDefault, method.IsVerified, method.Nickname
        );
    }

    public async Task<bool> UpdatePaymentMethodAsync(Guid id, Guid userId, UpdatePaymentMethodRequest request)
    {
        var method = await _methodRepository.GetByIdAsync(id);
        if (method == null || method.UserId != userId) return false;

        if (request.IsDefault == true)
        {
            await _methodRepository.ClearDefaultAsync(userId);
        }

        method.Update(request.Nickname, request.IsDefault);
        await _methodRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePaymentMethodAsync(Guid id, Guid userId)
    {
        var method = await _methodRepository.GetByIdAsync(id);
        if (method == null || method.UserId != userId) return false;

        await _methodRepository.DeleteAsync(method);
        await _methodRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetDefaultPaymentMethodAsync(Guid id, Guid userId)
    {
        var method = await _methodRepository.GetByIdAsync(id);
        if (method == null || method.UserId != userId) return false;

        await _methodRepository.ClearDefaultAsync(userId);
        method.SetDefault(true);
        await _methodRepository.SaveChangesAsync();
        return true;
    }

    // ============================================
    // REFUNDS
    // ============================================

    public async Task<RefundResponse?> CreateRefundAsync(Guid paymentId, Guid userId, CreateRefundRequest request)
    {
        var transaction = await _transactionRepository.GetByIdAsync(paymentId);
        if (transaction == null || transaction.UserId != userId || !transaction.CanRefund())
            return null;

        var refund = new RefundTransaction(
            paymentId,
            transaction.OrderId,
            userId,
            request.Amount,
            request.Reason,
            request.RefundMethod,
            request.OrderRefundId
        );

        if (request.RefundMethod == RefundMethods.BankTransfer)
        {
            refund.SetBankInfo(request.BankCode, request.BankName, request.BankAccount, request.BankAccountName);
        }

        await _refundRepository.AddAsync(refund);
        await _refundRepository.SaveChangesAsync();

        _logger.LogInformation("📝 Refund request created: {RefundCode}", refund.RefundCode);

        return MapToRefundResponse(refund);
    }

    public async Task<RefundResponse?> GetRefundByIdAsync(Guid id, Guid? userId = null)
    {
        var refund = await _refundRepository.GetByIdAsync(id);
        if (refund == null) return null;
        if (userId.HasValue && refund.UserId != userId.Value) return null;
        return MapToRefundResponse(refund);
    }

    public async Task<PagedResult<RefundResponse>> GetUserRefundsAsync(Guid userId, int page, int pageSize, string? status)
    {
        var refunds = await _refundRepository.GetByUserIdAsync(userId, page, pageSize, status);
        var total = await _refundRepository.GetCountByUserIdAsync(userId, status);
        return new PagedResult<RefundResponse>(
            refunds.Select(MapToRefundResponse).ToList(),
            total, page, pageSize
        );
    }

    // ============================================
    // ADMIN
    // ============================================

    public async Task<PagedResult<PaymentResponse>> GetAllTransactionsAsync(int page, int pageSize, string? status, string? gateway, DateTime? from, DateTime? to)
    {
        var transactions = await _transactionRepository.GetAllAsync(page, pageSize, status, gateway, from, to);
        var total = await _transactionRepository.GetTotalCountAsync(status, gateway, from, to);
        return new PagedResult<PaymentResponse>(
            transactions.Select(MapToResponse).ToList(),
            total, page, pageSize
        );
    }

    public async Task<PagedResult<RefundResponse>> GetAllRefundsAsync(int page, int pageSize, string? status)
    {
        var refunds = await _refundRepository.GetAllAsync(page, pageSize, status);
        var total = await _refundRepository.GetTotalCountAsync(status);
        return new PagedResult<RefundResponse>(
            refunds.Select(MapToRefundResponse).ToList(),
            total, page, pageSize
        );
    }

    public async Task<bool> ProcessRefundAsync(Guid refundId, ProcessRefundRequest request, Guid processedBy, string? processedByName)
    {
        var refund = await _refundRepository.GetByIdAsync(refundId);
        if (refund == null || refund.Status != RefundStatus.Pending) return false;

        if (request.Action == "APPROVE")
            refund.Approve(processedBy, processedByName, request.AdminNote);
        else
            refund.Reject(processedBy, processedByName, request.AdminNote);

        await _refundRepository.SaveChangesAsync();

        _logger.LogInformation("📝 Refund {RefundCode} {Action}", refund.RefundCode, request.Action);
        return true;
    }

    public async Task<bool> ExecuteRefundAsync(Guid refundId)
    {
        var refund = await _refundRepository.GetByIdAsync(refundId);
        if (refund == null || refund.Status != RefundStatus.Approved) return false;

        refund.Process();
        await _refundRepository.SaveChangesAsync();

        // TODO: Call gateway to process refund
        // For now, just mark as completed
        refund.Complete(null, "00", "Refund processed");
        await _refundRepository.SaveChangesAsync();

        // Publish PaymentRefunded event
        await PublishPaymentRefundedEventAsync(refund);

        _logger.LogInformation("✅ Refund executed: {RefundCode}", refund.RefundCode);
        return true;
    }

    public async Task<List<GatewayConfigResponse>> GetGatewayConfigsAsync()
    {
        var configs = await _gatewayConfigRepository.GetAllAsync();
        return configs.Select(c => new GatewayConfigResponse(
            c.Id, c.GatewayCode, c.GatewayName, c.IsActive, c.IsSandbox,
            c.SupportedMethods, c.FeePercent, c.FeeFixed, c.MinAmount, c.MaxAmount
        )).ToList();
    }

    public async Task<bool> UpdateGatewayConfigAsync(string gatewayCode, UpdateGatewayConfigRequest request)
    {
        var config = await _gatewayConfigRepository.GetByCodeAsync(gatewayCode);
        if (config == null) return false;

        config.Update(request.IsActive, request.IsSandbox, request.ConfigData, request.FeePercent, request.FeeFixed);
        await _gatewayConfigRepository.SaveChangesAsync();
        return true;
    }

    // ============================================
    // INTERNAL
    // ============================================

    public async Task<InternalVerifyResponse> VerifyPaymentAsync(Guid orderId)
    {
        var transaction = await _transactionRepository.GetByOrderIdAsync(orderId);
        if (transaction == null)
        {
            return new InternalVerifyResponse(orderId, false, null, null, null);
        }

        return new InternalVerifyResponse(
            orderId,
            transaction.Status == PaymentStatus.Completed,
            transaction.Id,
            transaction.PaidAt,
            transaction.Amount
        );
    }

    // ============================================
    // EVENT PUBLISHING
    // ============================================

    private async Task PublishPaymentSuccessEventAsync(PaymentTransaction transaction)
    {
        var paymentEvent = new PaymentSuccessEvent
        {
            TransactionId = transaction.Id,
            OrderId = transaction.OrderId,
            OrderNumber = transaction.OrderNumber,
            UserId = transaction.UserId,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            PaymentGateway = transaction.PaymentGateway ?? "",
            PaymentMethod = transaction.PaymentMethod,
            GatewayTransactionId = transaction.GatewayTransactionId,
            GatewayResponseCode = transaction.GatewayResponseCode,
            PaidAt = transaction.PaidAt ?? DateTime.UtcNow
        };

        await _eventPublisher.PublishAsync(
            EventConstants.PaymentExchange,
            EventConstants.PaymentSuccess,
            paymentEvent
        );

        _logger.LogInformation("📤 Published PaymentSuccessEvent for order: {OrderNumber}", transaction.OrderNumber);
    }

    private async Task PublishPaymentFailedEventAsync(PaymentTransaction transaction, string errorCode, string errorMessage)
    {
        var paymentEvent = new PaymentFailedEvent
        {
            TransactionId = transaction.Id,
            OrderId = transaction.OrderId,
            OrderNumber = transaction.OrderNumber,
            UserId = transaction.UserId,
            Amount = transaction.Amount,
            PaymentGateway = transaction.PaymentGateway ?? "",
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            FailureReason = errorCode == "USER_CANCELLED" ? "USER_CANCELLED" : "GATEWAY_ERROR",
            ReservationIds = new List<Guid>(), // Order service will handle this
            FailedAt = DateTime.UtcNow
        };

        await _eventPublisher.PublishAsync(
            EventConstants.PaymentExchange,
            EventConstants.PaymentFailed,
            paymentEvent
        );

        _logger.LogInformation("📤 Published PaymentFailedEvent for order: {OrderNumber}", transaction.OrderNumber);
    }

    private async Task PublishPaymentRefundedEventAsync(RefundTransaction refund)
    {
        var transaction = await _transactionRepository.GetByIdAsync(refund.PaymentTransactionId);
        if (transaction == null) return;

        var refundEvent = new PaymentRefundedEvent
        {
            RefundId = refund.Id,
            TransactionId = refund.PaymentTransactionId,
            OrderId = refund.OrderId,
            OrderNumber = transaction.OrderNumber,
            UserId = refund.UserId,
            RefundAmount = refund.Amount,
            RefundReason = refund.Reason,
            RefundType = refund.Amount >= transaction.Amount ? "FULL" : "PARTIAL",
            GatewayRefundId = refund.GatewayRefundId,
            RefundStatus = refund.Status,
            RefundedAt = refund.CompletedAt ?? DateTime.UtcNow
        };

        await _eventPublisher.PublishAsync(
            EventConstants.PaymentExchange,
            EventConstants.PaymentRefunded,
            refundEvent
        );

        _logger.LogInformation("📤 Published PaymentRefundedEvent for refund: {RefundCode}", refund.RefundCode);
    }

    // ============================================
    // HELPERS
    // ============================================

    private async Task<string?> GeneratePaymentUrlAsync(PaymentTransaction transaction, string gateway)
    {
        // TODO: Integrate with actual payment gateways
        // For now, return a mock URL
        return gateway switch
        {
            PaymentGateways.VnPay => $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_TxnRef={transaction.TransactionCode}&vnp_Amount={transaction.Amount * 100}",
            PaymentGateways.Momo => $"https://test-payment.momo.vn/pay/{transaction.TransactionCode}",
            PaymentGateways.ZaloPay => $"https://sbgateway.zalopay.vn/pay/{transaction.TransactionCode}",
            _ => null
        };
    }

    private async Task LogPaymentActionAsync(Guid? transactionId, Guid? refundId, string action, string? status, string? requestData, string? responseData, string? ipAddress, string? userAgent)
    {
        var log = new PaymentLog(transactionId, refundId, action, status, requestData, responseData, ipAddress, userAgent);
        await _logRepository.AddAsync(log);
        await _logRepository.SaveChangesAsync();
    }

    private static string GetVnPayErrorMessage(string? code) => code switch
    {
        "00" => "Giao dịch thành công",
        "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
        "09" => "Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking.",
        "10" => "Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần",
        "11" => "Đã hết hạn chờ thanh toán.",
        "12" => "Thẻ/Tài khoản của khách hàng bị khóa.",
        "13" => "Quý khách nhập sai mật khẩu xác thực giao dịch (OTP).",
        "24" => "Khách hàng hủy giao dịch",
        "51" => "Tài khoản của quý khách không đủ số dư để thực hiện giao dịch.",
        "65" => "Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày.",
        "75" => "Ngân hàng thanh toán đang bảo trì.",
        "79" => "Khách hàng nhập sai mật khẩu thanh toán quá số lần quy định.",
        "99" => "Lỗi không xác định",
        _ => "Giao dịch thất bại"
    };

    private static PaymentResponse MapToResponse(PaymentTransaction t) => new(
        t.Id, t.TransactionCode, t.OrderId, t.OrderNumber, t.Amount, t.Currency,
        t.PaymentMethod, t.PaymentGateway, t.Status, t.GatewayTransactionId,
        t.PaymentUrl, t.PaidAt, t.ExpiredAt, t.CreatedAt
    );

    private static RefundResponse MapToRefundResponse(RefundTransaction r) => new(
        r.Id, r.RefundCode, r.PaymentTransactionId, r.OrderId, r.Amount,
        r.Status, r.RefundMethod, r.Reason, r.AdminNote, r.CompletedAt, r.CreatedAt
    );
}

