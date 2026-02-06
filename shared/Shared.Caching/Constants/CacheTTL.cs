namespace Shared.Caching.Constants;

/// <summary>
/// Cache Time-To-Live constants
/// </summary>
public static class CacheTTL
{
    // Product Service
    public static readonly TimeSpan Product = TimeSpan.FromHours(1);           // 1 hour
    public static readonly TimeSpan ProductList = TimeSpan.FromMinutes(15);    // 15 minutes
    public static readonly TimeSpan Category = TimeSpan.FromHours(24);         // 24 hours (rarely changes)
    public static readonly TimeSpan Brand = TimeSpan.FromHours(24);            // 24 hours
    public static readonly TimeSpan ProductFeatured = TimeSpan.FromMinutes(30); // 30 minutes

    // Inventory Service
    public static readonly TimeSpan Inventory = TimeSpan.FromMinutes(5);       // 5 minutes (changes frequently)
    public static readonly TimeSpan StockAvailability = TimeSpan.FromMinutes(2); // 2 minutes

    // Discount Service
    public static readonly TimeSpan Discount = TimeSpan.FromMinutes(15);       // 15 minutes
    public static readonly TimeSpan DiscountValidation = TimeSpan.FromMinutes(5); // 5 minutes
    public static readonly TimeSpan FlashSale = TimeSpan.FromMinutes(1);       // 1 minute (real-time)
    public static readonly TimeSpan ActiveDiscounts = TimeSpan.FromMinutes(10); // 10 minutes

    // User Service
    public static readonly TimeSpan UserProfile = TimeSpan.FromMinutes(30);    // 30 minutes
    public static readonly TimeSpan UserAddresses = TimeSpan.FromMinutes(30);  // 30 minutes
    public static readonly TimeSpan UserPreferences = TimeSpan.FromHours(1);   // 1 hour
    public static readonly TimeSpan UserWishlist = TimeSpan.FromMinutes(15);   // 15 minutes

    // Auth Service
    public static readonly TimeSpan UserRoles = TimeSpan.FromMinutes(30);      // 30 minutes
    public static readonly TimeSpan UserPermissions = TimeSpan.FromMinutes(30); // 30 minutes
    public static readonly TimeSpan AllRoles = TimeSpan.FromHours(12);         // 12 hours

    // Order Service
    public static readonly TimeSpan Order = TimeSpan.FromMinutes(10);          // 10 minutes
    public static readonly TimeSpan Cart = TimeSpan.FromMinutes(30);           // 30 minutes

    // Payment Service
    public static readonly TimeSpan Payment = TimeSpan.FromMinutes(15);        // 15 minutes
    public static readonly TimeSpan GatewayConfigs = TimeSpan.FromHours(24);   // 24 hours

    // Rate Limiting
    public static readonly TimeSpan RateLimit = TimeSpan.FromMinutes(1);       // 1 minute window

    // Session
    public static readonly TimeSpan Session = TimeSpan.FromHours(24);          // 24 hours
    public static readonly TimeSpan ShortSession = TimeSpan.FromMinutes(15);   // 15 minutes
}


