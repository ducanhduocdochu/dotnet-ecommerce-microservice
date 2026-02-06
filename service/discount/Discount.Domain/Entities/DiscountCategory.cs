namespace Discount.Domain.Entities;

public class DiscountCategory
{
    public Guid Id { get; private set; }
    public Guid DiscountId { get; private set; }
    public Guid CategoryId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    // Navigation
    public DiscountEntity? Discount { get; private set; }
    
    private DiscountCategory() { }
    
    public DiscountCategory(Guid discountId, Guid categoryId)
    {
        Id = Guid.NewGuid();
        DiscountId = discountId;
        CategoryId = categoryId;
        CreatedAt = DateTime.UtcNow;
    }
}

