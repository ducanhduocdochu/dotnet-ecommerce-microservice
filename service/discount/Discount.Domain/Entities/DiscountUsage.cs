namespace Discount.Domain.Entities;

public class DiscountUsage
{
    public Guid Id { get; private set; }
    public Guid DiscountId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid OrderId { get; private set; }
    public string? OrderNumber { get; private set; }
    
    // Amount
    public decimal OrderAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    
    // Navigation
    public DiscountEntity? Discount { get; private set; }
    
    private DiscountUsage() { }
    
    public DiscountUsage(
        Guid discountId,
        Guid userId,
        Guid orderId,
        string? orderNumber,
        decimal orderAmount,
        decimal discountAmount)
    {
        Id = Guid.NewGuid();
        DiscountId = discountId;
        UserId = userId;
        OrderId = orderId;
        OrderNumber = orderNumber;
        OrderAmount = orderAmount;
        DiscountAmount = discountAmount;
        CreatedAt = DateTime.UtcNow;
    }
}

