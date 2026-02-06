namespace Shared.Messaging.Events;

public static class EventConstants
{
    // ============================================
    // EXCHANGES
    // ============================================
    public const string UserExchange = "user.events";
    public const string OrderExchange = "order.events";
    public const string PaymentExchange = "payment.events";
    public const string ProductExchange = "product.events";
    public const string InventoryExchange = "inventory.events";

    // ============================================
    // ROUTING KEYS - User Events
    // ============================================
    public const string UserProfileUpdated = "user.profile.updated";
    public const string UserCreated = "user.created";
    public const string UserDeleted = "user.deleted";

    // ============================================
    // ROUTING KEYS - Order Events
    // ============================================
    public const string OrderCreated = "order.created";
    public const string OrderConfirmed = "order.confirmed";
    public const string OrderCancelled = "order.cancelled";
    public const string OrderShipped = "order.shipped";
    public const string OrderDelivered = "order.delivered";
    public const string OrderRefunded = "order.refunded";

    // ============================================
    // ROUTING KEYS - Payment Events
    // ============================================
    public const string PaymentSuccess = "payment.success";
    public const string PaymentFailed = "payment.failed";
    public const string PaymentRefunded = "payment.refunded";

    // ============================================
    // ROUTING KEYS - Inventory Events
    // ============================================
    public const string StockReserved = "stock.reserved";
    public const string StockCommitted = "stock.committed";
    public const string StockReleased = "stock.released";
    public const string StockLow = "stock.low";

    // ============================================
    // QUEUE NAMES - User Profile Sync
    // ============================================
    public const string ProductUserSyncQueue = "product.user.sync";
    public const string OrderUserSyncQueue = "order.user.sync";

    // ============================================
    // QUEUE NAMES - Order Events Consumers
    // ============================================
    public const string InventoryOrderConfirmedQueue = "inventory.order.confirmed";
    public const string InventoryOrderCancelledQueue = "inventory.order.cancelled";
    public const string DiscountOrderConfirmedQueue = "discount.order.confirmed";
    public const string DiscountOrderCancelledQueue = "discount.order.cancelled";
    public const string NotificationOrderConfirmedQueue = "notification.order.confirmed";
    public const string NotificationOrderCancelledQueue = "notification.order.cancelled";
    public const string ProductOrderConfirmedQueue = "product.order.confirmed";

    // ============================================
    // QUEUE NAMES - Payment Events Consumers
    // ============================================
    public const string OrderPaymentSuccessQueue = "order.payment.success";
    public const string OrderPaymentFailedQueue = "order.payment.failed";
    public const string InventoryPaymentFailedQueue = "inventory.payment.failed";
    public const string NotificationPaymentSuccessQueue = "notification.payment.success";
    public const string NotificationPaymentFailedQueue = "notification.payment.failed";
}
