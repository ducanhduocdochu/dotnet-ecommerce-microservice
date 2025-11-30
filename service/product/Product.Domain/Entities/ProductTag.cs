namespace Product.Domain.Entities;

public class ProductTag
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; }
    public Guid TagId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public ProductEntity? Product { get; set; }
    public Tag? Tag { get; set; }

    private ProductTag() { }

    public ProductTag(Guid productId, Guid tagId)
    {
        ProductId = productId;
        TagId = tagId;
    }
}

