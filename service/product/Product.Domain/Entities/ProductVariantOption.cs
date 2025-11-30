namespace Product.Domain.Entities;

public class ProductVariantOption
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid VariantId { get; private set; }
    public string OptionName { get; private set; } = null!;
    public string OptionValue { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public ProductVariant? Variant { get; set; }

    private ProductVariantOption() { }

    public ProductVariantOption(Guid variantId, string optionName, string optionValue)
    {
        VariantId = variantId;
        OptionName = optionName;
        OptionValue = optionValue;
    }
}

