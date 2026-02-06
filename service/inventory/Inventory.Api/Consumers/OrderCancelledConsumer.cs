using Inventory.Application.DTOs;
using Inventory.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Messaging.Events;
using Shared.Messaging.RabbitMQ;

namespace Inventory.Api.Consumers;

/// <summary>
/// Consumes OrderCancelledEvent to release reserved stock or return committed stock
/// </summary>
public class OrderCancelledConsumer : EventConsumer<OrderCancelledEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderCancelledConsumer> _logger;

    public OrderCancelledConsumer(
        IRabbitMQConnection connection,
        IServiceProvider serviceProvider,
        ILogger<OrderCancelledConsumer> logger)
        : base(
            connection,
            logger,
            EventConstants.OrderExchange,
            EventConstants.InventoryOrderCancelledQueue,
            EventConstants.OrderCancelled)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task HandleAsync(OrderCancelledEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📦 [Inventory] Received OrderCancelledEvent for order: {OrderNumber}", message.OrderNumber);

        using var scope = _serviceProvider.CreateScope();
        var inventoryService = scope.ServiceProvider.GetRequiredService<InventoryService>();

        try
        {
            // Check if stock was already committed
            var hasCommittedStock = message.Items.Any(i => i.StockCommitted);

            if (hasCommittedStock)
            {
                // Return committed stock
                var returnRequest = new ReturnStockRequest(
                    message.OrderId,
                    message.OrderNumber,
                    message.Items
                        .Where(i => i.StockCommitted)
                        .Select(i => new ReturnStockItem(i.ProductId, i.VariantId, i.Quantity, message.Reason ?? "Order cancelled"))
                        .ToList()
                );

                var returned = await inventoryService.ReturnStockAsync(returnRequest);
                _logger.LogInformation("✅ [Inventory] Returned committed stock for order: {OrderNumber}, success: {Success}", 
                    message.OrderNumber, returned);
            }

            // Release any remaining reservations
            var releaseRequest = new ReleaseStockRequest(
                message.OrderId,
                message.Reason ?? "Order cancelled"
            );

            var released = await inventoryService.ReleaseStockAsync(releaseRequest);
            _logger.LogInformation("✅ [Inventory] Released reservations for order: {OrderNumber}, success: {Success}", 
                message.OrderNumber, released);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Inventory] Failed to release/return stock for order: {OrderNumber}", message.OrderNumber);
            throw; // Re-throw to trigger retry/dead-letter
        }
    }
}

