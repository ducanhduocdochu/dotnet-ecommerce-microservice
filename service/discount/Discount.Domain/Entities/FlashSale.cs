namespace Discount.Domain.Entities;

public class FlashSale
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    
    // Timing
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    
    // Status
    public bool IsActive { get; private set; } = true;
    
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    // Navigation
    public ICollection<FlashSaleItem> FlashSaleItems { get; private set; } = new List<FlashSaleItem>();
    
    private FlashSale() { }
    
    public FlashSale(string name, DateTime startTime, DateTime endTime)
    {
        Id = Guid.NewGuid();
        Name = name;
        StartTime = startTime;
        EndTime = endTime;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Update(string name, DateTime startTime, DateTime endTime, bool isActive)
    {
        Name = name;
        StartTime = startTime;
        EndTime = endTime;
        IsActive = isActive;
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
        return IsActive && StartTime <= now && EndTime >= now;
    }
}

