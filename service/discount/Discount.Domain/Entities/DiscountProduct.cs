namespace Discount.Domain.Entities;

public class DiscountProduct
{
    public Guid Id { get; private set; }
    public Guid DiscountId { get; private set; }
    public Guid ProductId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    // Navigation
    public DiscountEntity? Discount { get; private set; }
    
    private DiscountProduct() { }
    
    public DiscountProduct(Guid discountId, Guid productId)
    {
        Id = Guid.NewGuid();
        DiscountId = discountId;
        ProductId = productId;
        CreatedAt = DateTime.UtcNow;
    }
}

