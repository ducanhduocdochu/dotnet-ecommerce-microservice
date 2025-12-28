using Inventory.Application.DTOs;
using Inventory.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Messaging.Events;
using Shared.Messaging.RabbitMQ;

namespace Inventory.Api.Consumers;

/// <summary>
/// Consumes OrderConfirmedEvent to commit reserved stock
/// </summary>
public class OrderConfirmedConsumer : EventConsumer<OrderConfirmedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderConfirmedConsumer> _logger;

    public OrderConfirmedConsumer(
        IRabbitMQConnection connection,
        IServiceProvider serviceProvider,
        ILogger<OrderConfirmedConsumer> logger)
        : base(
            connection,
            logger,
            EventConstants.OrderExchange,
            EventConstants.InventoryOrderConfirmedQueue,
            EventConstants.OrderConfirmed)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task HandleAsync(OrderConfirmedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📦 [Inventory] Received OrderConfirmedEvent for order: {OrderNumber}", message.OrderNumber);

        using var scope = _serviceProvider.CreateScope();
        var inventoryService = scope.ServiceProvider.GetRequiredService<InventoryService>();

        try
        {
            // Commit stock - this marks reservations as committed
            var success = await inventoryService.CommitStockAsync(message.OrderId);

            if (success)
            {
                _logger.LogInformation("✅ [Inventory] Stock committed for order: {OrderNumber}", message.OrderNumber);
            }
            else
            {
                _logger.LogWarning("⚠️ [Inventory] No reservations found to commit for order: {OrderNumber}", message.OrderNumber);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Inventory] Failed to commit stock for order: {OrderNumber}", message.OrderNumber);
            throw; // Re-throw to trigger retry/dead-letter
        }
    }
}

