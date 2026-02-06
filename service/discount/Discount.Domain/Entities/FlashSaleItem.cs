namespace Discount.Domain.Entities;

public class FlashSaleItem
{
    public Guid Id { get; private set; }
    public Guid FlashSaleId { get; private set; }
    
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    
    // Pricing
    public decimal OriginalPrice { get; private set; }
    public decimal SalePrice { get; private set; }
    public decimal? DiscountPercent { get; private set; }
    
    // Quantity
    public int QuantityLimit { get; private set; }
    public int QuantitySold { get; private set; }
    
    // Per user limit
    public int LimitPerUser { get; private set; } = 1;
    
    public DateTime CreatedAt { get; private set; }
    
    // Navigation
    public FlashSale? FlashSale { get; private set; }
    
    private FlashSaleItem() { }
    
    public FlashSaleItem(
        Guid flashSaleId,
        Guid productId,
        Guid? variantId,
        decimal originalPrice,
        decimal salePrice,
        int quantityLimit,
        int limitPerUser)
    {
        Id = Guid.NewGuid();
        FlashSaleId = flashSaleId;
        ProductId = productId;
        VariantId = variantId;
        OriginalPrice = originalPrice;
        SalePrice = salePrice;
        DiscountPercent = originalPrice > 0 ? Math.Round((1 - salePrice / originalPrice) * 100, 2) : 0;
        QuantityLimit = quantityLimit;
        LimitPerUser = limitPerUser;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void Update(decimal salePrice, int quantityLimit, int limitPerUser)
    {
        SalePrice = salePrice;
        DiscountPercent = OriginalPrice > 0 ? Math.Round((1 - salePrice / OriginalPrice) * 100, 2) : 0;
        QuantityLimit = quantityLimit;
        LimitPerUser = limitPerUser;
    }
    
    public bool IsAvailable()
    {
        return QuantitySold < QuantityLimit;
    }
    
    public int GetQuantityRemaining()
    {
        return Math.Max(0, QuantityLimit - QuantitySold);
    }
    
    public void IncrementSold(int quantity = 1)
    {
        QuantitySold += quantity;
    }
}

