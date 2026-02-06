namespace Discount.Domain.Entities;

public class Promotion
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    
    // Banner
    public string? BannerUrl { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    
    // Validity
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    
    // Status
    public bool IsActive { get; private set; } = true;
    public bool IsFeatured { get; private set; }
    
    // Display
    public int DisplayOrder { get; private set; }
    
    public Guid? CreatedBy { get; private set; }
    public string? CreatedByName { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    // Navigation
    public ICollection<PromotionDiscount> PromotionDiscounts { get; private set; } = new List<PromotionDiscount>();
    
    private Promotion() { }
    
    public Promotion(
        string code,
        string name,
        string? description,
        string? bannerUrl,
        string? thumbnailUrl,
        DateTime startDate,
        DateTime endDate,
        bool isFeatured,
        int displayOrder,
        Guid? createdBy,
        string? createdByName)
    {
        Id = Guid.NewGuid();
        Code = code.ToUpperInvariant();
        Name = name;
        Description = description;
        BannerUrl = bannerUrl;
        ThumbnailUrl = thumbnailUrl;
        StartDate = startDate;
        EndDate = endDate;
        IsFeatured = isFeatured;
        DisplayOrder = displayOrder;
        CreatedBy = createdBy;
        CreatedByName = createdByName;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Update(
        string name,
        string? description,
        string? bannerUrl,
        string? thumbnailUrl,
        DateTime startDate,
        DateTime endDate,
        bool isFeatured,
        int displayOrder)
    {
        Name = name;
        Description = description;
        BannerUrl = bannerUrl;
        ThumbnailUrl = thumbnailUrl;
        StartDate = startDate;
        EndDate = endDate;
        IsFeatured = isFeatured;
        DisplayOrder = displayOrder;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public bool IsValid()
    {
        var now = DateTime.UtcNow;
        return IsActive && StartDate <= now && EndDate >= now;
    }
}

