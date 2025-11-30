using Discount.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Messaging.Events;
using Shared.Messaging.RabbitMQ;

namespace Discount.Api.Consumers;

/// <summary>
/// Consumes OrderCancelledEvent to rollback discount usage
/// </summary>
public class OrderCancelledConsumer : EventConsumer<OrderCancelledEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderCancelledConsumer> _logger;

    public OrderCancelledConsumer(
        IRabbitMQConnection connection,
        IServiceProvider serviceProvider,
        ILogger<OrderCancelledConsumer> logger)
        : base(connection, EventConstants.OrderExchange, EventConstants.DiscountOrderCancelledQueue, EventConstants.OrderCancelled, logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ProcessMessageAsync(OrderCancelledEvent message)
    {
        _logger.LogInformation("🎫 [Discount] Received OrderCancelledEvent for order: {OrderNumber}", message.OrderNumber);

        // Only process if discount was used
        if (!message.DiscountId.HasValue)
        {
            _logger.LogInformation("ℹ️ [Discount] No discount used for order: {OrderNumber}, skipping", message.OrderNumber);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var discountService = scope.ServiceProvider.GetRequiredService<DiscountService>();

        try
        {
            // Rollback discount usage
            await discountService.RollbackUsageAsync(message.OrderId);

            _logger.LogInformation("✅ [Discount] Rolled back usage for order: {OrderNumber}, discount: {DiscountId}",
                message.OrderNumber, message.DiscountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Discount] Failed to rollback usage for order: {OrderNumber}", message.OrderNumber);
            throw; // Re-throw to trigger retry/dead-letter
        }
    }
}

