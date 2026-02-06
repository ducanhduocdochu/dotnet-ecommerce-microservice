namespace Product.Domain.Entities;

public class Tag
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();

    private Tag() { }

    public Tag(string name, string slug)
    {
        Name = name;
        Slug = slug;
    }
}

