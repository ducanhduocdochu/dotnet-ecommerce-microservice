namespace Product.Domain.Entities;

public class ProductAttribute
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; }
    public string AttributeName { get; private set; } = null!;
    public string AttributeValue { get; private set; } = null!;
    public int SortOrder { get; private set; } = 0;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public ProductEntity? Product { get; set; }

    private ProductAttribute() { }

    public ProductAttribute(Guid productId, string attributeName, string attributeValue, int sortOrder = 0)
    {
        ProductId = productId;
        AttributeName = attributeName;
        AttributeValue = attributeValue;
        SortOrder = sortOrder;
    }

    public void Update(string attributeName, string attributeValue, int sortOrder)
    {
        AttributeName = attributeName;
        AttributeValue = attributeValue;
        SortOrder = sortOrder;
    }
}

