using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Order.Application.Services;
using Shared.Messaging.Events;
using Shared.Messaging.RabbitMQ;

namespace Order.Api.Consumers;

public class PaymentFailedConsumer : EventConsumer<PaymentFailedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentFailedConsumer> _logger;

    public PaymentFailedConsumer(
        IRabbitMQConnection connection,
        IServiceProvider serviceProvider,
        ILogger<PaymentFailedConsumer> logger)
        : base(
            connection,
            logger,
            EventConstants.PaymentExchange,
            EventConstants.OrderPaymentFailedQueue,
            EventConstants.PaymentFailed)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task HandleAsync(PaymentFailedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("❌ Received PaymentFailedEvent for order: {OrderId}, reason: {Reason}", 
            message.OrderId, message.FailureReason);

        using var scope = _serviceProvider.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

        await orderService.HandlePaymentFailedAsync(message.OrderId, message.ErrorMessage);

        _logger.LogInformation("⚠️ Order {OrderId} marked as payment failed", message.OrderId);
    }
}

