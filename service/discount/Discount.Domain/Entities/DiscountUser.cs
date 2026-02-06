namespace Discount.Domain.Entities;

public class DiscountUser
{
    public Guid Id { get; private set; }
    public Guid DiscountId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    // Navigation
    public DiscountEntity? Discount { get; private set; }
    
    private DiscountUser() { }
    
    public DiscountUser(Guid discountId, Guid userId)
    {
        Id = Guid.NewGuid();
        DiscountId = discountId;
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
    }
}

