namespace Product.Domain.Entities;

public class Brand
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private Brand() { }

    public Brand(string name, string slug, string? description = null, string? logoUrl = null, string? websiteUrl = null)
    {
        Name = name;
        Slug = slug;
        Description = description;
        LogoUrl = logoUrl;
        WebsiteUrl = websiteUrl;
    }

    public void Update(string name, string slug, string? description, string? logoUrl, string? websiteUrl, bool isActive)
    {
        Name = name;
        Slug = slug;
        Description = description;
        LogoUrl = logoUrl;
        WebsiteUrl = websiteUrl;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}

