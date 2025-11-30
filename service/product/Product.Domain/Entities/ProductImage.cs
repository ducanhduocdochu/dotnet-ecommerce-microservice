namespace Product.Domain.Entities;

public class ProductImage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; }
    public string ImageUrl { get; private set; } = null!;
    public string? AltText { get; private set; }
    public bool IsPrimary { get; private set; } = false;
    public int SortOrder { get; private set; } = 0;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public ProductEntity? Product { get; set; }

    private ProductImage() { }

    public ProductImage(Guid productId, string imageUrl, string? altText = null, bool isPrimary = false, int sortOrder = 0)
    {
        ProductId = productId;
        ImageUrl = imageUrl;
        AltText = altText;
        IsPrimary = isPrimary;
        SortOrder = sortOrder;
    }

    public void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
    public void Update(string? altText, int sortOrder)
    {
        AltText = altText;
        SortOrder = sortOrder;
    }
}

