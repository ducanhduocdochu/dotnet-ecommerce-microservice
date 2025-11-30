using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Order.Application.Services;
using Shared.Messaging.Events;
using Shared.Messaging.RabbitMQ;

namespace Order.Api.Consumers;

/// <summary>
/// Consumes UserProfileUpdatedEvent to sync denormalized user data in orders
/// </summary>
public class UserProfileUpdatedConsumer : EventConsumer<UserProfileUpdatedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserProfileUpdatedConsumer> _logger;

    public UserProfileUpdatedConsumer(
        IRabbitMQConnection connection,
        IServiceProvider serviceProvider,
        ILogger<UserProfileUpdatedConsumer> logger)
        : base(
            connection,
            logger,
            EventConstants.UserExchange,
            "order.user.sync",  // Unique queue for Order service
            EventConstants.UserProfileUpdated)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task HandleAsync(UserProfileUpdatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔄 [Order] Processing UserProfileUpdatedEvent for user {UserId}", message.UserId);

        using var scope = _serviceProvider.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

        // Update user info in all orders by this user
        await orderService.SyncUserInfoAsync(
            message.UserId, 
            message.FullName, 
            message.Email,
            message.Phone
        );

        _logger.LogInformation("✅ [Order] Synced user info for user {UserId}", message.UserId);
    }
}

