using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Product.Application.Interfaces;
using Shared.Messaging.Events;
using Shared.Messaging.RabbitMQ;

namespace Product.Api.Consumers;

/// <summary>
/// Consumes UserProfileUpdatedEvent to sync denormalized user data in products and reviews
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
            EventConstants.ProductUserSyncQueue,
            EventConstants.UserProfileUpdated)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task HandleAsync(UserProfileUpdatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔄 Processing UserProfileUpdatedEvent for user {UserId}", message.UserId);

        using var scope = _serviceProvider.CreateScope();
        var productRepo = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        var reviewRepo = scope.ServiceProvider.GetRequiredService<IProductReviewRepository>();

        // Update seller info in all products by this user
        var products = await productRepo.GetBySellerIdAsync(message.UserId, 1, int.MaxValue, null);
        foreach (var product in products)
        {
            product.UpdateSellerInfo(message.FullName, message.AvatarUrl);
        }
        await productRepo.SaveChangesAsync();

        // Update reviewer info in all reviews by this user
        var reviews = await reviewRepo.GetByUserIdAsync(message.UserId);
        foreach (var review in reviews)
        {
            review.UpdateReviewerInfo(message.FullName, message.AvatarUrl);
        }
        await reviewRepo.SaveChangesAsync();

        _logger.LogInformation("✅ Synced {ProductCount} products and {ReviewCount} reviews for user {UserId}",
            products.Count, reviews.Count, message.UserId);
    }
}

