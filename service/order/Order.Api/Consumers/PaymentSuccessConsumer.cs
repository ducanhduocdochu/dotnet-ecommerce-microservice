using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Order.Application.Services;
using Shared.Messaging.Events;
using Shared.Messaging.RabbitMQ;

namespace Order.Api.Consumers;

public class PaymentSuccessConsumer : EventConsumer<PaymentSuccessEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentSuccessConsumer> _logger;

    public PaymentSuccessConsumer(
        IRabbitMQConnection connection,
        IServiceProvider serviceProvider,
        ILogger<PaymentSuccessConsumer> logger)
        : base(connection, EventConstants.PaymentExchange, EventConstants.OrderPaymentSuccessQueue, EventConstants.PaymentSuccess, logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ProcessMessageAsync(PaymentSuccessEvent message)
    {
        _logger.LogInformation("💳 Received PaymentSuccessEvent for order: {OrderId}", message.OrderId);

        using var scope = _serviceProvider.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

        await orderService.HandlePaymentSuccessAsync(message.OrderId, message.TransactionId);

        _logger.LogInformation("✅ Order {OrderId} confirmed after payment success", message.OrderId);
    }
}

