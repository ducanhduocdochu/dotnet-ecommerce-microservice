namespace Product.Domain.Entities;

public class ProductVariant
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; }
    public string? Sku { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal? Price { get; private set; }
    public int Quantity { get; private set; } = 0;
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public ProductEntity? Product { get; set; }
    public ICollection<ProductVariantOption> Options { get; set; } = new List<ProductVariantOption>();

    private ProductVariant() { }

    public ProductVariant(Guid productId, string name, string? sku = null, decimal? price = null, int quantity = 0, string? imageUrl = null)
    {
        ProductId = productId;
        Name = name;
        Sku = sku;
        Price = price;
        Quantity = quantity;
        ImageUrl = imageUrl;
    }

    public void Update(string name, string? sku, decimal? price, int quantity, string? imageUrl, bool isActive)
    {
        Name = name;
        Sku = sku;
        Price = price;
        Quantity = quantity;
        ImageUrl = imageUrl;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}

