using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Order.Application.Clients;

namespace Order.Infrastructure.Clients;

public class InventoryClient : IInventoryClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryClient> _logger;

    public InventoryClient(HttpClient httpClient, ILogger<InventoryClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<StockCheckResult> CheckStockAsync(CheckStockRequest request)
    {
        try
        {
            _logger.LogInformation("🔍 Checking stock for {Count} items", request.Items.Count);
            
            var response = await _httpClient.PostAsJsonAsync("/api/inventory/check", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<StockCheckResult>();
                return result ?? new StockCheckResult(false, new List<StockCheckItemResult>(), "Invalid response from Inventory service");
            }
            
            _logger.LogWarning("⚠️ Stock check failed: {StatusCode}", response.StatusCode);
            return new StockCheckResult(false, new List<StockCheckItemResult>(), "Inventory service unavailable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error calling Inventory service");
            return new StockCheckResult(false, new List<StockCheckItemResult>(), "Inventory service error");
        }
    }

    public async Task<StockReserveResult> ReserveAsync(ReserveStockRequest request)
    {
        try
        {
            _logger.LogInformation("🔒 Reserving stock for order: {OrderId}", request.OrderId);
            
            var response = await _httpClient.PostAsJsonAsync("/api/inventory/reserve", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<StockReserveResult>();
                _logger.LogInformation("✅ Stock reserved successfully for order: {OrderId}", request.OrderId);
                return result ?? new StockReserveResult(false, new List<ReservationInfo>(), "Invalid response from Inventory service");
            }
            
            _logger.LogWarning("⚠️ Stock reservation failed: {StatusCode}", response.StatusCode);
            return new StockReserveResult(false, new List<ReservationInfo>(), "Failed to reserve stock");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error calling Inventory service");
            return new StockReserveResult(false, new List<ReservationInfo>(), "Inventory service error");
        }
    }

    public async Task<bool> ReleaseReservationAsync(ReleaseReservationRequest request)
    {
        try
        {
            _logger.LogInformation("🔓 Releasing reservation for order: {OrderId}", request.OrderId);
            
            var response = await _httpClient.PostAsJsonAsync("/api/inventory/release", request);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Reservation released for order: {OrderId}", request.OrderId);
                return true;
            }
            
            _logger.LogWarning("⚠️ Release reservation failed: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error calling Inventory service");
            return false;
        }
    }
}

