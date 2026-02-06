using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Product.Domain.Entities;

namespace Product.Infrastructure.DB;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductVariantOption> ProductVariantOptions => Set<ProductVariantOption>();
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ProductTag> ProductTags => Set<ProductTag>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure DateTime converter for PostgreSQL (UTC)
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(e => e.Parent).WithMany(e => e.Children).HasForeignKey(e => e.ParentId);
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // Brand
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.ToTable("brands");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.LogoUrl).HasColumnName("logo_url");
            entity.Property(e => e.WebsiteUrl).HasColumnName("website_url");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // Product
        modelBuilder.Entity<ProductEntity>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.SellerName).HasColumnName("seller_name").HasMaxLength(255);
            entity.Property(e => e.SellerAvatar).HasColumnName("seller_avatar");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.BrandId).HasColumnName("brand_id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ShortDescription).HasColumnName("short_description").HasMaxLength(500);
            entity.Property(e => e.Sku).HasColumnName("sku").HasMaxLength(100);
            entity.Property(e => e.Barcode).HasColumnName("barcode").HasMaxLength(100);
            entity.Property(e => e.BasePrice).HasColumnName("base_price").HasColumnType("decimal(15,2)");
            entity.Property(e => e.SalePrice).HasColumnName("sale_price").HasColumnType("decimal(15,2)");
            entity.Property(e => e.CostPrice).HasColumnName("cost_price").HasColumnType("decimal(15,2)");
            entity.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(10);
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.MinOrderQuantity).HasColumnName("min_order_quantity");
            entity.Property(e => e.MaxOrderQuantity).HasColumnName("max_order_quantity");
            entity.Property(e => e.Weight).HasColumnName("weight").HasColumnType("decimal(10,2)");
            entity.Property(e => e.Length).HasColumnName("length").HasColumnType("decimal(10,2)");
            entity.Property(e => e.Width).HasColumnName("width").HasColumnType("decimal(10,2)");
            entity.Property(e => e.Height).HasColumnName("height").HasColumnType("decimal(10,2)");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.IsFeatured).HasColumnName("is_featured");
            entity.Property(e => e.IsDigital).HasColumnName("is_digital");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
            entity.Property(e => e.ViewCount).HasColumnName("view_count");
            entity.Property(e => e.SoldCount).HasColumnName("sold_count");
            entity.Property(e => e.RatingAverage).HasColumnName("rating_average").HasColumnType("decimal(3,2)");
            entity.Property(e => e.RatingCount).HasColumnName("rating_count");
            entity.Property(e => e.MetaTitle).HasColumnName("meta_title").HasMaxLength(255);
            entity.Property(e => e.MetaDescription).HasColumnName("meta_description");
            entity.Property(e => e.MetaKeywords).HasColumnName("meta_keywords");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId);
            entity.HasOne(e => e.Brand).WithMany().HasForeignKey(e => e.BrandId);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Sku).IsUnique();
        });

        // ProductImage
        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("product_images");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url").IsRequired();
            entity.Property(e => e.AltText).HasColumnName("alt_text").HasMaxLength(255);
            entity.Property(e => e.IsPrimary).HasColumnName("is_primary");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasOne(e => e.Product).WithMany(p => p.Images).HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        // ProductVariant
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("product_variants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Sku).HasColumnName("sku").HasMaxLength(100);
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Price).HasColumnName("price").HasColumnType("decimal(15,2)");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(e => e.Product).WithMany(p => p.Variants).HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        // ProductVariantOption
        modelBuilder.Entity<ProductVariantOption>(entity =>
        {
            entity.ToTable("product_variant_options");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");
            entity.Property(e => e.OptionName).HasColumnName("option_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.OptionValue).HasColumnName("option_value").HasMaxLength(255).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasOne(e => e.Variant).WithMany(v => v.Options).HasForeignKey(e => e.VariantId).OnDelete(DeleteBehavior.Cascade);
        });

        // ProductAttribute
        modelBuilder.Entity<ProductAttribute>(entity =>
        {
            entity.ToTable("product_attributes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.AttributeName).HasColumnName("attribute_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.AttributeValue).HasColumnName("attribute_value").IsRequired();
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasOne(e => e.Product).WithMany(p => p.Attributes).HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        // Tag
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("tags");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // ProductTag
        modelBuilder.Entity<ProductTag>(entity =>
        {
            entity.ToTable("product_tags");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.TagId).HasColumnName("tag_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasOne(e => e.Product).WithMany(p => p.Tags).HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Tag).WithMany(t => t.ProductTags).HasForeignKey(e => e.TagId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.ProductId, e.TagId }).IsUnique();
        });

        // ProductReview
        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.ToTable("product_reviews");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ReviewerName).HasColumnName("reviewer_name").HasMaxLength(255);
            entity.Property(e => e.ReviewerAvatar).HasColumnName("reviewer_avatar");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(255);
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.IsVerifiedPurchase).HasColumnName("is_verified_purchase");
            entity.Property(e => e.IsApproved).HasColumnName("is_approved");
            entity.Property(e => e.HelpfulCount).HasColumnName("helpful_count");
            entity.Property(e => e.Images).HasColumnName("images");
            entity.Property(e => e.SellerReply).HasColumnName("seller_reply");
            entity.Property(e => e.SellerReplyAt).HasColumnName("seller_reply_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(e => e.Product).WithMany(p => p.Reviews).HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

