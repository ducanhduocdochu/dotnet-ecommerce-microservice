using Inventory.Application.DTOs;
using Inventory.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Messaging.Events;
using Shared.Messaging.RabbitMQ;

namespace Inventory.Api.Consumers;

/// <summary>
/// Consumes PaymentFailedEvent to release reserved stock
/// </summary>
public class PaymentFailedConsumer : EventConsumer<PaymentFailedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentFailedConsumer> _logger;

    public PaymentFailedConsumer(
        IRabbitMQConnection connection,
        IServiceProvider serviceProvider,
        ILogger<PaymentFailedConsumer> logger)
        : base(connection, EventConstants.PaymentExchange, "inventory.payment.failed", EventConstants.PaymentFailed, logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ProcessMessageAsync(PaymentFailedEvent message)
    {
        _logger.LogInformation("📦 [Inventory] Received PaymentFailedEvent for order: {OrderNumber}", message.OrderNumber);

        using var scope = _serviceProvider.CreateScope();
        var inventoryService = scope.ServiceProvider.GetRequiredService<InventoryService>();

        try
        {
            // Release reservations due to payment failure
            var releaseRequest = new ReleaseStockRequest(
                message.OrderId,
                $"Payment failed: {message.ErrorMessage}"
            );

            var released = await inventoryService.ReleaseStockAsync(releaseRequest);
            
            if (released)
            {
                _logger.LogInformation("✅ [Inventory] Released stock for order: {OrderNumber} due to payment failure", message.OrderNumber);
            }
            else
            {
                _logger.LogWarning("⚠️ [Inventory] No reservations found to release for order: {OrderNumber}", message.OrderNumber);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Inventory] Failed to release stock for order: {OrderNumber}", message.OrderNumber);
            throw;
        }
    }
}

