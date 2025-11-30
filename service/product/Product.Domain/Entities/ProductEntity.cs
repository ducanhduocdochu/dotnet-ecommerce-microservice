namespace Product.Domain.Entities;

public class ProductEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid SellerId { get; private set; }
    public string? SellerName { get; private set; }
    public string? SellerAvatar { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Guid? BrandId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? ShortDescription { get; private set; }
    public string? Sku { get; private set; }
    public string? Barcode { get; private set; }
    public decimal BasePrice { get; private set; }
    public decimal? SalePrice { get; private set; }
    public decimal? CostPrice { get; private set; }
    public string Currency { get; private set; } = "VND";
    public int Quantity { get; private set; } = 0;
    public int MinOrderQuantity { get; private set; } = 1;
    public int? MaxOrderQuantity { get; private set; }
    public decimal? Weight { get; private set; }
    public decimal? Length { get; private set; }
    public decimal? Width { get; private set; }
    public decimal? Height { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsFeatured { get; private set; } = false;
    public bool IsDigital { get; private set; } = false;
    public string Status { get; private set; } = "DRAFT"; // DRAFT, PENDING, APPROVED, REJECTED, PUBLISHED
    public int ViewCount { get; private set; } = 0;
    public int SoldCount { get; private set; } = 0;
    public decimal RatingAverage { get; private set; } = 0;
    public int RatingCount { get; private set; } = 0;
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? MetaKeywords { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public Category? Category { get; set; }
    public Brand? Brand { get; set; }
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
    public ICollection<ProductTag> Tags { get; set; } = new List<ProductTag>();
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();

    private ProductEntity() { }

    public ProductEntity(
        Guid sellerId,
        string name,
        string slug,
        decimal basePrice,
        string? sellerName = null,
        string? sellerAvatar = null,
        Guid? categoryId = null,
        Guid? brandId = null,
        string? description = null,
        string? shortDescription = null,
        string? sku = null)
    {
        SellerId = sellerId;
        SellerName = sellerName;
        SellerAvatar = sellerAvatar;
        Name = name;
        Slug = slug;
        BasePrice = basePrice;
        CategoryId = categoryId;
        BrandId = brandId;
        Description = description;
        ShortDescription = shortDescription;
        Sku = sku;
    }

    public void UpdateSellerInfo(string? sellerName, string? sellerAvatar)
    {
        SellerName = sellerName;
        SellerAvatar = sellerAvatar;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(
        string name,
        string slug,
        string? description,
        string? shortDescription,
        Guid? categoryId,
        Guid? brandId,
        decimal basePrice,
        decimal? salePrice,
        decimal? costPrice,
        string? sku,
        int quantity,
        decimal? weight,
        decimal? length,
        decimal? width,
        decimal? height,
        bool isDigital)
    {
        Name = name;
        Slug = slug;
        Description = description;
        ShortDescription = shortDescription;
        CategoryId = categoryId;
        BrandId = brandId;
        BasePrice = basePrice;
        SalePrice = salePrice;
        CostPrice = costPrice;
        Sku = sku;
        Quantity = quantity;
        Weight = weight;
        Length = length;
        Width = width;
        Height = height;
        IsDigital = isDigital;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Submit() { Status = "PENDING"; UpdatedAt = DateTime.UtcNow; }
    public void Approve() { Status = "PUBLISHED"; UpdatedAt = DateTime.UtcNow; }
    public void Reject() { Status = "REJECTED"; UpdatedAt = DateTime.UtcNow; }
    public void SetFeatured(bool featured) { IsFeatured = featured; UpdatedAt = DateTime.UtcNow; }
    public void IncrementViewCount() { ViewCount++; }
    public void UpdateSoldCount(int count) { SoldCount += count; UpdatedAt = DateTime.UtcNow; }
    public void UpdateRating(decimal average, int count) { RatingAverage = average; RatingCount = count; UpdatedAt = DateTime.UtcNow; }
    public void UpdateQuantity(int quantity) { Quantity = quantity; UpdatedAt = DateTime.UtcNow; }
}

