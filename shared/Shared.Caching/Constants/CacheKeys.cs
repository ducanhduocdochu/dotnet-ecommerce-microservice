namespace Shared.Caching.Constants;

/// <summary>
/// Centralized cache key constants
/// </summary>
public static class CacheKeys
{
    // Product Service
    public static class Product
    {
        public const string Prefix = "product:";
        public static string ById(Guid id) => $"{Prefix}id:{id}";
        public static string BySlug(string slug) => $"{Prefix}slug:{slug}";
        public static string List(int page, int pageSize) => $"{Prefix}list:p{page}:s{pageSize}";
        public static string ByCategoryId(Guid categoryId) => $"{Prefix}category:{categoryId}";
        public static string Featured() => $"{Prefix}featured";
        public static string AllPattern() => $"{Prefix}*";
    }

    public static class Category
    {
        public const string Prefix = "category:";
        public static string ById(Guid id) => $"{Prefix}id:{id}";
        public static string All() => $"{Prefix}all";
        public static string Tree() => $"{Prefix}tree";
        public static string AllPattern() => $"{Prefix}*";
    }

    public static class Brand
    {
        public const string Prefix = "brand:";
        public static string ById(Guid id) => $"{Prefix}id:{id}";
        public static string All() => $"{Prefix}all";
        public static string AllPattern() => $"{Prefix}*";
    }

    // Inventory Service
    public static class Inventory
    {
        public const string Prefix = "inventory:";
        public static string ByProductId(Guid productId, Guid? variantId = null) 
            => variantId.HasValue 
                ? $"{Prefix}product:{productId}:variant:{variantId}" 
                : $"{Prefix}product:{productId}";
        public static string StockAvailability(Guid productId) => $"{Prefix}stock:{productId}";
        public static string AllPattern() => $"{Prefix}*";
        public static string ByProductPattern(Guid productId) => $"{Prefix}product:{productId}*";
    }

    // Discount Service
    public static class Discount
    {
        public const string Prefix = "discount:";
        public static string ById(Guid id) => $"{Prefix}id:{id}";
        public static string ByCode(string code) => $"{Prefix}code:{code}";
        public static string Active() => $"{Prefix}active";
        public static string ForProduct(Guid productId) => $"{Prefix}product:{productId}";
        public static string Validation(string code, Guid userId) => $"{Prefix}validate:{code}:user:{userId}";
        public static string AllPattern() => $"{Prefix}*";
    }

    public static class FlashSale
    {
        public const string Prefix = "flashsale:";
        public static string ById(Guid id) => $"{Prefix}id:{id}";
        public static string Active() => $"{Prefix}active";
        public static string AllPattern() => $"{Prefix}*";
    }

    // User Service
    public static class User
    {
        public const string Prefix = "user:";
        public static string ProfileById(Guid userId) => $"{Prefix}profile:{userId}";
        public static string AddressesById(Guid userId) => $"{Prefix}addresses:{userId}";
        public static string PreferencesById(Guid userId) => $"{Prefix}preferences:{userId}";
        public static string WishlistById(Guid userId) => $"{Prefix}wishlist:{userId}";
        public static string AllPattern() => $"{Prefix}*";
        public static string ByUserPattern(Guid userId) => $"{Prefix}*:{userId}";
    }

    // Auth Service
    public static class Auth
    {
        public const string Prefix = "auth:";
        public static string UserById(Guid userId) => $"{Prefix}user:{userId}";
        public static string RolesById(Guid userId) => $"{Prefix}roles:{userId}";
        public static string PermissionsById(Guid userId) => $"{Prefix}permissions:{userId}";
        public static string AllRoles() => $"{Prefix}roles:all";
        public static string AllPattern() => $"{Prefix}*";
    }

    // Order Service (Cache ít hơn vì thay đổi nhiều)
    public static class Order
    {
        public const string Prefix = "order:";
        public static string ById(Guid orderId) => $"{Prefix}id:{orderId}";
        public static string ByNumber(string orderNumber) => $"{Prefix}number:{orderNumber}";
        public static string CartByUserId(Guid userId) => $"{Prefix}cart:{userId}";
        public static string AllPattern() => $"{Prefix}*";
    }

    // Payment Service
    public static class Payment
    {
        public const string Prefix = "payment:";
        public static string ById(Guid paymentId) => $"{Prefix}id:{paymentId}";
        public static string ByTransactionCode(string code) => $"{Prefix}txn:{code}";
        public static string GatewayConfigs() => $"{Prefix}gateways";
        public static string AllPattern() => $"{Prefix}*";
    }

    // Rate Limiting
    public static class RateLimit
    {
        public const string Prefix = "ratelimit:";
        public static string ByIp(string ip, string endpoint) => $"{Prefix}ip:{ip}:{endpoint}";
        public static string ByUser(Guid userId, string endpoint) => $"{Prefix}user:{userId}:{endpoint}";
    }

    // Session
    public static class Session
    {
        public const string Prefix = "session:";
        public static string ByToken(string token) => $"{Prefix}token:{token}";
        public static string ByUserId(Guid userId) => $"{Prefix}user:{userId}";
    }
}


