using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Order.Application.Clients;

namespace Order.Infrastructure.Clients;

public class DiscountClient : IDiscountClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DiscountClient> _logger;

    public DiscountClient(HttpClient httpClient, ILogger<DiscountClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<DiscountValidationResult> ValidateAsync(ValidateDiscountRequest request)
    {
        try
        {
            _logger.LogInformation("🔍 Validating discount code: {Code}", request.Code);
            
            var response = await _httpClient.PostAsJsonAsync("/api/discounts/validate", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<DiscountValidationResult>();
                return result ?? new DiscountValidationResult(false, null, 0, "Invalid response from Discount service");
            }
            
            _logger.LogWarning("⚠️ Discount validation failed: {StatusCode}", response.StatusCode);
            return new DiscountValidationResult(false, null, 0, "Discount service unavailable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error calling Discount service");
            return new DiscountValidationResult(false, null, 0, "Discount service error");
        }
    }

    public async Task<DiscountApplyResult> ApplyAsync(ApplyDiscountRequest request)
    {
        try
        {
            _logger.LogInformation("📝 Applying discount code: {Code} for order: {OrderId}", request.Code, request.OrderId);
            
            var response = await _httpClient.PostAsJsonAsync("/api/discounts/apply", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<DiscountApplyResult>();
                return result ?? new DiscountApplyResult(false, null, 0, "Invalid response from Discount service");
            }
            
            _logger.LogWarning("⚠️ Discount apply failed: {StatusCode}", response.StatusCode);
            return new DiscountApplyResult(false, null, 0, "Failed to apply discount");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error calling Discount service");
            return new DiscountApplyResult(false, null, 0, "Discount service error");
        }
    }
}

