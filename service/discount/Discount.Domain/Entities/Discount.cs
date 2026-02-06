namespace Discount.Domain.Entities;

public class DiscountEntity
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    
    // Discount Type
    public string Type { get; private set; } = string.Empty; // PERCENTAGE, FIXED_AMOUNT, FREE_SHIPPING, BUY_X_GET_Y
    
    // Discount Value
    public decimal Value { get; private set; }
    public decimal? MaxDiscountAmount { get; private set; }
    
    // Minimum Requirements
    public decimal MinOrderAmount { get; private set; }
    public int MinQuantity { get; private set; }
    
    // Buy X Get Y
    public int? BuyQuantity { get; private set; }
    public int? GetQuantity { get; private set; }
    public decimal? GetDiscountPercent { get; private set; }
    
    // Usage Limits
    public int? UsageLimit { get; private set; }
    public int UsageLimitPerUser { get; private set; } = 1;
    public int UsageCount { get; private set; }
    
    // Validity
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    
    // Scope
    public string Scope { get; private set; } = "ALL"; // ALL, SPECIFIC_PRODUCTS, SPECIFIC_CATEGORIES, SPECIFIC_USERS
    
    // Status
    public bool IsActive { get; private set; } = true;
    public bool IsPublic { get; private set; } = true;
    
    // Priority
    public int Priority { get; private set; }
    public bool IsStackable { get; private set; }
    
    // Created by
    public Guid? CreatedBy { get; private set; }
    public string? CreatedByName { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    // Navigation
    public ICollection<DiscountProduct> DiscountProducts { get; private set; } = new List<DiscountProduct>();
    public ICollection<DiscountCategory> DiscountCategories { get; private set; } = new List<DiscountCategory>();
    public ICollection<DiscountUser> DiscountUsers { get; private set; } = new List<DiscountUser>();
    public ICollection<DiscountUsage> DiscountUsages { get; private set; } = new List<DiscountUsage>();
    
    private DiscountEntity() { }
    
    public DiscountEntity(
        string code,
        string name,
        string? description,
        string type,
        decimal value,
        decimal? maxDiscountAmount,
        decimal minOrderAmount,
        int minQuantity,
        int? buyQuantity,
        int? getQuantity,
        decimal? getDiscountPercent,
        int? usageLimit,
        int usageLimitPerUser,
        DateTime startDate,
        DateTime endDate,
        string scope,
        bool isPublic,
        bool isStackable,
        int priority,
        Guid? createdBy,
        string? createdByName)
    {
        Id = Guid.NewGuid();
        Code = code.ToUpperInvariant();
        Name = name;
        Description = description;
        Type = type;
        Value = value;
        MaxDiscountAmount = maxDiscountAmount;
        MinOrderAmount = minOrderAmount;
        MinQuantity = minQuantity;
        BuyQuantity = buyQuantity;
        GetQuantity = getQuantity;
        GetDiscountPercent = getDiscountPercent;
        UsageLimit = usageLimit;
        UsageLimitPerUser = usageLimitPerUser;
        StartDate = startDate;
        EndDate = endDate;
        Scope = scope;
        IsPublic = isPublic;
        IsStackable = isStackable;
        Priority = priority;
        CreatedBy = createdBy;
        CreatedByName = createdByName;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Update(
        string name,
        string? description,
        decimal value,
        decimal? maxDiscountAmount,
        decimal minOrderAmount,
        int minQuantity,
        int? usageLimit,
        int usageLimitPerUser,
        DateTime startDate,
        DateTime endDate,
        string scope,
        bool isPublic,
        bool isStackable,
        int priority)
    {
        Name = name;
        Description = description;
        Value = value;
        MaxDiscountAmount = maxDiscountAmount;
        MinOrderAmount = minOrderAmount;
        MinQuantity = minQuantity;
        UsageLimit = usageLimit;
        UsageLimitPerUser = usageLimitPerUser;
        StartDate = startDate;
        EndDate = endDate;
        Scope = scope;
        IsPublic = isPublic;
        IsStackable = isStackable;
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void IncrementUsageCount()
    {
        UsageCount++;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void DecrementUsageCount()
    {
        if (UsageCount > 0)
        {
            UsageCount--;
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    public bool IsValid()
    {
        var now = DateTime.UtcNow;
        return IsActive && 
               StartDate <= now && 
               EndDate >= now &&
               (UsageLimit == null || UsageCount < UsageLimit);
    }
    
    public decimal CalculateDiscount(decimal orderAmount, int itemCount = 1)
    {
        if (!IsValid()) return 0;
        if (orderAmount < MinOrderAmount) return 0;
        if (itemCount < MinQuantity) return 0;
        
        decimal discount = Type switch
        {
            "PERCENTAGE" => orderAmount * (Value / 100),
            "FIXED_AMOUNT" => Value,
            "FREE_SHIPPING" => Value, // Value = shipping fee
            _ => 0
        };
        
        if (MaxDiscountAmount.HasValue && discount > MaxDiscountAmount.Value)
        {
            discount = MaxDiscountAmount.Value;
        }
        
        return Math.Min(discount, orderAmount);
    }
}

public static class DiscountTypes
{
    public const string Percentage = "PERCENTAGE";
    public const string FixedAmount = "FIXED_AMOUNT";
    public const string FreeShipping = "FREE_SHIPPING";
    public const string BuyXGetY = "BUY_X_GET_Y";
}

public static class DiscountScopes
{
    public const string All = "ALL";
    public const string SpecificProducts = "SPECIFIC_PRODUCTS";
    public const string SpecificCategories = "SPECIFIC_CATEGORIES";
    public const string SpecificUsers = "SPECIFIC_USERS";
}

