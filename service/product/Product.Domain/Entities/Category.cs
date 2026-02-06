namespace Product.Domain.Entities;

public class Category
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public Guid? ParentId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; } = 0;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();

    private Category() { }

    public Category(string name, string slug, string? description = null, string? imageUrl = null, Guid? parentId = null, int sortOrder = 0)
    {
        Name = name;
        Slug = slug;
        Description = description;
        ImageUrl = imageUrl;
        ParentId = parentId;
        SortOrder = sortOrder;
    }

    public void Update(string name, string slug, string? description, string? imageUrl, Guid? parentId, int sortOrder, bool isActive)
    {
        Name = name;
        Slug = slug;
        Description = description;
        ImageUrl = imageUrl;
        ParentId = parentId;
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}

