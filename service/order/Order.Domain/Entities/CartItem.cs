namespace Order.Domain.Entities;

public class CartItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    
    // Product Info (denormalized)
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public string? ProductImage { get; private set; }
    public decimal ProductPrice { get; private set; }
    
    // Variant Info
    public Guid? VariantId { get; private set; }
    public string? VariantName { get; private set; }
    public decimal? VariantPrice { get; private set; }
    
    // Seller Info (denormalized)
    public Guid SellerId { get; private set; }
    public string? SellerName { get; private set; }
    
    public int Quantity { get; private set; } = 1;
    
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private CartItem() { }

    public CartItem(
        Guid userId,
        Guid productId,
        string productName,
        decimal productPrice,
        Guid sellerId,
        int quantity = 1,
        string? productImage = null,
        Guid? variantId = null,
        string? variantName = null,
        decimal? variantPrice = null,
        string? sellerName = null)
    {
        UserId = userId;
        ProductId = productId;
        ProductName = productName;
        ProductImage = productImage;
        ProductPrice = productPrice;
        VariantId = variantId;
        VariantName = variantName;
        VariantPrice = variantPrice;
        SellerId = sellerId;
        SellerName = sellerName;
        Quantity = quantity;
    }

    public void UpdateQuantity(int quantity)
    {
        Quantity = quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementQuantity(int amount = 1)
    {
        Quantity += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal GetTotalPrice() => (VariantPrice ?? ProductPrice) * Quantity;
}

