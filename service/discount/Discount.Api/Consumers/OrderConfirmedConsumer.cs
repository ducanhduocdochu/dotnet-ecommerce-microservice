using Discount.Application.DTOs;
using Discount.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Messaging.Events;
using Shared.Messaging.RabbitMQ;

namespace Discount.Api.Consumers;

/// <summary>
/// Consumes OrderConfirmedEvent to record discount usage
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
            EventConstants.DiscountOrderConfirmedQueue,
            EventConstants.OrderConfirmed)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task HandleAsync(OrderConfirmedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🎫 [Discount] Received OrderConfirmedEvent for order: {OrderNumber}", message.OrderNumber);

        // Only process if discount was used
        if (!message.DiscountId.HasValue || message.DiscountAmount <= 0)
        {
            _logger.LogInformation("ℹ️ [Discount] No discount used for order: {OrderNumber}, skipping", message.OrderNumber);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var discountService = scope.ServiceProvider.GetRequiredService<DiscountService>();

        try
        {
            // Record discount usage
            await discountService.RecordUsageAsync(new RecordUsageRequest(
                message.DiscountId.Value,
                message.UserId,
                message.OrderId,
                message.OrderNumber,
                message.TotalAmount,
                message.DiscountAmount
            ));

            _logger.LogInformation("✅ [Discount] Recorded usage for discount {DiscountId}, order: {OrderNumber}, amount: {Amount}",
                message.DiscountId, message.OrderNumber, message.DiscountAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Discount] Failed to record usage for order: {OrderNumber}", message.OrderNumber);
            throw; // Re-throw to trigger retry/dead-letter
        }
    }
}

