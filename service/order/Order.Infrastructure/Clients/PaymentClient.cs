using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Order.Application.Clients;

namespace Order.Infrastructure.Clients;

public class PaymentClient : IPaymentClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentClient> _logger;

    public PaymentClient(HttpClient httpClient, ILogger<PaymentClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CreatePaymentResult> CreatePaymentAsync(CreatePaymentRequest request)
    {
        try
        {
            _logger.LogInformation("💳 Creating payment for order: {OrderId}, amount: {Amount}", 
                request.OrderId, request.Amount);
            
            var response = await _httpClient.PostAsJsonAsync("/api/payments/create", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CreatePaymentResult>();
                _logger.LogInformation("✅ Payment created for order: {OrderId}, transaction: {TransactionId}", 
                    request.OrderId, result?.TransactionId);
                return result ?? new CreatePaymentResult(false, null, null, "Invalid response from Payment service");
            }
            
            _logger.LogWarning("⚠️ Payment creation failed: {StatusCode}", response.StatusCode);
            return new CreatePaymentResult(false, null, null, "Failed to create payment");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error calling Payment service");
            return new CreatePaymentResult(false, null, null, "Payment service error");
        }
    }

    public async Task<PaymentStatusResult> GetStatusAsync(Guid transactionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/payments/{transactionId}/status");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PaymentStatusResult>();
                return result ?? new PaymentStatusResult(transactionId, Guid.Empty, "UNKNOWN", 0, null, null);
            }
            
            return new PaymentStatusResult(transactionId, Guid.Empty, "UNKNOWN", 0, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting payment status");
            return new PaymentStatusResult(transactionId, Guid.Empty, "ERROR", 0, null, null);
        }
    }
}

