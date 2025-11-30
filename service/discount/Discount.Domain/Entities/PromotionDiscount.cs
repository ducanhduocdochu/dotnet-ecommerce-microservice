namespace Discount.Domain.Entities;

public class PromotionDiscount
{
    public Guid Id { get; private set; }
    public Guid PromotionId { get; private set; }
    public Guid DiscountId { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    // Navigation
    public Promotion? Promotion { get; private set; }
    public DiscountEntity? Discount { get; private set; }
    
    private PromotionDiscount() { }
    
    public PromotionDiscount(Guid promotionId, Guid discountId, int displayOrder = 0)
    {
        Id = Guid.NewGuid();
        PromotionId = promotionId;
        DiscountId = discountId;
        DisplayOrder = displayOrder;
        CreatedAt = DateTime.UtcNow;
    }
}

