namespace Shared.Messaging.Events;

public static class EventConstants
{
    // Exchange names
    public const string UserExchange = "user.events";
    public const string ProductExchange = "product.events";
    public const string OrderExchange = "order.events";

    // Routing keys
    public const string UserProfileUpdated = "user.profile.updated";
    public const string UserCreated = "user.created";
    public const string UserDeleted = "user.deleted";

    // Queue names
    public const string ProductUserSyncQueue = "product.user.sync";
}

